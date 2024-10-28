using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/Attack")]
public class Attack : ScriptableObject
{
    [Header("Details")]
    public bool isEnabled;
    
    [Space(5)]
    
    public KeyBind[] keyBinds;
    public Vector2 inputDirection;
    public bool isMidair;
    
    [Space(5)]
    
    public AnimationClip attackClip;
    
    [Header("Stats")]
    
    public float damage;
}
