public class EnemyInitialState : EnemyState
{
    public override void OnEnter()
    {
        base.OnEnter();
        doNotRemove = true;
    }

    public override void OnResume()
    {
        base.OnResume();
        esm.SetIsAttacking(false);
    }
}