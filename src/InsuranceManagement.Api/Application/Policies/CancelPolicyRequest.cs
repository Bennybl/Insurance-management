using System.ComponentModel.DataAnnotations;

namespace InsuranceManagement.Api.Application.Policies;

public sealed class CancelPolicyRequest
{
    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}
