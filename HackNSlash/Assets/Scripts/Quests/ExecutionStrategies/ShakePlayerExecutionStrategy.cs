using UnityEngine;

public class ShakePlayerExecutionStrategy : IQuestEventExecutionStrategy
{
    protected override void OnInitialize()
    {
        base.OnInitialize();
        GameManager.Instance.pc.rb.AddForce(15 * Vector3.up, ForceMode.VelocityChange);
    }
    
    public override string ToString()
    {
        return "Shake Player";
    }
}