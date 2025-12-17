using Extensions.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AccessoryMenuUIPanel : ScrollUIPanel
{
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI amountText;
    public Image rarityBorder;
    
    public ItemUIInfo itemUIInfo;
    
    public override void OnSelected()
    {
        rarityBorder.color = Color.blue;
        
        if (descriptionText == null) return;
        descriptionText.text = itemUIInfo.itemDescription;
    }
    
    public override void OnDeselected()
    {
        rarityBorder.color = Color.white;
    }
    
    public override void Refresh(ItemUIInfo info)
    {
        itemUIInfo = info;
        
        icon.sprite = info.icon;
        nameText.text = info.itemName;
        amountText.text = info.isStackable ? info.amount.ToString() : "";
        //rarityBorder.color = 
    }
}