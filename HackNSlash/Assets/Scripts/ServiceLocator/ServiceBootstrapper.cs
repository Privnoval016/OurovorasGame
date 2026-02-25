using Systems;
using UnityEngine;

public class ServiceBootstrapper : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private PlayerController playerController;
    
    [Header("Systems")]
    [SerializeField] private AudioSystem audioSystem;
    [SerializeField] private VFXSystem vfxSystem;
    [SerializeField] private CombatSystem combatSystem;
    [SerializeField] private EffectSystem effectSystem;
    [SerializeField] private ElementSystem elementSystem;
    [SerializeField] private StyleSystem styleSystem;
    [SerializeField] private DamageSystem damageSystem;
    
    [Header("UI References")]
    [SerializeField] private OverworldMenuUI overworldMenuUI;
    [SerializeField] private HUDMenuUI hudMenuUI;
    
    private void Awake()
    {
        
        Services.Register(playerController);
        Services.Register(vfxSystem);
        Services.Register(combatSystem);
        Services.Register(effectSystem);
        Services.Register(elementSystem);
        Services.Register(styleSystem);
        Services.Register(audioSystem);
        Services.Register(damageSystem);
        Services.Register(overworldMenuUI);
        Services.Register(hudMenuUI);
    }
}
