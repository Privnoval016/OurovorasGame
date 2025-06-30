using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAnimData", menuName = "Enemy/EnemyAnimData", order = 1)]
public class EnemyAnimData : ScriptableObject
{
    public AnimationClip idle;
    public AnimationClip walk;
}
