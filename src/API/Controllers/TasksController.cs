using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailService _emailService;

    public TasksController(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IEmailService emailService)
    {
        _context = context;
        _currentUser = currentUser;
        _emailService = emailService;
    }

    [HttpGet]
    public async Task<ActionResult<List<TaskDto>>> GetTasks([FromQuery] TaskFilterRequest filter)
    {
        var query = _context.Tasks
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .Include(t => t.Team)
            .Include(t => t.Comments)
            .AsQueryable();

        // Role-based filtering
        if (_currentUser.Role == UserRole.User)
        {
            query = query.Where(t => t.AssignedToId == _currentUser.UserId);
        }
        else if (_currentUser.Role == UserRole.Manager)
        {
            // Managers see tasks they created or assigned to their team members
            var managedUserIds = await _context.TeamMembers
                .Where(tm => tm.Team.CreatedById == _currentUser.UserId ||
                             tm.Team.Members.Any(m => m.UserId == _currentUser.UserId))
                .Select(tm => tm.UserId)
                .Distinct()
                .ToListAsync();

            query = query.Where(t => t.CreatedById == _currentUser.UserId ||
                                     (t.AssignedToId != null && managedUserIds.Contains(t.AssignedToId.Value)));
        }

        // Apply filters
        if (filter.Status.HasValue)
            query = query.Where(t => t.Status == filter.Status.Value);
        if (filter.Priority.HasValue)
            query = query.Where(t => t.Priority == filter.Priority.Value);
        if (filter.DeadlineFrom.HasValue)
            query = query.Where(t => t.Deadline >= filter.DeadlineFrom.Value);
        if (filter.DeadlineTo.HasValue)
            query = query.Where(t => t.Deadline <= filter.DeadlineTo.Value);
        if (filter.AssignedToId.HasValue)
            query = query.Where(t => t.AssignedToId == filter.AssignedToId.Value);
        if (filter.TeamId.HasValue)
            query = query.Where(t => t.TeamId == filter.TeamId.Value);

        var tasks = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => MapToDto(t))
            .ToListAsync();

        return Ok(tasks);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TaskDto>> GetTask(Guid id)
    {
        var task = await _context.Tasks
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .Include(t => t.Team)
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null) return NotFound();

        if (!CanAccessTask(task))
            return Forbid();

        return Ok(MapToDto(task));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<TaskDto>> CreateTask([FromBody] CreateTaskRequest request)
    {
        var task = new TaskItem
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Priority = request.Priority,
            Deadline = request.Deadline,
            AssignedToId = request.AssignedToId,
            CreatedById = _currentUser.UserId!.Value,
            TeamId = request.TeamId,
            Status = Domain.Enums.TaskStatus.ToDo
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        // Reload with includes
        task = await _context.Tasks
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .Include(t => t.Team)
            .FirstAsync(t => t.Id == task.Id);

        // Send notification + email if assigned
        if (task.AssignedToId.HasValue && task.AssignedTo != null)
        {
            await CreateNotificationAndEmail(
                task.AssignedToId.Value,
                task.AssignedTo.Email,
                "New Task Assigned",
                $"You have been assigned a new task: <b>{task.Title}</b>",
                task.Id);
        }

        return CreatedAtAction(nameof(GetTask), new { id = task.Id }, MapToDto(task));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TaskDto>> UpdateTask(Guid id, [FromBody] UpdateTaskRequest request)
    {
        var task = await _context.Tasks
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .Include(t => t.Team)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task == null) return NotFound();

        if (!CanModifyTask(task))
            return Forbid();

        var oldStatus = task.Status;
        var oldAssignee = task.AssignedToId;

        if (request.Title != null) task.Title = request.Title.Trim();
        if (request.Description != null) task.Description = request.Description.Trim();
        if (request.Status.HasValue) task.Status = request.Status.Value;
        if (request.Priority.HasValue) task.Priority = request.Priority.Value;
        if (request.Deadline.HasValue) task.Deadline = request.Deadline;
        if (request.AssignedToId.HasValue) task.AssignedToId = request.AssignedToId;

        task.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Reload
        task = await _context.Tasks
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .Include(t => t.Team)
            .Include(t => t.Comments)
            .FirstAsync(t => t.Id == id);

        // Notify on status change
        if (request.Status.HasValue && request.Status.Value != oldStatus && task.AssignedToId.HasValue && task.AssignedTo != null)
        {
            await CreateNotificationAndEmail(
                task.AssignedToId.Value,
                task.AssignedTo.Email,
                "Task Status Updated",
                $"Task <b>{task.Title}</b> status changed to <b>{task.Status}</b>",
                task.Id);
        }

        // Notify on new assignment
        if (request.AssignedToId.HasValue && request.AssignedToId != oldAssignee && task.AssignedTo != null)
        {
            await CreateNotificationAndEmail(
                task.AssignedToId.Value,
                task.AssignedTo.Email,
                "New Task Assigned",
                $"You have been assigned a task: <b>{task.Title}</b>",
                task.Id);
        }

        return Ok(MapToDto(task));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> DeleteTask(Guid id)
    {
        var task = await _context.Tasks.FindAsync(id);
        if (task == null) return NotFound();

        if (_currentUser.Role == UserRole.Manager && task.CreatedById != _currentUser.UserId)
            return Forbid();

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> GetDashboard()
    {
        var query = _context.Tasks.AsQueryable();

        if (_currentUser.Role == UserRole.User)
            query = query.Where(t => t.AssignedToId == _currentUser.UserId);

        var tasks = await query.ToListAsync();

        var dashboard = new DashboardDto
        {
            TotalTasks = tasks.Count,
            ToDoCount = tasks.Count(t => t.Status == Domain.Enums.TaskStatus.ToDo),
            InProgressCount = tasks.Count(t => t.Status == Domain.Enums.TaskStatus.InProgress),
            DoneCount = tasks.Count(t => t.Status == Domain.Enums.TaskStatus.Done),
            OverdueCount = tasks.Count(t => t.Deadline.HasValue && t.Deadline < DateTime.UtcNow && t.Status != Domain.Enums.TaskStatus.Done),
            RecentTasks = await _context.Tasks
                .Include(t => t.AssignedTo)
                .Include(t => t.CreatedBy)
                .Include(t => t.Team)
                .Include(t => t.Comments)
                .Where(t => _currentUser.Role == UserRole.User ? t.AssignedToId == _currentUser.UserId : true)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .Select(t => MapToDto(t))
                .ToListAsync()
        };

        return Ok(dashboard);
    }

    private bool CanAccessTask(TaskItem task)
    {
        if (_currentUser.Role == UserRole.Admin) return true;
        if (_currentUser.Role == UserRole.Manager) return true; // simplified
        return task.AssignedToId == _currentUser.UserId || task.CreatedById == _currentUser.UserId;
    }

    private bool CanModifyTask(TaskItem task)
    {
        if (_currentUser.Role == UserRole.Admin) return true;
        if (_currentUser.Role == UserRole.Manager) return true;
        // Users can only update status of their own tasks
        return task.AssignedToId == _currentUser.UserId;
    }

    private async Task CreateNotificationAndEmail(Guid userId, string email, string title, string message, Guid taskId)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message.Replace("<b>", "").Replace("</b>", ""),
            RelatedEntityType = "Task",
            RelatedEntityId = taskId
        };
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        var htmlBody = $@"
            <h2>{title}</h2>
            <p>{message}</p>
            <p>Please login to the Task Management System for more details.</p>
            <hr>
            <small>This is an automated notification from Team Task Management System.</small>";

        await _emailService.SendEmailAsync(email, title, htmlBody);
    }

    private static TaskDto MapToDto(TaskItem t) => new()
    {
        Id = t.Id,
        Title = t.Title,
        Description = t.Description,
        Status = t.Status.ToString(),
        Priority = t.Priority.ToString(),
        Deadline = t.Deadline,
        AssignedToId = t.AssignedToId,
        AssignedToName = t.AssignedTo != null ? $"{t.AssignedTo.FirstName} {t.AssignedTo.LastName}" : null,
        CreatedById = t.CreatedById,
        CreatedByName = t.CreatedBy != null ? $"{t.CreatedBy.FirstName} {t.CreatedBy.LastName}" : "",
        TeamId = t.TeamId,
        TeamName = t.Team?.Name,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt,
        CommentCount = t.Comments?.Count ?? 0
    };
}