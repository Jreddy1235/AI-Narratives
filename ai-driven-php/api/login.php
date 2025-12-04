<?php
require __DIR__ . '/../config/db.php';
header('Content-Type: application/json');

$input = json_decode(file_get_contents('php://input'), true) ?? [];
$nickname = trim($input['nickname'] ?? 'Guest');

try {
    $pdo = DB::getConnection();
    
    // Check if user exists
    $existing = $pdo->prepare('SELECT id FROM users WHERE nickname = ?');
    $existing->execute([$nickname]);
    $user = $existing->fetch(PDO::FETCH_ASSOC);
    
    if (!$user) {
        // Create new user
        $stmt = $pdo->prepare('INSERT INTO users (nickname, created_at, last_seen_at) VALUES (?, NOW(), NOW())');
        $stmt->execute([$nickname]);
        $userId = (int)$pdo->lastInsertId();
    } else {
        // Update last seen for returning user
        $userId = (int)$user['id'];
        $pdo->prepare('UPDATE users SET last_seen_at = NOW() WHERE id = ?')->execute([$userId]);
    }

    $token = bin2hex(random_bytes(16));
    $pdo->prepare('INSERT INTO sessions (user_id, session_token, started_at, last_active_at) VALUES (?,?,NOW(),NOW())')
        ->execute([$userId, $token]);

    echo json_encode(['success' => true, 'user_id' => $userId, 'session_token' => $token]);
} catch (Throwable $e) {
    http_response_code(500);
    echo json_encode(['success' => false, 'error' => $e->getMessage()]);
}
