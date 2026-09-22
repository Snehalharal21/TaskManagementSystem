using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/tasks/{taskId:guid}/comments")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CommentsController(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<List<CommentDto>>> GetComments(Guid taskId)
    {
        var taskExists = await _context.Tasks.AnyAsync(t => t.Id == taskId);
        if (!taskExists) return NotFound(new { message = "Task not found." });

        var comments = await _context.Comments
            .Include(c => c.User)
            .Where(c => c.TaskId == taskId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto
            {
                Id = c.Id,
                Content = c.Content,
                TaskId = c.TaskId,
                UserId = c.UserId,
                UserName = c.User.FirstName + " " + c.User.LastName,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(comments);
    }

    [HttpPost]
    public async Task<ActionResult<CommentDto>> AddComment(Guid taskId, [FromBody] CreateCommentRequest request)
    {
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null) return NotFound(new { message = "Task not found." });

        var comment = new Comment
        {
            Content = request.Content.Trim(),
            TaskId = taskId,
            UserId = _currentUser.UserId!.Value
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        var user = await _context.Users.FindAsync(_currentUser.UserId);

        return CreatedAtAction(nameof(GetComments), new { taskId }, new CommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            TaskId = comment.TaskId,
            UserId = comment.UserId,
            UserName = user != null ? $"{user.FirstName} {user.LastName}" : "",
            CreatedAt = comment.CreatedAt
        });
    }

    [HttpDelete("{commentId:guid}")]
    public async Task<ActionResult> DeleteComment(Guid taskId, Guid commentId)
    {
        var comment = await _context.Comments
            .FirstOrDefaultAsync(c => c.Id == commentId && c.TaskId == taskId);

        if (comment == null) return NotFound();

        // Only comment owner or Admin can delete
        if (comment.UserId != _currentUser.UserId && _currentUser.Role != Domain.Enums.UserRole.Admin)
            return Forbid();

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}