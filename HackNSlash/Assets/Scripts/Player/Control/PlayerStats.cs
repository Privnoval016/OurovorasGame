using UnityEngine;

public class PlayerStats : IDamageable
{
    [HideInInspector] public PlayerInventory pi;
    
    #region Components

    [Header("Components")] 
    
    public BaseStats baseStats;
    
    #endregion

    #region Stat Info

    [Header("Stat Info")]

    public float CurrentHealth { get; private set; }
    public float CurrentElementCharge { get; private set; }
    public float CurrentUltimateCharge { get; private set; }

    public float CurrentFinisherCharge { get; private set; }

    public EvaluatedStats EvaluatedStats;

    #endregion

    public PlayerStats(PlayerInventory playerInventory)
    {
        pi = playerInventory;

        InitializeStats();
    }

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

        HUDMenuUI.Instance.UpdateUltimate(UltimatePercentage(CurrentUltimateCharge));
        HUDMenuUI.Instance.UpdateFinisher(FinisherPercentage(CurrentFinisherCharge));
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
        HUDMenuUI.Instance.UpdateHealth(StatPercentage(CurrentHealth, InnateStat.MaxHealth));
    }

    public void ChangeCharge(float amount, Attack a)
    {
        if (amount == 0) return;

        CurrentElementCharge = Mathf.Clamp(CurrentElementCharge + amount, 0, GetStat(InnateStat.MaxCharge));

        HUDMenuUI.Instance.UpdateCharge(StatPercentage(CurrentElementCharge, InnateStat.MaxCharge));
    }

    public void SetCharge(float value)
    {
        CurrentElementCharge = Mathf.Clamp(value, 0, GetStat(InnateStat.MaxCharge));
        HUDMenuUI.Instance.SetCharge(StatPercentage(CurrentElementCharge, InnateStat.MaxCharge));
    }

    public void ApplyAttackMeterChanges(Attack a)
    {
        ChangeCharge(a.stats.restoreCharge ? a.stats.charge : -a.stats.charge, a);

        ChangeFinisherCharge(a.stats.ultimateCharge);

        if (pi.pc.psm.movingState != MovingStates.Katana)
            ChangeUltimate(a.stats.ultimateCharge);
    }

    public float GetCooldownPercentage(KeyBind k)
    {
        float minCharge = pi.CurrentLoadout.elementLoadout.GetMinCharge(k);

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

        CurrentUltimateCharge = Mathf.Clamp(CurrentUltimateCharge + amount, 0, pi.statData.maxUltimateCharge);
        HUDMenuUI.Instance.UpdateUltimate(UltimatePercentage(CurrentUltimateCharge));
    }

    public float UltimatePercentage(float value)
    {
        float maxUltimate = pi.statData.maxUltimateCharge;

        return maxUltimate > 0 ? value / maxUltimate : 0f;
    }

    public void UpdateUltimateCharge()
    {
        if (pi.pc.psm.movingState != MovingStates.Katana) return;

        ChangeUltimate(-pi.statData.ultimateDrainRate * Time.deltaTime);

        if (CurrentUltimateCharge <= 0)
        {
            pi.pc.psm.SwapToUltimate();
        }
    }

    #endregion

    #region Finisher Methods

    public void ChangeFinisherCharge(float amount)
    {
        if (amount == 0) return;

        CurrentFinisherCharge = Mathf.Clamp(CurrentFinisherCharge + amount, 0, pi.statData.maxFinisherCharge);
        HUDMenuUI.Instance.UpdateFinisher(FinisherPercentage(CurrentFinisherCharge));
    }

    public float FinisherPercentage(float value)
    {
        float maxFinisher = pi.statData.maxFinisherCharge;

        return maxFinisher > 0 ? value / maxFinisher : 0f;
    }

    public void ResetFinisherCharge(Attack a = null)
    {
        if (a != null && !pi.CurrentLoadout.elementLoadout.AttackIsFinisher(a)) return;

        CurrentFinisherCharge = 0f;
        HUDMenuUI.Instance.UpdateFinisher(FinisherPercentage(CurrentFinisherCharge));
    }

    #endregion

    #region Stat Checks

    public bool CanUseUltimate()
    {
        if (pi.pc.psm.movingState == MovingStates.NonCombat) return false;
        if (!pi.pc.psm.canAttack) return false;
        return pi.pc.psm.movingState == MovingStates.Katana || CurrentUltimateCharge >= pi.statData.minActivationCharge;
    }

    public bool CanSwapToNonCombat()
    {
        if (!pi.pc.psm.canAttack) return false;
        if (!pi.pc.psm.IsGrounded) return false;

        return true;
    }

    public bool FinishedElementCooldown(Attack a)
    {
        if (!pi.AttackInElementLoadout(a)) return true;

        return GetCooldownPercentage(a.keyBinds[0]) >= 1f;
    }

    public bool CanUseFinisher(Attack a = null)
    {
        if (a != null && !pi.CurrentLoadout.elementLoadout.AttackIsFinisher(a)) return true;

        LockOnTarget target = pi.pc.psm.NearestHEnemy;

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