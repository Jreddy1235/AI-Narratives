using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Core game manager that orchestrates the narrative loop
/// Singleton pattern ensures only one instance exists
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Services")]
    [SerializeField] private LLMService llmService;
    [SerializeField] private UIController uiController;

    [Header("Game State")]
    private PlayerProfile playerProfile;
    private WorldState worldState;
    private List<string> actionHistory;
    private StoryBeat currentBeat;

    [Header("Settings")]
    [SerializeField] private bool debugMode = true;

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        actionHistory = new List<string>();
    }

    private void Start()
    {
        InitializeGame();
    }

    /// <summary>
    /// Initialize player profile, world state, and request the first beat
    /// </summary>
    private void InitializeGame()
    {
        Debug.Log("GameManager: Initializing game...");
        
        // Initialize player profile with default values
        playerProfile = new PlayerProfile
        {
            Name = "Runner",
            Stats = new Dictionary<string, int>
            {
                { "Courage", 50 },
                { "Morality", 50 },
                { "Rationality", 50 }
            },
            Inventory = new List<string>()
        };

        // Initialize world state
        worldState = new WorldState
        {
            CurrentAct = 1,
            Flags = new List<string>(),
            LocationContext = "Neon City - Downtown"
        };

        // Display initial UI state
        if (uiController != null)
        {
            uiController.DisplayNarration("Connecting to Neon City...");
            uiController.ClearChoices();
        }

        // Request the first story beat
        RequestNextBeat("Game Start");
    }

    /// <summary>
    /// Request a new story beat from the LLM service
    /// </summary>
    private void RequestNextBeat(string lastAction)
    {
        if (llmService == null)
        {
            Debug.LogError("GameManager: LLMService not assigned!");
            return;
        }

        if (uiController != null)
        {
            uiController.SetLoadingState(true);
        }

        Debug.Log($"GameManager: Requesting next beat with action: {lastAction}");
        StartCoroutine(llmService.GenerateBeat(worldState, playerProfile, lastAction, OnBeatReceived));
    }

    /// <summary>
    /// Callback when a new story beat is received from the LLM
    /// </summary>
    private void OnBeatReceived(StoryBeat beat)
    {
        if (beat == null)
        {
            Debug.LogError("GameManager: Received null beat!");
            if (uiController != null)
            {
                uiController.SetLoadingState(false);
                uiController.DisplayNarration("Error: Failed to generate story. Please try again.");
            }
            return;
        }

        currentBeat = beat;
        
        if (debugMode)
        {
            Debug.Log($"GameManager: Received beat type '{beat.BeatType}' with {beat.Choices.Count} choices");
        }

        // Check for Resolution (end of act/story arc)
        if (beat.BeatType == "Resolution")
        {
            HandleResolution(beat);
        }
        else
        {
            DisplayBeat(beat);
        }

        if (uiController != null)
        {
            uiController.SetLoadingState(false);
        }
    }

    /// <summary>
    /// Display the story beat to the player
    /// </summary>
    private void DisplayBeat(StoryBeat beat)
    {
        if (uiController == null)
        {
            Debug.LogError("GameManager: UIController not assigned!");
            return;
        }

        // Display narration
        uiController.DisplayNarration(beat.Narration);

        // Display choices
        uiController.DisplayChoices(beat.Choices, OnPlayerChoiceSelected);
    }

    /// <summary>
    /// Handle resolution beats (end of act/arc, but not game over)
    /// </summary>
    private void HandleResolution(StoryBeat beat)
    {
        Debug.Log("GameManager: Resolution beat received");
        
        if (uiController != null)
        {
            uiController.DisplayNarration(beat.Narration);
        }

        // Check if we should advance to next act
        if (beat.Choices.Count > 0)
        {
            // Resolution with choices - continue narrative
            if (uiController != null)
            {
                uiController.DisplayChoices(beat.Choices, OnPlayerChoiceSelected);
            }
        }
        else
        {
            // Resolution without choices - advance act
            AdvanceAct();
        }
    }

    /// <summary>
    /// Advance to the next act
    /// </summary>
    private void AdvanceAct()
    {
        worldState.CurrentAct++;
        Debug.Log($"GameManager: Advanced to Act {worldState.CurrentAct}");
        
        // Add act flag
        worldState.Flags.Add($"act_{worldState.CurrentAct - 1}_complete");
        
        // Request new beat for next act
        RequestNextBeat($"Beginning Act {worldState.CurrentAct}");
    }

    /// <summary>
    /// Called when player selects a choice
    /// </summary>
    public void OnPlayerChoiceSelected(Choice choice)
    {
        if (choice == null)
        {
            Debug.LogError("GameManager: Received null choice!");
            return;
        }

        Debug.Log($"GameManager: Player selected choice '{choice.ID}': {choice.Text}");

        // Update stats based on tags
        UpdateStatsFromTags(choice.Tags);

        // Add to action history
        actionHistory.Add(choice.Text);

        // Update world state flags if needed
        UpdateWorldFlags(choice);

        // Display player's choice in the story log
        if (uiController != null)
        {
            uiController.DisplayPlayerAction(choice.Text);
        }

        // Request next beat with this choice as the last action
        RequestNextBeat(choice.Text);
    }

    /// <summary>
    /// Update player stats based on choice tags
    /// </summary>
    private void UpdateStatsFromTags(List<string> tags)
    {
        if (tags == null || tags.Count == 0) return;

        foreach (string tag in tags)
        {
            switch (tag.ToLower())
            {
                case "brave":
                case "courageous":
                    ModifyStat("Courage", 5);
                    break;
                    
                case "cautious":
                case "fearful":
                    ModifyStat("Courage", -3);
                    break;
                    
                case "moral":
                case "ethical":
                case "heroic":
                    ModifyStat("Morality", 5);
                    break;
                    
                case "immoral":
                case "ruthless":
                case "selfish":
                    ModifyStat("Morality", -5);
                    break;
                    
                case "rational":
                case "logical":
                case "analytical":
                    ModifyStat("Rationality", 5);
                    break;
                    
                case "emotional":
                case "impulsive":
                    ModifyStat("Rationality", -3);
                    break;
            }
        }

        if (debugMode)
        {
            Debug.Log($"GameManager: Stats - Courage: {playerProfile.Stats["Courage"]}, " +
                     $"Morality: {playerProfile.Stats["Morality"]}, " +
                     $"Rationality: {playerProfile.Stats["Rationality"]}");
        }
    }

    /// <summary>
    /// Modify a player stat with clamping between 0-100
    /// </summary>
    private void ModifyStat(string statName, int change)
    {
        if (playerProfile.Stats.ContainsKey(statName))
        {
            playerProfile.Stats[statName] = Mathf.Clamp(playerProfile.Stats[statName] + change, 0, 100);
        }
    }

    /// <summary>
    /// Update world flags based on choice
    /// </summary>
    private void UpdateWorldFlags(Choice choice)
    {
        // Add choice ID as a flag for tracking story branches
        if (!worldState.Flags.Contains(choice.ID))
        {
            worldState.Flags.Add(choice.ID);
        }

        // Check for special tags that affect world state
        if (choice.Tags != null)
        {
            foreach (string tag in choice.Tags)
            {
                // Example: track if player has used stealth, violence, etc.
                string flagKey = $"used_{tag}";
                if (!worldState.Flags.Contains(flagKey))
                {
                    worldState.Flags.Add(flagKey);
                }
            }
        }
    }

    /// <summary>
    /// Get current player profile (for debugging or UI display)
    /// </summary>
    public PlayerProfile GetPlayerProfile()
    {
        return playerProfile;
    }

    /// <summary>
    /// Get current world state (for debugging or UI display)
    /// </summary>
    public WorldState GetWorldState()
    {
        return worldState;
    }
}
