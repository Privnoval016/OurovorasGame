public enum StatusEffectTargets
{
    None,
    Speed,          // affects move speed
    DamageDealt,    // affects damage dealt to others
    DamageTaken,    // affects damage taken from others
    DynamicDamage   // affects damage taken during updates/not necessarily just when hit (shared damage, DOT, etc)
}