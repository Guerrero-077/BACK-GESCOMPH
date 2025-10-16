using Business.Services.Business;
using Data.Interfaz.IDataImplement.Business;
using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.AdministrationSystem;
using MapsterMapper;
using Moq;

namespace Test.Modulo.Business;

public class ObligationMonthServiceTests
{
    private readonly Mock<IObligationMonthRepository> _obligationRepo = new();
    private readonly Mock<IContractRepository> _contractRepo = new();
    private readonly Mock<IDataGeneric<SystemParameter>> _systemParamRepo = new();
    private readonly Mock<IMapper> _mapper = new();

    private readonly ObligationMonthService _service;

    public ObligationMonthServiceTests()
    {
        _service = new ObligationMonthService(
            _obligationRepo.Object,
            _contractRepo.Object,
            _systemParamRepo.Object,
            _mapper.Object
        );
    }

    [Fact]
    public async Task GetTotalObligationsPaidByDayAsync_ReturnsExpected()
    {
        // Arrange
        var date = new DateTime(2024, 10, 1);
        _obligationRepo.Setup(r => r.GetTotalObligationsPaidByDayAsync(date))
                       .ReturnsAsync(1500m);

        // Act
        var result = await _service.GetTotalObligationsPaidByDayAsync(date);

        // Assert
        Assert.Equal(1500m, result);
        _obligationRepo.Verify(r => r.GetTotalObligationsPaidByDayAsync(date), Times.Once);
    }

    [Fact]
    public async Task GetTotalObligationsPaidByMonthAsync_ReturnsExpected()
    {
        // Arrange
        int year = 2024, month = 10;
        _obligationRepo.Setup(r => r.GetTotalObligationsPaidByMonthAsync(year, month))
                       .ReturnsAsync(5000m);

        // Act
        var result = await _service.GetTotalObligationsPaidByMonthAsync(year, month);

        // Assert
        Assert.Equal(5000m, result);
        _obligationRepo.Verify(r => r.GetTotalObligationsPaidByMonthAsync(year, month), Times.Once);
    }
}
