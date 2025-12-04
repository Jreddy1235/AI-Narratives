-- Migration script to update existing bingo_ai database to new schema
-- Run this AFTER backing up your database
-- Usage: mysql -u root -p bingo_ai < migrate_to_new_schema.sql

USE bingo_ai;

-- 1. Add new columns to users table (safe - will skip if column exists)
SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'bingo_ai' AND TABLE_NAME = 'users' AND COLUMN_NAME = 'last_seen_at');
SET @sqlstmt := IF(@exist = 0, 'ALTER TABLE users ADD COLUMN last_seen_at DATETIME NULL AFTER created_at', 'SELECT ''Column last_seen_at already exists''');
PREPARE stmt FROM @sqlstmt;
EXECUTE stmt;

SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'bingo_ai' AND TABLE_NAME = 'users' AND COLUMN_NAME = 'total_games_played');
SET @sqlstmt := IF(@exist = 0, 'ALTER TABLE users ADD COLUMN total_games_played INT DEFAULT 0 AFTER last_seen_at', 'SELECT ''Column total_games_played already exists''');
PREPARE stmt FROM @sqlstmt;
EXECUTE stmt;

SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'bingo_ai' AND TABLE_NAME = 'users' AND COLUMN_NAME = 'total_wins');
SET @sqlstmt := IF(@exist = 0, 'ALTER TABLE users ADD COLUMN total_wins INT DEFAULT 0 AFTER total_games_played', 'SELECT ''Column total_wins already exists''');
PREPARE stmt FROM @sqlstmt;
EXECUTE stmt;

SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'bingo_ai' AND TABLE_NAME = 'users' AND COLUMN_NAME = 'total_losses');
SET @sqlstmt := IF(@exist = 0, 'ALTER TABLE users ADD COLUMN total_losses INT DEFAULT 0 AFTER total_wins', 'SELECT ''Column total_losses already exists''');
PREPARE stmt FROM @sqlstmt;
EXECUTE stmt;

SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'bingo_ai' AND TABLE_NAME = 'users' AND COLUMN_NAME = 'play_style');
SET @sqlstmt := IF(@exist = 0, 'ALTER TABLE users ADD COLUMN play_style VARCHAR(32) NULL COMMENT ''slow|medium|fast'' AFTER total_losses', 'SELECT ''Column play_style already exists''');
PREPARE stmt FROM @sqlstmt;
EXECUTE stmt;

SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'bingo_ai' AND TABLE_NAME = 'users' AND COLUMN_NAME = 'favorite_room');
SET @sqlstmt := IF(@exist = 0, 'ALTER TABLE users ADD COLUMN favorite_room VARCHAR(32) NULL AFTER play_style', 'SELECT ''Column favorite_room already exists''');
PREPARE stmt FROM @sqlstmt;
EXECUTE stmt;

SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'bingo_ai' AND TABLE_NAME = 'users' AND COLUMN_NAME = 'avg_reaction_time_ms');
SET @sqlstmt := IF(@exist = 0, 'ALTER TABLE users ADD COLUMN avg_reaction_time_ms INT NULL COMMENT ''avg time to mark after number called'' AFTER favorite_room', 'SELECT ''Column avg_reaction_time_ms already exists''');
PREPARE stmt FROM @sqlstmt;
EXECUTE stmt;

-- 2. Create new games table (replaces game_logs for game summaries)
CREATE TABLE IF NOT EXISTS games (
  id INT AUTO_INCREMENT PRIMARY KEY,
  user_id INT NOT NULL,
  room VARCHAR(32) NOT NULL,
  status VARCHAR(32) NOT NULL DEFAULT 'ongoing' COMMENT 'ongoing|won|lost|abandoned',
  win_type VARCHAR(32) NULL COMMENT 'single_line|full_house',
  total_numbers_called INT DEFAULT 0,
  total_marks INT DEFAULT 0,
  correct_marks INT DEFAULT 0,
  incorrect_marks INT DEFAULT 0,
  powerups_used JSON NULL COMMENT 'array of powerup types used',
  game_duration_ms INT NULL,
  avg_mark_reaction_ms INT NULL COMMENT 'avg time between number call and mark',
  coins_won INT DEFAULT 0,
  started_at DATETIME NOT NULL,
  ended_at DATETIME NULL,
  FOREIGN KEY (user_id) REFERENCES users(id)
);

-- 3. Create game_actions table (detailed action log)
CREATE TABLE IF NOT EXISTS game_actions (
  id INT AUTO_INCREMENT PRIMARY KEY,
  game_id INT NOT NULL,
  action_type VARCHAR(64) NOT NULL COMMENT 'number_called|cell_marked|powerup_used|game_ended',
  action_data JSON NULL COMMENT 'details like number, cell_index, powerup_type, etc',
  reaction_time_ms INT NULL COMMENT 'time since last number call',
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (game_id) REFERENCES games(id)
);

