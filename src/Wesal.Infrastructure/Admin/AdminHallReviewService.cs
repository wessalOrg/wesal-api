using Microsoft.Extensions.Logging;
using Wesal.Application.Common.Interfaces;
using Wesal.Application.Common.Interfaces.Persistence;
using Wesal.Application.Common.Models;
using Wesal.Domain.Entities;
using Wesal.Domain.Enums;
using Wesal.Domain.Exceptions;
using Wesal.Infrastructure.Halls;

namespace Wesal.Infrastructure.Admin;

/// <summary>
/// Admin Panel hall-review actions (US-ADMIN-01/03/05): the pending queue, the full
/// hall/owner drill-down, the rejection flow, and the manual Admin lock. Admin role
/// authorization is enforced by the RequireAdmin policy at the controller.
///
/// Rejection only ever applies to a PendingReview hall (or an idempotent no-op on an
/// already Rejected hall); rejecting a live Approved hall requires an explicit
/// confirmation flag and otherwise returns 409 Conflict. The rejection reason is
/// delivered to the owner's Messages inbox via a hall-scoped conversation that stays
/// open so the owner can reply. The manual lock records the acting Admin and timestamp
/// and never touches PaymentStatus or SystemLocked (FR-SUB-05).
/// </summary>
public sealed class AdminHallReviewService : IAdminHallReviewService
{
    private readonly IAdminDashboardRepository _adminDashboardRepository;
    private readonly IHallRepository _hallRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTime _dateTime;
    private readonly ILogger<AdminHallReviewService> _logger;

    public AdminHallReviewService(
        IAdminDashboardRepository adminDashboardRepository,
        IHallRepository hallRepository,
        IUnitOfWork unitOfWork,
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        ICurrentUserService currentUser,
        IDateTime dateTime,
        ILogger<AdminHallReviewService> logger)
    {
        _adminDashboardRepository = adminDashboardRepository;
        _hallRepository = hallRepository;
        _unitOfWork = unitOfWork;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _currentUser = currentUser;
        _dateTime = dateTime;
        _logger = logger;
    }

    public async Task<PagedResult<AdminPendingHallDto>> GetPendingHallsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var items = await _adminDashboardRepository.GetPendingHallsAsync((page - 1) * pageSize, pageSize, cancellationToken);
        var totalCount = await _adminDashboardRepository.GetPendingHallsCountAsync(cancellationToken);

