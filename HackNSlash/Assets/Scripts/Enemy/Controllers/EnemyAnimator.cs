using System;
using Object = System.Object;

public class EnemyAnimator : EntityAnimator
{
    public Object CurrentAnim = null;
    
    public void SwitchAnimState(Object nextAnim, Action onExit = null, bool playExit = false)
    {
        PlayEntityAnimation(CurrentAnim, nextAnim, onExit, playExit);
        CurrentAnim = nextAnim;
    }
}

