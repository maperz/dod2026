-- Create a transient clone from schema (zerocopy / COW)
CREATE TRANSIENT SCHEMA IF NOT EXISTS "CUSTOM_CI_123" CLONE "PUBLIC";

USE SCHEMA "CUSTOM_CI_123";
SHOW TABLES;

SELECT * FROM "CUSTOM_CI_123"."DAILY_STOCK_PRICES"
    ORDER BY "TICKER";
    
-- Delete data in current schema 
DELETE FROM "CUSTOM_CI_123"."DAILY_STOCK_PRICES" 
    WHERE "TICKER" = 'A';

-- Data is still present on public
SELECT * FROM "PUBLIC"."DAILY_STOCK_PRICES" ORDER BY "TICKER";

-- Drop temporary schema again
DROP SCHEMA "CUSTOM_CI_123";
