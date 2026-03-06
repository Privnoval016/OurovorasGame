using Extensions.UtilityAI;
using UnityEngine;


[CreateAssetMenu(fileName = "EnemyIdleAction", menuName = "Enemy/AIActions/EnemyIdleAction", order = 0)]
public class EnemyIdleAIAction : EnemyAIActionBase
{
    [Tooltip("How long to hold the idle before releasing back to the brain. 0 = hold until consideration scores 0.")]
    [Min(0f)] public float duration = 0f;

    [Tooltip("If true, face the player each frame while idling.")]
    public bool facePlayer = true;

    private float _elapsed;
    private Transform _target;

    protected override void OnEnemyEnter(EnemyContext context, EnemyStateMachine esm)
    {
        _elapsed = 0f;
        _target = context.GetTarget(EnemyContextKeys.Player);
        esm.Brake();
        var idle = esm.enemyAnimData?.idleClip;
        if (idle != null && idle.Clip != null) esm.ts.ea.PlayEnemyAnimation(idle);
    }

    protected override void OnEnemyUpdate(EnemyContext context, EnemyStateMachine esm)
    {
        _elapsed += Time.deltaTime;

        if (facePlayer)
        {
            if (_target == null) _target = context.GetTarget(EnemyContextKeys.Player);
            if (_target != null) esm.TurnToPosition(_target.position);
        }

        if (duration > 0f && _elapsed >= duration)
            esm.sc.ResumePrevious();
    }

    protected override void OnEnemyExit(EnemyContext context, EnemyStateMachine esm) { }

    public override bool ResetsActionTimer => false;
}