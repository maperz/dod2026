SELECT
    "TICKER" AS Ticker,
    "DATE" AS Date,
    "CLOSE_PRICE" AS ClosePrice
FROM "DAILY_STOCK_PRICES"
WHERE "DATE" = TO_DATE(?date?)
  AND UPPER("TICKER") LIKE '%' || UPPER(?ticker?) || '%'
ORDER BY "TICKER"
