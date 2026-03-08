using System;
using PrimeTween;
using UnityEngine;

/** <summary>
 * <see cref="IElementChangeable"/> implementation for the crystal shader.
 * Creates a per-instance <see cref="Material"/> copy and smoothly tweens every
 * relevant crystal color property whenever the element changes.
 * </summary>
 */
[Serializable]
public class CrystalElementChangeable : IElementChangeable
{
    // ── Inspector ────────────────────────────────────────────────────────
    [Header("Components")]
    public Material crystalMaterial;
    public Renderer[] meshRenderers;

    [Header("Tween")]
    [Tooltip("Duration in seconds for the colour transition on element change.")]
    public float tweenDuration = 0.25f;

    // ── Shader property IDs — cached once to avoid per-frame string lookups ──
    private static readonly int PropBaseColor       = Shader.PropertyToID("_BaseColor");
    private static readonly int PropBaseColorDark   = Shader.PropertyToID("_BaseColorDark");
    private static readonly int PropRimColor        = Shader.PropertyToID("_RimColor");
    private static readonly int PropSubsurfaceColor = Shader.PropertyToID("_SubsurfaceColor");
    private static readonly int PropSparkleColor    = Shader.PropertyToID("_SparkleColor");
    private static readonly int PropEmissionColor   = Shader.PropertyToID("_EmissionColor");

    // ── Runtime ──────────────────────────────────────────────────────────
    private Material _instance;

    /** <summary>Creates a per-instance material copy and assigns it to all renderers.</summary> */
    public override void Initialize()
    {
        if (crystalMaterial == null)
        {
            Debug.LogError("CrystalElementChangeable: Crystal material is not assigned.");
            return;
        }

        _instance = new Material(crystalMaterial);

        foreach (var mr in meshRenderers)
        {
            if (mr == null)
            {
                Debug.LogWarning("CrystalElementChangeable: A MeshRenderer reference is null — skipping.");
                continue;
            }
            mr.material = _instance;
        }
    }

    /** <summary>Tweens all crystal shader colour properties to the values defined on the element's <see cref="ElementData"/>.</summary> */
    public override void UpdateElement(ElementEffect newElement)
    {
        if (_instance == null) return;

        ElementData data = Services.Get<ElementSystem>().GetElementData(newElement);
        if (data == null) return;

        Tween.MaterialColor(_instance, PropBaseColor,       data.crystalBaseColor,       tweenDuration);
        Tween.MaterialColor(_instance, PropBaseColorDark,   data.crystalBaseColorDark,   tweenDuration);
        Tween.MaterialColor(_instance, PropRimColor,        data.crystalRimColor,        tweenDuration);
        Tween.MaterialColor(_instance, PropSubsurfaceColor, data.crystalSubsurfaceColor, tweenDuration);
        Tween.MaterialColor(_instance, PropSparkleColor,    data.crystalSparkleColor,    tweenDuration);
        Tween.MaterialColor(_instance, PropEmissionColor,   data.crystalEmissionColor,   tweenDuration);
    }

    /** <summary>Destroys the instanced material to prevent a memory leak.</summary> */
    public override void Cleanup()
    {
        if (_instance != null)
            UnityEngine.Object.Destroy(_instance);
    }
}