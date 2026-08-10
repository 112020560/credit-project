-- Migration: Add rate_type, spread, reference_rate_id columns to rm_loan_summaries
-- Created: 2026-08-10
-- Idempotent: yes (uses IF NOT EXISTS pattern via DO blocks)

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'rm_loan_summaries' AND column_name = 'rate_type'
    ) THEN
        ALTER TABLE rm_loan_summaries ADD COLUMN rate_type VARCHAR(16) NOT NULL DEFAULT 'Fixed';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'rm_loan_summaries' AND column_name = 'spread'
    ) THEN
        ALTER TABLE rm_loan_summaries ADD COLUMN spread DECIMAL(8,4) NOT NULL DEFAULT 0;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_name = 'rm_loan_summaries' AND column_name = 'reference_rate_id'
    ) THEN
        ALTER TABLE rm_loan_summaries ADD COLUMN reference_rate_id VARCHAR(32);
    END IF;
END $$;
