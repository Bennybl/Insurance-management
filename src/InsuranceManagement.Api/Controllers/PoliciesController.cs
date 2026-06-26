using InsuranceManagement.Api.Application.Policies;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PoliciesController(IPolicyService policyService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(PolicyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PolicyResponse>> Issue(
        IssuePolicyRequest request,
        CancellationToken cancellationToken)
    {
        var policy = await policyService.IssueAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = policy.Id }, policy);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PolicyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<PolicyResponse>>> GetAll(
        [FromQuery] PolicyFilter filter,
        CancellationToken cancellationToken)
    {
        var policies = await policyService.GetAllAsync(filter, cancellationToken);

        return Ok(policies);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PolicyResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var policy = await policyService.GetByIdAsync(id, cancellationToken);

        return Ok(policy);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PolicyResponse>> Update(
        Guid id,
        UpdatePolicyRequest request,
        CancellationToken cancellationToken)
    {
        var policy = await policyService.UpdateAsync(id, request, cancellationToken);

        return Ok(policy);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(PolicyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PolicyResponse>> Cancel(
        Guid id,
        CancelPolicyRequest request,
        CancellationToken cancellationToken)
    {
        var policy = await policyService.CancelAsync(id, request, cancellationToken);

        return Ok(policy);
    }
}
