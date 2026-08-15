-- Migration: Rename customer_references → customer_credit_profiles
-- Created: 2026-06-24
-- Reason: Ubiquitous language alignment — the entity CustomerReference was renamed
--         to CustomerCreditProfile to better reflect its role in the credit domain.
-- Idempotent: yes (uses DO $$ blocks with existence checks)

DO $$
BEGIN
    -- Rename table if it still has the old name
    IF EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'customer_references'
    ) AND NOT EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'public'
          AND table_name = 'customer_credit_profiles'
    ) THEN
        ALTER TABLE customer_references RENAME TO customer_credit_profiles;
    END IF;
END $$;

-- Update table comment to reflect the new ubiquitous language
COMMENT ON TABLE customer_credit_profiles IS
    'Local cache of customer data synced from the CRM. '
    'Represents a CustomerCreditProfile — the credit-domain view of a customer, '
    'enriched with financial data (credit_score, monthly_income, monthly_debt) '
    'used by the underwriting engine.';
