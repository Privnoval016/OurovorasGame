public class EnemyInitialState : EnemyState
{
    public override void OnEnter()
    {
        base.OnEnter();
        doNotRemove = true;
        
        EntityManager.Instance.MarkEnemyUnaware(esm.ts);
    }

    public override void OnResume()
    {
        base.OnResume();
        esm.SetIsAttacking(false);
    }
}