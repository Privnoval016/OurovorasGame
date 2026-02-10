using Extensions.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Component for displaying a single level in the element progression.
/// Controller-only navigation - no mouse support.
/// </summary>
public class ProgressLevelDisplay : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Header("UI References")]
    [SerializeField] private Image levelIcon;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private GameObject lockedIndicator;
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipText;
    
    [Header("Colors")]
    [SerializeField] private Color unlockedColor = Color.green;
    [SerializeField] private Color lockedColor = Color.gray;
    
    private int level;
    private bool isUnlocked;
    private string description;
    
    /// <summary>
    /// Sets the level data and updates visuals.
    /// </summary>
    public void SetLevel(int levelNumber, bool unlocked, string desc)
    {
        level = levelNumber;
        isUnlocked = unlocked;
        description = desc;
        
        UpdateVisuals();
    }
    
    private void UpdateVisuals()
    {
        if (levelText != null)
            levelText.text = level.ToString();
        
        if (levelIcon != null)
            levelIcon.color = isUnlocked ? unlockedColor : lockedColor;
        
        if (lockedIndicator != null)
            lockedIndicator.SetActive(!isUnlocked);
        
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }
    
    public void OnSelect(BaseEventData eventData)
    {
        if (tooltipPanel != null && tooltipText != null)
        {
            tooltipText.text = description;
            tooltipPanel.SetActive(true);
        }
        
        UIAudio.PlayHover();
    }
    
    public void OnDeselect(BaseEventData eventData)
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }
}