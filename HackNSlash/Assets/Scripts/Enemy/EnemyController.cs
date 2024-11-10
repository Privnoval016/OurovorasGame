using UnityEngine;

public class EnemyController : MonoBehaviour, ITargetable, IDamageable
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void OnHit(Attack a)
    {

    }
    
    public bool TookDamageThisAction { get; set; }
    
}
