<?php
class OpenAIClient {
    private string $apiKey;
    private string $baseUrl;

    public function __construct() {
        $this->apiKey = getenv('OPENAI_API_KEY') ?: '';
        $this->baseUrl = 'https://api.openai.com/v1/chat/completions';
    }

    public function decide(array $payload): array {
        if (empty($this->apiKey)) {
            return ['error' => 'Missing OPENAI_API_KEY'];
        }

        $ch = curl_init($this->baseUrl);
        $model = 'gpt-4o-mini';

        $systemPrompt = $payload['system_prompt'] ?? 'You are an AI game director for an educational casino-style bingo game. Always respond with valid JSON only.';
        $userPrompt = $payload['user_prompt'] ?? 'Provide generic guidance.';

        $body = [
            'model' => $model,
            'messages' => [
                ['role' => 'system', 'content' => $systemPrompt],
                ['role' => 'user', 'content' => $userPrompt],
            ],
            'temperature' => 0.8,
            'response_format' => ['type' => 'json_object'],
        ];

        curl_setopt_array($ch, [
            CURLOPT_RETURNTRANSFER => true,
            CURLOPT_HTTPHEADER => [
                'Content-Type: application/json',
                'Authorization: Bearer ' . $this->apiKey,
            ],
            CURLOPT_POST => true,
            CURLOPT_POSTFIELDS => json_encode($body),
        ]);

        $raw = curl_exec($ch);
        if ($raw === false) {
            return ['error' => curl_error($ch)];
        }
        $code = curl_getinfo($ch, CURLINFO_HTTP_CODE);
        curl_close($ch);

        if ($code >= 400) {
            return ['error' => 'HTTP ' . $code, 'raw' => $raw];
        }

        $data = json_decode($raw, true);
        $content = $data['choices'][0]['message']['content'] ?? '{}';
        $json = json_decode($content, true);
        if ($json === null) {
            return ['error' => 'Invalid JSON from model', 'raw' => $content];
        }
        return $json;
    }
}
