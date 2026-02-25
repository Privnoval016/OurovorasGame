using Extensions.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Component for displaying and selecting attack assignments.
/// </summary>
public class AttackAssignmentButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image attackIcon;
    [SerializeField] private TextMeshProUGUI attackNameText;
    [SerializeField] private Image buttonIndicator; // Shows X, Y, or A
    
    private AttackDisplayData currentAttack;
    
    /// <summary>
    /// Sets the attack data for this button.
    /// </summary>
    public void SetAttack(AttackDisplayData attack)
    {
        currentAttack = attack;
        UpdateDisplay();
    }
    
    private void UpdateDisplay()
    {
        if (currentAttack == null)
            return;
        
        if (attackIcon != null)
        {
            attackIcon.sprite = currentAttack.icon;
            attackIcon.enabled = currentAttack.icon != null;
        }
        
        if (attackNameText != null)
            attackNameText.text = currentAttack.attackName;
    }
}