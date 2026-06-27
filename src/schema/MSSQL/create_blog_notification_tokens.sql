-- Stores one-click unsubscribe tokens for user notifications.
-- One active token per user per token type; UsedAt is set when the token is consumed.

CREATE TABLE Blog_NotificationTokens (
    Id          INT PRIMARY KEY IDENTITY(1,1),
    UserId      INT NOT NULL,
    TenantId    INT NOT NULL DEFAULT 0,
    Token       NVARCHAR(50) NOT NULL UNIQUE,
    TokenType   INT NOT NULL DEFAULT 0,             -- maps to enum NotificationTokenType
    CreatedAt   DATETIME2 NOT NULL DEFAULT GETDATE(),
    UsedAt      DATETIME2 NULL,                     -- set when user clicks unsubscribe link
    CONSTRAINT FK_NotificationTokens_User FOREIGN KEY (UserId) REFERENCES Blog_Users(Id)
);

CREATE INDEX [IX_Blog_NotificationTokens_Token] ON [Blog_NotificationTokens] (Token);
CREATE INDEX [IX_Blog_NotificationTokens_UserId] ON [Blog_NotificationTokens] (UserId, TokenType);
