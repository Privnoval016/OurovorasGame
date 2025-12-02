using Systems;
using UnityEngine;

public static class Services
{
    #region Global Services
    public static PlayerController PlayerController { get; private set; }
    public static ICombatSystem CombatSystem { get; private set; }
    public static IEffectSystem EffectSystem { get; private set; }
    
    public static IElementSystem ElementSystem { get; private set; }
    
    public static VFXSystem VFXSystem { get; private set; }
    
    public static StyleSystem StyleSystem { get; private set; }
    
    #endregion
    
    #region UI Services

    public static OverworldMenuUI OverworldMenuUI { get; private set; }
    
    public static HUDMenuUI HUDMenuUI { get; private set; }

    #endregion
    
    #region Global Service Registration Methods

    public static void RegisterPlayerController(PlayerController playerController) =>
        PlayerController = playerController;
    
    public static void RegisterCombatSystem(ICombatSystem combatSystem) =>
        CombatSystem = combatSystem;
    
    public static void RegisterEffectSystem(IEffectSystem effectSystem) =>
        EffectSystem = effectSystem;
    
    public static void RegisterElementSystem(IElementSystem elementSystem) =>
        ElementSystem = elementSystem;
    
    public static void RegisterVFXSystem(VFXSystem vfxSystem) =>
        VFXSystem = vfxSystem;
    
    public static void RegisterStyleSystem(StyleSystem styleSystem) =>
        StyleSystem = styleSystem;
    
    #endregion
    
    #region UI Service Registration Methods
    
    public static void RegisterOverworldMenuUI(OverworldMenuUI overworldMenuUI) =>
        OverworldMenuUI = overworldMenuUI;
    
    public static void RegisterHUDMenuUI(HUDMenuUI hudMenuUI) =>
        HUDMenuUI = hudMenuUI;
    
    #endregion
}