-- =============================================================================
-- TEST DATA SEED - Credit System
-- Execute this to create test data for functional testing
-- =============================================================================

-- 1. Create test customers in customer_references
INSERT INTO customer_references (id, external_id, full_name, email, phone, document_type, document_number, created_at, updated_at)
VALUES
    ('a1111111-1111-1111-1111-111111111111', 'e1111111-1111-1111-1111-111111111111', 'Juan Pérez García', 'juan.perez@email.com', '+52 55 1234 5678', 'INE', 'PEGJ850101HDFRNN01', NOW(), NOW()),
    ('a2222222-2222-2222-2222-222222222222', 'e2222222-2222-2222-2222-222222222222', 'María López Hernández', 'maria.lopez@email.com', '+52 55 8765 4321', 'INE', 'LOHM900215MDFPNR02', NOW(), NOW()),
    ('a3333333-3333-3333-3333-333333333333', 'e3333333-3333-3333-3333-333333333333', 'Carlos Ramírez Soto', 'carlos.ramirez@email.com', '+52 55 1111 2222', 'PASSPORT', 'G12345678', NOW(), NOW())
ON CONFLICT (external_id) DO UPDATE SET
    full_name = EXCLUDED.full_name,
    email = EXCLUDED.email,
    phone = EXCLUDED.phone,
    updated_at = NOW();

-- Verify insertion
SELECT id, external_id, full_name, email FROM customer_references;
