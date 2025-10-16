using Business.Services.Business;
using Data.Interfaz.IDataImplement.Business;
using Entity.Domain.Models.Implements.Business;
using Entity.DTOs.Implements.Business.Appointment;
using Entity.DTOs.Implements.Persons.Person;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities.Exceptions;
using Utilities.Messaging.Interfaces;
using Business.Interfaces.Implements.SecurityAuthentication;
using Business.Interfaces.Implements.Persons;
using Entity.Infrastructure.Context;

namespace Test.Modulo.Business;

public class AppointmentServiceTests
{
    private readonly Mock<IAppointmentRepository> _repo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IPersonService> _personService = new();
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<ISendCode> _emailService = new();
    private readonly Mock<ILogger<AppointmentService>> _logger = new();

    private readonly AppointmentService _service;

    public AppointmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);

        _service = new AppointmentService(
            _repo.Object,
            _mapper.Object,
            _personService.Object,
            _userService.Object,
            _emailService.Object,
            context,
            _logger.Object
        );
    }

    [Fact]
    public async Task Create_Throws_WhenDtoNull()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(null!));
        Assert.Contains("no puede ser nulo", ex.Message);
    }

    [Fact]
    public async Task Create_WithValidData_CreatesAppointment()
    {
        var dto = new AppointmentCreateDto
        {
            FirstName = "A",
            LastName = "B",
            Document = "1",
            Address = "X",
            Phone = "Y",
            Description = "d",
            RequestDate = DateTime.UtcNow,
            DateTimeAssigned = DateTime.UtcNow,
            EstablishmentId = 1,
            CityId = 1,
            Email = "a@b.com"
        };

        // Simular que el servicio de personas devuelve un objeto con Id
        _personService.Setup(s => s.GetOrCreateByDocumentAsync(It.IsAny<PersonDto>()))
            .ReturnsAsync(new PersonSelectDto { Id = 10, FirstName = "A", LastName = "B" });

        // Simular creación/obtención de usuario
        _userService.Setup(u => u.EnsureUserForPersonAsync(10, "a@b.com"))
            .ReturnsAsync((1, true, "temp123"));

        // Simular persistencia de la cita
        _repo.Setup(r => r.AddAsync(It.IsAny<Appointment>()))
            .ReturnsAsync((Appointment a) =>
            {
                a.Id = 7;
                return a;
            });

        _mapper.Setup(m => m.Map<Appointment>(dto))
            .Returns(new Appointment { PersonId = 10 });

        _mapper.Setup(m => m.Map<AppointmentSelectDto>(It.IsAny<Appointment>()))
            .Returns<Appointment>(a => new AppointmentSelectDto { Id = a.Id });

        var result = await _service.CreateAsync(dto);

        Assert.Equal(7, result.Id);
    }
}
