using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.UI;
using Extensions.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInventory : MonoBehaviour
{
    [HideInInspector] public PlayerController pc;

    #region Loadout Info
    
    [Header("Loadout Info")]
    
    public StatData statData;

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
    
    #region Stat Info
    
    [Header("Stat Info")]
    
    public bool isInvincible = false;
    
    public float currentHealth;
    public float currentCharge;
    public float currentUltimate;

    public float currentFinisher;
    
    public Dictionary<Stat, float> Stats = new();
    
    #endregion

    #region MonoBehaviour Callbacks

    private void Awake()
    {
        pc = GetComponent<PlayerController>();

        InitializeElementMenu();
        
        InitializeStats();

        InputManager.Instance.onElementMenuOpen += OnElementMenuAction;

        CurrentLoadout.elementLoadout?.ValidateElementAttacks();

        ActivateAttacksFromSkillTree();
    }

    private void Update()
    {
        UpdateUltimateCharge();
        UpdateElementMenu();
    }

    private void LateUpdate()
    {
        if (pc.psm.timeSinceLastAttack > statData.chargeRestoreTime)
        {
            // Restore charge over time
            SetCharge(currentCharge + statData.chargeRestoreRate * Time.deltaTime);
        }
    }

    #endregion

    #region Input Callbacks
    
    private void OnElementMenuAction(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            CombatManager.Instance.ApplySlowedTimeScale(pc, true);
            
            ElementRadialMenu.OnElementMenuOpen(CurrentElementOption);
            HUDMenuUI.Instance.ActivateElementSwapMenu();
        }
        else if (context.canceled)
        {
            CombatManager.Instance.ApplySlowedTimeScale(pc, false);
            
            var option = ElementRadialMenu.OnElementMenuClose();
            HUDMenuUI.Instance.DeactivateElementSwapMenu();
            
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
        HUDMenuUI.Instance.SetSelectedElementIcon(next);
        HUDMenuUI.Instance.UpdateElementalAttackIcons();
    }

    private void UpdateElementMenu()
    {
        Vector2 inputDirection = InputManager.Instance.CameraMove;
        inputDirection = inputDirection.magnitude > 0.4f ? inputDirection.normalized : Vector2.zero;
        RadialMenuOption<ElementEffect> selected = ElementRadialMenu.UpdateMenu(inputDirection);

        HUDMenuUI.Instance.SetRadialMenuLine(inputDirection);
        
        if (selected != null)
        {
            HUDMenuUI.Instance.SetRadialMenuIcon(selected.data, selected.index);
        }
    }
    
    #endregion
    
    #region Stat Methods

    private void InitializeStats()
    {
        Stats.Add(Stat.MaxHealth, 100f);
        Stats.Add(Stat.MaxCharge, 200f);
        Stats.Add(Stat.Strength, 10f);
        Stats.Add(Stat.Defense, 5f);

        currentHealth = 0f;
        currentCharge = 0f;
        currentUltimate = 0f;
        currentFinisher = 0f;
        
        ChangeHealth(GetStat(Stat.MaxHealth));
        SetCharge(GetStat(Stat.MaxCharge));
        
        HUDMenuUI.Instance.UpdateUltimate(UltimatePercentage(currentUltimate));
        HUDMenuUI.Instance.UpdateFinisher(FinisherPercentage(currentFinisher));
        
    }
    
    public float GetStat(Stat stat)
    {
        if (Stats.TryGetValue(stat, out float value))
        {
            return value;
        }
        
        return 0f;
    }
    
    public float StatPercentage(float value, Stat stat)
    {
        float maxStat = GetStat(stat);
        return maxStat > 0 ? value / maxStat : 0f;
    }
    
    public void ChangeHealth(float amount)
    {
        if (amount == 0) return;
        
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, GetStat(Stat.MaxHealth));
        HUDMenuUI.Instance.UpdateHealth(StatPercentage(currentHealth, Stat.MaxHealth));
    }
    
    public void ChangeCharge(float amount, Attack a)
    {
        if (amount == 0) return;
        
        currentCharge = Mathf.Clamp(currentCharge + amount, 0, GetStat(Stat.MaxCharge));
        
        HUDMenuUI.Instance.UpdateCharge(StatPercentage(currentCharge, Stat.MaxCharge));
    }
    
    private void SetCharge(float value)
    {
        currentCharge = Mathf.Clamp(value, 0, GetStat(Stat.MaxCharge));
        HUDMenuUI.Instance.SetCharge(StatPercentage(currentCharge, Stat.MaxCharge));
    }

    public void ApplyAttackMeterChanges(Attack a)
    {
        ChangeCharge(a.stats.restoreCharge ? a.stats.charge : -a.stats.charge, a);
        
        ChangeFinisherCharge(a.stats.ultimateCharge);
        
        if (pc.psm.movingState != MovingStates.Katana)
            pc.pi.ChangeUltimate(a.stats.ultimateCharge);
    }
    
    public float GetCooldownPercentage(KeyBind k)
    {
        float minCharge = CurrentLoadout.elementLoadout.GetMinCharge(k);
        
        if (minCharge <= 0) return 1f;
        
        return Mathf.Clamp01(currentCharge / minCharge);
    }
    
    
    public void ApplyStatChange(StatChange change)
    {
        
    }
    
    
    
    #endregion
    
    #region Ultimate Methods
    
    public void ChangeUltimate(float amount)
    {
        if (amount == 0) return;
        
        currentUltimate = Mathf.Clamp(currentUltimate + amount, 0, statData.maxUltimateCharge);
        HUDMenuUI.Instance.UpdateUltimate(UltimatePercentage(currentUltimate));
    }
    
    public float UltimatePercentage(float value)
    {
        float maxUltimate = statData.maxUltimateCharge;
        
        return maxUltimate > 0 ? value / maxUltimate : 0f;
    }

    private void UpdateUltimateCharge()
    {
        if (pc.psm.movingState != MovingStates.Katana) return;
        
        ChangeUltimate(-statData.ultimateDrainRate * Time.deltaTime);
        
        if (currentUltimate <= 0)
        {
            pc.psm.SwapToUltimate();
        }
    }
    
    #endregion

    #region Finisher Methods

    public void ChangeFinisherCharge(float amount)
    {
        if (amount == 0) return;
        
        currentFinisher = Mathf.Clamp(currentFinisher + amount, 0, statData.maxFinisherCharge);
        HUDMenuUI.Instance.UpdateFinisher(FinisherPercentage(currentFinisher));
    }
    
    public float FinisherPercentage(float value)
    {
        float maxFinisher = statData.maxFinisherCharge;
        
        return maxFinisher > 0 ? value / maxFinisher : 0f;
    }
    
    public void ResetFinisherCharge(Attack a = null)
    {
        if (a != null && !CurrentLoadout.elementLoadout.AttackIsFinisher(a)) return;
        
        currentFinisher = 0f;
        HUDMenuUI.Instance.UpdateFinisher(FinisherPercentage(currentFinisher));
    }

    #endregion

    #region Stat Checks
    
    public bool CanUseUltimate()
    {
        if (pc.psm.movingState == MovingStates.NonCombat) return false;
        if (!pc.psm.canAttack) return false;
        return pc.psm.movingState == MovingStates.Katana || currentUltimate >= statData.minActivationCharge;
    }

    public bool CanSwapToNonCombat()
    {
        if (!pc.psm.canAttack) return false;
        if (!pc.psm.IsGrounded) return false;

        return true;
    }
    
    public bool FinishedElementCooldown(Attack a)
    {
        if (!AttackInElementLoadout(a)) return true;
        
        return GetCooldownPercentage(a.keyBinds[0]) >= 1f;
    }

    public bool CanUseFinisher(Attack a = null)
    {
        if (a != null && !CurrentLoadout.elementLoadout.AttackIsFinisher(a)) return true;

        LockOnTarget target = pc.psm.NearestHEnemy;
        
        if (target == null) return false;
        
        return FinisherPercentage(currentFinisher) >= 1f;
        
    }
    
    #endregion
}


   
   
public enum Stat
{
    MaxHealth,
    MaxCharge,
    Strength,
    Defense,
}

[Serializable]
public class StatChange
{
    public enum ChangeType
    {
        Flat,
        AdditivePercent,
        MultiplicativePercent,
    }
    
    public Stat stat;
    public float value;
    public ChangeType changeType;
}