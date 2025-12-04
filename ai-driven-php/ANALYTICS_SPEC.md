# Analytics Data Collection Specification

## Overview
This document defines what data we collect, how it's stored, and how the AI uses it for personalized interactions.

---

## 1. User Profile Data (`users` table)

### Fields We Track:
- **`play_style`**: `slow` | `medium` | `fast`
  - Calculated from `avg_reaction_time_ms`
  - `slow`: >3000ms, `medium`: 1000-3000ms, `fast`: <1000ms
  
- **`total_games_played`**, **`total_wins`**, **`total_losses`**
  - Updated after each game ends
  
- **`last_seen_at`**
  - Updated on every login
  - Used to detect returning vs new users
  
- **`favorite_room`**
  - Room with highest `preference_score` in `room_stats`
  
- **`avg_reaction_time_ms`**
  - Rolling average of time between number call and mark

### AI Scenarios Enabled:
✅ **Welcome messages**:
- New user (no `last_seen_at`): "Welcome! Let me show you around."
- Returning user (last_seen < 24h): "Welcome back! Ready to continue?"
- Long absence (last_seen > 7 days): "Hey! It's been a while. Let's ease back in."

✅ **Play style coaching**:
- Fast player: "You're quick! Try a harder room for more coins."
- Slow player: "Take your time—accuracy beats speed here."

---

## 2. Game Summary Data (`games` table)

### Per-Game Metrics:
- **`status`**: `ongoing` | `won` | `lost` | `abandoned`
- **`win_type`**: `single_line` | `full_house` | `null`
- **`total_numbers_called`**: How many numbers before win/loss
- **`total_marks`**, **`correct_marks`**, **`incorrect_marks`**
- **`powerups_used`**: JSON array `["hint", "free_mark"]`
- **`game_duration_ms`**: Total time from start to end
- **`avg_mark_reaction_ms`**: Average reaction time for this game
- **`coins_won`**: Reward for this game

### AI Scenarios Enabled:
✅ **Streak detection**:
- 3+ wins in a row: "Amazing streak! Want to try a tougher room?"
- 3+ losses in a row: "Tough luck. Let's try an easier room or take a break."

✅ **Performance feedback**:
- Fast win (<10 numbers): "Lightning fast! You're a pro."
- Slow win (>30 numbers): "Patience paid off! Nice work."

---

## 3. Action Timeline Data (`game_actions` table)

### Per-Action Records:
- **`action_type`**: 
  - `number_called`: New number drawn
  - `cell_marked`: Player marked a cell
  - `powerup_used`: Player used a powerup
  - `game_ended`: Game finished
  
- **`action_data`**: JSON with details
  ```json
  {
    "number": 42,
    "cell_index": 12,
    "powerup_type": "hint",
    "was_correct": true
  }
  ```
  
- **`reaction_time_ms`**: Time since last `number_called` action
- **`created_at`**: Precise timestamp

### AI Scenarios Enabled:
✅ **Idle detection**:
- No `cell_marked` for 3+ `number_called` actions: "Still there? Take your time!"

✅ **Accuracy tracking**:
- High incorrect marks: "Watch the called numbers—only mark those!"

✅ **Powerup coaching**:
- Never uses powerups: "Try the AI Hint button if you're stuck."
- Overuses powerups: "You're doing great—trust your instincts more!"

---

## 4. Room Data (`rooms` + `room_stats` tables)

### Room Metadata (`rooms`):
- **`category`**: `classic` | `themed` | `challenge` | `seasonal`
- **`difficulty`**: `easy` | `medium` | `hard`
- **`is_active`**: Can be toggled for seasonal rooms

### Per-User Room Stats (`room_stats`):
- **`games_played`**, **`games_won`**
- **`total_coins_won`**
- **`last_played_at`**
- **`preference_score`**: Calculated as:
  ```
  (games_won / games_played) * 0.5 + 
  (total_coins_won / 1000) * 0.3 + 
  recency_bonus * 0.2
  ```

### AI Scenarios Enabled:
✅ **Dynamic room ordering**:
- Lobby shows rooms sorted by `preference_score` DESC
- User sees their best-performing rooms first

✅ **Room recommendations**:
- After a win: "You crushed 'Cozy Lanterns'! Want to go back?" [Button: Play Again | Not Now]
- After a loss in hard room: "Try 'Beginner Grove' to rebuild confidence."

✅ **Category-based suggestions**:
- Loves `themed` rooms: "New themed room 'Sunset Beach' just opened!"
- Avoids `challenge` rooms: "Feeling brave? Try a challenge room for 2x coins."

---

## 5. Data Collection Flow

### On Login (`api/login.php`):
```php
UPDATE users SET last_seen_at = NOW() WHERE id = ?
```

### On Game Start (`api/start_game.php`):
```php
INSERT INTO games (user_id, room, status, started_at) VALUES (?, ?, 'ongoing', NOW())
INSERT INTO game_actions (game_id, action_type, action_data) VALUES (?, 'game_started', '{}')
```

### On Number Draw (`api/submit_move.php`):
```php
INSERT INTO game_actions (game_id, action_type, action_data, created_at) 
VALUES (?, 'number_called', '{"number": 42}', NOW())

UPDATE games SET total_numbers_called = total_numbers_called + 1 WHERE id = ?
```

