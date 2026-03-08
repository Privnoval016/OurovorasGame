using System.Linq;
using Extensions.EventBus;
using Extensions.UI;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDMenuUI : MonoBehaviour, IService
{
    public PlayerController pc;
    
    #region Inspector Fields
    [Header("Health Sliders")]
    [SerializeField] private SliderBar healthSliderBar;
    
    
    [Header("Charge Sliders")]
    [SerializeField] private SliderBar chargeSliderBar;
    
    [SerializeField] private RectTransform[] minChargeIndicators;
    
    [Header("Ultimate Sliders")]
    [SerializeField] private UltimateSlider ultimateValueBar;

    [Header("Finisher Sliders")] [SerializeField]
    private ElementAttackSlider finisherAttackSlider;
    
    [Header("Element Swap")]
    [SerializeField] private ElementSwapDial elementSwapDial;
    
    [Header("Elemental Swap (Old)")]
    
    [SerializeField] private RectTransform elementSwapContainer;
    
    [SerializeField] private RectTransform radialMenuContainer;
    
    [SerializeField] private RawImage elementSwapBackground;
    
    [SerializeField] private RawImage selectedElementIcon;
    [SerializeField] private RawImage[] elementIcons;
    private RectTransform[] elementIconRects;

    [SerializeField] private RectTransform elementSelectBorder;
    [SerializeField] private RectTransform elementSelectLine;
    
    [Space(20)]
    [SerializeField] private float[] elementSwapScales = new float[] { 0.01f, 1.0f };
    [SerializeField] private Vector3[] elementalSwapPositions;
    [SerializeField] private float radialMenuSelectScaleFactor = 1.2f;
    private Vector3 radialMenuDefaultScale;
    private int selectedElementIndex;
    [SerializeField] private float elementSwapBackgroundAlpha = 0.3f;
    
    
    [Header("Elemental Attacks")]
    [SerializeField] private RectTransform elementalAttackContainer;
    private Vector3 elementalAttackContainerScale;
    [SerializeField] private float elementalAttackContainerScaleFactor = 1.3f;
    
    [SerializeField] private ElementAttackSlider northElementalAttack;
    [SerializeField] private ElementAttackSlider southElementalAttack;
    [SerializeField] private ElementAttackSlider westElementalAttack;
    
    [Header("Style Meter")]
    [SerializeField] private StyleMeterUI styleMeterUI;
    
    [Header("Enemy UI")]
    [SerializeField] private CanvasGroup enemyUIGroup;
    [SerializeField] private TMP_Text enemyNameText;
    [SerializeField] private TMP_Text enemyLevelText;
    [SerializeField] private SliderBar enemyHealthSliderBar;
    [SerializeField] private SliderBar enemyShieldSliderBar;
    [SerializeField] private TMP_Text enemyHealthBarCountText;
    [SerializeField] private EffectTileUI enemyEffectTileUI;
    
    private LockOnTarget currentLockOnTarget;
    
    #endregion
    
    [HideInInspector] public EquippedElementAttack CurrentElementalAttacks => pc.pcc.GetCurrentElementAttack();
    
    #region Events
    
    private EventBinding<HealthUpdateEvent> healthUpdateEventBinding;
    private EventBinding<ChargeUpdateEvent> chargeUpdateEventBinding;
    private EventBinding<FinisherUpdateEvent> finisherUpdateEventBinding;
    private EventBinding<UltimateUpdateEvent> ultimateUpdateEventBinding;
    private EventBinding<ElementMenuEvent> elementMenuEventBinding;
    private EventBinding<ElementUpdateEvent> elementUpdateEventBinding;
    private EventBinding<ElementAttackUpdateEvent> elementAttackUpdateEventBinding;
    private EventBinding<StyleUpdateEvent> styleUpdateEventBinding;
    private EventBinding<CameraLockOnEvent> cameraLockOnEventBinding;
    private EventBinding<StateSwapEvent> stateSwapEventBinding;
    
    #endregion


    #region MonoBehaviour Callbacks

    public void Awake()
    {
        ActivateEventBindings();
        
        gameObject.SetActive(true);
        
        elementalAttackContainerScale = elementalAttackContainer.localScale;
        finisherAttackSlider.AdditionalActivationCondition = () =>
        {
            if (pc == null) return false;
            return pc.ps.CanUseFinisher();
        };
    }

    private void Start()
    {
        UpdateElementalAttackIcons();
        elementSwapDial.SetDial(pc.rps.elementEffects.ToList(), pc.pcc.currentElementEffect);
        InitializeElementSwapMenu();
    }

    private void Update()
    {
        UpdateEnemyHealthUI();
        UpdateFinisherIcon();
    }

    private void OnDestroy()
    {
        DeactivateEventBindings();
    }

    private void ActivateEventBindings()
    {
        healthUpdateEventBinding = new EventBinding<HealthUpdateEvent>(OnHealthUpdate);
        EventBus<HealthUpdateEvent>.Register(healthUpdateEventBinding);
        
        chargeUpdateEventBinding = new EventBinding<ChargeUpdateEvent>(OnChargeUpdate);
        EventBus<ChargeUpdateEvent>.Register(chargeUpdateEventBinding);
        
        finisherUpdateEventBinding = new EventBinding<FinisherUpdateEvent>(OnFinisherUpdate);
        EventBus<FinisherUpdateEvent>.Register(finisherUpdateEventBinding);
        
        ultimateUpdateEventBinding = new EventBinding<UltimateUpdateEvent>(OnUltimateUpdate);
        EventBus<UltimateUpdateEvent>.Register(ultimateUpdateEventBinding);
        
        elementMenuEventBinding = new EventBinding<ElementMenuEvent>(OnElementMenuUpdate);
        EventBus<ElementMenuEvent>.Register(elementMenuEventBinding);
        
        elementUpdateEventBinding = new EventBinding<ElementUpdateEvent>(OnElementSelect);
        EventBus<ElementUpdateEvent>.Register(elementUpdateEventBinding);
        
        elementAttackUpdateEventBinding = new EventBinding<ElementAttackUpdateEvent>(OnElementalAttackActivate);
        EventBus<ElementAttackUpdateEvent>.Register(elementAttackUpdateEventBinding);
        
        styleUpdateEventBinding = new EventBinding<StyleUpdateEvent>(OnStyleUpdate);
        EventBus<StyleUpdateEvent>.Register(styleUpdateEventBinding);
        
        cameraLockOnEventBinding = new EventBinding<CameraLockOnEvent>(OnCameraLockOnEvent);
        EventBus<CameraLockOnEvent>.Register(cameraLockOnEventBinding);
        
        stateSwapEventBinding = new EventBinding<StateSwapEvent>(OnStateSwapEvent);
        EventBus<StateSwapEvent>.Register(stateSwapEventBinding);
    }

    private void DeactivateEventBindings()
    {
        EventBus<HealthUpdateEvent>.Deregister(healthUpdateEventBinding);
        EventBus<ChargeUpdateEvent>.Deregister(chargeUpdateEventBinding);
        EventBus<FinisherUpdateEvent>.Deregister(finisherUpdateEventBinding);
        EventBus<UltimateUpdateEvent>.Deregister(ultimateUpdateEventBinding);
        EventBus<ElementMenuEvent>.Deregister(elementMenuEventBinding);
        EventBus<ElementUpdateEvent>.Deregister(elementUpdateEventBinding);
        EventBus<ElementAttackUpdateEvent>.Deregister(elementAttackUpdateEventBinding);
        EventBus<StyleUpdateEvent>.Deregister(styleUpdateEventBinding);
        EventBus<CameraLockOnEvent>.Deregister(cameraLockOnEventBinding);
        EventBus<StateSwapEvent>.Deregister(stateSwapEventBinding);
    }

    #endregion
    
    #region Other Slider Methods
    
    private void OnUltimateUpdate(UltimateUpdateEvent e)
    {
        float ultimatePercentage = e.UltimatePercentage;
        
        ultimateValueBar.SetSliderValueAnimated(ultimatePercentage, 0.3f);
    }
    
    private void OnFinisherUpdate(FinisherUpdateEvent e)
    {
        float finisherPercentage = e.finisherPercentage;
        
        finisherAttackSlider.SetSliderValueAnimated(finisherPercentage, 0.2f);
    }
    
    /**
     * Needs to be called every frame because it changes depending on player position
     */
    private void UpdateFinisherIcon()
    {
        finisherAttackSlider.CheckForActivation();
    }
    
    #endregion
    
    #region Element Swap Methods

    private void InitializeElementSwapMenu()
    {
        elementIconRects = new RectTransform[elementIcons.Length];
        for (int i = 0; i < elementIcons.Length; i++)
        {
            elementIconRects[i] = elementIcons[i].GetComponent<RectTransform>();
            ElementEffect element = pc.rps.elementEffects[i];
            
            // Set icon sprite later
            elementIcons[i].color = Services.Get<ElementSystem>().GetElementData(element).elementInactiveColor;
        }
        
        radialMenuDefaultScale = elementIconRects[0].localScale;
        
        DeactivateElementSwapMenu();
    }
    
    private void DeactivateElementSwapMenu()
    {
        if (radialMenuContainer == null) return;

        if (radialMenuContainer.localScale != elementSwapScales[0] * Vector3.one)
            Tween.Scale(radialMenuContainer, elementSwapScales[0], 0.1f, useUnscaledTime: true);
        
        if (radialMenuContainer.localPosition != elementalSwapPositions[0])
            Tween.LocalPosition(elementSwapContainer, elementalSwapPositions[0], 0.1f, useUnscaledTime: true);

        if (elementSwapBackground != null)
        {
            elementSwapBackground.gameObject.SetActive(true);
            Tween.Alpha(elementSwapBackground, 0, 0.1f, useUnscaledTime: true);
        }
        
        elementSelectBorder?.gameObject.SetActive(false);
        elementSelectLine?.gameObject.SetActive(false);
    }

    private void OnElementMenuUpdate(ElementMenuEvent e)
    {
        if (e.IsActive) ActivateElementSwapMenu();
        else DeactivateElementSwapMenu();

        SetRadialMenuLine(e.Direction);
        if (e.ElementEffect != null) SetRadialMenuIcon((ElementEffect) e.ElementEffect, e.ElementIndex);
    }
    
    private void ActivateElementSwapMenu()
    {
        if (radialMenuContainer == null) return;
        
        if (radialMenuContainer.localScale != elementSwapScales[1] * Vector3.one)
            Tween.Scale(radialMenuContainer, elementSwapScales[1], 0.1f, useUnscaledTime: true);
        if (radialMenuContainer.localPosition != elementalSwapPositions[1])
            Tween.LocalPosition(elementSwapContainer, elementalSwapPositions[1], 0.1f, useUnscaledTime: true);

        if (elementSwapBackground != null)
        {
            elementSwapBackground.gameObject.SetActive(true);
            Tween.Alpha(elementSwapBackground, elementSwapBackgroundAlpha, 0.1f, useUnscaledTime: true);
        }
        
        elementSelectBorder?.gameObject.SetActive(false);
        elementSelectLine?.gameObject.SetActive(false);

        selectedElementIndex = pc.pcc.CurrentElementIndex;
    }
    
    private void OnElementSelect(ElementUpdateEvent e)
    {
        if (elementSwapDial == null) return;
        
        elementSwapDial.RotateInDirection(e.Direction, e.ElementEffect);
        
        UpdateElementalAttackIcons();
    }

    private void SetRadialMenuLine(Vector2 direction)
    {
        if (direction == Vector2.zero)
        {
            elementSelectLine.gameObject.SetActive(false);
            return;
        }
        
        elementSelectLine.gameObject.SetActive(true);
        
        float angle = Vector2.SignedAngle(Vector2.up, direction);
        
        elementSelectLine.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void SetRadialMenuIcon(ElementEffect element, int index)
    {
        if (selectedElementIndex == index) return;
        
        for (int i = 0; i < elementIcons.Length; i++)
        {
            if (i == index)
            {
                elementIcons[i].color = Services.Get<ElementSystem>().GetElementData(element).elementColor;
                Tween.Scale(elementIconRects[i], radialMenuDefaultScale * radialMenuSelectScaleFactor, 0.1f, useUnscaledTime: true);
                
                if (elementSelectBorder != null)
                {
                    elementSelectBorder.gameObject.SetActive(true);
                    
                    elementSelectBorder.rotation = Quaternion.Euler(0, 0, elementIconRects[i].rotation.eulerAngles.z);
                    
                    elementSelectBorder.localScale = radialMenuDefaultScale;
                    Tween.Scale(elementSelectBorder, radialMenuDefaultScale * radialMenuSelectScaleFactor, 0.1f, useUnscaledTime: true);
                }
            }
            else
            {
                elementIcons[i].color = Services.Get<ElementSystem>().GetElementData(pc.rps.elementEffects[i]).elementInactiveColor;
                if (i == selectedElementIndex)
                    Tween.Scale(elementIconRects[i], radialMenuDefaultScale, 0.1f, useUnscaledTime: true);
            }
        }
        
        selectedElementIndex = index;
    }
    
    #endregion
    
    #region Main Slider Methods

    private void OnHealthUpdate(HealthUpdateEvent e)
    {
        float healthPercentage = e.healthPercentage;

        healthSliderBar.TweenSliderValue(healthPercentage, 0.2f, 1f, 0.02f);
    }
    
    private void OnChargeUpdate(ChargeUpdateEvent e)
    {
        float chargePercentage = e.chargePercentage;
        
        chargeSliderBar.TweenSliderValue(chargePercentage, 0.2f, 1f);
        
        SetElementalAttackCharge();
    }

    #endregion
    
    #region Elemental Attack Methods

    private void SetElementalAttackCharge()
    {
        northElementalAttack.SetSliderValueAnimated(pc.pcc.GetElementAttackChargePercentage(KeyBind.North), 0.1f);
        southElementalAttack.SetSliderValueAnimated(pc.pcc.GetElementAttackChargePercentage(KeyBind.South), 0.1f);
        westElementalAttack.SetSliderValueAnimated(pc.pcc.GetElementAttackChargePercentage(KeyBind.West), 0.1f);
    }

    private void RefreshElementalAttackIconStatus()
    {
        northElementalAttack.SwapCurrentElement(
            pc.pcc.currentElementEffect,
            CurrentElementalAttacks?.northAttack,
            pc.psm.movingState);
        
        southElementalAttack.SwapCurrentElement(
            pc.pcc.currentElementEffect,
            CurrentElementalAttacks?.southAttack,
            pc.psm.movingState);
        
        westElementalAttack.SwapCurrentElement(
            pc.pcc.currentElementEffect,
            CurrentElementalAttacks?.westAttack,
            pc.psm.movingState);
        
        SetElementalAttackCharge();
    }
    
    
    private void UpdateElementalAttackIcons()
    {
        if (elementalAttackContainer == null) return;
        
        elementalAttackContainer.gameObject.SetActive(true);
        elementalAttackContainer.gameObject.PulseOutIn(elementalAttackContainerScaleFactor, 0.2f, 0, 0.1f);
        
        RefreshElementalAttackIconStatus();
    }

    private void OnElementalAttackActivate(ElementAttackUpdateEvent e)
    {
        if (elementalAttackContainer == null) return;
        
        bool activate = e.IsActive;
        if (!activate)
            Tween.Scale(elementalAttackContainer, elementalAttackContainerScale, 0.2f);
        else
            Tween.Scale(elementalAttackContainer,
                elementalAttackContainerScale * elementalAttackContainerScaleFactor,
                0.2f);
    }
    
    #endregion
    
    #region Style Methods
    
    private void OnStyleUpdate(StyleUpdateEvent e)
    {
        styleMeterUI.UpdateStyleValue(e);
    }
    
    #endregion
    
    #region Lock On Methods
    
    private void OnCameraLockOnEvent(CameraLockOnEvent e)
    {
        currentLockOnTarget = e.Target;

        enemyEffectTileUI.SetTarget(currentLockOnTarget);

        if (currentLockOnTarget == null)
        {
            if (Mathf.Approximately(enemyUIGroup.alpha, 0f)) return;
            Tween.Alpha(enemyUIGroup, 0f, 0.2f).OnComplete(() =>
            {
                enemyNameText.text = "";
                enemyLevelText.text = "";
                enemyHealthBarCountText.text = "";
            });
        }
        else
        {
            enemyNameText.text = currentLockOnTarget.enemyName;
            enemyLevelText.text = $"Lv. {currentLockOnTarget.damageable.Level}";
            enemyHealthBarCountText.text = $"x{currentLockOnTarget.damageable.NumHealthBars}";
            
            float healthPercentage = currentLockOnTarget.damageable.Stats.GetInnateStat(InnateStat.MaxHealth) > 0 ? 
                currentLockOnTarget.damageable.CurrentHealth / 
                currentLockOnTarget.damageable.Stats.GetInnateStat(InnateStat.MaxHealth) : 0f;
            
            enemyHealthSliderBar.SetSliderValueInstant(healthPercentage);

            float shieldPercentage = currentLockOnTarget.damageable.DamageableComponents.TryGetComponent<ShieldComponent>(out var shield) 
                ? shield.GetDisplayShieldPercentage() : 0f;
            
            enemyShieldSliderBar.SetSliderValueInstant(shieldPercentage);
            
            ElementEffect currentElement = currentLockOnTarget.damageable.DamageableComponents.TryGetComponent<ElementComponent>(out var elementComponent) 
                ? elementComponent.CurrentElementEffect : ElementEffect.None;
            
            enemyShieldSliderBar.SetMainSliderColor(Services.Get<ElementSystem>().GetElementData(currentElement).elementColor);
            
            if (Mathf.Approximately(enemyUIGroup.alpha, 1f)) return;
            Tween.Alpha(enemyUIGroup, 1f, 0.2f);
        }
    }
    
    private void UpdateEnemyHealthUI()
    {
        if (currentLockOnTarget == null) return;
        
        enemyHealthBarCountText.text = $"x{currentLockOnTarget.damageable.NumHealthBars}";
        
        float healthPercentage = currentLockOnTarget.damageable.Stats.GetInnateStat(InnateStat.MaxHealth) > 0 ? 
                                currentLockOnTarget.damageable.CurrentHealth / 
                                currentLockOnTarget.damageable.Stats.GetInnateStat(InnateStat.MaxHealth) : 0f;
        
        enemyHealthSliderBar.TweenSliderValue(healthPercentage, 0.2f, 1f, 0.02f);
        
        float shieldPercentage = currentLockOnTarget.damageable.DamageableComponents.TryGetComponent<ShieldComponent>(out var shield) 
            ? shield.GetDisplayShieldPercentage() : 0f;
        
        enemyShieldSliderBar.TweenSliderValue(shieldPercentage, 0.2f);
    }
    
    #endregion
    
    #region State Swap Methods
    
    private void OnStateSwapEvent(StateSwapEvent e)
    {
        switch (e.NewMovingState)
        {
            case MovingStates.Katana:
                ultimateValueBar.SetActiveColor(true);
                break;
            case MovingStates.DualSword:
                ultimateValueBar.SetActiveColor(false);
                break;
            case MovingStates.NonCombat:
                // TODO: exited combat -> get rid of the battle ui
                break;
        }
    }
    
    #endregion

}

public struct HealthUpdateEvent : IEvent
{
    public float healthPercentage;
}

public struct ChargeUpdateEvent : IEvent
{
    public float chargePercentage;
}

public struct FinisherUpdateEvent : IEvent
{
    public float finisherPercentage;
}

public struct UltimateUpdateEvent : IEvent
{
    public float UltimatePercentage;
}

public struct ElementUpdateEvent : IEvent
{
    public int Direction;
    public ElementEffect ElementEffect;
}

public struct ElementAttackUpdateEvent : IEvent
{
    public AttacksByWeapon NorthAttack;
    public AttacksByWeapon SouthAttack;
    public AttacksByWeapon WestAttack;

    public bool IsActive;
}

public struct ElementMenuEvent : IEvent
{
    public ElementEffect? ElementEffect;
    public int ElementIndex;
    public Vector2 Direction;
    public bool IsActive;
}

public struct StyleUpdateEvent : IEvent
{
    public enum SwapDirection
    {
        None,
        Increased,
        Decreased
    }
    
    public StyleLevel StyleLevel;
    public float StyleValue;
    public SwapDirection Swapped;
    
    public StyleUpdateEvent(StyleLevel level, float value, SwapDirection swapped)
    {
        StyleLevel = level;
        StyleValue = value;
        Swapped = swapped;
    }
}

public struct CameraLockOnEvent : IEvent
{
    public bool IsLockedOn;
    public LockOnTarget Target;
    
    public CameraLockOnEvent(bool isLockedOn, LockOnTarget target)
    {
        IsLockedOn = isLockedOn;
        Target = target;
    }
}

public struct StateSwapEvent : IEvent
{
    public MovingStates NewMovingState;
    
    public StateSwapEvent(MovingStates newMovingState)
    {
        NewMovingState = newMovingState;
    }
}
