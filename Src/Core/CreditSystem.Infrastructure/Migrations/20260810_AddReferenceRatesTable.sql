-- Migration: Add reference_rates table for variable-rate loan support
-- Created: 2026-08-10
-- Idempotent: yes

CREATE TABLE IF NOT EXISTS reference_rates (
    id              VARCHAR(32)    PRIMARY KEY,
    name            VARCHAR(128)   NOT NULL,
    current_value   DECIMAL(8,4)   NOT NULL,
    effective_date  DATE           NOT NULL,
    source          VARCHAR(64)    NOT NULL,
    updated_at      TIMESTAMPTZ    NOT NULL DEFAULT NOW()
);

-- Seed: Tasa Básica Pasiva del BCCR (valor referencial — actualizar con el vigente antes de producción)
INSERT INTO reference_rates (id, name, current_value, effective_date, source)
VALUES ('TBP_CRC', 'Tasa Básica Pasiva (BCCR)', 4.25, '2026-08-01', 'BCCR')
ON CONFLICT (id) DO NOTHING;
