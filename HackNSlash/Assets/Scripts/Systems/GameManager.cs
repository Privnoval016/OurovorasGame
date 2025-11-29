using System;
using System.Collections;
using System.Collections.Generic;
using Extensions.Patterns;
using Extensions.Utils;
using MEC;
using UnityEngine;
using UnityEngine.InputSystem;


public enum GameState
{
    PlayerControl,
    Cutscene,
    Menu,
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
    
    public static GameState CurrentGameState;
    private GameState previousGameState;
    
    
    [Header("Global Parameters")]
    public float globalGravity = -9.81f;
    
    public LayerMask groundLayer;
    public LayerMask enemyLayer;

    #region MonoBehavior Callbacks
    
    protected override void Awake()
    {
        base.Awake();
        
        CurrentGameState = GameState.PlayerControl;
        
        InputManager.Instance.onPause += OnPauseAction;
    }

    private void Start()
    {
        Services.OverworldMenuUI.gameObject.SetActive(true);
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
        Services.OverworldMenuUI.CloseMenu();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Services.CombatSystem.ApplyPausedTimescale(false);
    }
    
    private void SetCutsceneState()
    {
        Services.OverworldMenuUI.CloseMenu();
        Services.CombatSystem.ApplyPausedTimescale(true);
    }
    
    private void SetMenuState()
    {
        Services.OverworldMenuUI.OpenMenu();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Services.CombatSystem.ApplyPausedTimescale(true);
    }
    
    #endregion
}