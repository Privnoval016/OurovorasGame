using UnityEngine;
using Animancer;

[CreateAssetMenu(menuName = "Player/Attacks/PlayerAttack")]
public class PlayerAttack : Attack
{
    public OnAttackActions onAttackAction = OnAttackActions.None;
    
    [Tooltip("Case specific parameter (Dodge uses this)")]
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
    
    protected override void OnValidate()
    {
        base.OnValidate();
        
        if (hitInfo.onHitActions.Length == 0)
        {
            hitInfo.onHitActions = new[] {OnHitActions.BasicKnockBack};
        }
        
        if (attackType == AttackTypes.Spirit)
        {
            attackType = AttackTypes.Other;
        }
    }

    protected override void UpdateLinkedAttack()
    {
        base.UpdateLinkedAttack();
        if (linkedAttack == this) linkedAttack = null;
        if (linkedAttack == null) return;
        if (!updateLinkedAttack) return;
        if (linkedAttack is not PlayerAttack playerLinkedAttack) return;
        
        playerLinkedAttack.onAttackAction = onAttackAction;
        
        playerLinkedAttack.attackEventIndex = attackEventIndex;
        
        playerLinkedAttack.applyRootMotion = applyRootMotion;
        playerLinkedAttack.moveCameraWithAttack = moveCameraWithAttack;
        
        playerLinkedAttack.useNormalGravity = useNormalGravity;
        
        playerLinkedAttack.exitCondition = exitCondition;
    }
}

public enum ExitConditions
{
    AnimationEnd,
    Immediate,
    ExternalExit
}


