-- Initialize TaxFiler Database
-- This script runs when the PostgreSQL container starts for the first time

-- Create additional database if needed (optional)
-- The main database 'taxfiler' is already created via POSTGRES_DB environment variable

-- Set timezone
SET timezone = 'Europe/Berlin';

-- Create extensions that might be useful
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Grant necessary permissions
GRANT ALL PRIVILEGES ON DATABASE taxfiler TO taxfiler_user;

-- Log initialization
DO $$
BEGIN
    RAISE NOTICE 'TaxFiler database initialized successfully at %', NOW();
END $$;
