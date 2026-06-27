-- Stores one-click unsubscribe tokens for user notifications.
-- One active token per user per token type; used_at is set when the token is consumed.

CREATE TABLE IF NOT EXISTS blog_notification_tokens (
    id          SERIAL PRIMARY KEY,
    user_id     INT NOT NULL,
    tenant_id   INT NOT NULL DEFAULT 0,
    token       VARCHAR(50) NOT NULL UNIQUE,
    token_type  INT NOT NULL DEFAULT 0,                         -- maps to enum NotificationTokenType
    created_at  TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    used_at     TIMESTAMP WITH TIME ZONE NULL,                  -- set when user clicks unsubscribe link
    CONSTRAINT fk_notification_tokens_user FOREIGN KEY (user_id) REFERENCES blog_users(id)
);

CREATE INDEX ix_blog_notification_tokens_token ON blog_notification_tokens (token);
CREATE INDEX ix_blog_notification_tokens_user_id ON blog_notification_tokens (user_id, token_type);
