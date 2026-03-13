using System;
using Extensions.UtilityAI;
using UnityEngine;

/** <summary>
 * Wander around the spawn point until a time budget is exhausted.
 * </summary>
 */
[Serializable]
public class WanderStrategy : IMovementStrategy
{
    [Tooltip("If true, the brain can interrupt this action to choose a new one. If false, the enemy will be committed to the back jump until it finishes.")]
    public bool isInterruptible;
    public bool IsInterruptible => isInterruptible;
    
    [Tooltip("Seconds to wander before the brain re-evaluates.")]
    [Min(0.5f)] public float duration = 4f;

    private float _elapsed;

    public string DisplayName => "Wander";

    public void OnEnter(EnemyContext context, EnemyStateMachine esm, EnemyAnimData animData)
    {
        _elapsed = 0f;
        esm.ts.ea.RootMotionEnabled(false);
        var clip = animData?.WalkClip();
        if (clip != null) esm.ts.ea.PlayEnemyAnimation(clip);
    }

    public void OnUpdate(EnemyContext context, EnemyStateMachine esm) { _elapsed += Time.deltaTime; esm.Wander(); }
    public void OnExit(EnemyContext context, EnemyStateMachine esm) => esm.Brake();
    public bool IsComplete(EnemyContext context, EnemyStateMachine esm) => _elapsed >= duration;
}