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
    
    public static IEnumerator<float> TraverseDistanceInTime(Rigidbody rb, Vector3 direction, float distance, float time)
    {
        Vector3 initialPosition = rb.position;
        
        float force = 2 * rb.mass * Mathf.Sqrt(2 * distance - 2 * rb.linearVelocity.magnitude * time) / time;
        rb.AddForce(direction * force, ForceMode.Impulse);
        
        float initialTime = Time.time;
        
        
        yield return Timing.WaitUntilTrue(() => Vector3.Distance(rb.position, initialPosition) >= distance);
        
        Debug.Log(Time.time - initialTime);
        
        rb.linearVelocity = Vector3.zero;
    }
}
