using System;
using System.Collections.Generic;
using UnityEngine;
using Animancer;
using UnityEngine.Serialization;
using Object = System.Object;

[CreateAssetMenu(menuName = "Player/MoveAnimData")]
public class MoveAnimData : ScriptableObject
{
    public MovingStates state;
    
    [NonSerialized]
    private Dictionary<WalkingAnimStates, Object> _animStates;
    
    [Header("Idle")]
    public ClipTransition idleClip;
    
    
    [Header("Walking")]
    public AnimLoop walkCycle;
    public AnimLoop sprintCycle;
    
    public AnimMixerLoop targetWalkCycle;
    
    [Header("Jumping")]
    public MixerTransition2D jumpClip;
    public MixerTransition2D doubleJumpClip;
    
    public AnimLoop fallClip;
    
    [Header("Other")]
    
    public ClipTransition swapClip;
    [FormerlySerializedAs("nextState")] public MovingStates swapNextState;
    public WeaponType[] swapWeapons;

    public ClipTransition ultClip;
    public MovingStates ultNextState;

    public WeaponType[] ultWeapons;

    private void OnValidate()
    {
        _animStates = new();
        
        _animStates.Add(WalkingAnimStates.Idle, idleClip);
        _animStates.Add(WalkingAnimStates.Walking, walkCycle);
        _animStates.Add(WalkingAnimStates.Sprinting, sprintCycle);
        _animStates.Add(WalkingAnimStates.Targeting, targetWalkCycle);
        _animStates.Add(WalkingAnimStates.Jumping, jumpClip);
        _animStates.Add(WalkingAnimStates.DoubleJumping, doubleJumpClip);
        _animStates.Add(WalkingAnimStates.Falling, fallClip);
        _animStates.Add(WalkingAnimStates.Swapping, swapClip);
        _animStates.Add(WalkingAnimStates.Ultimate, ultClip);
    }
    
    public Object GetAnimState(WalkingAnimStates walkState)
    {
        if (_animStates == null) OnValidate();
        
        return _animStates.GetValueOrDefault(walkState, null);
    }
}

public enum WalkingAnimStates
{
    Idle,
    Walking,
    Targeting,
    Sprinting,
    Falling,
    Jumping,
    DoubleJumping,
    Swapping,
    Ultimate
}

public abstract class Loop
{
    public abstract ITransition StartClip { get; }
    public abstract ITransition LoopClip { get; }
    public abstract ITransition EndClip { get; }
}

[Serializable]
public class AnimLoop : Loop
{
    [SerializeField] private ClipTransition startClip;
    [SerializeField] private ClipTransition loopClip;
    [SerializeField] private ClipTransition endClip;
    
    public override ITransition StartClip => startClip;
    public override ITransition LoopClip => loopClip;
    public override ITransition EndClip => endClip;
}

[Serializable]
public class AnimMixerLoop : Loop
{
    [SerializeField] private MixerTransition2D startClip;
    [SerializeField] private MixerTransition2D loopClip;
    [SerializeField] private MixerTransition2D endClip;
    
    public override ITransition StartClip => startClip;
    public override ITransition LoopClip => loopClip;
    public override ITransition EndClip => endClip;
}
