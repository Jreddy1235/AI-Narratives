using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// Handles communication with OpenAI API to generate story beats
/// </summary>
public class LLMService : MonoBehaviour
{
    [SerializeField] private string apiKey = "";
    [SerializeField] private string modelName = "gpt-4o-mini";
    
    private const string API_URL = "https://api.openai.com/v1/chat/completions";

    /// <summary>
    /// Generates a story beat from the LLM based on current game state
    /// </summary>
    public IEnumerator GenerateBeat(WorldState world, PlayerProfile player, string lastAction, Action<StoryBeat> callback)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("LLMService: API Key is not set!");
            yield break;
        }

        // Build the system prompt
        string systemPrompt = BuildSystemPrompt();
        
        // Build the user prompt with context
        string userPrompt = BuildUserPrompt(world, player, lastAction);

        // Create the request payload
        var requestPayload = new
        {
            model = modelName,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.8,
            max_tokens = 1000
        };

        string jsonPayload = JsonConvert.SerializeObject(requestPayload);
        
        // Create UnityWebRequest
        using (UnityWebRequest request = new UnityWebRequest(API_URL, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            
            // Set headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            // Send request
            Debug.Log("LLMService: Sending request to OpenAI...");
            yield return request.SendWebRequest();

            // Handle response
            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"LLMService: Received response");
                
                try
                {
                    // Parse the OpenAI response
                    JObject responseJson = JObject.Parse(responseText);
                    string content = responseJson["choices"][0]["message"]["content"].ToString();
                    
                    // Clean the content (remove markdown code blocks if present)
                    content = CleanJsonContent(content);
                    
                    Debug.Log($"LLMService: Parsing content: {content}");
                    
                    // Deserialize to StoryBeat
                    StoryBeat beat = JsonConvert.DeserializeObject<StoryBeat>(content);
                    
                    if (beat != null)
                    {
                        Debug.Log($"LLMService: Successfully parsed StoryBeat with {beat.Choices.Count} choices");
                        callback?.Invoke(beat);
                    }
                    else
                    {
                        Debug.LogError("LLMService: Failed to deserialize StoryBeat - result was null");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"LLMService: Error parsing response: {e.Message}\n{e.StackTrace}");
                }
            }
            else
            {
                Debug.LogError($"LLMService: Request failed: {request.error}\nResponse: {request.downloadHandler.text}");
            }
        }
    }

    /// <summary>
    /// Builds the system prompt that enforces Neon City setting and JSON output
    /// </summary>
    private string BuildSystemPrompt()
    {
        return @"You are a GM for a Cyberpunk RPG. The city is ruled by MegaCorps. Themes: High Tech, Low Life. Generate immersive story beats with consequences for player choices. Never end the game - only narrative consequences.

Output ONLY JSON matching this schema:
{
  ""beatType"": ""Intro"" | ""Conflict"" | ""Resolution"",
  ""narration"": ""2-4 sentences describing what happens"",
  ""choices"": [
    {
      ""id"": ""choice_1"",
      ""text"": ""action player can take"",
      ""tags"": [""brave"", ""moral"", ""rational"", etc.]
    }
  ]
}

CRITICAL: Output ONLY the JSON object. No explanations, no markdown, no extra text.";
    }

    /// <summary>
    /// Builds the user prompt with current game context
    /// </summary>
    private string BuildUserPrompt(WorldState world, PlayerProfile player, string lastAction)
    {
        StringBuilder prompt = new StringBuilder();
        
        prompt.AppendLine($"CURRENT STATE:");
        prompt.AppendLine($"Act: {world.CurrentAct}");
        prompt.AppendLine($"Location: {world.LocationContext}");
        prompt.AppendLine($"Player: {player.Name}");
        prompt.AppendLine($"Stats - Courage: {player.Stats["Courage"]}, Morality: {player.Stats["Morality"]}, Rationality: {player.Stats["Rationality"]}");
        
        if (player.Inventory.Count > 0)
        {
            prompt.AppendLine($"Inventory: {string.Join(", ", player.Inventory)}");
        }
        
        if (world.Flags.Count > 0)
        {
            prompt.AppendLine($"Story Flags: {string.Join(", ", world.Flags)}");
        }
        
        if (!string.IsNullOrEmpty(lastAction))
        {
            prompt.AppendLine($"\nLast Action: {lastAction}");
            prompt.AppendLine("Generate the consequence of this action and present new choices.");
        }
        else
        {
            prompt.AppendLine("\nThis is the beginning. Generate an intro beat that sets the scene in Neon City.");
        }

        return prompt.ToString();
    }

    /// <summary>
    /// Cleans JSON content by removing markdown code blocks
    /// </summary>
    private string CleanJsonContent(string content)
    {
        content = content.Trim();
        
        // Remove markdown code blocks if present
        if (content.StartsWith("```json"))
        {
            content = content.Substring(7);
        }
        else if (content.StartsWith("```"))
        {
            content = content.Substring(3);
        }
        
        if (content.EndsWith("```"))
        {
            content = content.Substring(0, content.Length - 3);
        }
        
        return content.Trim();
    }
}
