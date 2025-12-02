using Extensions.Utils;

public interface IStyleEvent { }

public readonly struct AttackStyleEvent : IStyleEvent
{
    public readonly Attack Attack;
    public readonly float Damage => Attack.stats.damage;
    public readonly float BaseStyle => Attack.stats.baseStyleGain;
    public readonly bool IsAerial => Attack.isMidair == NBool.True;
    public readonly int EnemiesHit;
    
    public AttackStyleEvent(Attack attack, int enemiesHit)
    {
        Attack = attack;
        EnemiesHit = enemiesHit;
    }
}

public readonly struct HitStyleEvent : IStyleEvent
{
    public readonly HitInstance HitInstance;
    public float DamageTaken => HitInstance.damage;
    
    public HitStyleEvent(HitInstance hitInstance)
    {
        HitInstance = hitInstance;
    }
}

public readonly struct TickStyleEvent : IStyleEvent
{
    public readonly float DeltaTime;
    
    public TickStyleEvent(float deltaTime)
    {
        DeltaTime = deltaTime;
    }
}