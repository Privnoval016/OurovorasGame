using System;
using ExtensionUtils;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public class VFXController : MonoBehaviour
{
    [Header("Components")] 
    public Collider col;
    public VisualEffect[] vfxs;
    public MeshRenderer meshRenderer;
    
    [Header("Settings")]
    public Vector3 properScale = Vector3.one;
    public Vector3 initialScale;

    public bool activeHitbox = true;
    
    [HideInInspector] public Attack attack;
    [HideInInspector] public PlayerController player;
    [HideInInspector] public VFXInfo vfxInfo;
    [HideInInspector] public int vfxIndex;
    [HideInInspector] public float timeAlive;
    private float elapsedTime;

    private void Awake()
    {
        initialScale = transform.localScale;
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (col == null) col = GetComponent<Collider>();
    }
    
    private void Update()
    {
        elapsedTime += Time.deltaTime;
        if (elapsedTime >= timeAlive)
        {
            Destroy(gameObject);
        }
    }
    
    public void InitializeVFX(PlayerController pc, TransformInfo start, Attack a, VFXInfo v, VisualEffect[] vfx, int index, bool canCollide)
    {
        player = pc;
        attack = a;
        vfxs = vfx;
        vfxInfo = v;
        vfxIndex = index;
        
        transform.position = start.Position;
        transform.rotation = start.Rotation;
        UpdateScale(start.Scale);
        
        activeHitbox = canCollide;

        timeAlive = v.duration;
        
        meshRenderer.enabled = vfxs.Length == 0;

        UpdateVFXColorByElement(a.element);
    }
    
    public void EnableVFX()
    {
        foreach (VisualEffect vfx in vfxs)
        {
            vfx.gameObject.SetActive(true);
            vfx.Play();
        }
    }
    
    public void UpdateScale(Vector3 scale)
    {
        properScale = scale;
        transform.localScale = initialScale.ScaledBy(properScale);
    }
    
    public void ChangeParent(Transform parent)
    {
        transform.SetParent(parent);
        UpdateScale(properScale);
    }
    
    public void SetChildScale(GameObject child, Vector3 scale)
    {
        Vector3 targetLossyScale = properScale.ScaledBy(scale);
        
        child.transform.localScale = targetLossyScale.DividedBy(transform.lossyScale);
    }

    public void UpdateVFXFloat(string name, float value)
    {
        foreach (VisualEffect vfx in vfxs)
        {
            vfx.SafeSetFloat(name, value);
        }
    }
    
    public void UpdateVFXColorByElement(ElementEffect elementType)
    {
        Color brightColor = GameManager.ElementMap[elementType]().vfxBrightColor;
        Color darkColor = GameManager.ElementMap[elementType]().vfxDarkColor;

        foreach (VisualEffect vfx in vfxs)
        {
            vfx.SafeSetVector4("BrightColor", brightColor);
            vfx.SafeSetVector4("DarkColor", darkColor);
        }
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
