<?php
require __DIR__ . '/../config/db.php';
header('Content-Type: application/json');

$input = json_decode(file_get_contents('php://input'), true) ?? [];
$room = $input['room'] ?? 'beginner';
$token = $input['session_token'] ?? '';

function getUserFromToken($token) {
    $pdo = DB::getConnection();
    $stmt = $pdo->prepare('SELECT * FROM sessions WHERE session_token = ?');
    $stmt->execute([$token]);
    $session = $stmt->fetch();
    if (!$session) return null;
    return $session;
}

$session = getUserFromToken($token);
if (!$session) {
    echo json_encode(['success' => false, 'error' => 'Invalid session']);
    exit;
}
$userId = (int)$session['user_id'];
$pdo = DB::getConnection();

// generate simple 5x5 card 1-75 with center FREE
$numbers = range(1,75);
shuffle($numbers);
$cardNums = array_slice($numbers, 0, 25);
$card = [];
for ($i=0;$i<25;$i++) {
    if ($i === 12) {
        $card[] = 'FREE';
    } else {
        $card[] = $cardNums[$i];
    }
}
$marked = array_fill(0,25,false);
$marked[12] = true;

// Log game start in new games table
$stmt = $pdo->prepare('INSERT INTO games (user_id, room, status, started_at) VALUES (?, ?, ?, NOW())');
$stmt->execute([$userId, $room, 'ongoing']);
$gameId = $pdo->lastInsertId();

// Log initial action
$pdo->prepare('INSERT INTO game_actions (game_id, action_type, action_data) VALUES (?, ?, ?)')
    ->execute([$gameId, 'game_started', json_encode(['room' => $room])]);

// Initialize or update room stats
$pdo->prepare(
    'INSERT INTO room_stats (user_id, room_id, games_played, last_played_at) '
    . 'VALUES (?, ?, 1, NOW()) '
    . 'ON DUPLICATE KEY UPDATE games_played = games_played + 1, last_played_at = NOW()'
)->execute([$userId, $room]);

// starting coins (could be AI controlled later)
$coins = 100;

echo json_encode([
    'success' => true,
    'game_id' => $gameId,
    'card' => $card,
    'marked' => $marked,
    'coins' => $coins
]);
