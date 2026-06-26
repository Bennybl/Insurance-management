using System.Linq.Expressions;
using InsuranceManagement.Api.Application.Common;
using InsuranceManagement.Api.Domain;
using InsuranceManagement.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InsuranceManagement.Api.Application.Policies;

public class PolicyService(AppDbContext dbContext) : IPolicyService
{
    public async Task<PolicyResponse> IssueAsync(
        IssuePolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        var productCode = NormalizeProductCode(request.ProductCode);

        var issueData = await dbContext.Customers
            .AsNoTracking()
            .Where(customer => customer.Id == request.CustomerId)
            .Select(customer => new
            {
                CustomerId = customer.Id,
                CustomerIsActive = customer.IsActive,
                Product = dbContext.PolicyProducts
                    .Where(product => product.Code == productCode)
                    .Select(product => new
                    {
                        product.Code,
                        product.IsActive
                    })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (issueData is null)
        {
            throw new NotFoundException("Customer was not found.");
        }

        if (!issueData.CustomerIsActive)
        {
            throw new ConflictException("Cannot issue a policy to an inactive customer.");
        }

        if (issueData.Product is null)
        {
            throw new NotFoundException("Policy product was not found.");
        }

        if (!issueData.Product.IsActive)
        {
            throw new ConflictException("Cannot issue a policy for an inactive product.");
        }

        var policy = new Policy
        {
            Id = Guid.NewGuid(),
            CustomerId = issueData.CustomerId,
            PolicyNumber = GeneratePolicyNumber(),
            ProductCode = issueData.Product.Code,
            Status = PolicyStatus.Active,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PremiumAmount = request.PremiumAmount,
            DetailsJson = request.DetailsJson,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Policies.Add(policy);
        await dbContext.SaveChangesAsync(cancellationToken);

        return MapPolicy(policy);
    }

    public async Task<IReadOnlyList<PolicyResponse>> GetAllAsync(
        PolicyFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Policies.AsNoTracking();

        if (filter.CustomerId.HasValue)
        {
            query = query.Where(policy => policy.CustomerId == filter.CustomerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.ProductCode))
        {
            var productCode = NormalizeProductCode(filter.ProductCode);
            query = query.Where(policy => policy.ProductCode == productCode);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(policy => policy.Status == filter.Status.Value);
        }

        var policies = await query
            .OrderByDescending(policy => policy.CreatedAt)
            .ToListAsync(cancellationToken);

        return policies.Select(MapPolicy).ToList();
    }

    public async Task<PolicyResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var policy = await GetPolicyOrThrowAsync(
            policy => policy.Id == id,
            cancellationToken);

        return MapPolicy(policy);
    }

    public async Task<PolicyResponse> UpdateAsync(
        Guid id,
        UpdatePolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        var policy = await GetPolicyOrThrowAsync(
            policy => policy.Id == id,
            cancellationToken,
            asNoTracking: false);

        if (policy.Status == PolicyStatus.Cancelled)
        {
            throw new ConflictException("Cannot update a cancelled policy.");
        }

        policy.StartDate = request.StartDate;
        policy.EndDate = request.EndDate;
        policy.PremiumAmount = request.PremiumAmount;
        policy.DetailsJson = request.DetailsJson;
        policy.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return MapPolicy(policy);
    }

    public async Task<PolicyResponse> CancelAsync(
        Guid id,
        CancelPolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        var cancellationReason = request.Reason.Trim();
        if (string.IsNullOrWhiteSpace(cancellationReason))
        {
            throw new ConflictException("Cancellation reason is required.");
        }

        var policy = await GetPolicyOrThrowAsync(
            policy => policy.Id == id,
            cancellationToken,
            asNoTracking: false);

        if (policy.Status == PolicyStatus.Cancelled)
        {
            throw new ConflictException("Policy is already cancelled.");
        }

        policy.Status = PolicyStatus.Cancelled;
        policy.CancelledAt = DateTimeOffset.UtcNow;
        policy.CancellationReason = cancellationReason;
        policy.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return MapPolicy(policy);
    }

    private async Task<Policy> GetPolicyOrThrowAsync(
        Expression<Func<Policy, bool>> predicate,
        CancellationToken cancellationToken,
        bool asNoTracking = true)
    {
        var query = dbContext.Policies.AsQueryable();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        var policy = await query.FirstOrDefaultAsync(predicate, cancellationToken);

        if (policy is null)
        {
            throw new NotFoundException("Policy was not found.");
        }

        return policy;
    }

    private static string NormalizeProductCode(string productCode)
    {
        return productCode.Trim().ToUpperInvariant();
    }

    private static string GeneratePolicyNumber()
    {
        var suffix = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();

        return $"POL-{DateTimeOffset.UtcNow:yyyyMMdd}-{suffix}";
    }

    private static PolicyResponse MapPolicy(Policy policy)
    {
        return new PolicyResponse
        {
            Id = policy.Id,
            CustomerId = policy.CustomerId,
            PolicyNumber = policy.PolicyNumber,
            ProductCode = policy.ProductCode,
            Status = policy.Status,
            StartDate = policy.StartDate,
            EndDate = policy.EndDate,
            PremiumAmount = policy.PremiumAmount,
            DetailsJson = policy.DetailsJson,
            CreatedAt = policy.CreatedAt,
            UpdatedAt = policy.UpdatedAt,
            CancelledAt = policy.CancelledAt,
            CancellationReason = policy.CancellationReason
        };
    }
}
