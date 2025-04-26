using Animancer;
using UnityEngine;

[CreateAssetMenu(menuName = "Player/HitAnimData")]
public class HitAnimData : ScriptableObject
{
    [Header("Hit Knockback Animations")]
    public MixerTransition2D groundHit;
    public AnimLoop airHit;
    
    [Header("Death Animations")]
    
    public ClipTransition deathClip;
}
