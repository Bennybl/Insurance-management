using System.ComponentModel.DataAnnotations;

namespace InsuranceManagement.Api.Application.Policies;

public sealed class IssuePolicyRequest : IValidatableObject
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required]
    [StringLength(30)]
    public string ProductCode { get; set; } = string.Empty;

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999999999.99")]
    public decimal PremiumAmount { get; set; }

    public string? DetailsJson { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CustomerId == Guid.Empty)
        {
            yield return new ValidationResult(
                "CustomerId is required.",
                [nameof(CustomerId)]);
        }

        if (StartDate >= EndDate)
        {
            yield return new ValidationResult(
                "StartDate must be before EndDate.",
                [nameof(StartDate), nameof(EndDate)]);
        }
    }
}
