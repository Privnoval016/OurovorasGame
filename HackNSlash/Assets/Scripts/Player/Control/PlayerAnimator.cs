using System;
using Animancer;
using AYellowpaper.SerializedCollections;
using ExtensionUtils;
using UnityEngine;
using Object = System.Object;

public class PlayerAnimator : MonoBehaviour
{
    #region Inspector Variables
    
    [HideInInspector]
    public PlayerController pc;

    public StringAsset[] parameterNames;
    
    public AnimancerComponent animancer;
    public RedirectRootMotionToRigidbody rootMotion;
    
    
    [SerializedDictionary("Move State", "Animation Data")]
    public SerializedDictionary<MovingStates, MoveAnimData> moveAnimDataDict;
    
    
    #endregion


    [HideInInspector] public AnimancerState currentAnimState;
    
    public MoveAnimData MovingAnims => moveAnimDataDict[pc.psm.movingState];
    
    [HideInInspector]
    public WalkingAnimStates walkingAnim;

    
    
    #region MonoBehaviour Callbacks
    
    private void Awake()
    {
        pc = GetComponent<PlayerController>();
        animancer.TryGetComponent(out rootMotion);
    }

    private void Update()
    {
        UpdateAnimatorState();
    }

    #endregion


    #region Animator Methods

    private void UpdateAnimatorState()
    {
        foreach (StringAsset parameterName in parameterNames)
        {
            Parameter<float> param = animancer.Parameters.GetOrCreate<float>(parameterName);
            
            if (parameterName == "MoveX")
            {
                param.Value = EaseUtil.Damp(param.Value, pc.psm.StandardizedMoveDir.normalized.x, 2f, Time.deltaTime);
            }
            else if (parameterName == "MoveZ")
            {
                param.Value = EaseUtil.Damp(param.Value, pc.psm.StandardizedMoveDir.normalized.y, 2f, Time.deltaTime);
            }
        }
        
    }
    
    public AnimancerState PlayAnimation(AnimationClip clip, float fadeDuration = -1F, bool canInterrupt = true, FadeMode mode = FadeMode.FixedSpeed)
    {
        if (canInterrupt && animancer.States.Current.Clip == clip)
        {
            currentAnimState.Time = 0;
            return currentAnimState;
        }
        
        currentAnimState = animancer.Play(clip, fadeDuration, mode);
        return currentAnimState;
    }
    
    public AnimancerState PlayAnimation(TransitionAsset clip)
    {
        currentAnimState = animancer.Play(clip);
        return currentAnimState;
    }
    
    public AnimancerState PlayAnimation(ITransition clip)
    {
        currentAnimState = animancer.Play(clip);
        return currentAnimState;
    }
    
    
    public void ExitTimeAnimation(ITransition currentAnim, ITransition nextAnim, Action onExit = null)
    {
        AnimancerState state = PlayAnimation(currentAnim);
        state.Events(this).OnEnd ??= () => OnAnimExit(nextAnim, onExit);
    }
	
    public void OnAnimExit(ITransition nextAnim, Action onExit = null)
    {
        if (nextAnim != null) PlayAnimation(nextAnim);
        onExit?.Invoke();
    }
    
    public void ExitTimeAnimation(AnimationClip currentAnim, AnimationClip nextAnim, Action onExit = null)
    {
        AnimancerState state = PlayAnimation(currentAnim, -1F, false);
        state.Events(this).OnEnd ??= () => OnAnimExit(nextAnim, onExit);
    }
	
    public void OnAnimExit(AnimationClip nextAnim, Action onExit = null)
    {
        if (nextAnim != null) PlayAnimation(nextAnim, -1F, false);
        onExit?.Invoke();
    }
    
    public void StopCurrentAnimation()
    {
        animancer.Stop();
    }

    #endregion
    
    
    #region PlayerMoving Methods

