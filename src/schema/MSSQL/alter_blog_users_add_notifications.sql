-- Adds notification preference flags to existing Blog_Users rows.
-- Run this against existing databases; new installs use create_blog_users_tables.sql.

ALTER TABLE Blog_Users
    ADD NotificationFlags INT NOT NULL DEFAULT 3;  -- 3 = NewPosts|Periodic (both enabled)
