-- For Existing sites
ALTER TABLE Blog_Posts
    ADD TenantId INT NOT NULL CONSTRAINT DF_Blog_Posts_TenantId DEFAULT 0;

CREATE INDEX [IX_Blog_Posts_TenantId] ON [Blog_Posts]
    (TenantId);

ALTER TABLE Blog_Tags
    ADD TenantId INT NOT NULL CONSTRAINT DF_Blog_Tags_TenantId DEFAULT 0;



DROP TABLE IF EXISTS Blog_Settings;
CREATE TABLE Blog_Settings
(
    Id INT IDENTITY (1,1) PRIMARY KEY,
    TenantId INT NOT NULL DEFAULT 0,
    SettingKey NVARCHAR(100) NOT NULL,
    SettingValue NVARCHAR(MAX) NOT NULL,
    UNIQUE (TenantId, SettingKey)
);

