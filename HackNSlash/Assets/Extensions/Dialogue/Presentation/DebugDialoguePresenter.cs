using UnityEngine;

namespace Extensions.Dialogue.Presentation
{
    /// <summary>
    /// Basic dialogue presenter that logs dialogue to console.
    /// Can be extended or replaced with actual UI implementations.
    /// </summary>
    public sealed class DebugDialoguePresenter : Runtime.IDialoguePresenter
    {
        public void ShowLine(Runtime.FormattedText text, Data.SpeakerId speaker, Data.StyleId style, Runtime.Portrait portrait = null)
        {
            Debug.Log($"[Dialogue] Speaker {speaker}: {text.PlainText}");
        }

        public void ShowChoices(Data.TextKey[] choiceTexts, int[] availableIndices)
        {
            Debug.Log($"[Dialogue] Choices available: {availableIndices.Length}");
            for (int i = 0; i < availableIndices.Length; i++)
            {
                Debug.Log($"  [{availableIndices[i]}] {choiceTexts[availableIndices[i]]}");
            }
        }

        public void Hide()
        {
            Debug.Log("[Dialogue] Hidden");
        }

        public void Clear()
        {
            Debug.Log("[Dialogue] Cleared");
        }
    }
}

