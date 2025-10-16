using Business.Interfaces.Implements.Business;
using Business.Interfaces.PDF;
using System;
using Entity.DTOs.Implements.Business.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Utilities.Exceptions;
using WebGESCOMPH.Contracts.Requests;
using WebGESCOMPH.RealTime;
using System.Linq;
using WebGESCOMPH.RealTime.Contract;
using Business.Interfaces.Notifications;

namespace WebGESCOMPH.Controllers.Module.Business
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ContractController : ControllerBase
    {
        private readonly IContractService _contractService;
        private readonly IContractPdfGeneratorService _pdfService;
        private readonly IHubContext<ContractsHub> _hub;
        private readonly IContractNotificationService _notify;
        private readonly ILogger<ContractController> _logger;

        public ContractController(
            IContractService contractService,
            IContractPdfGeneratorService pdfService,
            ILogger<ContractController> logger,
            IHubContext<ContractsHub> hub,
            IContractNotificationService notify)
        {
            _contractService = contractService;
            _pdfService = pdfService;
            _logger = logger;
            _hub = hub;
            _notify = notify;
        }

        [HttpGet("mine")]
        [ProducesResponseType(typeof(IEnumerable<ContractCardDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMine()
        {
            var result = await _contractService.GetMineAsync();
            return Ok(result);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ContractSelectDto>> Post([FromBody] ContractCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var contractId = await _contractService.CreateContractWithPersonHandlingAsync(dto);

                // Notificar al tenant propietario usando el servicio de notificaciones
                var createdContract = await _contractService.GetByIdAsync(contractId);
                if (createdContract != null)
                    await _notify.NotifyContractCreated(contractId, createdContract.PersonId);

                return CreatedAtAction(nameof(GetById), new { id = contractId }, new { contractId });
            }
            catch (BusinessException ex)
            {
                _logger.LogWarning(ex, "Error de negocio al crear contrato: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al crear contrato");
                throw;
            }
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ContractSelectDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var contract = await _contractService.GetByIdAsync(id);
            if (contract == null) return NotFound();

            return Ok(contract);
        }

        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ContractSelectDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ContractSelectDto>> Put(int id, [FromBody] ContractUpdateDto dto)
        {
            if (dto is null)
                return BadRequest(new { detail = "El cuerpo de la solicitud no puede estar vacio." });

            if (dto.Id != 0 && dto.Id != id)
                return BadRequest(new { detail = "El ID del cuerpo no coincide con el ID de la ruta." });

            dto.Id = id;

            try
            {
                var updated = await _contractService.UpdateAsync(dto);

                await _hub.Clients
                    .Group($"tenant-{updated.PersonId}")
                    .SendAsync("contracts:mutated", new
                    {
                        type = "updated",
                        id,
                        active = updated.Active,
                        at = DateTime.UtcNow
                    });

                return Ok(updated);
            }
            catch (BusinessException ex)
            {
                _logger.LogWarning(ex, "Error de negocio al actualizar contrato {Id}: {Message}", id, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al actualizar contrato {Id}", id);
                throw;
            }
        }

        [HttpPatch("{id:int}/estado")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ChangeActiveStatus(int id, [FromBody] ChangeActiveStatusRequest body)
        {
            try
            {
                var active = body.Active!.Value;
                await _contractService.UpdateActiveStatusAsync(id, active);

                var after = await _contractService.GetByIdAsync(id);
                if (after != null)
                    await _notify.NotifyContractStatusChanged(id, active, after.PersonId);

                return NoContent();
            }
            catch (BusinessException ex)
            {
                _logger.LogWarning(ex, "Error de negocio al actualizar estado del contrato {Id}: {Message}", id, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al actualizar el estado del contrato {Id}", id);
                throw;
            }
        }

        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var existing = await _contractService.GetByIdAsync(id);
                if (existing == null)
                    return NotFound();

                await _contractService.DeleteAsync(id);

                await _notify.NotifyContractDeleted(id, existing.PersonId);

                return NoContent();
            }
            catch (BusinessException ex)
            {
                _logger.LogWarning(ex, "Error de negocio al eliminar contrato {Id}: {Message}", id, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado al eliminar contrato {Id}", id);
                throw;
            }
        }

        [HttpGet("{id:int}/pdf")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadContractPdf(int id)
        {
            var contract = await _contractService.GetByIdAsync(id);
            if (contract == null)
            {
                _logger.LogWarning("Contrato con ID {Id} no encontrado.", id);
                return NotFound(new { message = $"No se encontro un contrato con ID {id}" });
            }

            try
            {
                var pdfBytes = await _pdfService.GeneratePdfAsync(contract);
                return File(pdfBytes, "application/pdf", $"Contrato_{contract.FullName}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando PDF para contrato {Id}", id);

                var debug = HttpContext.Request.Query.TryGetValue("debug", out var dv) && dv.ToString() == "1";
                if (debug)
                {
                    return StatusCode(500, new
                    {
                        error = "Error generando PDF",
                        message = ex.Message,
                        inner = ex.InnerException?.Message,
                        stack = ex.StackTrace,
                    });
                }

                return StatusCode(500, new { error = "Error interno al generar el PDF." });
            }
        }

        [HttpGet("{id:int}/obligations")]
        [ProducesResponseType(typeof(IEnumerable<Entity.DTOs.Implements.Business.ObligationMonth.ObligationMonthSelectDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetObligations(int id)
        {
            var contract = await _contractService.GetByIdAsync(id);
            if (contract == null) return NotFound();

            var obligations = await _contractService.GetObligationsAsync(id);
            return Ok(obligations);
        }

        [HttpPost("expire/run")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> RunExpirationNow(CancellationToken ct)
        {
            var sweep = await _contractService.RunExpirationSweepAsync(ct);
            var deactivated = sweep.DeactivatedContractIds ?? Array.Empty<int>();

            // Resolver personId por contrato y notificar por grupo
            var perPerson = new Dictionary<int, List<int>>();
            foreach (var cid in deactivated)
            {
                var c = await _contractService.GetByIdAsync(cid);
                if (c == null) continue;
                if (!perPerson.TryGetValue(c.PersonId, out var list))
                {
                    list = new List<int>();
                    perPerson[c.PersonId] = list;
                }
                list.Add(cid);
            }

            foreach (var kv in perPerson)
            {
                var ids = kv.Value.Distinct().ToArray();
                var payloadForPerson = new
                {
                    deactivatedIds = ids,
                    counts = new
                    {
                        deactivatedContracts = ids.Length,
                        reactivatedEstablishments = sweep.ReactivatedEstablishments
                    },
                    at = DateTime.UtcNow
                };
                await _hub.Clients.Group($"tenant-{kv.Key}").SendAsync("contracts:expired", payloadForPerson, ct);
            }

            // Para respuesta HTTP devolvemos el resumen global
            var response = new
            {
                deactivatedIds = deactivated,
                counts = new
                {
                    deactivatedContracts = deactivated.Count,
                    reactivatedEstablishments = sweep.ReactivatedEstablishments
                },
                at = DateTime.UtcNow
            };
            return Ok(response);
        }
    }
}
