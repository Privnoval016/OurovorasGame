using System;
using Extensions.Patterns;
using Extensions.Utils;
using Extensions.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class OverworldMenuUI : Singleton<OverworldMenuUI>
{
    public EventSystem eventSystem;
    
    [Header("Tabs")]
    public TabGroup tabGroup;
    
    #region MonoBehaviour Callbacks
    
    protected override void Awake()
    {
        base.Awake();
    }

    private void Start()
    {
        CloseMenu();
    }

    private void Update()
    {

    }

    #endregion
    
    #region Menu Methods

    public void OpenMenu()
    {
        gameObject.SetActive(true);
        if (tabGroup == null) return;
        tabGroup.tabActive = true;
        
    }
    
    public void CloseMenu()
    {
        gameObject.SetActive(false);
        if (tabGroup == null) return;
        tabGroup.tabActive = false;
    }
    
    #endregion

    #region Event Callbacks

    public void SwapAccessory(int index)
    {
        print($"Swapping accessory at index {index}");
    }
    
    public void SwapPassive(int index)
    {
        print($"Swapping passive at index {index}");
    }
    
    public void SwapAttack(int index)
    {
        print($"Swapping attack at index {index}");
    }
    
    public void SwapReaction(int index)
    {
        print($"Swapping reaction at index {index}");
    }

    #endregion
}
