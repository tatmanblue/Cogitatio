-- Adds notification preference flags to existing blog_users rows.
-- Run this against existing databases; new installs use create_blog_user_tables.sql.

ALTER TABLE blog_users
    ADD COLUMN notification_flags INT NOT NULL DEFAULT 3;  -- 3 = NewPosts|Periodic (both enabled)
