using InsuranceManagement.Api.Domain;

namespace InsuranceManagement.Api.Infrastructure.Repositories;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Customer?> GetByIdAsync(Guid id, bool track = false, CancellationToken cancellationToken = default);

    void Add(Customer customer);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
