using Transational.Api.Domain.Entities;

namespace Transational.Api.Domain.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByExternalIdAsync(Guid externalOperationId, CancellationToken cancellationToken = default);
    Task<List<Payment>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<decimal> GetDailyTotalByCustomerAsync(Guid customerId, DateTime date, CancellationToken cancellationToken = default);
    Task<Payment> AddAsync(Payment payment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default);
    Task<Payment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
