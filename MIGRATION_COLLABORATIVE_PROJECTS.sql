-- SQL Migration: Implement Collaborative Project System
-- This migration updates the database to support multiple users per project

-- Step 1: Create the ProjectUsers junction table
CREATE TABLE ProjectUsers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProjectId INT NOT NULL,
    UserId INT NOT NULL,
    Role NVARCHAR(50) NOT NULL DEFAULT 'Editor', -- Owner, Editor, Viewer
    JoinedAt DATETIME NOT NULL DEFAULT GETUTCDATE(),
    
    FOREIGN KEY (ProjectId) REFERENCES Projects(Id) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT UK_ProjectUser UNIQUE (ProjectId, UserId)
);

-- Step 2: Add UpdatedAt column to Projects if not exists
-- ALTER TABLE Projects ADD UpdatedAt DATETIME NOT NULL DEFAULT GETUTCDATE();

-- Step 3: Migrate existing data - create ProjectUser records for existing project owners
-- ONLY RUN THIS IF YOU HAVE EXISTING DATA
-- INSERT INTO ProjectUsers (ProjectId, UserId, Role, JoinedAt)
-- SELECT Id, UserId, 'Owner', CreatedAt
-- FROM Projects
-- WHERE UserId IS NOT NULL
-- ON CONFLICT DO NOTHING; -- Skip if record already exists

-- Step 4: After migration is complete, drop the UserId column from Projects
-- ALTER TABLE Projects DROP CONSTRAINT FK_Projects_UserId;
-- ALTER TABLE Projects DROP COLUMN UserId;

-- Step 5: Create indexes for better query performance
CREATE INDEX IX_ProjectUsers_ProjectId ON ProjectUsers(ProjectId);
CREATE INDEX IX_ProjectUsers_UserId ON ProjectUsers(UserId);
CREATE INDEX IX_ProjectUsers_Role ON ProjectUsers(Role);
