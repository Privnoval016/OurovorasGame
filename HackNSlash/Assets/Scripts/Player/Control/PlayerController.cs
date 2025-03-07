using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(StateController))]
public class PlayerController : MonoBehaviour
{
    #region State Machine
    [HideInInspector] public StateController sc;
    #endregion
    
    #region Components
    
    [HideInInspector] public Rigidbody rb;
    [FormerlySerializedAs("col")] public CapsuleCollider mainCol;
    [HideInInspector] public CapsuleCollider[] allCols;
    
    [HideInInspector] public CameraController cam;
    
    [HideInInspector] public WeaponController wc;
    [HideInInspector] public PlayerAnimator pac;
    [HideInInspector] public PlayerStateMachine psm;

    public PlayerAnimListener model;
    
    #endregion
    
    public ElementEffect CurrentElementEffect = ElementEffect.None;
    public ElementData CurrentElementData => GameManager.ElementMap[CurrentElementEffect]();
    
    
    #region MonoBehaviour Callbacks

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        wc = GetComponent<WeaponController>();
        pac = GetComponent<PlayerAnimator>();
        psm = GetComponent<PlayerStateMachine>();

        allCols = GetComponents<CapsuleCollider>();
        
        if (mainCol == null)
        {
            foreach (CapsuleCollider c in allCols)
            {
                if (c.material != null) continue;
                
                mainCol = c;
                break;
            }
        }

        sc = GetComponent<StateController>();
        sc.parent = this;
        
        if (Camera.main != null)
            Camera.main.TryGetComponent(out cam);
        
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        model.pc = this;
    }
    
    
    #endregion
    
    public void IgnoreCollision(Collider col, bool ignore)
    {
        Debug.Log("NumCols: " + allCols.Length);
        foreach (CapsuleCollider c in allCols)
        {
            Physics.IgnoreCollision(c, col, ignore);
        }
    }
    
}