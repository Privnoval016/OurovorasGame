using System;
using System.Collections.Generic;
using System.Linq;
using ExtensionUtils;
using JetBrains.Annotations;
using UnityEngine;
using MEC;
using UnityEngine.VFX;

public class VFXController : MonoBehaviour
{

    [HideInInspector] public Collider col;
    
    
    [HideInInspector] public MeshRenderer meshRenderer;
    
    [Header("Settings")]
    public Vector3 properScale = Vector3.one;
    public Vector3 initialScale;

    
    [HideInInspector] public Attack attack;
    [HideInInspector] public PlayerController player;
    [HideInInspector] public VFXInfo vfxInfo;
    
    [HideInInspector] public Dictionary<VisualEffect, int> vfxs;
    [HideInInspector] public int vfxIndex;
    
    [HideInInspector] public float timeAlive;
    
    private Dictionary<VisualEffect, float> vfxDelays = new();
    
    public bool activeHitbox = true;
    private float elapsedTime;
    public bool vfxEnabled;

    private void Awake()
    {
        initialScale = transform.localScale;
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (col == null) col = GetComponent<Collider>();
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
            effect.gameObject.SetActive(true);
            effect.Play();
            vfxDelays.Remove(effect);
        });
        
        
    }
    
    public void InitializeVFX(PlayerController pc, TransformInfo start, Attack a, VFXInfo v, Dictionary<VisualEffect, int> vfx, int index, bool canCollide)
    {
        player = pc;
        attack = a;
        vfxs = vfx;
        vfxInfo = v;
        vfxIndex = index;
        
        AddVFXDelays();
        
        transform.position = start.Position;
        transform.rotation = start.Rotation;
        UpdateScale(start.Scale);
        
        activeHitbox = canCollide;

        timeAlive = v.duration;
        
        meshRenderer.enabled = vfxs.Count == 0;

        UpdateVFXColorByElement(a.element);
    }
    
    private void AddVFXDelays()
    {
        foreach (VisualEffect vfx in vfxs.Keys)
        {
            vfxDelays.Add(vfx, vfxInfo.vfxAttack.vfxDatas[vfxs[vfx]].delayScale);
        }
    }
    
    public void EnableVFX()
    {
        vfxEnabled = true;
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
        child.transform.localScale = scale.ScaledBy(properScale).DividedBy(initialScale);
    }

    public void UpdateVFXFloat(string name, float value)
    {
        foreach (VisualEffect vfx in vfxs.Keys)
        {
            vfx.SafeSetFloat(name, value);
        }
    }
    
    public void UpdateVFXColorByElement(ElementEffect elementType)
    {
        Color brightColor = GameManager.ElementMap[elementType]().vfxBrightColor;
        Color darkColor = GameManager.ElementMap[elementType]().vfxDarkColor;

        foreach (VisualEffect vfx in vfxs.Keys)
        {
            vfx.SafeSetVector4("BrightColor", brightColor);
            vfx.SafeSetVector4("DarkColor", darkColor);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Hit");
        if (!activeHitbox || !vfxEnabled) return;
        
        if (player == null || attack == null) return;
        
        if (other.TryGetComponent(out IDamageable enemy) && !enemy.tookDamageThisAction)
        {
            enemy.OnHit(player, attack);
        }
    }

    // private void OnTriggerStay(Collider other)
    // {
    //     Debug.Log("Hit");
    //     if (!activeHitbox || !vfxEnabled) return;
    //     
    //     if (player == null || attack == null) return;
    //     
    //     Debug.Log("Hit Ready");
    //     
    //     if (other.TryGetComponent(out IDamageable enemy) && !enemy.tookDamageThisAction)
    //     {
    //         enemy.OnHit(player, attack);
    //     }
    // }
}