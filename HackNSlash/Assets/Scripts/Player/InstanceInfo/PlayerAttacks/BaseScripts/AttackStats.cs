using UnityEngine;

[System.Serializable]
public class AttackStats
{
    [Header("Damage Properties")] 
    public float damage;
    public int statusEffectStacks;
    public float statusEffectDuration;

    public AttackStats(float damage, int statusEffectStacks = 0, float statusEffectDuration = 0)
    {
        this.damage = damage;
        this.statusEffectStacks = statusEffectStacks;
        this.statusEffectDuration = statusEffectDuration;
    }
}