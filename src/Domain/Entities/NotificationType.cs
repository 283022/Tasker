namespace src.Domain.Entities;

public enum NotificationType
{
    TaskAssigned,
    RemovedFromGroup,
    InvitationAccepted,
    AddedToGroup,
    TeamDeleted,
    MemberPromotedToAdmin,
    TeamJoinPolicyChanged,
    TeamIdNameChanged,
    TaskDeleted,
    TaskRenamed,
    TaskReassigned,
    JoinRequestApproved,
    JoinRequestRejected
}