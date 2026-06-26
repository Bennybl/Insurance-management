using System.ComponentModel.DataAnnotations;
using InsuranceManagement.Api.Domain;

namespace InsuranceManagement.Api.Application.Policies;

public class PolicyFilter
{
    public Guid? CustomerId { get; set; }

    [StringLength(30)]
    public string? ProductCode { get; set; }

    public PolicyStatus? Status { get; set; }
}
