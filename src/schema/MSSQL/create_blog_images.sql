-- Images uploaded for use in blog posts (GitHub issue #5).
-- Files live in the configured image store (Azure Blob or local folder); this table holds
-- the metadata. Posts reference images as /img/{Id}, never by storage path.

CREATE TABLE Blog_Images (
    Id          INT PRIMARY KEY IDENTITY(1,1),
    TenantId    INT NOT NULL DEFAULT 0,
    FileName    NVARCHAR(255) NOT NULL,             -- original file name, for display only
    StoragePath NVARCHAR(500) NOT NULL,             -- path within the image store
    ThumbPath   NVARCHAR(500) NULL,                 -- thumbnail path; NULL for GIFs
    ContentType NVARCHAR(100) NOT NULL,
    Width       INT NOT NULL,
    Height      INT NOT NULL,
    SizeBytes   BIGINT NOT NULL,
    AltText     NVARCHAR(500) NOT NULL,             -- default alt text, required
    CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX [IX_Blog_Images_TenantId] ON [Blog_Images] (TenantId, CreatedDate);
