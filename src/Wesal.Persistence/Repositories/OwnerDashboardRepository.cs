using Microsoft.EntityFrameworkCore;
using Wesal.Application.Common.Interfaces.Persistence;
using Wesal.Persistence.Data;

namespace Wesal.Persistence.Repositories;

public sealed class OwnerDashboardRepository : IOwnerDashboardRepository
{
    private readonly ApplicationDbContext _context;

    public OwnerDashboardRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<int> GetHallCountByOwnerAsync(string ownerId, CancellationToken cancellationToken = default)
        => _context.Halls
            .AsNoTracking()
            .CountAsync(hall => hall.OwnerId == ownerId && !hall.IsDeleted, cancellationToken);
}