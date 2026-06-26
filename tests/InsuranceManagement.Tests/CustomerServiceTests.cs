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
}
