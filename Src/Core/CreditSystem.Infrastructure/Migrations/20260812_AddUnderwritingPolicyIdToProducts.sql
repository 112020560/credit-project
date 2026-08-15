-- Migration: 20260812_AddUnderwritingPolicyIdToProducts
-- Purpose : Bind each credit product to a specific underwriting policy instead of
--           relying on a shared global singleton loaded at application startup.
--
-- Background:
--   Previously, the application loaded a single UnderwritingPolicy row (WHERE id = 'default')
--   at startup via AddSingleton<UnderwritingPolicy>. This meant all credit products shared
--   the same evaluation parameters (DTI ratio, score thresholds, shares multiplier, etc.),
--   making it impossible to offer differentiated risk profiles per product type.
--
-- Change:
--   Adds underwriting_policy_id VARCHAR NOT NULL DEFAULT 'default' to credit_products.
--   A FK to underwriting_policies.id enforces referential integrity.
--   Existing products automatically point to 'default' (the seed row guaranteed to exist).
--
-- Rollback (if needed):
--   ALTER TABLE credit_products DROP CONSTRAINT fk_credit_products_underwriting_policy;
--   ALTER TABLE credit_products DROP COLUMN underwriting_policy_id;
--
-- Prerequisites:
--   - underwriting_policies table must exist with at least one row WHERE id = 'default'
--   - Run AFTER any pending underwriting_policies seed migrations

ALTER TABLE credit_products
    ADD COLUMN underwriting_policy_id VARCHAR NOT NULL DEFAULT 'default';

-- FK: each product must reference a valid underwriting policy row.
-- ON DELETE RESTRICT / ON UPDATE CASCADE would be ideal but left as DB default (RESTRICT)
-- since policy IDs are immutable string keys (e.g. 'default', 'mortgage', 'business').
ALTER TABLE credit_products
    ADD CONSTRAINT fk_credit_products_underwriting_policy
        FOREIGN KEY (underwriting_policy_id)
        REFERENCES underwriting_policies(id);
