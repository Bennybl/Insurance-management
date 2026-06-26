using InsuranceManagement.Api.Application.Common;
using InsuranceManagement.Api.Application.Policies;
using InsuranceManagement.Api.Domain;
using InsuranceManagement.Api.Infrastructure.Repositories;

namespace InsuranceManagement.Api.Application.Customers;

public class CustomerService(
    ICustomerRepository customerRepository,
    IPolicyRepository policyRepository) : ICustomerService
{
    public async Task<CustomerResponse> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = NormalizeEmail(request.Email),
            PhoneNumber = request.PhoneNumber.Trim(),
            Address = request.Address.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        customerRepository.Add(customer);
        await customerRepository.SaveChangesAsync(cancellationToken);

        return MapCustomer(customer);
    }

    public async Task<IReadOnlyList<CustomerResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var customers = await customerRepository.GetAllAsync(cancellationToken);

        return customers.Select(MapCustomer).ToList();
    }

    public async Task<CustomerResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await GetCustomerOrThrowAsync(id, cancellationToken);

        return MapCustomer(customer);
    }

    public async Task<CustomerResponse> UpdateAsync(
        Guid id,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var customer = await GetCustomerOrThrowAsync(id, cancellationToken, track: true);

        customer.FirstName = request.FirstName.Trim();
        customer.LastName = request.LastName.Trim();
        customer.Email = NormalizeEmail(request.Email);
        customer.PhoneNumber = request.PhoneNumber.Trim();
        customer.Address = request.Address.Trim();
        customer.UpdatedAt = DateTimeOffset.UtcNow;

        await customerRepository.SaveChangesAsync(cancellationToken);

        return MapCustomer(customer);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await GetCustomerOrThrowAsync(id, cancellationToken, track: true);

        if (!customer.IsActive)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        var activePolicies = await policyRepository.GetActiveByCustomerAsync(id, cancellationToken);

        foreach (var policy in activePolicies)
        {
            policy.Status = PolicyStatus.Cancelled;
            policy.CancelledAt = now;
            policy.CancellationReason = "Customer deactivated.";
            policy.UpdatedAt = now;
        }

        customer.IsActive = false;
        customer.UpdatedAt = now;

        await customerRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PolicyResponse>> GetPoliciesAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var policies = await policyRepository.GetByCustomerAsync(id, cancellationToken);

        return policies.Select(MapPolicy).ToList();
    }

    private async Task<Customer> GetCustomerOrThrowAsync(
        Guid id,
        CancellationToken cancellationToken,
        bool track = false)
    {
        var customer = await customerRepository.GetByIdAsync(id, track, cancellationToken);

        if (customer is null)
        {
            throw new NotFoundException("Customer was not found.");
        }

        return customer;
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim();
    }

    private static CustomerResponse MapCustomer(Customer customer)
    {
        return new CustomerResponse
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            LastName = customer.LastName,
            Email = customer.Email,
            PhoneNumber = customer.PhoneNumber,
            Address = customer.Address,
            IsActive = customer.IsActive,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt
        };
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
