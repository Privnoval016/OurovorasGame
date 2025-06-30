using System;
using Extensions.UI;
using Extensions.Utils;
using PrimeTween;
using UnityEngine;
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
    
    [Header("Ultimate Sliders")]
    public Slider ultimateValueBar;
    
    [Header("Elemental Swap")]
    
    public ScrollMenu elementalSwapMenu;
    public RawImage[] elementalSwapIcons;
    private RectTransform[] elementalSwapIconRects;
    public float[] elementalSwapIconScales = { 0.4f, 0.6f, 1f, 0.6f, 0.4f };
    
    
    [Header("Elemental Attacks")]
    public RectTransform elementalAttackContainer;
    private Vector3 elementalAttackContainerScale;
    public float elementalAttackContainerScaleFactor = 1.3f;
    
    public RawImage northElementalAttack;
    public RawImage southElementalAttack;
    public RawImage eastElementalAttack;
    public RawImage westElementalAttack;
    
    [HideInInspector] public EquippedElementAttack CurrentElementalAttacks => pc.pi.GetCurrentElementAttack();
    [HideInInspector] public ElementEffect Element => pc.pi.CurrentElementEffect;


    #region MonoBehaviour Callbacks

    protected override void Awake()
    {
        base.Awake();
        
        elementalAttackContainerScale = elementalAttackContainer.localScale;
    }

    private void Start()
    {
        UpdateElementalAttackIcons();
        InitializeElementalSwapMenu();
    }

    private void Update()
    {
        
    }

    #endregion
    
    #region Element Swap Methods
    
    private void InitializeElementalSwapMenu()
    {
        elementalSwapIconRects = new RectTransform[elementalSwapIcons.Length];
        
        for (int i = 0; i < elementalSwapIcons.Length; i++)
        {
            elementalSwapIconRects[i] = elementalSwapIcons[i].GetComponent<RectTransform>();
            SetElementSwapIcons(i);
        }
        ScrollElementsLeft();
    }

    private void SetElementSwapIcons(int index)
    {
        ElementData elementData = GameManager.GetElementData(pc.pi.elementEffectOrder[index]);
        if (elementalSwapIcons == null || elementalSwapIcons.Length < index + 1) return;
        
        elementalSwapIcons[index].color = elementData.elementColor;
    }
    
    public void ScrollElementsLeft()
    {
        if (elementalSwapMenu == null) return;

        SetElementSizes();
        elementalSwapMenu.ScrollLeft();
    }
    
    public void ScrollElementsRight()
    {
        if (elementalSwapMenu == null) return;
        
        SetElementSizes();
        elementalSwapMenu.ScrollRight();
    }
    
    private void SetElementSizes()
    {
        if (elementalSwapIcons == null || elementalSwapIconScales.Length != 5) return;
        

        for (int i = 0; i < 5; i++)
        {
            int index = pc.pi.elementEffectOrder.IndexOf(Element);
            index = pc.pi.elementEffectOrder.ShiftedIndex(index, i - 2);
            Tween.Scale(elementalSwapIconRects[index], elementalSwapIconScales[i], 0.2f);
        }
    }
    
    #endregion
    
    #region Slider Methods

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
        ValidateAttackIcon(northElementalAttack, CurrentElementalAttacks.northAttack);
        ValidateAttackIcon(southElementalAttack, CurrentElementalAttacks.southAttack);
        ValidateAttackIcon(eastElementalAttack, CurrentElementalAttacks.eastAttack);
        ValidateAttackIcon(westElementalAttack, CurrentElementalAttacks.westAttack);
    }
    
    
    public void UpdateElementalAttackIcons()
    {
        if (elementalAttackContainer == null) return;
        
        elementalAttackContainer.gameObject.SetActive(true);
        
        RefreshElementalAttackIconStatus();
    }
    
    private void ValidateAttackIcon(RawImage attackIcon, AttacksByWeapon a)
    {
        if (attackIcon == null) return;
        
        if (a == null)
        {
            attackIcon.gameObject.SetActive(false);
            return;
        }

        Attack attack = a.GetAttackByState(pc.psm.movingState);
        
        if (attack == null)
        {
            attackIcon.gameObject.SetActive(false);
            return;
        }
        
        if (attack.HasEnoughCharge(pc))
            EnableAttackIcon(attackIcon, attack);
        else
            DisableAttackIcon(attackIcon, attack);
    }
    
    private void EnableAttackIcon(RawImage attackIcon, Attack a)
    {
        // add icon to attack icon later
        
        attackIcon.gameObject.SetActive(true);

        Color c = GameManager.GetElementData(Element).elementColor;
        attackIcon.color = c;
    }
    
    private void DisableAttackIcon(RawImage attackIcon, Attack a)
    {
        // add icon to attack icon later
        
        attackIcon.gameObject.SetActive(true);

        Color c = GameManager.GetElementData(Element).elementInactiveColor;
        attackIcon.color = c;
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

}


