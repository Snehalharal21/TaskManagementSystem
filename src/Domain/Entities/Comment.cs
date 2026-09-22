using Domain.Common;

namespace Domain.Entities;

public class Comment : BaseEntity
{
    public string Content { get; set; } = string.Empty;
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }

    // Navigation
    public TaskItem Task { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}