-- Migration: Add social capital and configurable payment waterfall
-- 2026-08-10

-- credit_products: waterfall config and social capital config stored as JSONB
ALTER TABLE credit_products ADD COLUMN IF NOT EXISTS waterfall_config JSONB;
ALTER TABLE credit_products ADD COLUMN IF NOT EXISTS social_capital_config JSONB;

-- cooperative_members: running total of social capital accumulated by the member
ALTER TABLE cooperative_members ADD COLUMN IF NOT EXISTS social_capital_balance NUMERIC(18,2) NOT NULL DEFAULT 0;

-- rm_payment_history: record how much social capital was contributed per payment
ALTER TABLE rm_payment_history ADD COLUMN IF NOT EXISTS social_capital_contributed NUMERIC(18,2) NOT NULL DEFAULT 0;

/*
 POST /api/products
  {
    "name": "Crédito Cooperativo CRC",
    "minAmount": 100000,
    "maxAmount": 5000000,
    "minTermMonths": 6,
    "maxTermMonths": 60,
    "baseInterestRate": 14.5,
    "defaultAmortizationMethod": "French",
    "requiresCollateral": false,

    "waterfall": [
      { "priority": 1, "component": "Fees" },
      { "priority": 2, "component": "PenaltyInterest" },
      { "priority": 3, "component": "RegularInterest" },
      { "priority": 4, "component": "SocialCapital" },
      { "priority": 5, "component": "Principal" }
    ],

    "socialCapitalConfig": {
      "calculationType": "FixedAmount",
      "value": 500.00,
      "collectionMode": "IncludedInPayment"
    }
  }

 */
 
