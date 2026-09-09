using Wesal.Domain.Enums;

namespace Wesal.Application.Common.Models;

/// <summary>
/// A hall owned by the authenticated Hall Owner together with its current approval
/// status (US-OWNER-05). The status is always read from the persisted hall record on
/// each request, so the owner sees the latest PendingReview/Approved/Rejected state
/// even when an Admin acted in another session. Ownership is resolved exclusively from
/// the authenticated session; the DTO never carries a client-supplied owner identity.
/// </summary>
public class OwnerHallDto
{
    public Guid HallId { get; init; }

    public string HallName { get; init; } = string.Empty;

    public HallStatus Status { get; init; }
}