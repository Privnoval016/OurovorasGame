using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttacking : State
{
    private PlayerController pc;
    private Attack attack;
    
    #region State Methods
    
    public PlayerAttacking(Attack attack)
    {
        this.attack = attack;
    }
    
    public override void OnEnter()
    {
        pc = (PlayerController) sc.parent;
        LaunchAttack();

        InputManager.Instance.lightAttack.performed += OnLightAttackAction;
        InputManager.Instance.heavyAttack.performed += OnHeavyAttackAction;
    }

    public override void OnUpdate()
    {
        
    }

    public override void OnFixedUpdate()
    {
    }

    public override void OnExit()
    {
        InputManager.Instance.lightAttack.performed -= OnLightAttackAction;
        InputManager.Instance.heavyAttack.performed -= OnHeavyAttackAction;
    }
    
    #endregion

    #region Input Callbacks

    private void OnLightAttackAction(InputAction.CallbackContext context)
    {
        Debug.Log("hi");
    }
    
    private void OnHeavyAttackAction(InputAction.CallbackContext context)
    {
        Debug.Log("hi");
    }

    #endregion

    #region Attack Methods

    private void LaunchAttack()
    {
        
    }

    #endregion
}
