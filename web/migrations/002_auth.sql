CREATE TABLE IF NOT EXISTS profile_secrets (
  profile_id TEXT PRIMARY KEY REFERENCES profiles(id) ON DELETE CASCADE,
  secret_hash TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS link_codes (
  code TEXT PRIMARY KEY,
  profile_id TEXT NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,
  expires_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS treasure_hours (
  start_utc TEXT PRIMARY KEY,
  end_utc TEXT NOT NULL,
  gold REAL NOT NULL DEFAULT 1,
  shop REAL NOT NULL DEFAULT 1,
  vehicle REAL NOT NULL DEFAULT 1,
  confirmed INTEGER NOT NULL DEFAULT 1
);

ALTER TABLE matches ADD COLUMN chest_times_json TEXT;
