using System;
using Object = System.Object;

public class EnemyAnimator : EntityAnimator
{
    public Object CurrentAnim;
    
    public void PlayEnemyAnimation(object animToPlay, Action onExit = null, bool playExit = false)
    {
        PlayEntityAnimation(CurrentAnim, animToPlay, onExit, playExit);
        CurrentAnim = animToPlay;
    }
}

