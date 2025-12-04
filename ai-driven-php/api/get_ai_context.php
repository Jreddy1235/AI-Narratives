<?php
/**
 * Helper to build rich AI context from database
 * Used by ai_decision.php
 */

require_once __DIR__ . '/../config/db.php';

function getAIContext($userId, $currentGameId = null) {
    $pdo = DB::getConnection();
    
    // 1. User Profile
    $userStmt = $pdo->prepare('SELECT * FROM users WHERE id = ?');
    $userStmt->execute([$userId]);
    $user = $userStmt->fetch(PDO::FETCH_ASSOC);
    
    $daysSinceLastSeen = null;
    $isNewUser = true;
    if ($user && $user['last_seen_at']) {
        $lastSeen = new DateTime($user['last_seen_at']);
        $now = new DateTime();
        $daysSinceLastSeen = $now->diff($lastSeen)->days;
        $isNewUser = ($user['total_games_played'] == 0);
    }
    
    $winRate = 0;
    if ($user && $user['total_games_played'] > 0) {
        $winRate = round($user['total_wins'] / $user['total_games_played'], 2);
    }
    
    $userProfile = [
        'is_new_user' => $isNewUser,
        'days_since_last_seen' => $daysSinceLastSeen,
        'total_games' => $user['total_games_played'] ?? 0,
        'total_wins' => $user['total_wins'] ?? 0,
        'total_losses' => $user['total_losses'] ?? 0,
        'win_rate' => $winRate,
        'play_style' => $user['play_style'] ?? 'unknown',
        'favorite_room' => $user['favorite_room'] ?? null,
        'avg_reaction_time_ms' => $user['avg_reaction_time_ms'] ?? null
    ];
    
    // 2. Current Game (if provided)
    $currentGame = null;
    if ($currentGameId) {
        $gameStmt = $pdo->prepare('SELECT * FROM games WHERE id = ?');
        $gameStmt->execute([$currentGameId]);
        $game = $gameStmt->fetch(PDO::FETCH_ASSOC);
        
        if ($game) {
            // Get room details
            $roomStmt = $pdo->prepare('SELECT category, difficulty FROM rooms WHERE id = ?');
            $roomStmt->execute([$game['room']]);
            $room = $roomStmt->fetch(PDO::FETCH_ASSOC);
            
            $currentGame = [
                'room' => $game['room'],
                'room_category' => $room['category'] ?? 'unknown',
                'room_difficulty' => $room['difficulty'] ?? 'unknown',
                'numbers_called' => $game['total_numbers_called'],
                'marks_made' => $game['total_marks'],
                'correct_marks' => $game['correct_marks'],
                'incorrect_marks' => $game['incorrect_marks'],
                'powerups_used' => json_decode($game['powerups_used'] ?? '[]', true),
                'duration_so_far_ms' => $game['game_duration_ms'] ?? 0
            ];
        }
    }
    
    // 3. Recent Performance (last 5 games)
    $recentStmt = $pdo->prepare(
        'SELECT room, status, win_type, total_numbers_called, coins_won, ended_at '
        . 'FROM games WHERE user_id = ? AND status IN ("won", "lost") '
        . 'ORDER BY ended_at DESC LIMIT 5'
    );
    $recentStmt->execute([$userId]);
    $recentGames = $recentStmt->fetchAll(PDO::FETCH_ASSOC);
    
    // Calculate streak
    $streak = ['type' => null, 'count' => 0];
    if (count($recentGames) > 0) {
        $firstStatus = $recentGames[0]['status'];
        $count = 0;
        foreach ($recentGames as $g) {
            if ($g['status'] === $firstStatus) {
                $count++;
            } else {
                break;
            }
        }
        $streak = ['type' => $firstStatus, 'count' => $count];
    }
    
    $recentPerformance = [
        'last_5_games' => $recentGames,
        'current_streak' => $streak
    ];
    
    // 4. Room Preferences
    $roomStatsStmt = $pdo->prepare(
        'SELECT room_id, games_played, games_won, total_coins_won, preference_score '
        . 'FROM room_stats WHERE user_id = ? ORDER BY preference_score DESC LIMIT 5'
    );
    $roomStatsStmt->execute([$userId]);
    $roomPreferences = $roomStatsStmt->fetchAll(PDO::FETCH_ASSOC);
    
    return [
        'user_profile' => $userProfile,
        'current_game' => $currentGame,
        'recent_performance' => $recentPerformance,
        'room_preferences' => $roomPreferences
    ];
}
