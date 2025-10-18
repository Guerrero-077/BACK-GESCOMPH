using Business.CustomJWT;
using Business.Interfaces;
using Business.Interfaces.Implements.Business;
using Business.Interfaces.Implements.Persons;
using Business.Interfaces.Implements.SecurityAuthentication;
using Business.Interfaces.PDF;
using Business.Repository;
using Data.Interfaz.IDataImplement.Business;
using Entity.Domain.Models.Implements.Business;
using Entity.DTOs.Implements.Business.Contract;
using Entity.DTOs.Implements.Business.ObligationMonth;
using Entity.DTOs.Implements.Persons.Person;
using Entity.Infrastructure.Context;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Utilities.Exceptions;
using Utilities.Messaging.Interfaces;

namespace Business.Services.Business
{
    public class ContractService
        : BusinessGeneric<ContractSelectDto, ContractCreateDto, ContractUpdateDto, Contract>, IContractService
    {
        private readonly IContractRepository _contractRepository;
        private readonly IPersonService _personService;
        private readonly IEstablishmentService _establishmentService;
        private readonly IUserService _userService;
        private readonly ISendCode _emailService;
        private readonly ApplicationDbContext _context;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUser _user;
        private readonly IObligationMonthService _obligationMonthService;
        private readonly IUserContextService _userContextService;
        private readonly IContractPdfGeneratorService _contractPdfService;
        private readonly ILogger<ContractService> _logger;
        private readonly IMapper _mapper;

        public ContractService(
            IContractRepository contractRepository,
            IPersonService personService,
            IEstablishmentService establishmentService,
            IUserService userService,
            IMapper mapper,
            ISendCode emailService,
            ApplicationDbContext context,
            ICurrentUser user,
            IObligationMonthService obligationMonthService,
            IUserContextService userContextService,
            IContractPdfGeneratorService contractPdfService,
            IUnitOfWork uow,
            ILogger<ContractService> logger
        ) : base(contractRepository, mapper)
        {
            _contractRepository = contractRepository;
            _personService = personService;
            _establishmentService = establishmentService;
            _userService = userService;
            _emailService = emailService;
            _context = context;
            _user = user;
            _obligationMonthService = obligationMonthService;
            _userContextService = userContextService;
            _contractPdfService = contractPdfService;
            _uow = uow;
            _logger = logger;
            _mapper = mapper;
        }

        /// <summary>
        /// Crea un contrato, gestionando la creación o recuperación de la persona y usuario asociados.
        /// </summary>
        public async Task<int> CreateContractWithPersonHandlingAsync(ContractCreateDto dto)
        {
            ValidatePayload(dto);

            var personPayload = _mapper.Map<PersonDto>(dto);
            var person = await _personService.GetOrCreateByDocumentAsync(personPayload);

            var (baseRent, uvtQty) = await _establishmentService.ReserveForContractAsync(dto.EstablishmentIds);

            var userResult = await _userService.EnsureUserForPersonAsync(person.Id, dto.Email);

            var contract = BuildContract(dto, person.Id, baseRent, uvtQty);
            await _contractRepository.AddAsync(contract);

            var snapshot = await BuildSnapshotAsync(contract);
            var fullName = ComposeFullName(person);

            await SchedulePostCommitAsync(contract.Id, userResult, dto.Email, fullName, snapshot);

            return contract.Id;
        }

        /// <summary>
        /// Obtiene los contratos asociados al usuario autenticado.
        /// </summary>
        public async Task<IReadOnlyList<ContractCardDto>> GetMineAsync()
        {
            if (_user.EsAdministrador)
            {
                return (await _contractRepository.GetCardsAllAsync())
                    .ToList()
                    .AsReadOnly();
            }

            if (!_user.PersonId.HasValue || _user.PersonId.Value <= 0)
                throw new BusinessException("El usuario autenticado no tiene persona asociada. Complete el perfil antes de consultar contratos.");

            var contracts = await _contractRepository.GetCardsByPersonAsync(_user.PersonId.Value);
            return contracts.ToList().AsReadOnly();
        }

        /// <summary>
        /// Obtiene las obligaciones mensuales de un contrato.
        /// </summary>
        public async Task<IReadOnlyList<ObligationMonthSelectDto>> GetObligationsAsync(int contractId)
        {
            if (contractId <= 0)
                throw new BusinessException("ContractId inválido.");

            return await _obligationMonthService.GetByContractAsync(contractId);
        }

        /// <summary>
        /// Ejecuta el barrido de contratos expirados y libera los establecimientos asociados.
        /// </summary>
        public async Task<ExpirationSweepResult> RunExpirationSweepAsync(CancellationToken ct = default)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync(ct);
                try
                {
                    var deactivated = await _contractRepository.DeactivateExpiredAsync(DateTime.UtcNow);
                    var released = await _contractRepository.ReleaseEstablishmentsForExpiredAsync(DateTime.UtcNow);

                    await tx.CommitAsync(ct);

                    _logger.LogInformation("Barrido de expiración: {Count} contratos desactivados, {Estabs} establecimientos liberados.",
                        deactivated.Count, released);

                    return new ExpirationSweepResult(deactivated, released);
                }
                catch
                {
                    await tx.RollbackAsync(ct);
                    throw;
                }
            });
        }

        /// <summary>
        /// Valida la carga útil de creación de contrato.
        /// </summary>
        private void ValidatePayload(ContractCreateDto dto)
        {
            if (dto == null)
                throw new BusinessException("Payload inválido.");

            if (dto.EstablishmentIds is null || dto.EstablishmentIds.Count == 0)
                throw new BusinessException("Debe seleccionar al menos un establecimiento.");
        }

        /// <summary>
        /// Construye la entidad de contrato a partir del DTO y datos complementarios.
        /// </summary>
        private Contract BuildContract(ContractCreateDto dto, int personId, decimal totalBaseRent, decimal totalUvt)
        {
            var contract = _mapper.Map<Contract>(dto);
            contract.PersonId = personId;
            contract.TotalBaseRentAgreed = totalBaseRent;
            contract.TotalUvtQtyAgreed = totalUvt;

            contract.PremisesLeased = dto.EstablishmentIds
                .Select(eid => new PremisesLeased { EstablishmentId = eid })
                .ToList();

            if (dto.ClauseIds is { Count: > 0 })
            {
                contract.ContractClauses = dto.ClauseIds
                    .Distinct()
                    .Select(cid => new ContractClause { ClauseId = cid })
                    .ToList();
            }

            return contract;
        }

        /// <summary>
        /// Construye un snapshot del contrato recién creado.
        /// </summary>
        private async Task<ContractSelectDto?> BuildSnapshotAsync(Contract contract)
        {
            var loaded = await _contractRepository.GetByIdAsync(contract.Id);
            return _mapper.Map<ContractSelectDto>(loaded ?? contract);
        }

        /// <summary>
        /// Compone el nombre completo de la persona asociada.
        /// </summary>
        private string ComposeFullName(PersonSelectDto person)
            => $"{person.FirstName} {person.LastName}".Trim();

        /// <summary>
        /// Programa tareas post-commit, como envío de correos y generación de obligaciones.
        /// </summary>
        private async Task SchedulePostCommitAsync(
            int contractId,
            (int userId, bool created, string? tempPassword) userResult,
            string? email,
            string fullName,
            ContractSelectDto? contractSnapshot)
        {
            if (userResult.created && !string.IsNullOrWhiteSpace(userResult.tempPassword) && !string.IsNullOrWhiteSpace(email))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("Enviando contraseña temporal a {Email} para {Name}", email, fullName);
                        await _emailService.SendTemporaryPasswordAsync(email!, fullName, userResult.tempPassword!);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error enviando contraseña temporal a {Email}", email);
                    }

                    try
                    {
                        _userContextService.InvalidateCache(userResult.userId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error invalidando cache para usuario {UserId}", userResult.userId);
                    }
                });
            }

            _uow.RegisterPostCommit(async ct =>
            {
                try
                {
                    var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
                        TimeZoneConverter.TZConvert.GetTimeZoneInfo("America/Bogota"));

                    await _obligationMonthService.GenerateForContractMonthAsync(contractId, now.Year, now.Month);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generando obligaciones para contrato {ContractId}", contractId);
                }
            });

            if (!string.IsNullOrWhiteSpace(email) && contractSnapshot != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var pdf = await _contractPdfService.GeneratePdfAsync(contractSnapshot);
                        await _emailService.SendContractWithPdfAsync(email!, fullName, contractId.ToString("D6"), pdf);

                        _logger.LogInformation("Correo de contrato enviado a {Email} con PDF {ContractId}", email, contractId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error enviando correo/PDF del contrato {ContractId} a {Email}. El contrato sigue creado.",
                            contractId, email);
                    }
                });
            }
        }

        protected override Expression<Func<Contract, string>>[] SearchableFields() =>
            new Expression<Func<Contract, string>>[]
            {
                c => c.Person.FirstName,
                c => c.Person.LastName,
                c => c.Person.Document
            };

        protected override string[] SortableFields() => new[]
        {
            nameof(Contract.StartDate),
            nameof(Contract.EndDate),
            nameof(Contract.TotalBaseRentAgreed),
            nameof(Contract.TotalUvtQtyAgreed),
            nameof(Contract.PersonId),
            nameof(Contract.Id),
            nameof(Contract.CreatedAt),
            nameof(Contract.Active)
        };

        protected override IDictionary<string, Func<string, Expression<Func<Contract, bool>>>> AllowedFilters() =>
            new Dictionary<string, Func<string, Expression<Func<Contract, bool>>>>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(Contract.PersonId)] = value => entity => entity.PersonId == int.Parse(value),
                [nameof(Contract.Active)] = value => entity => entity.Active == bool.Parse(value),
                [nameof(Contract.StartDate)] = value => entity => entity.StartDate == DateTime.Parse(value),
                [nameof(Contract.EndDate)] = value => entity => entity.EndDate == DateTime.Parse(value)
            };
    }
}
