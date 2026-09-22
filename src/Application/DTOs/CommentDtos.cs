using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class CreateCommentRequest
{
    [Required, MaxLength(1000)]
    public string Content { get; set; } = string.Empty;
}

public class CommentDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid TaskId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}