using System;
using Extensions.UI;
using Extensions.Utils;
using PrimeTween;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class HUDMenuUI : Singleton<HUDMenuUI>
{
    public PlayerController pc;
    
    [Header("Health Sliders")]
    public Slider healthValueBar;
    public Slider healthDepleteBar;
    public Slider healthRestoreBar;
    
    
    [Header("Charge Sliders")]
    
    public Slider chargeValueBar;
    public Slider chargeDepleteBar;
    
    public RectTransform[] minChargeIndicators;
    
    [Header("Ultimate Sliders")]
    public Slider ultimateValueBar;
    public RawImage ultimateIcon;
    
    [Header("Finisher Sliders")]
    
    public Slider finisherValueBar;
    public RawImage finisherIcon;
    
    [Header("Elemental Swap")]
    
    public RectTransform elementSwapContainer;
    
    public RectTransform radialMenuContainer;
    
    public RawImage elementSwapBackground;
    
    public RawImage selectedElementIcon;
    public RawImage[] elementIcons;
    private RectTransform[] elementIconRects;

    public RectTransform elementSelectBorder;
    public RectTransform elementSelectLine;
    
    [Space(20)]
    public float[] elementSwapScales = new float[] { 0.01f, 1.0f };
    public Vector3[] elementalSwapPositions;
    public float radialMenuSelectScaleFactor = 1.2f;
    private Vector3 radialMenuDefaultScale;
    private int selectedElementIndex;
    public float elementSwapBackgroundAlpha = 0.3f;
    
    
    [Header("Elemental Attacks")]
    public RectTransform elementalAttackContainer;
    private Vector3 elementalAttackContainerScale;
    public float elementalAttackContainerScaleFactor = 1.3f;
    
    public ElementalAttackIcon northElementalAttack;
    public ElementalAttackIcon southElementalAttack;
    public ElementalAttackIcon westElementalAttack;
    
    [HideInInspector] public EquippedElementAttack CurrentElementalAttacks => pc.pi.GetCurrentElementAttack();
    [HideInInspector] public ElementEffect Element => pc.pi.currentElementEffect;


    #region MonoBehaviour Callbacks

    protected override void Awake()
    {
        base.Awake();
        
        elementalAttackContainerScale = elementalAttackContainer.localScale;
    }

    private void Start()
    {
        UpdateElementalAttackIcons();
        InitializeElementSwapMenu();
    }

    private void Update()
    {
        UpdateFinisherIcon();
    }

    #endregion
    
    #region Other Slider Methods
    
    public void UpdateUltimate(float ultimatePercentage)
    {
        float currentUltimate = ultimateValueBar.value;
        
        ultimateValueBar.value = currentUltimate;
        
        if (currentUltimate > ultimatePercentage)
        {
            // Decrease ultimate
            ultimateValueBar.value = ultimatePercentage;
        }
        else if (currentUltimate < ultimatePercentage)
        {
            // Increase ultimate
            Tween.UISliderValue(ultimateValueBar, ultimatePercentage, 0.3f);
        }
    }
    
    public void UpdateFinisher(float finisherPercentage)
    {
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
    
    private void UpdateFinisherIcon()
    {
        ElementData elementData = GameManager.GetElementData(ElementEffect.Aether);
        
        if (pc.pi.CanUseFinisher())
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
            ElementEffect element = pc.pi.elementEffects[i];
            
            // Set icon sprite later
            elementIcons[i].color = GameManager.GetElementData(element).elementInactiveColor;
        }
        
        radialMenuDefaultScale = elementIconRects[0].localScale;
        
        DeactivateElementSwapMenu();
    }
    
    public void DeactivateElementSwapMenu()
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
    
    public void ActivateElementSwapMenu()
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

        selectedElementIndex = pc.pi.CurrentElementIndex;
    }
    
    public void SetSelectedElementIcon(ElementEffect element)
    {
        if (selectedElementIcon == null) return;

        selectedElementIcon.color = GameManager.GetElementData(element).elementColor;
    }

    public void SetRadialMenuLine(Vector2 direction)
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

    public void SetRadialMenuIcon(ElementEffect element, int index)
    {
        if (selectedElementIndex == index) return;
        
        for (int i = 0; i < elementIcons.Length; i++)
        {
            if (i == index)
            {
                elementIcons[i].color = GameManager.GetElementData(element).elementColor;
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
                elementIcons[i].color = GameManager.GetElementData(pc.pi.elementEffects[i]).elementInactiveColor;
                if (i == selectedElementIndex)
                    Tween.Scale(elementIconRects[i], radialMenuDefaultScale, 0.1f, useUnscaledTime: true);
            }
        }
        
        selectedElementIndex = index;
    }
    
    #endregion
    
    #region Main Slider Methods

    public void UpdateHealth(float healthPercentage)
    {
        float currentHealth = healthValueBar.value;
        
        healthValueBar.value = currentHealth;
        healthDepleteBar.value = currentHealth;
        healthRestoreBar.value = currentHealth;
        
        if (currentHealth > healthPercentage)
        {
            // Decrease health
            healthRestoreBar.value = healthPercentage;
            Tween.UISliderValue(healthValueBar, healthPercentage, 0.02f);
            Tween.UISliderValue(healthDepleteBar, healthPercentage, 1f);
        }
        else if (currentHealth < healthPercentage)
        {
            // Increase health
            Tween.UISliderValue(healthRestoreBar, healthPercentage, 0.02f);
            Tween.UISliderValue(healthValueBar, healthPercentage, 0.3f);
        }
    }
    
    public void UpdateCharge(float chargePercentage)
    {
        float currentCharge = chargeValueBar.value;
        
        chargeValueBar.value = currentCharge;
        chargeDepleteBar.value = currentCharge;
        
        if (currentCharge > chargePercentage)
        {
            // Decrease charge
            Tween.UISliderValue(chargeValueBar, chargePercentage, 0.02f);
            Tween.UISliderValue(chargeDepleteBar, chargePercentage, 1f);
        }
        else if (currentCharge < chargePercentage)
        {
            // Increase charge
            Tween.UISliderValue(chargeValueBar, chargePercentage, 0.3f);
        }
        
        RefreshElementalAttackIconStatus();
    }
    
    public void SetCharge(float chargePercentage)
    {
        chargeValueBar.value = chargePercentage;
        chargeDepleteBar.value = chargePercentage;
        
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
    
    
    public void UpdateElementalAttackIcons()
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
        
        if (!Mathf.Approximately(pc.pi.GetCooldownPercentage(k), attackIcon.chargeSlider.value))
            Tween.UISliderValue(attackIcon.chargeSlider, pc.pi.GetCooldownPercentage(k), 0.03f);
        
        if (pc.pi.FinishedElementCooldown(attack))
            EnableAttackIcon(attackIcon, attack);
        else
            DisableAttackIcon(attackIcon, attack);
    }
    
    private void EnableAttackIcon(ElementalAttackIcon attackIcon, Attack a)
    {
        // add icon to attack icon later
        
        attackIcon.icon.gameObject.SetActive(true);
        attackIcon.chargeFillImage.gameObject.SetActive(true);

        Color c = GameManager.GetElementData(Element).elementColor;
        attackIcon.icon.color = c;
    }
    
    private void DisableAttackIcon(ElementalAttackIcon attackIcon, Attack a)
    {
        // add icon to attack icon later
        
        attackIcon.icon.gameObject.SetActive(true);
        attackIcon.chargeFillImage.gameObject.SetActive(true);

        Color c = GameManager.GetElementData(Element).elementInactiveColor;
        attackIcon.icon.color = c;
    }

    public void ActivateElementalAttackIcons()
    {
        if (elementalAttackContainer == null) return;

        Tween.Scale(elementalAttackContainer,
            elementalAttackContainerScale * elementalAttackContainerScaleFactor,
            0.2f);
    }
    
    public void DeactivateElementalAttackIcons()
    {
        if (elementalAttackContainer == null) return;

        Tween.Scale(elementalAttackContainer, elementalAttackContainerScale, 0.2f);
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


