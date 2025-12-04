# Scene Setup Instructions

## UI Hierarchy Setup for SampleScene.unity

Follow these steps to set up the UI in Unity:

### 1. Create Canvas (if not exists)
- Right-click in Hierarchy → UI → Canvas
- Set Canvas Scaler to "Scale With Screen Size"
- Reference Resolution: 1920 x 1080

### 2. Create GameManager GameObject
- Create Empty GameObject: "GameManager"
- Add Component: `GameManager.cs`
- Add Component: `LLMService.cs`

### 3. Create UI Structure

#### A. Story Log Panel
```
Canvas
└── StoryLogPanel (Panel)
    └── StoryScrollView (Scroll Rect)
        ├── Viewport (Mask)
        │   └── Content (Vertical Layout Group + Content Size Fitter)
        └── Scrollbar Vertical
```

**StoryLogPanel Setup:**
- Anchor: Top-left to top-right, stretched
- Height: ~650
- Margins: Left 20, Right 20, Top 20

**StoryScrollView Setup:**
- Add Component: Scroll Rect
- Content: Assign "Content" object
- Viewport: Assign "Viewport" object
- Vertical Scrollbar: Assign "Scrollbar Vertical"
- Movement Type: Elastic
- Vertical: ✓ (checked)
- Horizontal: ✗ (unchecked)

**Content Setup:**
- Add Component: Vertical Layout Group
  - Child Alignment: Upper Center
  - Child Force Expand: Width ✓, Height ✗
  - Spacing: 10
  - Padding: Left 10, Right 10, Top 10, Bottom 10
- Add Component: Content Size Fitter
  - Vertical Fit: Preferred Size

#### B. Choice Panel
```
Canvas
└── ChoicePanel (Panel)
    └── ChoiceContainer (Vertical Layout Group + Content Size Fitter)
```

**ChoicePanel Setup:**
- Anchor: Bottom-left to bottom-right, stretched
- Height: ~300
- Margins: Left 20, Right 20, Bottom 20

**ChoiceContainer Setup:**
- Add Component: Vertical Layout Group
  - Child Alignment: Upper Center
  - Child Force Expand: Width ✓, Height ✗
  - Spacing: 10
  - Padding: 10
- Add Component: Content Size Fitter
  - Vertical Fit: Preferred Size

#### C. Loading Panel
```
Canvas
└── LoadingPanel (Panel with semi-transparent background)
    └── LoadingText (TextMeshPro)
```

**LoadingPanel Setup:**
- Anchor: Stretch (fill entire screen)
- Background Color: Black with alpha ~0.7
- Initially: Disabled (SetActive false)

**LoadingText Setup:**
- Text: "Generating story..."
- Font Size: 24
- Alignment: Center
- Color: Cyan or white

### 4. Wire Up UIController

Create a new GameObject: "UIController"
- Add Component: `UIController.cs`
- Assign References:
  - **Story Scroll Rect**: StoryScrollView
  - **Story Log Content**: Content (under Viewport)
  - **Story Text Prefab**: Drag from Assets/Prefabs/StoryText.prefab
  - **Choice Button Container**: ChoiceContainer
  - **Choice Button Prefab**: Drag from Assets/Prefabs/ChoiceButton.prefab
  - **Loading Panel**: LoadingPanel
  - **Loading Text**: LoadingText (TextMeshPro)

### 5. Wire Up GameManager

Select the GameManager GameObject:
- **LLM Service**: Drag LLMService component from same GameObject
- **UI Controller**: Drag UIController GameObject
- **API Key**: Enter your OpenAI API key in LLMService component

### 6. Optional: Styling

**Cyberpunk Theme Colors:**
- Background Panels: Dark blue/purple (#0A0E1A or similar)
- Narration Text: White or light cyan (#E0F0FF)
- Player Action Text: Bright cyan (#4DEEEA)
- Choice Buttons: Dark with neon borders
- Accent Colors: Neon pink (#FF006E), Cyan (#4DEEEA)

**Fonts:**
- Use TextMeshPro for all text
- Consider cyberpunk-style fonts if available

### 7. Testing

1. Enter your OpenAI API key in the LLMService component
2. Press Play
3. The game should:
   - Initialize automatically
   - Display "Connecting to Neon City..."
   - Request first story beat from OpenAI
   - Display narration and choices

## Quick Reference: Component Requirements

- **GameManager**: Needs LLMService and UIController assigned
- **LLMService**: Needs API key set
- **UIController**: Needs all 6 fields assigned (ScrollRect, Content, StoryTextPrefab, ButtonContainer, ButtonPrefab, LoadingPanel)

## Troubleshooting

- **"API Key not set"**: Assign API key in LLMService component
- **"Components not assigned"**: Check all references in Inspector
- **Text not appearing**: Ensure TextMeshPro assets are imported
- **Scroll not working**: Verify Scroll Rect references (Content, Viewport, Scrollbar)
