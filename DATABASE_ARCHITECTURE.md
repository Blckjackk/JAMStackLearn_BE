# Database Architecture - Collaborative Projects

## Perubahan dari Individual ke Collaborative

### Struktur Lama (Individual)

- 1 User → Many Projects (1-to-many)
- 1 Project → Many Tasks (1-to-many)
- Project.UserId menyimpan user pemilik

### Struktur Baru (Collaborative)

- 1 User ←→ Many Projects (many-to-many via ProjectUser)
- 1 Project ←→ Many Users (many-to-many via ProjectUser)
- 1 Project → Many Tasks (tetap 1-to-many)
- ProjectUser junction table menyimpan role/permission

## Tabel Database

### Users (unchanged)

```
Users
├─ Id (PK)
├─ Username
├─ Email
└─ PasswordHash
```

### Projects (modified)

```
Projects (UBAH: hapus UserId)
├─ Id (PK)
├─ Name
├─ Description
├─ CreatedAt
└─ UpdatedAt (baru)
```

### ProjectUsers (NEW - Junction Table)

```
ProjectUsers (BARU)
├─ Id (PK)
├─ ProjectId (FK)
├─ UserId (FK)
├─ Role (enum: Owner, Editor, Viewer)
├─ JoinedAt
└─ Unique(ProjectId, UserId)
```

### Tasks (unchanged)

```
TaskItems
├─ Id (PK)
├─ ProjectId (FK)
├─ Title
├─ Content
├─ IsCompleted
├─ DueDate
└─ (tetap sama, tidak ada perubahan)
```

### Tags (unchanged)

```
Tags (tetap sama)
```

## Migrasi Data

Untuk data yang sudah ada:

```sql
-- 1. Tambah tabel ProjectUsers
-- 2. Copy existing project ownership ke ProjectUsers (dengan role = 'Owner')
-- 3. Hapus column UserId dari Projects
```

## Role Permissions

| Role   | Can Edit Project | Can Add/Remove Members | Can Edit Tasks | Can Delete Project |
| ------ | ---------------- | ---------------------- | -------------- | ------------------ |
| Owner  | Yes              | Yes                    | Yes            | Yes                |
| Editor | Yes              | No                     | Yes            | No                 |
| Viewer | No               | No                     | No             | No                 |

## API Changes

### Old Endpoints (DEPRECATED)

- `POST /api/project` with UserId → tidak perlu UserId di body lagi, ambil dari auth user
- `GET /api/project/user/{userId}` → ganti dengan user context dari auth

### New Endpoints

- `GET /api/project` → list project yang authenticated user join
- `GET /api/project/{id}` → detail project + members
- `POST /api/project` → create project (user jadi owner otomatis)
- `POST /api/project/{id}/members` → add member (owner only)
- `DELETE /api/project/{id}/members/{userId}` → remove member (owner only)
- `PUT /api/project/{id}/members/{userId}` → update role (owner only)
- `DELETE /api/project/{id}` → delete project (owner only)

## DTO Changes

### ProjectResponseDto (Updated)

```csharp
// Old
public int UserId { get; set; }

// New
public List<ProjectMemberDto> Members { get; set; }
public string UserRole { get; set; } // Role user yang login dalam project ini
```

### ProjectMemberDto (New)

```csharp
public class ProjectMemberDto
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Role { get; set; } // "Owner", "Editor", "Viewer"
    public DateTime JoinedAt { get; set; }
}
```

### AddProjectMemberDto (New)

```csharp
public class AddProjectMemberDto
{
    public int UserId { get; set; }
    public string Role { get; set; } // "Editor" atau "Viewer"
}
```

### UpdateProjectMemberRoleDto (New)

```csharp
public class UpdateProjectMemberRoleDto
{
    public string Role { get; set; } // "Editor" atau "Viewer"
}
```

### CreateProjectDto (Updated)

```csharp
// Old
[Required]
public int UserId { get; set; }

// New (UserId dihapus, ambil dari auth context)
[Required]
[MaxLength(120)]
public string Name { get; set; }
```

## Task Ownership

Tasks tetap belonging to Project saja, bukan ke specific user.

- Setiap task punya ProjectId
- Task bisa dikerjakan oleh siapa saja yang ada di project dan punya role Editor/Owner
- Optional: bisa add "AssignedToUserId" jika mau track siapa yang ngerjain

---

## Implementation Status

### ✅ Completed

- Models: ProjectUser, Project (updated)
- DTOs: ProjectResponseDto, CreateProjectDto, ProjectMemberDto, AddProjectMemberDto, UpdateProjectMemberRoleDto
- Repository Interface: IProjectUserRepository
- Repository Implementation: ProjectUserRepository (fully implemented)
- Repository Updated: ProjectRepository (queries now use many-to-many)
- Dependency Injection registered in Program.cs
- SQL Migration script created

### ⏳ Next Steps

- [ ] Update ProjectService: add member management methods
- [ ] Update ProjectController: add new collaborative endpoints
- [ ] Add authentication middleware to extract userId from request
- [ ] Update frontend Astro app for collaborative features
- [ ] Create UI for:
  - List project members
  - Add/remove project members
  - Change member roles
  - Share project link with collaborators

---

**Database Migration**: See MIGRATION_COLLABORATIVE_PROJECTS.sql
