using Business.Interfaces.Implements.Business;
using Hangfire;
using Hangfire.Server;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using TimeZoneConverter;

namespace WebGESCOMPH.RealTime.Obligations
{
    /// <summary>
    /// Define los trabajos programados (jobs) relacionados con la generación 
    /// y notificación de obligaciones mensuales.
    /// 
    /// Esta clase es gestionada por Hangfire y se encarga de generar las 
    /// obligaciones del mes actual o de un período específico, notificando 
    /// los totales actualizados mediante SignalR.
    /// </summary>
    /// <remarks>
    /// Los métodos de esta clase están protegidos contra ejecución concurrente 
    /// y no realizan reintentos automáticos para evitar duplicaciones de datos.  
    /// 
    /// Se integran tres componentes clave:
    /// <list type="bullet">
    /// <item><see cref="IObligationMonthService"/> → Lógica de negocio para generar obligaciones.</item>
    /// <item><see cref="IHubContext{THub}"/> → Comunicación en tiempo real con clientes mediante SignalR.</item>
    /// <item><see cref="ILogger"/> y <see cref="IConfiguration"/> → Registro y configuración de entorno.</item>
    /// </list>
    /// </remarks>
    public sealed class ObligationJobs
    {
        private readonly IObligationMonthService _svc;
        private readonly ILogger<ObligationJobs> _log;
        private readonly IConfiguration _cfg;
        private readonly IHubContext<ObligationHub> _hub;

        /// <summary>
        /// Inicializa una nueva instancia del trabajo de generación de obligaciones.
        /// </summary>
        /// <param name="svc">Servicio de negocio responsable de generar las obligaciones mensuales.</param>
        /// <param name="log">Logger utilizado para registrar eventos operativos y diagnósticos.</param>
        /// <param name="cfg">Configuración de aplicación (utilizada para resolver la zona horaria).</param>
        /// <param name="hub">Contexto de SignalR utilizado para enviar datos en tiempo real.</param>
        public ObligationJobs(
            IObligationMonthService svc,
            ILogger<ObligationJobs> log,
            IConfiguration cfg,
            IHubContext<ObligationHub> hub)
        {
            _svc = svc;
            _log = log;
            _cfg = cfg;
            _hub = hub;
        }

        /// <summary>
        /// Genera las obligaciones correspondientes al mes actual y 
        /// notifica los totales diarios y mensuales en tiempo real mediante SignalR.
        /// </summary>
        /// <param name="jobToken">
        /// Token de cancelación proporcionado por Hangfire, que permite 
        /// interrumpir el trabajo si es necesario.
        /// </param>
        /// <remarks>
        /// - La zona horaria se determina a partir de la configuración (<c>Hangfire:TimeZoneIana</c>), 
        ///   por defecto <c>"America/Bogota"</c>.  
        /// - El método ejecuta <see cref="IObligationMonthService.GenerateMonthlyAsync(int, int)"/> 
        ///   para crear las obligaciones.  
        /// - Luego consulta los totales diarios y mensuales y los envía 
        ///   al canal SignalR con el evento <c>"ReceiveTotals"</c>.  
        /// </remarks>
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

            // Totales actualizados
            var totalDay = await _svc.GetTotalObligationsPaidByDayAsync(nowLocal);
            var totalMonth = await _svc.GetTotalObligationsPaidByMonthAsync(year, month);

            // Emisión en tiempo real
            await _hub.Clients.All.SendAsync("ReceiveTotals", new
            {
                TotalDay = totalDay,
                TotalMonth = totalMonth
            });

            _log.LogInformation("OK obligaciones {Year}-{Month}. Totales enviados por SignalR", year, month);
        }

        /// <summary>
        /// Genera las obligaciones para un período específico (año y mes) 
        /// y envía los totales mensuales mediante SignalR.
        /// </summary>
        /// <param name="year">Año del período objetivo.</param>
        /// <param name="month">Mes del período objetivo (1–12).</param>
        /// <param name="jobToken">Token de cancelación de Hangfire.</param>
        /// <remarks>
        /// - Este trabajo se ejecuta en la cola <c>"maintenance"</c>.  
        /// - Valida que el mes esté dentro del rango válido antes de continuar.  
        /// - No calcula totales diarios, solo mensuales, por ser ejecución ad-hoc.  
        /// - Emite el evento <c>"ReceiveTotals"</c> con <c>TotalDay = 0</c>.  
        /// </remarks>
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
                TotalDay = 0m, // Solo se notifica el mes en ejecuciones ad-hoc
                TotalMonth = totalMonth
            });

            _log.LogInformation("OK obligaciones (ad-hoc) {Year}-{Month}. Totales enviados por SignalR", year, month);
        }
    }
}
