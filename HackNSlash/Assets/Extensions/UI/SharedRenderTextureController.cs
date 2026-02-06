using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace Extensions.UI
{
    /// <summary>
    /// Manages a shared render texture that smoothly transitions between tab views.
    /// Uses PrimeTween for stylish animations with unscaled time (menu has timeScale = 0).
    /// Instead of each tab having its own render texture, this single instance moves and scales.
    /// </summary>
    public class SharedRenderTextureController : MonoBehaviour
    {
        [Header("Render Texture Configuration")]
        [SerializeField] private Camera renderCamera;
        [SerializeField] private RenderTexture renderTexture;
        [SerializeField] private RawImage targetImage;
        [SerializeField] private LayerMask cullingMask = -1;
        
        [Header("Animation Settings")]
        [SerializeField] private float transitionDuration = 0.4f;
        [SerializeField] private Ease transitionEase = Ease.OutCubic;
        [SerializeField] private float scaleOvershoot = 1.05f;
        
        [Header("Position Presets")]
        [SerializeField] private RectTransform[] tabPositions; // Assign position transforms for each tab
        
        private RectTransform rectTransform;
        private int currentTabIndex = -1;
        private Tween currentTween;
        
        #region MonoBehaviour Callbacks
        
        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            
            if (targetImage == null)
                targetImage = GetComponent<RawImage>();
            
            ApplyConfiguration();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Moves the render texture to the specified tab position with smooth animation.
        /// </summary>
        /// <param name="tabIndex">The index of the tab to move to.</param>
        public void MoveToTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= tabPositions.Length)
            {
                Debug.LogWarning($"SharedRenderTextureController: Invalid tab index {tabIndex}");
                return;
            }
            
            if (tabIndex == currentTabIndex)
                return; // Already at this position
            
            currentTabIndex = tabIndex;
            RectTransform targetPosition = tabPositions[tabIndex];
            
            if (targetPosition == null)
            {
                Debug.LogWarning($"SharedRenderTextureController: No position set for tab {tabIndex}");
                return;
            }
            
            // Cancel any ongoing animation
            currentTween.Stop();
            
            // Animate position with slight overshoot for snappy feel
            Sequence.Create()
                .Group(Tween.Position(rectTransform, targetPosition.position, 
                    duration: transitionDuration, ease: transitionEase, useUnscaledTime: true))
                .Group(Tween.Scale(rectTransform, scaleOvershoot, 
                    duration: transitionDuration * 0.3f, ease: Ease.OutQuad, useUnscaledTime: true))
                .Chain(Tween.Scale(rectTransform, 1f, 
                    duration: transitionDuration * 0.2f, ease: Ease.InQuad, useUnscaledTime: true));
            
            // Match size if needed (instant, as PrimeTween doesn't have direct sizeDelta animation)
            // Position and scale animations provide enough visual feedback
            if (rectTransform.sizeDelta != targetPosition.sizeDelta)
            {
                rectTransform.sizeDelta = targetPosition.sizeDelta;
            }
            
            // Activate camera if not already active
            if (renderCamera != null && !renderCamera.enabled)
                renderCamera.enabled = true;
        }
        
        /// <summary>
        /// Activates the render texture camera.
        /// </summary>
        public void Activate()
        {
            if (renderCamera != null)
                renderCamera.enabled = true;
            
            gameObject.SetActive(true);
        }
        
        /// <summary>
        /// Deactivates the render texture camera.
        /// </summary>
        public void Deactivate()
        {
            if (renderCamera != null)
                renderCamera.enabled = false;
            
            currentTween.Stop();
        }
        
        /// <summary>
        /// Sets the culling mask for what the camera renders.
        /// </summary>
        /// <param name="mask">The layer mask to use.</param>
        public void SetCullingMask(LayerMask mask)
        {
            cullingMask = mask;
            if (renderCamera != null)
                renderCamera.cullingMask = mask;
        }
        
        /// <summary>
        /// Hides the render texture with a fade animation.
        /// </summary>
        public void Hide()
        {
            if (targetImage != null)
            {
                Tween.Alpha(targetImage, 0f, duration: 0.2f, 
                    ease: Ease.OutQuad, useUnscaledTime: true);
            }
        }
        
        /// <summary>
        /// Shows the render texture with a fade animation.
        /// </summary>
        public void Show()
        {
            if (targetImage != null)
            {
                Tween.Alpha(targetImage, 1f, duration: 0.3f, 
                    ease: Ease.OutQuad, useUnscaledTime: true);
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void ApplyConfiguration()
        {
            if (targetImage != null && renderTexture != null)
            {
                targetImage.texture = renderTexture;
            }
            
            if (renderCamera != null)
            {
                renderCamera.targetTexture = renderTexture;
                renderCamera.cullingMask = cullingMask;
                renderCamera.enabled = false; // Start disabled
            }
        }
        
        #endregion
    }
}

