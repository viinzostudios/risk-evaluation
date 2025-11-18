using Transational.Api.Domain.Entities;

namespace Transational.Api.Domain.Interfaces;

/// <summary>
/// Repository interface for PaymentOperation entity
/// </summary>
public interface IPaymentRepository : IRepository<PaymentOperation>
{
    Task<PaymentOperation?> GetByExternalIdAsync(Guid externalOperationId, CancellationToken cancellationToken = default);
    Task<List<PaymentOperation>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<decimal> GetDailyTotalByCustomerAsync(Guid customerId, DateTime date, CancellationToken cancellationToken = default);
}
