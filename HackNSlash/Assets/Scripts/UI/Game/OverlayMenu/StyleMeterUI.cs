using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.Timers;
using Extensions.UI;
using Extensions.Utils;
using PrimeTween;
using UnityEngine;

public class StyleMeterUI : MonoBehaviour
{
    private int fadeParamID = Shader.PropertyToID("_FadeAmount");
    [SerializeField] private Material fadeMaterial;
    [SerializeField] private SliderBar[] sliderBars;
    [SerializeField] private Transform[] sliderBarContainers;
    [SerializeField] private float fadeOutThreshold = 0.2f;

    [Header("Fade Behavior")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float styleGainBufferTime = 0.5f;
    [Tooltip("The range of the shader fade effect (x = min fade, y = max fade). E.g., (0, 0.95) means fade from 0 to 0.95 as style goes from threshold to 0.")]
    [SerializeField] private Vector2 fadeRange = new Vector2(0f, 0.95f);

    [Header("Below Threshold Display")]
    [Tooltip("Minimum visible fill when below threshold (e.g., 0.05 = 5% always visible)")]
    [SerializeField] private float minimumVisibleFill = 0.05f;
    [Tooltip("How much the bar fills in the danger zone (e.g., 0.3 = uses 30% of bar for 0-20% style)")]
    [SerializeField] private float dangerZoneFillRange = 0.25f;

    public float TrueStylePercentage { get; private set; }

    private Vector3 initialScale;

    private List<SliderBar> ActiveSliderBars =>
        sliderBars.Where((bar, index) => activeSliderBarIndices.Contains(index)).ToList();
    private List<Transform> ActiveSliderBarContainers =>
        sliderBarContainers.Where((container, index) => activeSliderBarIndices.Contains(index)).ToList();
    private List<int> activeSliderBarIndices = new List<int>();

    private Material instancedMaterial;
    private Tween currentFadeTween;

    private bool isFadingOut = false;
    private CountdownTimer styleGainBufferTimer;

    private void Start()
    {
        initialScale = transform.localScale;

        instancedMaterial = new Material(fadeMaterial);
        instancedMaterial.SetFloat(fadeParamID, fadeRange.x);

        foreach (var t in sliderBarContainers)
        {
            t.gameObject.SetActive(false);
        }

        foreach (var sliderBar in sliderBars)
        {
            if (sliderBar == null) continue;
            sliderBar.SetMaterialForAll(instancedMaterial);
        }
        
        // Initialize timer
        styleGainBufferTimer = new CountdownTimer(styleGainBufferTime);
        
        // Initialize with starting rank (D)
        var styleSystem = Services.Get<StyleSystem>();
        if (styleSystem != null)
        {
            var initialSetting = styleSystem.GetStyleSettings(StyleLevel.D);
            if (initialSetting != null)
            {
                ApplyNewRankVisuals(initialSetting, 0f);
            }
        }
    }

    private void OnDestroy()
    {
        if (instancedMaterial != null)
        {
            Destroy(instancedMaterial);
        }
        
        styleGainBufferTimer?.Dispose();
    }

    public void UpdateStyleValue(StyleUpdateEvent e)
    {
        var setting = Services.Get<StyleSystem>().GetStyleSettings(e.StyleLevel);
        if (setting == null) return;

        float stylePercentage = Services.Get<StyleSystem>().GetStylePercentage(e.StyleLevel, e.StyleValue);
        bool wasGaining = stylePercentage > TrueStylePercentage;

        // Handle rank changes FIRST with animations on OLD rank
        switch (e.Swapped)
        {
            case StyleUpdateEvent.SwapDirection.Increased:
                // Reset fade immediately and start buffer timer
                isFadingOut = false;
                styleGainBufferTimer.Reset();
                styleGainBufferTimer.Start();
                
                // Set material to fully visible instantly
                if (currentFadeTween.isAlive)
                {
                    currentFadeTween.Stop();
                }
                instancedMaterial.SetFloat(fadeParamID, fadeRange.x);
                
                // Fill the old rank bar to 100% before pulsing
                foreach (var sliderBar in ActiveSliderBars)
                {
                    sliderBar.SetSliderValueInstant(1f);
                }
                UpgradeRank(() => ApplyNewRankVisuals(setting, stylePercentage));
                return;
            case StyleUpdateEvent.SwapDirection.Decreased:
                // Rank down already happened - fadeout already occurred, just switch visuals
                isFadingOut = false;
                ApplyNewRankVisuals(setting, stylePercentage);
                ResetMaterialFade();
                return;
            case StyleUpdateEvent.SwapDirection.None:
                // No rank change - handle fade-out logic and display updates
                HandleFadeOutLogic(stylePercentage, wasGaining);
                TrueStylePercentage = stylePercentage;
                UpdateBarDisplay(stylePercentage);
                
                // Only pulse if we're gaining style and not in fade-out
                if (wasGaining && !isFadingOut)
                {
                    StyleChangePulse();
                }
                break;
        }
    }

    private void HandleFadeOutLogic(float stylePercentage, bool wasGaining)
    {
        // Only fade out when below threshold
        bool isBelowThreshold = stylePercentage < fadeOutThreshold;
        
        // ALWAYS cancel fadeout immediately when gaining style
        if (wasGaining)
        {
            styleGainBufferTimer.Reset();
            styleGainBufferTimer.Start();
            
            if (isFadingOut)
            {
                CancelFadeOut();
            }
            return;
        }
        
        // If above threshold, ensure we're not fading out
        if (!isBelowThreshold)
        {
            if (isFadingOut)
            {
                CancelFadeOut();
            }
            return;
        }
        
        // Check if we're still in the buffer period
        if (styleGainBufferTimer.IsRunning)
        {
            // Don't start fading out yet, still in buffer
            // But also ensure we're not fading out
            if (isFadingOut)
            {
                CancelFadeOut();
            }
            return;
        }
        
        // We're below threshold, losing style, and buffer has expired - start or update fadeout
        if (!isFadingOut)
        {
            StartFadeOut(stylePercentage);
        }
        else
        {
            UpdateFadeOut(stylePercentage);
        }
    }

    private void ApplyNewRankVisuals(StyleSetting setting, float stylePercentage)
    {
        activeSliderBarIndices.Clear();
        activeSliderBarIndices.AddRange(setting.meterUsageIndex);

        foreach (var t in sliderBarContainers)
        {
            t.gameObject.SetActive(false);
        }

        foreach (var index in activeSliderBarIndices)
        {
            sliderBarContainers[index].gameObject.SetActive(true);
        }

        foreach (var sliderBar in ActiveSliderBars)
        {
            sliderBar.SetSpriteForAll(setting.meterFill);
            sliderBar.SetBorderSprite(setting.meterOutline);
            sliderBar.SetBackgroundSprite(setting.meterBackground);
            // Reapply material to ensure it's on all images
            sliderBar.SetMaterialForAll(instancedMaterial);
        }

        transform.localScale = setting.meterScale.ScaledBy(initialScale);

        TrueStylePercentage = stylePercentage;

        UpdateBarDisplay(stylePercentage);
    }

    private void UpdateBarDisplay(float stylePercentage)
    {
        float displayPercentage;

        if (stylePercentage >= fadeOutThreshold)
        {
            // Above threshold: map to 0-100% of the bar
            // Style 1.0 → 100%, threshold → 0%
            float normalizedAboveThreshold = (stylePercentage - fadeOutThreshold) / (1f - fadeOutThreshold);
            displayPercentage = normalizedAboveThreshold;
        }
        else
        {
            // Below threshold (danger zone)
            // If we're gaining style or in buffer period, show minimal progress for positive feedback
            // Otherwise (fading out), keep bar at 0%
            if (!isFadingOut || styleGainBufferTimer.IsRunning)
            {
                // Map 0-threshold to 0-10% of the bar for visual feedback
                // This gives players positive reinforcement when gaining style in danger zone
                float normalizedDangerZone = stylePercentage / fadeOutThreshold;
                displayPercentage = normalizedDangerZone * 0.1f; // Show up to 10% progress
            }
            else
            {
                // Fully fading out with no recent style gain - bar stays empty
                displayPercentage = 0f;
            }
        }

        displayPercentage = Mathf.Clamp01(displayPercentage);

        foreach (var sliderBar in ActiveSliderBars)
        {
            sliderBar.TweenSliderValue(displayPercentage, 0.2f, 0, 0.1f);
        }
    }

    private void StartFadeOut(float currentPercentage)
    {
        isFadingOut = true;

        if (currentFadeTween.isAlive)
        {
            currentFadeTween.Stop();
        }

        // Map current percentage (0 to threshold) to fade range (fadeRange.x to fadeRange.y)
        float normalizedFade = 1f - (currentPercentage / fadeOutThreshold);
        float targetFade = Mathf.Lerp(fadeRange.x, fadeRange.y, normalizedFade);
        
        // Tween to the fade value instead of setting instantly
        currentFadeTween = Tween.MaterialProperty(
            instancedMaterial,
            fadeParamID,
            targetFade,
            duration: 0.2f,
            ease: Ease.Linear
        );
    }

    private void UpdateFadeOut(float currentPercentage)
    {
        // Map current percentage (0 to threshold) to fade range (fadeRange.x to fadeRange.y)
        float normalizedFade = 1f - (currentPercentage / fadeOutThreshold);
        normalizedFade = Mathf.Clamp01(normalizedFade);
        float targetFade = Mathf.Lerp(fadeRange.x, fadeRange.y, normalizedFade);

        if (currentFadeTween.isAlive)
        {
            currentFadeTween.Stop();
        }

        currentFadeTween = Tween.MaterialProperty(
            instancedMaterial,
            fadeParamID,
            targetFade,
            duration: 0.1f,
            ease: Ease.Linear
        );
    }

    private void CancelFadeOut()
    {
        isFadingOut = false;
        ResetMaterialFade();
    }

    private void ResetMaterialFade()
    {
        if (currentFadeTween.isAlive)
        {
            currentFadeTween.Stop();
        }

        currentFadeTween = Tween.MaterialProperty(
            instancedMaterial,
            fadeParamID,
            fadeRange.x,
            duration: 0.2f,
            ease: Ease.OutCubic
        );
    }

    private void UpgradeRank(Action onComplete)
    {
        var currentContainers = ActiveSliderBarContainers.ToList();
        if (currentContainers.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        // Quick pulse animation - much faster than before
        float durationOut = 0.2f;
        float durationStay = 0.15f;
        float durationIn = 0.1f;
        
        foreach (var container in currentContainers)
        {
            container.gameObject.PulseOutIn(1.8f, durationOut, durationStay, durationIn);
        }
        
        // Invoke callback after animation duration
        float delay = durationOut + durationStay + durationIn;
        Tween.Delay(delay, onComplete);
    }

    private void StyleChangePulse()
    {
        foreach (var container in ActiveSliderBarContainers)
        {
            container.gameObject.PulseOutIn(1.1f, 0.05f, 0f, 0.05f);
        }
    }
}
