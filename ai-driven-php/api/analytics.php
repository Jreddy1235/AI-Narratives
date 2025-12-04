<?php
require_once __DIR__ . '/../config/db.php';

$input = json_decode(file_get_contents('php://input'), true) ?? [];
$pdo = DB::getConnection();
$event = $input['event'] ?? 'unknown';
$gameId = $input['game_id'] ?? null;
$room = $input['room'] ?? null;
$extra = $input['extra'] ?? [];
$actions = $input['actions'] ?? [];
$sessionDuration = $input['session_duration_ms'] ?? null;
$reactionTime = $input['reaction_time_ms'] ?? null;

try {
    // Map event types to action types
    $actionType = match($event) {
        'mark_cell' => 'cell_marked',
        'powerup_hint', 'powerup_free_mark', 'powerup_slow' => 'powerup_used',
        'exit_room' => 'game_ended',
        default => $event
    };
    
    // Prepare action data
    $actionData = $extra;
    if ($event === 'mark_cell' && isset($extra['idx'])) {
        $actionData['cell_index'] = $extra['idx'];
        $actionData['was_correct'] = $extra['marked'] ?? true;
    } elseif (str_starts_with($event, 'powerup_')) {
        $actionData['powerup_type'] = str_replace('powerup_', '', $event);
    }
    
    // Log to game_actions
    if ($gameId) {
        $stmt = $pdo->prepare('INSERT INTO game_actions (game_id, action_type, action_data, reaction_time_ms) VALUES (?, ?, ?, ?)');
        $stmt->execute([$gameId, $actionType, json_encode($actionData), $reactionTime]);
        
        // Update game summary for marks
        if ($event === 'mark_cell') {
            $wasCorrect = $extra['marked'] ?? true;
            $pdo->prepare(
                'UPDATE games SET '
                . 'total_marks = total_marks + 1, '
                . 'correct_marks = correct_marks + ?, '
                . 'incorrect_marks = incorrect_marks + ? '
                . 'WHERE id = ?'
            )->execute([$wasCorrect ? 1 : 0, $wasCorrect ? 0 : 1, $gameId]);
        }
        
        // Track powerup usage
        if (str_starts_with($event, 'powerup_')) {
            $game = $pdo->query("SELECT powerups_used FROM games WHERE id = $gameId")->fetch(PDO::FETCH_ASSOC);
            $powerups = json_decode($game['powerups_used'] ?? '[]', true);
            $powerups[] = str_replace('powerup_', '', $event);
            $pdo->prepare('UPDATE games SET powerups_used = ? WHERE id = ?')
                ->execute([json_encode($powerups), $gameId]);
        }
    }
} catch (Throwable $e) {
    error_log('Analytics error: ' . $e->getMessage());
    http_response_code(500);
    echo json_encode(['success' => false, 'error' => $e->getMessage()]);
}
