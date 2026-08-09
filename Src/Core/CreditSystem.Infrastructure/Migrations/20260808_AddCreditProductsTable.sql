-- Migration: Add credit_products table
-- Date: 2026-08-08

CREATE TABLE IF NOT EXISTS credit_products (
    id                          UUID            PRIMARY KEY DEFAULT gen_random_uuid(),
    name                        VARCHAR(100)    NOT NULL,
    min_amount                  NUMERIC(18, 2)  NOT NULL,
    max_amount                  NUMERIC(18, 2)  NOT NULL,
    min_term_months             INT             NOT NULL,
    max_term_months             INT             NOT NULL,
    base_interest_rate          NUMERIC(6, 4),              -- NULL = usa la tasa de la política global
    max_ltv                     NUMERIC(6, 4),              -- NULL = sin restricción LTV
    default_amortization_method VARCHAR(30)     NOT NULL DEFAULT 'French',
    requires_collateral         BOOLEAN         NOT NULL DEFAULT FALSE,
    status                      VARCHAR(10)     NOT NULL DEFAULT 'Active',
    created_at                  TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_amount_range CHECK (min_amount < max_amount AND min_amount > 0),
    CONSTRAINT chk_term_range   CHECK (min_term_months < max_term_months AND min_term_months > 0),
    CONSTRAINT chk_status       CHECK (status IN ('Active', 'Inactive')),
    CONSTRAINT chk_ltv          CHECK (max_ltv IS NULL OR (max_ltv > 0 AND max_ltv <= 1))
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_credit_products_name ON credit_products (name);

-- Seed: 5 productos típicos de cooperativa costarricense (SUGEF)
-- Tasas almacenadas como porcentaje (14.0 = 14%), consistent con UnderwritingPolicy.BaseInterestRate
-- MaxLtv almacenado como fracción decimal (0.80 = 80%)
INSERT INTO credit_products (id, name, min_amount, max_amount, min_term_months, max_term_months, base_interest_rate, max_ltv, default_amortization_method, requires_collateral, status)
VALUES
    (gen_random_uuid(), 'Préstamo Personal',    100000, 5000000,    1,  60,  14.0, NULL, 'French', FALSE, 'Active'),
    (gen_random_uuid(), 'Préstamo de Consumo',  50000,  2000000,    1,  36,  18.0, NULL, 'French', FALSE, 'Active'),
    (gen_random_uuid(), 'Préstamo Vehicular',   500000, 15000000,   12, 84,  12.0, 0.90, 'French', TRUE,  'Active'),
    (gen_random_uuid(), 'Préstamo Hipotecario', 1000000,100000000,  60, 300,  9.0, 0.80, 'French', TRUE,  'Active'),
    (gen_random_uuid(), 'Microcrédito',         10000,  500000,     1,  24,  20.0, NULL, 'French', FALSE, 'Active')
ON CONFLICT (name) DO NOTHING;
