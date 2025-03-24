using UnityEngine;

public class SpiritFollowing : SpiritState
{
    public override void OnEnter()
    {
        doNotRemove = true;
    }

    public override void OnUpdate()
    {
        FollowPlayer();
    }

    private void FollowPlayer()
    {
        spirit.evaluator.SetTargetTransform(spirit.ClosestTarget);

        spirit.transform.position = spirit.evaluator.output + spirit.VerticalBob();
    }
}
