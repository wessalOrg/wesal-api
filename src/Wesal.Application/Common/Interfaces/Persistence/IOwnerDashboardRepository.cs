namespace Wesal.Application.Common.Interfaces.Persistence;

/// <summary>
/// Read-only data needed by the Hall Owner management interface (US-OWNER-01).
/// Kept separate from <see cref="IHallRepository"/> so the owner sidebar feature
/// does not widen the hall repository contract used by existing features.
/// </summary>
public interface IOwnerDashboardRepository
{
    /// <summary>Counts the non-deleted halls owned by a given user.</summary>
    Task<int> GetHallCountByOwnerAsync(string ownerId, CancellationToken cancellationToken = default);
}