### On Cell Mark (`public/js/game.js` → `api/analytics.php`):
```php
$reactionTime = now - last_number_called_time
INSERT INTO game_actions (game_id, action_type, action_data, reaction_time_ms) 
VALUES (?, 'cell_marked', '{"cell_index": 12, "was_correct": true}', ?)

UPDATE games SET 
  total_marks = total_marks + 1,
  correct_marks = correct_marks + IF(was_correct, 1, 0),
  incorrect_marks = incorrect_marks + IF(!was_correct, 1, 0)
WHERE id = ?
```

### On Game End (`api/submit_move.php` when win detected):
```php
$duration = ended_at - started_at
$avgReaction = AVG(reaction_time_ms) FROM game_actions WHERE game_id = ?

UPDATE games SET 
  status = 'won',
  win_type = 'single_line',
  game_duration_ms = ?,
  avg_mark_reaction_ms = ?,
  coins_won = ?,
  ended_at = NOW()
WHERE id = ?

UPDATE users SET 
  total_games_played = total_games_played + 1,
  total_wins = total_wins + 1,
  avg_reaction_time_ms = (avg_reaction_time_ms * total_games_played + ?) / (total_games_played + 1)
WHERE id = ?

UPDATE room_stats SET 
  games_played = games_played + 1,
  games_won = games_won + 1,
  total_coins_won = total_coins_won + ?,
  last_played_at = NOW()
WHERE user_id = ? AND room_id = ?
```

---

## 6. AI Decision Payload Structure

### What We Send to `/api/ai_decision.php`:

```json
{
  "mode": "decision",
  "trigger": "game_start" | "draw" | "win" | "loss" | "idle" | "chat",
  
  "user_profile": {
    "is_new_user": false,
    "days_since_last_seen": 2,
    "total_games": 45,
    "win_rate": 0.67,
    "play_style": "fast",
    "favorite_room": "kiln_studio"
  },
  
  "current_game": {
    "room": "cozy_lanterns",
    "room_category": "themed",
    "numbers_called": 12,
    "marks_made": 8,
    "correct_marks": 7,
    "incorrect_marks": 1,
    "powerups_used": ["hint"],
    "duration_so_far_ms": 45000
  },
  
  "recent_performance": {
    "last_5_games": [
      {"room": "beginner", "status": "won", "win_type": "full_house"},
      {"room": "kiln_studio", "status": "won", "win_type": "single_line"},
      {"room": "relic_trail", "status": "lost"},
      {"room": "cozy_lanterns", "status": "won", "win_type": "single_line"},
      {"room": "beginner", "status": "won", "win_type": "full_house"}
    ],
    "current_streak": {"type": "win", "count": 2}
  },
  
  "room_preferences": [
    {"room": "beginner", "games": 15, "wins": 12, "preference_score": 0.85},
    {"room": "kiln_studio", "games": 10, "wins": 7, "preference_score": 0.72}
  ]
}
```

### What AI Returns:

```json
{
  "emotional_state": "confident" | "frustrated" | "neutral" | "excited",
  "bot_message": "Nice streak! Ready for a tougher challenge?",
  "should_suggest_break": false,
  "recommended_action": {
    "type": "room_suggestion" | "break" | "continue" | "powerup_tip",
    "room_id": "relic_trail",
    "button_text": "Try Challenge Room",
    "dismiss_text": "Not Now"
  },
  "reward": {
    "grant": true,
    "type": "streak_bonus",
    "coins": 25
  }
}
```

---

## 7. Implementation Priority

### Phase 1 (Current):
- ✅ Schema updated
- ⏳ Update `start_game.php` to use `games` table
- ⏳ Update `submit_move.php` to log `game_actions` and update `games`
- ⏳ Update `analytics.php` to log marks with reaction time
- ⏳ Update `login.php` to track `last_seen_at`

### Phase 2:
- Calculate `play_style` and `preference_score` in a cron job or on-demand
- Build rich AI payload in `ai_decision.php`
- Update `bot.js` to handle `recommended_action` with buttons

### Phase 3:
- Dynamic room ordering in lobby based on `preference_score`
- Seasonal room activation/deactivation
- Advanced idle/frustration detection

---

## 8. Example AI Interactions

### Scenario 1: Returning User After 5 Days
**Trigger**: Login  
**AI sees**: `days_since_last_seen: 5`, `total_games: 23`, `favorite_room: "kiln_studio"`  
**AI says**: "Welcome back! It's been a few days. Want to jump into Kiln Studio?"  
**Action**: [Play Kiln Studio] [Browse Rooms]

### Scenario 2: 3-Win Streak
**Trigger**: Win #3  
**AI sees**: `current_streak: {type: "win", count: 3}`, `current_room_difficulty: "easy"`  
**AI says**: "Amazing streak! You're crushing it. Ready for Relic Trail?"  
**Action**: [Try Hard Room] [Stay Here]

### Scenario 3: Slow Player, Many Incorrect Marks
**Trigger**: Mid-game (draw #10)  
**AI sees**: `incorrect_marks: 4`, `play_style: "slow"`, `powerups_used: []`  
**AI says**: "Take your time! Try the AI Hint if you need help spotting patterns."  
**Action**: None (just encouragement)

### Scenario 4: First-Time User
**Trigger**: Game start  
**AI sees**: `is_new_user: true`, `total_games: 0`  
**AI says**: "Welcome! I'm your AI coach. I'll help you learn and suggest rooms as you play."  
**Action**: None

---

This structure gives the AI everything it needs to be truly interactive and personalized!
