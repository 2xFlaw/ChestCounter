-- Treasure Hunt "found" ticks shared by the app and the website. payload maps reward key to {v, t}.
CREATE TABLE IF NOT EXISTS owned_rewards (
  profile_id TEXT PRIMARY KEY REFERENCES profiles(id) ON DELETE CASCADE,
  payload TEXT NOT NULL,
  updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);
