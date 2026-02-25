using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Extensions.UI
{
    /// <summary>
    /// Makes Unity dropdowns more controller-friendly by:
    /// 1. Playing audio when dropdown opens/closes
    /// 2. Visual feedback when selected
    /// 3. Handles controller navigation properly
    /// 
    /// NOTE: Dropdowns spawn a separate list GameObject that handles its own navigation.
    /// This is handled by Unity's InputSystemUIInputModule automatically.
    /// </summary>
    [RequireComponent(typeof(TMP_Dropdown))]
    public class ControllerFriendlyDropdown : MonoBehaviour, ISelectHandler, IDeselectHandler, ISubmitHandler
    {
        [Header("Visual Feedback")]
        [SerializeField] private GameObject selectedIndicator;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image borderImage;
        
        private TMP_Dropdown dropdown;
        private UIAnimationManager animationManager;
        
        private void Awake()
        {
            dropdown = GetComponent<TMP_Dropdown>();
            animationManager = UIAnimationManager.Instance;
            
            if (dropdown != null)
            {
                // Play audio when dropdown changes value (option selected)
                dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            }
            
            if (selectedIndicator != null)
                selectedIndicator.SetActive(false);
        }
        
        private void OnDropdownValueChanged(int index)
        {
            UIAudio.PlaySelect();
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
        
        public void OnSubmit(BaseEventData eventData)
        {
            // Play sound when opening dropdown
            UIAudio.PlaySelect();
            
            // Punch animation
            if (animationManager != null)
            {
                animationManager.CreateBuilder(transform)
                    .AnimatePunch();
            }
        }
    }
}

