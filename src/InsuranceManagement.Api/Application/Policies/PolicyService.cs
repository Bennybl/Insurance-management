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
            .Select(customer => new PolicyIssueData
            {
                CustomerId = customer.Id,
                CustomerIsActive = customer.IsActive,
                Product = dbContext.PolicyProducts
                    .Where(product => product.Code == productCode)
                    .Select(product => new PolicyProductIssueData
                    {
                        product.Code,
                        product.IsActive
                    })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        var validatedIssueData = ValidateIssueData(issueData);

        var policy = new Policy
        {
            Id = Guid.NewGuid(),
            CustomerId = validatedIssueData.CustomerId,
            PolicyNumber = GeneratePolicyNumber(),
            ProductCode = validatedIssueData.ProductCode,
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
        var policies = await BuildPolicyQuery(filter)
            .OrderByDescending(policy => policy.CreatedAt)
            .ToListAsync(cancellationToken);

        return policies.Select(MapPolicy).ToList();
    }

    public async Task<PolicyResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var policy = await GetPolicyOrThrowAsync(
            BuildPolicyQuery().Where(policy => policy.Id == id),
            cancellationToken);

        return MapPolicy(policy);
    }

    public async Task<PolicyResponse> UpdateAsync(
        Guid id,
        UpdatePolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        var policy = await GetPolicyOrThrowAsync(
            BuildPolicyQuery(asNoTracking: false).Where(policy => policy.Id == id),
            cancellationToken);

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

        var policy = await GetPolicyOrThrowAsync(
            BuildPolicyQuery(asNoTracking: false).Where(policy => policy.Id == id),
            cancellationToken);

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

    private IQueryable<Policy> BuildPolicyQuery(PolicyFilter? filter = null, bool asNoTracking = true)
    {
        var query = dbContext.Policies.AsQueryable();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        if (filter is not null && filter.CustomerId.HasValue)
        {
            query = query.Where(policy => policy.CustomerId == filter.CustomerId.Value);
        }

        if (filter is not null && !string.IsNullOrWhiteSpace(filter.ProductCode))
        {
            var productCode = NormalizeProductCode(filter.ProductCode);
            query = query.Where(policy => policy.ProductCode == productCode);
        }

        if (filter is not null && filter.Status.HasValue)
        {
            query = query.Where(policy => policy.Status == filter.Status.Value);
        }

        return query;
    }

    private static async Task<Policy> GetPolicyOrThrowAsync(
        IQueryable<Policy> query,
        CancellationToken cancellationToken)
    {
        var policy = await query.FirstOrDefaultAsync(cancellationToken);

        if (policy is null)
        {
            throw new NotFoundException("Policy was not found.");
        }

        return policy;
    }

    private static (Guid CustomerId, string ProductCode) ValidateIssueData(PolicyIssueData? issueData)
    {
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

        return (issueData.CustomerId, issueData.Product.Code);
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

    private class PolicyIssueData
    {
        public Guid CustomerId { get; set; }

        public bool CustomerIsActive { get; set; }

        public PolicyProductIssueData? Product { get; set; }
    }

    private class PolicyProductIssueData
    {
        public string Code { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
