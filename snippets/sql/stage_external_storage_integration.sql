CREATE STORAGE INTEGRATION s3_integration
  TYPE = EXTERNAL_STAGE
  STORAGE_PROVIDER = 'S3'
  ENABLED = TRUE
  STORAGE_AWS_ROLE_ARN = 'arn:aws:iam::123456789012:role/snowflake-s3-role'
  STORAGE_ALLOWED_LOCATIONS = ('s3://my-company-data/orders/');

CREATE STAGE orders_stage
  URL = 's3://my-company-data/orders/'
  STORAGE_INTEGRATION = s3_integration;