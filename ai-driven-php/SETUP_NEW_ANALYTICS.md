# Setup Guide: Enhanced Analytics & AI

## What Changed

We've restructured the analytics system to enable much richer AI interactions:

### New Capabilities:
✅ **User profiling** - Track play style, win/loss records, favorite rooms  
✅ **Detailed game tracking** - Every action logged with reaction times  
✅ **Room categorization** - Classic, themed, challenge rooms with preferences  
✅ **Smart AI responses** - Welcome messages, streak detection, personalized suggestions  

---

## Setup Steps

### 1. Backup Your Current Database
```bash
mysqldump -u root -p bingo_ai > backup_$(date +%Y%m%d).sql
```

### 2. Run Migration Script
```bash
mysql -u root -p bingo_ai < sql/migrate_to_new_schema.sql
```

This will:
- Add new columns to `users` table
- Create `games`, `game_actions`, `rooms`, `room_stats` tables
- Migrate existing data from `game_logs` and `room_history`
- Calculate initial preference scores

### 3. Verify Tables
```bash
mysql -u root -p bingo_ai -e "SHOW TABLES;"
```

You should see:
- users (enhanced)
- sessions
- games (new)
- game_actions (new)
- rooms (new)
- room_stats (new)
- ai_decisions
- coin_transactions
- game_logs (old - can be dropped after verification)
- room_history (old - can be dropped after verification)

### 4. Test the Game
1. Open `http://localhost:8000/public/login.html`
2. Enter as guest
3. Play a game in any room
4. Watch the AI coach respond automatically every 5 draws and on wins
5. Try chatting with the AI coach

### 5. Verify Data Collection
```sql
-- Check if games are being logged
SELECT * FROM games ORDER BY id DESC LIMIT 5;

-- Check if actions are being tracked
SELECT * FROM game_actions ORDER BY id DESC LIMIT 10;

-- Check if room stats are updating
SELECT * FROM room_stats;

-- Check user profile updates
SELECT id, nickname, total_games_played, total_wins, play_style, last_seen_at FROM users;
```

---

## What the AI Now Knows

### On Every Interaction:
- **User Profile**: New vs returning, play style (slow/fast), win rate, favorite room
- **Current Game**: Room category, marks made, powerups used, reaction times
- **Recent Performance**: Last 5 games, current win/loss streak
- **Room Preferences**: Which rooms they play most and win in

### Example AI Behaviors:

#### New User (First Game)
```
Trigger: game_start
AI sees: is_new_user: true, total_games: 0
AI says: "Welcome! I'm your AI coach. I'll help you learn and suggest rooms as you play."
```

#### Returning User (After 5 Days)
```
Trigger: login
AI sees: days_since_last_seen: 5, favorite_room: "kiln_studio"
AI says: "Welcome back! It's been a few days. Want to jump into Kiln Studio?"
Action: [Play Kiln Studio] [Browse Rooms]
```

#### Win Streak (3 Wins)
```
Trigger: win
AI sees: current_streak: {type: "win", count: 3}, current_room_difficulty: "easy"
AI says: "Amazing streak! You're crushing it. Ready for Relic Trail?"
Action: [Try Hard Room] [Stay Here]
```

#### Loss Streak (3 Losses)
```
Trigger: loss
AI sees: current_streak: {type: "lost", count: 3}
AI says: "Tough luck. Let's try an easier room or take a break."
Action: [Try Easy Room] [Take Break]
```

#### Slow Player with Mistakes
```
Trigger: draw (mid-game)
AI sees: incorrect_marks: 4, play_style: "slow", powerups_used: []
AI says: "Take your time! Try the AI Hint if you need help spotting patterns."
```

---

## Troubleshooting

### AI Not Responding
1. Check OpenAI API key is set:
   ```bash
   echo $OPENAI_API_KEY
   ```

2. Check browser console for errors:
   - Open DevTools → Console
   - Look for "AI Coach error" messages

3. Check PHP error logs:
   ```bash
   tail -f /var/log/apache2/error.log  # or your PHP error log path
   ```

4. Test AI endpoint directly:
   ```bash
   curl -X POST http://localhost:8000/api/ai_decision.php \
     -H "Content-Type: application/json" \
     -d '{"mode":"decision","trigger":"test","user_id":1,"session_token":"demo-token"}'
   ```

### Data Not Being Collected
1. Check if `games` table is being populated:
   ```sql
   SELECT COUNT(*) FROM games;
   ```

2. Check if `game_actions` table is being populated:
   ```sql
   SELECT COUNT(*) FROM game_actions;
   ```

3. If empty, check PHP error logs for database errors

### Migration Issues
If migration fails:
1. Restore from backup:
   ```bash
   mysql -u root -p bingo_ai < backup_YYYYMMDD.sql
   ```

2. Check for conflicting table names
3. Manually run migration SQL statements one by one

---

## Next Steps

### Phase 2 Enhancements (Optional):
1. **Calculate play_style automatically**:
   - Add a cron job or scheduled task to update `users.play_style` based on `avg_reaction_time_ms`

2. **Dynamic room ordering in lobby**:
   - Update `lobby.html` to fetch rooms sorted by user's `preference_score`

3. **AI-driven room suggestions with buttons**:
   - Update `bot.js` to handle `recommended_action` from AI response
   - Show interactive buttons like "Try This Room" / "Not Now"

4. **Advanced idle detection**:
   - Track time between actions
   - AI nudges if player is idle for >30 seconds

5. **Seasonal rooms**:
   - Add/remove rooms by toggling `is_active` in `rooms` table
   - AI announces new rooms to users

---

## Files Modified

### Backend (PHP):
- ✅ `api/login.php` - Track last_seen_at
- ✅ `api/start_game.php` - Use games table, init room_stats
- ✅ `api/submit_move.php` - Log to game_actions, finalize game stats on win
- ✅ `api/analytics.php` - Log detailed actions with reaction times
- ✅ `api/ai_decision.php` - Use rich context from get_ai_context.php
- ✅ `api/get_ai_context.php` - NEW: Build comprehensive AI payload

### Frontend (JavaScript):
- ✅ `public/js/game.js` - Calculate reaction times, pass user_id/game_id to AI
- ✅ `public/js/bot.js` - Pass user_id/session_token to AI chat

### Database:
- ✅ `sql/schema.sql` - Complete new schema
- ✅ `sql/migrate_to_new_schema.sql` - Migration from old schema

### Documentation:
- ✅ `ANALYTICS_SPEC.md` - Comprehensive data collection spec
- ✅ `SETUP_NEW_ANALYTICS.md` - This file

---

## Success Checklist

- [ ] Database migrated successfully
- [ ] New tables exist and are populated
- [ ] Game starts and logs to `games` table
- [ ] Actions log to `game_actions` with reaction times
- [ ] AI coach responds automatically during gameplay
- [ ] AI chat works when typing to the avatar
- [ ] User stats update after games
- [ ] Room stats track preferences

---

You're all set! The AI now has rich context to provide truly personalized, interactive coaching. 🎉
