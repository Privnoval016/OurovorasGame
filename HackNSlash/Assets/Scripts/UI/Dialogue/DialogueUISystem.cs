using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.TextCore.Text;


/// <summary>
/// Configuration for dialogue text rendering.
/// Keeps all styling self-contained and inspector-tunable.
/// </summary>
[System.Serializable]
public sealed class DialogueTextConfig
{
    [SerializeField] public TMPro.TextMeshProUGUI textComponent;
    [SerializeField] public Color textColor = Color.white;
    [SerializeField] public int fontSize = 36;
    [SerializeField] public float typewriterSpeed = 50f; // chars per second
    [SerializeField] public bool useTypewriter = true;
    [SerializeField] public FontAsset font;
}

/// <summary>
/// Configuration for speaker name display.
/// </summary>
[System.Serializable]
public sealed class SpeakerNameConfig
{
    [SerializeField] public TMPro.TextMeshProUGUI nameComponent;
    [SerializeField] public bool showSpeakerName = true;
    [SerializeField] public Color nameColor = Color.yellow;
    [SerializeField] public int fontSize = 28;
}

/// <summary>
/// Configuration for dialogue panel styling.
/// </summary>
[System.Serializable]
public sealed class DialoguePanelConfig
{
    [SerializeField] public RectTransform panelRectTransform;
    [SerializeField] public UnityEngine.UI.Image panelBackground;
    [SerializeField] public Color backgroundColor = new Color(0, 0, 0, 0.8f);
    [SerializeField] public float cornerRadius = 10f;
    [SerializeField] public float padding = 20f;
    [SerializeField] public CanvasGroup canvasGroup;
    [SerializeField] public float fadeInDuration = 0.3f;
    [SerializeField] public float fadeOutDuration = 0.3f;
}

/// <summary>
/// Model for dialogue UI state.
/// Keeps presentation logic separate from data.
/// </summary>
public sealed class DialogueUIModel
{
    public Extensions.Dialogue.Runtime.FormattedText CurrentText { get; set; }
    public string SpeakerName { get; set; }
    public bool IsVisible { get; set; }
    public float TypewriterProgress { get; set; } // 0-1

    public DialogueUIModel()
    {
        CurrentText = new Extensions.Dialogue.Runtime.FormattedText("");
        SpeakerName = "";
        IsVisible = false;
        TypewriterProgress = 0f;
    }
}

/// <summary>
/// View controller for dialogue UI.
/// Handles rendering and animations.
/// </summary>
public sealed class DialogueUIView : MonoBehaviour
{
    [SerializeField] private DialogueTextConfig textConfig = new();
    [SerializeField] private SpeakerNameConfig speakerConfig = new();
    [SerializeField] private DialoguePanelConfig panelConfig = new();

    private DialogueUIModel _model;
    private float _typewriterCharIndex = 0f;
    private float _fadeTimer = 0f;
    private bool _isFadingIn = false;

    public DialogueUIModel Model => _model;

    private void Awake()
    {
        _model = new DialogueUIModel();
        InitializeUI();
    }

    private void InitializeUI()
    {
        if (panelConfig.panelBackground != null)
        {
            panelConfig.panelBackground.color = panelConfig.backgroundColor;
        }

        if (panelConfig.canvasGroup != null)
        {
            panelConfig.canvasGroup.alpha = 0f;
        }

        if (textConfig.textComponent != null)
        {
            textConfig.textComponent.color = textConfig.textColor;
            textConfig.textComponent.fontSize = textConfig.fontSize;
        }

        if (speakerConfig.nameComponent != null)
        {
            speakerConfig.nameComponent.color = speakerConfig.nameColor;
            speakerConfig.nameComponent.fontSize = speakerConfig.fontSize;
        }
    }

    public void ShowDialogue(Extensions.Dialogue.Runtime.FormattedText text, string speakerName)
    {
        _model.CurrentText = text;
        _model.SpeakerName = speakerName;
        _model.IsVisible = true;
        _typewriterCharIndex = 0f;
        _isFadingIn = true;
        _fadeTimer = 0f;

        if (speakerConfig.nameComponent != null && speakerConfig.showSpeakerName)
        {
            speakerConfig.nameComponent.text = speakerName;
        }

        if (textConfig.useTypewriter)
        {
            _typewriterCharIndex = 0f;
        }
        else
        {
            if (textConfig.textComponent != null)
            {
                textConfig.textComponent.text = text.PlainText;
            }
        }

        FadeIn();
    }

    public void SkipTypewriter()
    {
        if (textConfig.useTypewriter && textConfig.textComponent != null)
        {
            textConfig.textComponent.text = _model.CurrentText.PlainText;
            _typewriterCharIndex = _model.CurrentText.PlainText.Length;
        }
    }

