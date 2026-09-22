using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class TaskItem : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Domain.Enums.TaskStatus Status { get; set; } = Domain.Enums.TaskStatus.ToDo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? Deadline { get; set; }
    public Guid? AssignedToId { get; set; }
    public Guid CreatedById { get; set; }
    public Guid? TeamId { get; set; }

    // Navigation
    public ApplicationUser? AssignedTo { get; set; }
    public ApplicationUser CreatedBy { get; set; } = null!;
    public Team? Team { get; set; }
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}