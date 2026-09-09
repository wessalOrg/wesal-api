using Microsoft.EntityFrameworkCore;
using Wesal.Domain.Entities;
using Wesal.Domain.Enums;
using Wesal.Persistence.Data;
using Wesal.Persistence.Repositories;

namespace Wesal.Tests.Persistence;

public class OwnerDashboardRepositoryShould
{
    [Fact]
    public async Task GetOwnedHalls_ReturnsOnlyOwnersHalls_WithCurrentStatus()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid().ToString();
        var otherId = Guid.NewGuid().ToString();
        var pending = new Hall { Id = Guid.NewGuid(), Name = "Pending Hall", OwnerId = ownerId, Status = HallStatus.PendingReview };
        var approved = new Hall { Id = Guid.NewGuid(), Name = "Approved Hall", OwnerId = ownerId, Status = HallStatus.Approved };
        var rejected = new Hall { Id = Guid.NewGuid(), Name = "Rejected Hall", OwnerId = ownerId, Status = HallStatus.Rejected };
        var other = new Hall { Id = Guid.NewGuid(), Name = "Other's Hall", OwnerId = otherId, Status = HallStatus.Approved };
        context.Halls.AddRange(pending, approved, rejected, other);
        await context.SaveChangesAsync();

        var repository = new OwnerDashboardRepository(context);

        var result = await repository.GetOwnedHallsAsync(ownerId);

        Assert.Equal(3, result.Count);
        Assert.Contains(result, hall => hall.Id == pending.Id && hall.Status == HallStatus.PendingReview);
        Assert.Contains(result, hall => hall.Id == approved.Id && hall.Status == HallStatus.Approved);
        Assert.Contains(result, hall => hall.Id == rejected.Id && hall.Status == HallStatus.Rejected);
        Assert.DoesNotContain(result, hall => hall.Id == other.Id);
    }

    [Fact]
    public async Task GetOwnedHalls_ExcludesDeletedHalls()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid().ToString();
        context.Halls.AddRange(
            new Hall { Id = Guid.NewGuid(), Name = "Visible", OwnerId = ownerId, Status = HallStatus.PendingReview },
            new Hall { Id = Guid.NewGuid(), Name = "Deleted", OwnerId = ownerId, Status = HallStatus.PendingReview, IsDeleted = true });
        await context.SaveChangesAsync();

        var repository = new OwnerDashboardRepository(context);

        var result = await repository.GetOwnedHallsAsync(ownerId);

        var hall = Assert.Single(result);
        Assert.Equal("Visible", hall.Name);
    }

    [Fact]
    public async Task GetOwnedHalls_ReturnsEmpty_WhenOwnerHasNoHalls()
    {
        await using var context = CreateContext();

        var repository = new OwnerDashboardRepository(context);

        var result = await repository.GetOwnedHallsAsync(Guid.NewGuid().ToString());

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetOwnedHalls_OrdersByCreatedAtDescending_ThenByName()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid().ToString();
        var now = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
        context.Halls.AddRange(
            new Hall { Id = Guid.NewGuid(), Name = "A", OwnerId = ownerId, Status = HallStatus.PendingReview, CreatedAt = now },
            new Hall { Id = Guid.NewGuid(), Name = "B", OwnerId = ownerId, Status = HallStatus.PendingReview, CreatedAt = now.AddMinutes(-10) },
            new Hall { Id = Guid.NewGuid(), Name = "C", OwnerId = ownerId, Status = HallStatus.PendingReview, CreatedAt = now });
        await context.SaveChangesAsync();

        var repository = new OwnerDashboardRepository(context);

        var result = await repository.GetOwnedHallsAsync(ownerId);

        Assert.Collection(
            result,
            hall => Assert.Equal("A", hall.Name),
            hall => Assert.Equal("C", hall.Name),
            hall => Assert.Equal("B", hall.Name));
    }

    [Fact]
    public async Task GetOwnedHallWithDetails_ReturnsOwnedHallWithPeriodsAndPhotos()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid().ToString();
        var hallId = Guid.NewGuid();
        var hall = new Hall { Id = hallId, Name = "Grand Hall", OwnerId = ownerId, Status = HallStatus.Approved };
        context.Halls.Add(hall);
        context.HallImages.AddRange(
            new HallImage { HallId = hallId, Url = "a.jpg", DisplayOrder = 0 },
            new HallImage { HallId = hallId, Url = "b.jpg", DisplayOrder = 1, IsDeleted = true });
        context.HallBookingPeriods.AddRange(
            new HallBookingPeriod { HallId = hallId, Type = BookingPeriodType.FirstPeriod, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(15, 0) },
            new HallBookingPeriod { HallId = hallId, Type = BookingPeriodType.SecondPeriod, StartTime = new TimeOnly(16, 0), EndTime = new TimeOnly(23, 0) });
        await context.SaveChangesAsync();

        var repository = new OwnerDashboardRepository(context);

        var result = await repository.GetOwnedHallWithDetailsAsync(hallId, ownerId);

        Assert.NotNull(result);
        Assert.Equal(hallId, result.Id);
        Assert.Equal(2, result.BookingPeriods.Count);
        Assert.Equal(2, result.Images.Count);
    }

    [Fact]
    public async Task GetOwnedHallWithDetails_AnotherOwnersHall_ReturnsNull()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid().ToString();
        var otherId = Guid.NewGuid().ToString();
        var hall = new Hall { Id = Guid.NewGuid(), Name = "Other's Hall", OwnerId = otherId, Status = HallStatus.Approved };
        context.Halls.Add(hall);
        await context.SaveChangesAsync();

        var repository = new OwnerDashboardRepository(context);

        var result = await repository.GetOwnedHallWithDetailsAsync(hall.Id, ownerId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetOwnedHallWithDetails_DeletedHall_ReturnsNull()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid().ToString();
        var hall = new Hall { Id = Guid.NewGuid(), Name = "Deleted Hall", OwnerId = ownerId, Status = HallStatus.Approved, IsDeleted = true };
        context.Halls.Add(hall);
        await context.SaveChangesAsync();

        var repository = new OwnerDashboardRepository(context);

        var result = await repository.GetOwnedHallWithDetailsAsync(hall.Id, ownerId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetOwnedHallForUpdate_ReturnsTrackedAggregateThatPersistsChanges()
    {
        await using var context = CreateContext();
        var ownerId = Guid.NewGuid().ToString();
        var hallId = Guid.NewGuid();
        var hall = new Hall { Id = hallId, Name = "Grand Hall", OwnerId = ownerId, Status = HallStatus.Approved };
        context.Halls.Add(hall);
        context.HallImages.Add(new HallImage { HallId = hallId, Url = "a.jpg", DisplayOrder = 0 });
        context.HallBookingPeriods.Add(new HallBookingPeriod
        {
            HallId = hallId,
            Type = BookingPeriodType.FirstPeriod,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(15, 0)
        });
        await context.SaveChangesAsync();

        var repository = new OwnerDashboardRepository(context);

        var result = await repository.GetOwnedHallForUpdateAsync(hallId, ownerId);
        Assert.NotNull(result);

        result.Name = "Grand Hall Renamed";
        result.Images.Single().IsDeleted = true;
        await context.SaveChangesAsync();

        Assert.Equal("Grand Hall Renamed", context.Halls.AsNoTracking().Single(item => item.Id == hallId).Name);
        Assert.True(context.HallImages.AsNoTracking().Single(image => image.HallId == hallId).IsDeleted);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}