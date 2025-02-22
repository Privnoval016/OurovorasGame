using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerState
{
    public PlayerController sc;
    public bool doNotRemove = false;

    public void OnStateEnter(PlayerController stateController)
    {
        sc = stateController;
        OnEnter();
    }

    public virtual void OnEnter()
    {

    }

    public virtual void OnUpdate()
    {

    }

    public virtual void OnFixedUpdate()
    {

    }

    public virtual void OnExit()
    {

    }

    public virtual void OnTriggerEnter(Collider other)
    {

    }

    public virtual void OnCollisionEnter(Collision collision)
    {

    }
}