using System;
using Animancer;
using Extensions.Utils;
using UnityEngine;
using Object = System.Object;

public class EntityAnimator : MonoBehaviour
{
    
    [HideInInspector] public AnimancerState currentAnimState;
    
    public AnimancerComponent animancer;
    protected RedirectRootMotionToRigidbody rootMotion;
    
    #region MonoBehaviour Callbacks

    private void Awake()
    {
        animancer.TryGetComponent(out rootMotion);
    }

    #endregion
    
    #region Animator Methods

    public void SetAnimancerParam(string paramName, float value, bool damping = true)
    {
        Parameter<float> param = animancer.Parameters.GetOrCreate<float>(paramName);
        if (damping)
            param.Value = EaseUtil.Damp(param.Value, value, 2f, Time.deltaTime);
        else
            param.Value = value;
    }

    /** <summary>
     * Overload accepting a <see cref="StringAsset"/> directly so callers do not
     * have to perform a ToString conversion. Null-safe — does nothing when
     * <paramref name="paramName"/> is null (unassigned in the inspector).
     * </summary>
     */
    public void SetAnimancerParam(StringAsset paramName, float value, bool damping = true)
    {
        if (paramName == null) return;
        Parameter<float> param = animancer.Parameters.GetOrCreate<float>(paramName);
        if (damping)
            param.Value = EaseUtil.Damp(param.Value, value, 2f, Time.deltaTime);
        else
            param.Value = value;
    }
    
    public AnimancerState PlayAnimation(AnimationClip clip, float fadeDuration = -1F, bool canInterrupt = true, FadeMode mode = FadeMode.FromStart)
    {
        if (canInterrupt && (animancer.States.Current != null && animancer.States.Current.Clip && animancer.States.Current.Clip == clip))
        {
	        animancer.Stop();
        }
        
        currentAnimState = animancer.Play(clip, fadeDuration, mode);
        return currentAnimState;
    }
    
    public AnimancerState PlayAnimation(TransitionAsset clip, bool forceRestart = false)
    {
        if (forceRestart)
        {
            // Play with zero fade and FromStart so the state always begins at frame 0,
            // even if it was already the current state.
            currentAnimState = animancer.Play(clip, 0f, FadeMode.FromStart);
            return currentAnimState;
        }
        currentAnimState = animancer.Play(clip);
        return currentAnimState;
    }

    public AnimancerState PlayAnimation(ITransition clip, bool forceRestart = false)
    {
        if (forceRestart)
        {
            // Force a clean restart by playing with FromStart and a zero fade.
            // This is the correct Animancer pattern for re-triggering an animation
            // that may already be the current state.
            currentAnimState = animancer.Play(clip, 0f, FadeMode.FromStart);
            return currentAnimState;
        }
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
    
    protected void PlayEntityAnimation(Object currentAnim, Object nextAnim, Action onExit, bool playExit, bool forceRestart = false)
    {
        if (nextAnim == null) return;

        playExit = playExit && currentAnim is Loop;

        if (playExit)
        {
            Loop currentLoop = (Loop)currentAnim;
            if (nextAnim is Loop nextLoop)
                ExitTimeAnimation(currentLoop.EndClip, nextLoop.LoopClip, onExit);
            else
                ExitTimeAnimation(currentLoop.EndClip, (ITransition)nextAnim, onExit);
        }
        else if (nextAnim is Loop nextLoop)
        {
            ITransition loopClip = nextLoop.LoopClip;
            if (!IsPlayable(loopClip)) return;
            PlayAnimation(loopClip, forceRestart).Events(this).OnEnd ??= () => onExit?.Invoke();
        }
        else
        {
            ITransition transition = (ITransition)nextAnim;
            if (!IsPlayable(transition)) return;
            PlayAnimation(transition, forceRestart).Events(this).OnEnd ??= () => onExit?.Invoke();
        }
    }

    /** <summary>
     * Returns true when <paramref name="transition"/> is safe to hand to Animancer.
     * A transition is considered unplayable when it is null, or when it is a
     * <see cref="ClipTransition"/> whose <see cref="ClipTransition.Clip"/> has not
     * been assigned in the inspector. This prevents the ArgumentException Animancer
     * throws when trying to create a state from a null clip.
     * </summary>
     */
    protected static bool IsPlayable(ITransition transition)
    {
        if (transition == null) return false;
        if (transition is ClipTransition ct && ct.Clip == null) return false;
        return true;
    }

    #endregion
    
    #region Animancer Methods

    public void RootMotionEnabled(bool isEnabled)
    {
        rootMotion.enabled = isEnabled;
    }
    
    public void ActivateFootIK(bool isActive)
    {
        animancer.Graph.ApplyFootIK = isActive;
    }
    
    #endregion
    
}
