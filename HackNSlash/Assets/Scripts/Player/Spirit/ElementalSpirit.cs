using UnityEngine;
using Extensions.CustomMath;
using Extensions.Utils;

public class ElementalSpirit : KinematicBehaviour
{
    private SODEvaluator _evaluator;

    public Transform[] targets;
    
    public Transform closestTarget => targets.GetClosestTransform(transform.position);

    private void Awake()
    {
        SetKinematicAttributes();
        
        _evaluator = GetComponent<SODEvaluator>(); 
    }

    private void Update()
    {
        UpdateKinematicAttributes();
        
        _evaluator.SetTargetTransform(closestTarget);

        transform.position = _evaluator.output + VerticalBob();
    }
    
    private Vector3 VerticalBob()
    {
        return Mathf.Sin(Time.time * 2) * 0.1f * Vector3.up;
    }
}
