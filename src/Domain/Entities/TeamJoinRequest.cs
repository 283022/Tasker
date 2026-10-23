namespace src.Domain.Entities;

public class TeamJoinRequest
{
    public Guid Id { get; private set; }
    public Guid TeamId { get; private set; }
    public Guid UserId { get; private set; }
    public string Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public Guid? ReviewedByUserId { get; private set; }
    public Team Team { get; private set; } = null!;
    public User User { get; private set; } = null!;
    public User? ReviewedByUser { get; private set; }
    
    //for ef core
    private TeamJoinRequest()
    {
    }

    private TeamJoinRequest(Guid teamId, Guid userId)
    {
        TeamId = teamId;
        UserId = userId;
        Status = JoinRequestStatus.Pending.ToString();
        CreatedAt = DateTime.UtcNow;
    }
    public static Result<TeamJoinRequest> Create(Guid teamId, Guid userId)
    {
        if (teamId == Guid.Empty)
            return new Error("Validation", "Team ID cannot be empty");

        if (userId == Guid.Empty)
            return new Error("Validation", "User ID cannot be empty");

        return new TeamJoinRequest(teamId, userId);
    }

    public Result<bool> Approve(Guid reviewedByUserId)
    {
        if (reviewedByUserId == Guid.Empty)
            return new Error("Validation", "Reviewer ID cannot be empty");

        if (Status != JoinRequestStatus.Pending.ToString())
            return new Error("Validation", "Join request has already been reviewed");

        Status = JoinRequestStatus.Approved.ToString();
        ReviewedAt = DateTime.UtcNow;
        ReviewedByUserId = reviewedByUserId;

        return true;
    }

    public Result<bool> Reject(Guid reviewedByUserId)
    {
        if (reviewedByUserId == Guid.Empty)
            return new Error("Validation", "Reviewer ID cannot be empty");

        if (Status != JoinRequestStatus.Pending.ToString())
            return new Error("Validation", "Join request has already been reviewed");

        Status = JoinRequestStatus.Rejected.ToString();
        ReviewedAt = DateTime.UtcNow;
        ReviewedByUserId = reviewedByUserId;

        return true;
    }
}

public enum JoinRequestStatus
{
    Pending,
    Approved,
    Rejected
}