-- Migration: Add member_number_sequences table
-- Created: 2026-08-11
-- Idempotent: yes
-- Purpose: Tracks per-year sequential counters for cooperative member number generation.
--          The CreditSystem owns member number generation; the CRM does not.

CREATE TABLE IF NOT EXISTS member_number_sequences (
    year        INT     PRIMARY KEY,
    last_value  INT     NOT NULL DEFAULT 0
);
