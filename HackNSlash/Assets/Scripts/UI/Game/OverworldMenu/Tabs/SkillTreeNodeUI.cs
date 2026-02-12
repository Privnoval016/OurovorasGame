using Extensions.UI;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual representation of a single skill tree node.
/// Shows lock/unlock/activation status with color and icon changes.
/// </summary>
public class SkillTreeNodeUI : MonoBehaviour
{
    [Header("Visual Components")]
    [SerializeField] private Image nodeBackground;
    [SerializeField] private Image nodeIcon;
    [SerializeField] private Image nodeBorder;
    [SerializeField] private TextMeshProUGUI nodeNameText;
    [SerializeField] private Slider holdProgressRing; // Radial progress for hold actions
    [SerializeField] private Image holdProgressRingImage; // The image component of the progress ring, for color changes
    
    [Header("Status Colors")]
    [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f);
    [SerializeField] private Color unlockedColor = new Color(0.5f, 0.7f, 1f);
    [SerializeField] private Color activatedColor = new Color(0.2f, 1f, 0.3f);
    [SerializeField] private Color canUnlockColor = new Color(1f, 0.8f, 0.3f);
    [SerializeField] private Color progressRingColor = Color.white;
    
    private SkillNodeDisplayData nodeData;
    
    /// <summary>
    /// Initializes the node UI with data.
    /// </summary>
    public void Initialize(SkillNodeDisplayData data)
    {
        nodeData = data;
        
        // Initialize hold progress ring
        if (holdProgressRing != null && holdProgressRingImage != null)
        {
            holdProgressRing.value = 0f;
            holdProgressRingImage.color = progressRingColor;
            holdProgressRing.gameObject.SetActive(false);
        }
        
        UpdateVisuals();
    }
    
    /// <summary>
    /// Updates the node's visual state.
    /// </summary>
    public void UpdateState(SkillNodeDisplayData data)
    {
        nodeData = data;
        UpdateVisuals();
    }
    
    private void UpdateVisuals()
    {
        if (nodeData == null)
            return;
        
        // Determine color based on state
        Color targetColor = lockedColor;
        
        if (nodeData.isActivated)
        {
            targetColor = activatedColor;
        }
        else if (nodeData.isUnlocked)
        {
            targetColor = unlockedColor;
        }
        else if (nodeData.canUnlock)
        {
            targetColor = canUnlockColor;
        }
        
        // Update background color
        if (nodeBackground != null)
        {
            nodeBackground.color = targetColor;
        }
        
        // Update icon
        if (nodeIcon != null)
        {
            nodeIcon.sprite = nodeData.icon;
            nodeIcon.enabled = nodeData.icon != null;
            
            // Gray out icon if locked
            nodeIcon.color = nodeData.isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }
        
        // Update name text
        if (nodeNameText != null)
        {
            nodeNameText.text = nodeData.nodeName;
            nodeNameText.color = nodeData.isUnlocked ? Color.white : Color.gray;
        }
        
        // Update border
        if (nodeBorder != null)
        {
            nodeBorder.color = nodeData.isActivated ? activatedColor : Color.white;
        }
    }
    
    /// <summary>
    /// Plays a pulse animation (for feedback when hovering).
    /// </summary>
    public void PlayPulseAnimation()
    {
        if (nodeBackground != null)
        {
            Tween.PunchScale(nodeBackground.transform, Vector3.one * 0.2f, duration: 0.3f, 
                useUnscaledTime: true);
        }
    }
    
    /// <summary>
    /// Sets the hold progress ring fill amount and visibility.
    /// </summary>
    /// <param name="progress">Progress from 0-1</param>
    /// <param name="finished">Whether the hold action is finished (to trigger completion feedback)</param>
    public void SetHoldProgress(float progress, bool finished)
    {
        if (holdProgressRing == null || holdProgressRingImage == null)
            return;
        
        holdProgressRing.value = progress;
        holdProgressRingImage.gameObject.SetActive(true);
        
        // Animate the ring appearance
        if (true && progress > 0f)
        {
            // Optional: Pulse effect as it fills
            float scale = 1f + (progress * 0.1f);
            holdProgressRingImage.transform.localScale = Vector3.one * scale;
        }
        else
        {
            holdProgressRingImage.transform.localScale = Vector3.one;
        }

        if (finished)
        {
            holdProgressRing.value = 0f; // Reset progress once it finishes
            PlayPulseAnimation();
        }
    }
}

