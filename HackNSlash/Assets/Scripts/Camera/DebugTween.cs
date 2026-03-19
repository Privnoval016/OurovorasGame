using System;
using System.Collections.Generic;
using Extensions.Utils;
using MEC;
using UnityEngine;

public class DebugTween : MonoBehaviour
{
    [Serializable]
    public struct TweenInfo
    {
        public bool useLocalSpace;
        public Vector3 position;
        public Vector3 rotation;
        public float duration;
        public AnimationCurve curve;
    }
    
    [Header("Settings")]
    [SerializeField] private List<TweenInfo> tweenInfos;
    [SerializeField] private int currentTweenIndex = 0;
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            CycleToNext();
        }
    }
    
    private void CycleToNext()
    {
        this.RunSegmentCoroutine(Cycle());
    }

    private IEnumerator<float> Cycle()
    {
        float elapsed = 0f;
        TweenInfo info = tweenInfos[currentTweenIndex];
        Vector3 startPos = info.useLocalSpace ? transform.localPosition : transform.position;
        Vector3 startRot = info.useLocalSpace ? transform.localEulerAngles : transform.eulerAngles;
        Vector3 targetPos = info.position;
        Vector3 targetRot = info.rotation;
        
        while (elapsed < info.duration)
        {
            float t = info.curve.Evaluate(elapsed / info.duration);
            Vector3 newPos = Vector3.Lerp(startPos, targetPos, t);
            Quaternion newRot = Quaternion.Slerp(Quaternion.Euler(startRot), Quaternion.Euler(targetRot), t);
            
            if (info.useLocalSpace)
            {
                transform.localPosition = newPos;
                transform.localRotation = newRot;
            }
            else
            {
                transform.position = newPos;
                transform.rotation = newRot;
            }
            
            elapsed += Time.deltaTime;
            yield return Timing.WaitForOneFrame;
        }
        
        // Ensure final position and rotation are set
        if (info.useLocalSpace)
        {
            transform.localPosition = targetPos;
            transform.localRotation = Quaternion.Euler(targetRot);
        }
        else
        {
            transform.position = targetPos;
            transform.rotation = Quaternion.Euler(targetRot);
        }
        
        currentTweenIndex = (currentTweenIndex + 1) % tweenInfos.Count;
    }
}