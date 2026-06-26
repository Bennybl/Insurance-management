using System.ComponentModel.DataAnnotations;

namespace InsuranceManagement.Api.Application.Customers;

public sealed class UpdateCustomerRequest
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(320)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    [StringLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Address { get; set; } = string.Empty;
}
