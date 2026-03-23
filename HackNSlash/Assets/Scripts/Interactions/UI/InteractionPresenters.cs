using UnityEngine;
using Sirenix.OdinInspector;


/// <summary>
/// UI presenter for interactions using TextMeshPro and Canvas.
/// Displays prompts, progress, and blocking messages.
/// </summary>
public sealed class CanvasInteractionPresenter : MonoBehaviour, IInteractionPresenter
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private TMPro.TextMeshProUGUI promptText;
    [SerializeField] private RectTransform promptPanel;
    [SerializeField] private UnityEngine.UI.Image progressBar;
    [SerializeField] private TMPro.TextMeshProUGUI blockedText;
    [SerializeField] private InteractionPromptStyle promptStyle = new();

    private CanvasGroup _promptCanvasGroup;
    private CanvasGroup _progressCanvasGroup;
    private CanvasGroup _blockedCanvasGroup;

    private void Start()
    {
        _promptCanvasGroup = promptPanel?.GetComponent<CanvasGroup>();
        _progressCanvasGroup = progressBar?.GetComponent<CanvasGroup>();
        _blockedCanvasGroup = blockedText?.GetComponent<CanvasGroup>();

        HidePrompt();
        HideProgress();
    }

    public void ShowPrompt(string promptText, IInteractable interactable)
    {
        if (this.promptText == null)
            return;

        this.promptText.text = promptText;
        this.promptText.color = promptStyle.PromptColor;

        if (_promptCanvasGroup != null)
        {
            _promptCanvasGroup.alpha = 1f;
        }

        if (promptPanel != null)
        {
            promptPanel.gameObject.SetActive(true);
        }
    }

    public void HidePrompt()
    {
        if (_promptCanvasGroup != null)
        {
            _promptCanvasGroup.alpha = 0f;
        }

        if (promptPanel != null)
        {
            promptPanel.gameObject.SetActive(false);
        }
    }

    public void ShowProgress(float progress)
    {
        if (progressBar == null)
            return;

        if (_progressCanvasGroup != null)
        {
            _progressCanvasGroup.alpha = 1f;
        }

        progressBar.gameObject.SetActive(true);
        progressBar.fillAmount = progress;
    }

    public void HideProgress()
    {
        if (_progressCanvasGroup != null)
        {
            _progressCanvasGroup.alpha = 0f;
        }

        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(false);
        }
    }

    public void ShowBlocked(string reason)
    {
        if (blockedText == null)
            return;

        blockedText.text = reason;

        if (_blockedCanvasGroup != null)
        {
            _blockedCanvasGroup.alpha = 1f;
        }

        blockedText.gameObject.SetActive(true);

        CancelInvoke(nameof(HideBlockedMessage));
        Invoke(nameof(HideBlockedMessage), 3f);
    }

    private void HideBlockedMessage()
    {
        if (_blockedCanvasGroup != null)
        {
            _blockedCanvasGroup.alpha = 0f;
        }

        if (blockedText != null)
        {
            blockedText.gameObject.SetActive(false);
        }
    }
}

/// <summary>
/// World-space UI presenter for interaction prompts.
/// Shows prompts floating above interactables.
/// </summary>
public sealed class WorldSpaceInteractionPresenter : MonoBehaviour, IInteractionPresenter
{
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private TMPro.TextMeshProUGUI promptPrefab;
    [SerializeField] private InteractionPromptStyle promptStyle = new();

    private TMPro.TextMeshProUGUI _currentPrompt;
    private IInteractable _currentInteractable;

    public void ShowPrompt(string promptText, IInteractable interactable)
    {
        if (worldCanvas == null || promptPrefab == null)
            return;

        _currentInteractable = interactable;

        if (_currentPrompt == null)
        {
            _currentPrompt = Instantiate(promptPrefab, worldCanvas.transform);
        }

        _currentPrompt.text = promptText;
        _currentPrompt.color = promptStyle.PromptColor;
        _currentPrompt.gameObject.SetActive(true);
    }

    public void HidePrompt()
    {
        if (_currentPrompt != null)
        {
            _currentPrompt.gameObject.SetActive(false);
        }

        _currentInteractable = null;
    }

    public void ShowProgress(float progress)
    {
        // World space progress bar would be more complex
        // Left for future implementation
    }

    public void HideProgress()
    {
        // World space progress bar would be more complex
        // Left for future implementation
    }

    public void ShowBlocked(string reason)
    {
        ShowPrompt($"<color=red>{reason}</color>", _currentInteractable);
    }

    private void Update()
    {
        if (_currentPrompt != null && _currentInteractable != null)
        {
            // Update prompt position to follow interactable (if it has a Transform)
            // This would require passing additional context
        }
    }
}


