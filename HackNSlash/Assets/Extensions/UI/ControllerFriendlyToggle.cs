using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Extensions.UI
{
    /// <summary>
    /// Makes Unity toggles more controller-friendly by:
    /// 1. Playing audio on toggle
    /// 2. Visual feedback when selected
    /// 3. Better animation support
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class ControllerFriendlyToggle : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        [Header("Visual Feedback")]
        [SerializeField] private GameObject selectedIndicator;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image borderImage;
        
        private Toggle toggle;
        private UIAnimationManager animationManager;
        
        private void Awake()
        {
            toggle = GetComponent<Toggle>();
            animationManager = UIAnimationManager.Instance;
            
            if (toggle != null)
            {
                toggle.onValueChanged.AddListener(OnToggleValueChanged);
            }
            
            if (selectedIndicator != null)
                selectedIndicator.SetActive(false);
        }
        
        private void OnToggleValueChanged(bool isOn)
        {
            // Play different sound based on on/off state
            if (isOn)
                UIAudio.PlaySelect();
            else
                UIAudio.PlayBack();
            
            // Punch animation on toggle
            if (animationManager != null)
            {
                animationManager.CreateBuilder(transform)
                    .AnimatePunch();
            }
        }
        
        public void OnSelect(BaseEventData eventData)
        {
            if (selectedIndicator != null)
                selectedIndicator.SetActive(true);
            
            // Animate selection
            if (animationManager != null && (borderImage != null || backgroundImage != null))
            {
                animationManager.CreateBuilder(transform, borderImage, backgroundImage)
                    .AnimateSelection();
            }
            
            UIAudio.PlayHover();
        }
        
        public void OnDeselect(BaseEventData eventData)
        {
            if (selectedIndicator != null)
                selectedIndicator.SetActive(false);
            
            // Animate deselection
            if (animationManager != null && (borderImage != null || backgroundImage != null))
            {
                animationManager.CreateBuilder(transform, borderImage, backgroundImage)
                    .AnimateDeselection();
            }
        }
    }
}

