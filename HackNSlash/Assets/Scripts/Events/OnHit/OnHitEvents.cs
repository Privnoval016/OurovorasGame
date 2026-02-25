using UnityEngine;

public class OnHitEvents : MonoBehaviour
{
    [HideInInspector] public PlayerController pc;

    private void Awake()
    {
        pc = GetComponent<PlayerController>();
    }
    
    public void ActivateHitAction(int index, PhysicsEnemy ec, Attack a, Transform t)
    {
        if (index < 0 || a.hitInfo.hitActionInfos == null || index >= a.hitInfo.hitActionInfos.Length)
        {
            return;
        }

        var action = a.hitInfo.hitActionInfos[index].hitAction;

        action?.Execute(this, ec, a, t);
    }
}
