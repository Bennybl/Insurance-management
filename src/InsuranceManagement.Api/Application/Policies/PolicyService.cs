using InsuranceManagement.Api.Application.Common;
using InsuranceManagement.Api.Domain;
using InsuranceManagement.Api.Infrastructure.Repositories;

namespace InsuranceManagement.Api.Application.Policies;

public class PolicyService(
    IPolicyRepository policyRepository,
    ICustomerRepository customerRepository) : IPolicyService
{
    public async Task<PolicyResponse> IssueAsync(
        IssuePolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken: cancellationToken);

        if (customer is null)
        {
            throw new NotFoundException("Customer was not found.");
        }

        if (!customer.IsActive)
        {
            throw new ConflictException("Cannot issue a policy to an inactive customer.");
        }

        var product = await policyRepository.GetProductAsync(request.ProductCode, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException("Policy product was not found.");
        }

        if (!product.IsActive)
        {
            throw new ConflictException("Cannot issue a policy for an inactive product.");
        }

        var policy = new Policy
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            PolicyNumber = GeneratePolicyNumber(),
            ProductCode = product.Code,
            Status = PolicyStatus.Active,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            PremiumAmount = request.PremiumAmount,
            DetailsJson = request.DetailsJson,
            CreatedAt = DateTimeOffset.UtcNow
        };

        policyRepository.Add(policy);
        await policyRepository.SaveChangesAsync(cancellationToken);

        return MapPolicy(policy);
    }

    public async Task<IReadOnlyList<PolicyResponse>> GetAllAsync(
        PolicyFilter filter,
        CancellationToken cancellationToken = default)
    {
        var policies = await policyRepository.GetAllAsync(
            filter.CustomerId,
            filter.ProductCode,
            filter.Status,
            cancellationToken);

        return policies.Select(MapPolicy).ToList();
    }

    public async Task<PolicyResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var policy = await GetPolicyOrThrowAsync(id, cancellationToken);

        return MapPolicy(policy);
    }

    public async Task<PolicyResponse> UpdateAsync(
        Guid id,
        UpdatePolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        var policy = await GetPolicyOrThrowAsync(id, cancellationToken, track: true);

        if (policy.Status == PolicyStatus.Cancelled)
        {
            throw new ConflictException("Cannot update a cancelled policy.");
        }

        policy.StartDate = request.StartDate;
        policy.EndDate = request.EndDate;
        policy.PremiumAmount = request.PremiumAmount;
        policy.DetailsJson = request.DetailsJson;
        policy.UpdatedAt = DateTimeOffset.UtcNow;

        await policyRepository.SaveChangesAsync(cancellationToken);

        return MapPolicy(policy);
    }

    public async Task<PolicyResponse> CancelAsync(
        Guid id,
        CancelPolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        var cancellationReason = request.Reason.Trim();

        var policy = await GetPolicyOrThrowAsync(id, cancellationToken, track: true);

        if (policy.Status == PolicyStatus.Cancelled)
        {
            throw new ConflictException("Policy is already cancelled.");
        }

        var now = DateTimeOffset.UtcNow;

        policy.Status = PolicyStatus.Cancelled;
        policy.CancelledAt = now;
        policy.CancellationReason = cancellationReason;
        policy.UpdatedAt = now;

        await policyRepository.SaveChangesAsync(cancellationToken);

        return MapPolicy(policy);
    }

    private async Task<Policy> GetPolicyOrThrowAsync(
        Guid id,
        CancellationToken cancellationToken,
        bool track = false)
    {
        var policy = await policyRepository.GetByIdAsync(id, track, cancellationToken);

        if (policy is null)
        {
            throw new NotFoundException("Policy was not found.");
        }

        return policy;
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
