<?php
require __DIR__ . '/../config/db.php';
require __DIR__ . '/../config/openai.php';
require __DIR__ . '/get_ai_context.php';
header('Content-Type: application/json');

$input = json_decode(file_get_contents('php://input'), true) ?? [];
$mode = $input['mode'] ?? 'decision';
$message = $input['message'] ?? '';
$trigger = $input['trigger'] ?? 'unknown';
$userId = $input['user_id'] ?? null;
$gameId = $input['game_id'] ?? null;

try {
    // Get rich context if we have a user
    $context = null;
    if ($userId) {
        $context = getAIContext($userId, $gameId);
    } else {
        // Fallback: try to get user from session token
        $token = $input['session_token'] ?? '';
        if ($token) {
            $pdo = DB::getConnection();
            $session = $pdo->prepare('SELECT user_id FROM sessions WHERE session_token = ?');
            $session->execute([$token]);
            $sessionData = $session->fetch(PDO::FETCH_ASSOC);
            if ($sessionData) {
                $userId = $sessionData['user_id'];
                $context = getAIContext($userId, $gameId);
            }
        }
    }

    $systemPrompt = 'You are an AI game director and coach for an educational, non-monetary bingo game. '
        . 'The UI will surface your bot_message directly to the player during live play, so keep it very short '
        . '(1–2 sentences), reactive, and friendly. '
        . 'CRITICAL: NEVER say "welcome" or "keep it up" during draw triggers. Be SPECIFIC about the game state. '
        . 'TRIGGER-SPECIFIC RULES: '
        . '1. game_start: ONLY welcome messages based on user history: '
        . '   - New user (total_games=0): "Hi! I\'m your AI coach. Let\'s play!" '
        . '   - Returning same day: "Back for more? Let\'s go!" '
        . '   - Returning after days: "Hey! Missed you. Ready?" '
        . '2. draw: MUST comment on SPECIFIC game progress using marked_count and numbers_called: '
        . '   - marked_count 0-3: "Ouch, no matches yet!" / "Dry spell! Your numbers are coming." / "Zero hits so far—patience!" '
        . '   - marked_count 4-8: "A few hits! Momentum building." / "Nice, got some marks!" / "Warming up now!" '
        . '   - marked_count 9-15: "Halfway there! Line forming?" / "Getting spicy! Watch row 3." / "Ooh, diagonal looks good!" '
        . '   - marked_count 16-20: "SO close to bingo! One more!" / "Line is RIGHT there!" / "Next number could be it!" '
        . '   - marked_count 21-24: "ONE away from full house!" / "Final number! Come on!" / "Full card incoming!" '
        . '   ALSO vary by numbers_called: if numbers_called > 20 and marked_count < 10, say "Numbers not loving you today, huh?" or "Unlucky draw so far!"'
        . '3. line: Celebrate line completion but encourage continuing: '
        . '   - "Nice line! +20 coins! Keep going for full house!" / "Bingo line! Don\'t stop now!" / "First line down! Full house next?" '
        . '4. win: Celebrate full house with maximum enthusiasm: '
        . '   - full_house: "FULL HOUSE! You crushed it! 🏆" / "WOW! Complete card! Amazing!" / "Incredible! Full house win!" '
        . '   Add congratulations and be genuinely excited for the player.'
        . 'NEVER repeat the same phrase twice in a row. Be creative, casual, and human-like. '
        . 'Return strict JSON with: "emotional_state", "bot_message", "should_suggest_break", "recommended_action", "reward". '
        . 'Never mention money or gambling.';

    if ($mode === 'chat') {
        $userPrompt = json_encode([
            'type' => 'chat',
            'player_message' => $message,
            'context' => $context
        ], JSON_PRETTY_PRINT);
    } else {
        $userPrompt = json_encode([
            'type' => 'decision',
            'trigger' => $trigger,
            'context' => $context,
            'additional_data' => [
                'last_number' => $input['last_number'] ?? null,
                'win_type' => $input['win_type'] ?? null,
                'progress_percent' => $input['progress_percent'] ?? 0,
                'marked_count' => $input['marked_count'] ?? 0,
                'numbers_called' => $input['numbers_called'] ?? 0
            ]
        ], JSON_PRETTY_PRINT);
    }

    $client = new OpenAIClient();
    $result = $client->decide([
        'system_prompt' => $systemPrompt,
        'user_prompt' => $userPrompt
    ]);

    if (!empty($result['error'])) {
        echo json_encode(['success' => false, 'error' => $result['error']]);
        exit;
    }

    // store in ai_decisions
    $pdo = DB::getConnection();
    $stmt = $pdo->prepare('INSERT INTO ai_decisions (decision_json, created_at) VALUES (?, NOW())');
    $stmt->execute([json_encode($result, JSON_THROW_ON_ERROR)]);

    echo json_encode(array_merge(['success' => true], $result));
} catch (Throwable $e) {
    http_response_code(500);
    echo json_encode(['success' => false, 'error' => $e->getMessage()]);
}
