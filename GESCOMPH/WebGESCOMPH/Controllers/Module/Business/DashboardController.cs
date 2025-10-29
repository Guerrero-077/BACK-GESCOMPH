using Business.Interfaces.Implements.Business;
using Business.Services.Business;
using Microsoft.AspNetCore.Mvc;

namespace WebGESCOMPH.Controllers.Module.Business
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly ILogger<DashboardController> _logger;
        private readonly IObligationMonthService _svc;

        public DashboardController(ILogger<DashboardController> logger, IObligationMonthService obligationService)
        {
            _logger = logger;
            _svc = obligationService;

        }

        /// <summary>
        /// Metodos que se encargan de obtener el total de obligaciones pagadas por día y por mes.
        /// </summary>
        /// <returns></returns>
        /// 
        [HttpGet("TotalDay")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetTotalDay()
        {
            try
            {
                var totalDay = await _svc.GetTotalObligationsPaidByDayAsync(DateTime.UtcNow);
                return Ok(totalDay);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo total de obligaciones pagadas por día");
                return BadRequest("Error obteniendo total de obligaciones pagadas por día");
            }
        }

        [HttpGet("TotalMonth")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetTotalMonth()
        {
            try
            {
                var today = DateTime.UtcNow;
                var totalMonth = await _svc.GetTotalObligationsPaidByMonthAsync(today.Year, today.Month);
                return Ok(totalMonth);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo total de obligaciones pagadas por mes");
                return BadRequest("Error obteniendo total de obligaciones pagadas por mes");
            }
        }

        [HttpGet("LastSixMonthsPaid")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLastSixMonthsPaid()
        {
            var data = await _svc.GetLastSixMonthsPaidAsync();
            return Ok(data);
        }
    }
}
