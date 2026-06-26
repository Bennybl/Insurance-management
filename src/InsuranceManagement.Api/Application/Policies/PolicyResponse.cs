using InsuranceManagement.Api.Domain;

namespace InsuranceManagement.Api.Application.Policies;

public class PolicyResponse
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public string PolicyNumber { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public PolicyStatus Status { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal PremiumAmount { get; set; }

    public string? DetailsJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public string? CancellationReason { get; set; }
}
