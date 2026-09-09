using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wesal.Application.Common.Interfaces;
using Wesal.Application.Common.Models;
using Wesal.Domain.Constants;

namespace Wesal.API.Controllers;

/// <summary>
/// Hall Owner management interface (Epic 8, US-OWNER-01). The RequireHallOwner
/// policy provides role-based routing: only Hall Owners reach these endpoints,
/// so a Regular User tapping the Profile icon keeps the simple profile panel.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/owner")]
[Authorize(Policy = ApplicationPolicies.RequireHallOwner)]
public class OwnerController : ControllerBase
{
    private readonly IOwnerSidebarService _sidebarService;
    private readonly IHallCreationService _hallCreationService;

    public OwnerController(IOwnerSidebarService sidebarService, IHallCreationService hallCreationService)
    {
        _sidebarService = sidebarService;
        _hallCreationService = hallCreationService;
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

    [HttpPost("halls")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(CreateHallResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CreateHallResponse>> CreateHall(
        [FromForm] string Name,
        [FromForm] string ContactPhone,
        [FromForm] string Region,
        [FromForm] string Address,
        [FromForm] string Description,
        [FromForm] int Capacity,
        [FromForm] decimal? Price,
        [FromForm] TimeOnly FirstPeriodStart,
        [FromForm] TimeOnly FirstPeriodEnd,
        [FromForm] TimeOnly SecondPeriodStart,
        [FromForm] TimeOnly SecondPeriodEnd,
        [FromForm] IFormFile[]? Photos,
        CancellationToken cancellationToken)
    {
        var photoUploads = Photos == null ? null : await Task.WhenAll(Photos.Select(async p =>
        {
            using var ms = new MemoryStream();
            await p.CopyToAsync(ms, cancellationToken);
            return new HallPhotoUpload { FileName = p.FileName, ContentType = p.ContentType, Content = ms.ToArray() };
        }));

        var request = new CreateHallRequest
        {
            Name = Name,
            ContactPhone = ContactPhone,
            Region = Region,
            Address = Address,
            Description = Description,
            Capacity = Capacity,
            Price = Price,
            FirstPeriodStart = FirstPeriodStart,
            FirstPeriodEnd = FirstPeriodEnd,
            SecondPeriodStart = SecondPeriodStart,
            SecondPeriodEnd = SecondPeriodEnd,
            Photos = photoUploads
        };

        var response = await _hallCreationService.CreateHallAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetSidebar), response);
    }
}