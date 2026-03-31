# Migration Setup & Execution Guide

## Files Created

### 1. Documentation

- ✅ **EF_CORE_MIGRATION.md** - Comprehensive migration documentation

### 2. Database Layer

- ✅ **Database/ApplicationDbContext.cs** - DbContext with all entity configurations

### 3. Migration Files

- ✅ **Migrations/20260329100000_AddCollaborativeProjects.cs** - Migration code (Up/Down)
- ✅ **Migrations/ApplicationDbContextModelSnapshot.cs** - Current schema snapshot

### 4. Dependency Injection

- ✅ **Program.cs** - Updated with DbContext registration and auto-migration runner

### 5. Dependencies

- ✅ **.csproj** - Already has EF Core packages:
  - Microsoft.EntityFrameworkCore 9.0.1
  - Microsoft.EntityFrameworkCore.SqlServer 9.0.1
  - Microsoft.EntityFrameworkCore.Tools 9.0.1

## Next Steps - User Action Required

### Step 1: Restore NuGet Packages

```bash
cd "c:\Computer Science\Semester 6\Learn Jamstack\New Astro\api-app\api-app"
dotnet restore
```

### Step 2: Apply Migration to Database

```bash
# This applies the migration and creates ProjectUsers table
dotnet ef database update
```

### Step 3: Verify in SQL Server Management Studio

1. Open SQL Server Management Studio
2. Connect to: `localhost\SQLEXPRESS`
3. Database: `ListDB`
4. Verify these objects were created:
   - Table: `ProjectUsers` (with columns: Id, ProjectId, UserId, Role, JoinedAt)
   - Index: `IX_ProjectUsers_UserId`
   - Unique Constraint: `UQ_ProjectUsers_ProjectId_UserId`
   - Foreign Keys: `FK_ProjectUsers_Projects_ProjectId`, `FK_ProjectUsers_Users_UserId`

### Step 4: Verify Projects Table Changes

1. Look at `Projects` table structure
2. Confirm `UserId` column is **REMOVED**
3. Confirm `UpdatedAt` column is **ADDED**

## Database Migration Flow

```
Before:
Projects
├── Id (PK)
├── Name
├── Description
├── UserId (FK) ← Single owner
├── CreatedAt

                    ↓ Migration: Add Collaborative Projects

After:
Projects
├── Id (PK)
├── Name
├── Description
├── CreatedAt
└── UpdatedAt ← Added

ProjectUsers (NEW - Junction Table)
├── Id (PK)
├── ProjectId (FK)
├── UserId (FK)
├── Role (Owner|Editor|Viewer)
├── JoinedAt
└── Unique(ProjectId, UserId)
```

## What Changed in Code

### Program.cs Changes

```csharp
// NEW: Added DbContext registration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)
);

// NEW: Auto-apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}
```

### ApplicationDbContext

- Configured all DbSets: Users, Projects, ProjectUsers, Tasks, Tags, TaskTags
- Set up many-to-many relationship: Project ↔ ProjectUser ↔ User
- Configured cascade delete behaviors
- Set up unique constraints and indexes
- Applied SQL default values (GETUTCDATE() for timestamps)

## Troubleshooting

### Migration Already Applied?

If you see error: `The migration '20260329100000_AddCollaborativeProjects' has already been applied to the database`

- Run: `dotnet ef migrations remove` (to remove from tracking)
- Database is already updated, proceed to next steps

### Connection String Issues?

ConnectionString is automatically picked up from:

1. `appsettings.json` → ConnectionString → DefaultConnection
2. Fallback: `Server=localhost\SQLEXPRESS;Database=ListDB;...`

### EF CLI Not Found?

```bash
dotnet tool install --global dotnet-ef
# or update if already installed:
dotnet tool update --global dotnet-ef
```

### Database Lock Issues

If you encounter database locks:

```bash
# List active migrations
dotnet ef migrations list

# See pending migrations
dotnet ef migrations list --connection "Server=localhost\\SQLEXPRESS;Database=ListDB;Trusted_Connection=True;"
```

## Validation Checklist

After running migration, verify:

- [ ] No errors during `dotnet ef database update`
- [ ] ProjectUsers table exists in ListDB
- [ ] Projects table no longer has UserId column
- [ ] Projects table has UpdatedAt column
- [ ] Indexes created: IX_ProjectUsers_UserId
- [ ] Unique constraint created: UQ_ProjectUsers_ProjectId_UserId
- [ ] Foreign keys created with CASCADE delete

## Next Phase - After Migration Success

Once migration is applied:

1. Update ProjectService methods for member management
2. Update ProjectController with new endpoints
3. Create frontend UI for managing project members
4. Implement authentication middleware for userId extraction
5. Test collaborative features end-to-end

See **EF_CORE_MIGRATION.md** for API changes and endpoint documentation.
