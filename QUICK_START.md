# Quick Start Guide - AI Narratives

## 🚀 5-Minute Setup

### Step 1: Open Unity
1. Open the project in Unity 2022 LTS or Unity 6
2. Wait for Unity to import all packages (including Newtonsoft.Json)

### Step 2: Automatic Scene Setup
1. In Unity, go to the top menu: **Tools → AI Narratives → Setup Scene**
2. This will automatically create:
   - ✅ Managers GameObject (with GameManager & LLMService)
   - ✅ Canvas with proper UI structure
   - ✅ Story Log Panel (ScrollView)
   - ✅ Choice Panel (Button Container)
   - ✅ Loading Panel
   - ✅ UIController
   - ✅ All references wired up

### Step 3: Add Your OpenAI API Key
1. Select the **Managers** GameObject in the Hierarchy
2. In the Inspector, find the **LLMService** component
3. Paste your OpenAI API key into the **Api Key** field
   - Get an API key at: https://platform.openai.com/api-keys

### Step 4: Press Play! 🎮
1. Click the Play button
2. The game will:
   - Initialize automatically
   - Send a request to OpenAI
   - Display the opening story beat
   - Show choice buttons
3. Click a choice to continue the narrative!

---

## 🎮 How to Play

- **Read** the narration in the upper panel (story log)
- **Choose** an action by clicking a button in the lower panel
- **Watch** as your choices affect your stats and the narrative
- **Explore** different paths - there's no game over, only consequences!

---

## 📊 Stats System

Your choices affect three core stats:

- **Courage** (0-100): Influenced by brave/cautious choices
- **Morality** (0-100): Influenced by ethical/ruthless choices
- **Rationality** (0-100): Influenced by logical/emotional choices

The AI adapts the story based on your stats!

---

## 🔧 Troubleshooting

### "API Key not set!" error
- Make sure you've added your OpenAI API key to the LLMService component

### UI not appearing
- Run **Tools → AI Narratives → Setup Scene** again
- Check that all prefabs are in Assets/Prefabs folder

### No response from AI
- Check your internet connection
- Verify your API key is valid
- Check Console for error messages
- Make sure you have API credits in your OpenAI account

### TextMeshPro errors
- Unity will prompt you to import TMP Essentials on first use
- Click "Import TMP Essentials" when prompted

---

## 🎨 Customization

### Change AI Model
- Select Managers → LLMService component
- Change **Model Name** (default: gpt-4o-mini)
- Options: gpt-4o-mini, gpt-3.5-turbo, gpt-4

### Adjust UI Colors
- Select UIController GameObject
- Modify **Narration Color** and **Player Action Color** in Inspector

### Modify the Setting
- Edit the system prompt in `LLMService.cs` → `BuildSystemPrompt()` method

---

## 📝 Next Steps

Once you've played through a few story beats:

1. **Experiment** with different choices
2. **Check** the Console for debug logs showing stat changes
3. **Customize** the cyberpunk theme (colors, fonts, etc.)
4. **Extend** the system (add inventory UI, stat displays, etc.)

---

## 🆘 Need Help?

Check the detailed documentation:
- **README.md** - Full project overview
- **SCENE_SETUP.md** - Manual scene setup (if automatic fails)
- Unity Console - Detailed error messages and debug logs

---

**Enjoy your cyberpunk narrative adventure in Neon City!** 🌃
