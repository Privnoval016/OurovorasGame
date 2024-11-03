using UnityEngine;

public abstract class State
{
    public StateController sc;
    public bool doNotRemove = false;

    public void OnStateEnter(StateController stateController)
    {
        sc = stateController;
        OnEnter();
    }

    public virtual void OnEnter() { }

    public virtual void OnUpdate() { }

    public virtual void OnFixedUpdate() { }

    public virtual void OnExit() { }
    
    public virtual void OnInterrupt() { }
    
    public virtual void OnResume() { }

    public virtual void OnTriggerEnter(Collider other) { }

    public virtual void OnCollisionEnter(Collision collision) { }
}