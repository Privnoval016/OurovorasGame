using System;
using System.Collections;

[Serializable]
public class NoneEnemyAttackStrategy : IEnemyAttackStrategy
{
    protected override bool AutoPlayAttackClips => true;
}