using System;
using Object = System.Object;

public class EnemyAnimator : EntityAnimator
{
    public Object CurrentAnim;
    
    public void PlayEnemyAnimation(Object animToPlay, Action onExit = null, bool playExit = true)
    {
        PlayEntityAnimation(CurrentAnim, animToPlay, onExit, playExit);
        CurrentAnim = animToPlay;
    }
}

