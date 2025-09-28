using System;
using System.Collections.Generic;
using System.Linq;
using Extensions.Utils;
using UnityEngine;

public class VFXController : KinematicBehaviour, IContactDetector
{
    [Header("Components")]

    public VFXHitbox[] hitboxes;
    
    [HideInInspector] public MeshRenderer meshRenderer;
    
    public VFXHitDetector HitDetector;
    
    [Header("Settings")]
    
    [HideInInspector] public VFXSpawnInfo vfxSpawnInfo;
    
    public ElementEffect elementType;
    
    [HideInInspector] public VFXActivator[] vas;
    
    [HideInInspector] public float timeActive;
    [HideInInspector] public float timeVFXEnabled;
    
    private Dictionary<VFXActivator, float> vfxDelays = new();
    
    public bool activeHitbox = true;
    public float hitboxEnableDelay = 0f;
    private bool delayedHitboxEnabled = false;
    private float elapsedTime;
    public bool vfxEnabled;

    private void Awake()
    {
        SetKinematicAttributes();
        
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (rb == null) rb = GetComponent<Rigidbody>();
        
        if (hitboxes.Length == 0)
        {
            hitboxes = GetComponentsInChildren<VFXHitbox>();
        }
        
        foreach (VFXHitbox hitbox in hitboxes)
        {
            if (hitboxEnableDelay != 0f)
            {
                hitbox.DisableCollider();
            }
            else
            {
                hitbox.EnableCollider();
            }
        }
    }
    
    private void Update()
    {
        UpdateKinematicAttributes();
        
        if (!vfxEnabled) return;
        
        elapsedTime += Time.deltaTime;
        
        if (hitboxEnableDelay > 0f && !delayedHitboxEnabled)
        {
            if (elapsedTime >= hitboxEnableDelay)
            {
                delayedHitboxEnabled = true;
                foreach (VFXHitbox hitbox in hitboxes)
                {
                    hitbox.EnableCollider();
                }
            }
        }
        
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
    
    public bool IsPlayerVFX()
    {
        return HitDetector is PlayerVFXHitDetector;
    }
    
    public bool IsEnemyVFX()
    {
        return HitDetector is EnemyVFXHitDetector;
    }
    
    // Disable hitboxes and notify listeners that the VFX has been disabled
    public void DisableVFX()
    {
        activeHitbox = false;

        foreach (var hitbox in hitboxes)
        {
            hitbox.DisableCollider();
        }
        
        foreach (var va in vas)
        {
            va.SetAllVFXEvents(va.vfxEventEnd);
        }
        
        OnVFXEvents.Instance.OnVFXDisabled(this);
    }
    
    // Destroy the actual GameObject and end all visual effects
    public void DestroyVFX()
    {
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }
    
    public void InitializeVFX(ElementEffect element, TransformInfo start, VFXSpawnInfo v, VFXActivator[] vfx, bool canCollide)
    {
        elementType = element;
        vas = vfx;
        vfxSpawnInfo = v;
        
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
    
    public void AddHitDetector(VFXHitDetector hitDetector)
    {
        HitDetector = hitDetector;
        HitDetector.vfx = this;
        
        foreach (VFXHitbox hitbox in hitboxes)
        {
            hitbox.HitDetector = HitDetector;
        }
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
    
    public Vector3 GetClosestPointOnCollider(Collider col)
    {
        if (col == null) return Vector3.zero;

        Vector3 closestPoint = Vector3.zero;
        float minDistance = float.MaxValue;

        foreach (VFXHitbox h in hitboxes)
        {
            Transform t = h.transform;
            Vector3 point = col.ClosestPoint(t.position);
            float distance = Vector3.Distance(t.position, point);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestPoint = point;
            }
        }

        return closestPoint;
    }
}