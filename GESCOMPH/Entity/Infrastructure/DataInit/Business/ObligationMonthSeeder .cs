using Entity.Domain.Models.Implements.Business;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entity.Infrastructure.DataInit.Business
{
    public sealed class ObligationMonthSeeder : IEntityTypeConfiguration<ObligationMonth>
    {
        public void Configure(EntityTypeBuilder<ObligationMonth> builder)
        {
            var seedAt = new DateTime(2025, 01, 01, 0, 0, 0, DateTimeKind.Utc);

            // Obligaciones pagadas para distintos meses (sin incluir el mes vigente)
            builder.HasData(
                // Contrato 1
                new ObligationMonth
                {
                    Id = 1,
                    ContractId = 1,
                    Year = 2025,
                    Month = 4, // abril
                    DueDate = new DateTime(2025, 04, 30, 0, 0, 0, DateTimeKind.Utc),
                    PaymentDate = new DateTime(2025, 04, 28, 0, 0, 0, DateTimeKind.Utc),
                    UvtQtyApplied = 38m,
                    UvtValueApplied = 47_065m,
                    VatRateApplied = 0.19m,
                    BaseAmount = 5_700_000m,
                    VatAmount = 1_083_000m,
                    TotalAmount = 6_783_000m,
                    DaysLate = 0,
                    LateAmount = 0m,
                    Status = "PAID",
                    Locked = true,
                    CreatedAt = seedAt
                },
                new ObligationMonth
                {
                    Id = 2,
                    ContractId = 1,
                    Year = 2025,
                    Month = 5, // mayo
                    DueDate = new DateTime(2025, 05, 31, 0, 0, 0, DateTimeKind.Utc),
                    PaymentDate = new DateTime(2025, 05, 29, 0, 0, 0, DateTimeKind.Utc),
                    UvtQtyApplied = 38m,
                    UvtValueApplied = 47_065m,
                    VatRateApplied = 0.19m,
                    BaseAmount = 5_700_000m,
                    VatAmount = 1_083_000m,
                    TotalAmount = 6_783_000m,
                    DaysLate = 0,
                    LateAmount = 0m,
                    Status = "PAID",
                    Locked = true,
                    CreatedAt = seedAt
                },
                new ObligationMonth
                {
                    Id = 3,
                    ContractId = 1,
                    Year = 2025,
                    Month = 6, // junio
                    DueDate = new DateTime(2025, 06, 30, 0, 0, 0, DateTimeKind.Utc),
                    PaymentDate = new DateTime(2025, 06, 29, 0, 0, 0, DateTimeKind.Utc),
                    UvtQtyApplied = 38m,
                    UvtValueApplied = 47_065m,
                    VatRateApplied = 0.19m,
                    BaseAmount = 5_700_000m,
                    VatAmount = 1_083_000m,
                    TotalAmount = 6_783_000m,
                    DaysLate = 0,
                    LateAmount = 0m,
                    Status = "PAID",
                    Locked = true,
                    CreatedAt = seedAt
                },

                // Contrato 2 (activo desde julio 2025)
                new ObligationMonth
                {
                    Id = 4,
                    ContractId = 2,
                    Year = 2025,
                    Month = 7,
                    DueDate = new DateTime(2025, 07, 31, 0, 0, 0, DateTimeKind.Utc),
                    PaymentDate = new DateTime(2025, 07, 29, 0, 0, 0, DateTimeKind.Utc),
                    UvtQtyApplied = 48m,
                    UvtValueApplied = 47_065m,
                    VatRateApplied = 0.19m,
                    BaseAmount = 7_100_000m,
                    VatAmount = 1_349_000m,
                    TotalAmount = 8_449_000m,
                    DaysLate = 0,
                    LateAmount = 0m,
                    Status = "PAID",
                    Locked = true,
                    CreatedAt = seedAt
                },
                new ObligationMonth
                {
                    Id = 5,
                    ContractId = 2,
                    Year = 2025,
                    Month = 8,
                    DueDate = new DateTime(2025, 08, 31, 0, 0, 0, DateTimeKind.Utc),
                    PaymentDate = new DateTime(2025, 08, 29, 0, 0, 0, DateTimeKind.Utc),
                    UvtQtyApplied = 48m,
                    UvtValueApplied = 47_065m,
                    VatRateApplied = 0.19m,
                    BaseAmount = 7_100_000m,
                    VatAmount = 1_349_000m,
                    TotalAmount = 8_449_000m,
                    DaysLate = 0,
                    LateAmount = 0m,
                    Status = "PAID",
                    Locked = true,
                    CreatedAt = seedAt
                },
                new ObligationMonth
                {
                    Id = 6,
                    ContractId = 2,
                    Year = 2025,
                    Month = 9,
                    DueDate = new DateTime(2025, 09, 30, 0, 0, 0, DateTimeKind.Utc),
                    PaymentDate = new DateTime(2025, 09, 29, 0, 0, 0, DateTimeKind.Utc),
                    UvtQtyApplied = 48m,
                    UvtValueApplied = 47_065m,
                    VatRateApplied = 0.19m,
                    BaseAmount = 7_100_000m,
                    VatAmount = 1_349_000m,
                    TotalAmount = 8_449_000m,
                    DaysLate = 0,
                    LateAmount = 0m,
                    Status = "PAID",
                    Locked = true,
                    CreatedAt = seedAt
                }
            );
        }
    }
}
