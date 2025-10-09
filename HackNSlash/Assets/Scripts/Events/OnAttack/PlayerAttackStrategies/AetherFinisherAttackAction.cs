using System;
using UnityEngine;

[Serializable]
public class AetherFinisherAttackAction : IAttackAction
{
    public override void Execute(OnAttackEvents onAttackEvents, PlayerAttack attack)
    {
        base.Execute(onAttackEvents, attack);
    }
}
