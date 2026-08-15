-- S3 bucket
COPY INTO mytable FROM 's3://mybucket/./../a.csv';

-- Google Cloud Storage bucket
COPY INTO mytable FROM 'gcs://mybucket/./../a.csv';

-- Azure container
COPY INTO mytable FROM 'azure://myaccount.blob.core.windows.net/mycontainer/./../a.csv';