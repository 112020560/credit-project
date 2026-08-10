CREATE TABLE IF NOT EXISTS audit_log (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    user_id     VARCHAR(256),
    action      VARCHAR(128) NOT NULL,
    entity_type VARCHAR(128) NOT NULL,
    entity_id   UUID NOT NULL,
    details     JSONB
);

CREATE INDEX IF NOT EXISTS ix_audit_log_entity ON audit_log (entity_type, entity_id);
CREATE INDEX IF NOT EXISTS ix_audit_log_occurred_at ON audit_log (occurred_at);
