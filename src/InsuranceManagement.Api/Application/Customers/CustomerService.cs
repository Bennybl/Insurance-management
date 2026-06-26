using InsuranceManagement.Api.Application.Common;
using InsuranceManagement.Api.Application.Policies;
using InsuranceManagement.Api.Domain;
using InsuranceManagement.Api.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InsuranceManagement.Api.Application.Customers;

public class CustomerService(AppDbContext dbContext) : ICustomerService
{
    public async Task<CustomerResponse> CreateAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            PhoneNumber = request.PhoneNumber.Trim(),
            Address = request.Address.Trim(),
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Customers.Add(customer);
        await SaveChangesAsync(cancellationToken);

        return MapCustomer(customer);
    }

    public async Task<IReadOnlyList<CustomerResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var customers = await dbContext.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.LastName)
            .ThenBy(customer => customer.FirstName)
            .ToListAsync(cancellationToken);

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
        var customer = await dbContext.Customers
            .FirstOrDefaultAsync(customer => customer.Id == id, cancellationToken);

        if (customer is null)
        {
            throw new NotFoundException("Customer was not found.");
        }

        var email = NormalizeEmail(request.Email);

        customer.FirstName = request.FirstName.Trim();
        customer.LastName = request.LastName.Trim();
        customer.Email = email;
        customer.PhoneNumber = request.PhoneNumber.Trim();
        customer.Address = request.Address.Trim();
        customer.UpdatedAt = DateTimeOffset.UtcNow;

        await SaveChangesAsync(cancellationToken);

        return MapCustomer(customer);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers
            .FirstOrDefaultAsync(customer => customer.Id == id, cancellationToken);

        if (customer is null)
        {
            throw new NotFoundException("Customer was not found.");
        }

        var hasActivePolicies = await dbContext.Policies
            .AnyAsync(
                policy => policy.CustomerId == id && policy.Status == PolicyStatus.Active,
                cancellationToken);

        if (hasActivePolicies)
        {
            throw new ConflictException("Customer cannot be deactivated while they have active policies.");
        }

        if (!customer.IsActive)
        {
            return;
        }

        customer.IsActive = false;
        customer.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PolicyResponse>> GetPoliciesAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        _ = await GetCustomerOrThrowAsync(id, cancellationToken);

        var policies = await dbContext.Policies
            .AsNoTracking()
            .Where(policy => policy.CustomerId == id)
            .OrderByDescending(policy => policy.CreatedAt)
            .ToListAsync(cancellationToken);

        return policies.Select(MapPolicy).ToList();
    }

    private async Task<Customer> GetCustomerOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var customer = await dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(customer => customer.Id == id, cancellationToken);

        if (customer is null)
        {
            throw new NotFoundException("Customer was not found.");
        }

        return customer;
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsDuplicateCustomerEmail(exception))
        {
            throw new ConflictException("A customer with this email already exists.");
        }
    }

    private static bool IsDuplicateCustomerEmail(DbUpdateException exception)
    {
        return exception.InnerException is SqliteException { SqliteErrorCode: 19 } sqliteException &&
            sqliteException.Message.Contains("Customers.Email", StringComparison.OrdinalIgnoreCase);
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
