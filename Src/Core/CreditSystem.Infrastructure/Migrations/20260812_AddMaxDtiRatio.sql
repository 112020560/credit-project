-- Migration: Add max_dti_ratio to underwriting_policies
-- 2026-08-12

ALTER TABLE underwriting_policies
    ADD COLUMN IF NOT EXISTS max_dti_ratio NUMERIC(4,2) NOT NULL DEFAULT 0.50;
