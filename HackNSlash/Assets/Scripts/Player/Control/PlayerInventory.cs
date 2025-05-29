using System;
using System.Linq;
using Extensions.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventory : MonoBehaviour
{
    [HideInInspector] public PlayerController pc;
    
    #region Loadout Info
    
    public AttackConfig[] attackDatas;
    
    public EquipmentLoadout[] loadouts = Array.Empty<EquipmentLoadout>();
    public int currentLoadoutIndex = 0;
    public EquipmentLoadout CurrentLoadout => loadouts[currentLoadoutIndex];
    
    public PlayerSkillTree skillTree;
    
    #endregion
    
    #region Element Info
    
    public ElementEffect CurrentElementEffect = ElementEffect.None;
    public ElementEffect ImbuedElementEffect = ElementEffect.None;
    
    public ElementEffect[] elementEffects = Array.Empty<ElementEffect>();
    public CircularList<ElementEffect> elementEffectOrder;
    
    #endregion
    
    #region MonoBehaviour Callbacks

    private void Awake()
    {
        pc = GetComponent<PlayerController>();
        
        elementEffectOrder = new CircularList<ElementEffect>(elementEffects.ToList());
        InputManager.Instance.swapElementLeft.performed += OnSwapElementLeft;
        InputManager.Instance.swapElementRight.performed += OnSwapElementRight;
        
        ActivateAttacksFromSkillTree();
    }

    #endregion
    
    #region Input Callbacks
    
    private void OnSwapElementLeft(InputAction.CallbackContext context)
    {
        int index = elementEffectOrder.IndexOf(CurrentElementEffect);
        CurrentElementEffect = elementEffectOrder.ShiftIndex(index, -1);
    }
    
    private void OnSwapElementRight(InputAction.CallbackContext context)
    {
        int index = elementEffectOrder.IndexOf(CurrentElementEffect);
        CurrentElementEffect = elementEffectOrder.ShiftIndex(index, 1);
    }
    
    #endregion
    
    #region Attack Activation Methods
    
    public void ActivateAttacksFromSkillTree()
    {
        foreach (var attackData in attackDatas)
        {
            foreach (var attack in attackData.allAttacks)
            {
                attack.isEnabled = true;
            }
        }
        
        foreach (var node in skillTree.skillTreeNodes)
        {
            if (!node) continue;
            
            if (!node.isUnlocked || !node.isActive)
            {
                foreach (var attack in node.unlockedAttacks)
                {
                    attack.isEnabled = false;
                }
            }
        }
    }
    
    #endregion
}
