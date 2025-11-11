using System;
using System.Linq;
using Extensions.EventBus;
using Extensions.UI;
using UnityEngine;
using UnityEngine.InputSystem;

/**
 * The PlayerCombatControl class manages the player's combat capabilities, including attack configurations,
 * skill tree integration, and elemental effects. Anything related to the player's ability to attack or use
 * elements should be handled here.
 */
public class PlayerCombatControl : MonoBehaviour
{
    [HideInInspector] public PlayerController pc;
    
    [Header("Combat")]
    
    public AttackConfig[] attackDatas;
    public PlayerSkillTree skillTree;
    
    #region Element Info
    
    [Header("Element Info")]
    
    public ElementLoadout[] elementLoadouts;
    public int currentElementLoadoutIndex = 0;
    public ElementLoadout CurrentElementLoadout => elementLoadouts.Length > 0 ? elementLoadouts[currentElementLoadoutIndex] : null;

    public ElementEffect currentElementEffect = ElementEffect.None;
    public ElementEffect imbuedElementEffect = ElementEffect.None;

    public ElementEffect[] elementEffects = Array.Empty<ElementEffect>();
    
    public int CurrentElementIndex => elementEffects.ToList().IndexOf(currentElementEffect);
    private RadialMenuOption<ElementEffect> CurrentElementOption => ElementRadialMenu.GetOption(CurrentElementIndex);
    
    public RadialMenu<ElementEffect> ElementRadialMenu;
    
    
    #endregion
    
    #region MonoBehaviour Callbacks

    private void Awake()
    {
        pc = GetComponent<PlayerController>();
        
        InitializeElementMenu();
        
        InputManager.Instance.onElementMenuOpen += OnElementMenuAction;
        
        CurrentElementLoadout?.ValidateElementAttacks();

        ActivateAttacksFromSkillTree();
    }

    private void Start()
    {
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
        
        if (!skillTree) return;

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
        if (CurrentElementLoadout == null) return false;
        
        return CurrentElementLoadout.GetElementAttack(currentElementEffect)?.AttackInLoadout(attack) ?? false;
    }

    #endregion
    
    #region Element Attack Methods
    
    public EquippedElementAttack GetCurrentElementAttack()
    {
        return CurrentElementLoadout?.GetElementAttack(currentElementEffect);
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
    
    public float GetCooldownPercentage(KeyBind k)
    {
        float minCharge = CurrentElementLoadout.GetMinCharge(k);

        if (minCharge <= 0) return 1f;

        return Mathf.Clamp01(pc.ps.CurrentElementCharge / minCharge);
    }
    
    public bool FinishedElementCooldown(Attack a)
    {
        if (!AttackInElementLoadout(a)) return true;

        return GetCooldownPercentage(a.keyBinds[0]) >= 1f;
    }
    
    #endregion
    
    
}