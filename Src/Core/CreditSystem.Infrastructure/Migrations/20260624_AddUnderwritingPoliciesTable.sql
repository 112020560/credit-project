-- Migration: Add underwriting_policies table
-- Created: 2026-06-24
-- Idempotent: yes

CREATE TABLE IF NOT EXISTS underwriting_policies (
    id                          VARCHAR PRIMARY KEY,
    base_interest_rate          DECIMAL(5,2) NOT NULL,
    auto_default_threshold_days INT NOT NULL,
    no_score_behavior           VARCHAR(50) NOT NULL,
    created_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at                  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

INSERT INTO underwriting_policies (id, base_interest_rate, auto_default_threshold_days, no_score_behavior, created_at, updated_at)
VALUES ('default', 8.0, 90, 'approve_with_penalty', NOW(), NOW())
ON CONFLICT DO NOTHING;
