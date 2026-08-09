-- Migration: Add mora y comisiones columns
-- 2026-08-09

-- underwriting_policies: grace period, penalty rate, origination fee rate
ALTER TABLE underwriting_policies ADD COLUMN IF NOT EXISTS grace_period_days INT NOT NULL DEFAULT 5;
ALTER TABLE underwriting_policies ADD COLUMN IF NOT EXISTS penalty_rate NUMERIC(6,4) NOT NULL DEFAULT 0;
ALTER TABLE underwriting_policies ADD COLUMN IF NOT EXISTS origination_fee_rate NUMERIC(6,4) NOT NULL DEFAULT 0;

-- credit_products: optional rate overrides per product
ALTER TABLE credit_products ADD COLUMN IF NOT EXISTS penalty_rate NUMERIC(6,4);
ALTER TABLE credit_products ADD COLUMN IF NOT EXISTS origination_fee_rate NUMERIC(6,4);

-- rm_loan_summaries: new read-model fields
ALTER TABLE rm_loan_summaries ADD COLUMN IF NOT EXISTS origination_fee NUMERIC NOT NULL DEFAULT 0;
ALTER TABLE rm_loan_summaries ADD COLUMN IF NOT EXISTS accrued_penalty_interest NUMERIC NOT NULL DEFAULT 0;
