namespace InsuranceManagement.Api.Application.Policies;

public interface IPolicyService
{
    Task<PolicyResponse> IssueAsync(IssuePolicyRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PolicyResponse>> GetAllAsync(PolicyFilter filter, CancellationToken cancellationToken = default);

    Task<PolicyResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PolicyResponse> UpdateAsync(Guid id, UpdatePolicyRequest request, CancellationToken cancellationToken = default);

    Task<PolicyResponse> CancelAsync(Guid id, CancelPolicyRequest request, CancellationToken cancellationToken = default);
}
