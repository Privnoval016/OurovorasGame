using System;
using System.Collections.Generic;
using Animancer;
using Extensions.Utils;
using UnityEngine;
using Object = System.Object;

[Serializable]
public class AnimContainer
{
	public MovingStates movingState;
	public MoveAnimData moveAnimData;
	public HitAnimData hitAnimData;
}

public class PlayerAnimator : EntityAnimator
{
    #region Inspector Variables
    
    [HideInInspector]
    public PlayerController pc;

    public StringAsset[] parameterNames;
    
    public AnimContainer[] animDataArray;
    
    private Dictionary<MovingStates, AnimContainer> animDataDict;
    
    #endregion
    
    public MoveAnimData MovingAnims => animDataDict[pc != null ? pc.psm.movingState : MovingStates.NonCombat].moveAnimData;
    public HitAnimData HitAnims => animDataDict[pc != null ? pc.psm.movingState : MovingStates.NonCombat].hitAnimData;
    
    [HideInInspector]
    public WalkingAnimStates walkingAnim;

    
    
    #region MonoBehaviour Callbacks
    
    private void Awake()
    {
        pc = GetComponent<PlayerController>();
        animancer.TryGetComponent(out rootMotion);
        
        animDataDict = new Dictionary<MovingStates, AnimContainer>();
        foreach (AnimContainer animContainer in animDataArray)
		{
			animDataDict.TryAdd(animContainer.movingState, animContainer);
		}
    }

    private void Update()
    {
        UpdateAnimatorState();
    }

    #endregion

    private void UpdateAnimatorState()
    {
        foreach (StringAsset parameterName in parameterNames)
        {
            Parameter<float> param = animancer.Parameters.GetOrCreate<float>(parameterName);
            
            if (parameterName == "MoveX")
            {
	            SetAnimancerParam("MoveX", pc.psm.StandardizedMoveDir.normalized.x);
            }
            else if (parameterName == "MoveZ")
            {
	            SetAnimancerParam("MoveZ", pc.psm.StandardizedMoveDir.normalized.y);
            }
        }
        
    }
    
    
    #region PlayerMoving Methods

    public void UpdateAnimation()
    {
	    switch (walkingAnim)
	    {
		    case WalkingAnimStates.Idle:
			    if (pc.psm.IsMidair) SwitchAnimState(WalkingAnimStates.Falling);
			    else if (pc.cam.IsLockedOn && pc.psm.IsWalking) SwitchAnimState(WalkingAnimStates.Targeting);
			    else if (pc.psm.IsSprinting) SwitchAnimState(WalkingAnimStates.Sprinting);
			    else if (pc.psm.IsWalking) SwitchAnimState(WalkingAnimStates.Walking);

			    break;
		    
		    case WalkingAnimStates.Walking:
			    pc.pac.currentAnimState.Speed = pc.rb.linearVelocity.ZeroVector3Axis().magnitude / pc.psm.playerData.runMaxSpeed;
			    
			    if (pc.psm.IsMidair) SwitchAnimState(WalkingAnimStates.Falling);
			    else if (pc.cam.IsLockedOn) SwitchAnimState(WalkingAnimStates.Targeting);
			    else if (pc.psm.IsSprinting) SwitchAnimState(WalkingAnimStates.Sprinting);
			    else if (!pc.psm.IsWalking) SwitchAnimState(WalkingAnimStates.Idle, null, true);

			    break;
		    
		    case WalkingAnimStates.Sprinting:
			    
			    currentAnimState.Speed = pc.rb.linearVelocity.ZeroVector3Axis().magnitude / pc.psm.playerData.sprintMaxSpeed;
			    
			    if (pc.psm.IsMidair) SwitchAnimState(WalkingAnimStates.Falling);
			    else if (pc.cam.IsLockedOn) SwitchAnimState(WalkingAnimStates.Targeting);
			    else if (!pc.psm.IsWalking) SwitchAnimState(WalkingAnimStates.Idle, null, true);
			    else if (!pc.psm.IsSprinting) SwitchAnimState(WalkingAnimStates.Walking);

			    break;
		    
		    case WalkingAnimStates.Targeting:
			    
			    currentAnimState.Speed = pc.rb.linearVelocity.ZeroVector3Axis().magnitude / pc.psm.playerData.runMaxSpeed;
			    
			    if (pc.psm.IsMidair) SwitchAnimState(WalkingAnimStates.Falling);
			    else if (!pc.psm.IsWalking) SwitchAnimState(WalkingAnimStates.Idle, null, true);
			    else if (!pc.cam.IsLockedOn && !pc.psm.IsSprinting) SwitchAnimState(WalkingAnimStates.Walking);
			    else if (pc.psm.IsSprinting) SwitchAnimState(WalkingAnimStates.Sprinting);
			    break;
		    
		    case WalkingAnimStates.Falling:
			    if (!pc.psm.LastOnGroundTimer.IsFinished)
				    SwitchAnimState(WalkingAnimStates.Idle);
			    
			    break;
		    
		    case WalkingAnimStates.Jumping:
			    break;
		    
		    case WalkingAnimStates.DoubleJumping:
			    break;
		    
		    case WalkingAnimStates.Swapping:
			    break;
	    }
    }
    
    public void SwitchAnimState(WalkingAnimStates newState, Action onExit = null, bool playExit = false)
    {
	    Object currentAnim = MovingAnims.GetAnimState(walkingAnim);
	    Object nextAnim = MovingAnims.GetAnimState(newState);
	    
	    PlayEntityAnimation(currentAnim, nextAnim, onExit, playExit);
	    
	    InputManager.Instance.ReleaseHoldAttacks();
	    walkingAnim = newState;
    }
    
    
    #endregion
}
