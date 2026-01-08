using System;
using System.Collections.Generic;
using System.Linq;
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
    [FormerlySerializedAs("statData")] public BattleParameters battleParameters;
    
    #endregion

    #region Stat Info

    [Header("Stat Info")] 
    [ReadOnly] public int Level { get; private set; } = 10;
    
    [field: SerializeField] public float CurrentHealth { get; private set; }
    [field: SerializeField] public float CurrentElementCharge { get; private set; }
    [field: SerializeField] public float CurrentUltimateCharge { get; private set; }

    public float CurrentFinisherCharge { get; private set; }

    public EvaluatedStats EvaluatedStats;
    
    public bool isInvincible = false;

    #endregion
    
    #region MonoBehaviour Callbacks

    private void Awake()
    {
        pc = GetComponent<PlayerController>();
    }

    private void Start()
    {
        InitializeStats();
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

    private void InitializeStats()
    {
        EvaluatedStats = new EvaluatedStats(baseStats, () => Level);

        CurrentHealth = 0f;
        CurrentElementCharge = 0f;
        CurrentUltimateCharge = 0f;
        CurrentFinisherCharge = 0f;

        SetHealth(GetStat(InnateStat.MaxHealth));
        SetCharge(GetStat(InnateStat.MaxCharge));
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
        if (a != null && !pc.pcc.CurrentElementLoadout.AttackIsFinisher(a)) return;
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
        if (a != null && !pc.pcc.CurrentElementLoadout.AttackIsFinisher(a)) return true;

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
        
        ChangeHealth(-damageAmount);
    }
    
    public void Heal(float healAmount)
    {
        ChangeHealth(healAmount);
    }

    public int NumHealthBars => 1; // Player has a single health bar

    public EvaluatedStats Stats => EvaluatedStats;

    public IEnumerable<IDamageRule> DamageEvalRules
        => pc.pi.CurrentLoadout?.equippedAccessories?.SelectMany(acc => acc?.ContributeRules() ?? Enumerable.Empty<IDamageRule>()) 
           ?? Enumerable.Empty<IDamageRule>();

    #endregion
}