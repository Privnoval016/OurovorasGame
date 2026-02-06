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
    [SerializeField] private float styleGainBufferTime = 0.5f;
    [Tooltip("The range of the shader fade effect (x = min fade, y = max fade). E.g., (0, 0.95) means fade from 0 to 0.95 as style goes from threshold to 0.")]
    [SerializeField] private Vector2 fadeRange = new Vector2(0f, 0.95f);
    
    [Header("Rank Change Buffer")]
    [Tooltip("Minimum percentage (0-1) of the new rank's bar that must be filled before committing to a rank up. E.g., 0.1 = must be at least 10% into the new rank.")]
    [SerializeField] private float rankUpCommitThreshold = 0.15f;

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
    
    private bool isPlayingRankChangeAnimation = false;
    private StyleLevel currentDisplayedRank = StyleLevel.D;
    private StyleUpdateEvent? pendingUpdate = null;

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
        currentDisplayedRank = StyleLevel.D;
        TrueStylePercentage = 0f;
        
        var styleSystem = Services.Get<StyleSystem>();
        if (styleSystem != null)
        {
            var initialSetting = styleSystem.GetStyleSettings(StyleLevel.D);
            if (initialSetting != null)
            {
                // Forcefully apply D rank visuals
                activeSliderBarIndices.Clear();
                activeSliderBarIndices.AddRange(initialSetting.meterUsageIndex);

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
                    sliderBar.SetSpriteForAll(initialSetting.meterFill);
                    sliderBar.SetBorderSprite(initialSetting.meterOutline);
                    sliderBar.SetBackgroundSprite(initialSetting.meterBackground);
                    sliderBar.SetMaterialForAll(instancedMaterial);
                    sliderBar.SetSliderValueInstant(0f);
                }

                transform.localScale = initialSetting.meterScale.ScaledBy(initialScale);
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

        // If we're currently playing a rank change animation, queue this update for later
        if (isPlayingRankChangeAnimation)
        {
            // Only queue if it's a different rank change
            if (e.StyleLevel != currentDisplayedRank)
            {
                pendingUpdate = e;
            }
            return;
        }

        // Determine if we should commit to showing a new rank
        bool shouldShowNewRank = false;
        
        if (e.StyleLevel != currentDisplayedRank)
        {
            // The actual rank differs from what we're displaying
            if (e.StyleLevel > currentDisplayedRank)
            {
                // Ranking up - only commit if above threshold OR if we explicitly got a rank increase event
                shouldShowNewRank = stylePercentage >= rankUpCommitThreshold || e.Swapped == StyleUpdateEvent.SwapDirection.Increased;
            }
            else
            {
                // Ranking down - always commit immediately
                shouldShowNewRank = true;
            }
        }

        // Handle rank changes
        if (shouldShowNewRank)
        {
            if (e.StyleLevel > currentDisplayedRank)
            {
                // RANK UP - Switch to new rank FIRST, then pulse
                isPlayingRankChangeAnimation = true;
                
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
                
                // Update to new rank FIRST
                currentDisplayedRank = e.StyleLevel;
                ApplyNewRankVisuals(setting, stylePercentage);
                
                // Fill the NEW rank bar to 100%
                foreach (var sliderBar in ActiveSliderBars)
                {
                    sliderBar.SetSliderValueInstant(1f);
                }
                
                // THEN pulse the new rank
                UpgradeRank(() => 
                {
                    // After pulse, update the bar to the actual percentage
                    UpdateBarDisplay(stylePercentage);
                    isPlayingRankChangeAnimation = false;
                    ProcessPendingUpdate();
                });
            }
            else
            {
                // RANK DOWN
                isPlayingRankChangeAnimation = true;
                
                // Rank down - just switch visuals
                isFadingOut = false;
                currentDisplayedRank = e.StyleLevel;
                ApplyNewRankVisuals(setting, stylePercentage);
                ResetMaterialFade();
                
                isPlayingRankChangeAnimation = false;
                ProcessPendingUpdate();
            }
        }
        else
        {
            // No rank change visual update - just update the bar
            HandleNoRankChange(stylePercentage, wasGaining);
        }
    }
    
    private void HandleNoRankChange(float stylePercentage, bool wasGaining)
    {
        // No rank change - handle fade-out logic and display updates
        HandleFadeOutLogic(stylePercentage, wasGaining);
        TrueStylePercentage = stylePercentage;
        UpdateBarDisplay(stylePercentage);
        
        // Only pulse if we're gaining style and not in fade-out
        if (wasGaining && !isFadingOut)
        {
            StyleChangePulse();
        }
    }
    
    private void ProcessPendingUpdate()
    {
        if (pendingUpdate.HasValue)
        {
            StyleUpdateEvent update = pendingUpdate.Value;
            pendingUpdate = null;
            UpdateStyleValue(update);
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
