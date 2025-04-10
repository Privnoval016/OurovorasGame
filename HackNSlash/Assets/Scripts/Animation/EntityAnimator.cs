using System;
using Animancer;
using Extensions.Utils;
using UnityEngine;

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
    
    public AnimancerState PlayAnimation(AnimationClip clip, float fadeDuration = -1F, bool canInterrupt = true, FadeMode mode = FadeMode.FixedSpeed)
    {
        if (canInterrupt && (animancer.States.Current.Clip && animancer.States.Current.Clip == clip))
        {
	        animancer.Stop();
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
    
    #region Root Motion Methods

    public void RootMotionEnabled(bool enabled)
    {
        rootMotion.enabled = enabled;
    }
    
    #endregion
    
}
