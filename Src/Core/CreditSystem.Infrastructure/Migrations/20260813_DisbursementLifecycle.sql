-- Migration: 20260813_DisbursementLifecycle
-- Adds rm_pending_disbursements table and inserts DisbursementConfirmed synthetic events
-- for loans that already have LoanDisbursed persisted (so replay doesn't leave them in Disbursing).

-- ============================================================
-- 1. Create rm_pending_disbursements read model table
-- ============================================================

CREATE TABLE IF NOT EXISTS rm_pending_disbursements (
    loan_id                    UUID         PRIMARY KEY,
    customer_id                UUID         NOT NULL,
    customer_name              VARCHAR(200),
    principal                  NUMERIC(18,2) NOT NULL,
    currency                   VARCHAR(3)   NOT NULL DEFAULT 'USD',
    disbursement_method        VARCHAR(50),
    destination_account        VARCHAR(100),
    approved_at                TIMESTAMPTZ  NOT NULL,
    disbursement_instructed_at TIMESTAMPTZ,
    status                     VARCHAR(20)  NOT NULL DEFAULT 'Approved',
    updated_at                 TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_pending_disbursements_status
    ON rm_pending_disbursements(status, disbursement_instructed_at ASC);

-- ============================================================
-- 2. Synthetic DisbursementConfirmed events for historical loans
--
-- Any loan that has a LoanDisbursed event but no DisbursementConfirmed event
-- is effectively Active from the old model. Insert a synthetic
-- DisbursementConfirmed so that replay leaves them in Active, not Disbursing.
-- ============================================================

INSERT INTO stored_events (
    id,
    stream_id,
    event_type,
    event_data,
    metadata,
    version,
    hash,
    previous_hash,
    occurred_at,
    stored_at
)
WITH disbursed_loans AS (
    SELECT DISTINCT ON (stream_id)
        stream_id,
        occurred_at,
        hash
    FROM stored_events
    WHERE event_type = 'LoanDisbursed'
      AND NOT EXISTS (
          SELECT 1
          FROM stored_events confirmed
          WHERE confirmed.stream_id = stored_events.stream_id
            AND confirmed.event_type = 'DisbursementConfirmed'
      )
    ORDER BY stream_id, version DESC
),
max_versions AS (
    SELECT stream_id, MAX(version) AS max_version
    FROM stored_events
    GROUP BY stream_id
)
SELECT
    gen_random_uuid()                                   AS id,
    dl.stream_id                                        AS stream_id,
    'DisbursementConfirmed'                             AS event_type,
    jsonb_build_object(
        'aggregateId',  dl.stream_id,
        'confirmedBy',  'migration',
        'disbursedAt',  dl.occurred_at,
        'version',      mv.max_version + 1,
        'occurredAt',   dl.occurred_at,
        'id',           gen_random_uuid()
    )                                                   AS event_data,
    '{}'::jsonb                                         AS metadata,
    mv.max_version + 1                                  AS version,
    md5(gen_random_uuid()::text)                        AS hash,
    dl.hash                                             AS previous_hash,
    dl.occurred_at                                      AS occurred_at,
    NOW()                                               AS stored_at
FROM disbursed_loans dl
JOIN max_versions mv ON mv.stream_id = dl.stream_id;
