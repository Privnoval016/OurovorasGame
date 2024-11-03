using System.Collections.Generic;
using UnityEngine;

public class StateController : MonoBehaviour
{
    Stack<State> currentState = new Stack<State>();
    [HideInInspector] public MonoBehaviour parent;

    void Update()
    {
        if (currentState.Count > 0)
        {
            currentState.Peek().OnUpdate();
        }
    }

    private void FixedUpdate()
    {
        if (currentState.Count > 0)
        {
            currentState.Peek().OnFixedUpdate();
        }
    }

    public void ChangeState(State newState)
    {
        RemoveTop();
        AddNewState(newState);
    }

    public void Interrupt(State newState)
    {
        currentState.Peek().OnInterrupt();
        AddNewState(newState);
    }

    public void ResumePrevious()
    {
        RemoveTop();
        if (currentState.Count > 0)
        {
            currentState.Peek().OnResume();
        }
    }
    
    public State GetCurrentState()
    {
        return currentState.Peek();
    }

    private void RemoveTop()
    {
        if (currentState.Count > 0 && !currentState.Peek().doNotRemove)
        {
            currentState.Peek().OnExit();
            currentState.Pop();
        }
    }

    private void AddNewState(State newState)
    {
        currentState.Push(newState);
        currentState.Peek().OnStateEnter(this);
    }

    public void ClearStates()
    {
        while (currentState.Count > 0 && !currentState.Peek().doNotRemove)
        {
            currentState.Pop();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentState.Count > 0)
        {
            currentState.Peek().OnTriggerEnter(other);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (currentState.Count > 0)
        {
            currentState.Peek().OnCollisionEnter(collision);
        }
    }
}
