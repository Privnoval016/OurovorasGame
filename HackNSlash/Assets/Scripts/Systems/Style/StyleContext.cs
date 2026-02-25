using System.Collections.Generic;
using UnityEngine;

public enum StyleLevel 
{
    D = 0,
    C = 1,
    B = 2,
    A = 3,
    S = 4,
    SS = 5,
    SSS = 6,
    X = 7
    
}

public class StyleContext
{
    public float CurrentStyle { get; private set; } = 0f;
    public Attack LastAttack { get; set; } = null;
    public Dictionary<Attack, int> AttackHitCounts { get; private set; } = new Dictionary<Attack, int>();
    public float TimeSinceLastAction { get; private set; } = 0f;

    public readonly float[] StyleThresholds;
    
    public StyleContext(float[] styleThresholds = null)
    {
        StyleThresholds = styleThresholds ?? new float[] { 0f, 100f, 300f, 600f, 1000f, 1500f, 2100f, 2800f };
    }
    
    public void AddStyle(float amount)
    {
        CurrentStyle = Mathf.Clamp(CurrentStyle + amount, 0f, StyleThresholds[^1]);
        TimeSinceLastAction = 0f;
    }
    
    public void TickDecay(float deltaTime, float decayRate)
    {
        TimeSinceLastAction += deltaTime;
        AddStyle(-decayRate * deltaTime);
    }

    public StyleLevel GetLevel()
    {
        for (int i = StyleThresholds.Length - 1; i >= 0; i--)
        {
            if (CurrentStyle >= StyleThresholds[i])
            {
                return (StyleLevel)i;
            }
        }
        return StyleLevel.D;
    }
    
    public void SetStyle(StyleLevel level)
    {
        if ((int)level >= 0 && (int)level < StyleThresholds.Length)
        {
            CurrentStyle = StyleThresholds[(int)level];
            TimeSinceLastAction = 0f;
        }
    }
    
    public void ResetCombo()
    {
        LastAttack = null;
        AttackHitCounts.Clear();
    }
}