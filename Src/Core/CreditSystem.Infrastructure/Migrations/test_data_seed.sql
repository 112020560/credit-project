-- =============================================================================
-- TEST DATA SEED - Credit System
-- Execute this to create test data for functional testing
-- =============================================================================

-- 1. Create test customers in customer_references
-- credit_score / monthly_income / monthly_debt simulate values sent by the CRM
-- via CustomerCreated.Metadata or CustomerUpdated.Changes
INSERT INTO customer_references (id, external_id, full_name, email, phone, document_type, document_number, credit_score, monthly_income, monthly_debt, created_at, updated_at)
VALUES
    -- Score excelente (>= 750) → tasa base sin ajuste por score
    ('a1111111-1111-1111-1111-111111111111', 'e1111111-1111-1111-1111-111111111111',
     'Juan Pérez García',       'juan.perez@email.com',    '+52 55 1234 5678', 'INE',      'PEGJ850101HDFRNN01', 780,  5000.00, 300.00, NOW(), NOW()),

    -- Score bueno (700-749) → +1.5% tasa
    ('a2222222-2222-2222-2222-222222222222', 'e2222222-2222-2222-2222-222222222222',
     'María López Hernández',   'maria.lopez@email.com',   '+52 55 8765 4321', 'INE',      'LOHM900215MDFPNR02', 720,  3500.00, 400.00, NOW(), NOW()),

    -- Score bajo (500-549) → +12% tasa, aun aprobado (hard stop es < 500)
    ('a3333333-3333-3333-3333-333333333333', 'e3333333-3333-3333-3333-333333333333',
     'Carlos Ramírez Soto',     'carlos.ramirez@email.com','+52 55 1111 2222', 'PASSPORT', 'G12345678',           520,  2000.00, 200.00, NOW(), NOW()),

    -- Sin score → CreditScoreRule hace skip, solo aplican otras reglas
    ('a4444444-4444-4444-4444-444444444444', 'e4444444-4444-4444-4444-444444444444',
     'Ana Torres Villanueva',   'ana.torres@email.com',    '+52 55 9999 0000', 'INE',      'TOVA950310MDFRRN04', NULL, 4000.00, 500.00, NOW(), NOW())
ON CONFLICT (external_id) DO UPDATE SET
    full_name      = EXCLUDED.full_name,
    email          = EXCLUDED.email,
    phone          = EXCLUDED.phone,
    credit_score   = EXCLUDED.credit_score,
    monthly_income = EXCLUDED.monthly_income,
    monthly_debt   = EXCLUDED.monthly_debt,
    updated_at     = NOW();

-- Verify insertion
SELECT id, external_id, full_name, credit_score, monthly_income, monthly_debt FROM customer_references;
