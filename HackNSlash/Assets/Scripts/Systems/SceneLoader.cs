using System.Collections.Generic;
using Extensions.Patterns;
using Extensions.Utils;
using MEC;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : Singleton<SceneLoader>
{
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }
    
    public void OnSceneLoad(string mainSceneAsset, params string[] additiveSceneAssets)
    {
        Debug.Log("OnStartButtonPressed");
        this.RunSegmentCoroutine(LoadMainScene(mainSceneAsset, additiveSceneAssets));
    }
    
    private IEnumerator<float> LoadMainScene(string mainSceneAsset, string[] additiveSceneAssets)
    {
        // Load main scene asynchronously
        AsyncOperation mainSceneAsync = SceneManager.LoadSceneAsync(mainSceneAsset);
        mainSceneAsync.allowSceneActivation = true;
        yield return Timing.WaitUntilDone(mainSceneAsync);
        
        yield return Timing.WaitForOneFrame;
        
        // Load additive scenes asynchronously
        
        foreach (var additiveSceneAsset in additiveSceneAssets)
        {
            AsyncOperation additiveSceneAsync = SceneManager.LoadSceneAsync(additiveSceneAsset, LoadSceneMode.Additive);
            additiveSceneAsync.allowSceneActivation = true;
            yield return Timing.WaitUntilDone(additiveSceneAsync);
        }
    }
}
