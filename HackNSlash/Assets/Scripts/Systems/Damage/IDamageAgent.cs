using System.Collections.Generic;
using Extensions.Patterns;

/**
 * <summary>
 * Interface for damage agents that can hook into the damage calculation system.
 * </summary>
 */
public interface IDamageAgent
{
    EvaluatedStats Stats { get; }
    
    IEnumerable<IRule<IDamageEvent, DamageContext, DamageResult>> DamageEvalRules { get; }
}