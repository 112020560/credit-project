-- Migration: Add cooperative_members table
-- Created: 2026-08-08
-- Idempotent: yes

CREATE TABLE IF NOT EXISTS cooperative_members (
    id                       UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    external_id              UUID NOT NULL,
    member_number            VARCHAR(50) NOT NULL,
    status                   VARCHAR(20) NOT NULL DEFAULT 'Active',
    joined_at                TIMESTAMPTZ NOT NULL,
    total_shares_amount      DECIMAL(18,2) NOT NULL DEFAULT 0,
    shares_currency          VARCHAR(3) NOT NULL DEFAULT 'CRC',
    number_of_contributions  INT NOT NULL DEFAULT 0,
    last_contribution_date   TIMESTAMPTZ,
    created_at               TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at               TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_cooperative_members_external_id
    ON cooperative_members (external_id);

CREATE UNIQUE INDEX IF NOT EXISTS ux_cooperative_members_member_number
    ON cooperative_members (member_number);
