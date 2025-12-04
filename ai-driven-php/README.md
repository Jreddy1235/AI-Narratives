# Neon Bingo AI (Hackathon Prototype)

Educational, AI-directed bingo prototype inspired by social casino games. Built with PHP 8, MySQL, Bootstrap 5, vanilla JS, and OpenAI.

## Tech Stack

- PHP 8+
- MySQL 5.7+/8
- Apache (XAMPP/LAMP/WAMP) or built-in PHP server
- Bootstrap 5, Chart.js
- OpenAI Chat Completions API (via PHP cURL)

## Folder Structure

- `public/`
  - `index.html` – Landing page
  - `login.html` – Guest login
  - `lobby.html` – Room selection
  - `room.html` – Bingo gameplay
  - `admin.html` – Analytics dashboard
  - `styles.css` – Dark neon theme
  - `js/bot.js` – Floating AI coach UI
  - `js/game.js` – Front-end bingo logic
  - `js/admin.js` – Dashboard charts
- `api/`
  - `login.php`
  - `start_game.php`
  - `submit_move.php`
  - `analytics.php`
  - `ai_decision.php`
  - `reward.php`
  - `get_dashboard.php`
- `config/`
  - `db.php` – PDO connection helper
  - `openai.php` – OpenAI client via cURL
- `sql/schema.sql` – MySQL schema + sample data
- `.env.example` – OpenAI key template

## Setup (XAMPP / LAMP / WAMP)

1. Clone or copy this folder into your web root, e.g. `htdocs/ai-driven`.
2. Create database:
   - Import `sql/schema.sql` into MySQL (phpMyAdmin or CLI).
3. Configure DB connection:
   - Edit `config/db.php` with your MySQL user/password if different from defaults.
4. Configure OpenAI:
   - Copy `.env.example` to `.env` (or set env var at server level).
   - Set `OPENAI_API_KEY` to a valid key.
5. Start server:
   - With Apache: access `http://localhost/ai-driven/public/index.html`.
   - Or built-in PHP server (from project root):
     - `php -S localhost:8000` and open `http://localhost:8000/public/index.html`.

## How It Works

- **Rooms:** Beginner, Intermediate, High-Stakes – selected from `lobby.html`.
- **Gameplay:**
  - `start_game.php` creates a 5x5 card and starts a game log row.
  - `submit_move.php` draws random numbers and checks bingo lines/full house using the `marked` array.
  - Front-end `game.js` manages the grid UI, marks, and polls numbers.
- **Analytics:**
  - `analytics.php` logs events (marks, powerups, exits) into `game_logs` with JSON payload.
- **AI Decision System:**
  - `ai_decision.php` pulls recent analytics and calls OpenAI with a JSON-only prompt.
  - OpenAI returns emotional state, room recommendation, reward suggestion, difficulty hint, and `bot_message`.
  - `bot.js` uses this for the floating coach.
- **Rewards & Difficulty:**
  - `reward.php` sends user-centric analytics to OpenAI.
  - Model decides whether to grant a comeback bonus / reward and coin amount.
  - If granted, `coin_transactions` is updated and decision logged in `ai_decisions`.
- **Admin Dashboard:**
  - `admin.html` calls `get_dashboard.php` and plots:
    - Active users, rage quits, AI reward decisions, net coin flow.
    - Room heatmap and win/loss trends using Chart.js.

## Notes & Limitations

- This is a **hackathon prototype**, not production-ready.
- No real-money gambling, branding, or copyrighted assets. Purely educational demo.
- Game state is simplified (best-effort logging and RNG-based caller).
- All high-level room/reward/break decisions are delegated to OpenAI via prompts.
