using System;
using System.Collections;
using System.Collections.Generic;
using MEC;
using UnityEngine;
using UnityEngine.InputSystem;


public enum GameState
{
    PlayerControl,
    Combat,
    Cutscene,
    PauseMenu,
    UpgradeMenu,
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")] 
    
    public static GameState CurrentGameState;
    private GameState previousGameState;
    
    
    [Header("In Game Instances")]
    public GameObject player;
    
    
    [Header("Global Parameters")]
    public float globalGravity = -9.81f;

    #region MonoBehavior Callbacks
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        
        player = GameObject.FindWithTag("Player");
        
        CurrentGameState = GameState.PlayerControl;
        previousGameState = GameState.PlayerControl;
        
        InputManager.Instance.pause.performed += OnPauseAction;
    }
    
    #endregion


    #region Input Callbacks
    
    private void OnPauseAction(InputAction.CallbackContext context)
    {
        if (CurrentGameState == GameState.PauseMenu)
        {
            SetGameState(previousGameState);
        }
        else
        {
            SetGameState(GameState.PauseMenu);
        }
    }

    #endregion
    
    
    #region GameState Methods
    
    public void SetGameState(GameState state)
    {
        
        switch (state)
        {
            case GameState.PlayerControl:
                Time.timeScale = 1;
                break;
            case GameState.Combat:
                Time.timeScale = 1;
                break;
            case GameState.Cutscene:
                Time.timeScale = 1;
                break;
            case GameState.PauseMenu:
                Time.timeScale = 0;
                break;
            case GameState.UpgradeMenu:
                Time.timeScale = 0;
                break;
        }
        
        previousGameState = CurrentGameState;
        CurrentGameState = state;
    }
    
    #endregion
}
