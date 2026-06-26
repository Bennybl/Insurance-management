namespace InsuranceManagement.Api.Domain;

public class PolicyProduct
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<Policy> Policies { get; set; } = new List<Policy>();
}
