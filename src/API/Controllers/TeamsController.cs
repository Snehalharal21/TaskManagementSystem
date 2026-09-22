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
public class TeamsController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public TeamsController(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<List<TeamDto>>> GetTeams()
    {
        var query = _context.Teams
            .Include(t => t.CreatedBy)
            .Include(t => t.Members).ThenInclude(m => m.User)
            .AsQueryable();

        // Users only see teams they belong to
        if (_currentUser.Role == UserRole.User)
        {
            query = query.Where(t => t.Members.Any(m => m.UserId == _currentUser.UserId));
        }

        var teams = await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TeamDto
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                CreatedById = t.CreatedById,
                CreatedByName = t.CreatedBy.FirstName + " " + t.CreatedBy.LastName,
                CreatedAt = t.CreatedAt,
                Members = t.Members.Select(m => new TeamMemberDto
                {
                    UserId = m.UserId,
                    FullName = m.User.FirstName + " " + m.User.LastName,
                    Email = m.User.Email,
                    Role = m.User.Role.ToString(),
                    JoinedAt = m.JoinedAt
                }).ToList()
            })
            .ToListAsync();

        return Ok(teams);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TeamDto>> GetTeam(Guid id)
    {
        var team = await _context.Teams
            .Include(t => t.CreatedBy)
            .Include(t => t.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (team == null) return NotFound();

        if (_currentUser.Role == UserRole.User &&
            !team.Members.Any(m => m.UserId == _currentUser.UserId))
            return Forbid();

        return Ok(new TeamDto
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            CreatedById = team.CreatedById,
            CreatedByName = $"{team.CreatedBy.FirstName} {team.CreatedBy.LastName}",
            CreatedAt = team.CreatedAt,
            Members = team.Members.Select(m => new TeamMemberDto
            {
                UserId = m.UserId,
                FullName = $"{m.User.FirstName} {m.User.LastName}",
                Email = m.User.Email,
                Role = m.User.Role.ToString(),
                JoinedAt = m.JoinedAt
            }).ToList()
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult<TeamDto>> CreateTeam([FromBody] CreateTeamRequest request)
    {
        var team = new Team
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CreatedById = _currentUser.UserId!.Value
        };

        _context.Teams.Add(team);

        // Add creator as member
        _context.TeamMembers.Add(new TeamMember
        {
            TeamId = team.Id,
            UserId = _currentUser.UserId.Value
        });

        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTeam), new { id = team.Id }, new TeamDto
        {
            Id = team.Id,
            Name = team.Name,
            Description = team.Description,
            CreatedById = team.CreatedById,
            CreatedByName = "",
            CreatedAt = team.CreatedAt
        });
    }

    [HttpPost("{id:guid}/members")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> AddMember(Guid id, [FromBody] AddTeamMemberRequest request)
    {
        var team = await _context.Teams.FindAsync(id);
        if (team == null) return NotFound(new { message = "Team not found." });

        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null || !user.IsActive)
            return BadRequest(new { message = "User not found or inactive." });

        var exists = await _context.TeamMembers
            .AnyAsync(tm => tm.TeamId == id && tm.UserId == request.UserId);

        if (exists)
            return BadRequest(new { message = "User is already a member of this team." });

        _context.TeamMembers.Add(new TeamMember
        {
            TeamId = id,
            UserId = request.UserId
        });

        await _context.SaveChangesAsync();
        return Ok(new { message = "Member added successfully." });
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<ActionResult> RemoveMember(Guid id, Guid userId)
    {
        var member = await _context.TeamMembers
            .FirstOrDefaultAsync(tm => tm.TeamId == id && tm.UserId == userId);

        if (member == null) return NotFound();

        _context.TeamMembers.Remove(member);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Member removed successfully." });
    }
}