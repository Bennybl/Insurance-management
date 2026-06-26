using System.ComponentModel.DataAnnotations;

namespace InsuranceManagement.Api.Application.Policies;

public class CancelPolicyRequest : IValidatableObject
{
    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Reason))
        {
            yield return new ValidationResult(
                "Cancellation reason is required.",
                [nameof(Reason)]);
        }
    }
}
