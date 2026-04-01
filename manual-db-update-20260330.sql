-- Manual DB bootstrap for SQL Server
-- Fresh database: create all tables directly

-- Optional: choose DB
-- USE [ListDB];

CREATE TABLE dbo.Users (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    Username NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    UserCode NVARCHAR(20) NOT NULL,
    Role NVARCHAR(20) NOT NULL CONSTRAINT DF_Users_Role DEFAULT ('Developer'),
    PasswordHash NVARCHAR(200) NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (GETUTCDATE())
);

CREATE UNIQUE INDEX UK_Users_Email ON dbo.Users(Email);
CREATE UNIQUE INDEX UK_Users_UserCode ON dbo.Users(UserCode);

CREATE TABLE dbo.Projects (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Projects PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(1000) NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Projects_CreatedAt DEFAULT (GETUTCDATE()),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Projects_UpdatedAt DEFAULT (GETUTCDATE())
);

CREATE TABLE dbo.Tags (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tags PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Color NVARCHAR(7) NOT NULL CONSTRAINT DF_Tags_Color DEFAULT ('#3b82f6'),
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Tags_CreatedAt DEFAULT (GETUTCDATE())
);

CREATE TABLE dbo.ProjectUsers (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProjectUsers PRIMARY KEY,
    ProjectId INT NOT NULL,
    UserId INT NOT NULL,
    Role NVARCHAR(50) NOT NULL,
    JoinedAt DATETIME2 NOT NULL CONSTRAINT DF_ProjectUsers_JoinedAt DEFAULT (GETUTCDATE())
);

CREATE UNIQUE INDEX UK_ProjectUsers_ProjectId_UserId ON dbo.ProjectUsers(ProjectId, UserId);
ALTER TABLE dbo.ProjectUsers
    ADD CONSTRAINT FK_ProjectUsers_Projects_ProjectId
    FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(Id) ON DELETE CASCADE;
ALTER TABLE dbo.ProjectUsers
    ADD CONSTRAINT FK_ProjectUsers_Users_UserId
    FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE;

CREATE TABLE dbo.Tasks (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tasks PRIMARY KEY,
    ProjectId INT NOT NULL,
    AssigneeUserId INT NULL,
    Title NVARCHAR(255) NOT NULL,
    Description NVARCHAR(2000) NOT NULL CONSTRAINT DF_Tasks_Description DEFAULT (''),
    Content NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Tasks_Content DEFAULT (''),
    Status NVARCHAR(50) NOT NULL CONSTRAINT DF_Tasks_Status DEFAULT ('TODO'),
    Priority NVARCHAR(50) NOT NULL CONSTRAINT DF_Tasks_Priority DEFAULT ('Medium'),
    IsCompleted BIT NOT NULL CONSTRAINT DF_Tasks_IsCompleted DEFAULT (0),
    DueDate DATETIMEOFFSET NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Tasks_CreatedAt DEFAULT (GETUTCDATE()),
    UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Tasks_UpdatedAt DEFAULT (GETUTCDATE())
);

ALTER TABLE dbo.Tasks
    ADD CONSTRAINT FK_Tasks_Projects_ProjectId
    FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(Id) ON DELETE CASCADE;
ALTER TABLE dbo.Tasks
    ADD CONSTRAINT FK_Tasks_Users_AssigneeUserId
    FOREIGN KEY (AssigneeUserId) REFERENCES dbo.Users(Id) ON DELETE SET NULL;

CREATE TABLE dbo.TaskTags (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TaskTags PRIMARY KEY,
    TaskId INT NOT NULL,
    TagId INT NOT NULL
);

CREATE UNIQUE INDEX UK_TaskTags_TaskId_TagId ON dbo.TaskTags(TaskId, TagId);
ALTER TABLE dbo.TaskTags
    ADD CONSTRAINT FK_TaskTags_Tasks_TaskId
    FOREIGN KEY (TaskId) REFERENCES dbo.Tasks(Id) ON DELETE CASCADE;
ALTER TABLE dbo.TaskTags
    ADD CONSTRAINT FK_TaskTags_Tags_TagId
    FOREIGN KEY (TagId) REFERENCES dbo.Tags(Id) ON DELETE CASCADE;

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

CREATE INDEX IX_ProjectInvites_ProjectId ON dbo.ProjectInvites(ProjectId);
CREATE INDEX IX_ProjectInvites_InvitedUserId ON dbo.ProjectInvites(InvitedUserId);
CREATE INDEX IX_ProjectInvites_Status ON dbo.ProjectInvites(Status);
ALTER TABLE dbo.ProjectInvites
    ADD CONSTRAINT FK_ProjectInvites_Projects_ProjectId
    FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(Id) ON DELETE CASCADE;
ALTER TABLE dbo.ProjectInvites
    ADD CONSTRAINT FK_ProjectInvites_Users_InvitedUserId
    FOREIGN KEY (InvitedUserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE;
ALTER TABLE dbo.ProjectInvites
    ADD CONSTRAINT FK_ProjectInvites_Users_InvitedByUserId
    FOREIGN KEY (InvitedByUserId) REFERENCES dbo.Users(Id) ON DELETE NO ACTION;

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

CREATE UNIQUE INDEX IX_UserIdentities_Provider_ProviderUserId
    ON dbo.UserIdentities (Provider, ProviderUserId);
CREATE INDEX IX_UserIdentities_UserId ON dbo.UserIdentities (UserId);
ALTER TABLE dbo.UserIdentities
    ADD CONSTRAINT FK_UserIdentities_Users_UserId
    FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE;

-- Done
