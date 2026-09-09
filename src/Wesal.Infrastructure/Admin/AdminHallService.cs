using Microsoft.Extensions.Logging;
using Wesal.Application.Common.Interfaces;
using Wesal.Application.Common.Interfaces.Persistence;
using Wesal.Application.Common.Models;
using Wesal.Domain.Enums;
using Wesal.Domain.Exceptions;

namespace Wesal.Infrastructure.Admin;

public class AdminHallService : IAdminHallService
{
    private readonly IHallRepository _hallRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHallSearchIndexer _indexer;
    private readonly ILogger<AdminHallService> _logger;

    public AdminHallService(IHallRepository hallRepository, IUnitOfWork unitOfWork, IHallSearchIndexer indexer, ILogger<AdminHallService> logger)
    {
        _hallRepository = hallRepository;
        _unitOfWork = unitOfWork;
        _indexer = indexer;
        _logger = logger;
    }

    public async Task<HallApprovalResponse> ApproveHallAsync(Guid hallId, CancellationToken cancellationToken = default)
    {
        var hall = await _hallRepository.GetHallByIdForUpdateAsync(hallId, cancellationToken);
        if (hall == null || hall.IsDeleted)
            throw new NotFoundException("Hall", hallId);

        if (hall.Status == HallStatus.Approved)
        {
            // Idempotent - already approved, ensure indexed
            await TryIndexAsync(hall, cancellationToken);
            return new HallApprovalResponse { HallId = hall.Id, HallName = hall.Name, Status = hall.Status, ApprovedAt = hall.UpdatedAt ?? hall.CreatedAt };
        }

        if (hall.Status != HallStatus.PendingReview)
            throw new BusinessRuleException("HallNotPending", $"Hall with status {hall.Status} cannot be approved.");

        hall.Status = HallStatus.Approved;
        hall.UpdatedAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Hall {HallId} approved, triggering search indexing", hallId);

        // Trigger indexing after successful commit - never rollback approval on indexing failure
        await TryIndexAsync(hall, cancellationToken);

        return new HallApprovalResponse { HallId = hall.Id, HallName = hall.Name, Status = hall.Status, ApprovedAt = hall.UpdatedAt ?? DateTimeOffset.UtcNow };
    }

    private async Task TryIndexAsync(Wesal.Domain.Entities.Hall hall, CancellationToken cancellationToken)
    {
        try
        {
            var dto = new HallSearchIndexDto
            {
                HallId = hall.Id,
                HallName = hall.Name,
                Region = hall.Region,
                Address = hall.Address,
                Description = hall.Description,
                Capacity = hall.Capacity,
                Price = hall.Price,
                Status = hall.Status
            };
            await _indexer.IndexHallAsync(dto, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Search indexing failed for hall {HallId}, will retry", hall.Id);
            // Do not throw - approval remains Approved, indexing is retryable
        }
    }
}
