-- Add optional excerpt/summary field to posts
ALTER TABLE blog_posts ADD COLUMN excerpt VARCHAR(500);

-- Support fast recent-posts lookups on the home page and date-range search
CREATE INDEX ix_blog_posts_published_date ON blog_posts (published_date);
CREATE INDEX ix_blog_posts_tenant_id_published_date ON blog_posts (tenant_id, published_date);