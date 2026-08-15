-- Migration: 20260812_AsyncProjectionDispatcher
-- Purpose : Replace synchronous in-request projection with an async background worker.
--
-- Changes:
--   1. Fix interest_rate column overflow (NUMERIC(5,4) → NUMERIC(8,4)) in rm_loan_summaries.
--      The old type maxed at 9.9999%, causing failures for any real interest rate (e.g. 16.5%).
--   2. Add sequence BIGSERIAL to stored_events for stable ordering by insertion order.
--   3. Create projection_checkpoints: tracks per-projector position in the event stream.
--   4. Create projection_failures: auditable log of permanent projection failures.
--
-- Rollback:
--   ALTER TABLE rm_loan_summaries ALTER COLUMN interest_rate TYPE NUMERIC(5,4);
--   ALTER TABLE stored_events DROP COLUMN IF EXISTS sequence;
--   DROP TABLE IF EXISTS projection_failures;
--   DROP TABLE IF EXISTS projection_checkpoints;
--
-- Prerequisites: stored_events and rm_loan_summaries must exist.

-- 1. Fix interest_rate precision (was NUMERIC(5,4), max 9.9999%)
ALTER TABLE rm_loan_summaries
    ALTER COLUMN interest_rate TYPE NUMERIC(8,4);

-- 2. Add sequence column for stable, monotonic event ordering.
--    BIGSERIAL generates values automatically on insert; existing rows get values via sequence backfill.
ALTER TABLE stored_events
    ADD COLUMN IF NOT EXISTS sequence BIGSERIAL;

CREATE INDEX IF NOT EXISTS idx_stored_events_sequence
    ON stored_events(sequence ASC);

-- 3. Per-projector checkpoint: tracks the last sequence number successfully processed.
--    A missing row is equivalent to last_sequence = 0 (process from the beginning).
CREATE TABLE IF NOT EXISTS projection_checkpoints (
    projector_name  VARCHAR(100) PRIMARY KEY,
    last_sequence   BIGINT       NOT NULL DEFAULT 0,
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- 4. Permanent projection failure log: one row per projector per event that failed all retries.
--    Operators can inspect failures via GET /api/admin/projection-failures and resolve them
--    manually or trigger a full rebuild via POST /api/admin/projections/rebuild.
CREATE TABLE IF NOT EXISTS projection_failures (
    id              UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    event_id        UUID         NOT NULL REFERENCES stored_events(id),
    stream_id       UUID         NOT NULL,
    event_type      VARCHAR(100) NOT NULL,
    projector_name  VARCHAR(100) NOT NULL,
    error_message   TEXT         NOT NULL,
    occurred_at     TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    attempts        INT          NOT NULL DEFAULT 1,
    resolved        BOOLEAN      NOT NULL DEFAULT false,
    resolved_at     TIMESTAMPTZ
);

-- Index for the common query: unresolved failures ordered by recency.
CREATE INDEX IF NOT EXISTS idx_projection_failures_unresolved
    ON projection_failures(occurred_at DESC)
    WHERE resolved = false;
