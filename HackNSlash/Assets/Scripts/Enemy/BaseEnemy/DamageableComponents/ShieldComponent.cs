using System;
using Extensions.Timers;
using UnityEngine;

public class ShieldComponent : DamageableComponentBase
{
    [Header("Shield")]
    [SerializeField] private float maxShield = 100f;
    [SerializeField] private float currentShield = 0f;
    [SerializeField] private float shieldRegenDelay = 5f;
    [SerializeField] private float shieldRegenDuration = 10f;
    public float CurrentShieldPercentage => maxShield > 0 ? currentShield / maxShield : 0f;
    
    private CountdownTimer shieldRegenTimer;
    private bool isRegenDelayActive;

    /** <summary>Returns the current shield percentage [0,1]. Full shield = 1, broken = 0.</summary> */
    public override float Evaluate() => Mathf.Clamp01(CurrentShieldPercentage);

    public void ChangeShield(float amount)
    {
        currentShield = Mathf.Clamp(currentShield + amount, 0, maxShield);

        if (currentShield <= 0 && (shieldRegenTimer == null || !shieldRegenTimer.IsRunning))
        {
            BeginShieldRegen();
        }
        
    }
    
    public float GetDisplayShieldPercentage()
    {
        if (currentShield > 0)
        {
            return CurrentShieldPercentage;
        }
        else
        {
            float currentTime = shieldRegenTimer?.CurrentTime ?? 0f;
            return isRegenDelayActive ? 1 - (currentTime / shieldRegenDuration) : 0f;
        }
    }

    private void Awake()
    {
        currentShield = maxShield;
    }

    private void BeginShieldRegen()
    {
        isRegenDelayActive = false;
        shieldRegenTimer = new CountdownTimer(shieldRegenDelay);
        shieldRegenTimer.OnTimerStop += OnShieldRegenDelayFinish;
        shieldRegenTimer.Start();
    }
    
    private void OnShieldRegenDelayFinish()
    {
        shieldRegenTimer = new CountdownTimer(shieldRegenDuration);
        shieldRegenTimer.OnTimerStop += OnShieldRegenFinish;
        shieldRegenTimer.Start();
        isRegenDelayActive = true;
    }
    
    private void OnShieldRegenFinish()
    {
        isRegenDelayActive = false;
        currentShield = maxShield;
        shieldRegenTimer = null; // Clear the timer reference since regen is complete
    }
}