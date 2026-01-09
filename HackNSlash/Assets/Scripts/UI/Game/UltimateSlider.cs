using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

/*
 * UltimateSlider charges on the left first until it reaches the swapThreshold,
 * then it starts charging the right slider, keeping the left slider full.
 * When decreasing, it depletes the right slider first until empty,
 * then starts depleting the left slider.
 */
public class UltimateSlider : MonoBehaviour
{
    private int valuePropID = Shader.PropertyToID("_Fill");
    [Header("Settings")]
    [SerializeField, Range(0, 1)] private float swapThreshold = 0.5f;
    [SerializeField] private float swapBuffer = 0.05f;
    public float CurrentValue { get; private set; }
    
    [Header("Left Slider")]
    [SerializeField] private Material leftSliderMat;
    [SerializeField] private Image leftSliderSprite;
    private Material leftSlider;
    [SerializeField] private Vector2 minMaxLeft;
    
    [Header("Right Slider")]
    [SerializeField] private Material rightSliderMat;
    [SerializeField] private Image rightSliderSprite;
    private Material rightSlider;
    [SerializeField] private Vector2 minMaxRight;
    
    private void Awake()
    {
        InitializeSliders();
    }
    
    private void InitializeSliders()
    {
        leftSlider = new Material(leftSliderMat);
        leftSliderSprite.material = leftSlider;
        
        rightSlider = new Material(rightSliderMat);
        rightSliderSprite.material = rightSlider;
        
        SetSliderValueInstant(0f);
    }
    
    /**
     * <summary>
     * Sets the slider value instantly, distributing the value between the left and right sliders based on the swapThreshold.
     * </summary>
     * <param name="value">The target value to set the slider to (0 to 1).</param>
     */
    public void SetSliderValueInstant(float value)
    {
        value = Mathf.Clamp01(value);
        CurrentValue = value;
        
        if (value <= swapThreshold)
        {
            leftSlider.SetFloat(valuePropID, Mathf.Lerp(minMaxLeft.x, minMaxLeft.y - swapBuffer, Mathf.InverseLerp(0f, swapThreshold, value)));
            rightSlider.SetFloat(valuePropID, minMaxLeft.x);
        }
        else
        {
            leftSlider.SetFloat(valuePropID, minMaxLeft.y);
            rightSlider.SetFloat(valuePropID, Mathf.Lerp(minMaxRight.x + swapBuffer, minMaxRight.y, Mathf.InverseLerp(swapThreshold, 1f, value)));
        }
    }
    
    /**
     * <summary>
     * Sets the slider value with an animation over the specified duration, distributing the value between the left and right sliders based on the swapThreshold.
     * </summary>
     * <param name="value">The target value to set the slider to (0 to 1).</param>
     * <param name="duration">The duration of the animation in seconds.</param>
     */
    public void SetSliderValueAnimated(float value, float duration)
    {
        value = Mathf.Clamp01(value); 
        CurrentValue = value;
        
        float leftSliderTarget;
        float rightSliderTarget;
        if (value <= swapThreshold)
        {
            leftSliderTarget = Mathf.Lerp(minMaxLeft.x, minMaxLeft.y - swapBuffer, Mathf.InverseLerp(0f, swapThreshold, value));
            rightSliderTarget = minMaxRight.x;
        }
        else
        {
            leftSliderTarget = minMaxLeft.y;
            rightSliderTarget = Mathf.Lerp(minMaxRight.x + swapBuffer, minMaxRight.y, Mathf.InverseLerp(swapThreshold, 1f, value));
        }
        
        if (!Mathf.Approximately(leftSlider.GetFloat(valuePropID), leftSliderTarget))
            Tween.MaterialProperty(leftSlider, valuePropID, leftSliderTarget, duration);
        if (!Mathf.Approximately(rightSlider.GetFloat(valuePropID), rightSliderTarget))
            Tween.MaterialProperty(rightSlider, valuePropID, rightSliderTarget, duration);
    }
}