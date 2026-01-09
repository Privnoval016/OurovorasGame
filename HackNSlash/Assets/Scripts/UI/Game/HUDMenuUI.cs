using System;
using Extensions.EventBus;
using Extensions.UI;
using PrimeTween;
using Systems.Element;
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
    
    [Header("Finisher Sliders")]
    
    [SerializeField] private Slider finisherValueBar;
    [SerializeField] private RawImage finisherIcon;
    
    [Header("Elemental Swap")]
    
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
    
    [SerializeField] private ElementalAttackIcon northElementalAttack;
    [SerializeField] private ElementalAttackIcon southElementalAttack;
    [SerializeField] private ElementalAttackIcon westElementalAttack;
    
    [Header("Style Meter")]
    [SerializeField] private Slider styleMeterSlider;
    [SerializeField] private Image styleMeterBackground;
    [SerializeField] private Image styleMeterOutline;
    [SerializeField] private Image styleMeterFill;
    
    [Header("Enemy UI")]
    [SerializeField] private CanvasGroup enemyUIGroup;
    [SerializeField] private TMP_Text enemyNameText;
    [SerializeField] private TMP_Text enemyLevelText;
    [SerializeField] private SliderBar enemyHealthSliderBar;
    [SerializeField] private TMP_Text enemyHealthBarCountText;
    [SerializeField] private EffectTileUI enemyEffectTileUI;
    
    private LockOnTarget currentLockOnTarget;
    
    #endregion
    
    [HideInInspector] public EquippedElementAttack CurrentElementalAttacks => pc.pcc.GetCurrentElementAttack();
    [HideInInspector] public ElementEffect Element => pc.pcc.currentElementEffect;
    
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
    
    #endregion


    #region MonoBehaviour Callbacks

    public void Awake()
    {
        ActivateEventBindings();
        
        gameObject.SetActive(true);
        
        elementalAttackContainerScale = elementalAttackContainer.localScale;
    }

    private void Start()
    {
        UpdateElementalAttackIcons();
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
        
        float currentFinisher = finisherValueBar.value;
        finisherValueBar.value = currentFinisher;
        
        if (currentFinisher > finisherPercentage)
        {
            // Decrease finisher
            Tween.UISliderValue(finisherValueBar, finisherPercentage, 0.5f);
        }
        else if (currentFinisher < finisherPercentage)
        {
            // Increase finisher
            Tween.UISliderValue(finisherValueBar, finisherPercentage, 0.3f);
        }
    }
    
    /**
     * Needs to be called every frame because it changes depending on player position
     */
    private void UpdateFinisherIcon()
    {
        ElementData elementData = Services.Get<ElementSystem>().GetElementData(ElementEffect.Aether);
        
        if (pc.ps.CanUseFinisher())
        {
            // Enable finisher icon
            finisherIcon.color = elementData.elementColor;
        }
        else
        {
            // Disable finisher icon
            finisherIcon.color = elementData.elementInactiveColor;
        }
    }
    
    #endregion
    
    #region Element Swap Methods

    private void InitializeElementSwapMenu()
    {
        elementIconRects = new RectTransform[elementIcons.Length];
        for (int i = 0; i < elementIcons.Length; i++)
        {
            elementIconRects[i] = elementIcons[i].GetComponent<RectTransform>();
            ElementEffect element = pc.pcc.elementEffects[i];
            
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
        ElementEffect element = e.ElementEffect;
        
        if (selectedElementIcon == null) return;

        selectedElementIcon.color = Services.Get<ElementSystem>().GetElementData(element).elementColor;
        
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
                elementIcons[i].color = Services.Get<ElementSystem>().GetElementData(pc.pcc.elementEffects[i]).elementInactiveColor;
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
        
        chargeSliderBar.TweenSliderValue(chargePercentage, 0.02f, 1f);
        
        RefreshElementalAttackIconStatus();
    }

    #endregion
    
    #region Elemental Attack Methods

    private void RefreshElementalAttackIconStatus()
    {
        ValidateAttackIcon(northElementalAttack, CurrentElementalAttacks.northAttack, KeyBind.North);
        ValidateAttackIcon(southElementalAttack, CurrentElementalAttacks.southAttack, KeyBind.South);
        ValidateAttackIcon(westElementalAttack, CurrentElementalAttacks.westAttack, KeyBind.West);
    }
    
    
    private void UpdateElementalAttackIcons()
    {
        if (elementalAttackContainer == null) return;
        
        elementalAttackContainer.gameObject.SetActive(true);
        
        RefreshElementalAttackIconStatus();
    }
    
    private void ValidateAttackIcon(ElementalAttackIcon attackIcon, AttacksByWeapon a, KeyBind k)
    {
        if (attackIcon == null) return;
        
        if (a == null)
        {
            attackIcon.icon.gameObject.SetActive(false);
            attackIcon.chargeFillImage.gameObject.SetActive(false);
            return;
        }

        Attack attack = a.GetAttackByState(pc.psm.movingState);
        
        if (attack == null)
        {
            attackIcon.icon.gameObject.SetActive(false);
            attackIcon.chargeFillImage.gameObject.SetActive(false);
            return;
        }
        
        if (!Mathf.Approximately(pc.pcc.GetCooldownPercentage(k), attackIcon.chargeSlider.value))
            Tween.UISliderValue(attackIcon.chargeSlider, pc.pcc.GetCooldownPercentage(k), 0.03f);
        
        if (pc.pcc.FinishedElementCooldown(attack))
            EnableAttackIcon(attackIcon, attack);
        else
            DisableAttackIcon(attackIcon, attack);
    }
    
    private void EnableAttackIcon(ElementalAttackIcon attackIcon, Attack a)
    {
        // add icon to attack icon later
        
        attackIcon.icon.gameObject.SetActive(true);
        attackIcon.chargeFillImage.gameObject.SetActive(true);

        Color c = Services.Get<ElementSystem>().GetElementData(Element).elementColor;
        attackIcon.icon.color = c;
    }
    
    private void DisableAttackIcon(ElementalAttackIcon attackIcon, Attack a)
    {
        // add icon to attack icon later
        
        attackIcon.icon.gameObject.SetActive(true);
        attackIcon.chargeFillImage.gameObject.SetActive(true);

        Color c = Services.Get<ElementSystem>().GetElementData(Element).elementInactiveColor;
        attackIcon.icon.color = c;
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
        var setting = Services.Get<StyleSystem>().GetStyleSettings(e.StyleLevel);
        if (setting == null) return;
        
        if (styleMeterBackground == null || styleMeterOutline == null || styleMeterFill == null || styleMeterSlider == null)
            return;
        
        styleMeterBackground.sprite = setting.meterBackground != null ? setting.meterBackground : styleMeterBackground.sprite;
        styleMeterOutline.sprite = setting.meterOutline != null ? setting.meterOutline : styleMeterOutline.sprite;
        styleMeterFill.sprite = setting.meterFill != null ? setting.meterFill : styleMeterFill.sprite;
        
        float stylePercentage = Services.Get<StyleSystem>().GetStylePercentage(e.StyleLevel, e.StyleValue);
        if (Mathf.Approximately(styleMeterSlider.value, stylePercentage)) return;
        
        Tween.UISliderValue(styleMeterSlider, stylePercentage, 0.2f);
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
    }
    
    #endregion

    [Serializable]
    public class ElementalAttackIcon
    {
        public RawImage icon;
        public Slider chargeSlider;
        public Image chargeFillImage;
    }

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
    public StyleLevel StyleLevel;
    public float StyleValue;
    
    public StyleUpdateEvent(StyleLevel level, float value)
    {
        StyleLevel = level;
        StyleValue = value;
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
