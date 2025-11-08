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
    
    public static readonly Dictionary<ElementEffect, Func<ElementData>> ElementMap = new();
    
    
    [Header("In Game Instances")]
    public PlayerController pc;
    
    
    [Header("Global Parameters")]
    public float globalGravity = -9.81f;
    
    public LayerMask groundLayer;
    public LayerMask enemyLayer;
    
    public ElementData[] elementData;

    #region MonoBehavior Callbacks
    
    protected override void Awake()
    {
        base.Awake();
        
        if (pc == null)
            GameObject.FindWithTag("Player").TryGetComponent(out pc);
        
        CurrentGameState = GameState.PlayerControl;
        
        SetElementMap();
        
        InputManager.Instance.onPause += OnPauseAction;
    }

    private void Start()
    {
        OverworldMenuUI.Instance.gameObject.SetActive(true);
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
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        CombatManager.Instance.ApplyPausedTimescale(pc, false);
    }
    
    private void SetCutsceneState()
    {
        OverworldMenuUI.Instance.CloseMenu();
        CombatManager.Instance.ApplyPausedTimescale(pc, true);
    }
    
    private void SetMenuState()
    {
        OverworldMenuUI.Instance.OpenMenu();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        CombatManager.Instance.ApplyPausedTimescale(pc, true);
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
        
        ElementMap.Add(ElementEffect.MatchCurrent, () => GetElementData(pc?.pcc?.currentElementEffect ?? ElementEffect.None));
    }
    
    public static ElementData GetElementData(ElementEffect elementEffect)
    {
        if (ElementMap.TryGetValue(elementEffect, out var elementFunc))
        {
            return elementFunc();
        }
        
        return null;
    }
    
    #endregion
}