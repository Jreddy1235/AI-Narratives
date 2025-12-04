<?php
// Simple DB connection helper
class DB {
    private static ?\PDO $pdo = null;

    public static function getConnection(): \PDO {
        if (self::$pdo === null) {
            $host = 'localhost';
            $db   = 'bingo_ai';
            $user = 'root';
            $pass = 'toor';
            $charset = 'utf8mb4';

            $dsn = "mysql:host=$host;dbname=$db;charset=$charset";
            $options = [
                \PDO::ATTR_ERRMODE            => \PDO::ERRMODE_EXCEPTION,
                \PDO::ATTR_DEFAULT_FETCH_MODE => \PDO::FETCH_ASSOC,
                \PDO::ATTR_EMULATE_PREPARES   => false,
            ];

            self::$pdo = new \PDO($dsn, $user, $pass, $options);
        }
        return self::$pdo;
    }
}
