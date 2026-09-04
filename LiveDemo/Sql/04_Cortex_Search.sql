USE ROLE "DOD_DEVELOPER";
USE DATABASE "DOD2026";
USE SCHEMA "PUBLIC";

CREATE OR REPLACE CORTEX SEARCH SERVICE "STOCK_NEWS_SEARCH_SERVICE"
ON "SEARCH_TEXT"
ATTRIBUTES "HEADLINE", "STOCK", "DATE", "SENTIMENT"
WAREHOUSE = "DOD_WAREHOUSE"
TARGET_LAG = '30 days'
AS
SELECT
    "HEADLINE",
    "STOCK", 
    "DATE",
    "SENTIMENT",
    "PUBLISHER",
    ('Headline\n\n' || "HEADLINE" || '\n\nPublisher\n\n' || "PUBLISHER" || '\n\n\STOCK TICKER\n\n' || "STOCK" || '\n\n\SENTIMENT: \n\n'  || "SENTIMENT" || '\n\n\On Date: \n\n' || TO_VARCHAR("DATE")) as "SEARCH_TEXT"
FROM STOCK_NEWS_EVENT;


-- DROP CORTEX SEARCH SERVICE "STOCK_NEWS_SEARCH_SERVICE";

SELECT PARSE_JSON(
  SNOWFLAKE.CORTEX.SEARCH_PREVIEW(
      'STOCK_NEWS_SEARCH_SERVICE',
      '{
        "query": "earning calls",
        "columns":[
            "HEADLINE",
            "STOCK",
            "DATE",
            "PUBLISHER",
            "SENTIMENT"
        ],
        "limit": 5
      }'
  )
)['results'] as r;


SELECT
    r.VALUE:"HEADLINE"::VARCHAR AS "HEADLINE",
    r.VALUE:"STOCK"::VARCHAR AS "STOCK",
    r.VALUE:"SENTIMENT"::VARCHAR AS "SENTIMENT",
    r.VALUE:"PUBLISHER"::VARCHAR AS "PUBLISHER",
    r.VALUE:"DATE"::DATE AS "DATE",
    r.VALUE:"@scores"."cosine_similarity"::FLOAT AS "COSINE_SIMILARITY",
    r.VALUE:"@scores"."text_match"::FLOAT AS "TEXT_MATCH",
    r.VALUE:"@scores"."reranker_score"::FLOAT AS "RERANKER_SCORE"
FROM TABLE(FLATTEN(input => PARSE_JSON(
  SNOWFLAKE.CORTEX.SEARCH_PREVIEW(
      'STOCK_NEWS_SEARCH_SERVICE',
      '{
        "query": "earning calls",
        "columns":[
            "HEADLINE",
            "STOCK",
            "DATE",
            "PUBLISHER",
            "SENTIMENT"
        ],
        "limit": 5
      }'
  )
)['results'])) r;


-- Costs $2.00 or $2.20 pr AI Credit
SELECT
    USAGE_DATE,
    CONSUMPTION_TYPE,
    CREDITS,
    CREDITS * 2.20 AS "PRICE", 
    MODEL_NAME,
    TOKENS,
FROM SNOWFLAKE.ACCOUNT_USAGE.CORTEX_SEARCH_DAILY_USAGE_HISTORY
WHERE SERVICE_NAME = 'STOCK_NEWS_SEARCH_SERVICE'
  AND DATABASE_NAME = 'DOD2026'
ORDER BY USAGE_DATE DESC;
