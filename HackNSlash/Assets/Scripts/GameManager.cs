using System;
using System.Collections;
using System.Collections.Generic;
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
    Water,
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
    
    public static readonly Dictionary<ElementEffect, Func<ElementData>> ElementMap = new();
    
    
    [Header("In Game Instances")]
    public PlayerController pc;
    
    
    [Header("Global Parameters")]
    public float globalGravity = -9.81f;
    
    public ElementData[] elementData;

    #region MonoBehavior Callbacks
    
    protected override void Awake()
    {
        base.Awake();
        
        if (pc == null)
            GameObject.FindWithTag("Player").TryGetComponent(out pc);
        
        CurrentGameState = GameState.PlayerControl;
        
        SetElementMap();
        
        InputManager.Instance.pause.performed += OnPauseAction;
    }

    private void Start()
    {
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
        OverworldMenuUI.Instance.CloseMenu();
        Time.timeScale = 1;
    }
    
    private void SetCutsceneState()
    {
        OverworldMenuUI.Instance.CloseMenu();
        Time.timeScale = 1;
    }
    
    private void SetMenuState()
    {
        OverworldMenuUI.Instance.OpenMenu();
        Time.timeScale = 0;
    }
    
    #endregion
    
    #region Element Methods
    
    private void SetElementMap()
    {
        if (elementData == null) return;
        
        foreach (ElementData element in elementData)
        {
            ElementMap.Add(element.element, () => element);
        }
        
        ElementMap.Add(ElementEffect.MatchCurrent, () => GetElementData(pc?.pi?.CurrentElementEffect ?? ElementEffect.None));
    }
    
    public static ElementData GetElementData(ElementEffect elementEffect)
    {
        if (ElementMap.TryGetValue(elementEffect, out var elementFunc))
        {
            return elementFunc();
        }
        
        Debug.LogWarning($"No ElementData found for {elementEffect}");
        return null;
    }
    
    #endregion
}
