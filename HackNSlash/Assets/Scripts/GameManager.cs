using System;
using System.Collections;
using System.Collections.Generic;
using MEC;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public GameObject player;
    
    public float globalGravity = -9.81f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        player = GameObject.FindWithTag("Player");
    }
    
    public static IEnumerator<float> TraverseDistanceInTime(Rigidbody rb, Vector3 direction, float distance, float time, Func<bool> condition = null)
    {
        direction.Normalize();
        Vector3 initialPosition = rb.position;
        float startTime = Time.time;



        float impulse = rb.mass * (distance / time - rb.linearVelocity.magnitude);
        rb.AddForce(direction * impulse, ForceMode.Impulse);
        
        yield return Timing.WaitUntilTrue(() => Vector3.Distance(rb.position, initialPosition) >= distance || Time.time - startTime >= time || (condition != null && condition()));
        
        //Debug.Log("Intended distance: " + distance + " Actual distance: " + Vector3.Distance(rb.position, initialPosition));
        //Debug.Log("Intended time: " + time + " Actual time: " + (Time.time - startTime));
        
        rb.linearVelocity = Vector3.zero;
    }

    public static IEnumerator<float> TraverseWithVelocity(Rigidbody rb, Vector3 direction, float magnitude,
        Func<bool> loopCondition)
    {
        direction.Normalize();
        
        while (loopCondition())
        {
            rb.linearVelocity = direction * magnitude;
            yield return Timing.WaitForOneFrame;
        }
    }
}
