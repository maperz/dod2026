CREATE OR REPLACE NETWORK POLICY "DOD_NETWORK_POLICY"
    ALLOWED_IP_LIST = ('0.0.0.0/0')  -- allows all IPs; restrict in production
    COMMENT = 'Policy for programmatic access on DevOpsDays';

ALTER ACCOUNT SET NETWORK_POLICY = 'DOD_NETWORK_POLICY';