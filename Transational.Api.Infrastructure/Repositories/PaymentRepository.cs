using Microsoft.EntityFrameworkCore;
using Transational.Api.Domain.Entities;
using Transational.Api.Domain.Interfaces;
using Transational.Api.Infrastructure.Data;

namespace Transational.Api.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly AppDbContext _context;

    public PaymentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByExternalIdAsync(Guid externalOperationId, CancellationToken cancellationToken = default)
    {
        var bytes = externalOperationId.ToByteArray();

        return await _context.Payments
            .Include(p => p.PaymentStatus)
            .Include(p => p.PaymentMethod)
            .FirstOrDefaultAsync(p => p.ExternalOperationId == bytes, cancellationToken);
    }

    public async Task<List<Payment>> GetByCustomerIdAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        var customerIdBytes = customerId.ToByteArray();

        return await _context.Payments
            .Include(p => p.PaymentStatus)
            .Include(p => p.PaymentMethod)
            .Where(p => p.ExternalOperationId == customerIdBytes)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetDailyTotalByCustomerAsync(Guid customerId, DateTime date, CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1).AddTicks(-1);
        var customerIdBytes = customerId.ToByteArray();


        return await _context.Payments
            .Where(p => p.ExternalOperationId == customerIdBytes)
            .Where(p => p.CreatedAt >= startOfDay && p.CreatedAt <= endOfDay)
            .Where(p => p.PaymentStatusId == 2) // accepted
            .SumAsync(p => p.Amount, cancellationToken);
    }

    public async Task<Payment> AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public async Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Payment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .Include(p => p.PaymentStatus)
            .Include(p => p.PaymentMethod)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }
}
