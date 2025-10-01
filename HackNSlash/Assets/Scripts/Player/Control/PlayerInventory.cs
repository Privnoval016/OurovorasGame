using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.EventBus;
using Extensions.UI;
using Extensions.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventory : MonoBehaviour
{
    [HideInInspector] public PlayerController pc;

    #region Loadout Info
    
    [Header("Loadout Info")]

    public AttackConfig[] attackDatas;

    public EquipmentLoadout[] loadouts = Array.Empty<EquipmentLoadout>();
    public int currentLoadoutIndex = 0;
    public EquipmentLoadout CurrentLoadout => loadouts[currentLoadoutIndex];

    public PlayerSkillTree skillTree;

    #endregion

    #region Element Info
    
    [Header("Element Info")]

    public ElementEffect currentElementEffect = ElementEffect.None;
    public ElementEffect imbuedElementEffect = ElementEffect.None;

    public ElementEffect[] elementEffects = Array.Empty<ElementEffect>();
    
    public int CurrentElementIndex => elementEffects.ToList().IndexOf(currentElementEffect);
    private RadialMenuOption<ElementEffect> CurrentElementOption => ElementRadialMenu.GetOption(CurrentElementIndex);
    
    public RadialMenu<ElementEffect> ElementRadialMenu;
    
    
    #endregion
    
    public bool isInvincible = false;

    #region MonoBehaviour Callbacks

    private void Awake()
    {
        pc = GetComponent<PlayerController>();

        InitializeElementMenu();

        InputManager.Instance.onElementMenuOpen += OnElementMenuAction;

        CurrentLoadout.elementLoadout?.ValidateElementAttacks();

        ActivateAttacksFromSkillTree();
    }

    private void Update()
    {
        UpdateElementMenu();
    }

    #endregion

    #region Input Callbacks
    
    private void OnElementMenuAction(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            CombatManager.Instance.ApplySlowedTimeScale(pc, true);
            
            ElementRadialMenu.OnElementMenuOpen(CurrentElementOption);
            EventBus<ElementMenuEvent>.Raise(new ElementMenuEvent
            {
                elementEffect = currentElementEffect,
                elementIndex = CurrentElementIndex,
                isActive = true,
                direction = Vector2.zero,
            });
        }
        else if (context.canceled)
        {
            CombatManager.Instance.ApplySlowedTimeScale(pc, false);
            
            var option = ElementRadialMenu.OnElementMenuClose();
            EventBus<ElementMenuEvent>.Raise(new ElementMenuEvent
            {
                elementEffect = option?.data,
                elementIndex = option?.index ?? -1,
                isActive = false,
                direction = Vector2.zero,
            });
            
            if (option == null || option.data == currentElementEffect) return;
            
            SwapElement(option.data);
        }
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
    
    public bool AttackInElementLoadout(Attack attack)
    {
        if (CurrentLoadout.elementLoadout == null) return false;
        
        return CurrentLoadout.elementLoadout.GetElementAttack(currentElementEffect)?.AttackInLoadout(attack) ?? false;
    }

    #endregion
    
    #region Element Attack Methods
    
    public EquippedElementAttack GetCurrentElementAttack()
    {
        return CurrentLoadout.elementLoadout?.GetElementAttack(currentElementEffect);
    }

    private void InitializeElementMenu()
    {
        RadialMenuOption<ElementEffect>[] elementOptions = new RadialMenuOption<ElementEffect>[elementEffects.Length];
        for (int i = 0; i < elementEffects.Length; i++)
        {
            elementOptions[i] = new RadialMenuOption<ElementEffect>(i, elementEffects[i]);
        }
        
        ElementRadialMenu = new RadialMenu<ElementEffect>(elementOptions, Vector2.up);
        
        SwapElement(elementEffects.Length > 0 ? elementEffects[0] : ElementEffect.Wind);
    }

    public void SwapElement(ElementEffect next)
    {
        currentElementEffect = next;
        EventBus<ElementUpdateEvent>.Raise(new ElementUpdateEvent
        {
            elementEffect = currentElementEffect,
        });
    }

    private void UpdateElementMenu()
    {
        Vector2 inputDirection = InputManager.Instance.CameraMove;
        inputDirection = inputDirection.magnitude > 0.4f ? inputDirection.normalized : Vector2.zero;

        if (inputDirection == Vector2.zero) return;
        
        RadialMenuOption<ElementEffect> selected = ElementRadialMenu.UpdateMenu(inputDirection);

        EventBus<ElementMenuEvent>.Raise(new ElementMenuEvent
        {
            elementEffect = selected?.data,
            elementIndex = selected?.index ?? -1,
            isActive = ElementRadialMenu.isMenuOpen,
            direction = inputDirection,
        });
    }
    
    #endregion
    
    
    
   
}