-- Images uploaded for use in blog posts (GitHub issue #5).
-- Files live in the configured image store (Azure Blob or local folder); this table holds
-- the metadata. Posts reference images as /img/{id}, never by storage path.

CREATE TABLE IF NOT EXISTS blog_images (
    id           SERIAL PRIMARY KEY,
    tenant_id    INT NOT NULL DEFAULT 0,
    file_name    VARCHAR(255) NOT NULL,                         -- original file name, for display only
    storage_path VARCHAR(500) NOT NULL,                         -- path within the image store
    thumb_path   VARCHAR(500) NULL,                             -- thumbnail path; NULL for GIFs
    content_type VARCHAR(100) NOT NULL,
    width        INT NOT NULL,
    height       INT NOT NULL,
    size_bytes   BIGINT NOT NULL,
    alt_text     VARCHAR(500) NOT NULL,                         -- default alt text, required
    created_date TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_blog_images_tenant_id ON blog_images (tenant_id, created_date);
