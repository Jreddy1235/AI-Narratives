<?php
require __DIR__ . '/../config/db.php';
header('Content-Type: application/json');

$input = json_decode(file_get_contents('php://input'), true) ?? [];
$gameId = (int)($input['game_id'] ?? 0);
$marked = $input['marked'] ?? [];

if (!$gameId) {
    echo json_encode(['success' => false, 'error' => 'Missing game_id']);
    exit;
}

// random number 1-75 for demo
$number = rand(1, 75);
$numberCalledAt = microtime(true);

// very simple win detection based only on marked cells
$winType = null;
if (is_array($marked) && count($marked) === 25) {
    $lines = [];
    // rows
    for ($r = 0; $r < 5; $r++) {
        $lines[] = [$r*5, $r*5+1, $r*5+2, $r*5+3, $r*5+4];
    }
    // cols
    for ($c = 0; $c < 5; $c++) {
        $lines[] = [$c, $c+5, $c+10, $c+15, $c+20];
    }
    // diagonals
    $lines[] = [0,6,12,18,24];
    $lines[] = [4,8,12,16,20];

    $hasLine = false;
    foreach ($lines as $line) {
        $all = true;
        foreach ($line as $idx) {
            if (empty($marked[$idx])) { $all = false; break; }
        }
        if ($all) { $hasLine = true; break; }
    }

    $allMarked = true;
    foreach ($marked as $m) {
        if (empty($m)) { $allMarked = false; break; }
    }

    if ($allMarked) {
        $winType = 'full_house';
    } elseif ($hasLine) {
        $winType = 'single_line';
    }
}

$coinsDelta = 0;
$statusText = 'Next number drawn. Keep playing.';
$gameStatus = 'ongoing';
$lineBonus = 0;

if ($winType === 'full_house') {
    $coinsDelta = 50;
    $statusText = 'Full house Bingo!';
    $gameStatus = 'won';
} elseif ($winType === 'single_line') {
    // Give bonus for line but continue playing
    $lineBonus = 20;
    $statusText = 'Line complete! Keep going for full house!';
    $gameStatus = 'ongoing'; // Continue playing
}

// Log number called action
try {
    $pdo = DB::getConnection();
    $pdo->prepare('INSERT INTO game_actions (game_id, action_type, action_data) VALUES (?, ?, ?)')
        ->execute([$gameId, 'number_called', json_encode(['number' => $number])]);
    
    // Update game summary
    $pdo->prepare('UPDATE games SET total_numbers_called = total_numbers_called + 1, win_type = COALESCE(win_type, ?), status = ?, line_bonus = line_bonus + ? WHERE id = ?')
        ->execute([$winType, $gameStatus, $lineBonus, $gameId]);
    
    // If game ended, finalize stats
    if ($gameStatus === 'won') {
        $gameData = $pdo->query("SELECT user_id, room, started_at, total_marks, correct_marks FROM games WHERE id = $gameId")->fetch(PDO::FETCH_ASSOC);
        $duration = (microtime(true) * 1000) - (strtotime($gameData['started_at']) * 1000);
        
        // Calculate avg reaction time from game_actions
        $avgReaction = $pdo->query("SELECT AVG(reaction_time_ms) as avg_rt FROM game_actions WHERE game_id = $gameId AND reaction_time_ms IS NOT NULL")->fetch(PDO::FETCH_ASSOC);
        
        $pdo->prepare('UPDATE games SET game_duration_ms = ?, avg_mark_reaction_ms = ?, coins_won = ?, ended_at = NOW() WHERE id = ?')
            ->execute([$duration, $avgReaction['avg_rt'] ?? 0, $coinsDelta, $gameId]);
        
        // Update user stats
        $pdo->prepare('UPDATE users SET total_games_played = total_games_played + 1, total_wins = total_wins + 1 WHERE id = ?')
            ->execute([$gameData['user_id']]);
        
        // Update room stats
        $pdo->prepare('UPDATE room_stats SET games_won = games_won + 1, total_coins_won = total_coins_won + ? WHERE user_id = ? AND room_id = ?')
            ->execute([$coinsDelta, $gameData['user_id'], $gameData['room']]);
        
        // Update coin transactions
        $pdo->prepare('INSERT INTO coin_transactions (user_id, amount, reason, created_at) VALUES (?, ?, ?, NOW())')
            ->execute([$gameData['user_id'], $coinsDelta, "Win: $winType"]);
    }
} catch (Throwable $e) {
    // best-effort only
    error_log('Game logging error: ' . $e->getMessage());
}

echo json_encode([
    'success' => true,
    'number' => $number,
    'win_type' => $winType,
    'coins_delta' => $coinsDelta,
    'line_bonus' => $lineBonus,
    'status_text' => $statusText,
    'game_ended' => $gameStatus === 'won'
]);
