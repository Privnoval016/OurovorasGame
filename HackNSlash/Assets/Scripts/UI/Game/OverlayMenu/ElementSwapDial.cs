using System.Collections.Generic;
using System.Linq;
using Extensions.UI;
using Extensions.Utils;
using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

public class ElementSwapDial : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image elementImage;
    [SerializeField] private RectTransform dialRectTransform;
    [SerializeField] private List<Image> dialElements;

    [Header("Angles")] [SerializeField] private int centeringOffset = 2;
    [SerializeField] private float stepAngle = 18f;
    [SerializeField] private float overshootAngle = 5f;
    
    [Header("Durations")]
    [SerializeField] private float rotateDuration = 0.25f;
    [SerializeField] private float clickDuration = 0.1f;
    
    private float targetIndex = 0f;
    
    private float TargetAngle => targetIndex * stepAngle;
    private float MaxNotches => stepAngle != 0f ? 360f / stepAngle : 1f;

    private Tween activeTween;

    private ElementEffect selectedElement;
    
    private List<ElementEffect> elements;
    
    public void SetDial(List<ElementEffect> elementList, ElementEffect currentElement) 
    {
        elements = elementList;
        
        for (int i = 0; i < elements.Count; i++)
        {
            var data = Services.Get<ElementSystem>().GetElementData(elements[i]);
            dialElements[i].color = data.elementColor;
        }
        
        targetIndex = (elements.IndexOf(currentElement) - centeringOffset + MaxNotches) % MaxNotches;
        dialRectTransform.localRotation = Quaternion.Euler(0, 0, TargetAngle);
        SetElement(currentElement);
    }
    
    public void RotateInDirection(int direction, ElementEffect element)
    {
        if (direction == 0)
        {
            // No rotation, just set the element
            SetElement(element);
            return;
        }

        targetIndex += direction;
        targetIndex = (targetIndex + MaxNotches) % MaxNotches;
        PlayTween(direction, element);
    }

    private void SetElement(ElementEffect element)
    {
        elementImage.color = Services.Get<ElementSystem>().GetElementData(element).elementColor;
        elementImage.gameObject.PulseAfterimage(1.5f, 0.5f, 0.6f);
    }
    
    void PlayTween(int direction, ElementEffect element) 
    {
        
        if (activeTween.isAlive)
            activeTween.Stop();

        float overshoot = TargetAngle + direction * overshootAngle;

        activeTween = Tween.LocalRotation(
            dialRectTransform,
            new Vector3(0, 0, overshoot),
            rotateDuration,
            Ease.OutCubic
        ).OnComplete(() =>
            {
                Tween.LocalRotation(
                    dialRectTransform,
                    new Vector3(0, 0, TargetAngle),
                    clickDuration,
                    Ease.InQuad
                );
                SetElement(element);
            }
        );
    }
}