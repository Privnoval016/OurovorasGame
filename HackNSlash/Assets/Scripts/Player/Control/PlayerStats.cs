using System.Collections.Generic;
using UnityEngine;

public class PlayerStats
{
    [HideInInspector] public PlayerInventory pi;

    #region Stat Info

    [Header("Stat Info")]

    public float currentHealth;
    public float currentCharge;
    public float currentUltimate;

    public float currentFinisher;

    public Dictionary<InnateStat, float> Stats = new();

    #endregion

    public PlayerStats(PlayerInventory playerInventory)
    {
        pi = playerInventory;

        InitializeStats();
    }

    #region Stat Methods

    private void InitializeStats()
    {
        Stats.Add(InnateStat.MaxHealth, 100f);
        Stats.Add(InnateStat.MaxCharge, 200f);
        Stats.Add(InnateStat.Strength, 10f);
        Stats.Add(InnateStat.Defense, 5f);

        currentHealth = 0f;
        currentCharge = 0f;
        currentUltimate = 0f;
        currentFinisher = 0f;

        ChangeHealth(GetStat(InnateStat.MaxHealth));
        SetCharge(GetStat(InnateStat.MaxCharge));

        HUDMenuUI.Instance.UpdateUltimate(UltimatePercentage(currentUltimate));
        HUDMenuUI.Instance.UpdateFinisher(FinisherPercentage(currentFinisher));
    }

    public float GetStat(InnateStat innateStat)
    {
        if (Stats.TryGetValue(innateStat, out float value))
        {
            return value;
        }

        return 0f;
    }

    public float StatPercentage(float value, InnateStat innateStat)
    {
        float maxStat = GetStat(innateStat);
        return maxStat > 0 ? value / maxStat : 0f;
    }

    public void ChangeHealth(float amount)
    {
        if (amount == 0) return;

        currentHealth = Mathf.Clamp(currentHealth + amount, 0, GetStat(InnateStat.MaxHealth));
        HUDMenuUI.Instance.UpdateHealth(StatPercentage(currentHealth, InnateStat.MaxHealth));
    }

    public void ChangeCharge(float amount, Attack a)
    {
        if (amount == 0) return;

        currentCharge = Mathf.Clamp(currentCharge + amount, 0, GetStat(InnateStat.MaxCharge));

        HUDMenuUI.Instance.UpdateCharge(StatPercentage(currentCharge, InnateStat.MaxCharge));
    }

    public void SetCharge(float value)
    {
        currentCharge = Mathf.Clamp(value, 0, GetStat(InnateStat.MaxCharge));
        HUDMenuUI.Instance.SetCharge(StatPercentage(currentCharge, InnateStat.MaxCharge));
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

        currentUltimate = Mathf.Clamp(currentUltimate + amount, 0, pi.statData.maxUltimateCharge);
        HUDMenuUI.Instance.UpdateUltimate(UltimatePercentage(currentUltimate));
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

        if (currentUltimate <= 0)
        {
            pi.pc.psm.SwapToUltimate();
        }
    }

    #endregion

    #region Finisher Methods

    public void ChangeFinisherCharge(float amount)
    {
        if (amount == 0) return;

        currentFinisher = Mathf.Clamp(currentFinisher + amount, 0, pi.statData.maxFinisherCharge);
        HUDMenuUI.Instance.UpdateFinisher(FinisherPercentage(currentFinisher));
    }

    public float FinisherPercentage(float value)
    {
        float maxFinisher = pi.statData.maxFinisherCharge;

        return maxFinisher > 0 ? value / maxFinisher : 0f;
    }

    public void ResetFinisherCharge(Attack a = null)
    {
        if (a != null && !pi.CurrentLoadout.elementLoadout.AttackIsFinisher(a)) return;

        currentFinisher = 0f;
        HUDMenuUI.Instance.UpdateFinisher(FinisherPercentage(currentFinisher));
    }

    #endregion

    #region Stat Checks

    public bool CanUseUltimate()
    {
        if (pi.pc.psm.movingState == MovingStates.NonCombat) return false;
        if (!pi.pc.psm.canAttack) return false;
        return pi.pc.psm.movingState == MovingStates.Katana || currentUltimate >= pi.statData.minActivationCharge;
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

        return FinisherPercentage(currentFinisher) >= 1f;
    }

    #endregion
}