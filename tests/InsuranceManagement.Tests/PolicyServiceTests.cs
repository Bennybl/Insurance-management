using InsuranceManagement.Api.Application.Common;
using InsuranceManagement.Api.Application.Policies;
using InsuranceManagement.Api.Domain;
using InsuranceManagement.Api.Infrastructure.Repositories;
using Xunit;

namespace InsuranceManagement.Tests;

public class PolicyServiceTests
{
    [Fact]
    public async Task IssueAsync_creates_active_policy_for_active_customer_and_product()
    {
        using var database = await TestDatabase.CreateAsync();
        var customer = await AddCustomerAsync(database);
        var service = new PolicyService(
            new PolicyRepository(database.Context),
            new CustomerRepository(database.Context));

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

        var service = new PolicyService(
            new PolicyRepository(database.Context),
            new CustomerRepository(database.Context));

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateAsync(policy.Id, new UpdatePolicyRequest
        {
            StartDate = new DateOnly(2026, 2, 1),
            EndDate = new DateOnly(2027, 2, 1),
            PremiumAmount = 120
        }));
    }

    [Fact]
    public async Task IssueAsync_throws_NotFound_when_customer_missing()
    {
        using var database = await TestDatabase.CreateAsync();
        var service = NewService(database);

        await Assert.ThrowsAsync<NotFoundException>(() => service.IssueAsync(new IssuePolicyRequest
        {
            CustomerId = Guid.NewGuid(),
            ProductCode = "CAR",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2027, 1, 1),
            PremiumAmount = 100
        }));
    }

    [Fact]
    public async Task IssueAsync_throws_Conflict_when_customer_inactive()
    {
        using var database = await TestDatabase.CreateAsync();
        var customer = await AddCustomerAsync(database, isActive: false);
        var service = NewService(database);

        await Assert.ThrowsAsync<ConflictException>(() => service.IssueAsync(new IssuePolicyRequest
        {
            CustomerId = customer.Id,
            ProductCode = "CAR",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2027, 1, 1),
            PremiumAmount = 100
        }));
    }

    [Fact]
    public async Task IssueAsync_throws_NotFound_when_product_missing()
    {
        using var database = await TestDatabase.CreateAsync();
        var customer = await AddCustomerAsync(database);
        var service = NewService(database);

        await Assert.ThrowsAsync<NotFoundException>(() => service.IssueAsync(new IssuePolicyRequest
        {
            CustomerId = customer.Id,
            ProductCode = "NOPE",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2027, 1, 1),
            PremiumAmount = 100
        }));
    }

    [Fact]
    public async Task CancelAsync_cancels_active_policy_and_trims_reason()
    {
        using var database = await TestDatabase.CreateAsync();
        var customer = await AddCustomerAsync(database);
        var service = NewService(database);

        var policy = await service.IssueAsync(new IssuePolicyRequest
        {
            CustomerId = customer.Id,
            ProductCode = "CAR",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2027, 1, 1),
            PremiumAmount = 100
        });

        var cancelled = await service.CancelAsync(policy.Id, new CancelPolicyRequest
        {
            Reason = "  Customer request  "
        });

        Assert.Equal(PolicyStatus.Cancelled, cancelled.Status);
        Assert.NotNull(cancelled.CancelledAt);
        Assert.Equal("Customer request", cancelled.CancellationReason);
    }

    private static PolicyService NewService(TestDatabase database)
    {
        return new PolicyService(
            new PolicyRepository(database.Context),
            new CustomerRepository(database.Context));
    }

    private static async Task<Customer> AddCustomerAsync(TestDatabase database, bool isActive = true)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FirstName = "Dana",
            LastName = "Cohen",
            Email = $"{Guid.NewGuid():N}@example.com",
            PhoneNumber = "0501234567",
            Address = "Tel Aviv",
            IsActive = isActive,
            CreatedAt = DateTimeOffset.UtcNow
        };

        database.Context.Customers.Add(customer);
        await database.Context.SaveChangesAsync();

        return customer;
    }
}
