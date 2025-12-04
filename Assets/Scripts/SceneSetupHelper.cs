using UnityEngine;
using UnityEngine.UI;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Helper script to automatically create the complete UI structure for AI Narratives
/// Run this once via Tools > AI Narratives > Setup Scene
/// </summary>
public class SceneSetupHelper : MonoBehaviour
{
    [MenuItem("Tools/AI Narratives/Setup Scene")]
    public static void SetupScene()
    {
        // Create Managers GameObject
        GameObject managers = CreateManagers();
        
        // Create UI
        GameObject canvas = CreateCanvas();
        GameObject storyPanel = CreateStoryLogPanel(canvas.transform);
        GameObject choicePanel = CreateChoicePanel(canvas.transform);
        GameObject loadingPanel = CreateLoadingPanel(canvas.transform);
        
        // Create UIController
        GameObject uiControllerObj = new GameObject("UIController");
        UIController uiController = uiControllerObj.AddComponent<UIController>();
        
        // Wire up references
        WireUpReferences(managers, uiController, storyPanel, choicePanel, loadingPanel);
        
        Debug.Log("Scene setup complete! Don't forget to set your API Key in the LLMService component on the Managers GameObject.");
        Selection.activeGameObject = managers;
    }
    
    private static GameObject CreateManagers()
    {
        GameObject managers = new GameObject("Managers");
        GameManager gameManager = managers.AddComponent<GameManager>();
        LLMService llmService = managers.AddComponent<LLMService>();
        
        Debug.Log("Created Managers GameObject with GameManager and LLMService");
        return managers;
    }
    
    private static GameObject CreateCanvas()
    {
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Add EventSystem if it doesn't exist
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        
        Debug.Log("Created Canvas with proper settings");
        return canvasObj;
    }
    
    private static GameObject CreateStoryLogPanel(Transform canvasTransform)
    {
        // Story Log Panel
        GameObject panel = new GameObject("StoryLogPanel");
        panel.transform.SetParent(canvasTransform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0.35f);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.offsetMin = new Vector2(20, 10);
        panelRect.offsetMax = new Vector2(-20, -10);
        
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);
        
        // ScrollView
        GameObject scrollView = new GameObject("StoryScrollView");
        scrollView.transform.SetParent(panel.transform, false);
        RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.sizeDelta = Vector2.zero;
        
        ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        
        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform, false);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = new Vector2(-20, 0);
        viewportRect.pivot = new Vector2(0, 1);
        
        viewport.AddComponent<CanvasRenderer>();
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        
        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 0);
        
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.spacing = 15;
        vlg.padding = new RectOffset(15, 15, 15, 15);
        
        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        // Scrollbar
        GameObject scrollbar = CreateScrollbar(scrollView.transform);
        
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.verticalScrollbar = scrollbar.GetComponent<Scrollbar>();
        
        Debug.Log("Created Story Log Panel with ScrollView");
        return panel;
    }
    
    private static GameObject CreateScrollbar(Transform parent)
    {
        GameObject scrollbar = new GameObject("Scrollbar Vertical");
        scrollbar.transform.SetParent(parent, false);
        RectTransform scrollbarRect = scrollbar.AddComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.pivot = new Vector2(1, 1);
        scrollbarRect.sizeDelta = new Vector2(20, 0);
        
        Image scrollbarImage = scrollbar.AddComponent<Image>();
        scrollbarImage.color = new Color(0.1f, 0.1f, 0.15f, 0.8f);
        
        Scrollbar scrollbarComponent = scrollbar.AddComponent<Scrollbar>();
        scrollbarComponent.direction = Scrollbar.Direction.BottomToTop;
        
        // Handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(scrollbar.transform, false);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.sizeDelta = Vector2.zero;
        
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = new Color(0.3f, 0.6f, 0.8f, 1f);
        
        scrollbarComponent.handleRect = handleRect;
        scrollbarComponent.targetGraphic = handleImage;
        
        return scrollbar;
    }
    
    private static GameObject CreateChoicePanel(Transform canvasTransform)
    {
        GameObject panel = new GameObject("ChoicePanel");
        panel.transform.SetParent(canvasTransform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 0.35f);
        panelRect.offsetMin = new Vector2(20, 10);
        panelRect.offsetMax = new Vector2(-20, -10);
        
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);
        
        // Choice Container
        GameObject container = new GameObject("ChoiceContainer");
        container.transform.SetParent(panel.transform, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = Vector2.zero;
        containerRect.anchorMax = Vector2.one;
        containerRect.offsetMin = new Vector2(10, 10);
        containerRect.offsetMax = new Vector2(-10, -10);
        
        VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 12;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        
        ContentSizeFitter csf = container.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        Debug.Log("Created Choice Panel");
        return panel;
    }
    
    private static GameObject CreateLoadingPanel(Transform canvasTransform)
    {
        GameObject panel = new GameObject("LoadingPanel");
        panel.transform.SetParent(canvasTransform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = Vector2.zero;
        
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.7f);
        
        // Loading Text
        GameObject textObj = new GameObject("LoadingText");
        textObj.transform.SetParent(panel.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.sizeDelta = new Vector2(400, 100);
        
        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Generating story...";
        text.fontSize = 28;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.3f, 0.8f, 1f);
        
        panel.SetActive(false);
        
        Debug.Log("Created Loading Panel");
        return panel;
    }
    
    private static void WireUpReferences(GameObject managers, UIController uiController, 
        GameObject storyPanel, GameObject choicePanel, GameObject loadingPanel)
    {
        GameManager gameManager = managers.GetComponent<GameManager>();
        LLMService llmService = managers.GetComponent<LLMService>();
        
        // Wire GameManager
        SerializedObject serializedGM = new SerializedObject(gameManager);
        serializedGM.FindProperty("llmService").objectReferenceValue = llmService;
        serializedGM.FindProperty("uiController").objectReferenceValue = uiController;
        serializedGM.ApplyModifiedProperties();
        
        // Wire UIController
        Transform content = storyPanel.transform.Find("StoryScrollView/Viewport/Content");
        Transform choiceContainer = choicePanel.transform.Find("ChoiceContainer");
        ScrollRect scrollRect = storyPanel.GetComponentInChildren<ScrollRect>();
        TextMeshProUGUI loadingText = loadingPanel.GetComponentInChildren<TextMeshProUGUI>();
        
        SerializedObject serializedUI = new SerializedObject(uiController);
        serializedUI.FindProperty("storyScrollRect").objectReferenceValue = scrollRect;
        serializedUI.FindProperty("storyLogContent").objectReferenceValue = content;
        serializedUI.FindProperty("choiceButtonContainer").objectReferenceValue = choiceContainer;
        serializedUI.FindProperty("loadingPanel").objectReferenceValue = loadingPanel;
        serializedUI.FindProperty("loadingText").objectReferenceValue = loadingText;
        
        // Load prefabs
        string storyTextGuid = "f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1";
        string choiceButtonGuid = "g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2";
        
        string storyTextPath = AssetDatabase.GUIDToAssetPath(storyTextGuid);
        string choiceButtonPath = AssetDatabase.GUIDToAssetPath(choiceButtonGuid);
        
        GameObject storyTextPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(storyTextPath);
        GameObject choiceButtonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(choiceButtonPath);
        
        serializedUI.FindProperty("storyTextPrefab").objectReferenceValue = storyTextPrefab;
        serializedUI.FindProperty("choiceButtonPrefab").objectReferenceValue = choiceButtonPrefab;
        serializedUI.ApplyModifiedProperties();
        
        Debug.Log("Wired up all references!");
    }
}
#endif
