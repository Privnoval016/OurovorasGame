using Extensions.Patterns;
using UnityEngine;

public class EffectSystem : MonoBehaviour, IEffectSystem
{
    [SerializeField] private StatusEffect[] statusEffects;
    
    public StatusEffect[] StatusEffects => statusEffects;
    
    
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

public interface IEffectSystem : IService
{
    StatusEffect[] StatusEffects { get; }
    StatusEffect GetStatusEffect(StatusEffect statusEffect);
}