-- Add optional excerpt/summary field to posts
ALTER TABLE blog_posts ADD COLUMN excerpt VARCHAR(500);