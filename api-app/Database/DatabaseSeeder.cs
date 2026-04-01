using api_app.Models;
using BCrypt.Net;

namespace api_app.Database;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        if (db.Users.Any())
        {
            return;
        }

        var now = DateTime.UtcNow;

        var user1 = new User
        {
            Username = "    admin",
            Email = "admin@mail.com",
            UserCode = "USR-ADMIN",
            Role = "Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            CreatedAt = now
        };

        var user2 = new User
        {
            Username = "member",
            Email = "member@mail.com",
            UserCode = "USR-MEMBER",
            Role = "Developer",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("member123"),
            CreatedAt = now
        };

        db.Users.AddRange(user1, user2);
        await db.SaveChangesAsync(cancellationToken);

        var project = new Project
        {
            Name = "Jamstack Sprint",
            Description = "Initial collaborative project",
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken);

        db.ProjectUsers.AddRange(
            new ProjectUser
            {
                ProjectId = project.Id,
                UserId = user1.Id,
                Role = TeamRoles.ProjectManager,
                JoinedAt = now
            },
            new ProjectUser
            {
                ProjectId = project.Id,
                UserId = user2.Id,
                Role = TeamRoles.Backend,
                JoinedAt = now
            }
        );

        var tag1 = new Tag { Name = "backend", Color = "#2563eb", CreatedAt = now };
        var tag2 = new Tag { Name = "urgent", Color = "#dc2626", CreatedAt = now };

        db.Tags.AddRange(tag1, tag2);
        await db.SaveChangesAsync(cancellationToken);

        var task = new TaskItem
        {
            ProjectId = project.Id,
            AssigneeUserId = user2.Id,
            Title = "Setup API migration",
            Description = "Prepare collaborative schema and test endpoints",
            Content = "Run migration and verify project member data",
            Status = "IN_PROGRESS",
            Priority = "High",
            IsCompleted = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Tasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);

        db.TaskTags.AddRange(
            new TaskTag { TaskId = task.Id, TagId = tag1.Id },
            new TaskTag { TaskId = task.Id, TagId = tag2.Id }
        );

        await db.SaveChangesAsync(cancellationToken);
    }
}
