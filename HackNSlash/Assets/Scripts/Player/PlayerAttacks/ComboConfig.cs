using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Player/ComboConfig")]
public class ComboConfig : ScriptableObject
{
    [Header("General")]
    public bool isEnabled = true;
    public AttackTypes attackType;
    
    
    [Header("Data")]
    public ComboAction[] comboActions;

    private void OnValidate()
    {
        foreach (ComboAction comboAction in comboActions)
        {
            if (comboAction.actionType == ComboActionType.Pause)
            {
                comboAction.attack = null;
            }
        }
    }
}

[Serializable]
public class ComboAction
{
    public ComboActionType actionType;
    public Attack attack;
    public float time; // cooldown if press, hold time if hold, pause time if pause
}

public enum ComboActionType
{
    Press,
    Hold,
    Pause
}