        return PagedResult<AdminPendingHallDto>.Create(items, page, pageSize, totalCount);
    }

    public async Task<AdminHallDetailDto> GetAdminHallDetailAsync(
        Guid hallId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var row = await _adminDashboardRepository.GetHallDetailForAdminAsync(hallId, cancellationToken);

        if (row is null)
        {
            throw new NotFoundException(nameof(Hall), hallId);
        }

        return new AdminHallDetailDto
        {
            HallId = row.HallId,
            Name = row.Name,
            Region = row.Region,
            RegionDisplayName = HallDisplayNames.GetRegionDisplayName(row.Region),
            Address = row.Address,
            Description = row.Description,
            Capacity = row.Capacity,
            Price = row.Price,
            SubmittedAt = row.SubmittedAt,
            Status = row.Status,
            OwnerFullName = row.OwnerFullName,
            OwnerPhoneNumber = row.OwnerPhoneNumber,
            OwnerEmail = row.OwnerEmail,
            PhotoUrls = row.PhotoUrls
        };
    }

    public async Task<AdminRejectHallResultDto> RejectHallAsync(
        Guid hallId,
        AdminRejectHallRequestDto request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var hall = await _hallRepository.GetHallByIdForUpdateAsync(hallId, cancellationToken);

        if (hall is null || hall.IsDeleted)
        {
            throw new NotFoundException(nameof(Hall), hallId);
        }

        if (hall.Status == HallStatus.Rejected)
        {
            return new AdminRejectHallResultDto
            {
                HallId = hall.Id,
                Name = hall.Name,
                Status = hall.Status,
                IsAlreadyRejected = true,
                NotificationDelivered = false
            };
        }

        if (hall.Status == HallStatus.Approved && !request.ConfirmLiveApproved)
        {
            throw new ConflictException(
                "This hall is currently Approved (live). Rejecting it will take the hall offline; send an explicit confirmation to proceed.");
        }

        if (hall.Status == HallStatus.Approved || hall.Status == HallStatus.PendingReview)
        {
            hall.Status = HallStatus.Rejected;
            hall.UpdatedAt = _dateTime.Now;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Hall {HallId} was rejected by admin {AdminId}", hall.Id, ResolveAdminUserId());

            var notificationDelivered = await TryDeliverRejectionMessageAsync(hall, request.Reason, cancellationToken);

            return new AdminRejectHallResultDto
            {
                HallId = hall.Id,
                Name = hall.Name,
                Status = hall.Status,
                IsAlreadyRejected = false,
                NotificationDelivered = notificationDelivered
            };
        }

        throw new BusinessRuleException(
            "HallNotPending",
            $"A hall with status {hall.Status} cannot be rejected.");
    }

    public async Task<AdminLockHallResultDto> LockHallAsync(
        Guid hallId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var hall = await _hallRepository.GetHallByIdForUpdateAsync(hallId, cancellationToken);

        if (hall is null || hall.IsDeleted)
        {
            throw new NotFoundException(nameof(Hall), hallId);
        }

        if (!hall.IsAdminLocked)
        {
            hall.IsAdminLocked = true;
            hall.LockedByAdminUserId = ResolveAdminUserId();
            hall.LockedAt = _dateTime.Now;
            hall.UpdatedAt = _dateTime.Now;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Hall {HallId} was locked by admin {AdminId}", hall.Id, hall.LockedByAdminUserId);
        }

        return new AdminLockHallResultDto
        {
            HallId = hall.Id,
            Name = hall.Name,
            IsLocked = hall.IsAdminLocked,
            LockedAt = hall.LockedAt,
            LockedByAdminUserId = hall.LockedByAdminUserId
        };
    }

    private string ResolveAdminUserId()
        => _currentUser.IsAuthenticated && !string.IsNullOrWhiteSpace(_currentUser.UserId)
            ? _currentUser.UserId
            : "admin";

    private async Task<bool> TryDeliverRejectionMessageAsync(
        Hall hall,
        string? reason,
        CancellationToken cancellationToken)
    {
        try
        {
            await DeliverRejectionMessageAsync(hall, reason, cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deliver the rejection message for hall {HallId}", hall.Id);
            return false;
        }
    }

    private async Task DeliverRejectionMessageAsync(
        Hall hall,
        string? reason,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(hall.OwnerId))
        {
            return;
        }

        var adminUserId = ResolveAdminUserId();

        var conversation = await _conversationRepository.GetByHallAndUserAsync(hall.Id, adminUserId, cancellationToken);

        if (conversation is null)
        {
            conversation = new Conversation
            {
                HallId = hall.Id,
                SenderUserId = adminUserId,
                HallOwnerId = hall.OwnerId!
            };

            await _conversationRepository.AddAsync(conversation, cancellationToken);
        }

        var message = new Message
        {
            ConversationId = conversation.Id,
            SenderUserId = adminUserId,
            Content = BuildRejectionContent(hall, reason)
        };

        await _messageRepository.AddAsync(message, cancellationToken);
        await _messageRepository.SaveChangesAsync(cancellationToken);
    }

    private static string BuildRejectionContent(Hall hall, string? reason)
    {
        var content = $"Your hall \"{hall.Name}\" was rejected by the administrator.";

        if (!string.IsNullOrWhiteSpace(reason))
        {
            content += $" Reason: {reason.Trim()}";
        }

        content += " You can edit your hall and resubmit it for review.";

        return content;
    }
}