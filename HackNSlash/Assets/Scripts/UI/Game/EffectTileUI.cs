using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Extensions.Timers;

public class EffectTileUI : MonoBehaviour
{
    [SerializeField] private List<EffectIconUI> effectIcons = new List<EffectIconUI>();

    [SerializeField] private float iconScrollInterval = 2f; // Time in seconds before swapping to the next set of icons
    
    private int IconsLength => effectIcons.Count; // Total number of effect icons that can be displayed at once
    private int currentStartIndex = 0; // Index of the first icon currently being displayed
    
    private LockOnTarget target;
    
    private CountdownTimer iconScrollTimer; // Timer to manage icon scrolling

    private void Awake()
    {
        iconScrollTimer = new CountdownTimer(iconScrollInterval, true);
        iconScrollTimer.OnTimerStop += OnIconTimerComplete;
    }

    private void Update()
    {
        RefreshEffectIcons();
    }
    
    public void SetTarget(LockOnTarget newTarget)
    {
        target = newTarget;
        currentStartIndex = 0; // Reset to the beginning when changing targets
        RefreshEffectIcons();
        iconScrollTimer.Reset();
    }
    
    private void OnIconTimerComplete()
    {
        if (target == null) return;

        var statusEffects = target.damageable.Stats.StatusEffects().ToList();
        int totalEffects = statusEffects.Count;

        if (totalEffects <= IconsLength)
        {
            // No need to scroll if all effects fit within the available icons
            return;
        }

        // Update the starting index for the next set of icons
        currentStartIndex += IconsLength;
        if (currentStartIndex >= totalEffects)
        {
            currentStartIndex = 0; // Loop back to the beginning
        }
        
        RefreshEffectIcons();
        
        // Restart the timer for the next scroll
        iconScrollTimer.Reset();
    }

    private void RefreshEffectIcons()
    {
        if (target == null) return;

        var statusEffects = target.damageable.Stats.StatusEffects().ToList();
        int totalEffects = statusEffects.Count;
        
        for (int i = 0; i < IconsLength; i++)
        {
            // Calculate the index of the effect to display in this icon, if 0 stacks move to next
            
            int effectIndex = currentStartIndex + i;
            
            while (effectIndex < totalEffects && statusEffects[effectIndex].Value <= 0)
            {
                effectIndex++;
            }
            
            if (effectIndex < totalEffects)
            {
                var effectKvp = statusEffects[effectIndex];
                effectIcons[i].SetEffect(effectKvp.Key, effectKvp.Value);
            }
            else
            {
                effectIcons[i].ClearEffect();
            }
        }
        
    }
}