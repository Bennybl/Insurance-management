using InsuranceManagement.Api.Application.Policies;

namespace InsuranceManagement.Api.Application.Customers;

public interface ICustomerService
{
    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CustomerResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<CustomerResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CustomerResponse> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);

    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PolicyResponse>> GetPoliciesAsync(Guid id, CancellationToken cancellationToken = default);
}
