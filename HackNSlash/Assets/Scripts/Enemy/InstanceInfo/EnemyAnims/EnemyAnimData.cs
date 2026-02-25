using Animancer;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAnimData", menuName = "Enemy/EnemyAnimData", order = 1)]
public class EnemyAnimData : ScriptableObject
{
    [Header("Idle")]
    public ClipTransition idleClip;
    
    
    [Header("Walking")]
    public AnimLoop walkCycle;
    
    [Header("Hit")]
    public ClipTransition groundHitClip;
    public ClipTransition airHitClip;
    public ClipTransition getUpClip;
    public ClipTransition staggerClip;
}
