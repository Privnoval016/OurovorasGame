using System;
using Animancer;
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
        [SerializeField] private Transform playerModel;
        [SerializeField] private AnimancerComponent animancer;
        [SerializeField] private RawImage targetImage;
        [SerializeField] private LayerMask cullingMask = -1;
        
        [Header("Animation Settings")]
        [SerializeField] private float transitionDuration = 0.4f;
        [SerializeField] private Ease transitionEase = Ease.OutCubic;
        [SerializeField] private float scaleOvershoot = 1.05f;

        [Header("Position Presets")] 
        [SerializeField] private Vector3 playerScale;
        [SerializeField] private TabPositionInfo[] tabPositions; // Assign position transforms for each tab
        
        private int currentTabIndex = -1;
        private Tween currentTween;
        
        #region MonoBehaviour Callbacks
        
        private void Awake()
        {
            ApplyConfiguration();
            SetCullingMask(cullingMask);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Moves the render texture to the specified tab position with smooth animation.
        /// </summary>
        /// <param name="tabIndex">The index of the tab to move to.</param>
        /// <param name="force">If true, forces the move even if already at the target tab.</param>
        public void MoveToTab(int tabIndex, bool force = false)
        {
            if (tabIndex < 0 || tabIndex >= tabPositions.Length)
            {
                Debug.LogWarning($"SharedRenderTextureController: Invalid tab index {tabIndex}");
                return;
            }
            
            if (tabIndex == currentTabIndex && !force)
                return; // Already at this position
            
            if (!tabPositions[tabIndex].enableRenderTexture)
            {
                Hide();
                return;
            }

            Show();
            
            currentTabIndex = tabIndex;
            RectTransform targetPosition = tabPositions[tabIndex].uiPosition;
            
            if (targetPosition == null)
            {
                Debug.LogWarning($"SharedRenderTextureController: No position set for tab {tabIndex}");
                return;
            }
            
            // Cancel any ongoing animation
            currentTween.Stop();
            
            animancer.Play(tabPositions[tabIndex].animationClip);
            
            //Animate all properties together for a smooth transition
            Sequence.Create(useUnscaledTime: true)
                .Group(Tween.Position(targetImage.rectTransform, targetPosition.position, transitionDuration, transitionEase))
                .Group(Tween.Position(playerModel, tabPositions[tabIndex].playerPosition.position,
                    transitionDuration, transitionEase))
                .Group(Tween.Rotation(playerModel, tabPositions[tabIndex].playerPosition.rotation,
                    transitionDuration, transitionEase))
                .Group(Tween.Scale(targetImage.rectTransform, targetPosition.localScale * scaleOvershoot, transitionDuration * 0.5f,
                    Ease.OutQuad))
                .Chain(Tween.Scale(targetImage.rectTransform, targetPosition.localScale, transitionDuration * 0.5f, Ease.InQuad));
            
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
            if (targetImage != null && targetImage.color.a > 0f)
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
            if (targetImage != null && targetImage.color.a < 1f)
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
            
            playerModel.localScale = playerScale;
        }
        
        #endregion
    }

    [Serializable]
    public class TabPositionInfo
    {
        public bool enableRenderTexture = true;
        [Header("UI")]
        public RectTransform uiPosition;
        
        [Header("Camera")]
        public Transform playerPosition;
        
        [Header("Animation")]
        public AnimationClip animationClip;
    }
}

