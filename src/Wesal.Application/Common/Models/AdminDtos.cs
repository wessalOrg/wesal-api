using Wesal.Domain.Enums;

namespace Wesal.Application.Common.Models;

// --- US-ADMIN-01: pending queue + full hall/owner detail ---

public class AdminPendingHallDto
{
    public Guid HallId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? ThumbnailUrl { get; init; }
    public DateTimeOffset SubmittedAt { get; init; }
}

public class AdminHallDetailDto
{
    public Guid HallId { get; init; }
    public string Name { get; init; } = string.Empty;
    public HallRegion Region { get; init; }
    public string RegionDisplayName { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Capacity { get; init; }
    public decimal? Price { get; init; }
    public DateTimeOffset SubmittedAt { get; init; }
    public HallStatus Status { get; init; }
    public string? OwnerFullName { get; init; }
    public string? OwnerPhoneNumber { get; init; }
    public string? OwnerEmail { get; init; }
    public IReadOnlyList<string> PhotoUrls { get; init; } = [];
}

/// <summary>Repository projection for the full submission drill-down; assembled by
/// <see cref="AdminHallDetailDto"/> (which adds the localized region display name) in
/// the admin service layer.</summary>
public class AdminHallDetailRow
{
    public Guid HallId { get; init; }
    public string Name { get; init; } = string.Empty;
    public HallRegion Region { get; init; }
    public string Address { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int Capacity { get; init; }
    public decimal? Price { get; init; }
    public DateTimeOffset SubmittedAt { get; init; }
    public HallStatus Status { get; init; }
    public string? OwnerFullName { get; init; }
    public string? OwnerPhoneNumber { get; init; }
    public string? OwnerEmail { get; init; }
    public IReadOnlyList<string> PhotoUrls { get; init; } = [];
}

// --- US-ADMIN-03: rejection ---

public class AdminRejectHallRequestDto
{
    /// <summary>Optional human-readable rejection reason delivered to the owner's inbox.</summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Required before rejecting a hall that is currently Approved (live). Rejecting a
    /// live hall is destructive and must be explicitly confirmed by the Admin.
    /// </summary>
    public bool ConfirmLiveApproved { get; init; }
}

public class AdminRejectHallResultDto
{
    public Guid HallId { get; init; }
    public string Name { get; init; } = string.Empty;
    public HallStatus Status { get; init; }
    public bool IsAlreadyRejected { get; init; }
    public bool NotificationDelivered { get; init; }
}

// --- US-ADMIN-05: manual lock ---

public class AdminLockHallResultDto
{
    public Guid HallId { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsLocked { get; init; }
    public DateTimeOffset? LockedAt { get; init; }
    public string? LockedByAdminUserId { get; init; }
}

// --- US-ADMIN-11: subscription overview ---

public class AdminSubscriptionOverviewQueryDto
{
    public const string SortByOwnerName = "ownerName";
    public const string SortByDaysRemaining = "daysRemaining";

    public HallStatus? ApprovalStatus { get; init; }

    public HallPaymentStatus? PaymentStatus { get; init; }

    /// <summary>When set, only halls that are locked (Admin lock and/or system lock) are returned.</summary>
    public bool? Locked { get; init; }

    /// <summary>Sort key: <see cref="SortByOwnerName"/> (default) or <see cref="SortByDaysRemaining"/>.</summary>
    public string SortBy { get; init; } = SortByOwnerName;
}

public class AdminSubscriptionHallRow
{
    public Guid HallId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string OwnerId { get; init; } = string.Empty;
    public string? OwnerFullName { get; init; }
    public string? OwnerPhoneNumber { get; init; }
    public string? OwnerEmail { get; init; }
    public HallStatus Status { get; init; }
    public HallPaymentStatus PaymentStatus { get; init; }
    public bool SystemLocked { get; init; }
    public bool AdminLocked { get; init; }
    public DateOnly? NextBillingDate { get; init; }
    public DateOnly? LastPaymentDate { get; init; }
    public DateTimeOffset? LockedAt { get; init; }
    public string? LockedByAdminUserId { get; init; }
}

public class AdminSubscriptionHallDto
{
    public Guid HallId { get; init; }
    public string Name { get; init; } = string.Empty;
    public HallStatus ApprovalStatus { get; init; }
    public HallPaymentStatus PaymentStatus { get; init; }
    public bool SystemLocked { get; init; }
    public bool AdminLocked { get; init; }
    public DateOnly? NextBillingDate { get; init; }
    public int? DaysRemaining { get; init; }
    public DateOnly? LastPaymentDate { get; init; }
    public DateTimeOffset? LockedAt { get; init; }
    public string? LockedByAdminUserId { get; init; }
}

public class AdminSubscriptionOwnerGroupDto
{
    public string OwnerId { get; init; } = string.Empty;
    public string? OwnerFullName { get; init; }
    public string? OwnerPhoneNumber { get; init; }
    public string? OwnerEmail { get; init; }
    public IReadOnlyList<AdminSubscriptionHallDto> Halls { get; init; } = [];
}