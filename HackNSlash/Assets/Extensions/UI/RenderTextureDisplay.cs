using UnityEngine;
using UnityEngine.UI;

namespace Extensions.UI
{
    /// <summary>
    /// Component for displaying a render texture on a RawImage.
    /// Manages the render texture lifecycle and camera activation.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class RenderTextureDisplay : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private RenderTextureConfig config;
        
        [Header("UI References")]
        [SerializeField] private RawImage targetImage;
        
        private bool isInitialized = false;
        
        #region MonoBehaviour Callbacks
        
        private void Awake()
        {
            if (targetImage == null)
                targetImage = GetComponent<RawImage>();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Initializes the render texture display with the given configuration.
        /// </summary>
        /// <param name="configuration">The render texture configuration to use.</param>
        public void Initialize(RenderTextureConfig configuration)
        {
            config = configuration;
            ApplyConfiguration();
        }
        
        /// <summary>
        /// Activates the render texture display.
        /// </summary>
        public void Activate()
        {
            if (!isInitialized)
                ApplyConfiguration();
                
            if (config?.renderCamera != null)
                config.renderCamera.enabled = true;
                
            gameObject.SetActive(true);
        }
        
        /// <summary>
        /// Deactivates the render texture display.
        /// </summary>
        public void Deactivate()
        {
            if (config?.renderCamera != null)
                config.renderCamera.enabled = false;
                
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Sets whether this display is visible.
        /// </summary>
        /// <param name="visible">Whether the display should be visible.</param>
        public void SetVisible(bool visible)
        {
            if (visible)
                Activate();
            else
                Deactivate();
        }
        
        #endregion
        
        #region Private Methods
        
        private void ApplyConfiguration()
        {
            if (config == null)
            {
                Debug.LogWarning($"RenderTextureDisplay on {gameObject.name} has no configuration!");
                return;
            }
            
            if (targetImage != null && config.renderTexture != null)
            {
                targetImage.texture = config.renderTexture;
            }
            
            if (config.renderCamera != null)
            {
                config.renderCamera.targetTexture = config.renderTexture;
                config.renderCamera.cullingMask = config.cullingMask;
                config.renderCamera.enabled = config.isActive;
            }
            
            isInitialized = true;
        }
        
        #endregion
    }
}

