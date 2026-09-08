using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wesal.Application.Common.Interfaces;
using Wesal.Application.Common.Models;
using Wesal.Domain.Constants;

namespace Wesal.API.Controllers;

/// <summary>
/// Hall Owner management interface (Epic 8: US-OWNER-01, US-OWNER-03). The
/// RequireHallOwner policy provides role-based routing: only Hall Owners reach
/// these endpoints, so a Regular User tapping the Profile icon keeps the simple
/// profile panel.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/owner")]
[Authorize(Policy = ApplicationPolicies.RequireHallOwner)]
public class OwnerController : ControllerBase
{
    private readonly IOwnerSidebarService _sidebarService;
    private readonly IHallInitiationService _hallInitiationService;

    public OwnerController(
        IOwnerSidebarService sidebarService,
        IHallInitiationService hallInitiationService)
    {
        _sidebarService = sidebarService;
        _hallInitiationService = hallInitiationService;
    }

    /// <summary>
    /// Returns the sidebar data for the Hall Owner management interface.
    /// The 'Profile' section is first and open by default; its content is loaded
    /// through the profile API rather than embedded here, so the Profile section
    /// remains accessible even if this management data fails to load.
    /// </summary>
    [HttpGet("sidebar")]
    [ProducesResponseType(typeof(OwnerSidebarResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<OwnerSidebarResponse>> GetSidebar(CancellationToken cancellationToken)
    {
        var response = await _sidebarService.GetSidebarAsync(cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Starts the 'Add Hall' flow (US-OWNER-03). Tapping the Add Hall button calls
    /// this endpoint before opening the Add Hall form (US-OWNER-04). It verifies a
    /// live, authenticated Hall Owner session and returns a machine-readable 'Ready'
    /// response; it never creates an empty or invalid hall record. Blocked states
    /// surface as HTTP problem details: 401 (unauthenticated/expired session), 403
    /// (not a Hall Owner) and 404 (session points to a deleted account).
    /// </summary>
    [HttpPost("halls/initiate")]
    [ProducesResponseType(typeof(HallInitiationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HallInitiationResponse>> InitiateAddHall(CancellationToken cancellationToken)
    {
        var response = await _hallInitiationService.InitiateAsync(cancellationToken);
        return Ok(response);
    }
}