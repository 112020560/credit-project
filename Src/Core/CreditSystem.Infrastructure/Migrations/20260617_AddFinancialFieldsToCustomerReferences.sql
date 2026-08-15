-- Migration: Add Financial Fields to Customer References
-- Date: 2026-06-17
-- Description: Adds credit_score, monthly_income and monthly_debt columns to
--              customer_references. These values are populated from the CRM via
--              the CustomerCreated/CustomerUpdated messages using the Metadata /
--              Changes dictionary (Option B).

ALTER TABLE customer_references
    ADD COLUMN IF NOT EXISTS credit_score    INT            NULL,
    ADD COLUMN IF NOT EXISTS monthly_income  DECIMAL(18,2)  NULL,
    ADD COLUMN IF NOT EXISTS monthly_debt    DECIMAL(18,2)  NULL;

COMMENT ON COLUMN customer_references.credit_score   IS 'Credit score received from CRM via CustomerCreated.Metadata["CreditScore"] or CustomerUpdated.Changes["CreditScore"]';
COMMENT ON COLUMN customer_references.monthly_income IS 'Monthly gross income received from CRM via Metadata/Changes["MonthlyIncome"]';
COMMENT ON COLUMN customer_references.monthly_debt   IS 'Existing monthly debt obligations received from CRM via Metadata/Changes["MonthlyDebt"]';
