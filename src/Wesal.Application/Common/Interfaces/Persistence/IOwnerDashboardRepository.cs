using Wesal.Domain.Entities;

namespace Wesal.Application.Common.Interfaces.Persistence;

/// <summary>
/// Read-only data needed by the Hall Owner management interface (US-OWNER-01,
/// US-OWNER-05). Kept separate from <see cref="IHallRepository"/> so the owner
/// dashboard features do not widen the hall repository contract used by existing
/// public features.
/// </summary>
public interface IOwnerDashboardRepository
{
    /// <summary>Counts the non-deleted halls owned by a given user.</summary>
    Task<int> GetHallCountByOwnerAsync(string ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the non-deleted halls owned by a given user. The status of each hall
    /// is read from its persisted record on every call so the owner always sees the
    /// current approval state. Ownership is filtered server-side; the owner id is
    /// never taken from client input.
    /// </summary>
    Task<IReadOnlyList<Hall>> GetOwnedHallsAsync(string ownerId, CancellationToken cancellationToken = default);
}