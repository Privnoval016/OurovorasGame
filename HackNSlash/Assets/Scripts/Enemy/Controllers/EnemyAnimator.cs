using System;
using Object = System.Object;

public class EnemyAnimator : EntityAnimator
{
    public Object CurrentAnim;

    /** <summary>
     * Play <paramref name="animToPlay"/> on this enemy.
     * </summary>
     * <param name="animToPlay">The clip, transition, or <see cref="Loop"/> to play.</param>
     * <param name="onExit">Optional callback when the animation ends.</param>
     * <param name="playExit">When true and the current anim is a <see cref="Loop"/>,
     * the loop's end clip plays before transitioning.</param>
     * <param name="forceRestart">When true, forces the animation to restart from
     * frame 0 even if it is already the current state. Pass true for attacks that
     * must play again immediately when used consecutively.</param>
     */
    public void PlayEnemyAnimation(object animToPlay, Action onExit = null, bool playExit = false, bool forceRestart = false)
    {
        PlayEntityAnimation(CurrentAnim, animToPlay, onExit, playExit, forceRestart);
        CurrentAnim = animToPlay;
    }
}
