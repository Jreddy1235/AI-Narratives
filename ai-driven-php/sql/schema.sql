CREATE DATABASE IF NOT EXISTS bingo_ai CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE bingo_ai;

CREATE TABLE users (
  id INT AUTO_INCREMENT PRIMARY KEY,
  nickname VARCHAR(64) NOT NULL,
  created_at DATETIME NOT NULL,
  last_seen_at DATETIME NULL,
  total_games_played INT DEFAULT 0,
  total_wins INT DEFAULT 0,
  total_losses INT DEFAULT 0,
  play_style VARCHAR(32) NULL COMMENT 'slow|medium|fast',
  favorite_room VARCHAR(32) NULL,
  avg_reaction_time_ms INT NULL COMMENT 'avg time to mark after number called'
);

CREATE TABLE sessions (
  id INT AUTO_INCREMENT PRIMARY KEY,
  user_id INT NOT NULL,
  session_token VARCHAR(64) NOT NULL,
  started_at DATETIME NOT NULL,
  last_active_at DATETIME NOT NULL,
  FOREIGN KEY (user_id) REFERENCES users(id)
);

CREATE TABLE games (
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
  line_bonus INT DEFAULT 0 COMMENT 'bonus coins for completing lines',
  started_at DATETIME NOT NULL,
  ended_at DATETIME NULL,
  FOREIGN KEY (user_id) REFERENCES users(id)
);

CREATE TABLE game_actions (
  id INT AUTO_INCREMENT PRIMARY KEY,
  game_id INT NOT NULL,
  action_type VARCHAR(64) NOT NULL COMMENT 'number_called|cell_marked|powerup_used|game_ended',
  action_data JSON NULL COMMENT 'details like number, cell_index, powerup_type, etc',
  reaction_time_ms INT NULL COMMENT 'time since last number call',
  created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FOREIGN KEY (game_id) REFERENCES games(id)
);

CREATE TABLE rooms (
  id VARCHAR(32) PRIMARY KEY,
  name VARCHAR(64) NOT NULL,
  category VARCHAR(32) NOT NULL COMMENT 'classic|themed|challenge|seasonal',
  difficulty VARCHAR(32) NOT NULL COMMENT 'easy|medium|hard',
  description TEXT NULL,
  background_class VARCHAR(64) NULL,
  base_coins INT DEFAULT 100,
  is_active BOOLEAN DEFAULT TRUE
);

CREATE TABLE room_stats (
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

CREATE TABLE ai_decisions (
  id INT AUTO_INCREMENT PRIMARY KEY,
  decision_json JSON NOT NULL,
  created_at DATETIME NOT NULL
);

CREATE TABLE coin_transactions (
  id INT AUTO_INCREMENT PRIMARY KEY,
  user_id INT NOT NULL,
  amount INT NOT NULL,
  reason VARCHAR(255) NULL,
  created_at DATETIME NOT NULL
);

-- Sample data
INSERT INTO users (nickname, created_at) VALUES
 ('DemoPlayer', NOW());

INSERT INTO sessions (user_id, session_token, started_at, last_active_at) VALUES
 (1, 'demo-token', NOW(), NOW());

-- Insert rooms FIRST (required for foreign keys)
INSERT INTO rooms (id, name, category, difficulty, description, background_class, base_coins) VALUES
('cozy_lanterns', 'Cozy Lanterns', 'themed', 'easy', 'Warm lantern glow and gentle music for relaxed practice.', 'room-cozy-bg', 100),
('kiln_studio', 'Kiln Studio', 'themed', 'medium', 'Creative room with steady pace and pattern-focused tips.', 'room-kiln-bg', 100),
('relic_trail', 'Relic Trail', 'challenge', 'hard', 'Explore ancient tiles with faster calls and comeback rewards.', 'room-relic-bg', 100),
('beginner', 'Beginner Grove', 'classic', 'easy', 'Relaxed pace, extra hints from the AI coach.', 'room-beginner-bg', 100),
('intermediate', 'Neon Plaza', 'classic', 'medium', 'Balanced challenge with standard rewards.', 'room-intermediate-bg', 100),
('high', 'High-Stakes Vault', 'challenge', 'hard', 'Fast calls, risky swings, AI monitors closely.', 'room-high-bg', 100);

INSERT INTO games (user_id, room, status, win_type, total_numbers_called, started_at)
VALUES (1, 'beginner', 'won', 'single_line', 15, NOW());

INSERT INTO room_stats (user_id, room_id, games_played, games_won, last_played_at) VALUES
 (1, 'beginner', 1, 1, NOW());

INSERT INTO coin_transactions (user_id, amount, reason, created_at) VALUES
 (1, 50, 'Initial bonus', NOW());
