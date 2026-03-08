using System.Collections.Generic;
using Extensions.EntityComponent;
using Extensions.EventBus;
using Extensions.Modifiers;
using Extensions.Patterns;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

public class EnemyStats : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    
    [field: SerializeField] public int Level { get; private set; } = 5;
    [field: SerializeField] public int NumHealthBars { get; private set; } = 1;
    [field: SerializeField] public int MaxHealthBars { get; private set; } = 3;
    
    public BaseStats baseStats;

    public EvaluatedStats EvaluatedStats;
    [field: SerializeField] public float CurrentHealth { get; private set; }

    public Entity<IDamageableComponent> DamageableComponents { get; } = new Entity<IDamageableComponent>();

    public ElementEffect currentElementEffect = ElementEffect.None;

    #region MonoBehaviour Callbacks

    private void Awake()
    {
        EvaluatedStats = new EvaluatedStats(baseStats, () => Level);
        CurrentHealth = EvaluatedStats.GetInnateStat(InnateStat.MaxHealth);

        NumHealthBars = MaxHealthBars;
        
        DamageableComponents.AddComponent(new ElementComponent(() => currentElementEffect));
    }

    private void Update()
    {
        EvaluatedStats.Update();
    }

    #endregion

    #region Element Methods

    public ElementEffect GetElementFromAttack(ElementEffect attackElement)
    {
        ElementEffect element = attackElement;

        if (element == ElementEffect.MatchCurrent)
        {
            element = currentElementEffect;
        }
    
        return element;
    }

    #endregion

    #region Stat Methods

    public float GetStat(InnateStat stat)
    {
        return EvaluatedStats.GetInnateStat(stat);
    }
    
    public void ChangeHealth(float amount)
    {
        if (CurrentHealth + amount <= 0 && NumHealthBars >= 1)
        {
            NumHealthBars--;
            if (NumHealthBars > 0)
            {
                CurrentHealth = EvaluatedStats.GetInnateStat(InnateStat.MaxHealth);
                Debug.Log($"{gameObject.name} lost a health bar! Remaining health bars: {NumHealthBars}");
            }
            else
            {
                CurrentHealth = 0;
                Debug.Log($"{gameObject.name} has been defeated!");
                // Handle enemy defeat logic here (e.g., destroy the game object, play animation, etc.)
            }
        }
        else
        {
            CurrentHealth += amount;
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, EvaluatedStats.GetInnateStat(InnateStat.MaxHealth));
        }
    }
    
    public void ChangeShield(float amount)
    {
        if (!DamageableComponents.TryGetComponent<ShieldComponent>(out var shieldComponent)) return;
        shieldComponent.ChangeShield(amount);
    }

    #endregion

    #region Damageable Methods
    
    public virtual void ApplyStatusEffect(Modifier<StatusEffectQueryKey> statusEffectModifier)
    {
        if (statusEffectModifier?.Key == null ||
            statusEffectModifier.Key.Key is NoStatusEffect) return;
        
        EvaluatedStats.StatusEffectMediator.AddModifier(statusEffectModifier);
        Debug.Log($"{gameObject.name} applied status effect {statusEffectModifier.Key.Key}");
    }

    public virtual void TakeDamage(ElementEffect element, IDamageable attacker, IDamageEvent damageEvent)
    {
        float damageAmount = Services.Get<DamageSystem>().ResolveDamage(attacker, this, damageEvent).FinalDamage;
        
        Debug.Log($"{gameObject.name} took {damageAmount} damage of element {element}");
        
        ChangeHealth(-damageAmount);
        ChangeShield(-damageEvent.BaseShieldDamage);

        PlayHitAudio();
    }

    public virtual void Heal(float healAmount)
    {
        Debug.Log($"{gameObject.name} healed {healAmount} health");
    
        ChangeHealth(healAmount);
    }

    public EvaluatedStats Stats => EvaluatedStats;

    public IEnumerable<IDamageRule> DamageEvalRules
    => new List<IDamageRule>();

    #endregion
    
    public void PlayHitAudio()
    {
        AudioParamValue[] paramsArray = TryGetComponent(out InstanceParameter instance)
            ? new[] { instance.instanceParam.param }
            : null;

        EventBus<PlaySFXEvent>.Raise(new PlaySFXEvent(AudioLookupAtlas.Instance.enemyHitSound,
            transform, paramsArray));
    }
}

