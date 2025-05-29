using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Events/OnSpiritParameters")]
public class OnSpiritParameters : ScriptableObject
{
    [Header("Ranged Attack")]
    public float spiritProjectileHoldTime = 0.5f;

    [Header("Spawn VFX")] 
    public float vfxCooldownTime = 0.1f;
}
