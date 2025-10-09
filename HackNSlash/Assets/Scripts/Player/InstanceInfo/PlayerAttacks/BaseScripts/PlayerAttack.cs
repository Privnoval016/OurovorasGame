using System;
using UnityEngine;
using Animancer;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Player/Attacks/PlayerAttack")]
public class PlayerAttack : Attack
{
    public AttackActionInfo[] attackActions;
    
    [Header("Root Motion")]
    public bool applyRootMotion = true;
    public bool moveCameraWithAttack = true;
    
    [Header("Animation")]
    
    public TransitionAsset[] attackTransitions;
    public AnimationClip[] attackClips;
    
    public float animDelay = 0;
    public float animFade = 0.2f;

    public int clipsToPlay = 1;

    public bool useNormalGravity = false;
    
    public ExitConditions exitCondition = ExitConditions.AnimationEnd;
    
    protected override void OnValidate()
    {
        base.OnValidate();
        
        if (attackType == AttackTypes.Spirit)
        {
            attackType = AttackTypes.Other;
        }
    }
}

[Serializable]
public struct AttackActionInfo
{
    [FormerlySerializedAs("playerAttackStrategy")] [SerializeReference] public IAttackAction attackAction;
}

public enum ExitConditions
{
    AnimationEnd,
    Immediate,
    ExternalExit
}


