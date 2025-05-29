using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Extensions.Utils;
using Animancer;

[CreateAssetMenu(menuName = "Player/Attacks/PlayerAttack")]
public class PlayerAttack : Attack
{
    public OnAttackActions onAttackAction = OnAttackActions.None;
    
    [Tooltip("Case specific parameter")]
    public int attackEventIndex = 0;
    
    
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
    
    


    private void OnValidate()
    {
        if (hitInfo.onHitActions.Length == 0)
        {
            hitInfo.onHitActions = new[] {OnHitActions.BasicKnockBack};
        }
        
        if (attackType == AttackTypes.Spirit)
        {
            attackType = AttackTypes.Other;
        }
    }
}

public enum ExitConditions
{
    AnimationEnd,
    Immediate,
    ExternalExit
}


