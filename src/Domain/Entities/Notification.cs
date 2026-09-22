using Domain.Common;

namespace Domain.Entities;

public class Notification : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public bool IsRead { get; set; } = false;
    public string? RelatedEntityType { get; set; } // e.g. "Task"
    public Guid? RelatedEntityId { get; set; }

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}