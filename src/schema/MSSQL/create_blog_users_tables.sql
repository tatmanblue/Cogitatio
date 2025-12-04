-- because we might want to keep user tables in a seaparate database
-- the create statements are kept here for easy reference

CREATE TABLE blog_users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    DisplayName NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    AccountState INT NOT NULL DEFAULT 0,    -- maps to enum UserAccountStates
    TenantId INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
);