    public void UpdateAnimation()
    {
	    switch (walkingAnim)
	    {
		    case WalkingAnimStates.Idle:
			    if (pc.psm.IsMidair) SwitchAnimState(WalkingAnimStates.Falling);
			    else if (pc.cam.isLockedOn && pc.psm.IsWalking) SwitchAnimState(WalkingAnimStates.Targeting);
			    else if (pc.psm.IsSprinting) SwitchAnimState(WalkingAnimStates.Sprinting);
			    else if (pc.psm.IsWalking) SwitchAnimState(WalkingAnimStates.Walking);

			    break;
		    
		    case WalkingAnimStates.Walking:
			    pc.pac.currentAnimState.Speed = pc.rb.linearVelocity.ZeroVector3Axis().magnitude / pc.psm.playerData.runMaxSpeed;
			    
			    if (pc.psm.IsMidair) SwitchAnimState(WalkingAnimStates.Falling);
			    else if (pc.cam.isLockedOn) SwitchAnimState(WalkingAnimStates.Targeting);
			    else if (pc.psm.IsSprinting) SwitchAnimState(WalkingAnimStates.Sprinting);
			    else if (!pc.psm.IsWalking) SwitchAnimState(WalkingAnimStates.Idle, null, true);

			    break;
		    
		    case WalkingAnimStates.Sprinting:
			    
			    currentAnimState.Speed = pc.rb.linearVelocity.ZeroVector3Axis().magnitude / pc.psm.playerData.sprintMaxSpeed;
			    
			    if (pc.psm.IsMidair) SwitchAnimState(WalkingAnimStates.Falling);
			    else if (pc.cam.isLockedOn) SwitchAnimState(WalkingAnimStates.Targeting);
			    else if (!pc.psm.IsWalking) SwitchAnimState(WalkingAnimStates.Idle, null, true);
			    else if (!pc.psm.IsSprinting) SwitchAnimState(WalkingAnimStates.Walking);

			    break;
		    
		    case WalkingAnimStates.Targeting:
			    
			    currentAnimState.Speed = pc.rb.linearVelocity.ZeroVector3Axis().magnitude / pc.psm.playerData.runMaxSpeed;
			    
			    if (pc.psm.IsMidair) SwitchAnimState(WalkingAnimStates.Falling);
			    else if (!pc.psm.IsWalking) SwitchAnimState(WalkingAnimStates.Idle, null, true);
			    else if (!pc.cam.isLockedOn && !pc.psm.IsSprinting) SwitchAnimState(WalkingAnimStates.Walking);
			    else if (pc.psm.IsSprinting) SwitchAnimState(WalkingAnimStates.Sprinting);
			    break;
		    
		    case WalkingAnimStates.Falling:
			    if (pc.psm.IsGrounded) SwitchAnimState(WalkingAnimStates.Idle);
			    
			    break;
		    
		    case WalkingAnimStates.Jumping:
			    if (!pc.psm.isJumpFalling || pc.rb.linearVelocity.y > 0) break;
			    
			    if (pc.psm.IsGrounded) SwitchAnimState(WalkingAnimStates.Idle);
			    else if (pc.psm.IsMidair) SwitchAnimState(WalkingAnimStates.Falling);

			    break;
		    
		    case WalkingAnimStates.DoubleJumping:
			    break;
		    
		    case WalkingAnimStates.Swapping:
			    break;
	    }
    }
    
    public void SwitchAnimState(WalkingAnimStates newState, Action onExit = null, bool playExit = false)
    {
	    Object currentAnim = MovingAnims._animStates[walkingAnim];
	    Object nextAnim = MovingAnims._animStates[newState];
	    
	    playExit = playExit && currentAnim is Loop;

	    if (playExit)
	    {
		    Loop currentLoop = (Loop) currentAnim;
		    
		    if (nextAnim is Loop nextLoop)
		    {
			    ExitTimeAnimation(currentLoop.EndClip, nextLoop.LoopClip, onExit);
		    }
		    else
		    {
			    ExitTimeAnimation(currentLoop.EndClip, (ITransition) nextAnim, onExit);
		    }
	    }
	    else if (nextAnim is Loop nextLoop)
	    {
		    PlayAnimation(nextLoop.LoopClip).Events(this).OnEnd ??= () => onExit?.Invoke();
	    }
	    else
	    {
		    PlayAnimation((ITransition) nextAnim).Events(this).OnEnd ??= () => onExit?.Invoke();
	    }
	    
	    InputManager.Instance.ReleaseHoldAttacks();
	    walkingAnim = newState;
    }
    
    
    #endregion
}
