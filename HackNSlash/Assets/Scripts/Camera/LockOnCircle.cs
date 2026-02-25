using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.Serialization;

public class LockOnCircle : MonoBehaviour
{
    [Header("References")]
    public CameraController cameraController;
    public bool alignUp = true; // Keep quad upright
    
    [Header("Pulse Settings")]
    public float screenFraction = 0.1f;  
    public float pulseSpeed = 2f;
    public float pulseMagnitude = 0.05f;
    
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    void LateUpdate()
    {
        MoveToTarget();
        FaceCamera();
        Pulse();
    }
    
    private void MoveToTarget()
    {
        if (cameraController.IsLockedOn)
        {
            meshRenderer.enabled = true;
            transform.position = cameraController.TargetedEnemy.LockOnAimPosition();
        }
        else
        {
            meshRenderer.enabled = false;
        }
    }
    
    private void FaceCamera()
    {
        Vector3 direction = cameraController.transform.position - transform.position;

        if (alignUp)
        {
            // Keep quad upright
            direction.y = 0;
        }

        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
    
    private void Pulse()
    {
        float distance = Vector3.Distance(transform.position, cameraController.transform.position);
        float fovRad = cameraController.camBrain.OutputCamera.fieldOfView * Mathf.Deg2Rad;
        float scale = 2f * distance * Mathf.Tan(fovRad / 2f) * screenFraction;
        
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseMagnitude;
        transform.localScale = Vector3.one * scale * pulse;
    }
}