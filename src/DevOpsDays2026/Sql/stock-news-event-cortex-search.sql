SELECT
    r.VALUE:"HEADLINE"::VARCHAR AS Headline,
    r.VALUE:"PUBLISHER"::VARCHAR AS Publisher,
    r.VALUE:"DATE"::DATE AS Date,
    r.VALUE:"STOCK"::VARCHAR AS Stock,
    r.VALUE:"SENTIMENT"::VARCHAR AS Sentiment,
    'cosine: ' || COALESCE(TO_CHAR(ROUND(r.VALUE:"@scores"."cosine_similarity"::FLOAT, 4), 'FM9999990.0000'), 'n/a')
        || ', text: ' || COALESCE(TO_CHAR(ROUND(r.VALUE:"@scores"."text_match"::FLOAT, 4), 'FM9999990.0000'), 'n/a')
        || ', reranker: ' || COALESCE(TO_CHAR(ROUND(r.VALUE:"@scores"."reranker_score"::FLOAT, 4), 'FM9999990.0000'), 'n/a') AS SearchScore
FROM TABLE(FLATTEN(input => PARSE_JSON(
  SNOWFLAKE.CORTEX.SEARCH_PREVIEW(
      'STOCK_NEWS_SEARCH_SERVICE',
      '__queryParametersJson__'
  )
)['results'])) r
