-- Migration: Add loan_guarantees table
-- Date: 2026-08-08

CREATE TABLE IF NOT EXISTS loan_guarantees (
    id                  UUID            PRIMARY KEY DEFAULT uuid_generate_v4(),
    loan_contract_id    UUID            NOT NULL,  -- referencia lógica al contrato (sin FK: read models son eventualmente consistentes)
    type                VARCHAR(30)     NOT NULL,
    description         TEXT            NOT NULL,
    appraisal_value     NUMERIC(18, 2)  NOT NULL,
    coverage_rate       NUMERIC(6, 4)   NOT NULL,
    status              VARCHAR(20)     NOT NULL DEFAULT 'Vigente',
    expiration_date     DATE,
    created_at          TIMESTAMPTZ     NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_appraisal_value CHECK (appraisal_value > 0),
    CONSTRAINT chk_coverage_rate   CHECK (coverage_rate > 0 AND coverage_rate <= 1),
    CONSTRAINT chk_status          CHECK (status IN ('Vigente', 'Liberada', 'Ejecutada'))
);

CREATE INDEX IF NOT EXISTS ix_loan_guarantees_contract ON loan_guarantees (loan_contract_id);
