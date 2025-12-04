// Simple front-end bingo logic; server still validates and logs via APIs
let gameState = {
  gameId: null,
  card: [],
  marked: [],
  room: '',
  coins: 0,
  actions: [],
  sessionStart: Date.now(),
  drawnNumbers: [],
  drawCount: 0,
  lastNumberTime: null,
  gameStartTime: null,
  gameTimerInterval: null,
  gameEnded: false,
  totalLineBonus: 0,
  linesCompleted: 0
};

function qs(name) {
  const url = new URL(window.location.href);
  return url.searchParams.get(name);
}

function renderCard() {
  const grid = document.getElementById('bingoCard');
  grid.innerHTML = '';
  gameState.card.forEach((num, idx) => {
    const cell = document.createElement('div');
    cell.className = 'bingo-cell';
    cell.dataset.index = idx;
    cell.textContent = num === 'FREE' ? '★' : num;
    if (gameState.marked[idx]) cell.classList.add('marked');
    cell.addEventListener('click', () => handleCellClick(idx));
    grid.appendChild(cell);
  });
}

function renderNumberHistory() {
  const bar = document.getElementById('numberHistory');
  if (!bar) return;
  bar.innerHTML = '';
  const recent = gameState.drawnNumbers.slice(-10).slice().reverse();
  recent.forEach((num) => {
     const b = document.createElement('div');
     b.className = 'number-history-ball';
     b.textContent = num;
     bar.appendChild(b);
  });
}

function updateCoins(delta) {
  gameState.coins += delta;
  document.getElementById('coinCount').textContent = gameState.coins;
  const topCoins = document.getElementById('roomTopCoins');
  if (topCoins) topCoins.textContent = gameState.coins;
}

function handleCellClick(idx) {
  const alreadyMarked = !!gameState.marked[idx];
  const value = gameState.card[idx];

  // Prevent unmarking - once marked, it stays marked
  if (alreadyMarked) {
    return; // Do nothing if already marked
  }

  // Only allow marking if this value has been called or is the FREE center
  const isFree = value === 'FREE';
  const hasBeenCalled = isFree || gameState.drawnNumbers.includes(value);
  if (!hasBeenCalled) {
    // small shake feedback on invalid pick
    const cellEl = document.querySelector(`.bingo-cell[data-index="${idx}"]`);
    if (cellEl) {
      cellEl.classList.add('shake');
      setTimeout(() => cellEl.classList.remove('shake'), 250);
    }
    return; // ignore clicks on numbers that have not been called yet
  }
  
  gameState.marked[idx] = true;

  // Calculate reaction time
  const reactionTime = gameState.lastNumberTime ? Date.now() - gameState.lastNumberTime : null;

  renderCard();
  logAction('mark_cell', { idx, marked: gameState.marked[idx] });
  sendMove('mark', { idx, reaction_time_ms: reactionTime });
}

function logAction(type, payload) {
  gameState.actions.push({ type, payload, ts: Date.now() });
  if (gameState.actions.length > 50) gameState.actions.shift();
}

async function startGame() {
  const room = qs('room') || 'beginner';
  const token = localStorage.getItem('session_token');
  if (!token) {
    window.location.href = '/public/login.html';
    return;
  }
  document.getElementById('statusText').textContent = 'Starting game…';
  const res = await fetch('/api/start_game.php', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ room, session_token: token })
  });
  const data = await res.json();
  if (!data.success) {
    document.getElementById('statusText').textContent = 'Failed to start game.';
    return;
  }
  gameState.room = room;
  gameState.card = data.card;
  gameState.marked = data.marked;
  gameState.gameId = data.game_id;
  gameState.coins = data.coins;
  gameState.drawnNumbers = [];
  gameState.drawCount = 0;
  document.getElementById('coinCount').textContent = gameState.coins;
  document.getElementById('roomInfo').textContent = room.toUpperCase() + ' ROOM';
  renderCard();
  logAction('start_game', { room });
  const btn = document.getElementById('startGameBtn');
  if (btn) btn.textContent = 'Restart Game';
  renderNumberHistory();
  
  // Start game timer (2 minutes)
  gameState.gameStartTime = Date.now();
  gameState.gameEnded = false;
  gameState.totalLineBonus = 0;
  gameState.linesCompleted = 0;
  startGameTimer();
  
  // Welcome message on game start
  maybeAskAICoach('game_start', { room });
  
  scheduleNextNumber();
}

