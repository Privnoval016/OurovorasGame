using Extensions.UI;
using PrimeTween;
using Systems.Element;
using UnityEngine;
using UnityEngine.UI;

public class ElementAttackSlider : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private KeyBind attackKeybind;
    [SerializeField] private Slider elementSlider;
    [SerializeField] private Vector2 minMaxFillAmounts = new Vector2(0.125f, 0.875f);
    
    [Header("UI Components")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image fillImage;
    [SerializeField] private Image centerImage;
    [SerializeField] private Image attackIconImage;
    [SerializeField] private Color inactiveColor = Color.gray;
    
    public float CurrentValue { get; private set; }

    private ElementEffect currentElement = ElementEffect.None;
    private Color centerColor;
    private Color iconColor;
    private Color fillColor;
    
    private Sequence colorSequence;

    /**
     * <summary>
     * Sets the slider value instantly without animation.
     * </summary>
     *
     * <param name="value">The new value for the slider (0 to 1).</param>
     */
    public void SetSliderValueInstant(float value)
    {
        value = Mathf.Clamp01(value);
        
        if (Mathf.Approximately(CurrentValue, value)) return;
        
        CurrentValue = value;
        CheckForActivation();
        
        elementSlider.value = Mathf.Lerp(minMaxFillAmounts.x, minMaxFillAmounts.y, value);
    }
    
    
    public void SetSliderValueAnimated(float value, float duration)
    {
        value = Mathf.Clamp01(value);
        
        if (Mathf.Approximately(CurrentValue, value)) return;
        
        CurrentValue = value;
        CheckForActivation();
        
        float targetFill = Mathf.Lerp(minMaxFillAmounts.x, minMaxFillAmounts.y, value);
        Tween.CompleteAll(elementSlider);
        Tween.UISliderValue(elementSlider, targetFill, duration);
    }
    
    private void CheckForActivation()
    {
        if (CurrentValue >= 1f && currentElement != ElementEffect.None)
        {
            centerColor = Services.Get<ElementSystem>().GetElementData(currentElement).elementColor;
            iconColor = Services.Get<ElementSystem>().GetElementData(currentElement).elementInactiveColor;
            fillColor = Services.Get<ElementSystem>().GetElementData(currentElement).elementColor;
        }
        else
        {
            centerColor = inactiveColor;
            iconColor = Services.Get<ElementSystem>().GetElementData(currentElement).elementColor;
            fillColor = Services.Get<ElementSystem>().GetElementData(currentElement).elementInactiveColor;
        }
        
        if (centerImage.color == centerColor && attackIconImage.color == iconColor && fillImage.color == fillColor) return;
        
        if (colorSequence.isAlive)
        {
            colorSequence.Complete();
        }
        
        colorSequence = Sequence.Create()
            .Group(Tween.Color(centerImage, centerColor, 0.2f))
            .Group(Tween.Color(attackIconImage, iconColor, 0.2f))
            .Group(Tween.Color(fillImage, fillColor, 0.2f))
            .ChainCallback(() => attackIconImage.gameObject.PulseAfterimage(1.7f, 0.5f, 0.9f));
    }
    
    public void SwapCurrentElement(ElementEffect element, AttacksByWeapon attacksByWeapon, MovingStates movingState)
    {
        if (currentElement == element) return;
        currentElement = element;
        
        if (attacksByWeapon == null)
        {
            EnableAttackIcon(false);
            return;
        }

        Attack attack = attacksByWeapon.GetAttackByState(movingState);
        
        EnableAttackIcon(attack != null);
    }

    private void EnableAttackIcon(bool enable)
    {
        CheckForActivation();
        
        if (Mathf.Approximately(canvasGroup.alpha, enable ? 1f : 0f)) return;
        
        gameObject.AlphaFade(0.2f, enable ? 0f : 1f, enable ? 1f : 0f);
    }
}