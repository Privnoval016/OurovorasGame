using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.EntityComponent;
using Extensions.EventBus;
using Extensions.Modifiers;
using Extensions.Patterns;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerStats : MonoBehaviour, IDamageable
{
    [HideInInspector] public PlayerController pc;
    
    #region Components

    [Header("Components")] 
    
    public BaseStats baseStats;
    public BattleParameters battleParameters;
    
    #endregion

    #region Stat Info

    [Header("Stat Info")] 
    
    public int Level => pc.rps.Level;
    
    [field: SerializeField] public float CurrentHealth { get; private set; }
    [field: SerializeField] public float CurrentElementCharge { get; private set; }
    [field: SerializeField] public float CurrentUltimateCharge { get; private set; }

    public float CurrentFinisherCharge { get; private set; }

    public EvaluatedStats EvaluatedStats;
    
    public bool isInvincible = false;

    #endregion
    
    private EventBinding<PlayerStatsChangedEvent> statsChangedBinding;

    #region MonoBehaviour Callbacks

    private void Awake()
    {
        pc = GetComponent<PlayerController>();
        
        DamageableComponents.AddComponent(new ElementComponent(() => pc.pcc.currentElementEffect));
    }

    private void Start()
    {
        InitializeStats();
    }

    private void OnEnable()
    {
        // Subscribe to stat change events
        statsChangedBinding = new EventBinding<PlayerStatsChangedEvent>(OnStatsChanged);
        EventBus<PlayerStatsChangedEvent>.Register(statsChangedBinding);
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        if (statsChangedBinding != null)
        {
            EventBus<PlayerStatsChangedEvent>.Deregister(statsChangedBinding);
        }
    }

    private void Update()
    {
        EvaluatedStats.Update();
        UpdateUltimateChargeOverTime();
    }

    private void LateUpdate()
    {
        if (pc.psm.TimeSinceLastAttack.CurrentTime > pc.ps.battleParameters.chargeRestoreTime)
        {
            // Restore charge over time
            pc.ps.SetCharge(pc.ps.CurrentElementCharge + battleParameters.chargeRestoreRate * Time.deltaTime);
        }
    }

    #endregion

    #region Stat Methods

    /// <summary>
    /// Called when any system raises a PlayerStatsChangedEvent.
    /// Triggers a complete stat recalculation.
    /// </summary>
    private void OnStatsChanged(PlayerStatsChangedEvent evt)
    {
        Debug.Log($"PlayerStats: Stats changed from {evt.Source}, refreshing stats...");
        RefreshStats();
    }

    /// <summary>
    /// Refreshes all player stats by reinitializing the evaluation system.
    /// Recreates EvaluatedStats and reapplies all modifiers from equipment, elements, and skills.
    /// </summary>
    public void RefreshStats()
    {
        if (pc == null || pc.rps == null)
        {
            Debug.LogWarning("PlayerStats: Cannot refresh stats - player data not available!");
            return;
        }

        // Recreate EvaluatedStats with fresh mediators
        // NOTE: this will reset all status effects and temporary modifiers, which is intentional for now to avoid stale data issues.
        // In the future, we may want to preserve certain temporary effects through a more sophisticated refresh process.
        EvaluatedStats = new EvaluatedStats(baseStats, () => pc.rps.Level);

        // Reapply equipment modifiers
        ApplyEquipmentModifiers();

        // Reapply element unlock modifiers
        ApplyElementUnlockModifiers();

        // Reapply skill tree modifiers (future)
        ApplySkillTreeModifiers();

        Debug.Log("PlayerStats: Stats refreshed successfully");
    }

    private void InitializeStats()
    {
        EvaluatedStats = new EvaluatedStats(baseStats, () => pc.rps.Level);

        // Apply element unlocks
        ApplyElementUnlockModifiers();

        // Apply equipment
        ApplyEquipmentModifiers();
        
        // Apply skill tree nodes
        ApplySkillTreeModifiers();
        
        CurrentHealth = 0f;
        CurrentElementCharge = 0f;
        CurrentUltimateCharge = 0f;
        CurrentFinisherCharge = 0f;

        SetHealth(GetStat(InnateStat.MaxHealth));
        SetCharge(GetStat(InnateStat.MaxCharge));
    }

    /// <summary>
    /// Applies stat modifiers from equipped items.
    /// </summary>
    private void ApplyEquipmentModifiers()
    {
        if (pc == null || pc.rps == null || pc.rps.CurrentLoadout == null)
            return;

        var loadout = pc.rps.CurrentLoadout;

        // Apply modifiers from equipped accessories
        if (loadout.equippedAccessories != null)
        {
            foreach (var accessory in loadout.equippedAccessories)
            {
                if (accessory != null)
                {
                    // Use the existing InitializeEffects method
                    accessory.InitializeEffects(EvaluatedStats);
                }
            }
        }

        // TODO: Add passive modifiers when passive system is implemented
    }

    /// <summary>
    /// Applies stat modifiers from unlocked element levels.
    /// </summary>
    private void ApplyElementUnlockModifiers()
    {
        if (pc == null || pc.rps == null || pc.rps.elementUnlocks == null)
            return;

        foreach (var elementUnlock in pc.rps.elementUnlocks)
        {
            if (elementUnlock != null)
            {
                // Use the existing Initialize method
                elementUnlock.Initialize(EvaluatedStats);
            }
        }
    }

    /// <summary>
    /// Applies stat modifiers from activated skill tree nodes.
    /// </summary>
    private void ApplySkillTreeModifiers()
    {
        // TODO: Implement when skill tree nodes have stat bonuses
        // For now, skill nodes only unlock attacks, no stat modifiers
    }

    public float GetStat(InnateStat innateStat)
    {
        return EvaluatedStats.GetInnateStat(innateStat);
    }

    public float GetStatPercentage(float value, InnateStat innateStat)
    {
        float maxStat = GetStat(innateStat);
        return maxStat > 0 ? value / maxStat : 0f;
    }
    
    private void ChangeHealth(float amount) => SetHealth(CurrentHealth + amount);
    
    public void SetHealth(float value)
    {
        if (Mathf.Approximately(value, CurrentHealth)) return;
        
        CurrentHealth = Mathf.Clamp(value, 0, GetStat(InnateStat.MaxHealth));
        
        EventBus<HealthUpdateEvent>.Raise(new HealthUpdateEvent
        {
            healthPercentage = GetStatPercentage(CurrentHealth, InnateStat.MaxHealth)
        });
    }

    public void ChangeCharge(float amount, Attack a) => SetCharge(CurrentElementCharge + amount);

    public void SetCharge(float value)
    {
        if (Mathf.Approximately(value, CurrentElementCharge)) return;
        
        CurrentElementCharge = Mathf.Clamp(value, 0, GetStat(InnateStat.MaxCharge));
        EventBus<ChargeUpdateEvent>.Raise(new ChargeUpdateEvent
        {
            chargePercentage = GetStatPercentage(CurrentElementCharge, InnateStat.MaxCharge),
        });
    }

    public void ApplyAttackMeterChanges(Attack a)
    {
        ChangeCharge(a.stats.restoreCharge ? a.stats.charge : -a.stats.charge, a);

        ChangeFinisherCharge(a.stats.ultimateCharge);

        if (pc.psm.movingState != MovingStates.Katana)
            ChangeUltimate(a.stats.ultimateCharge);
    }
    
    #endregion

    #region Ultimate Methods

    public void ChangeUltimate(float amount) => SetUltimate(CurrentUltimateCharge + amount);
    
    public void SetUltimate(float value)
    {
        if (Mathf.Approximately(value, CurrentUltimateCharge)) return;
        
        CurrentUltimateCharge = Mathf.Clamp(value, 0, battleParameters.maxUltimateCharge);
        
        EventBus<UltimateUpdateEvent>.Raise(new UltimateUpdateEvent
        {
            UltimatePercentage = GetUltimatePercentage(CurrentUltimateCharge)
        });
    }

    public float GetUltimatePercentage(float value) => 
        battleParameters.maxUltimateCharge > 0 ? value / battleParameters.maxUltimateCharge : 0f;

    public void UpdateUltimateChargeOverTime()
    {
        if (pc.pi.pc.psm.movingState != MovingStates.Katana) return;

        ChangeUltimate(-battleParameters.ultimateDrainRate * Time.deltaTime);

        if (CurrentUltimateCharge <= 0)
        {
            pc.pi.pc.psm.SwapToUltimate();
        }
    }

    #endregion

    #region Finisher Methods

    public void ChangeFinisherCharge(float amount) => SetFinisherCharge(CurrentFinisherCharge + amount);
    
    public void SetFinisherCharge(float value)
    {
        if (Mathf.Approximately(value, CurrentFinisherCharge)) return;
        
        CurrentFinisherCharge = Mathf.Clamp(value, 0, battleParameters.maxFinisherCharge);
        
        EventBus<FinisherUpdateEvent>.Raise(new FinisherUpdateEvent
        {
            finisherPercentage = GetFinisherPercentage(CurrentFinisherCharge)
        });
    }

    public float GetFinisherPercentage(float value) => 
        battleParameters.maxFinisherCharge > 0 ? value / battleParameters.maxFinisherCharge : 0f;


    public void TryResetFinisherCharge(Attack a)
    {
        if (a != null && !pc.rps.CurrentElementLoadout.AttackIsFinisher(a)) return;
        ResetFinisherCharge();
    }
    
    public void ResetFinisherCharge() => SetFinisherCharge(0f);

    #endregion

    #region Stat Checks

    public bool CanUseUltimate()
    {
        if (pc.pi.pc.psm.movingState == MovingStates.NonCombat) return false;
        if (!pc.pi.pc.psm.canAttack) return false;
        return pc.pi.pc.psm.movingState == MovingStates.Katana || CurrentUltimateCharge >= battleParameters.minActivationCharge;
    }

    public bool CanSwapToNonCombat()
    {
        if (!pc.pi.pc.psm.canAttack) return false;
        if (!pc.pi.pc.psm.IsGrounded) return false;

        return true;
    }

    public bool CanUseFinisher(Attack a = null)
    {
        if (a != null && !pc.rps.CurrentElementLoadout.AttackIsFinisher(a)) return true;

        LockOnTarget target = pc.pi.pc.psm.NearestHEnemy;

        if (target == null) return false;

        return GetFinisherPercentage(CurrentFinisherCharge) >= 1f;
    }

    #endregion

    #region IDamageable Implementation

    public void ApplyStatusEffect(Modifier<StatusEffectQueryKey> statusEffectModifier)
    {
        if (statusEffectModifier?.Key == null ||
            statusEffectModifier.Key.Key is NoStatusEffect) return;
        
        EvaluatedStats.StatusEffectMediator.AddModifier(statusEffectModifier);
        Debug.Log($"{gameObject.name} applied status effect {statusEffectModifier.Key.Key}");
    }

    public void TakeDamage(ElementEffect element, IDamageable attacker, IDamageEvent damageEvent)
    {
        float damageAmount = Services.Get<DamageSystem>().ResolveDamage(attacker, this, damageEvent).FinalDamage;
        
        Debug.Log($"{gameObject.name} took {damageAmount} damage of element {element}");
        
        ChangeHealth(-damageAmount);
    }
    
    public void Heal(float healAmount)
    {
        ChangeHealth(healAmount);
    }

    public int NumHealthBars => 1; // Player has a single health bar
    
    public float CurrentShieldPercentage => 0f; // Player does not have shields like enemies do

    public EvaluatedStats Stats => EvaluatedStats;

    public Entity<IDamageableComponent> DamageableComponents { get; } = new Entity<IDamageableComponent>();

    public IEnumerable<IDamageRule> DamageEvalRules
        => pc.rps.CurrentLoadout?.equippedAccessories?.SelectMany(acc => acc?.ContributeRules() ?? Enumerable.Empty<IDamageRule>()) 
           ?? Enumerable.Empty<IDamageRule>();

    #endregion
}