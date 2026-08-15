-- Migration: Replace credit_products table with domain-aligned schema
-- Date: 2026-08-08
-- Context: The pre-existing credit_products table had a different schema (single term_months,
--          interest_rate/interest_type instead of base_interest_rate, no status/requires_collateral/max_ltv).
--          This migration drops and recreates the table to match the CreditProduct domain entity.
--
-- WARNING: This drops all existing data in credit_products.
--          Run only in dev/test environments or after backing up any needed data.

DROP TABLE IF EXISTS credit_products CASCADE;

CREATE TABLE credit_products (
    id                          UUID            PRIMARY KEY DEFAULT uuid_generate_v4(),
    name                        VARCHAR(100)    NOT NULL,
    min_amount                  NUMERIC(18, 2)  NOT NULL,
    max_amount                  NUMERIC(18, 2)  NOT NULL,
    min_term_months             INT             NOT NULL,
    max_term_months             INT             NOT NULL,
    base_interest_rate          NUMERIC(6, 2),              -- NULL = usa la tasa de la política global; valor en porcentaje (14.0 = 14%)
    max_ltv                     NUMERIC(6, 4),              -- NULL = sin restricción LTV; fracción decimal (0.80 = 80%)
    default_amortization_method VARCHAR(30)     NOT NULL DEFAULT 'French',
    requires_collateral         BOOLEAN         NOT NULL DEFAULT FALSE,
    status                      VARCHAR(10)     NOT NULL DEFAULT 'Active',
    created_at                  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_amount_range CHECK (min_amount < max_amount AND min_amount > 0),
    CONSTRAINT chk_term_range   CHECK (min_term_months < max_term_months AND min_term_months > 0),
    CONSTRAINT chk_status       CHECK (status IN ('Active', 'Inactive')),
    CONSTRAINT chk_ltv          CHECK (max_ltv IS NULL OR (max_ltv > 0 AND max_ltv <= 1))
);

CREATE UNIQUE INDEX ux_credit_products_name ON credit_products (name);

-- Seed: 5 productos típicos de cooperativa costarricense (SUGEF)
-- base_interest_rate en porcentaje (14.0 = 14%), consistent con UnderwritingPolicy.BaseInterestRate
-- max_ltv como fracción decimal (0.80 = 80%)
INSERT INTO credit_products (id, name, min_amount, max_amount, min_term_months, max_term_months, base_interest_rate, max_ltv, default_amortization_method, requires_collateral, status)
VALUES
    (uuid_generate_v4(), 'Préstamo Personal',    100000,   5000000,   1,   60,  14.0, NULL, 'French', FALSE, 'Active'),
    (uuid_generate_v4(), 'Préstamo de Consumo',   50000,   2000000,   1,   36,  18.0, NULL, 'French', FALSE, 'Active'),
    (uuid_generate_v4(), 'Préstamo Vehicular',   500000,  15000000,  12,   84,  12.0, 0.90, 'French', TRUE,  'Active'),
    (uuid_generate_v4(), 'Préstamo Hipotecario', 1000000, 100000000, 60,  300,   9.0, 0.80, 'French', TRUE,  'Active'),
    (uuid_generate_v4(), 'Microcrédito',          10000,    500000,   1,   24,  20.0, NULL, 'French', FALSE, 'Active')
ON CONFLICT (name) DO NOTHING;
