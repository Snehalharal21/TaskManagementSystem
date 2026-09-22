using Domain.Entities;
using Domain.Enums;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Data;

public static class SeedData
{
    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        if (context.Users.Any())
            return; // Already seeded

        var admin = new ApplicationUser
        {
            FirstName = "System",
            LastName = "Admin",
            Email = "admin@taskmgmt.com",
            PasswordHash = HashPassword("Admin@123"),
            Role = UserRole.Admin
        };

        var manager = new ApplicationUser
        {
            FirstName = "John",
            LastName = "Manager",
            Email = "manager@taskmgmt.com",
            PasswordHash = HashPassword("Manager@123"),
            Role = UserRole.Manager
        };

        var user1 = new ApplicationUser
        {
            FirstName = "Alice",
            LastName = "Developer",
            Email = "alice@taskmgmt.com",
            PasswordHash = HashPassword("User@123"),
            Role = UserRole.User
        };

        var user2 = new ApplicationUser
        {
            FirstName = "Bob",
            LastName = "Designer",
            Email = "bob@taskmgmt.com",
            PasswordHash = HashPassword("User@123"),
            Role = UserRole.User
        };

        context.Users.AddRange(admin, manager, user1, user2);
        await context.SaveChangesAsync();

        // Create a sample team
        var team = new Team
        {
            Name = "Development Team",
            Description = "Core development team for the product",
            CreatedById = manager.Id
        };
        context.Teams.Add(team);
        await context.SaveChangesAsync();

        context.TeamMembers.AddRange(
            new TeamMember { TeamId = team.Id, UserId = manager.Id },
            new TeamMember { TeamId = team.Id, UserId = user1.Id },
            new TeamMember { TeamId = team.Id, UserId = user2.Id }
        );

        // Sample tasks
        var task1 = new TaskItem
        {
            Title = "Setup project repository",
            Description = "Initialize Git repo and push initial code",
            Status = Domain.Enums.TaskStatus.Done,
            Priority = TaskPriority.High,
            Deadline = DateTime.UtcNow.AddDays(-2),
            AssignedToId = user1.Id,
            CreatedById = manager.Id,
            TeamId = team.Id
        };

        var task2 = new TaskItem
        {
            Title = "Design login page UI",
            Description = "Create responsive login and register pages",
            Status = Domain.Enums.TaskStatus.InProgress,
            Priority = TaskPriority.Medium,
            Deadline = DateTime.UtcNow.AddDays(3),
            AssignedToId = user2.Id,
            CreatedById = manager.Id,
            TeamId = team.Id
        };

        var task3 = new TaskItem
        {
            Title = "Implement JWT authentication",
            Description = "Add JWT based auth with role support",
            Status = Domain.Enums.TaskStatus.ToDo,
            Priority = TaskPriority.Critical,
            Deadline = DateTime.UtcNow.AddDays(5),
            AssignedToId = user1.Id,
            CreatedById = manager.Id,
            TeamId = team.Id
        };

        context.Tasks.AddRange(task1, task2, task3);
        await context.SaveChangesAsync();

        // Sample comment
        context.Comments.Add(new Comment
        {
            Content = "Repo is ready. Please review the README.",
            TaskId = task1.Id,
            UserId = user1.Id
        });

        await context.SaveChangesAsync();
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "TaskMgmtSalt2024"));
        return Convert.ToBase64String(bytes);
    }
}