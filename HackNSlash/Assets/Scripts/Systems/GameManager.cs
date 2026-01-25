using Extensions.EventBus;
using Extensions.Patterns;
using UnityEngine;
using UnityEngine.InputSystem;


/**
 * <summary>
 * Manages overall game state and global parameters.
 * </summary>
 */
public enum GameState
{
    PlayerControl,
    Cutscene,
    Menu,
}

/**
 * <summary>
 * Specific states within the overworld.
 * </summary>
 */
public enum OverworldState
{
    NonCombat,
    CombatNormal,
    CombatElite,
    CombatBoss
}

public enum ElementEffect
{
    None,
    MatchCurrent,
    Fire,
    Ice,
    Lightning,
    Earth,
    Wind,
    Aether
}

public class GameManager : Singleton<GameManager>
{

    [Header("Game State")] 
    
    [field: SerializeField] public GameState CurrentGameState { get; private set; }
    private GameState previousGameState;
    
    [Header("Overworld State")]
    [field: SerializeField] public OverworldState CurrentOverworldState { get; private set; }
    private OverworldState previousOverworldState;
    
    
    [Header("Global Parameters")]
    public float globalGravity = -9.81f;
    
    public LayerMask groundLayer;
    public LayerMask enemyLayer;

    #region MonoBehavior Callbacks
    
    protected override void Awake()
    {
        base.Awake();
        
        CurrentGameState = GameState.PlayerControl;
        
        CurrentOverworldState = OverworldState.NonCombat;
        ChangeOverworldState(OverworldState.NonCombat);
        
        InputManager.Instance.onPause += OnPauseAction;
    }

    private void Start()
    {
        Services.Get<OverworldMenuUI>().gameObject.SetActive(true);
        SetGameState(GameState.Menu);
        SetGameState(GameState.PlayerControl);
    }

    private void Update()
    {

    }

    #endregion


    #region Input Callbacks
    
    private void OnPauseAction(InputAction.CallbackContext context)
    {
        if (CurrentGameState == GameState.Menu)
        {
            SetGameState(previousGameState);
        }
        else
        {
            SetGameState(GameState.Menu);
        }
    }

    #endregion
    
    
    #region GameState Methods
    
    public void SetGameState(GameState state)
    {
        
        switch (state)
        {
            case GameState.PlayerControl:
                SetPlayerControlState();
                break;
            case GameState.Cutscene:
                SetCutsceneState();
                break;
            case GameState.Menu:
                SetMenuState();
                break;
        }

        InputManager.Instance.EnableStateInputs(state);
        
        previousGameState = CurrentGameState;
        CurrentGameState = state;
    }

    private void SetPlayerControlState()
    {
        Services.Get<OverworldMenuUI>().CloseMenu();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Services.Get<CombatSystem>().ApplyPausedTimescale(false);
    }
    
    private void SetCutsceneState()
    {
        Services.Get<OverworldMenuUI>().CloseMenu();
        Services.Get<CombatSystem>().ApplyPausedTimescale(true);
    }
    
    private void SetMenuState()
    {
        Services.Get<OverworldMenuUI>().OpenMenu();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Services.Get<CombatSystem>().ApplyPausedTimescale(true);
    }
    
    #endregion
    
    #region Overworld State Methods
    
    public bool ChangeOverworldState(OverworldState newState)
    {
        if (newState == CurrentOverworldState) return false;
        
        previousOverworldState = CurrentOverworldState;
        CurrentOverworldState = newState;
        
        // Notify listeners of state change
        EventBus<ChangeOverworldStateEvent>.Raise(new ChangeOverworldStateEvent(previousOverworldState, CurrentOverworldState));
        
        // Update music state parameter
        var value = new AudioParamValue(AudioLookupAtlas.Instance.musicStateParam, (int)CurrentOverworldState);
        EventBus<PlayMusicEvent>.Raise(new PlayMusicEvent(new [] { value }));
        
        return true;
    }
    
    #endregion
}

public struct ChangeOverworldStateEvent : IEvent
{
    public readonly OverworldState PreviousState;
    public readonly OverworldState NewState;

    public ChangeOverworldStateEvent(OverworldState previousState, OverworldState newState)
    {
        PreviousState = previousState;
        NewState = newState;
    }
}