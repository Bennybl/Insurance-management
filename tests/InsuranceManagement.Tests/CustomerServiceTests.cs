using InsuranceManagement.Api.Application.Common;
using InsuranceManagement.Api.Application.Customers;
using InsuranceManagement.Api.Domain;
using Xunit;

namespace InsuranceManagement.Tests;

public class CustomerServiceTests
{
    [Fact]
    public async Task CreateAsync_creates_active_customer()
    {
        using var database = await TestDatabase.CreateAsync();
        var service = new CustomerService(database.Context);

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
    public async Task DeactivateAsync_rejects_customer_with_active_policy()
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

        database.Context.Customers.Add(customer);
        database.Context.Policies.Add(new Policy
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
        });
        await database.Context.SaveChangesAsync();

        var service = new CustomerService(database.Context);

        await Assert.ThrowsAsync<ConflictException>(() => service.DeactivateAsync(customer.Id));
    }
}
