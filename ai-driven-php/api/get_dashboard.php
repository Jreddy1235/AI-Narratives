<?php
require __DIR__ . '/../config/db.php';
header('Content-Type: application/json');

try {
    $pdo = DB::getConnection();

    $activeUsers = (int)$pdo->query('SELECT COUNT(DISTINCT user_id) AS c FROM sessions WHERE last_active_at > NOW() - INTERVAL 30 MINUTE')->fetch()['c'];
    $rageQuits = (int)$pdo->query("SELECT COUNT(*) AS c FROM game_logs WHERE event_type = 'rage_quit'")->fetch()['c'];
    $rewardEvents = (int)$pdo->query('SELECT COUNT(*) AS c FROM ai_decisions')->fetch()['c'];

    $row = $pdo->query('SELECT COALESCE(SUM(amount),0) AS net FROM coin_transactions')->fetch();
    $coinNet = (int)$row['net'];

    $roomHeat = $pdo->query('SELECT room, COUNT(*) AS c FROM room_history GROUP BY room')->fetchAll();
    $roomsLabels = [];
    $roomsValues = [];
    foreach ($roomHeat as $r) {
        $roomsLabels[] = $r['room'];
        $roomsValues[] = (int)$r['c'];
    }

    $wins = $pdo->query('SELECT DATE(started_at) AS d, SUM(win_type IS NOT NULL) AS wins, COUNT(*)-SUM(win_type IS NOT NULL) AS losses FROM game_logs WHERE started_at IS NOT NULL GROUP BY DATE(started_at) ORDER BY d DESC LIMIT 7')->fetchAll();
    $wlLabels = [];
    $wlWins = [];
    $wlLosses = [];
    foreach (array_reverse($wins) as $w) {
        $wlLabels[] = $w['d'];
        $wlWins[] = (int)$w['wins'];
        $wlLosses[] = (int)$w['losses'];
    }

    $recentDecisions = $pdo->query('SELECT * FROM ai_decisions ORDER BY id DESC LIMIT 10')->fetchAll();

    echo json_encode([
        'active_users' => $activeUsers,
        'rage_quits' => $rageQuits,
        'reward_events' => $rewardEvents,
        'coin_net' => $coinNet,
        'room_heatmap' => [
            'labels' => $roomsLabels,
            'values' => $roomsValues
        ],
        'win_loss' => [
            'labels' => $wlLabels,
            'wins' => $wlWins,
            'losses' => $wlLosses
        ],
        'recent_ai_decisions' => $recentDecisions
    ]);
} catch (Throwable $e) {
    http_response_code(500);
    echo json_encode(['success' => false, 'error' => $e->getMessage()]);
}
