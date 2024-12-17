
-- Table to store blog posts
CREATE TABLE Blog_Posts (
    PostId INT IDENTITY(1,1) PRIMARY KEY, -- Auto-incrementing ID
    Slug NVARCHAR(255) NOT NULL,          -- SEO optimized Url
    Title NVARCHAR(255) NOT NULL,         -- Title of the blog post
    Author NVARCHAR(100) NOT NULL,        -- Author name
    PublishedDate DATETIME NOT NULL DEFAULT GETDATE(), -- Publish date
    Content NVARCHAR(MAX) NOT NULL,       -- Content of the blog post
    Status INT NOT NULL DEFAULT 0,
);

CREATE INDEX [IX_Blog_Posts_PostId] ON [Blog_Posts]
    (PostId);

CREATE INDEX [IX_Blog_Posts_Slug] ON [Blog_Posts]
    (Slug);

CREATE TABLE Blog_Tags (
    TagId INT IDENTITY(1,1) PRIMARY KEY, -- Auto-incrementing ID
    PostId INT NOT NULL,                     -- Foreign key to BlogPosts table
    Tag NVARCHAR(100) NOT NULL,              -- Single Tag
    FOREIGN KEY (PostId) REFERENCES Blog_Posts(PostId) -- Foreign key constraint
);

CREATE INDEX [IX_Blog_Tags_TagId] ON [Blog_Tags]
    (TagId);

CREATE NONCLUSTERED INDEX [IX_Blog_Tags_Tag] ON [Blog_Tags]
    (Tag);
	
CREATE TABLE Blog_Request_Contact
(
    Id INT IDENTITY (1,1) PRIMARY KEY,
    Name VARCHAR(75) NOT NULL,
    Email VARCHAR(75) NOT NULL,
    Slug NVARCHAR(255),
    Message VARCHAR(100),
    RequestDate DATETIME NOT NULL DEFAULT GETDATE()
);	

/*
Since comments are not supported right now, this table is not required
-- Table to store comments associated with blog posts
CREATE TABLE Blog_Comments (
    CommentId INT IDENTITY(1,1) PRIMARY KEY, -- Auto-incrementing ID
    PostId INT NOT NULL,                     -- Foreign key to BlogPosts table
    Author NVARCHAR(100) NOT NULL,           -- Comment author's name
    Text NVARCHAR(MAX) NOT NULL,             -- Comment text
    PostedDate DATETIME NOT NULL DEFAULT GETDATE(), -- Comment posted date
    FOREIGN KEY (PostId) REFERENCES Blog_Posts(PostId) -- Foreign key constraint
);
*/