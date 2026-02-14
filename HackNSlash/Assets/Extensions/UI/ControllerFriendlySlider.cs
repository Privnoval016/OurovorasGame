using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Extensions.UI
{
    /// <summary>
    /// Makes Unity sliders more controller-friendly by:
    /// 1. Displaying current value as percentage/value
    /// 2. Playing audio feedback on value change
    /// 3. Optionally clamping to increments for cleaner adjustment
    /// 4. Visual feedback when selected
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class ControllerFriendlySlider : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        [Header("Controller Settings")]
        [SerializeField] private float sensitivity = 1f;
        [Tooltip("Snap to increments (e.g., 0.1 for 10% steps). Set to 0 for smooth.")]
        [SerializeField] private float snapIncrement = 0.05f; // 5% steps
        [SerializeField] private bool playAudioOnChange = true;
        
        [Header("Visual Feedback")]
        [SerializeField] private GameObject selectedIndicator;
        
        private Slider slider;
        private float lastValue;
        private float audioThrottleTimer = 0f;
        private const float AUDIO_THROTTLE_TIME = 0.1f; // Limit audio to once per 0.1s
        
        private void Awake()
        {
            slider = GetComponent<Slider>();
            
            if (slider != null)
            {
                lastValue = slider.value;
                slider.onValueChanged.AddListener(OnSliderValueChanged);
            }
            
            if (selectedIndicator != null)
                selectedIndicator.SetActive(false);
        }
        
        private void Update()
        {
            if (audioThrottleTimer > 0f)
                audioThrottleTimer -= Time.unscaledDeltaTime;
        }
        
        private void OnSliderValueChanged(float value)
        {
            // Snap to increment if enabled
            if (snapIncrement > 0f)
            {
                float snapped = Mathf.Round(value / snapIncrement) * snapIncrement;
                if (Mathf.Abs(snapped - value) > 0.001f)
                {
                    slider.value = snapped;
                    return; // Will trigger this callback again with snapped value
                }
            }
            
            // Play audio feedback (throttled)
            if (playAudioOnChange && Mathf.Abs(value - lastValue) > 0.01f)
            {
                if (audioThrottleTimer <= 0f)
                {
                    UIAudio.PlayHover();
                    audioThrottleTimer = AUDIO_THROTTLE_TIME;
                }
            }
            
            lastValue = value;
        }
        
        public void OnSelect(BaseEventData eventData)
        {
            if (selectedIndicator != null)
                selectedIndicator.SetActive(true);
            
            UIAudio.PlayHover();
        }
        
        public void OnDeselect(BaseEventData eventData)
        {
            if (selectedIndicator != null)
                selectedIndicator.SetActive(false);
        }
    }
}

