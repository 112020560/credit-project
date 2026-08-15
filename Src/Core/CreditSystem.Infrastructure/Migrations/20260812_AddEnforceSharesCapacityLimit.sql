-- Migration: Add enforce_shares_capacity_limit to underwriting_policies
-- 2026-08-12

ALTER TABLE underwriting_policies
    ADD COLUMN IF NOT EXISTS enforce_shares_capacity_limit BOOLEAN NOT NULL DEFAULT false;
