using UnityEngine;
using UnityEngine.Video;

namespace Extensions.UI
{
    /// <summary>
    /// Component for managing video playback in UI panels.
    /// Used for ability demonstrations and preview videos.
    /// </summary>
    [RequireComponent(typeof(VideoPlayer))]
    public class VideoDisplay : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private VideoDisplayConfig config;
        
        [Header("Component References")]
        [SerializeField] private VideoPlayer videoPlayer;
        
        private bool isInitialized = false;
        
        #region MonoBehaviour Callbacks
        
        private void Awake()
        {
            if (videoPlayer == null)
                videoPlayer = GetComponent<VideoPlayer>();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Initializes the video display with the given configuration.
        /// </summary>
        /// <param name="configuration">The video display configuration to use.</param>
        public void Initialize(VideoDisplayConfig configuration)
        {
            config = configuration;
            ApplyConfiguration();
        }
        
        /// <summary>
        /// Sets the video clip to play.
        /// </summary>
        /// <param name="clip">The video clip to play.</param>
        /// <param name="autoPlay">Whether to automatically start playback.</param>
        public void SetVideo(VideoClip clip, bool autoPlay = true)
        {
            if (videoPlayer == null) return;
            
            videoPlayer.clip = clip;
            
            if (autoPlay && clip != null)
                videoPlayer.Play();
            else
                videoPlayer.Stop();
        }
        
        /// <summary>
        /// Plays the current video.
        /// </summary>
        public void Play()
        {
            if (videoPlayer != null && videoPlayer.clip != null)
                videoPlayer.Play();
        }
        
        /// <summary>
        /// Stops the current video.
        /// </summary>
        public void Stop()
        {
            if (videoPlayer != null)
                videoPlayer.Stop();
        }
        
        /// <summary>
        /// Pauses the current video.
        /// </summary>
        public void Pause()
        {
            if (videoPlayer != null)
                videoPlayer.Pause();
        }
        
        /// <summary>
        /// Activates the video display.
        /// </summary>
        public void Activate()
        {
            if (!isInitialized)
                ApplyConfiguration();
                
            gameObject.SetActive(true);
            
            if (config?.autoPlay ?? false)
                Play();
        }
        
        /// <summary>
        /// Deactivates the video display.
        /// </summary>
        public void Deactivate()
        {
            Stop();
            gameObject.SetActive(false);
        }
        
        #endregion
        
        #region Private Methods
        
        private void ApplyConfiguration()
        {
            if (config == null)
            {
                Debug.LogWarning($"VideoDisplay on {gameObject.name} has no configuration!");
                return;
            }
            
            if (videoPlayer != null)
            {
                if (config.videoClip != null)
                    videoPlayer.clip = config.videoClip;
                    
                videoPlayer.isLooping = config.loop;
                
                if (config.autoPlay && config.videoClip != null)
                    videoPlayer.Play();
            }
            
            isInitialized = true;
        }
        
        #endregion
    }
}

