using System;
using System.Collections.Generic;
using System.Linq;
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

    public ElementEffect CurrentElementEffect = ElementEffect.None;
    public ElementEffect ImbuedElementEffect = ElementEffect.None;

    public ElementEffect[] elementEffects = Array.Empty<ElementEffect>();
    public CircularList<ElementEffect> elementEffectOrder;

    #endregion
    
    #region Stat Info
    
    [Header("Stat Info")]
    
    public float currentHealth;
    public float currentCharge;
    
    public Dictionary<Stat, float> Stats = new();
    
    #endregion

    #region MonoBehaviour Callbacks

    private void Awake()
    {
        pc = GetComponent<PlayerController>();
        
        InitializeStats();

        elementEffectOrder = new CircularList<ElementEffect>(elementEffects.ToList());
        InputManager.Instance.swapElementLeft.performed += OnSwapElementLeft;
        InputManager.Instance.swapElementRight.performed += OnSwapElementRight;

        CurrentLoadout.elementLoadout?.ValidateElementAttacks();

        ActivateAttacksFromSkillTree();
    }

    private void Update()
    {

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

    private void OnSwapElementLeft(InputAction.CallbackContext context)
    {
        int index = elementEffectOrder.IndexOf(CurrentElementEffect);
        CurrentElementEffect = elementEffectOrder.ItemAtShiftedIndex(index, -1);

        HUDMenuUI.Instance.ScrollElementsLeft();
        HUDMenuUI.Instance.UpdateElementalAttackIcons();
    }

    private void OnSwapElementRight(InputAction.CallbackContext context)
    {
        int index = elementEffectOrder.IndexOf(CurrentElementEffect);
        CurrentElementEffect = elementEffectOrder.ItemAtShiftedIndex(index, 1);
        
        HUDMenuUI.Instance.ScrollElementsRight();
        HUDMenuUI.Instance.UpdateElementalAttackIcons();
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
    
    #region Element Attack Methods
    
    public EquippedElementAttack GetCurrentElementAttack()
    {
        return CurrentLoadout.elementLoadout?.GetElementAttack(CurrentElementEffect);
    }
    
    #endregion
    
    #region Stat Methods

    private void InitializeStats()
    {
        Stats.Add(Stat.MaxHealth, 100f);
        Stats.Add(Stat.MaxCharge, 100f);
        Stats.Add(Stat.Strength, 10f);
        Stats.Add(Stat.Defense, 5f);
        
        currentHealth = GetStat(Stat.MaxHealth);
        currentCharge = GetStat(Stat.MaxCharge);
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
    
    public void ChangeCharge(float amount)
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

    public void ChangeCharge(Attack a)
    {
        pc.pi.ChangeCharge(a.stats.restoreCharge ? a.stats.chargeRequired : -a.stats.chargeRequired);
    }
    
    public void ApplyStatChange(StatChange change)
    {
        
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