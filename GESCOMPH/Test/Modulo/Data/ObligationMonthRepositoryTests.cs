using Data.Services.Business;
using Entity.Domain.Models.Implements.Business;
using Entity.Infrastructure.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Test.Modulo.Data;

public class ObligationMonthRepositoryTests
{
    private static ApplicationDbContext Ctx()
    {
        var opt = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(opt);
    }

    [Fact]
    public async Task GetByContractYearMonth_ReturnsExpected()
    {
        await using var ctx = Ctx();
        var repo = new ObligationMonthRepository(ctx);
        ctx.ObligationMonths.AddRange(
            new ObligationMonth { Id = 1, ContractId = 5, Year = 2024, Month = 1, DueDate = DateTime.Today, Status = "P" },
            new ObligationMonth { Id = 2, ContractId = 5, Year = 2024, Month = 2, DueDate = DateTime.Today, Status = "P" }
        );
        await ctx.SaveChangesAsync();

        var hit = await repo.GetByContractYearMonthAsync(5, 2024, 2);
        hit.Should().NotBeNull();

        var miss = await repo.GetByContractYearMonthAsync(5, 2023, 12);
        miss.Should().BeNull();
    }

    [Fact]
    public async Task GetByContractQueryable_OrdersDesc_AndFiltersDeleted()
    {
        await using var ctx = Ctx();
        var repo = new ObligationMonthRepository(ctx);
        ctx.ObligationMonths.AddRange(
            new ObligationMonth { Id = 1, ContractId = 7, Year = 2023, Month = 12, DueDate = DateTime.Today, Status = "P" },
            new ObligationMonth { Id = 2, ContractId = 7, Year = 2024, Month = 1, DueDate = DateTime.Today, Status = "P", IsDeleted = true },
            new ObligationMonth { Id = 3, ContractId = 7, Year = 2024, Month = 2, DueDate = DateTime.Today, Status = "P" }
        );
        await ctx.SaveChangesAsync();

        var list = repo.GetByContractQueryable(7).ToList();
        list.Should().HaveCount(2);
        list.First().Month.Should().Be(2);
    }

    [Fact]
    public async Task GetTotalObligationsPaidByDayAsync_ReturnsSumForGivenDate()
    {
        await using var ctx = Ctx();
        var repo = new ObligationMonthRepository(ctx);

        var today = DateTime.Today;
        ctx.ObligationMonths.AddRange(
            new ObligationMonth { Id = 1, Status = "PAID", PaymentDate = today, TotalAmount = 1000 },
            new ObligationMonth { Id = 2, Status = "PAID", PaymentDate = today, TotalAmount = 500 },
            new ObligationMonth { Id = 3, Status = "PENDING", PaymentDate = today, TotalAmount = 999 },
            new ObligationMonth { Id = 4, Status = "PAID", PaymentDate = today.AddDays(-1), TotalAmount = 300 }
        );
        await ctx.SaveChangesAsync();

        var total = await repo.GetTotalObligationsPaidByDayAsync(today);

        total.Should().Be(1500); // Solo los dos primeros
    }

    [Fact]
    public async Task GetTotalObligationsPaidByMonthAsync_ReturnsSumForGivenMonth()
    {
        await using var ctx = Ctx();
        var repo = new ObligationMonthRepository(ctx);

        ctx.ObligationMonths.AddRange(
            new ObligationMonth { Id = 1, Status = "PAID", PaymentDate = new DateTime(2025, 10, 1), TotalAmount = 200 },
            new ObligationMonth { Id = 2, Status = "PAID", PaymentDate = new DateTime(2025, 10, 15), TotalAmount = 800 },
            new ObligationMonth { Id = 3, Status = "PENDING", PaymentDate = new DateTime(2025, 10, 20), TotalAmount = 500 },
            new ObligationMonth { Id = 4, Status = "PAID", PaymentDate = new DateTime(2025, 9, 30), TotalAmount = 999 }
        );
        await ctx.SaveChangesAsync();

        var total = await repo.GetTotalObligationsPaidByMonthAsync(2025, 10);

        total.Should().Be(1000); // Suma de Id 1 y 2
    }
}
