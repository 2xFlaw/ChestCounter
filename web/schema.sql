CREATE TABLE IF NOT EXISTS profiles (
  id TEXT PRIMARY KEY,
  username TEXT UNIQUE,
  username_changed_at TEXT,
  is_private INTEGER NOT NULL DEFAULT 0,
  starting_total INTEGER NOT NULL DEFAULT 0,
  created_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS linked_pcs (
  id TEXT PRIMARY KEY,
  profile_id TEXT NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,
  token_hash TEXT NOT NULL,
  created_at TEXT NOT NULL DEFAULT (datetime('now')),
  last_seen_at TEXT
);

CREATE TABLE IF NOT EXISTS matches (
  profile_id TEXT NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,
  session_id TEXT NOT NULL,
  started_at TEXT NOT NULL,
  mode TEXT,
  opened INTEGER NOT NULL DEFAULT 0,
  destroyed INTEGER NOT NULL DEFAULT 0,
  chest_types_json TEXT,
  PRIMARY KEY (profile_id, session_id)
);
CREATE INDEX IF NOT EXISTS idx_matches_started ON matches(started_at);
-- Teammate chests seen and match length, synced by app 2.4.3+ (created by the Worker when first needed).
CREATE TABLE IF NOT EXISTS match_extras (
  profile_id TEXT NOT NULL,
  session_id TEXT NOT NULL,
  mates INTEGER,
  duration_s INTEGER,
  PRIMARY KEY (profile_id, session_id)
);

CREATE TABLE IF NOT EXISTS friend_groups (
  id TEXT PRIMARY KEY,
  owner_profile_id TEXT NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,
  name TEXT NOT NULL,
  created_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS friend_group_members (
  group_id TEXT NOT NULL REFERENCES friend_groups(id) ON DELETE CASCADE,
  profile_id TEXT NOT NULL REFERENCES profiles(id) ON DELETE CASCADE,
  PRIMARY KEY (group_id, profile_id)
);

CREATE TABLE IF NOT EXISTS seasons (
  id TEXT PRIMARY KEY,
  name TEXT NOT NULL,
  starts_at TEXT NOT NULL,
  ends_at TEXT NOT NULL
);

-- Treasure Hunt "found" ticks shared by the app and the website. payload maps reward key to {v, t}.
CREATE TABLE IF NOT EXISTS owned_rewards (
  profile_id TEXT PRIMARY KEY REFERENCES profiles(id) ON DELETE CASCADE,
  payload TEXT NOT NULL,
  updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);
