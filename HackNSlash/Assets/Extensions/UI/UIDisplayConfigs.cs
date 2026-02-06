using UnityEngine;
using UnityEngine.Video;

namespace Extensions.UI
{
    /// <summary>
    /// Configuration data for a render texture display in the UI.
    /// Can be used to show 3D model previews, ability demonstrations, etc.
    /// </summary>
    [System.Serializable]
    public class RenderTextureConfig
    {
        [Tooltip("The camera that renders to this texture")]
        public Camera renderCamera;
        
        [Tooltip("The render texture to display")]
        public RenderTexture renderTexture;
        
        [Tooltip("Whether this render texture should be active")]
        public bool isActive = true;
        
        [Tooltip("Layer mask for what this camera should render")]
        public LayerMask cullingMask = -1;
    }
    
    /// <summary>
    /// Configuration for video playback in UI panels.
    /// Used for ability demonstrations and preview videos.
    /// </summary>
    [System.Serializable]
    public class VideoDisplayConfig
    {
        [Tooltip("The video player component")]
        public VideoPlayer videoPlayer;
        
        [Tooltip("The video clip to play")]
        public VideoClip videoClip;
        
        [Tooltip("Whether the video should loop")]
        public bool loop = true;
        
        [Tooltip("Whether the video should auto-play when set")]
        public bool autoPlay = false;
    }
}

