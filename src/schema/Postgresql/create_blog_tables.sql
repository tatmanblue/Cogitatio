-- Table to store blog posts
CREATE TABLE blog_posts (
    post_id SERIAL PRIMARY KEY,           -- Replaces INT IDENTITY(1,1)
    slug VARCHAR(255) NOT NULL,          -- NVARCHAR(255) -> VARCHAR(255)
    title VARCHAR(255) NOT NULL,         -- NVARCHAR(255) -> VARCHAR(255)
    author VARCHAR(100) NOT NULL,        -- NVARCHAR(100) -> VARCHAR(100)
    published_date TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, -- DATETIME -> TIMESTAMP
    content TEXT NOT NULL,               -- NVARCHAR(MAX) -> TEXT
    status INT NOT NULL DEFAULT 0,
    tenant_id INT NOT NULL DEFAULT 0
);

CREATE INDEX ix_blog_posts_post_id ON blog_posts (post_id);
CREATE INDEX ix_blog_posts_tenant_id ON blog_posts (tenant_id);
CREATE INDEX ix_blog_posts_slug ON blog_posts (slug);

-- Table to store blog tags
CREATE TABLE blog_tags (
   tag_id SERIAL PRIMARY KEY,           -- Replaces INT IDENTITY(1,1)
   post_id INT NOT NULL,
   tenant_id INT NOT NULL DEFAULT 0,
   tag VARCHAR(100) NOT NULL,          -- NVARCHAR(100) -> VARCHAR(100)
   CONSTRAINT fk_post_id FOREIGN KEY (post_id) REFERENCES blog_posts (post_id) ON DELETE CASCADE
);

CREATE INDEX ix_blog_tags_tag_id ON blog_tags (tag_id);
CREATE INDEX ix_blog_tags_tag ON blog_tags (tag);

-- Table to store contact requests
CREATE TABLE blog_request_contact (
      id SERIAL PRIMARY KEY,               -- Replaces INT IDENTITY(1,1)
      name VARCHAR(75) NOT NULL,
      email VARCHAR(75) NOT NULL,
      slug VARCHAR(255),                  -- NVARCHAR(255) -> VARCHAR(255)
      message VARCHAR(100),
      request_date TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP -- DATETIME -> TIMESTAMP
);

-- Table to store blog settings
CREATE TABLE blog_settings (
       id SERIAL PRIMARY KEY,               -- Replaces INT IDENTITY(1,1)
       tenant_id INT NOT NULL DEFAULT 0,
       setting_key VARCHAR(100) NOT NULL,   -- NVARCHAR(100) -> VARCHAR(100)
       setting_value TEXT NOT NULL,         -- NVARCHAR(MAX) -> TEXT
       CONSTRAINT unique_tenant_setting UNIQUE (tenant_id, setting_key)
);

-- Table to store comments associated with blog posts
CREATE TABLE Blog_Comments (
       comment_id SERIAL PRIMARY KEY,
       post_id INT NOT NULL,                     -- Foreign key to BlogPosts table
       user_id INT NOT NULL,
       text VARCHAR(256) NOT NULL,             -- Comment text
       tenant_id INT NOT NULL DEFAULT 0,
       posted_date TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, -- Comment posted date
       CONSTRAINT unique_comment_per_post UNIQUE (tenant_id, comment_id, author)
);
