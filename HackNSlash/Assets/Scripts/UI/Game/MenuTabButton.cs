using System;
using UnityEngine;
using Extensions.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuTabButton : Extensions.UI.TabButton
{
    public Button defaultButton;
    public GameObject selectedButtonObject;
    
    private EventSystem eventSystem;
    
    
    #region MonoBehaviour Callbacks

    private void Start()
    {
        eventSystem = OverworldMenuUI.Instance.eventSystem;
    }

    private void Update()
    {
        if (!isSelected) return;

        selectedButtonObject = eventSystem.currentSelectedGameObject;
    }

    #endregion
    
    #region Inherited Methods
    
    protected override void OnTabSelect()
    {
        base.OnTabSelect();
        if (defaultButton != null)
           eventSystem.SetSelectedGameObject(defaultButton.gameObject);
    }
    
    protected override void OnTabDeselect()
    {
        base.OnTabDeselect();
    }
    
    #endregion
}
