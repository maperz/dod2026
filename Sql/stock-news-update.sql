UPDATE "STOCK_NEWS"
SET
    "TICKER" = ?Ticker?,
    "TEXT" = ?Text?,
    "DATE" = TO_DATE(?Date?)
WHERE "ID" = ?id?
