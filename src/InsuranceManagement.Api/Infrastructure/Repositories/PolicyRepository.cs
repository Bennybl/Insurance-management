using InsuranceManagement.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace InsuranceManagement.Api.Infrastructure.Repositories;

public class PolicyRepository(AppDbContext dbContext) : IPolicyRepository
{
    public async Task<IReadOnlyList<Policy>> GetAllAsync(
        Guid? customerId = null,
        string? productCode = null,
        PolicyStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Policies.AsNoTracking();

        if (customerId.HasValue)
        {
            query = query.Where(policy => policy.CustomerId == customerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(productCode))
        {
            var normalizedProductCode = NormalizeProductCode(productCode);
            query = query.Where(policy => policy.ProductCode == normalizedProductCode);
        }

        if (status.HasValue)
        {
            query = query.Where(policy => policy.Status == status.Value);
        }

        return await query
            .OrderByDescending(policy => policy.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Policy?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Policies.AsQueryable();

        if (!track)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(policy => policy.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Policy>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Policies
            .AsNoTracking()
            .Where(policy => policy.CustomerId == customerId)
            .OrderByDescending(policy => policy.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Policy>> GetActiveByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Policies
            .Where(policy => policy.CustomerId == customerId && policy.Status == PolicyStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public async Task<PolicyProduct?> GetProductAsync(string productCode, CancellationToken cancellationToken = default)
    {
        var normalizedProductCode = NormalizeProductCode(productCode);

        return await dbContext.PolicyProducts
            .AsNoTracking()
            .FirstOrDefaultAsync(product => product.Code == normalizedProductCode, cancellationToken);
    }

    public void Add(Policy policy)
    {
        dbContext.Policies.Add(policy);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeProductCode(string productCode)
    {
        return productCode.Trim().ToUpperInvariant();
    }
}
