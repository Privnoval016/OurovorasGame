using UnityEngine;

public class ShakePlayerExecutionStrategy : IQuestExecutionStrategy
{
    public float shakeForce = 15f;
    protected override void OnInitialize()
    {
        base.OnInitialize();
        GameManager.Instance.pc.rb.AddForce(shakeForce * Vector3.up, ForceMode.VelocityChange);
    }
    
    public override string ToString()
    {
        return "Shake Player";
    }
}