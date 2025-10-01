using System;
using Extensions.EventBus;
using UnityEngine;
using UnityEngine.Rendering;
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

    // TODO: make event bus for ui updates
    public float CurrentHealth { get; private set; }
    public float CurrentElementCharge { get; private set; }
    public float CurrentUltimateCharge { get; private set; }

    public float CurrentFinisherCharge { get; private set; }

    public EvaluatedStats EvaluatedStats;

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
        UpdateUltimateCharge();
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
        EvaluatedStats = new EvaluatedStats(baseStats);

        CurrentHealth = 0f;
        CurrentElementCharge = 0f;
        CurrentUltimateCharge = 0f;
        CurrentFinisherCharge = 0f;

        ChangeHealth(GetStat(InnateStat.MaxHealth));
        SetCharge(GetStat(InnateStat.MaxCharge));

        EventBus<UltimateUpdateEvent>.Raise(new UltimateUpdateEvent
        {
            ultimatePercentage = UltimatePercentage(CurrentUltimateCharge)
        });
        
        EventBus<FinisherUpdateEvent>.Raise(new FinisherUpdateEvent
        {
            finisherPercentage = FinisherPercentage(CurrentFinisherCharge)
        });
    }

    public float GetStat(InnateStat innateStat)
    {
        return EvaluatedStats.GetStat(innateStat);
    }

    public float StatPercentage(float value, InnateStat innateStat)
    {
        float maxStat = GetStat(innateStat);
        return maxStat > 0 ? value / maxStat : 0f;
    }

    private void ChangeHealth(float amount)
    {
        if (amount == 0) return;

        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, GetStat(InnateStat.MaxHealth));

        EventBus<HealthUpdateEvent>.Raise(new HealthUpdateEvent
        {
            healthPercentage = StatPercentage(CurrentHealth, InnateStat.MaxHealth)
        });
    }

    public void ChangeCharge(float amount, Attack a)
    {
        if (amount == 0) return;

        CurrentElementCharge = Mathf.Clamp(CurrentElementCharge + amount, 0, GetStat(InnateStat.MaxCharge));

        EventBus<ChargeUpdateEvent>.Raise(new ChargeUpdateEvent
        {
            chargePercentage = StatPercentage(CurrentElementCharge, InnateStat.MaxCharge),
        });
    }

    public void SetCharge(float value)
    {
        CurrentElementCharge = Mathf.Clamp(value, 0, GetStat(InnateStat.MaxCharge));
        EventBus<ChargeUpdateEvent>.Raise(new ChargeUpdateEvent
        {
            chargePercentage = StatPercentage(CurrentElementCharge, InnateStat.MaxCharge),
        });
    }

    public void ApplyAttackMeterChanges(Attack a)
    {
        ChangeCharge(a.stats.restoreCharge ? a.stats.charge : -a.stats.charge, a);

        ChangeFinisherCharge(a.stats.ultimateCharge);

        if (pc.psm.movingState != MovingStates.Katana)
            ChangeUltimate(a.stats.ultimateCharge);
    }

    public float GetCooldownPercentage(KeyBind k)
    {
        float minCharge = pc.pi.CurrentLoadout.elementLoadout.GetMinCharge(k);

        if (minCharge <= 0) return 1f;

        return Mathf.Clamp01(CurrentElementCharge / minCharge);
    }


    public void ApplyStatChange(StatChange change)
    {
    }

    #endregion

    #region Ultimate Methods

    public void ChangeUltimate(float amount)
    {
        if (amount == 0) return;

        CurrentUltimateCharge = Mathf.Clamp(CurrentUltimateCharge + amount, 0, battleParameters.maxUltimateCharge);
        
        EventBus<UltimateUpdateEvent>.Raise(new UltimateUpdateEvent
        {
            ultimatePercentage = UltimatePercentage(CurrentUltimateCharge)
        });
    }

    public float UltimatePercentage(float value)
    {
        float maxUltimate = battleParameters.maxUltimateCharge;

        return maxUltimate > 0 ? value / maxUltimate : 0f;
    }

    public void UpdateUltimateCharge()
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

    public void ChangeFinisherCharge(float amount)
    {
        if (amount == 0) return;

        CurrentFinisherCharge = Mathf.Clamp(CurrentFinisherCharge + amount, 0, battleParameters.maxFinisherCharge);
        
        EventBus<FinisherUpdateEvent>.Raise(new FinisherUpdateEvent
        {
            finisherPercentage = FinisherPercentage(CurrentFinisherCharge)
        });
    }

    public float FinisherPercentage(float value)
    {
        float maxFinisher = battleParameters.maxFinisherCharge;

        return maxFinisher > 0 ? value / maxFinisher : 0f;
    }

    public void ResetFinisherCharge(Attack a = null)
    {
        if (a != null && !pc.pi.CurrentLoadout.elementLoadout.AttackIsFinisher(a)) return;

        CurrentFinisherCharge = 0f;
        EventBus<FinisherUpdateEvent>.Raise(new FinisherUpdateEvent
        {
            finisherPercentage = FinisherPercentage(CurrentFinisherCharge)
        });
    }

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

    public bool FinishedElementCooldown(Attack a)
    {
        if (!pc.pi.AttackInElementLoadout(a)) return true;

        return GetCooldownPercentage(a.keyBinds[0]) >= 1f;
    }

    public bool CanUseFinisher(Attack a = null)
    {
        if (a != null && !pc.pi.CurrentLoadout.elementLoadout.AttackIsFinisher(a)) return true;

        LockOnTarget target = pc.pi.pc.psm.NearestHEnemy;

        if (target == null) return false;

        return FinisherPercentage(CurrentFinisherCharge) >= 1f;
    }

    #endregion

    public void TakeDamage(ElementEffect element, float damageAmount)
    {
        ChangeHealth(-damageAmount);
    }
    
    public void Heal(float healAmount)
    {
        ChangeHealth(healAmount);
    }
}