using src.Domain;
using src.Domain.Entities;
using src.UnitOfWork;

namespace src.Service;

public class TeamJoinRequestService(
    IUnitOfWork unitOfWork,
    ILogger<TeamJoinRequestService> logger,
    NotificationService notificationService)
{
    public async Task<Result> CreateAsync(Guid teamId, Guid userId)
    {
        logger.LogInformation(
            "User {UserId} is requesting to join team {TeamId}",
            userId, teamId);

        if (userId == Guid.Empty)
            return Result.Failure(
                Errors.Validation("User ID cannot be empty.", "userId"));

        if (teamId == Guid.Empty)
            return Result.Failure(
                Errors.Validation("Team ID cannot be empty.", "teamId"));

        var team = await unitOfWork.TeamRepository.GetTeamByIdAsync(teamId);

        if (team is null)
            return Result.Failure(
                Errors.NotFound("Team", teamId.ToString()));

        var user = await unitOfWork.UserRepository.GetUserByIdAsync(userId);

        if (user is null)
            return Result.Failure(Errors.NotFound("User isn't exist"));

        var isMember = await unitOfWork.TeamRepository
            .IsUserInTeamAsync(userId, teamId);

        if (isMember)
            return Result.Failure(
                Errors.Conflict("User is already a member of this team."));

        // При политике All заявка не нужна:
        // пользователь может вступить напрямую.
        if (team.JoinPolicy == JoinPolicy.All.ToString())
        {
            var memberResult = TeamMember.Create(
                teamId, userId, isAdmin: false);

            if (memberResult.IsFailure)
                return Result.Failure(memberResult.Error);

            await unitOfWork.TeamMemberRepository
                .AddTeamMemberAsync(memberResult.Value);

            await unitOfWork.SaveChangesAsync();

            logger.LogInformation(
                "User {UserId} joined team {TeamId}",
                userId, teamId);

            return Result.Success();
        }

        var existingRequest = await unitOfWork.TeamJoinRequestRepository
            .GetPendingByTeamAndUserAsync(teamId, userId);

        if (existingRequest is not null)
            return Result.Failure(
                Errors.Conflict("A pending join request already exists."));

        var requestResult = TeamJoinRequest.Create(teamId, userId);

        if (requestResult.IsFailure)
            return Result.Failure(requestResult.Error);

        await unitOfWork.TeamJoinRequestRepository
            .AddAsync(requestResult.Value);

        await unitOfWork.SaveChangesAsync();

        logger.LogInformation(
            "Join request created for user {UserId} and team {TeamId}",
            userId, teamId);

        return Result.Success();
    }

    public async Task<Result<List<JoinRequestDto>>> GetAllRequest(
        Guid teamId,
        Guid userId)
    {
        if (teamId == Guid.Empty)
            return Result<List<JoinRequestDto>>.Failure(
                Errors.Validation("Team ID cannot be empty.", "teamId"));

        if (userId == Guid.Empty)
            return Result<List<JoinRequestDto>>.Failure(
                Errors.Validation("User ID cannot be empty.", "userId"));

        var team = await unitOfWork.TeamRepository
            .GetTeamByIdAsync(teamId);

        if (team is null)
            return Result<List<JoinRequestDto>>.Failure(
                Errors.NotFound("Team", teamId.ToString()));

        var isAdmin = await unitOfWork.TeamRepository
            .IsUserAdminOfTeamAsync(userId, teamId);

        if (!isAdmin)
            return Result<List<JoinRequestDto>>.Failure(
                Errors.Forbidden(
                    "Only team administrators can view join requests."));

        var requests = await unitOfWork.TeamJoinRequestRepository
            .GetAllByTeamIdAsync(teamId);

        var result = requests
            .Select(request => new JoinRequestDto
            {
                Id = request.Id,
                TeamId = request.TeamId,
                UserId = request.UserId,
                Status = request.Status,
                CreatedAt = request.CreatedAt,
                ReviewedAt = request.ReviewedAt,
                ReviewedByUserId = request.ReviewedByUserId
            })
            .ToList();

        return Result<List<JoinRequestDto>>.Success(result);
    }
    
    public async Task<Result> ApproveAsync(
    Guid requestId,
    Guid reviewerUserId)
{
    if (requestId == Guid.Empty)
        return Result.Failure(
            Errors.Validation("Request ID cannot be empty.", "requestId"));

    if (reviewerUserId == Guid.Empty)
        return Result.Failure(
            Errors.Validation("User ID cannot be empty.", "userId"));

    var request = await unitOfWork.TeamJoinRequestRepository
        .GetByIdAsync(requestId);

    if (request is null)
        return Result.Failure(
            Errors.NotFound("Join request", requestId.ToString()));

    var team = await unitOfWork.TeamRepository
        .GetTeamByIdAsync(request.TeamId);

    if (team is null)
        return Result.Failure(
            Errors.NotFound("Team", request.TeamId.ToString()));

    var isAdmin = await unitOfWork.TeamRepository
        .IsUserAdminOfTeamAsync(reviewerUserId, request.TeamId);

    if (!isAdmin)
        return Result.Failure(
            Errors.Forbidden(
                "Only team administrators can approve join requests."));

    var isMember = await unitOfWork.TeamRepository
        .IsUserInTeamAsync(request.UserId, request.TeamId);

    if (isMember)
        return Result.Failure(
            Errors.Conflict("User is already a member of this team."));

    var memberResult = TeamMember.Create(
        request.TeamId,
        request.UserId,
        isAdmin: false);

    if (memberResult.IsFailure)
        return Result.Failure(memberResult.Error);

    try
    {
        var approveResult = request.Approve(reviewerUserId);

        if (approveResult.IsFailure)
            return Result.Failure(approveResult.Error);

        await unitOfWork.TeamMemberRepository
            .AddTeamMemberAsync(memberResult.Value);

        var notificationResult =
            await notificationService.NotifyJoinRequestApprovedAsync(
                request.UserId,
                team.Id,
                team.Name);

        if (notificationResult.IsFailure)
            return Result.Failure(notificationResult.Error);

        // Сохраняем заявку, членство и уведомление вместе.
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation(
            "Join request {RequestId} approved by user {ReviewerUserId}. " +
            "User {JoinedUserId} joined team {TeamId}",
            requestId,
            reviewerUserId,
            request.UserId,
            request.TeamId);

        return Result.Success();
    }
    catch (Exception ex)
    {
        logger.LogError(
            ex,
            "Failed to approve join request {RequestId} by user {ReviewerUserId}",
            requestId,
            reviewerUserId);

        return Result.Failure(
            Errors.Internal("An error occurred while approving the join request."));
    }
}

    public async Task<Result> RejectAsync(
    Guid requestId,
    Guid reviewerUserId)
{
    if (requestId == Guid.Empty)
        return Result.Failure(
            Errors.Validation("Request ID cannot be empty.", "requestId"));

    if (reviewerUserId == Guid.Empty)
        return Result.Failure(
            Errors.Validation("User ID cannot be empty.", "userId"));

    var request = await unitOfWork.TeamJoinRequestRepository
        .GetByIdAsync(requestId);

    if (request is null)
        return Result.Failure(
            Errors.NotFound("Join request", requestId.ToString()));

    var team = await unitOfWork.TeamRepository
        .GetTeamByIdAsync(request.TeamId);

    if (team is null)
        return Result.Failure(
            Errors.NotFound("Team", request.TeamId.ToString()));

    var isAdmin = await unitOfWork.TeamRepository
        .IsUserAdminOfTeamAsync(reviewerUserId, request.TeamId);

    if (!isAdmin)
        return Result.Failure(
            Errors.Forbidden(
                "Only team administrators can reject join requests."));

    try
    {
        var rejectResult = request.Reject(reviewerUserId);

        if (rejectResult.IsFailure)
            return Result.Failure(rejectResult.Error);

        var notificationResult =
            await notificationService.NotifyJoinRequestRejectedAsync(
                request.UserId,
                team.Id,
                team.Name);

        if (notificationResult.IsFailure)
            return Result.Failure(notificationResult.Error);

        await unitOfWork.SaveChangesAsync();

        logger.LogInformation(
            "Join request {RequestId} rejected by user {ReviewerUserId}",
            requestId,
            reviewerUserId);

        return Result.Success();
    }
    catch (Exception ex)
    {
        logger.LogError(
            ex,
            "Failed to reject join request {RequestId} by user {ReviewerUserId}",
            requestId,
            reviewerUserId);

        return Result.Failure(
            Errors.Internal("An error occurred while rejecting the join request."));
    }
}
}