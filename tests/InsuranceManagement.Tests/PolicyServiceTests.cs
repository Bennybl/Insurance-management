using InsuranceManagement.Api.Application.Common;
using InsuranceManagement.Api.Application.Policies;
using InsuranceManagement.Api.Domain;
using Xunit;

namespace InsuranceManagement.Tests;

public class PolicyServiceTests
{
    [Fact]
    public async Task IssueAsync_creates_active_policy_for_active_customer_and_product()
    {
        using var database = await TestDatabase.CreateAsync();
        var customer = await AddCustomerAsync(database);
        var service = new PolicyService(database.Context);

        var policy = await service.IssueAsync(new IssuePolicyRequest
        {
            CustomerId = customer.Id,
            ProductCode = "car",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2027, 1, 1),
            PremiumAmount = 100
        });

        Assert.Equal(customer.Id, policy.CustomerId);
        Assert.Equal("CAR", policy.ProductCode);
        Assert.Equal(PolicyStatus.Active, policy.Status);
        Assert.StartsWith("POL-", policy.PolicyNumber);
    }

    [Fact]
    public async Task UpdateAsync_rejects_cancelled_policy()
    {
        using var database = await TestDatabase.CreateAsync();
        var customer = await AddCustomerAsync(database);
        var policy = new Policy
        {
            Id = Guid.NewGuid(),
            CustomerId = customer.Id,
            PolicyNumber = "POL-TEST-0002",
            ProductCode = "CAR",
            Status = PolicyStatus.Cancelled,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2027, 1, 1),
            PremiumAmount = 100,
            CreatedAt = DateTimeOffset.UtcNow,
            CancelledAt = DateTimeOffset.UtcNow,
            CancellationReason = "Cancelled"
        };

        database.Context.Policies.Add(policy);
        await database.Context.SaveChangesAsync();

        var service = new PolicyService(database.Context);

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateAsync(policy.Id, new UpdatePolicyRequest
        {
            StartDate = new DateOnly(2026, 2, 1),
            EndDate = new DateOnly(2027, 2, 1),
            PremiumAmount = 120
        }));
    }

    private static async Task<Customer> AddCustomerAsync(TestDatabase database)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Dana",
            LastName = "Cohen",
            Email = $"{Guid.NewGuid():N}@example.com",
            PhoneNumber = "0501234567",
            Address = "Tel Aviv",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        database.Context.Customers.Add(customer);
        await database.Context.SaveChangesAsync();

        return customer;
    }
}
