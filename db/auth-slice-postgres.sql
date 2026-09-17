-- =============================================================================
-- auth-slice-postgres.sql
-- Minimal AUTH-ONLY schema for the login vertical slice, for PostgreSQL (Render).
-- This is NOT the full system — it exists only to prove login end-to-end.
-- The full warehouse schema (PostgreSQL port of warehouse-db.sql) comes later.
--
-- Convention: snake_case (idiomatic for PostgreSQL; EF Core + Npgsql map to it).
--
-- PASSWORDS: password_hash below is a PLACEHOLDER. The API owns hashing — its
-- seeding step sets real hashes using ASP.NET Core's PasswordHasher for the demo
-- password (see SLICE-LOGIN.md). Do not put real passwords in SQL.
--
-- Load it with psql against the Render EXTERNAL database URL, e.g.:
--   psql "postgresql://USER:PASS@HOST/DBNAME?sslmode=require" -f auth-slice-postgres.sql
-- =============================================================================

DROP TABLE IF EXISTS admin_users;
DROP TABLE IF EXISTS customers;

CREATE TABLE admin_users (
    admin_id      INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    email         VARCHAR(200) NOT NULL UNIQUE,
    password_hash VARCHAR(255),
    full_name     VARCHAR(150),
    role          VARCHAR(50)  NOT NULL DEFAULT 'Owner',
    status        VARCHAR(20)  NOT NULL DEFAULT 'Active'
                     CHECK (status IN ('Active','Inactive')),
    created_at    TIMESTAMPTZ  NOT NULL DEFAULT now()
);

CREATE TABLE customers (
    customer_id    INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    account_number VARCHAR(30) UNIQUE,
    business_name  VARCHAR(200) NOT NULL,
    email          VARCHAR(200) NOT NULL UNIQUE,
    password_hash  VARCHAR(255),
    -- Login must be allowed only when status = 'Approved'
    status         VARCHAR(20)  NOT NULL DEFAULT 'Pending'
                     CHECK (status IN ('Pending','Approved','OnHold','Inactive','Rejected')),
    created_at     TIMESTAMPTZ  NOT NULL DEFAULT now()
);

-- Seed: one admin, one APPROVED customer (login should succeed),
-- one PENDING customer (login should be rejected — proves the approval gate).
INSERT INTO admin_users (email, password_hash, full_name, role, status) VALUES
 ('admin@stockroom.co', 'REPLACE_WITH_REAL_HASH', 'Amir M.', 'Owner', 'Active');

INSERT INTO customers (account_number, business_name, email, password_hash, status) VALUES
 ('ACC-10231', 'Mario''s Cafe',    'mario@mariocafe.com',       'REPLACE_WITH_REAL_HASH', 'Approved'),
 ('ACC-10355', 'Sunrise Grocers',  'priya@sunrisegrocers.com',  'REPLACE_WITH_REAL_HASH', 'Pending');

-- Quick check after loading:
--   SELECT email, status FROM admin_users;
--   SELECT email, status FROM customers;
