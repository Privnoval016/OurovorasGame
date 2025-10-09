using Extensions.Patterns;
using UnityEngine;

public class EffectManager : Singleton<EffectManager>
{
    public StatusEffect[] statusEffects;
    
    #region MonoBehaviour Callbacks
    
    protected override void Awake()
    {
        base.Awake();
    }
    
    #endregion
    
    
    #region Status Effect Methods
    
    public StatusEffect GetStatusEffect(StatusEffect statusEffect)
    {
        statusEffect ??= ScriptableObject.CreateInstance<NoStatusEffect>();
        
        foreach (var effect in statusEffects)
        {
            if (effect.Equals(statusEffect))
                return effect;
        }

        Debug.LogError($"Status Effect {statusEffect} not found in EffectManager.");
        return null;
    }
    
    #endregion
}