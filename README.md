# AI Narratives - Cyberpunk Narrative RPG

A dynamic narrative RPG where an AI (OpenAI GPT) acts as the Game Master, generating story beats in real-time based on player choices.

## 🎮 Project Overview

**Engine**: Unity 2022 LTS / Unity 6 (2D Mode)  
**Theme**: "Neon City" - Cyberpunk  
**AI Integration**: OpenAI API (GPT-4o-mini / GPT-3.5-turbo)

### Core Concept
Players navigate through a branching cyberpunk narrative where every choice matters. The AI Game Master dynamically generates story beats, adapting to player decisions and stats. There are no "game over" screens—only narrative consequences.

## 🏗️ Architecture

### Core Scripts

#### 1. **DataModels.cs** (Non-MonoBehaviour)
Defines the data structures for the game:
- **PlayerProfile**: Name, Stats (Courage/Morality/Rationality), Inventory
- **Choice**: ID, Text, Tags (brave, stealth, moral, etc.)
- **StoryBeat**: BeatType (Intro/Conflict/Resolution), Narration, Choices
- **WorldState**: CurrentAct, Flags, LocationContext

#### 2. **LLMService.cs** (MonoBehaviour)
Handles all communication with OpenAI API:
- Constructs prompts with game context
- Enforces JSON output format
- Parses responses using Newtonsoft.Json
- Manages API requests via UnityWebRequest

#### 3. **GameManager.cs** (Singleton)
Orchestrates the entire game loop:
- Initializes PlayerProfile and WorldState
- Requests story beats from LLMService
- Processes player choices
- Updates stats based on choice tags
- Manages action history and world flags

#### 4. **UIController.cs** (MonoBehaviour)
Manages all UI elements:
- Displays narration in scrollable story log
- Dynamically generates choice buttons
- Handles loading states
- Auto-scrolls to latest content

## 🎯 Game Loop

```
Start Game
    ↓
Initialize Player & World State
    ↓
Request Story Beat (AI generates JSON)
    ↓
Display Narration & Choices
    ↓
Player Selects Choice
    ↓
Update Stats & Flags
    ↓
Request Next Story Beat
    ↓
(Loop continues...)
```

## 📊 Stat System

Player actions affect three core stats (0-100 range):

- **Courage**: Affected by brave/cautious choices
- **Morality**: Affected by ethical/ruthless choices  
- **Rationality**: Affected by logical/emotional choices

### Tag → Stat Mapping
| Tag | Stat | Modifier |
|-----|------|----------|
| brave, courageous | Courage | +5 |
| cautious, fearful | Courage | -3 |
| moral, ethical, heroic | Morality | +5 |
| immoral, ruthless, selfish | Morality | -5 |
| rational, logical, analytical | Rationality | +5 |
| emotional, impulsive | Rationality | -3 |

## 🔧 Setup Instructions

### Prerequisites
- Unity 2022 LTS or Unity 6
- OpenAI API Key ([Get one here](https://platform.openai.com/api-keys))

### Installation

1. **Clone/Open the project in Unity**

2. **Install Newtonsoft.Json**
   - Already added to `Packages/manifest.json`
   - Unity will auto-install on first open

3. **Scene Setup**
   - Follow detailed instructions in `SCENE_SETUP.md`
   - Set up Canvas, ScrollView, and Button containers
   - Wire up references in Inspector

4. **Add API Key**
   - Select GameManager in Hierarchy
   - Find LLMService component
   - Paste your OpenAI API key in the `Api Key` field

5. **Press Play!**

## 🎨 Cyberpunk Theme

### Setting: Neon City
A sprawling cyberpunk metropolis where:
- Mega-corporations control entire districts
- Street gangs fight for territory
- Hackers infiltrate corporate networks
- Augmented humans navigate moral gray zones
- Rain-slicked streets glow with neon signs
- The divide between rich and poor is stark

### Recommended Visual Style
- **Colors**: Dark backgrounds (#0A0E1A) with neon accents
- **Accent Colors**: Cyan (#4DEEEA), Neon Pink (#FF006E)
- **Text**: High contrast, cyberpunk fonts
- **UI**: Futuristic panels with glowing borders

## 🔌 API Integration

### OpenAI Configuration
- **Model**: gpt-4o-mini (default) or gpt-3.5-turbo
- **Temperature**: 0.8 (creative responses)
- **Max Tokens**: 1000

### JSON Response Format
```json
{
  "beatType": "Intro | Conflict | Resolution",
  "narration": "2-4 sentences describing what happens",
  "choices": [
    {
      "id": "choice_1",
      "text": "Action player can take",
      "tags": ["brave", "moral"]
    }
  ]
}
```

## 📝 Technical Notes

### Why Newtonsoft.Json?
- Robust nested object parsing
- Better error handling than JsonUtility
- Supports complex data structures
- Industry standard for JSON in Unity

### Coroutine-Based Architecture
- LLM requests are asynchronous (IEnumerator)
- Non-blocking UI during API calls
- Loading states provide feedback

### Singleton Pattern
GameManager uses singleton pattern to:
- Ensure single instance
- Persist across scene loads
- Provide global access point

## 🐛 Troubleshooting

### Common Issues

**"API Key not set!"**
- Add your OpenAI API key in LLMService component

**"UIController: Components not assigned!"**
- Check all references in UIController Inspector
- Ensure prefabs are assigned

**Text not displaying**
- Import TextMeshPro Essentials (Unity prompts on first TMP use)

**API request fails**
- Verify API key is valid
- Check internet connection
- Review Console for specific error messages

**Scroll view not working**
- Verify ScrollRect references (Content, Viewport)
- Check Content has Vertical Layout Group + Content Size Fitter

## 📦 Project Structure

```
AI-Narratives/
├── Assets/
│   ├── Scenes/
│   │   └── SampleScene.unity
│   ├── Scripts/
│   │   ├── DataModels.cs
│   │   ├── LLMService.cs
│   │   ├── GameManager.cs
│   │   └── UIController.cs
│   └── Prefabs/
│       ├── StoryText.prefab
│       └── ChoiceButton.prefab
├── Packages/
│   └── manifest.json (includes Newtonsoft.Json)
├── README.md
└── SCENE_SETUP.md
```

## 🚀 Future Enhancements

Potential features for expansion:
- [ ] Save/Load system
- [ ] Multiple acts with different themes
- [ ] Character customization
- [ ] Inventory management
- [ ] Combat encounters
- [ ] Relationship tracking with NPCs
- [ ] Visual novel-style character sprites
- [ ] Sound effects and music
- [ ] Achievement system
- [ ] Multiple endings based on stats

## 📄 License

This project is a learning/portfolio piece. Feel free to use as reference or template.

## 🙏 Credits

- **OpenAI**: GPT API for dynamic story generation
- **Unity**: Game engine
- **Newtonsoft.Json**: JSON parsing library

---

**Built with ❤️ for narrative-driven gaming**
