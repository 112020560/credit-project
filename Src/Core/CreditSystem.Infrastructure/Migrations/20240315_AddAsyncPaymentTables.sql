-- Migration: Add Async Payment Infrastructure Tables
-- Date: 2024-03-15
-- Description: Creates tables for async payment processing, outbox pattern, and webhooks

-- ============================================================================
-- Payment Tracking Table
-- Tracks the status of asynchronously processed payments
-- ============================================================================
CREATE TABLE IF NOT EXISTS rm_payment_tracking (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_id UUID NOT NULL UNIQUE,
    loan_id UUID,
    credit_line_id UUID,
    customer_id UUID NOT NULL,
    amount DECIMAL(18,2) NOT NULL,
    currency VARCHAR(3) NOT NULL DEFAULT 'MXN',
    payment_method VARCHAR(20) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    error_code VARCHAR(50),
    error_message TEXT,
    principal_paid DECIMAL(18,2),
    interest_paid DECIMAL(18,2),
    fees_paid DECIMAL(18,2),
    new_balance DECIMAL(18,2),
    new_available_credit DECIMAL(18,2),
    is_paid_off BOOLEAN,
    correlation_id UUID,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    processed_at TIMESTAMPTZ,

    CONSTRAINT chk_payment_tracking_status
        CHECK (status IN ('PENDING', 'PROCESSING', 'COMPLETED', 'FAILED', 'REJECTED')),
    CONSTRAINT chk_payment_tracking_loan_or_credit
        CHECK (loan_id IS NOT NULL OR credit_line_id IS NOT NULL)
);

-- Indexes for payment tracking queries
CREATE INDEX IF NOT EXISTS idx_payment_tracking_payment_id
    ON rm_payment_tracking(payment_id);
CREATE INDEX IF NOT EXISTS idx_payment_tracking_loan_id
    ON rm_payment_tracking(loan_id) WHERE loan_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_payment_tracking_credit_line_id
    ON rm_payment_tracking(credit_line_id) WHERE credit_line_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_payment_tracking_customer_id
    ON rm_payment_tracking(customer_id);
CREATE INDEX IF NOT EXISTS idx_payment_tracking_status
    ON rm_payment_tracking(status) WHERE status IN ('PENDING', 'PROCESSING');
CREATE INDEX IF NOT EXISTS idx_payment_tracking_correlation_id
    ON rm_payment_tracking(correlation_id) WHERE correlation_id IS NOT NULL;

-- ============================================================================
-- Outbox Messages Table
-- Implements the Outbox Pattern for guaranteed message delivery
-- ============================================================================
CREATE TABLE IF NOT EXISTS outbox_messages (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    message_type VARCHAR(100) NOT NULL,
    payload JSONB NOT NULL,
    correlation_id UUID,
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    published_at TIMESTAMPTZ,
    retry_count INT NOT NULL DEFAULT 0,
    last_error TEXT,

    CONSTRAINT chk_outbox_status
        CHECK (status IN ('PENDING', 'PUBLISHED', 'FAILED'))
);

-- Index for polling pending messages (used by OutboxPublisherWorker)
CREATE INDEX IF NOT EXISTS idx_outbox_pending
    ON outbox_messages(status, created_at)
    WHERE status = 'PENDING';
CREATE INDEX IF NOT EXISTS idx_outbox_correlation_id
    ON outbox_messages(correlation_id)
    WHERE correlation_id IS NOT NULL;

-- ============================================================================
-- Webhook Subscriptions Table
-- Stores customer webhook subscriptions for event notifications
-- ============================================================================
CREATE TABLE IF NOT EXISTS webhook_subscriptions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id UUID NOT NULL,
    event_type VARCHAR(50) NOT NULL,
    callback_url TEXT NOT NULL,
    secret_key VARCHAR(255) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    -- Each customer can have one subscription per event type and URL
    CONSTRAINT uq_webhook_subscription
        UNIQUE(customer_id, event_type, callback_url)
);

-- Indexes for webhook subscription queries
CREATE INDEX IF NOT EXISTS idx_webhook_subs_customer_active
    ON webhook_subscriptions(customer_id)
    WHERE is_active = true;
CREATE INDEX IF NOT EXISTS idx_webhook_subs_event_active
    ON webhook_subscriptions(event_type)
    WHERE is_active = true;
CREATE INDEX IF NOT EXISTS idx_webhook_subs_customer_event
    ON webhook_subscriptions(customer_id, event_type)
    WHERE is_active = true;

-- ============================================================================
-- Webhook Deliveries Table
-- Tracks webhook delivery attempts and status
-- ============================================================================
CREATE TABLE IF NOT EXISTS webhook_deliveries (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    subscription_id UUID NOT NULL REFERENCES webhook_subscriptions(id) ON DELETE CASCADE,
    payment_id UUID NOT NULL,
    event_type VARCHAR(50) NOT NULL,
    payload JSONB NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    http_status_code INT,
    response_body TEXT,
    attempt_count INT NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    delivered_at TIMESTAMPTZ,
    next_retry_at TIMESTAMPTZ,

    CONSTRAINT chk_webhook_delivery_status
        CHECK (status IN ('PENDING', 'DELIVERED', 'FAILED'))
);

-- Index for polling pending deliveries (used by WebhookDeliveryWorker)
CREATE INDEX IF NOT EXISTS idx_webhook_deliveries_pending
    ON webhook_deliveries(status, next_retry_at, created_at)
    WHERE status = 'PENDING';
CREATE INDEX IF NOT EXISTS idx_webhook_deliveries_subscription
    ON webhook_deliveries(subscription_id);
CREATE INDEX IF NOT EXISTS idx_webhook_deliveries_payment
    ON webhook_deliveries(payment_id);

-- ============================================================================
-- Comments for documentation
-- ============================================================================
COMMENT ON TABLE rm_payment_tracking IS
    'Tracks asynchronous payment processing status. Clients poll this table to check payment status.';
COMMENT ON COLUMN rm_payment_tracking.status IS
    'PENDING: Accepted, awaiting processing. PROCESSING: Being processed. COMPLETED: Success. FAILED: Technical error. REJECTED: Business rule violation.';

COMMENT ON TABLE outbox_messages IS
    'Outbox pattern for guaranteed message delivery. Messages are stored here within the same transaction as the business operation.';
COMMENT ON COLUMN outbox_messages.status IS
    'PENDING: Awaiting publication. PUBLISHED: Successfully sent to message broker. FAILED: Max retries exceeded.';

COMMENT ON TABLE webhook_subscriptions IS
    'Customer subscriptions for webhook notifications. Each customer can subscribe to different event types.';
COMMENT ON COLUMN webhook_subscriptions.secret_key IS
    'HMAC secret key used to sign webhook payloads. Clients verify signatures using this key.';

COMMENT ON TABLE webhook_deliveries IS
    'Tracks individual webhook delivery attempts with retry support.';
COMMENT ON COLUMN webhook_deliveries.next_retry_at IS
    'When set, the delivery will be retried after this timestamp.';
