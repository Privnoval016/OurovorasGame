using System;
using System.Collections.Generic;
using Extensions.UI;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class StyleMeterUI : MonoBehaviour
{
    private int fadeParamID = Shader.PropertyToID("_FadeAmount");
    [SerializeField] private Material fadeMaterial;
    [SerializeField] private SliderBar sliderBar;
    [SerializeField] private float fadeOutThreshold = 0.2f;
    
    [Header("Fade Behavior")]
    [SerializeField] private float idleTimeBeforeFade = 1.5f; // Time of no style gain before fading starts
    [SerializeField] private float fadeDuration = 0.5f;
    
    [Header("Below Threshold Display")]
    [Tooltip("Minimum visible fill when below threshold (e.g., 0.05 = 5% always visible)")]
    [SerializeField] private float minimumVisibleFill = 0.05f;
    [Tooltip("How much the bar fills in the danger zone (e.g., 0.3 = uses 30% of bar for 0-20% style)")]
    [SerializeField] private float dangerZoneFillRange = 0.25f;
    
    [Header("UI")]
    [SerializeField] private Image styleMeterBackground;
    [SerializeField] private List<Image> styleMeterFills;
    [SerializeField] private Image styleMeterOutline;
    
    public float TrueStylePercentage { get; private set; }
    
    private Material instancedMaterial;
    private Tween currentFadeTween;
    private float lastStyleValue = 0f;
    private float timeSinceLastStyleGain = 0f;
    private bool isGainingStyle = false;

    private void Start()
    {
        // Create an instanced material so we don't modify the original asset
        instancedMaterial = new Material(fadeMaterial);
        instancedMaterial.SetFloat(fadeParamID, 0f);
        
        // Apply the same instanced material to all elements so they fade together
        styleMeterBackground.material = instancedMaterial;
        styleMeterOutline.material = instancedMaterial;
        foreach (var fill in styleMeterFills)
        {            
            fill.material = instancedMaterial;
        }
    }

    private void Update()
    {
        // Always increment idle timer
        timeSinceLastStyleGain += Time.deltaTime;
        
        // Check if we should be fading
        if (TrueStylePercentage < fadeOutThreshold && timeSinceLastStyleGain >= idleTimeBeforeFade)
        {
            // Calculate and apply fade
            float targetFade;
            
            if (TrueStylePercentage <= 0f)
            {
                targetFade = 1f; // Fully faded
            }
            else
            {
                // Gradually fade based on how low we are
                targetFade = Mathf.Clamp01((fadeOutThreshold - TrueStylePercentage) / fadeOutThreshold);
            }
            
            // Get current fade value to avoid redundant tweens
            float currentFade = instancedMaterial.GetFloat(fadeParamID);
            
            // Only tween if there's a meaningful difference
            if (Mathf.Abs(currentFade - targetFade) > 0.01f)
            {
                if (currentFadeTween.isAlive)
                {
                    currentFadeTween.Stop();
                }
                
                currentFadeTween = Tween.MaterialProperty(
                    instancedMaterial, 
                    fadeParamID, 
                    targetFade, 
                    duration: fadeDuration,
                    ease: Ease.InOutCubic
                );
            }
        }
    }

    private void OnDestroy()
    {
        // Clean up the instanced material
        if (instancedMaterial != null)
        {
            Destroy(instancedMaterial);
        }
    }

    public void UpdateStyleValue(StyleUpdateEvent e)
    {
        var setting = Services.Get<StyleSystem>().GetStyleSettings(e.StyleLevel);
        if (setting == null) return;
        
        // Update sprites based on rank
        styleMeterBackground.sprite = setting.meterBackground;
        styleMeterOutline.sprite = setting.meterOutline;
        foreach (var fill in styleMeterFills)
        {
            fill.sprite = setting.meterFill;
        }
        
        // Get true style percentage (0-1 range)
        float stylePercentage = Services.Get<StyleSystem>().GetStylePercentage(e.StyleLevel, e.StyleValue);
        
        // Detect if we're gaining style
        isGainingStyle = stylePercentage > lastStyleValue;
        
        // Reset idle timer if we gained style OR if we're above threshold
        if (isGainingStyle || stylePercentage >= fadeOutThreshold)
        {
            timeSinceLastStyleGain = 0f;
            
            // If we just started gaining style or crossed back above threshold, ensure meter is fully visible
            if (TrueStylePercentage < fadeOutThreshold)
            {
                FadeToVisible();
            }
        }
        
        TrueStylePercentage = stylePercentage;
        lastStyleValue = stylePercentage;
        
        // Calculate display percentage with improved below-threshold visibility
        float displayPercentage;
        
        if (stylePercentage <= fadeOutThreshold)
        {
            // Below threshold: map to danger zone (0 to dangerZoneFillRange)
            // This gives a slower, compressed fill to show progress while staying low
            float normalizedDangerZone = stylePercentage / fadeOutThreshold; // 0 to 1 within danger zone
            displayPercentage = minimumVisibleFill + (normalizedDangerZone * (dangerZoneFillRange - minimumVisibleFill));
        }
        else
        {
            // Above threshold: map remaining range (threshold to 1.0) to fill the rest of the bar
            float normalizedAboveThreshold = (stylePercentage - fadeOutThreshold) / (1f - fadeOutThreshold);
            displayPercentage = dangerZoneFillRange + (normalizedAboveThreshold * (1f - dangerZoneFillRange));
        }
        
        displayPercentage = Mathf.Clamp01(displayPercentage);
        
        // Update slider bar
        sliderBar.TweenSliderValue(displayPercentage, 0.35f, 0, 0.25f);

        // Handle rank change animations
        switch (e.Swapped)
        {
            case StyleUpdateEvent.SwapDirection.Increased:
                UpgradeRank();
                break;
            case StyleUpdateEvent.SwapDirection.Decreased:
                LowerRank();
                break;
            case StyleUpdateEvent.SwapDirection.None:
                StyleChangePulse();
                break;
        }
    }

    private void FadeToVisible()
    {
        // Stop any existing fade tween
        if (currentFadeTween.isAlive)
        {
            currentFadeTween.Stop();
        }
        
        // Quickly fade to fully visible
        currentFadeTween = Tween.MaterialProperty(
            instancedMaterial, 
            fadeParamID, 
            0f, // Fully visible
            duration: 0.2f,
            ease: Ease.OutCubic
        );
    }

    private void LowerRank()
    {
        sliderBar.gameObject.AlphaFade(0, 1, 1f); // Reset alpha
    }

    private void UpgradeRank()
    {
        sliderBar.gameObject.AlphaFade(0, 1, 1f); // Reset alpha
        sliderBar.gameObject.PulseOutIn(1.7f, 0.2f, 0.05f, 0.1f);
    }
    
    private void StyleChangePulse()
    {
        sliderBar.gameObject.PulseOutIn(1.2f, 0.15f, 0f, 0.15f);
    }
}