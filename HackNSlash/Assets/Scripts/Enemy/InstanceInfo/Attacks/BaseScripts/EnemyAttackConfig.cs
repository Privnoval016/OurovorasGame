using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAttackConfig", menuName = "Enemy/EnemyAttackConfig", order = 1)]
public class EnemyAttackConfig : ScriptableObject
{
    public EnemyAttackInfo[] infos;
}
