using InsuranceManagement.Api.Application.Common;
using InsuranceManagement.Api.Application.Customers;
using InsuranceManagement.Api.Domain;
using InsuranceManagement.Api.Infrastructure.Repositories;
using Xunit;

namespace InsuranceManagement.Tests;

public class CustomerServiceTests
{
    [Fact]
    public async Task CreateAsync_creates_active_customer()
    {
        using var database = await TestDatabase.CreateAsync();
        var service = new CustomerService(
            new CustomerRepository(database.Context),
            new PolicyRepository(database.Context));

        var customer = await service.CreateAsync(new CreateCustomerRequest
        {
            FirstName = "Dana",
            LastName = "Cohen",
            Email = " dana@example.com ",
            PhoneNumber = "0501234567",
            Address = "Tel Aviv"
        });

        Assert.NotEqual(Guid.Empty, customer.Id);
        Assert.True(customer.IsActive);
        Assert.Equal("dana@example.com", customer.Email);
    }

    [Fact]
    public async Task DeactivateAsync_cancels_customers_active_policies()
    {
        using var database = await TestDatabase.CreateAsync();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Dana",
            LastName = "Cohen",
            Email = "dana@example.com",
            PhoneNumber = "0501234567",
            Address = "Tel Aviv",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var policy = new Policy
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            PolicyNumber = "POL-TEST-0001",
            ProductCode = "CAR",
            Status = PolicyStatus.Active,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2027, 1, 1),
            PremiumAmount = 100,
            CreatedAt = DateTimeOffset.UtcNow
        };

        database.Context.Customers.Add(customer);
        database.Context.Policies.Add(policy);
        await database.Context.SaveChangesAsync();

        var service = new CustomerService(
            new CustomerRepository(database.Context),
            new PolicyRepository(database.Context));

        await service.DeactivateAsync(customer.Id);

        Assert.False(customer.IsActive);
        Assert.Equal(PolicyStatus.Cancelled, policy.Status);
        Assert.NotNull(policy.CancelledAt);
        Assert.Equal("Customer deactivated.", policy.CancellationReason);
    }

    [Fact]
    public async Task GetByIdAsync_throws_NotFound_when_customer_missing()
    {
        using var database = await TestDatabase.CreateAsync();
        var service = NewService(database);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateAsync_updates_fields_and_sets_UpdatedAt()
    {
        using var database = await TestDatabase.CreateAsync();
        var service = NewService(database);

        var created = await service.CreateAsync(new CreateCustomerRequest
        {
            FirstName = "Dana",
            LastName = "Cohen",
            Email = "dana@example.com",
            PhoneNumber = "0501234567",
            Address = "Tel Aviv"
        });

        var updated = await service.UpdateAsync(created.Id, new UpdateCustomerRequest
        {
            FirstName = " Noa ",
            LastName = " Levi ",
            Email = " noa@example.com ",
            PhoneNumber = "0521112222",
            Address = " Haifa "
        });

        Assert.Equal("Noa", updated.FirstName);
        Assert.Equal("Levi", updated.LastName);
        Assert.Equal("noa@example.com", updated.Email);
        Assert.Equal("Haifa", updated.Address);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task GetAllAsync_returns_customers_ordered_by_last_then_first_name()
    {
        using var database = await TestDatabase.CreateAsync();
        var service = NewService(database);

        await service.CreateAsync(new CreateCustomerRequest
        {
            FirstName = "Dana",
            LastName = "Cohen",
            Email = "dana@example.com",
            PhoneNumber = "0501234567",
            Address = "Tel Aviv"
        });
        await service.CreateAsync(new CreateCustomerRequest
        {
            FirstName = "Boaz",
            LastName = "Avraham",
            Email = "boaz@example.com",
            PhoneNumber = "0507654321",
            Address = "Eilat"
        });

        var customers = await service.GetAllAsync();

        Assert.Equal(2, customers.Count);
        Assert.Equal("Avraham", customers[0].LastName);
        Assert.Equal("Cohen", customers[1].LastName);
    }

    [Fact]
    public async Task DeactivateAsync_does_nothing_when_customer_already_inactive()
    {
        using var database = await TestDatabase.CreateAsync();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Dana",
            LastName = "Cohen",
            Email = "dana@example.com",
            PhoneNumber = "0501234567",
            Address = "Tel Aviv",
            IsActive = false,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var policy = NewPolicy(customer.Id, "POL-TEST-1003");
        database.Context.Customers.Add(customer);
        database.Context.Policies.Add(policy);
        await database.Context.SaveChangesAsync();

        var service = NewService(database);

        await service.DeactivateAsync(customer.Id);

        // Already inactive: the service returns early and leaves existing policies untouched.
        Assert.False(customer.IsActive);
        Assert.Equal(PolicyStatus.Active, policy.Status);
        Assert.Null(policy.CancelledAt);
    }

    private static CustomerService NewService(TestDatabase database)
    {
        return new CustomerService(
            new CustomerRepository(database.Context),
            new PolicyRepository(database.Context));
    }

    private static Policy NewPolicy(Guid customerId, string policyNumber)
    {
        return new Policy
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            PolicyNumber = policyNumber,
            ProductCode = "CAR",
            Status = PolicyStatus.Active,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2027, 1, 1),
            PremiumAmount = 100,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
