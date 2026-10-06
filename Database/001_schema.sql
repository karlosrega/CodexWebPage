-- 001 / Administrative schema. Run only against A1IntegrationsAdmin.
-- Isolated namespace preserves existing objects outside a1admin.
BEGIN;
SELECT pg_advisory_xact_lock(418048);
CREATE SCHEMA IF NOT EXISTS a1admin;
CREATE TABLE IF NOT EXISTS a1admin.schema_versions (version integer PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS a1admin.users (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    username varchar(80) NOT NULL,
    display_name varchar(120) NOT NULL,
    email varchar(254) NOT NULL,
    password_hash text NOT NULL,
    active boolean NOT NULL DEFAULT true,
    failed_attempts integer NOT NULL DEFAULT 0 CHECK (failed_attempts >= 0),
    locked_until timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX IF NOT EXISTS users_username_unique ON a1admin.users (lower(username));
CREATE UNIQUE INDEX IF NOT EXISTS users_email_unique ON a1admin.users (lower(email));
CREATE TABLE IF NOT EXISTS a1admin.profiles (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    name varchar(80) NOT NULL,
    description varchar(300) NOT NULL DEFAULT '',
    active boolean NOT NULL DEFAULT true,
    is_system boolean NOT NULL DEFAULT false
);
CREATE UNIQUE INDEX IF NOT EXISTS profiles_name_unique ON a1admin.profiles (lower(name));
CREATE TABLE IF NOT EXISTS a1admin.permissions (code varchar(60) PRIMARY KEY, label varchar(120) NOT NULL);
CREATE TABLE IF NOT EXISTS a1admin.user_profiles (
    user_id integer NOT NULL REFERENCES a1admin.users(id),
    profile_id integer NOT NULL REFERENCES a1admin.profiles(id),
    PRIMARY KEY (user_id, profile_id)
);
CREATE INDEX IF NOT EXISTS user_profiles_profile ON a1admin.user_profiles(profile_id);
CREATE TABLE IF NOT EXISTS a1admin.profile_permissions (
    profile_id integer NOT NULL REFERENCES a1admin.profiles(id),
    permission_code varchar(60) NOT NULL REFERENCES a1admin.permissions(code),
    PRIMARY KEY (profile_id, permission_code)
);
CREATE TABLE IF NOT EXISTS a1admin.sessions (
    token_hash varchar(64) PRIMARY KEY,
    user_id integer NOT NULL REFERENCES a1admin.users(id),
    created_at timestamptz NOT NULL DEFAULT now(),
    expires_at timestamptz NOT NULL,
    revoked_at timestamptz
);
CREATE INDEX IF NOT EXISTS sessions_user ON a1admin.sessions(user_id);
CREATE INDEX IF NOT EXISTS sessions_expiry ON a1admin.sessions(expires_at);
CREATE TABLE IF NOT EXISTS a1admin.audit_events (
    id bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    actor_id integer REFERENCES a1admin.users(id),
    action varchar(80) NOT NULL,
    entity_id integer,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS audit_created ON a1admin.audit_events(created_at DESC);
CREATE TABLE IF NOT EXISTS a1admin.promotions (
    id integer GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    position integer NOT NULL,
    anchor varchar(40) NOT NULL UNIQUE,
    category varchar(80) NOT NULL,
    title varchar(160) NOT NULL,
    body text NOT NULL,
    image_path varchar(240) NOT NULL,
    product_url varchar(300) NOT NULL,
    active boolean NOT NULL DEFAULT true
);
INSERT INTO a1admin.schema_versions(version) VALUES (1) ON CONFLICT DO NOTHING;
COMMIT;
