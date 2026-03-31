# EF Core Migration: Collaborative Projects Implementation

## Overview

This document describes the Entity Framework Core migration strategy for implementing collaborative project functionality. This migration moves the database from a single-owner project model to a many-to-many collaborative model using EF Core Migrations.

## Migration Strategy

Instead of manual SQL scripts, this project uses **Entity Framework Core 9.0** for automated, version-controlled database migrations (similar to Laravel migrations).

**Rationale:**

- Version-controlled schema changes with rollback capability
- Type-safe C# code instead of raw SQL
- Automatic DbContext scaffolding and seeding
- Cross-platform compatibility (SQL Server, PostgreSQL, SQLite)

## Pre-Migration State

### Models (Before)

```csharp
// Old structure: 1 User owns many Projects
public class Project
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int UserId { get; set; }  // ← Single owner (REMOVED)
    public DateTime CreatedAt { get; set; }
    public User User { get; set; }  // ← Single navigation (REMOVED)
    public List<TaskItem> Tasks { get; set; }
}
```

### Database Schema (Before)

```sql
CREATE TABLE Projects (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(MAX) NOT NULL,
    Description NVARCHAR(MAX),
    UserId INT NOT NULL,  -- ← Single owner constraint
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);
```

## Post-Migration State

### Models (After)

```csharp
// New structure: Many Users work on many Projects via ProjectUser junction
public class Project
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<ProjectUser> Members { get; set; } = new();  // ← New many-to-many
    public List<TaskItem> Tasks { get; set; }
}

public class ProjectUser
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; }  // "Owner", "Editor", "Viewer"
    public DateTime JoinedAt { get; set; }

    public Project Project { get; set; }
    public User User { get; set; }
}

public enum ProjectRole
{
    Owner = 0,      // Can manage members, edit project, edit/delete tasks
    Editor = 1,     // Can edit tasks, create tasks, cannot manage members
    Viewer = 2      // Read-only access
}
```

### Database Schema (After)

```sql
-- Projects table: Remove UserId FK
CREATE TABLE Projects (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(MAX) NOT NULL,
    Description NVARCHAR(MAX),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE()
);

-- New junction table: ProjectUsers
CREATE TABLE ProjectUsers (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ProjectId INT NOT NULL,
    UserId INT NOT NULL,
    Role NVARCHAR(50) NOT NULL DEFAULT 'Editor',  -- Owner|Editor|Viewer
    JoinedAt DATETIME2 DEFAULT GETUTCDATE(),

    FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    UNIQUE (ProjectId, UserId)
);

-- Index for performance
CREATE INDEX IX_ProjectUsers_UserId ON ProjectUsers(UserId);
```

## Migration Steps

### Step 1: Install EF Core Packages

```bash
cd api-app
dotnet add package Microsoft.EntityFrameworkCore --version 9.0.1
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.1
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 9.0.1
```

### Step 2: Create ApplicationDbContext

File: `Database/ApplicationDbContext.cs`

- Defines all DbSets (Users, Projects, ProjectUsers, Tasks, etc.)
- Configures many-to-many relationship with HasMany/WithMany
- Configures cascade delete behaviors
- Configures indexes and constraints

### Step 3: Register in Dependency Injection

File: `Program.cs`

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services
    .AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString)
    );

