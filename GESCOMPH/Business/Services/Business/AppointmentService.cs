using Business.Interfaces.Implements.Business;
using Business.Interfaces.Implements.Persons;
using Business.Interfaces.Implements.SecurityAuthentication;
using Business.Repository;
using Data.Interfaz.IDataImplement.Business;
using Entity.Domain.Models.Implements.Business;
using Entity.DTOs.Implements.Business.Appointment;
using Entity.DTOs.Implements.Persons.Person; 
using Entity.Infrastructure.Context;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;
using Utilities.Helpers.Business;
using Utilities.Messaging.Interfaces;

namespace Business.Services.Business
{
    public class AppointmentService
        : BusinessGeneric<AppointmentSelectDto, AppointmentCreateDto, AppointmentUpdateDto, Appointment>,
          IAppointmentService
    {
        private readonly IAppointmentRepository _data;
        private readonly IMapper _mapper;
        private readonly IPersonService _personService;
        private readonly IUserService _userService;
        private readonly ISendCode _emailService;
        private readonly ApplicationDbContext _context;   
        private readonly ILogger<AppointmentService> _logger;

        public AppointmentService(
            IAppointmentRepository data,
            IMapper mapper,
            IPersonService personService,
            IUserService userService,
            ISendCode emailService,
            ApplicationDbContext context,
            ILogger<AppointmentService> logger
        ) : base(data, mapper)
        {
            _data = data;
            _mapper = mapper;
            _personService = personService;
            _userService = userService;
            _emailService = emailService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Crea una cita nueva, gestionando automáticamente la persona y el usuario asociado.
        /// También envía las credenciales por correo si se crea un usuario nuevo.
        /// </summary>
        /// <param name="dto">Datos de creación de la cita.</param>
        /// <returns>Objeto <see cref="AppointmentSelectDto"/> con la cita creada.</returns>
        public override async Task<AppointmentSelectDto> CreateAsync(AppointmentCreateDto dto)
        {
            BusinessValidationHelper.ThrowIfNull(dto, "El DTO no puede ser nulo.");

            var strategy = _context.Database.CreateExecutionStrategy();
            Appointment? createdAppointment = null;
            string? tempPassword = null;

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();

                var personDto = _mapper.Map<PersonDto>(dto);
                var person = await _personService.GetOrCreateByDocumentAsync(personDto);

                var (userId, created, password) = await _userService.EnsureUserForPersonAsync(person.Id, dto.Email);
                tempPassword = password;

                var appointment = _mapper.Map<Appointment>(dto);
                appointment.PersonId = person.Id;
                appointment.Active = true;

                createdAppointment = await _data.AddAsync(appointment);

                await tx.CommitAsync();
            });

            if (!string.IsNullOrWhiteSpace(dto.Email) && !string.IsNullOrWhiteSpace(tempPassword))
            {
                try
                {
                    await _emailService.SendTemporaryPasswordAsync(dto.Email, dto.FirstName, tempPassword!);
                    _logger.LogInformation(
                        "Correo enviado correctamente a {Email} para la cita {AppointmentId}",
                        dto.Email, createdAppointment!.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error al enviar correo de credenciales a {Email} para la cita {AppointmentId}. " +
                        "El proceso de negocio no se interrumpió.",
                        dto.Email, createdAppointment?.Id);
                }
            }

            return _mapper.Map<AppointmentSelectDto>(createdAppointment!);
        }

        /// <summary>
        /// Define los campos de la entidad que son buscables mediante operaciones de texto.
        /// </summary>
        /// <returns>Expresiones que representan los campos buscables.</returns>
        protected override Expression<Func<Appointment, string>>[] SearchableFields() =>
        [
            a => a.Description!,
            a => a.Person.FirstName!,
            a => a.Person.LastName!,
            a => a.Person.Phone!,
            a => a.Establishment.Name!
        ];

        /// <summary>
        /// Define los campos que se pueden utilizar para ordenar los resultados de las consultas.
        /// </summary>
        /// <returns>Arreglo de nombres de campos ordenables.</returns>
        protected override string[] SortableFields() => new[]
        {
            nameof(Appointment.Description),
            nameof(Appointment.RequestDate),
            nameof(Appointment.DateTimeAssigned),
            nameof(Appointment.EstablishmentId),
            nameof(Appointment.PersonId),
            nameof(Appointment.Id),
            nameof(Appointment.CreatedAt),
            nameof(Appointment.Active)
        };

        /// <summary>
        /// Define los filtros permitidos para la búsqueda y consulta de citas.
        /// </summary>
        /// <returns>Diccionario de filtros válidos y sus expresiones asociadas.</returns>
        protected override IDictionary<string, Func<string, Expression<Func<Appointment, bool>>>> AllowedFilters() =>
            new Dictionary<string, Func<string, Expression<Func<Appointment, bool>>>>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(Appointment.EstablishmentId)] = v => e => e.EstablishmentId == int.Parse(v),
                [nameof(Appointment.PersonId)] = v => e => e.PersonId == int.Parse(v),
                [nameof(Appointment.Active)] = v => e => e.Active == bool.Parse(v),
                [nameof(Appointment.RequestDate)] = v => e => e.RequestDate == DateTime.Parse(v)
            };
    }
}
