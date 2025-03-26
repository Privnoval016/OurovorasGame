using UnityEngine;

public class SpiritFollowing : SpiritState
{
    public override void OnEnter()
    {
        doNotRemove = true;
    }

    public override void OnUpdate()
    {
        spirit.FollowPlayer();
    }
}