function startGameTimer() {
  // Clear any existing timer
  if (gameState.gameTimerInterval) {
    clearInterval(gameState.gameTimerInterval);
  }
  
  const timerDisplay = document.getElementById('gameTimer');
  const timerPill = timerDisplay?.closest('.metric-pill');
  const maxTime = 2 * 60 * 1000; // 3 minutes in milliseconds
  
  gameState.gameTimerInterval = setInterval(() => {
    const elapsed = Date.now() - gameState.gameStartTime;
    const remaining = Math.max(0, maxTime - elapsed);
    
    const minutes = Math.floor(remaining / 60000);
    const seconds = Math.floor((remaining % 60000) / 1000);
    
    if (timerDisplay) {
      timerDisplay.textContent = `${minutes}:${seconds.toString().padStart(2, '0')}`;
    }
    
    // Visual warnings
    if (timerPill) {
      timerPill.classList.remove('timer-warning', 'timer-danger');
      if (remaining <= 30000 && remaining > 0) {
        timerPill.classList.add('timer-danger'); // Last 30 seconds
      } else if (remaining <= 60000 && remaining > 30000) {
        timerPill.classList.add('timer-warning'); // Last minute
      }
    }
    
    // End game when timer runs out
    if (remaining === 0 && !gameState.gameEnded) {
      endGameByTimer();
    }
  }, 100);
}

function endGameByTimer() {
  gameState.gameEnded = true;
  clearInterval(gameState.gameTimerInterval);
  
  document.getElementById('statusText').textContent = 'Time\'s up! Game over.';
  
  // Show results for timeout
  showGameResults({
    win_type: null,
    coins_delta: 0,
    status_text: 'Time ran out!'
  });
}

function showGameResults(data) {
  const totalCoins = gameState.totalLineBonus + (data.coins_delta || 0);
  const winType = data.win_type || 'timeout';
  
  // Store result for lobby greeting
  localStorage.setItem('last_game_result', JSON.stringify({
    win_type: winType,
    total_coins: totalCoins,
    lines_completed: gameState.linesCompleted,
    timestamp: Date.now()
  }));
  
  let resultHTML = `
    <div style="position: fixed; top: 0; left: 0; right: 0; bottom: 0; background: rgba(0,0,0,0.9); z-index: 9999; display: flex; align-items: center; justify-content: center;">
      <div style="background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px; border-radius: 20px; text-align: center; max-width: 500px; box-shadow: 0 20px 60px rgba(0,0,0,0.5);">
        <h2 style="font-size: 2.5rem; margin-bottom: 20px; color: #fff;">
          ${winType === 'full_house' ? '🏆 FULL HOUSE! 🏆' : winType === 'timeout' ? '⏰ Time\'s Up!' : '🎮 Game Over'}
        </h2>
        <div style="background: rgba(255,255,255,0.1); padding: 20px; border-radius: 10px; margin: 20px 0;">
          <div style="font-size: 1.2rem; margin: 10px 0; color: #fff;">
            <strong>Lines Completed:</strong> ${gameState.linesCompleted}
          </div>
          <div style="font-size: 1.2rem; margin: 10px 0; color: #fff;">
            <strong>Line Bonuses:</strong> +${gameState.totalLineBonus} coins
          </div>
          ${data.coins_delta > 0 ? `<div style="font-size: 1.2rem; margin: 10px 0; color: #ffd700;">
            <strong>Full House Bonus:</strong> +${data.coins_delta} coins
          </div>` : ''}
          <div style="font-size: 1.8rem; margin-top: 15px; padding-top: 15px; border-top: 2px solid rgba(255,255,255,0.3); color: #ffd700;">
            <strong>Total Earned:</strong> ${totalCoins} coins
          </div>
        </div>
        <button onclick="window.location.href='/public/lobby.html'" style="background: #fff; color: #667eea; border: none; padding: 15px 40px; border-radius: 25px; font-size: 1.2rem; font-weight: bold; cursor: pointer; margin-top: 20px; box-shadow: 0 4px 15px rgba(0,0,0,0.2);">
          Return to Lobby
        </button>
      </div>
    </div>
  `;
  
  document.body.insertAdjacentHTML('beforeend', resultHTML);
  
  // AI celebration
  if (window.AICoach && typeof window.AICoach.setMessage === 'function') {
    if (winType === 'full_house') {
      maybeAskAICoach('win', { win_type: 'full_house', coins_delta: totalCoins });
    } else {
      window.AICoach.setMessage(`Game over! You earned ${totalCoins} coins total.`);
    }
  }
}

