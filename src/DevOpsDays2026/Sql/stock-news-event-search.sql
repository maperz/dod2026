WITH "QUERY_EMBEDDING" AS (
    SELECT SNOWFLAKE.CORTEX.EMBED_TEXT_768(
        'snowflake-arctic-embed-m-v1.5',
        ?searchText?
    ) AS "EMBEDDING"
),
"SCORED_EVENTS" AS (
    SELECT
        "EVENTS"."HEADLINE" AS Headline,
        "EVENTS"."PUBLISHER" AS Publisher,
        "EVENTS"."DATE" AS Date,
        "EVENTS"."STOCK" AS Stock,
        "EVENTS"."SENTIMENT" AS Sentiment,
        VECTOR_COSINE_SIMILARITY(
            "EVENTS"."EMBEDDING",
            "QUERY_EMBEDDING"."EMBEDDING"
        ) AS CosineSimilarity
    FROM "STOCK_NEWS_EVENT" AS "EVENTS"
    CROSS JOIN "QUERY_EMBEDDING"
    WHERE "EVENTS"."EMBEDDING" IS NOT NULL
),
"RANKED_EVENTS" AS (
    SELECT
        Headline,
        Publisher,
        Date,
        Stock,
        Sentiment,
        CosineSimilarity,
        ROW_NUMBER() OVER (ORDER BY CosineSimilarity DESC, Date DESC) AS RowNumber
    FROM "SCORED_EVENTS"
)
SELECT
    Headline,
    Publisher,
    Date,
    Stock,
    Sentiment,
    TO_CHAR(ROUND(CosineSimilarity, 4), 'FM9999990.0000') AS SearchScore
FROM "RANKED_EVENTS"
WHERE RowNumber <= ?limit?
ORDER BY CosineSimilarity DESC, Date DESC
