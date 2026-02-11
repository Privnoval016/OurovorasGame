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

    public ElementEffect currentElementEffect = ElementEffect.None;
    public ElementEffect imbuedElementEffect = ElementEffect.None;
    
    public int CurrentElementIndex => pc.rps.elementEffects.ToList().IndexOf(currentElementEffect);
    
    // old
    private RadialMenuOption<ElementEffect> CurrentElementOption => ElementRadialMenu.GetOption(CurrentElementIndex);
    
    public RadialMenu<ElementEffect> ElementRadialMenu;
    // end old
    
    #endregion
    
    #region MonoBehaviour Callbacks

    private void Awake()
    {
        pc = GetComponent<PlayerController>();
        
        InitializeElementMenu();
        
        //InputManager.Instance.onElementMenuOpen += OnElementMenuAction;
        InputManager.Instance.onElementSwapLeft += OnElementSwapLeftAction;
        InputManager.Instance.onElementSwapRight += OnElementSwapRightAction;
        
        pc.rps.CurrentElementLoadout?.ValidateElementAttacks();

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
    
    // old
    private void OnElementMenuAction(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Services.Get<CombatSystem>().ApplySlowedTimescale(true);
            
            ElementRadialMenu.OnElementMenuOpen(CurrentElementOption);
            EventBus<ElementMenuEvent>.Raise(new ElementMenuEvent
            {
                ElementEffect = currentElementEffect,
                ElementIndex = CurrentElementIndex,
                IsActive = true,
                Direction = Vector2.zero,
            });
        }
        else if (context.canceled)
        {
            Services.Get<CombatSystem>().ApplySlowedTimescale(false);
            
            var option = ElementRadialMenu.OnElementMenuClose();
            EventBus<ElementMenuEvent>.Raise(new ElementMenuEvent
            {
                ElementEffect = option?.data,
                ElementIndex = option?.index ?? -1,
                IsActive = false,
                Direction = Vector2.zero,
            });
            
            if (option == null || option.data == currentElementEffect) return;
            
            SwapElement(0, option.data);
        }
    }
    
    private void OnElementSwapLeftAction(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        int nextIndex = (CurrentElementIndex - 1 + pc.rps.elementEffects.Length) % pc.rps.elementEffects.Length;
        SwapElement(-1, pc.rps.elementEffects[nextIndex]);
    }
    
    private void OnElementSwapRightAction(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        int nextIndex = (CurrentElementIndex + 1) % pc.rps.elementEffects.Length;
        SwapElement(1, pc.rps.elementEffects[nextIndex]);
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
        if (pc.rps.CurrentElementLoadout == null) return false;
        
        return pc.rps.CurrentElementLoadout.GetElementAttack(currentElementEffect)?.AttackInLoadout(attack) ?? false;
    }

    #endregion
    
    #region Element Attack Methods
    
    public EquippedElementAttack GetCurrentElementAttack()
    {
        return pc.rps.CurrentElementLoadout?.GetElementAttack(currentElementEffect);
    }

    public float GetElementAttackChargePercentage(KeyBind k)
    {
        float keybindCooldown = pc.rps.CurrentElementLoadout?.GetMinCharge(k) ?? 0f;
        
        return keybindCooldown > 0 ? Mathf.Clamp01(pc.ps.CurrentElementCharge / keybindCooldown) : 1f;
    }

    private void InitializeElementMenu()
    {
        RadialMenuOption<ElementEffect>[] elementOptions = new RadialMenuOption<ElementEffect>[pc.rps.elementEffects.Length];
        for (int i = 0; i < pc.rps.elementEffects.Length; i++)
        {
            elementOptions[i] = new RadialMenuOption<ElementEffect>(i, pc.rps.elementEffects[i]);
        }
        
        ElementRadialMenu = new RadialMenu<ElementEffect>(elementOptions, Vector2.up);
        
        SwapElement(0, pc.rps.elementEffects.Length > 0 ? pc.rps.elementEffects[0] : ElementEffect.Wind);
    }

    public void SwapElement(int direction, ElementEffect next)
    {
        currentElementEffect = next;
        EventBus<ElementUpdateEvent>.Raise(new ElementUpdateEvent
        {
            Direction = direction,
            ElementEffect = currentElementEffect,
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
            ElementEffect = selected?.data,
            ElementIndex = selected?.index ?? -1,
            IsActive = ElementRadialMenu.isMenuOpen,
            Direction = inputDirection,
        });
    }
    
    public float GetCooldownPercentage(KeyBind k)
    {
        float minCharge = pc.rps.CurrentElementLoadout.GetMinCharge(k);

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