using Business.Interfaces.Implements.Business;
using Hangfire;
using Hangfire.Server;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace WebGESCOMPH.RealTime
{
    public sealed class ObligationJobs
    {
        private readonly IObligationMonthService _svc;
        private readonly ILogger<ObligationJobs> _log;
        private readonly IConfiguration _cfg;
        private readonly IHubContext<ObligationHub> _hub;

        public ObligationJobs(IObligationMonthService svc, ILogger<ObligationJobs> log, IConfiguration cfg, IHubContext<ObligationHub> hub)
        {
            _svc = svc;
            _log = log;
            _cfg = cfg;
            _hub = hub;
        }

        [DisableConcurrentExecution(timeoutInSeconds: 60 * 60)]
        [AutomaticRetry(Attempts = 0)]
        public async Task GenerateForCurrentMonthAsync(IJobCancellationToken jobToken)
        {
            jobToken?.ThrowIfCancellationRequested();

            var tzId = _cfg["Hangfire:TimeZoneIana"] ?? "America/Bogota";
            var tz = TZConvert.GetTimeZoneInfo(tzId);
            var nowLocal = TimeZoneInfo.ConvertTime(DateTime.UtcNow, tz);

            var year = nowLocal.Year;
            var month = nowLocal.Month;

            _log.LogInformation("Generando obligaciones para {Year}-{Month}", year, month);
            await _svc.GenerateMonthlyAsync(year, month);

            // 👇 Aquí usamos los métodos que ya tienes
            var totalDay = await _svc.GetTotalObligationsPaidByDayAsync(nowLocal);
            var totalMonth = await _svc.GetTotalObligationsPaidByMonthAsync(year, month);

            // 👇 Mandamos los datos en tiempo real por SignalR
            await _hub.Clients.All.SendAsync("ReceiveTotals", new
            {
                TotalDay = totalDay,
                TotalMonth = totalMonth
            });

            _log.LogInformation("OK obligaciones {Year}-{Month}. Totales enviados por SignalR", year, month);
        }

        [DisableConcurrentExecution(timeoutInSeconds: 60 * 60)]
        [Queue("maintenance")]
        [AutomaticRetry(Attempts = 0)]
        public async Task GenerateForPeriodAsync(int year, int month, IJobCancellationToken jobToken)
        {
            jobToken?.ThrowIfCancellationRequested();

            if (month is < 1 or > 12)
            {
                _log.LogWarning("Mes inválido {Month} para año {Year}", month, year);
                return;
            }

            _log.LogInformation("Generando obligaciones (ad-hoc) para {Year}-{Month}", year, month);
            await _svc.GenerateMonthlyAsync(year, month);

            var totalMonth = await _svc.GetTotalObligationsPaidByMonthAsync(year, month);

            await _hub.Clients.All.SendAsync("ReceiveTotals", new
            {
                TotalDay = 0m, // 👈 aquí solo mandamos mes porque es ad-hoc
                TotalMonth = totalMonth
            });

            _log.LogInformation("OK obligaciones (ad-hoc) {Year}-{Month}. Totales enviados por SignalR", year, month);
        }
    }
}