// Auto-apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}
```

### Step 4: Generate Migration

```bash
dotnet ef migrations add AddCollaborativeProjects
```

This creates:

- `Migrations/{timestamp}_AddCollaborativeProjects.cs` - Up/Down migration methods
- `Migrations/ApplicationDbContextModelSnapshot.cs` - Current schema state

### Step 5: Apply Migration to Database

```bash
dotnet ef database update
```

This:

- Adds ProjectUsers table
- Removes UserId Foreign Key from Projects
- Adds UpdatedAt column to Projects
- Creates indexes and constraints

## Migration Contents

### ProjectUsers Table Creation

```csharp
migrationBuilder.CreateTable(
    name: "ProjectUsers",
    columns: table => new
    {
        Id = table.Column<int>(type: "int", nullable: false)
            .Annotation("SqlServer:Identity", "1, 1"),
        ProjectId = table.Column<int>(type: "int", nullable: false),
        UserId = table.Column<int>(type: "int", nullable: false),
        Role = table.Column<string>(type: "nvarchar(50)", nullable: false, defaultValue: "Editor"),
        JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_ProjectUsers", x => x.Id);
        table.ForeignKey(
            name: "FK_ProjectUsers_Projects_ProjectId",
            column: x => x.ProjectId,
            principalTable: "Projects",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
        table.ForeignKey(
            name: "FK_ProjectUsers_Users_UserId",
            column: x => x.UserId,
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
        table.UniqueConstraint(
            name: "UQ_ProjectUsers_ProjectId_UserId",
            columns: new[] { "ProjectId", "UserId" });
    });

migrationBuilder.CreateIndex(
    name: "IX_ProjectUsers_UserId",
    table: "ProjectUsers",
    column: "UserId");
```

### Projects Table Modifications

```csharp
// Remove UserId FK
migrationBuilder.DropForeignKey(
    name: "FK_Projects_Users_UserId",
    table: "Projects");

// Drop UserId column
migrationBuilder.DropColumn(
    name: "UserId",
    table: "Projects");

// Add UpdatedAt column
migrationBuilder.AddColumn<DateTime>(
    name: "UpdatedAt",
    table: "Projects",
    type: "datetime2",
    nullable: false,
    defaultValueSql: "GETUTCDATE()");
```

## API Impact

### Breaking Changes

| Operation          | Old Endpoint          | New Endpoint                              | Change                                              |
| ------------------ | --------------------- | ----------------------------------------- | --------------------------------------------------- |
| Create Project     | POST /api/project     | POST /api/project                         | Remove `userId` from body; extract from auth header |
| Get Projects       | GET /api/project      | GET /api/project                          | Return user's projects via ProjectUsers JOIN        |
| Get Project Detail | GET /api/project/{id} | GET /api/project/{id}                     | Include `members` array with roles                  |
| Add Member         | N/A                   | POST /api/project/{id}/members            | New endpoint - requires Owner role                  |
| Remove Member      | N/A                   | DELETE /api/project/{id}/members/{userId} | New endpoint - requires Owner role                  |
| Update Member Role | N/A                   | PUT /api/project/{id}/members/{userId}    | New endpoint - requires Owner role                  |

### Response Changes

**Before:**

```json
{
  "id": 1,
  "name": "Project Alpha",
  "description": "...",
  "userId": 5,
  "createdAt": "2026-03-29T10:00:00Z"
}
```

**After:**

```json
{
  "id": 1,
  "name": "Project Alpha",
  "description": "...",
  "createdAt": "2026-03-29T10:00:00Z",
  "updatedAt": "2026-03-29T10:00:00Z",
  "userRole": "Owner",
  "members": [
    {
      "userId": 5,
      "username": "john_doe",
      "email": "john@example.com",
      "role": "Owner",
      "joinedAt": "2026-03-29T10:00:00Z"
    },
    {
      "userId": 8,
      "username": "jane_smith",
      "email": "jane@example.com",
      "role": "Editor",
      "joinedAt": "2026-03-30T14:30:00Z"
    }
  ]
}
```

## Rollback Instructions

If issues arise, rollback the migration:

```bash
# List all migrations
dotnet ef migrations list

# Revert to previous migration (removes AddCollaborativeProjects)
dotnet ef database update {previous-migration-name}

# Remove migration file
dotnet ef migrations remove
```

## Migration Verification Checklist

- [ ] EF Core packages installed (dotnet list package)
- [ ] ApplicationDbContext created and registered in Program.cs
- [ ] Migration generated successfully (`dotnet ef migrations add`)
- [ ] Database updated (`dotnet ef database update`)
- [ ] ProjectUsers table visible in SQL Server Management Studio
- [ ] Verify indexes exist: IX_ProjectUsers_UserId, IX_ProjectUsers_ProjectId
- [ ] Verify unique constraint: UQ_ProjectUsers_ProjectId_UserId
- [ ] Verify cascade delete behavior on Projects delete
- [ ] All existing projects migrated to ProjectUsers table (one row per project owner)

## Next Steps After Migration

1. Update ProjectService to use DbContext queries instead of SqlClient
2. Implement ProjectMemberService for member management (add/remove/update role)
3. Create ProjectService.AddMemberAsync(), RemoveMemberAsync(), UpdateMemberRoleAsync()
4. Update ProjectController with new collaborative endpoints
5. Implement authentication middleware to extract userId from bearer token
6. Build frontend UI for member management (invite, remove, change role)

## EF Core Conventions Used

- **Naming:** ProjectUser (convention for many-to-many models)
- **Foreign Keys:** Implicit (EF detects ProjectId references Project)
- **Cascade Behaviors:** OnDelete: Cascade for both FKs (delete project → deletes members)
- **Unique Constraints:** Fluent API configuration (one member per project)
- **Default Values:** SQL defaults for timestamps (GETUTCDATE())
- **Index:** Manual IX_ProjectUsers_UserId for query performance

## Resources

- [EF Core Migrations Documentation](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [EF Core Many-to-Many Relationships](https://learn.microsoft.com/en-us/ef/core/modeling/relationships)
- [EF Core Change Tracking](https://learn.microsoft.com/en-us/ef/core/change-tracking/)
