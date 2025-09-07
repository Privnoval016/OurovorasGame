using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSceneLoader : MonoBehaviour
{
    private void Awake()
    {
        SceneManager.LoadScene(1, LoadSceneMode.Additive);
    }
}
