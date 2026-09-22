using Domain.Common;

namespace Domain.Entities;

public class Team : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CreatedById { get; set; }

    // Navigation
    public ApplicationUser CreatedBy { get; set; } = null!;
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}