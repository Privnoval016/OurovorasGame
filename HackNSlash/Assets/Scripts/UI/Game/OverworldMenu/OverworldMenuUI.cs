using Extensions.Patterns;
using Extensions.UI;
using UnityEngine;
using UnityEngine.EventSystems;


public class OverworldMenuUI : MonoBehaviour
{
    public EventSystem eventSystem;
    public PlayerInventory playerInventory;

    [Header("Tabs")]
    public TabGroup tabGroup;

    #region MonoBehaviour Callbacks

    private void Awake()
    {
        playerInventory = Services.PlayerController.pi;
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