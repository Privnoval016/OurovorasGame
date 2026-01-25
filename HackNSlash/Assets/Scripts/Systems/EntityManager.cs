using System.Collections.Generic;
using Extensions.Patterns;
using UnityEngine;

public class EntityManager : LazySingleton<EntityManager>
{
    private readonly HashSet<EnemyController> enemiesInScene = new();
    private readonly HashSet<EnemyController> awareEnemies = new();
    
    #region Registration
    
    public void RegisterEnemy(EnemyController enemy)
    {
        enemiesInScene.Add(enemy);
        UpdateCombatState();
    }
    
    public void UnregisterEnemy(EnemyController enemy)
    {
        enemiesInScene.Remove(enemy);
        UpdateCombatState();
    }
    
    #endregion
    
    #region Enemy Queries
    
    public void MarkEnemyAware(EnemyController enemy)
    {
        awareEnemies.Add(enemy);
        UpdateCombatState();
    }
    
    public void MarkEnemyUnaware(EnemyController enemy)
    {
        awareEnemies.Remove(enemy);
        UpdateCombatState();
    }
    
    #endregion

    private void UpdateCombatState()
    {
        OverworldState state;
        if (awareEnemies.Count > 0)
        {
            state = OverworldState.CombatNormal;
        }
        else
        {
            state = OverworldState.NonCombat;
        }
        
        GameManager.Instance?.ChangeOverworldState(state);
    }
}