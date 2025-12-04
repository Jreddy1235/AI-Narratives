using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Player profile containing stats, inventory, and player identity
/// </summary>
[Serializable]
public class PlayerProfile
{
    [JsonProperty("name")]
    public string Name;
    
    [JsonProperty("stats")]
    public Dictionary<string, int> Stats;
    
    [JsonProperty("inventory")]
    public List<string> Inventory;

    public PlayerProfile()
    {
        Name = "Runner";
        Stats = new Dictionary<string, int>
        {
            { "Courage", 50 },
            { "Morality", 50 },
            { "Rationality", 50 }
        };
        Inventory = new List<string>();
    }
}

/// <summary>
/// Represents a single choice option for the player
/// </summary>
[Serializable]
public class Choice
{
    [JsonProperty("id")]
    public string ID;
    
    [JsonProperty("text")]
    public string Text;
    
    [JsonProperty("tags")]
    public List<string> Tags;

    public Choice()
    {
        Tags = new List<string>();
    }
}

/// <summary>
/// A story beat containing narration and available choices
/// </summary>
[Serializable]
public class StoryBeat
{
    [JsonProperty("beatType")]
    public string BeatType; // Intro, Conflict, Resolution
    
    [JsonProperty("narration")]
    public string Narration;
    
    [JsonProperty("choices")]
    public List<Choice> Choices;

    public StoryBeat()
    {
        Choices = new List<Choice>();
    }
}

/// <summary>
/// Tracks the current state of the game world
/// </summary>
[Serializable]
public class WorldState
{
    [JsonProperty("currentAct")]
    public int CurrentAct;
    
    [JsonProperty("flags")]
    public List<string> Flags;
    
    [JsonProperty("locationContext")]
    public string LocationContext;

    public WorldState()
    {
        CurrentAct = 1;
        Flags = new List<string>();
        LocationContext = "Neon City - Downtown";
    }
}
