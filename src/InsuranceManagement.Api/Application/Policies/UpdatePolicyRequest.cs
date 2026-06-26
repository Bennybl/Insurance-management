using System.ComponentModel.DataAnnotations;

namespace InsuranceManagement.Api.Application.Policies;

public class UpdatePolicyRequest : IValidatableObject
{
    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    public decimal PremiumAmount { get; set; }

    public string? DetailsJson { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate >= EndDate)
        {
            yield return new ValidationResult(
                "StartDate must be before EndDate.",
                [nameof(StartDate), nameof(EndDate)]);
        }

        if (PremiumAmount <= 0)
        {
            yield return new ValidationResult(
                "PremiumAmount must be positive.",
                [nameof(PremiumAmount)]);
        }
    }
}
