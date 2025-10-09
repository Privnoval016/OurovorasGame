using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Components")]

    [HideInInspector] public PhysicsEnemy pe;

    public PhysicsNavigator nav;

    [HideInInspector] public EnemyAnimator ea;
    
    [HideInInspector] public EnemyStateMachine esm;

    [HideInInspector] public EnemyStats stats;

    [HideInInspector] public OnEnemyEvents onEnemyEvents;

    public EnemyAnimListener animListener;

    public EnemyHitbox[] attackHitboxes;
    

    #region Attack Properties
    
    public bool parryWindowActive = false;

    #endregion

    #region Monobehaviour Callbacks

    private void Awake()
    {
        nav = GetComponent<PhysicsNavigator>();
        ea = GetComponent<EnemyAnimator>();
        pe = GetComponent<PhysicsEnemy>();
        onEnemyEvents = GetComponent<OnEnemyEvents>();
        esm = GetComponent<EnemyStateMachine>();
        stats = GetComponent<EnemyStats>();
    
        animListener.ts = this;
    
        foreach (var hitbox in attackHitboxes)
        {
            hitbox.ts = this;
        }
    }

    #endregion
}

