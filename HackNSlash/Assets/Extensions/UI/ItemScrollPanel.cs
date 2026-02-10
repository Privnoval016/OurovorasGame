using Extensions.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Concrete implementation of ScrollUIPanel for displaying inventory items.
/// Used in Equipment, Inventory, and other item selection menus.
/// 
/// HOW IT WORKS:
/// 1. ScrollMenu manages a linked list of these panels
/// 2. As you scroll, panels are recycled (moved from front to back or vice versa)
/// 3. Refresh() is called with new ItemUIInfo data when a panel is recycled
/// 4. OnSelected() is called when this panel becomes the center/focused panel
/// 5. OnDeselected() is called when this panel loses focus
/// 
/// SETUP IN UNITY:
/// - Create a prefab with this component
/// - Add UI elements: Icon (Image), Name (TMP), Description (TMP), Amount (TMP), Border (Image)
/// - Assign references in Inspector
/// - Set this prefab as the panel prefab in ScrollMenu
/// - ScrollMenu will instantiate 5-7 of these as children
/// </summary>
public class ItemScrollPanel : ScrollUIPanel
{
    [Header("UI References")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private Image borderImage;
    [SerializeField] private GameObject emptyIndicator;
    
    [Header("Visual Settings")]
    [SerializeField] private Color normalBorderColor = Color.white;
    [SerializeField] private Color selectedBorderColor = Color.yellow;
    [SerializeField] private Color emptyColor = Color.gray;
    
    [Header("Rarity Colors")]
    [SerializeField] private Color commonColor = Color.white;
    [SerializeField] private Color uncommonColor = Color.green;
    [SerializeField] private Color rareColor = Color.blue;
    [SerializeField] private Color epicColor = Color.magenta;
    [SerializeField] private Color legendaryColor = Color.yellow;
    
    private ItemUIInfo currentInfo;
    
    #region ScrollUIPanel Implementation
    
    /// <summary>
    /// Called when this panel becomes the focused/selected panel in the scroll menu.
    /// Use this to update visuals (border color, scale, etc.) and play sounds.
    /// </summary>
    public override void OnSelected()
    {
        if (borderImage != null)
            borderImage.color = selectedBorderColor;
        
        // Show description for selected item
        if (descriptionText != null && currentInfo != null)
        {
            descriptionText.gameObject.SetActive(true);
            descriptionText.text = currentInfo.itemDescription;
        }
        
        // Play hover sound via EventBus
        UIAudio.PlayHover();
    }
    
    /// <summary>
    /// Called when this panel loses focus (no longer the center panel).
    /// Reset visuals to normal state.
    /// </summary>
    public override void OnDeselected()
    {
        if (borderImage != null)
            borderImage.color = normalBorderColor;
        
        // Hide description for non-selected items
        if (descriptionText != null)
            descriptionText.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Called to update this panel with new item data.
    /// This happens when the panel is recycled during scrolling.
    /// </summary>
    /// <param name="info">The item information to display, or null for empty panel.</param>
    public override void Refresh(ItemUIInfo info)
    {
        currentInfo = info;
        
        if (info == null)
        {
            // Hide the entire panel when empty (items < panels)
            HidePanel();
            return;
        }
        
        // Show the panel
        ShowPanel();
        
        // Update icon
        if (icon != null)
        {
            icon.sprite = info.icon;
            icon.enabled = info.icon != null;
            icon.color = info.icon != null ? Color.white : emptyColor;
        }
        
        // Update name
        if (nameText != null)
        {
            nameText.text = info.itemName;
            nameText.color = GetRarityColor(info.itemRarity);
        }
        
        // Update amount (only show for stackable items)
        if (amountText != null)
        {
            if (info.isStackable && info.amount > 1)
            {
                amountText.text = $"x{info.amount}";
                amountText.gameObject.SetActive(true);
            }
            else
            {
                amountText.gameObject.SetActive(false);
            }
        }
        
        // Description handled in OnSelected/OnDeselected
        if (descriptionText != null)
            descriptionText.text = info.itemDescription;
        
        // Hide empty indicator
        if (emptyIndicator != null)
            emptyIndicator.SetActive(false);
    }
    
    #endregion
    
    #region Helper Methods
    
    private void HidePanel()
    {
        // Hide the entire panel's canvas group or just disable all visuals
        gameObject.SetActive(false);
    }
    
    private void ShowPanel()
    {
        gameObject.SetActive(true);
    }
    
    private Color GetRarityColor(Rarity rarity)
    {
        return rarity switch
        {
            Rarity.Common => commonColor,
            Rarity.Uncommon => uncommonColor,
            Rarity.Rare => rareColor,
            Rarity.Epic => epicColor,
            Rarity.Legendary => legendaryColor,
            _ => commonColor
        };
    }
    
    #endregion
}

