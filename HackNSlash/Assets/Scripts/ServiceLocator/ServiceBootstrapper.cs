using Systems;
using UnityEngine;

public class ServiceBootstrapper : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private PlayerController playerController;
    
    [Header("Systems")]
    [SerializeField] private VFXSystem vfxSystem;
    [SerializeField] private CombatSystem combatSystem;
    [SerializeField] private EffectSystem effectSystem;
    [SerializeField] private ElementSystem elementSystem;
    [SerializeField] private StyleSystem styleSystem;
    
    [Header("UI References")]
    [SerializeField] private OverworldMenuUI overworldMenuUI;
    [SerializeField] private HUDMenuUI hudMenuUI;
    
    private void Awake()
    {
        Services.RegisterPlayerController(playerController);
        Services.RegisterVFXSystem(vfxSystem);
        Services.RegisterCombatSystem(combatSystem);
        Services.RegisterEffectSystem(effectSystem);
        Services.RegisterElementSystem(elementSystem);
        Services.RegisterStyleSystem(styleSystem);
        Services.RegisterOverworldMenuUI(overworldMenuUI);
        Services.RegisterHUDMenuUI(hudMenuUI);
    }
}
