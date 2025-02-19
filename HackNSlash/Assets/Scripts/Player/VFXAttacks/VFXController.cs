using System;
using ExtensionUtils;
using UnityEngine;
using UnityEngine.VFX;

public class VFXController : MonoBehaviour
{
    [Header("Components")] 
    public Collider col;
    public VisualEffect[] vfxs;
    public MeshRenderer meshRenderer;
    
    [Header("Settings")]
    public Vector3 properScale = Vector3.one;
    private Vector3 initialScale;

    public bool activeHitbox = true;
    
    [HideInInspector] public Attack attack;
    [HideInInspector] public PlayerController player;

    private void Awake()
    {
        initialScale = transform.localScale;
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (col == null) col = GetComponent<Collider>();
    }
    
    public void InitializeVFX(PlayerController pc, TransformInfo start, Attack a, VisualEffect[] vfx, bool canCollide)
    {
        player = pc;
        attack = a;
        vfxs = vfx;
        
        transform.position = start.Position;
        transform.rotation = start.Rotation;
        
        UpdateScale(properScale);
        
        activeHitbox = canCollide;
        
        meshRenderer.enabled = vfxs.Length == 0;
    }
    
    public void UpdateScale(Vector3 scale)
    {
        properScale = scale;
        transform.localScale = initialScale.ScaledBy(properScale);
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Hit");
        if (!activeHitbox) return;
        
        if (player == null || attack == null) return;
        
        if (other.TryGetComponent(out IDamageable enemy))
        {
            enemy.OnHit(player, attack);
        }
    }
}
