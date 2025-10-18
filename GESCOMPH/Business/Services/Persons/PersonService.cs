using Business.Interfaces.Implements.Persons;
using Business.Repository;
using Data.Interfaz.IDataImplement.Persons;
using Entity.Domain.Models.Implements.Persons;
using Entity.DTOs.Implements.Persons.Person;
using MapsterMapper;
using Utilities.Exceptions;
using System.Linq.Expressions;

namespace Business.Services.Persons
{
    /// <summary>
    /// Servicio encargado de gestionar las operaciones de negocio
    /// relacionadas con las personas (creación, actualización, búsqueda, etc.).
    /// </summary>
    public class PersonService : BusinessGeneric<PersonSelectDto, PersonDto, PersonUpdateDto, Person>, IPersonService
    {
        private readonly IPersonRepository _personRepository;

        /// <summary>
        /// Inicializa una nueva instancia del servicio de personas.
        /// </summary>
        /// <param name="repository">Repositorio específico de persona.</param>
        /// <param name="mapper">Instancia de <see cref="IMapper"/> utilizada para mapear entidades y DTOs.</param>
        public PersonService(IPersonRepository repository, IMapper mapper)
            : base(repository, mapper)
        {
            _personRepository = repository;
        }

        /// <summary>
        /// Crea una nueva persona validando duplicados por documento.
        /// </summary>
        /// <param name="dto">Datos de creación de persona.</param>
        /// <returns>La persona creada en formato <see cref="PersonSelectDto"/>.</returns>
        /// <exception cref="BusinessException">Si el payload es nulo o ya existe una persona con el mismo documento.</exception>
        public override async Task<PersonSelectDto> CreateAsync(PersonDto dto)
        {
            if (dto == null)
                throw new BusinessException("Payload inválido.");

            // Validar documento duplicado
            if (await _personRepository.ExistsByDocumentAsync(dto.Document))
                throw new BusinessException("Ya existe una persona registrada con ese número de documento.");

            var entity = _mapper.Map<Person>(dto);

            var created = await Data.AddAsync(entity);

            var reloaded = await _personRepository.GetByIdAsync(created.Id) ?? created;

            return _mapper.Map<PersonSelectDto>(reloaded);
        }

        /// <summary>
        /// Actualiza los datos de una persona existente.
        /// </summary>
        /// <param name="dto">Datos actualizados de la persona.</param>
        /// <returns>La persona actualizada en formato <see cref="PersonSelectDto"/>.</returns>
        /// <exception cref="BusinessException">Si el payload es nulo o la persona no existe.</exception>
        public override async Task<PersonSelectDto> UpdateAsync(PersonUpdateDto dto)
        {
            if (dto == null)
                throw new BusinessException("Payload inválido.");

            var existing = await Data.GetByIdAsync(dto.Id)
                ?? throw new BusinessException("Persona no encontrada.");

            _mapper.Map(dto, existing);

            await Data.UpdateAsync(existing);

            var reloaded = await _personRepository.GetByIdAsync(existing.Id) ?? existing;
            return _mapper.Map<PersonSelectDto>(reloaded);
        }

        /// <summary>
        /// Obtiene una persona a partir de su número de documento.
        /// </summary>
        /// <param name="document">Número de documento a buscar.</param>
        /// <returns>Una persona en formato <see cref="PersonSelectDto"/> o null si no existe.</returns>
        public async Task<PersonSelectDto?> GetByDocumentAsync(string document)
        {
            var person = await _personRepository.GetByDocumentAsync(document);
            return person is null ? null : _mapper.Map<PersonSelectDto>(person);
        }

        /// <summary>
        /// Busca una persona por documento y, si no existe, la crea automáticamente.
        /// </summary>
        /// <param name="dto">Datos de la persona a buscar o crear.</param>
        /// <returns>La persona encontrada o creada.</returns>
        public async Task<PersonSelectDto> GetOrCreateByDocumentAsync(PersonDto dto)
        {
            if (dto == null)
                throw new BusinessException("Payload inválido.");

            var existing = await _personRepository.GetByDocumentAsync(dto.Document);
            if (existing is not null)
                return _mapper.Map<PersonSelectDto>(existing);

            return await CreateAsync(dto);
        }

        /// <summary>
        /// Define los campos sobre los que se puede realizar búsqueda de texto.
        /// </summary>
        protected override Expression<Func<Person, string>>[] SearchableFields() =>
        [
            p => p.FirstName,
            p => p.LastName,
            p => p.Document,
            p => p.Address,
            p => p.Phone,
            p => p.User.Email
        ];

        /// <summary>
        /// Define los campos permitidos para ordenamiento dinámico.
        /// </summary>
        protected override string[] SortableFields() => new[]
        {
            nameof(Person.FirstName),
            nameof(Person.LastName),
            nameof(Person.Document),
            nameof(Person.CityId),
            nameof(Person.Id),
            nameof(Person.CreatedAt),
            nameof(Person.Active)
        };

        /// <summary>
        /// Define los filtros disponibles mediante parámetros de query.
        /// </summary>
        protected override IDictionary<string, Func<string, Expression<Func<Person, bool>>>> AllowedFilters() =>
            new Dictionary<string, Func<string, Expression<Func<Person, bool>>>>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(Person.Document)] = value => p => p.Document == value,
                [nameof(Person.CityId)] = value => p => p.CityId == int.Parse(value),
                [nameof(Person.Active)] = value => p => p.Active == bool.Parse(value),
                [nameof(Person.FirstName)] = value => p => p.FirstName == value,
                [nameof(Person.LastName)] = value => p => p.LastName == value
            };
    }
}