    public bool IsTypewriterFinished()
    {
        if (!textConfig.useTypewriter) return true;
        return _typewriterCharIndex >= _model.CurrentText.PlainText.Length;
    }

    public void Hide()
    {
        _model.IsVisible = false;
        _isFadingIn = false;
        _fadeTimer = 0f;

        if (textConfig.textComponent != null)
        {
            textConfig.textComponent.text = "";
        }

        FadeOut();
    }

    private void FadeIn()
    {
        if (panelConfig.canvasGroup != null)
        {
            StartCoroutine(FadeCoroutine(0f, 1f, panelConfig.fadeInDuration));
        }
    }

    private void FadeOut()
    {
        if (panelConfig.canvasGroup != null)
        {
            StartCoroutine(FadeCoroutine(panelConfig.canvasGroup.alpha, 0f, panelConfig.fadeOutDuration));
        }
    }

    private System.Collections.IEnumerator FadeCoroutine(float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            panelConfig.canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            yield return null;
        }

        panelConfig.canvasGroup.alpha = toAlpha;
    }

    private void Update()
    {
        if (_model.IsVisible && textConfig.useTypewriter && textConfig.textComponent != null)
        {
            UpdateTypewriter();
        }
    }

    private void UpdateTypewriter()
    {
        _typewriterCharIndex += textConfig.typewriterSpeed * Time.deltaTime;
        int charCount = Mathf.FloorToInt(_typewriterCharIndex);
        charCount = Mathf.Min(charCount, _model.CurrentText.PlainText.Length);

        textConfig.textComponent.text = _model.CurrentText.PlainText.Substring(0, charCount);
        _model.TypewriterProgress = charCount / (float)_model.CurrentText.PlainText.Length;
    }
}

/// <summary>
/// Presenter for bottom-of-screen dialogue UI.
/// </summary>
public sealed class BottomDialoguePresenter : MonoBehaviour, Extensions.Dialogue.Runtime.IDialoguePresenter
{
    [SerializeField] private DialogueUIView view;

    private void Start()
    {
        if (view == null)
        {
            view = GetComponentInChildren<DialogueUIView>();
        }
    }

    public void ShowLine(Extensions.Dialogue.Runtime.FormattedText text, Extensions.Dialogue.Data.SpeakerId speaker, Extensions.Dialogue.Data.StyleId style, Extensions.Dialogue.Runtime.Portrait portrait = null)
    {
        string speakerName = speaker.Value >= 0 ? $"Speaker {speaker.Value}" : "";
        view.ShowDialogue(text, speakerName);
    }

    public void ShowChoices(Extensions.Dialogue.Data.TextKey[] choiceTexts, int[] availableIndices)
    {
        // Not used in voiceover dialogue
    }

    public void Hide()
    {
        view.Hide();
    }

    public void Clear()
    {
        Hide();
    }
}

/// <summary>
/// Presenter for combat/voiceover dialogue (middle-left).
/// </summary>
public sealed class CombatDialoguePresenter : MonoBehaviour, Extensions.Dialogue.Runtime.IDialoguePresenter
{
    [SerializeField] private DialogueUIView view;
    [SerializeField] private float displayDuration = 5f;

    private Coroutine _autoHideCoroutine;

    private void Start()
    {
        if (view == null)
        {
            view = GetComponentInChildren<DialogueUIView>();
        }
    }

    public void ShowLine(Extensions.Dialogue.Runtime.FormattedText text, Extensions.Dialogue.Data.SpeakerId speaker, Extensions.Dialogue.Data.StyleId style, Extensions.Dialogue.Runtime.Portrait portrait = null)
    {
        // Cancel previous auto-hide
        if (_autoHideCoroutine != null)
        {
            StopCoroutine(_autoHideCoroutine);
        }

        string speakerName = speaker.Value >= 0 ? $"Speaker {speaker.Value}" : "";
        view.ShowDialogue(text, speakerName);

        // Auto-hide after duration
        _autoHideCoroutine = StartCoroutine(AutoHideAfterDelay());
    }

    private System.Collections.IEnumerator AutoHideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        Hide();
    }

    public void ShowChoices(Extensions.Dialogue.Data.TextKey[] choiceTexts, int[] availableIndices)
    {
        // Not used in voiceover dialogue
    }

    public void Hide()
    {
        if (_autoHideCoroutine != null)
        {
            StopCoroutine(_autoHideCoroutine);
            _autoHideCoroutine = null;
        }

        view.Hide();
    }

    public void Clear()
    {
        Hide();
    }
}


