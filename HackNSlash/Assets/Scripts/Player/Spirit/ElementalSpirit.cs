using UnityEngine;
using Extensions.CustomMath;
using Extensions.Utils;

public class ElementalSpirit : KinematicBehaviour
{
    private SecondOrderDynamics _dynamics;
    
    [Header("Dynamics Parameters")]
    
    public SODInfo sodInfo;

    public KinematicBehaviour target;

    private float f0, z0, r0;

    private void Awake()
    {
        InitializeDynamics();
    }

    private void Update()
    {
        UpdateKinematicAttributes();
        
        if (target == null) return;
        
        if (sodInfo.frequency != f0 || sodInfo.dampingRatio != z0 || sodInfo.responseTime != r0) 
            InitializeDynamics();
        else
        {
            Vector3? dynamicsOutput = _dynamics.Update(Time.deltaTime, target.transform.position, target.discreteVelocity);
            
            if (dynamicsOutput.Vec3NotNull())
            {
                Vector3 output = (Vector3) dynamicsOutput;
                transform.position = output;
            }
        }
        
    }

    private void InitializeDynamics()
    {
        f0 = sodInfo.frequency;
        z0 = sodInfo.dampingRatio;
        r0 = sodInfo.responseTime;
        _dynamics = new SecondOrderDynamics(f0, z0, r0, transform.position);
    }
}
