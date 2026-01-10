using System;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;
using Sirenix.OdinInspector;

namespace Extensions.UI
{
    public class SliderBar : MonoBehaviour
    {
        [Header("Main Slider")]
        [SerializeField] private Slider mainSlider;
        
        [Header("Refill Slider")]
        [SerializeField] private bool useRefillSlider = true;
        [SerializeField] private Slider refillSlider;
        
        [Header("Drain Slider")]
        [SerializeField] private bool useDrainSlider = true;
        [SerializeField] private Slider drainSlider;
        
        [Header("Initialization Settings")]
        [SerializeField] private float initialValue = 1f;
        [SerializeField] private Slider.Direction sliderDirection = Slider.Direction.LeftToRight;

        public float CurrentValue => mainSlider.value;
        
        private void Awake()
        {
            SetDirection();
        }
        
        [Button]
        private void SetDirection()
        {
            mainSlider.direction = sliderDirection;
            mainSlider.value = initialValue;

            if (useRefillSlider && refillSlider != null)
            {
                refillSlider.direction = sliderDirection;
                refillSlider.value = initialValue;
            }

            if (useDrainSlider && drainSlider != null)
            {
                drainSlider.direction = sliderDirection;
                drainSlider.value = initialValue;
            }
        }
        
        public void SetSliderValueInstant(float value)
        {
            mainSlider.value = value;

            if (useRefillSlider && refillSlider != null)
            {
                refillSlider.value = value;
            }

            if (useDrainSlider && drainSlider != null)
            {
                drainSlider.value = value;
            }
        }

        public void TweenSliderValue(float targetValue, float mainDuration, float drainDuration = 0.01f, float refillDuration = 0.01f)
        {
            if (Mathf.Approximately(mainSlider.value, targetValue))
            {
                return; // No change needed
            }
            
            if (mainSlider.value > targetValue)
            {
                // Decrease health
                if (refillSlider != null) refillSlider.value = targetValue;
                
                Tween.UISliderValue(mainSlider, targetValue, mainDuration);
                
                if (drainSlider != null)
                    Tween.UISliderValue(drainSlider, targetValue, drainDuration);
            }
            else if (mainSlider.value < targetValue)
            {
                // Increase health
                if (refillSlider != null)
                    Tween.UISliderValue(refillSlider, targetValue, refillDuration);
                
                Tween.UISliderValue(mainSlider, targetValue, mainDuration);
            }
        }
    }
}