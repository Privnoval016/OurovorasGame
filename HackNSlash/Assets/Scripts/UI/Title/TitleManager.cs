using System;
using Extensions.Patterns;
using UnityEngine;

/**
 * <summary>
 * Manages the title screen and provides a hook to transition into the game.
 * Calls <see cref="SceneLoader.LoadMainScene"/> to bring in the persistent player scene.
 * </summary>
 */
public class TitleManager : Singleton<TitleManager>
{
    /** <summary>Called to begin loading the game from the title screen.</summary> */
    public void BeginGame()
    {
        SceneLoader.Instance.LoadMainScene();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            BeginGame();
        }
    }
}