-- 4. Create rooms table
CREATE TABLE IF NOT EXISTS rooms (
  id VARCHAR(32) PRIMARY KEY,
  name VARCHAR(64) NOT NULL,
  category VARCHAR(32) NOT NULL COMMENT 'classic|themed|challenge|seasonal',
  difficulty VARCHAR(32) NOT NULL COMMENT 'easy|medium|hard',
  description TEXT NULL,
  background_class VARCHAR(64) NULL,
  base_coins INT DEFAULT 100,
  is_active BOOLEAN DEFAULT TRUE
);

-- 5. Create room_stats table (replaces room_history for preferences)
CREATE TABLE IF NOT EXISTS room_stats (
  id INT AUTO_INCREMENT PRIMARY KEY,
  user_id INT NOT NULL,
  room_id VARCHAR(32) NOT NULL,
  games_played INT DEFAULT 0,
  games_won INT DEFAULT 0,
  total_coins_won INT DEFAULT 0,
  last_played_at DATETIME NULL,
  preference_score FLOAT DEFAULT 0 COMMENT 'calculated score for room ordering',
  FOREIGN KEY (user_id) REFERENCES users(id),
  FOREIGN KEY (room_id) REFERENCES rooms(id),
  UNIQUE KEY user_room (user_id, room_id)
);

-- 6. Populate rooms table with current rooms
INSERT IGNORE INTO rooms (id, name, category, difficulty, description, background_class, base_coins) VALUES
('cozy_lanterns', 'Cozy Lanterns', 'themed', 'easy', 'Warm lantern glow and gentle music for relaxed practice.', 'room-cozy-bg', 100),
('kiln_studio', 'Kiln Studio', 'themed', 'medium', 'Creative room with steady pace and pattern-focused tips.', 'room-kiln-bg', 100),
('relic_trail', 'Relic Trail', 'challenge', 'hard', 'Explore ancient tiles with faster calls and comeback rewards.', 'room-relic-bg', 100),
('beginner', 'Beginner Grove', 'classic', 'easy', 'Relaxed pace, extra hints from the AI coach.', 'room-beginner-bg', 100),
('intermediate', 'Neon Plaza', 'classic', 'medium', 'Balanced challenge with standard rewards.', 'room-intermediate-bg', 100),
('high', 'High-Stakes Vault', 'challenge', 'hard', 'Fast calls, risky swings, AI monitors closely.', 'room-high-bg', 100);

-- 7. Migrate existing game_logs data to new games table (if game_logs exists)
-- This is a best-effort migration - adjust as needed based on your data
INSERT IGNORE INTO games (id, user_id, room, status, win_type, started_at, ended_at)
SELECT 
  gl.game_id,
  gl.user_id,
  gl.room,
  CASE 
    WHEN gl.status = 'finished' AND gl.win_type IS NOT NULL THEN 'won'
    WHEN gl.status = 'finished' AND gl.win_type IS NULL THEN 'lost'
    ELSE 'ongoing'
  END as status,
  gl.win_type,
  gl.started_at,
  gl.ended_at
FROM game_logs gl
WHERE gl.game_id IS NOT NULL
  AND gl.user_id IS NOT NULL
  AND gl.room IS NOT NULL
  AND gl.started_at IS NOT NULL
ON DUPLICATE KEY UPDATE games.id=games.id;

-- 8. Initialize room_stats from room_history (if room_history exists)
INSERT IGNORE INTO room_stats (user_id, room_id, games_played, last_played_at)
SELECT 
  user_id,
  room as room_id,
  COUNT(*) as games_played,
  MAX(entered_at) as last_played_at
FROM room_history
WHERE room IN (SELECT id FROM rooms)
GROUP BY user_id, room
ON DUPLICATE KEY UPDATE 
  games_played = VALUES(games_played),
  last_played_at = VALUES(last_played_at);

-- 9. Update user stats from games table
UPDATE users u
SET 
  total_games_played = (SELECT COUNT(*) FROM games WHERE user_id = u.id AND status IN ('won', 'lost')),
  total_wins = (SELECT COUNT(*) FROM games WHERE user_id = u.id AND status = 'won'),
  total_losses = (SELECT COUNT(*) FROM games WHERE user_id = u.id AND status = 'lost')
WHERE EXISTS (SELECT 1 FROM games WHERE user_id = u.id);

-- 10. Calculate preference scores (simple formula)
UPDATE room_stats rs
SET preference_score = (
  CASE 
    WHEN rs.games_played > 0 THEN
      (rs.games_won / rs.games_played) * 0.5 +
      (rs.total_coins_won / 1000.0) * 0.3 +
      (CASE 
        WHEN DATEDIFF(NOW(), rs.last_played_at) < 7 THEN 0.2
        WHEN DATEDIFF(NOW(), rs.last_played_at) < 30 THEN 0.1
        ELSE 0
      END)
    ELSE 0
  END
);

-- Done! Your database is now ready for the enhanced AI features.
-- You can optionally drop old tables after verifying everything works:
-- DROP TABLE IF EXISTS game_logs;
-- DROP TABLE IF EXISTS room_history;
