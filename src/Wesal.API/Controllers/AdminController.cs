using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wesal.Application.Common.Interfaces;
using Wesal.Application.Common.Models;
using Wesal.Domain.Constants;

namespace Wesal.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin/halls")]
[Authorize(Policy = ApplicationPolicies.RequireAdmin)]
public class AdminController : ControllerBase
{
    private readonly IAdminHallService _adminHallService;

    public AdminController(IAdminHallService adminHallService)
    {
        _adminHallService = adminHallService;
    }

    [HttpPost("{hallId:guid}/approve")]
    [ProducesResponseType(typeof(HallApprovalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<HallApprovalResponse>> ApproveHall(Guid hallId, CancellationToken cancellationToken)
    {
        var response = await _adminHallService.ApproveHallAsync(hallId, cancellationToken);
        return Ok(response);
    }
}
