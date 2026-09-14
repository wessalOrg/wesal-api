using Wesal.Application.Common.Interfaces;
using Wesal.Application.Common.Interfaces.Persistence;
using Wesal.Application.Common.Models;

namespace Wesal.Infrastructure.Admin;

/// <summary>
/// Admin subscription overview (US-ADMIN-11, FR-SUB-06). Aggregates every hall across
/// all owners into a single grouped, filterable, sortable view exposing HallStatus,
/// PaymentStatus, SystemLocked, AdminLocked, the current cycle end date (days
/// remaining) and the last payment date. All values are computed live from the
/// persisted hall records on every call, so the dashboard never shows stale lock or
/// payment state before an Admin acts (the inline Lock/Unlock/Paid actions remain the
/// existing US-ADMIN-05/06/10 endpoints — no parallel duplicates are created here).
/// </summary>
public sealed class AdminSubscriptionService : IAdminSubscriptionService
{
    private readonly IAdminDashboardRepository _adminDashboardRepository;
    private readonly IDateTime _dateTime;

    public AdminSubscriptionService(
        IAdminDashboardRepository adminDashboardRepository,
        IDateTime dateTime)
    {
        _adminDashboardRepository = adminDashboardRepository;
        _dateTime = dateTime;
    }

    public async Task<IReadOnlyList<AdminSubscriptionOwnerGroupDto>> GetSubscriptionOverviewAsync(
        AdminSubscriptionOverviewQueryDto query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await _adminDashboardRepository.GetSubscriptionOverviewAsync(cancellationToken);

        var filtered = rows
            .Where(row => query.ApprovalStatus is null || row.Status == query.ApprovalStatus)
            .Where(row => query.PaymentStatus is null || row.PaymentStatus == query.PaymentStatus)
            .Where(row => query.Locked is null || (row.SystemLocked || row.AdminLocked) == query.Locked.Value)
            .ToList();

        var today = DateOnly.FromDateTime(_dateTime.Now.UtcDateTime);

        var groups = filtered
            .GroupBy(row => new AdminOwnerKey(
                row.OwnerId,
                row.OwnerFullName,
                row.OwnerPhoneNumber,
                row.OwnerEmail))
            .Select(group => new AdminSubscriptionOwnerGroupDto
            {
                OwnerId = group.Key.OwnerId,
                OwnerFullName = group.Key.OwnerFullName,
                OwnerPhoneNumber = group.Key.OwnerPhoneNumber,
                OwnerEmail = group.Key.OwnerEmail,
                Halls = OrderHalls(group.Select(row => MapHall(row, today)), query.SortBy)
            })
            .ToList();

        return OrderGroups(groups, query.SortBy);
    }

    private static List<AdminSubscriptionHallDto> OrderHalls(
        IEnumerable<AdminSubscriptionHallDto> halls,
        string sortBy)
    {
        return sortBy.Equals(AdminSubscriptionOverviewQueryDto.SortByDaysRemaining, StringComparison.OrdinalIgnoreCase)
            ? halls
                .OrderBy(hall => hall.DaysRemaining ?? int.MaxValue)
                .ThenBy(hall => hall.Name, StringComparer.OrdinalIgnoreCase)
                .ToList()
            : halls
                .OrderBy(hall => hall.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    private static IReadOnlyList<AdminSubscriptionOwnerGroupDto> OrderGroups(
        List<AdminSubscriptionOwnerGroupDto> groups,
        string sortBy)
    {
        return sortBy.Equals(AdminSubscriptionOverviewQueryDto.SortByDaysRemaining, StringComparison.OrdinalIgnoreCase)
            ? groups
                .OrderBy(group => group.Halls.Min(hall => hall.DaysRemaining ?? int.MaxValue))
                .ThenBy(group => group.OwnerFullName ?? group.OwnerId, StringComparer.OrdinalIgnoreCase)
                .ToList()
            : groups
                .OrderBy(group => group.OwnerFullName ?? group.OwnerId, StringComparer.OrdinalIgnoreCase)
                .ToList();
    }

    private static AdminSubscriptionHallDto MapHall(AdminSubscriptionHallRow row, DateOnly today)
        => new()
        {
            HallId = row.HallId,
            Name = row.Name,
            ApprovalStatus = row.Status,
            PaymentStatus = row.PaymentStatus,
            SystemLocked = row.SystemLocked,
            AdminLocked = row.AdminLocked,
            NextBillingDate = row.NextBillingDate,
            DaysRemaining = row.NextBillingDate is null
                ? null
                : row.NextBillingDate.Value.DayNumber - today.DayNumber,
            LastPaymentDate = row.LastPaymentDate,
            LockedAt = row.LockedAt,
            LockedByAdminUserId = row.LockedByAdminUserId
        };

    private sealed record AdminOwnerKey(
        string OwnerId,
        string? OwnerFullName,
        string? OwnerPhoneNumber,
        string? OwnerEmail);
}