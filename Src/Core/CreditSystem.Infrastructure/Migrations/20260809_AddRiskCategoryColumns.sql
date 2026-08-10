-- Migration: Add SUGEF 1-05 risk category columns
-- 2026-08-09

ALTER TABLE rm_loan_summaries ADD COLUMN IF NOT EXISTS risk_category VARCHAR(3);
ALTER TABLE rm_loan_summaries ADD COLUMN IF NOT EXISTS estimated_provision NUMERIC NOT NULL DEFAULT 0;
