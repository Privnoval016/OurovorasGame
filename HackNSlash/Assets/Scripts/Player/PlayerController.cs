using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    Stack<PlayerState> currentState = new Stack<PlayerState>();

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
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

    public void ChangeState(PlayerState newState)
    {
        RemoveTop();
        AddNewState(newState);
    }

    public void Interrupt(PlayerState newState)
    {
        AddNewState(newState);
    }

    public void ResumePrevious()
    {
        RemoveTop();
    }

    public void RemoveTop()
    {
        if (currentState.Count > 0 && !currentState.Peek().doNotRemove)
        {
            currentState.Peek().OnExit();
            currentState.Pop();
        }
    }

    public void AddNewState(PlayerState newState)
    {
        currentState.Push(newState);
        currentState.Peek().OnStateEnter(this);
    }

    public void ClearStates()
    {
        //Clear all states, except the doNotRemove at the bottom
        while (currentState.Count > 0 && !currentState.Peek().doNotRemove)
        {
            //Call exit, or just close it all down?
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