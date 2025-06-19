using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.Utils;
using JetBrains.Annotations;
using UnityEngine;
using MEC;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public class VFXController : MonoBehaviour
{
    [Header("Components")]

    public VFXHitbox[] hitboxes;
    
    [HideInInspector] public MeshRenderer meshRenderer;
    [HideInInspector] public Rigidbody rb;
    
    [Header("Settings")]
    
    [HideInInspector] public Attack attack;
    [HideInInspector] public PlayerController player;
    [HideInInspector] public VFXSpawnInfo vfxSpawnInfo;
    
    public ElementEffect elementType;
    
    [HideInInspector] public VFXActivator[] vas;
    [HideInInspector] public int vfxIndex;
    
    [HideInInspector] public float timeActive;
    [HideInInspector] public float timeVFXEnabled;
    
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
        if (elapsedTime >= timeActive)
        {
            DisableVFX();
        }
        
        if (elapsedTime >= timeVFXEnabled)
        {
            DestroyVFX();
            return;
        }

        vfxDelays.Keys.Where(effect => elapsedTime >= vfxDelays[effect] * timeActive).ToList().ForEach(effect =>
        {
            effect.PlayVFX();
            vfxDelays.Remove(effect);
        });
        
        
    }
    
    public void DisableVFX()
    {
        activeHitbox = false;
        OnVFXEvents.Instance.OnVFXDisabled(this);
    }
    
    public void DestroyVFX()
    {
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
    
    public void InitializeVFX(PlayerController pc, TransformInfo start, Attack a, VFXSpawnInfo v, VFXActivator[] vfx, int index, bool canCollide)
    {
        elementType = ElementData.GetElementFromAttack(a, pc);
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

        timeActive = v.duration;
        
        timeVFXEnabled = vfx.Length > 0 ? (0.2f + vfx.Max(p => p.MaximumLifetimeScale)) * timeActive : timeActive;

        foreach (VFXHitbox hitbox in hitboxes)
        {
            hitbox.meshRenderer.enabled = vas.Length == 0;
        }
        if (meshRenderer != null) meshRenderer.enabled = vas.Length == 0;

        UpdateVFXColorByElement();
        
        OnVFXEvents.Instance.OnVFXInitialize(this);
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
    
    public void UpdateVFXColorByElement()
    {
        Color brightColor = GameManager.ElementMap[elementType]().vfxBrightColor;
        Color darkColor = GameManager.ElementMap[elementType]().vfxDarkColor;
        Color pureColor = GameManager.ElementMap[elementType]().vfxPureColor;
        Gradient gradient = GameManager.ElementMap[elementType]().vfxGradient;

        foreach (VFXActivator va in vas)
        {
            foreach (string s in va.pureColorOverrides)
            {
                va.SetVFXVector4(s, pureColor);
            }
            
            foreach (string s in va.brightColorOverrides)
            {
                va.SetVFXVector4(s, brightColor);
            }
            
            foreach (string s in va.darkColorOverrides)
            {
                va.SetVFXVector4(s, darkColor);
            }
            
            foreach (string s in va.gradientColorOverrides)
            {
                va.SetVFXGradient(s, gradient);
            }
        }
    }

    public void HitboxTriggerEnter(Collider other)
    {
        Debug.Log("Hit");
        if (!activeHitbox || !vfxEnabled) return;
        
        if (player == null || attack == null) return;
        
        if (other.TryGetComponent(out LockOnTarget enemy) && !enemy.tookDamageThisAction)
        {
            enemy.OnHit(elementType, player, attack, vfxSpawnInfo.onHitActionIndex);
        }
    }

    public void HitboxTriggerStay(Collider other)
    {
        
    }
}