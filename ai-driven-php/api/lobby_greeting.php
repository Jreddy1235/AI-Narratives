<?php
require __DIR__ . '/../config/db.php';
require __DIR__ . '/../config/openai.php';
header('Content-Type: application/json');

$input = json_decode(file_get_contents('php://input'), true) ?? [];
$userId = (int)($input['user_id'] ?? 0);

if (!$userId) {
    echo json_encode(['success' => false, 'error' => 'Missing user_id']);
    exit;
}

try {
    $pdo = DB::getConnection();
    
    // Get user's recent game
    $lastGame = $pdo->query("
        SELECT win_type, coins_won, line_bonus, status, room 
        FROM games 
        WHERE user_id = $userId 
        ORDER BY ended_at DESC 
        LIMIT 1
    ")->fetch(PDO::FETCH_ASSOC);
    
    // Get user's room preferences
    $roomStats = $pdo->query("
        SELECT room_id, games_played, games_won, total_coins_won, last_played_at
        FROM room_stats
        WHERE user_id = $userId
        ORDER BY last_played_at DESC
        LIMIT 5
    ")->fetchAll(PDO::FETCH_ASSOC);
    
    // Get user profile
    $user = $pdo->query("
        SELECT nickname, total_games_played, total_wins, play_style, favorite_room
        FROM users
        WHERE id = $userId
    ")->fetch(PDO::FETCH_ASSOC);
    
    $context = [
        'user' => $user,
        'last_game' => $lastGame,
        'room_history' => $roomStats
    ];
    
    // Build AI prompt
    $systemPrompt = 'You are an AI game director. Player just returned to lobby. '
        . 'Give: 1) Contextual greeting (1 sentence), 2) Room recommendation with reason. '
        . 'GREETING EXAMPLES: '
        . '- Full house win: "Wow! Full house! You\'re on fire!" / "Incredible win! That was amazing!" '
        . '- Timeout/no win: "Good effort! Ready for another round?" / "Almost had it! Try again?" '
        . '- Multiple lines: "Nice! X lines completed! Keep it up!" '
        . '- First time: "Welcome back! Ready to play?" '
        . 'RECOMMEND: Suggest a room based on their play style and recent performance. '
        . 'If they won: suggest same difficulty or harder. If they lost: suggest easier or different genre. '
        . 'Return JSON: {"bot_message": "greeting", "recommended_action": {"room_id": "...", "reason": "..."}}';
    
    $userPrompt = json_encode($context, JSON_PRETTY_PRINT);
    
    $client = new OpenAIClient();
    $result = $client->decide([
        'system_prompt' => $systemPrompt,
        'user_prompt' => $userPrompt
    ]);
    
    if (!empty($result['error'])) {
        echo json_encode(['success' => false, 'error' => $result['error']]);
        exit;
    }
    
    echo json_encode([
        'success' => true,
        'greeting' => $result['bot_message'] ?? 'Welcome back!',
        'recommendation' => $result['recommended_action'] ?? null
    ]);
    
} catch (Throwable $e) {
    echo json_encode(['success' => false, 'error' => $e->getMessage()]);
}
