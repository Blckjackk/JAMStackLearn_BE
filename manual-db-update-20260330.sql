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

END

    -- 1.1) UserIdentities indexes (safe re-run)
    IF OBJECT_ID('dbo.UserIdentities', 'U') IS NOT NULL
    BEGIN
        BEGIN TRY
            CREATE UNIQUE INDEX IX_UserIdentities_Provider_ProviderUserId
                ON dbo.UserIdentities (Provider, ProviderUserId);
        END TRY
        BEGIN CATCH
            IF ERROR_NUMBER() NOT IN (1913, 2714)
                THROW;
        END CATCH

        BEGIN TRY
            CREATE INDEX IX_UserIdentities_UserId
                ON dbo.UserIdentities (UserId);
        END TRY
        BEGIN CATCH
            IF ERROR_NUMBER() NOT IN (1913, 2714)
                THROW;
        END CATCH
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
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Tags') AND name = 'TagName')
BEGIN
    EXEC sp_executesql N'UPDATE dbo.Tags SET Name = TagName WHERE (Name IS NULL OR Name = '''')';
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Tags') AND name = 'ColorHex')
BEGIN
    EXEC sp_executesql N'UPDATE dbo.Tags SET Color = ColorHex WHERE (Color IS NULL OR Color = '''')';
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
IF OBJECT_ID('dbo.ProjectUsers', 'U') IS NOT NULL
BEGIN
    BEGIN TRY
        CREATE UNIQUE INDEX UK_ProjectUser ON dbo.ProjectUsers(ProjectId, UserId);
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() NOT IN (1913, 2714)
            THROW;
    END CATCH
END

-- 4.1) Users: ensure UserCode column + unique indexes
IF COL_LENGTH('dbo.Users', 'UserCode') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD UserCode NVARCHAR(20) NOT NULL CONSTRAINT DF_Users_UserCode DEFAULT ('');
END

-- Backfill empty user codes using Id (USR-000001)
UPDATE dbo.Users
SET UserCode = 'USR-' + RIGHT('000000' + CAST(Id AS VARCHAR(6)), 6)
WHERE (UserCode IS NULL OR LTRIM(RTRIM(UserCode)) = '');

IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL
BEGIN
    BEGIN TRY
        CREATE UNIQUE INDEX UK_Users_Email ON dbo.Users(Email);
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() NOT IN (1913, 2714)
            THROW;
    END CATCH
END

IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL
BEGIN
    BEGIN TRY
        CREATE UNIQUE INDEX UK_Users_UserCode ON dbo.Users(UserCode);
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() NOT IN (1913, 2714)
            THROW;
    END CATCH
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

-- 6) ProjectInvites table
IF OBJECT_ID('dbo.ProjectInvites', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProjectInvites (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProjectInvites PRIMARY KEY,
        ProjectId INT NOT NULL,
        InvitedUserId INT NOT NULL,
        InvitedByUserId INT NOT NULL,
        Role NVARCHAR(50) NOT NULL,
        Status NVARCHAR(20) NOT NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ProjectInvites_CreatedAt DEFAULT (GETUTCDATE()),
        RespondedAt DATETIME2 NULL
    );

    ALTER TABLE dbo.ProjectInvites
        ADD CONSTRAINT FK_ProjectInvites_Projects_ProjectId
        FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(Id) ON DELETE CASCADE;

    ALTER TABLE dbo.ProjectInvites
        ADD CONSTRAINT FK_ProjectInvites_Users_InvitedUserId
        FOREIGN KEY (InvitedUserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE;

    ALTER TABLE dbo.ProjectInvites
        ADD CONSTRAINT FK_ProjectInvites_Users_InvitedByUserId
        FOREIGN KEY (InvitedByUserId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION;

    BEGIN TRY
        CREATE INDEX IX_ProjectInvites_ProjectId ON dbo.ProjectInvites(ProjectId);
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() NOT IN (1913, 2714)
            THROW;
    END CATCH

    BEGIN TRY
        CREATE INDEX IX_ProjectInvites_InvitedUserId ON dbo.ProjectInvites(InvitedUserId);
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() NOT IN (1913, 2714)
            THROW;
    END CATCH

    BEGIN TRY
        CREATE INDEX IX_ProjectInvites_Status ON dbo.ProjectInvites(Status);
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() NOT IN (1913, 2714)
            THROW;
    END CATCH
END

-- Done
