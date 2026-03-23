using System;
using UnityEngine;

namespace Extensions.Dialogue.Runtime
{
    /// <summary>
    /// Formatted text segment with optional styling applied.
    /// </summary>
    [Serializable]
    public readonly struct TextSegment
    {
        public readonly string Text;
        public readonly TextStyle Style;

        public TextSegment(string text, TextStyle style = default)
        {
            Text = text;
            Style = style;
        }
    }

    /// <summary>
    /// Combined formatted text with multiple segments.
    /// </summary>
    [Serializable]
    public readonly struct FormattedText
    {
        public readonly TextSegment[] Segments;

        public FormattedText(TextSegment[] segments)
        {
            Segments = segments ?? System.Array.Empty<TextSegment>();
        }

        public FormattedText(string plainText)
        {
            Segments = new[] { new TextSegment(plainText) };
        }

        public string PlainText
        {
            get
            {
                var sb = new System.Text.StringBuilder();
                foreach (var segment in Segments)
                {
                    sb.Append(segment.Text);
                }
                return sb.ToString();
            }
        }
    }

    /// <summary>
    /// Styling information for text segments.
    /// </summary>
    [Serializable]
    public sealed class TextStyle
    {
        [SerializeField] public Color Color = Color.white;
        [SerializeField] public int FontSize = 36;
        [SerializeField] public FontStyle FontStyle = FontStyle.Normal;
        [SerializeField] public bool IsBold = false;
        [SerializeField] public bool IsItalic = false;
    }

    /// <summary>
    /// Portrait configuration for speaker display.
    /// </summary>
    [Serializable]
    public sealed class Portrait
    {
        [SerializeField] public Sprite Image;
        [SerializeField] public string Emotion = "neutral";
    }

    /// <summary>
    /// Layout configuration for dialogue presentation.
    /// </summary>
    [Serializable]
    public sealed class LayoutConfig
    {
        public enum Layout { BottomCutscene, LeftCombat, WorldSpace, TopBanner, Custom }

        [SerializeField] public Layout Type = Layout.BottomCutscene;
        [SerializeField] public Vector2 Position = Vector2.zero;
        [SerializeField] public Vector2 Size = new Vector2(1920, 540);
        [SerializeField] public float Padding = 20f;
    }

    /// <summary>
    /// Animation configuration for dialogue text.
    /// </summary>
    [Serializable]
    public sealed class AnimationConfig
    {
        [SerializeField] public bool UseTypewriter = false;
        [SerializeField] public float TypewriterSpeed = 50f; // characters per second
        [SerializeField] public bool UseWaveEffect = false;
        [SerializeField] public bool UseShakeEffect = false;
        [SerializeField] public float ShakeAmount = 0.1f;
    }

    /// <summary>
    /// Behavior configuration for dialogue playback.
    /// </summary>
    [Serializable]
    public sealed class BehaviorConfig
    {
        [SerializeField] public bool AutoPlay = false;
        [SerializeField] public float AutoPlayDelay = 3f;
        [SerializeField] public bool SkipTypewriterOnClick = true;
        [SerializeField] public bool CanSkipDialogue = true;
    }

    /// <summary>
    /// Complete style configuration for dialogue presentation.
    /// </summary>
    [Serializable]
    public sealed class DialogueStyle
    {
        [SerializeField] public string StyleName = "Default";
        [SerializeField] public LayoutConfig Layout = new();
        [SerializeField] public AnimationConfig Animation = new();
        [SerializeField] public BehaviorConfig Behavior = new();
        [SerializeField] public Color BackgroundColor = new Color(0, 0, 0, 0.8f);
        [SerializeField] public float CornerRadius = 10f;
    }

    /// <summary>
    /// Interface for dialogue presentation/UI rendering.
    /// Implementations handle displaying lines, choices, and character portraits.
    /// </summary>
    public interface IDialoguePresenter
    {
        void ShowLine(FormattedText text, Data.SpeakerId speaker, Data.StyleId style, Portrait portrait = null);
        void ShowChoices(Data.TextKey[] choiceTexts, int[] availableIndices);
        void Hide();
        void Clear();
    }
}

