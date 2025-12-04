using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages all UI elements including story log and choice buttons
/// </summary>
public class UIController : MonoBehaviour
{
    [Header("Story Log")]
    [SerializeField] private ScrollRect storyScrollRect;
    [SerializeField] private Transform storyLogContent;
    [SerializeField] private GameObject storyTextPrefab;
    
    [Header("Choice Buttons")]
    [SerializeField] private Transform choiceButtonContainer;
    [SerializeField] private GameObject choiceButtonPrefab;
    
    [Header("Loading Indicator")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private TextMeshProUGUI loadingText;
    
    [Header("Settings")]
    [SerializeField] private Color narrationColor = Color.white;
    [SerializeField] private Color playerActionColor = new Color(0.3f, 0.8f, 1f); // Cyan
    [SerializeField] private float scrollToBottomDelay = 0.1f;

    private List<GameObject> activeChoiceButtons = new List<GameObject>();

    private void Start()
    {
        // Ensure loading panel starts hidden
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Display narration text from the story beat
    /// </summary>
    public void DisplayNarration(string text)
    {
        AppendStoryText(text, narrationColor, false);
    }

    /// <summary>
    /// Display player's chosen action in the story log
    /// </summary>
    public void DisplayPlayerAction(string text)
    {
        string formattedText = $"> {text}";
        AppendStoryText(formattedText, playerActionColor, true);
    }

    /// <summary>
    /// Append text to the story log scroll view
    /// </summary>
    public void AppendStoryText(string text, Color color, bool isPlayerAction)
    {
        if (storyLogContent == null || storyTextPrefab == null)
        {
            Debug.LogError("UIController: Story log components not assigned!");
            return;
        }

        // Instantiate the text prefab
        GameObject textObject = Instantiate(storyTextPrefab, storyLogContent);
        
        // Get the TextMeshProUGUI component and set the text
        TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = text;
            textComponent.color = color;
            
            // Make player actions italic
            if (isPlayerAction)
            {
                textComponent.fontStyle = FontStyles.Italic;
            }
        }

        // Force scroll to bottom after a short delay (allows layout to update)
        StartCoroutine(ScrollToBottomCoroutine());
    }

    /// <summary>
    /// Coroutine to scroll to bottom after layout update
    /// </summary>
    private System.Collections.IEnumerator ScrollToBottomCoroutine()
    {
        yield return new WaitForSeconds(scrollToBottomDelay);
        
        if (storyScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            storyScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    /// <summary>
    /// Display choices as buttons
    /// </summary>
    public void DisplayChoices(List<Choice> choices, System.Action<Choice> onChoiceSelected)
    {
        if (choiceButtonContainer == null || choiceButtonPrefab == null)
        {
            Debug.LogError("UIController: Choice button components not assigned!");
            return;
        }

        // Clear existing buttons
        ClearChoices();

        if (choices == null || choices.Count == 0)
        {
            Debug.LogWarning("UIController: No choices to display");
            return;
        }

        // Create a button for each choice
        foreach (Choice choice in choices)
        {
            GameObject buttonObject = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            activeChoiceButtons.Add(buttonObject);

            // Set button text
            TextMeshProUGUI buttonText = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = choice.Text;
            }

            // Add onClick listener
            Button button = buttonObject.GetComponent<Button>();
            if (button != null)
            {
                // Capture the choice in a local variable for the closure
                Choice currentChoice = choice;
                button.onClick.AddListener(() => 
                {
                    OnChoiceButtonClicked(currentChoice, onChoiceSelected);
                });
            }
        }

        Debug.Log($"UIController: Displayed {choices.Count} choices");
    }

    /// <summary>
    /// Called when a choice button is clicked
    /// </summary>
    private void OnChoiceButtonClicked(Choice choice, System.Action<Choice> callback)
    {
        // Disable all choice buttons to prevent double-clicking
        SetChoiceButtonsInteractable(false);
        
        // Invoke the callback
        callback?.Invoke(choice);
    }

    /// <summary>
    /// Clear all choice buttons
    /// </summary>
    public void ClearChoices()
    {
        foreach (GameObject button in activeChoiceButtons)
        {
            if (button != null)
            {
                Destroy(button);
            }
        }
        activeChoiceButtons.Clear();
    }

    /// <summary>
    /// Enable or disable all choice buttons
    /// </summary>
    private void SetChoiceButtonsInteractable(bool interactable)
    {
        foreach (GameObject buttonObject in activeChoiceButtons)
        {
            if (buttonObject != null)
            {
                Button button = buttonObject.GetComponent<Button>();
                if (button != null)
                {
                    button.interactable = interactable;
                }
            }
        }
    }

    /// <summary>
    /// Show or hide loading indicator
    /// </summary>
    public void SetLoadingState(bool isLoading)
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(isLoading);
        }

        // Update loading text with animation dots
        if (isLoading && loadingText != null)
        {
            StartCoroutine(AnimateLoadingText());
        }
    }

    /// <summary>
    /// Animate loading text with dots
    /// </summary>
    private System.Collections.IEnumerator AnimateLoadingText()
    {
        string[] frames = { "Generating story.", "Generating story..", "Generating story..." };
        int frameIndex = 0;

        while (loadingPanel != null && loadingPanel.activeSelf)
        {
            if (loadingText != null)
            {
                loadingText.text = frames[frameIndex];
                frameIndex = (frameIndex + 1) % frames.Length;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>
    /// Clear the entire story log
    /// </summary>
    public void ClearStoryLog()
    {
        if (storyLogContent == null) return;

        foreach (Transform child in storyLogContent)
        {
            Destroy(child.gameObject);
        }
    }
}
