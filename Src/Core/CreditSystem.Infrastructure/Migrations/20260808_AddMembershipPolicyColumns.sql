-- Migration: Add membership policy columns to underwriting_policies
-- Created: 2026-08-08
-- Idempotent: yes

ALTER TABLE underwriting_policies
    ADD COLUMN IF NOT EXISTS shares_multiplier_limit    INT     NOT NULL DEFAULT 5,
    ADD COLUMN IF NOT EXISTS require_active_membership  BOOLEAN NOT NULL DEFAULT FALSE;
