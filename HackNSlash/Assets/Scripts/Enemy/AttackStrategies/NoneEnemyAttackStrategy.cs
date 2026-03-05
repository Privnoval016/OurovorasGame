using System;

[Serializable]
public class NoneEnemyAttackStrategy : IEnemyAttackStrategy
{
    protected override bool AutoPlayAttackClips => true;
}