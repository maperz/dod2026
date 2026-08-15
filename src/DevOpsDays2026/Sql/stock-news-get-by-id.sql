SELECT
    "ID" AS Id,
    "TICKER" AS Ticker,
    "TEXT" AS Text,
    "DATE" AS Date
FROM "STOCK_NEWS"
WHERE "ID" = ?id?