function scheduleNextNumber() {
  const delay = 2500;
  setTimeout(drawNumber, delay);
}

async function drawNumber() {
  if (!gameState.gameId || gameState.gameEnded) return;
  const res = await fetch('/api/submit_move.php', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      game_id: gameState.gameId,
      action: 'draw',
      marked: gameState.marked
    })
  });
  const data = await res.json();
  if (!data.success) return;
  const num = data.number;
  gameState.lastNumberTime = Date.now();
  gameState.drawnNumbers.push(num);
  gameState.drawCount += 1;
  document.getElementById('currentNumber').textContent = num;
  renderNumberHistory();
  document.getElementById('statusText').textContent = data.status_text || 'Match numbers as they are called.';
  
  // Handle line bonus
  if (data.line_bonus && data.line_bonus > 0) {
    gameState.totalLineBonus += data.line_bonus;
    gameState.linesCompleted += 1;
    updateCoins(data.line_bonus);
    maybeAskAICoach('line', { line_bonus: data.line_bonus });
  }
  
  // Handle full house win or other coins
  if (data.coins_delta && data.coins_delta > 0) {
    updateCoins(data.coins_delta);
  }
  
  // Only end game on full house
  if (data.game_ended) {
    gameState.gameEnded = true;
    clearInterval(gameState.gameTimerInterval);
    
    showGameResults(data);
  } else {
    maybeAskAICoach('draw', { win_type: null });
    scheduleNextNumber();
  }
}

async function maybeAskAICoach(trigger, extra) {
  // Only comment on: game_start (once), every 8 draws, line completions, and wins
  const isWin = !!(extra && extra.win_type);
  const isGameStart = trigger === 'game_start';
  const isLine = trigger === 'line';
  if (!isWin && !isGameStart && !isLine && gameState.drawCount % 8 !== 0) return;

  try {
    const userId = localStorage.getItem('user_id');
    const token = localStorage.getItem('session_token');
    
    // Calculate progress for context
    const markedCount = gameState.marked.filter(m => m).length;
    const totalCells = 25;
    const progress = Math.round((markedCount / totalCells) * 100);
    
    const res = await fetch('/api/ai_decision.php', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        mode: 'decision',
        trigger,
        user_id: userId ? parseInt(userId) : null,
        game_id: gameState.gameId,
        session_token: token,
        last_number: gameState.drawnNumbers[gameState.drawnNumbers.length - 1] || null,
        win_type: extra && extra.win_type ? extra.win_type : null,
        progress_percent: progress,
        marked_count: markedCount,
        numbers_called: gameState.drawCount,
        actions: gameState.actions.slice(-10),
        session_duration_ms: Date.now() - gameState.sessionStart
      })
    });
    const data = await res.json();
    if (data && data.bot_message && window.AICoach && typeof window.AICoach.setMessage === 'function') {
      window.AICoach.setMessage(data.bot_message);
    }
  } catch (e) {
    // ignore AI errors for gameplay
    console.error('AI Coach error:', e);
  }
}

async function sendMove(type, extra) {
  if (!gameState.gameId) return;
  await fetch('/api/analytics.php', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      event: type,
      game_id: gameState.gameId,
      room: gameState.room,
      extra,
      reaction_time_ms: extra.reaction_time_ms || null,
      actions: gameState.actions.slice(-10),
      session_duration_ms: Date.now() - gameState.sessionStart
    })
  });
}

window.addEventListener('DOMContentLoaded', () => {
  const room = qs('room') || 'beginner';
  document.body.classList.add('room-' + room);
  document.getElementById('roomInfo').textContent = room.toUpperCase() + ' ROOM';
  document.getElementById('startGameBtn').addEventListener('click', startGame);
  document.getElementById('exitRoomBtn').addEventListener('click', () => {
    logAction('exit_room', {});
    window.location.href = '/public/lobby.html';
  });

  document.getElementById('powerupHint').addEventListener('click', () => {
    logAction('powerup_hint', {});
    sendMove('powerup_hint', {});
  });
  document.getElementById('powerupFreeMark').addEventListener('click', () => {
    logAction('powerup_free_mark', {});
    sendMove('powerup_free_mark', {});
  });
  document.getElementById('powerupSlow').addEventListener('click', () => {
    logAction('powerup_slow', {});
    sendMove('powerup_slow', {});
  });

  // auto-start first game so entering a room goes straight into play
  startGame();
});
