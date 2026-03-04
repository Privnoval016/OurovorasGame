using System;
using System.Collections.Generic;
using Extensions.EntityComponent;
using Extensions.Modifiers;
using UnityEngine;

/**
 * <summary>
 * Interface for damageable entities that can take damage, heal, and apply status effects.
 * Inherits from IDamageAgent to include damage-calculation functionalities.
 * </summary>
 */
public interface IDamageable
{
    public Entity<IDamageableComponent> DamageableComponents { get; }
    
    public float CurrentHealth { get; }
    public int Level { get; }
    public int NumHealthBars { get; }
    
    public void ApplyStatusEffect(Modifier<StatusEffectQueryKey> statusEffectModifier);
    
    public void TakeDamage(ElementEffect element, IDamageable attacker, IDamageEvent damageEvent);
    
    public void Heal(float healAmount);
    
    EvaluatedStats Stats { get; }
    
    IEnumerable<IDamageRule> DamageEvalRules { get; }
}

public interface IDamageableComponent : IComponent
{
    /** <summary>
     * Returns a normalised [0,1] value representing this component's current state for use
     * as a context value in the utility AI.
     * Return 1 when the component's "active" or "full" condition is met, 0 when it is not.
     * </summary>
     */
    float Evaluate();
}

public class ShieldComponent : IDamageableComponent
{
    public WrappedField<float> CurrentShieldPercentage { get; private set; }

    public ShieldComponent(Func<float> onShieldChange)
    {
        CurrentShieldPercentage = new WrappedField<float>(onShieldChange);
    }

    /** <summary>Returns the current shield percentage [0,1]. Full shield = 1, broken = 0.</summary> */
    public float Evaluate() => Mathf.Clamp01(CurrentShieldPercentage.Value);
}

public class ElementComponent : IDamageableComponent
{
    public WrappedField<ElementEffect> CurrentElementEffect { get; private set; }

    public ElementComponent(Func<ElementEffect> onElementChange)
    {
        CurrentElementEffect = new WrappedField<ElementEffect>(onElementChange);
    }

    /** <summary>Returns 1 if the element is not None, 0 if it is None.</summary> */
    public float Evaluate() => CurrentElementEffect.Value != ElementEffect.None ? 1f : 0f;
}