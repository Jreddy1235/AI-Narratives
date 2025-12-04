<?php
require __DIR__ . '/../config/db.php';
require __DIR__ . '/../config/openai.php';
header('Content-Type: application/json');

$input = json_decode(file_get_contents('php://input'), true) ?? [];
$userId = (int)($input['user_id'] ?? 0);
$context = $input['context'] ?? [];

try {
    $pdo = DB::getConnection();

    $snapshot = [
        'coin_transactions' => $pdo->query('SELECT * FROM coin_transactions WHERE user_id = ' . (int)$userId . ' ORDER BY id DESC LIMIT 20')->fetchAll(),
        'recent_games' => $pdo->query('SELECT * FROM game_logs WHERE user_id = ' . (int)$userId . ' ORDER BY id DESC LIMIT 10')->fetchAll(),
        'context' => $context
    ];

    $systemPrompt = 'You control rewards and comeback bonuses for a non-monetary bingo game. '
        . 'Given analytics JSON, decide if you should grant a reward now. '
        . 'Respond with JSON: {"grant":bool,"type":string,"coins":int,"reason":string}.';

    $client = new OpenAIClient();
    $result = $client->decide([
        'system_prompt' => $systemPrompt,
        'user_prompt' => json_encode($snapshot, JSON_PRETTY_PRINT)
    ]);

    if (!empty($result['error'])) {
        echo json_encode(['success' => false, 'error' => $result['error']]);
        exit;
    }

    $grant = !empty($result['grant']);
    $coins = (int)($result['coins'] ?? 0);

    if ($grant && $userId && $coins !== 0) {
        $stmt = $pdo->prepare('INSERT INTO coin_transactions (user_id, amount, reason, created_at) VALUES (?,?,?,NOW())');
        $stmt->execute([$userId, $coins, $result['reason'] ?? 'AI reward']);
    }

    $stmt = $pdo->prepare('INSERT INTO ai_decisions (decision_json, created_at) VALUES (?, NOW())');
    $stmt->execute([json_encode($result, JSON_THROW_ON_ERROR)]);

    echo json_encode(['success' => true, 'reward' => $result]);
} catch (Throwable $e) {
    http_response_code(500);
    echo json_encode(['success' => false, 'error' => $e->getMessage()]);
}
