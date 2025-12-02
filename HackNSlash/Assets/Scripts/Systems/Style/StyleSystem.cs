using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Extensions.EventBus;
using Extensions.Patterns;
using Extensions.Utils;
using UnityEngine;

public class StyleSystem : MonoBehaviour
{
    [Header("Style Settings")]
    [SerializeField] private List<StyleSetting> styleSettings;
    
    private readonly RuleSystem<StyleContext, StyleResult> ruleSystem = new RuleSystem<StyleContext, StyleResult>();
    public StyleContext Context { get; private set; }
    
    public float CurrentStyleValue => Context.CurrentStyle;
    public StyleLevel CurrentStyleLevel => Context.GetLevel();
    
    private static StyleResult Combine(StyleResult a, StyleResult b) => a + b;
    
    #region MonoBehaviour Callbacks
    
    private void Awake()
    {
        Context = new StyleContext(StyleThresholds);
        Func<float, float> decayCurve = (style) =>
        {
            var level = StyleByThreshold(style);

            float baseDecay = level.decayRate;
            return baseDecay;
        };
        
        Func<int, float> multiHitMultiplier = (hits) => 1f + (hits - 1) * 0.25f;
        
        RegisterRule(new AttackBaseRule());
        RegisterRule(new VarietyRule(0.3f,0.5f));
        RegisterRule(new RepetitionPenaltyRule(-5f));
        RegisterRule(new MultiHitRule(multiHitMultiplier));
        RegisterRule(new AerialRule(3f));
        RegisterRule(new HitTakenPenaltyRule(1f, level => GetStyleSettings(level).penaltyRate));
        RegisterRule(new DecayRule(decayCurve));
    }

    private void Update()
    {
        Tick();
    }
    
    #endregion
    
    #region Style Rule Methods

    private void RegisterRule<TEvent>(IRule<TEvent, StyleContext, StyleResult> rule) where TEvent : IStyleEvent
    {
        ruleSystem.AddRule(rule);
    }
    
    private void RaiseEvent<TEvent>(TEvent eventData) where TEvent : IStyleEvent
    {
        var result = ruleSystem.ApplyRules(eventData, Context, Combine);
        
        if (!EqualityComparer<StyleResult>.Default.Equals(result, null))
        {
            if (result.BreakCombo) Context.ResetCombo();
            Context.AddStyle(result.StylePoints);
            if (result.ForceMaxRank)
            {
                Context.SetStyle(StyleLevel.X);
            }
        }
        
        EventBus<StyleUpdateEvent>.Raise(new StyleUpdateEvent(CurrentStyleLevel, CurrentStyleValue));
    }
    
    public void RaiseAttackEvent(Attack attack, int enemiesHit)
    {
        var attackEvent = new AttackStyleEvent(attack, enemiesHit);
        RaiseEvent(attackEvent);
    }
    
    public void RaiseHitEvent(HitInstance hitInstance)
    {
        var hitEvent = new HitStyleEvent(hitInstance);
        RaiseEvent(hitEvent);
    }

    private void Tick()
    {
        RaiseEvent(new TickStyleEvent(Time.deltaTime));
    }
    
    #endregion
    
    #region Style Setting Methods
    
    private void SortStylesByName()
    {
        styleSettings.Sort((a, b) => a.level.CompareTo(b.level));
    }
    
    private StyleSetting StyleByThreshold(float style)
    {
        SortStylesByName();
        
        for (int i = styleSettings.Count - 1; i >= 0; i--)
        {
            if (style >= styleSettings[i].threshold)
            {
                return styleSettings[i];
            }
        }
        return styleSettings.Count > 0 ? styleSettings[0] : null;
    }

    public StyleSetting GetStyleSettings(StyleLevel level)
    { 
        foreach (var setting in styleSettings)
        {
            if (setting.level == level)
            {
                return setting;
            }
        }
        return null;
    }

    public float GetStylePercentage(StyleLevel level, float value)
    {
        var setting = GetStyleSettings(level);
        var nextSetting = GetStyleSettings(level + 1);
        if (setting == null) return 0f;
        if (nextSetting == null) return 1f;

        float lowerThreshold = setting.threshold;
        float upperThreshold = nextSetting.threshold;
        
        return Mathf.Clamp01((value - lowerThreshold) / (upperThreshold - lowerThreshold));
    }

    private float[] StyleThresholds
    {
        get 
        {
            SortStylesByName();
        
            float[] thresholds = new float[styleSettings.Count];
            for (int i = 0; i < styleSettings.Count; i++)
            {
                thresholds[i] = styleSettings[i].threshold;
            }
            return thresholds;
        }
    }
    
    #endregion

    private void OnValidate()
    {
        SortStylesByName();
    }
}