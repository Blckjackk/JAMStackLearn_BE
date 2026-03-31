-- Manual DB update script for SQL Server
-- Target: align schema with current backend models/repositories
-- Safe to run multiple times (uses IF checks)

-- 0) Optional: choose DB
-- USE [ListDB];

-- 1) UserIdentities table (for future Google/Facebook login)
IF OBJECT_ID('dbo.UserIdentities', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserIdentities (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserIdentities PRIMARY KEY,
        UserId INT NOT NULL,
        Provider NVARCHAR(50) NOT NULL,
        ProviderUserId NVARCHAR(255) NOT NULL,
        Email NVARCHAR(255) NULL,
        DisplayName NVARCHAR(150) NULL,
        AvatarUrl NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserIdentities_CreatedAt DEFAULT (GETUTCDATE())
    );

    ALTER TABLE dbo.UserIdentities
        ADD CONSTRAINT FK_UserIdentities_Users_UserId
        FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE;

    CREATE UNIQUE INDEX IX_UserIdentities_Provider_ProviderUserId
        ON dbo.UserIdentities (Provider, ProviderUserId);

    CREATE INDEX IX_UserIdentities_UserId
        ON dbo.UserIdentities (UserId);
END

-- 2) Tags table: ensure Name + Color columns exist
IF COL_LENGTH('dbo.Tags', 'Name') IS NULL
BEGIN
    ALTER TABLE dbo.Tags ADD Name NVARCHAR(100) NOT NULL CONSTRAINT DF_Tags_Name DEFAULT ('');
END

IF COL_LENGTH('dbo.Tags', 'Color') IS NULL
BEGIN
    ALTER TABLE dbo.Tags ADD Color NVARCHAR(7) NOT NULL CONSTRAINT DF_Tags_Color DEFAULT ('#3b82f6');
END

-- Optional: migrate data from legacy columns if they exist
IF COL_LENGTH('dbo.Tags', 'TagName') IS NOT NULL
BEGIN
    UPDATE dbo.Tags SET Name = TagName WHERE (Name IS NULL OR Name = '');
END

IF COL_LENGTH('dbo.Tags', 'ColorHex') IS NOT NULL
BEGIN
    UPDATE dbo.Tags SET Color = ColorHex WHERE (Color IS NULL OR Color = '');
END

-- Optional cleanup (manual):
-- ALTER TABLE dbo.Tags DROP COLUMN TagName;
-- ALTER TABLE dbo.Tags DROP COLUMN ColorHex;

-- 3) Tasks table: ensure columns align with model
IF COL_LENGTH('dbo.Tasks', 'Description') IS NULL
BEGIN
    ALTER TABLE dbo.Tasks ADD Description NVARCHAR(2000) NOT NULL CONSTRAINT DF_Tasks_Description DEFAULT ('');
END

IF COL_LENGTH('dbo.Tasks', 'Status') IS NULL
BEGIN
    ALTER TABLE dbo.Tasks ADD Status NVARCHAR(50) NOT NULL CONSTRAINT DF_Tasks_Status DEFAULT ('TODO');
END

IF COL_LENGTH('dbo.Tasks', 'Priority') IS NULL
BEGIN
    ALTER TABLE dbo.Tasks ADD Priority NVARCHAR(50) NOT NULL CONSTRAINT DF_Tasks_Priority DEFAULT ('Medium');
END

IF COL_LENGTH('dbo.Tasks', 'CreatedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Tasks ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Tasks_CreatedAt DEFAULT (GETUTCDATE());
END

IF COL_LENGTH('dbo.Tasks', 'UpdatedAt') IS NULL
BEGIN
    ALTER TABLE dbo.Tasks ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Tasks_UpdatedAt DEFAULT (GETUTCDATE());
END

-- Normalize existing rows
UPDATE dbo.Tasks SET Description = '' WHERE Description IS NULL;
UPDATE dbo.Tasks SET Status = 'TODO' WHERE Status IS NULL OR LTRIM(RTRIM(Status)) = '';
UPDATE dbo.Tasks SET Priority = 'Medium' WHERE Priority IS NULL OR LTRIM(RTRIM(Priority)) = '';
UPDATE dbo.Tasks SET CreatedAt = GETUTCDATE() WHERE CreatedAt IS NULL;
UPDATE dbo.Tasks SET UpdatedAt = GETUTCDATE() WHERE UpdatedAt IS NULL;

-- 4) ProjectUsers: ensure unique constraint exists
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes i
    WHERE i.name = 'UK_ProjectUser' AND i.object_id = OBJECT_ID('dbo.ProjectUsers')
)
BEGIN
    CREATE UNIQUE INDEX UK_ProjectUser ON dbo.ProjectUsers(ProjectId, UserId);
END

-- 5) Optional: Tasks.AssigneeUserId FK (if missing)
IF COL_LENGTH('dbo.Tasks', 'AssigneeUserId') IS NOT NULL
AND NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_Tasks_Users_AssigneeUserId'
)
BEGIN
    ALTER TABLE dbo.Tasks
        ADD CONSTRAINT FK_Tasks_Users_AssigneeUserId
        FOREIGN KEY (AssigneeUserId) REFERENCES dbo.Users(Id) ON DELETE SET NULL;
END

-- Done
