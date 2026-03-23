using UnityEngine;
using Sirenix.OdinInspector;


/// <summary>
/// Manager for dialogue UI across different contexts (bottom, combat, etc).
/// Allows smooth switching between different dialogue presentations.
/// </summary>
public sealed class DialogueUIManager : MonoBehaviour
{
    [SerializeField] private BottomDialoguePresenter bottomPresenter;
    [SerializeField] private CombatDialoguePresenter combatPresenter;

    public enum UIContext { Bottom, Combat }
    private UIContext _currentContext = UIContext.Bottom;

    public UIContext CurrentContext => _currentContext;

    private void Start()
    {
        if (bottomPresenter == null)
        {
            bottomPresenter = GetComponentInChildren<BottomDialoguePresenter>();
        }

        if (combatPresenter == null)
        {
            combatPresenter = GetComponentInChildren<CombatDialoguePresenter>();
        }
    }

    [Button]
    public void SwitchToBottom()
    {
        SwitchContext(UIContext.Bottom);
    }

    [Button]
    public void SwitchToCombat()
    {
        SwitchContext(UIContext.Combat);
    }

    private void SwitchContext(UIContext newContext)
    {
        if (_currentContext == newContext)
            return;

        // Hide current context
        switch (_currentContext)
        {
            case UIContext.Bottom:
                bottomPresenter?.Hide();
                break;
            case UIContext.Combat:
                combatPresenter?.Hide();
                break;
        }

        _currentContext = newContext;
    }

    public Extensions.Dialogue.Runtime.IDialoguePresenter GetCurrentPresenter()
    {
        return _currentContext switch
        {
            UIContext.Bottom => bottomPresenter,
            UIContext.Combat => combatPresenter,
            _ => bottomPresenter
        };
    }
}


