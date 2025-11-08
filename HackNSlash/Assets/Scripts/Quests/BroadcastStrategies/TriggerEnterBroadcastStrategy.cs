using UnityEngine;

public class TriggerEnterBroadcastStrategy : IQuestEventBroadcastStrategy
{
    public TriggerListener triggerListener;
    
    protected override void OnInitialize()
    {
        base.OnInitialize();
        triggerListener.onTriggerEnter += OnTriggerEnter;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out PlayerController pc))
        {
            return;
        }
        
        Broadcast(Receiver);
    }

    public override string ToString()
    {
        return "Trigger Enter";
    }
}