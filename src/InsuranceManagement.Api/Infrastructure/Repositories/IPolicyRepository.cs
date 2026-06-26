using InsuranceManagement.Api.Domain;

namespace InsuranceManagement.Api.Infrastructure.Repositories;

public interface IPolicyRepository
{
    Task<IReadOnlyList<Policy>> GetAllAsync(
        Guid? customerId = null,
        string? productCode = null,
        PolicyStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<Policy?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Policy>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Policy>> GetActiveByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<PolicyProduct?> GetProductAsync(string productCode, CancellationToken cancellationToken = default);

    void Add(Policy policy);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
