using Data.Interfaz.IDataImplement.Business;
using Data.Repository;
using Entity.Domain.Models.Implements.Business;
using Entity.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Data.Services.Business
{
    public class ObligationMonthRepository : DataGeneric<ObligationMonth>, IObligationMonthRepository
    {
        public ObligationMonthRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<ObligationMonth?> GetByContractYearMonthAsync(int contractId, int year, int month)
        {
            return await _dbSet.AsNoTracking()
                .FirstOrDefaultAsync(o => o.ContractId == contractId && o.Year == year && o.Month == month && !o.IsDeleted);
        }

        public IQueryable<ObligationMonth> GetByContractQueryable(int contractId)
        {
            return _dbSet.AsNoTracking()
                         .Where(o => o.ContractId == contractId && !o.IsDeleted)
                         .OrderByDescending(o => o.Year)
                         .ThenByDescending(o => o.Month);
        }

        public async Task<decimal> GetTotalObligationsPaidByDayAsync(DateTime date)
        {
            return await _dbSet.AsNoTracking()
                              .Where(o => o.Status == "PAID"
                                  && o.PaymentDate.HasValue
                                  && o.PaymentDate.Value.Date == date.Date)
                              .SumAsync(o => o.TotalAmount);
        }

        public async Task<decimal> GetTotalObligationsPaidByMonthAsync(int year, int month)
        {
            var start = new DateTime(year, month, 1);
            var end = start.AddMonths(1);

            return await _dbSet.AsNoTracking()
                .Where(o => o.Status == "PAID"
                    && o.PaymentDate >= start
                    && o.PaymentDate < end)
                .SumAsync(o => o.TotalAmount);
        }
    }
}
