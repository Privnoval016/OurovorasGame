using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Extensions.UI
{
    /// <summary>
    /// Custom selectable button for save/load slots.
    /// Displays slot information (save name, timestamp, level, etc.).
    /// </summary>
    public class SaveSlotButton : Selectable, ISubmitHandler
    {
        [Header("UI References")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image borderImage;
        [SerializeField] private TextMeshProUGUI slotNumberText;
        [SerializeField] private TextMeshProUGUI saveNameText;
        [SerializeField] private TextMeshProUGUI timestampText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private GameObject emptySlotIndicator;
        
        [Header("Event")]
        public UnityEngine.Events.UnityEvent<int> onSlotSelected;
        
        private int slotIndex;
        private bool isEmpty;
        private UIAnimationManager animationManager;
        
        protected override void Awake()
        {
            base.Awake();
            animationManager = UIAnimationManager.Instance;
        }
        
        /// <summary>
        /// Initializes the save slot with data.
        /// </summary>
        public void Initialize(int index, string saveName, string timestamp, string level, bool empty)
        {
            slotIndex = index;
            isEmpty = empty;
            
            if (slotNumberText != null)
                slotNumberText.text = index == -1 ? "AUTOSAVE" : $"SLOT {index + 1}";
            
            if (empty)
            {
                if (emptySlotIndicator != null)
                    emptySlotIndicator.SetActive(true);
                
                if (saveNameText != null)
                    saveNameText.text = "Empty Slot";
                
                if (timestampText != null)
                    timestampText.text = "";
                
                if (levelText != null)
                    levelText.text = "";
            }
            else
            {
                if (emptySlotIndicator != null)
                    emptySlotIndicator.SetActive(false);
                
                if (saveNameText != null)
                    saveNameText.text = saveName;
                
                if (timestampText != null)
                    timestampText.text = timestamp;
                
                if (levelText != null)
                    levelText.text = $"Level {level}";
            }
            
            if (animationManager == null)
                animationManager = UIAnimationManager.Instance;
            
            UpdateVisuals();
        }
        
        #region Selectable Overrides
        
        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            
            if (animationManager != null)
            {
                animationManager.CreateBuilder(transform, borderImage, backgroundImage)
                    .AnimateSelection();
            }
            
            UIAudio.PlayHover();
        }
        
        public override void OnDeselect(BaseEventData eventData)
        {
            base.OnDeselect(eventData);
            
            if (animationManager != null)
            {
                animationManager.CreateBuilder(transform, borderImage, backgroundImage)
                    .AnimateDeselection();
            }
            
            UpdateVisuals();
        }
        
        public void OnSubmit(BaseEventData eventData)
        {
            if (animationManager != null)
            {
                animationManager.CreateBuilder(transform)
                    .AnimatePunch();
            }
            
            onSlotSelected?.Invoke(slotIndex);
            UIAudio.PlaySelect();
        }
        
        #endregion
        
        private void UpdateVisuals()
        {
            if (animationManager == null)
            {
                animationManager = UIAnimationManager.Instance;
                if (animationManager == null) return;
            }
            
            Color normalColor = animationManager.GetNormalColor();
            
            if (backgroundImage != null)
                backgroundImage.color = isEmpty ? animationManager.GetEmptyColor() : normalColor;
            
            if (borderImage != null)
                borderImage.color = normalColor;
        }
    }
}

