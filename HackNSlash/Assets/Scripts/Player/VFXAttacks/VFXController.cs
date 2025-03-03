using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionUtils;
using JetBrains.Annotations;
using UnityEngine;
using MEC;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public class VFXController : MonoBehaviour
{

    public VFXHitbox[] hitboxes;
    
    
    [HideInInspector] public MeshRenderer meshRenderer;
    [HideInInspector] public Rigidbody rb;
    
    [Header("Settings")]
    
    [HideInInspector] public Attack attack;
    [HideInInspector] public PlayerController player;
    [HideInInspector] public VFXSpawnInfo vfxSpawnInfo;
    
    [HideInInspector] public VFXActivator[] vas;
    [HideInInspector] public int vfxIndex;
    
    [HideInInspector] public float timeAlive;
    
    private Dictionary<VFXActivator, float> vfxDelays = new();
    
    public bool activeHitbox = true;
    private float elapsedTime;
    public bool vfxEnabled;

    private void Awake()
    {
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (rb == null) rb = GetComponent<Rigidbody>();
        
        if (hitboxes.Length == 0)
        {
            hitboxes = GetComponentsInChildren<VFXHitbox>();
        }
        
        foreach (VFXHitbox hitbox in hitboxes)
        {
            hitbox.vfxController = this;
        }
    }
    
    private void Update()
    {
        if (!vfxEnabled) return;
        
        elapsedTime += Time.deltaTime;
        if (elapsedTime >= timeAlive)
        {
            Destroy(gameObject);
        }

        vfxDelays.Keys.Where(effect => elapsedTime >= vfxDelays[effect] * timeAlive).ToList().ForEach(effect =>
        {
            effect.PlayVFX();
            vfxDelays.Remove(effect);
        });
        
        
    }
    
    public void InitializeVFX(PlayerController pc, TransformInfo start, Attack a, VFXSpawnInfo v, VFXActivator[] vfx, int index, bool canCollide)
    {
        player = pc;
        attack = a;
        vas = vfx;
        vfxSpawnInfo = v;
        vfxIndex = index;
        
        AddVFXDelays();
        
        transform.position = start.Position;
        transform.rotation = start.Rotation;
        transform.localScale = start.Scale;
        
        activeHitbox = canCollide;

        timeAlive = v.duration;

        foreach (VFXHitbox hitbox in hitboxes)
        {
            hitbox.meshRenderer.enabled = vas.Length == 0;
        }
        if (meshRenderer != null) meshRenderer.enabled = vas.Length == 0;

        UpdateVFXColorByElement(a.element);
    }
    
    private void AddVFXDelays()
    {
        for (int i = 0; i < vas.Length; i++)
        {
            VFXActivator va = vas[i];
            vfxDelays.Add(va, vfxSpawnInfo.vfxAttack.vfxDatas[i].delayScale);
        }
    }
    
    public void EnableVFX()
    {
        vfxEnabled = true;
    }
    
    public void ChangeParent(Transform parent, Vector3 localScale = default)
    {
        if (localScale == default) localScale = Vector3.one;
        transform.SetParent(parent);
        transform.localScale = localScale;
        
    }

    public void UpdateVFXFloat(string name, float value)
    {
        foreach (VFXActivator va in vas)
        {
            va.SetVFXFloat(name, value);
        }
    }
    
    public void UpdateVFXColorByElement(ElementEffect elementType)
    {
        Color brightColor = GameManager.ElementMap[elementType]().vfxBrightColor;
        Color darkColor = GameManager.ElementMap[elementType]().vfxDarkColor;
        Color pureColor = GameManager.ElementMap[elementType]().vfxPureColor;

        foreach (VFXActivator va in vas)
        {
            va.SetVFXVector4("PureColor", pureColor);
            va.SetVFXVector4("BrightColor", brightColor);
            va.SetVFXVector4("DarkColor", darkColor);
        }
    }

    public void HitboxTriggerEnter(Collider other)
    {
        Debug.Log("Hit");
        if (!activeHitbox || !vfxEnabled) return;
        
        if (player == null || attack == null) return;
        
        if (other.TryGetComponent(out IDamageable enemy) && !enemy.tookDamageThisAction)
        {
            enemy.OnHit(player, attack, vfxSpawnInfo.hitIndex);
        }
    }

    public void HitboxTriggerStay(Collider other)
    {
        
    }
}