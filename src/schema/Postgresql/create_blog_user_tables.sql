-- because we might want to keep user tables in a seaparate database
-- the create statements are kept here for easy reference

CREATE TABLE IF NOT EXISTS blog_users (
    user_id SERIAL PRIMARY KEY,
    display_name VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    account_state INT NOT NULL DEFAULT 0,    -- maps to enum UserAccountStates
    tenant_id INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX ix_blog_users_id ON blog_posts (user_id);
