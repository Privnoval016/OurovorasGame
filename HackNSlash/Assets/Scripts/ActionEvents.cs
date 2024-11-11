using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ExtensionUtils;
using MEC;





public class ActionEvents : MonoBehaviour
{
    public static ActionEvents Instance { get; private set; }
    
    public OnAttackEvents onAttackEvents;
    public OnHitEvents onHitEvents;
    
    public static Dictionary<OnAttackActions, Action<PlayerController, Attack>> OnAttackActionMap;
    
    public static Dictionary<OnHitActions, Action<PlayerController, Attack>> OnHitActionMap;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        onAttackEvents = GetComponent<OnAttackEvents>();
        onHitEvents = GetComponent<OnHitEvents>();
    }
    
}

