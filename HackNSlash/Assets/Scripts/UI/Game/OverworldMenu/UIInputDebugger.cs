using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Debugging tool to diagnose EventSystem and Input System issues.
/// Add to EventSystem GameObject and check logs to see what's wrong.
/// </summary>
[RequireComponent(typeof(EventSystem))]
public class UIInputDebugger : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool logEveryFrame = false;
    [SerializeField] private KeyCode debugKey = KeyCode.F1;
    
    private EventSystem eventSystem;
    private InputSystemUIInputModule uiInputModule;
    
    private void Awake()
    {
        eventSystem = GetComponent<EventSystem>();
        uiInputModule = GetComponent<InputSystemUIInputModule>();
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(debugKey))
        {
            LogFullDiagnostics();
        }
        
        if (logEveryFrame)
        {
            LogQuickStatus();
        }
    }
    
    [ContextMenu("Run Full Diagnostics")]
    public void LogFullDiagnostics()
    {
        Debug.Log("========== UI INPUT DIAGNOSTICS ==========");
        
        // EventSystem
        Debug.Log($"EventSystem: {(eventSystem != null ? "✅ Found" : "❌ MISSING")}");
        if (eventSystem != null)
        {
            Debug.Log($"  - Current Selected: {(eventSystem.currentSelectedGameObject != null ? eventSystem.currentSelectedGameObject.name : "❌ NONE (This is the problem!)")}");
            Debug.Log($"  - First Selected: {(eventSystem.firstSelectedGameObject != null ? eventSystem.firstSelectedGameObject.name : "None")}");
        }
        
        // InputSystemUIInputModule
        Debug.Log($"InputSystemUIInputModule: {(uiInputModule != null ? "✅ Found" : "❌ MISSING (Add UIInputConfigurator!)")}");
        if (uiInputModule != null)
        {
            Debug.Log($"  - Move Action: {(uiInputModule.move != null ? $"✅ {uiInputModule.move.action.name}" : "❌ NOT ASSIGNED")}");
            Debug.Log($"  - Submit Action: {(uiInputModule.submit != null ? $"✅ {uiInputModule.submit.action.name}" : "❌ NOT ASSIGNED")}");
            Debug.Log($"  - Cancel Action: {(uiInputModule.cancel != null ? $"✅ {uiInputModule.cancel.action.name}" : "❌ NOT ASSIGNED")}");
            
            if (uiInputModule.move != null)
                Debug.Log($"  - Move Enabled: {(uiInputModule.move.action.enabled ? "✅ Yes" : "❌ NO (Action map disabled!)")}");
            if (uiInputModule.submit != null)
                Debug.Log($"  - Submit Enabled: {(uiInputModule.submit.action.enabled ? "✅ Yes" : "❌ NO")}");
            if (uiInputModule.cancel != null)
                Debug.Log($"  - Cancel Enabled: {(uiInputModule.cancel.action.enabled ? "✅ Yes" : "❌ NO")}");
        }
        
        // Input System
        Debug.Log($"Gamepad Connected: {(Gamepad.current != null ? $"✅ {Gamepad.current.name}" : "❌ NO GAMEPAD")}");
        
        if (Gamepad.current != null)
        {
            Debug.Log($"  - D-Pad: {Gamepad.current.dpad.ReadValue()}");
            Debug.Log($"  - Left Stick: {Gamepad.current.leftStick.ReadValue()}");
            Debug.Log($"  - A Button (South): {(Gamepad.current.buttonSouth.isPressed ? "PRESSED" : "Not pressed")}");
            Debug.Log($"  - B Button (East): {(Gamepad.current.buttonEast.isPressed ? "PRESSED" : "Not pressed")}");
        }
        
        // InputManager
        var inputManager = InputManager.Instance;
        Debug.Log($"InputManager: {(inputManager != null ? "✅ Found" : "❌ MISSING")}");
        
        Debug.Log("========================================");
    }
    
    private void LogQuickStatus()
    {
        if (eventSystem != null && eventSystem.currentSelectedGameObject == null)
        {
            Debug.LogWarning("⚠️ EventSystem has NO selected GameObject! Controller navigation will not work.");
        }
    }
    
    private int GetSubscriberCount(System.Delegate del)
    {
        return del != null ? del.GetInvocationList().Length : 0;
    }
}

