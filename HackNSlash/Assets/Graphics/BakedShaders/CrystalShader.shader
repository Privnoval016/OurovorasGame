// CrystalShader — URP Unlit Transparent
//
// Philosophy: A real diamond/crystal reads as bright because of sharp specular
// highlights and sparkle, not because it is uniformly luminous. This shader
// focuses on that — each geometric face catches light differently, point
// sparkles flash on and off at facet corners, and a thin subsurface tint
// gives depth. The magical glow is kept subtle: a gentle edge rim and a slow
// pulsing emission underneath the clear effect.
//
// All geometry-dependent effects are driven by the world-space normal (per-face
// shading) or object-space position (sparkles) so they work on any mesh —
// diamond, ring, shard, sphere — regardless of UV layout.
//
// Effects:
//   1. Per-face tint  — each flat face gets a unique luminance from its normal hash
//   2. Subsurface depth — inner glow brightest where view is perpendicular (face-on)
//   3. Fresnel rim     — edge glow, controllable width and colour
//   4. Dispersion      — narrow prismatic RGB split at the very edge of the rim
//   5. Sparkles        — per-cell object-space flashes that twinkle independently
//   6. Specular rays   — two offset Blinn-Phong lobes that create a star-burst hotspot
//   7. Emission pulse  — slow sine-wave heartbeat on the magic glow colour

Shader "Custom/CrystalShader"
{
    Properties
    {
        [Header(Base Crystal)]
        _BaseColor          ("Base Colour (transparent)",   Color)          = (0.25, 0.55, 1.0, 0.35)
        _BaseColorDark      ("Base Colour (shadow tint)",   Color)          = (0.04, 0.08, 0.3, 0.35)
        _BaseBlendSpeed     ("Tint Drift Speed",            Range(0,0.3))   = 0.025

        [Header(Per Face Luminance)]
        // Each geometric face gets a unique brightness derived from its normal — no circular rings.
        _FacetContrast      ("Face Contrast",               Range(1,6))     = 2.5
        _FacetDark          ("Darkest Face Brightness",     Range(0,1))     = 0.35

        [Header(Subsurface Depth)]
        _SubsurfaceColor    ("Subsurface Colour",           Color)          = (0.15, 0.45, 1.0, 1.0)
        _SubsurfaceIntensity("Subsurface Intensity",        Range(0,2))     = 0.55
        _SubsurfacePower    ("Subsurface Falloff",          Range(0.5,6))   = 1.8

        [Header(Fresnel Rim Glow)]
        _RimColor           ("Rim Colour",                  Color)          = (0.5, 0.85, 1.0, 1.0)
        _RimPower           ("Rim Falloff",                 Range(0.5,8))   = 3.5
        _RimIntensity       ("Rim Intensity",               Range(0,5))     = 1.8

        [Header(Prism Dispersion)]
        // Rainbow split at the very edge of the rim — simulates light refracting.
        _DispersionIntensity("Dispersion Intensity",        Range(0,2))     = 0.6
        _DispersionBand     ("Dispersion Band Width",       Range(0.01,0.4))= 0.12

        [Header(Sparkles)]
        // Independent per-cell object-space flash: each cell has its own phase.
        _SparkleColor       ("Sparkle Colour",              Color)          = (1.0, 1.0, 1.0, 1.0)
        _SparkleIntensity   ("Sparkle Intensity",           Range(0,10))    = 5.0
        _SparkleScale       ("Sparkle Cell Size",           Range(2,40))    = 18.0
        _SparkleSpeed       ("Sparkle Twinkle Speed",       Range(0,6))     = 2.5
        _SparkleThreshold   ("Sparkle Threshold",           Range(0.6,0.99))= 0.82
        _SparkleSharpness   ("Sparkle Sharpness",           Range(0.001,0.1))= 0.018

        [Header(Specular Star Burst)]
        // Two anisotropic lobes rotated 45 degrees — produces a 4-point star highlight.
        _SpecColor2         ("Specular Colour",             Color)          = (1.0, 1.0, 1.0, 1.0)
        _SpecPowerSharp     ("Tight Lobe Power",            Range(16,1024)) = 256.0
        _SpecPowerWide      ("Wide Lobe Power",             Range(4,128))   = 32.0
        _SpecIntensitySharp ("Tight Lobe Intensity",        Range(0,6))     = 3.0
        _SpecIntensityWide  ("Wide Lobe Intensity",         Range(0,3))     = 0.8
        _LightDir           ("Light Direction (world)",     Vector)         = (0.45, 0.9, 0.2, 0.0)

        [Header(Emission Pulse)]
        _EmissionColor      ("Emission Colour",             Color)          = (0.3, 0.65, 1.0, 1.0)
        _EmissionIntensity  ("Emission Intensity",          Range(0,4))     = 0.7
        _PulseSpeed         ("Pulse Speed",                 Range(0,6))     = 1.2
        _PulseMin           ("Pulse Min",                   Range(0,1))     = 0.2
        _PulseMax           ("Pulse Max",                   Range(0,2))     = 1.0

        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull Mode", Float) = 0
        _ZWrite ("ZWrite", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
        }

        Pass
        {
            Name "CrystalForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite [_ZWrite]
            Cull   [_Cull]

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4  _BaseColor;
                half4  _BaseColorDark;
                half   _BaseBlendSpeed;

                half   _FacetContrast;
                half   _FacetDark;

                half4  _SubsurfaceColor;
                half   _SubsurfaceIntensity;
                half   _SubsurfacePower;

                half4  _RimColor;
                half   _RimPower;
                half   _RimIntensity;

                half   _DispersionIntensity;
                half   _DispersionBand;

                half4  _SparkleColor;
                half   _SparkleIntensity;
                half   _SparkleScale;
                half   _SparkleSpeed;
                half   _SparkleThreshold;
                half   _SparkleSharpness;

                half4  _SpecColor2;
                half   _SpecPowerSharp;
                half   _SpecPowerWide;
                half   _SpecIntensitySharp;
                half   _SpecIntensityWide;
                float4 _LightDir;

                half4  _EmissionColor;
                half   _EmissionIntensity;
                half   _PulseSpeed;
                half   _PulseMin;
                half   _PulseMax;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 positionOS : TEXCOORD1;  // object-space for sparkles
                float3 normalWS   : TEXCOORD2;
                float3 viewDirWS  : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ──────────────────────────────────────────────────────────────
            // Hash — maps any float3 lattice coordinate to a stable [0,1] value.
            // Used for per-face luminance and sparkle cell IDs.
            // ──────────────────────────────────────────────────────────────
            float hash(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            // Smooth 3-D value noise — C2 continuous, used only for the slow
            // tint-blend drift so it is called once with a very coarse scale.
            float valueNoise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(lerp(hash(i),              hash(i+float3(1,0,0)), u.x),
                         lerp(hash(i+float3(0,1,0)),hash(i+float3(1,1,0)),u.x), u.y),
                    lerp(lerp(hash(i+float3(0,0,1)),hash(i+float3(1,0,1)),u.x),
                         lerp(hash(i+float3(0,1,1)),hash(i+float3(1,1,1)),u.x), u.y),
                    u.z);
            }

            // ──────────────────────────────────────────────────────────────
            // Hue rotation — used for the 3-channel dispersion split.
            // Rotates RGB by `angle` radians in hue space.
            // ──────────────────────────────────────────────────────────────
            half3 hueRotate(half3 col, float a)
            {
                float c = cos(a), s = sin(a);
                float k = (1.0 - c) / 3.0;
                return half3(
                    col.r*(c+k) + col.g*(k-s*0.5774) + col.b*k,
                    col.r*(k+s*0.5774) + col.g*(c+k) + col.b*(k-s*0.5774),
                    col.r*k + col.g*(k+s*0.5774) + col.b*(c+k)
                );
            }

            Varyings vert(Attributes IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Varyings OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   vni = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = vpi.positionCS;
                OUT.positionWS = vpi.positionWS;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.normalWS   = vni.normalWS;
                OUT.viewDirWS  = normalize(GetCameraPositionWS() - vpi.positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 N   = normalize(IN.normalWS);
                float3 V   = normalize(IN.viewDirWS);
                float3 L   = normalize(_LightDir.xyz);
                float  t   = _Time.y;
                float  NdotV = saturate(dot(N, V));

                // ── 1. PER-FACE LUMINANCE ─────────────────────────────────
                // Quantise the world normal to coarse steps so every fragment
                // on the same flat face maps to the same hash bucket → each face
                // gets a unique, stable brightness. Neighbouring faces get clearly
                // different values, just like a cut diamond's facets.
                //
                // Works correctly on rings, shards, and spheres too: curved
                // surfaces get a smooth gradient of face hashes instead of bands.
                float3 quantN  = round(N * 14.0) / 14.0;
                float  faceID  = hash(quantN * 9.1 + 3.7);
                // Map faceID to [_FacetDark, 1] then sharpen the contrast curve.
                float  faceLum = pow(lerp(_FacetDark, 1.0, faceID), _FacetContrast);

                // Slow tint drift gives internal colour variation without UVs.
                float  drift   = valueNoise(IN.positionWS * 0.8 + t * _BaseBlendSpeed);
                half4  baseCol = lerp(_BaseColorDark, _BaseColor, drift);
                baseCol.rgb   *= faceLum;

                // ── 2. SUBSURFACE DEPTH ───────────────────────────────────
                // Brightest when the surface faces the camera directly (NdotV high),
                // simulating light scattering through the crystal's interior.
                // Kept soft (_SubsurfacePower < 2) so it reads as interior glow,
                // not as another view-dependent ring.
                float  sssMask = pow(NdotV, _SubsurfacePower);
                half3  sss     = _SubsurfaceColor.rgb * sssMask * _SubsurfaceIntensity;

                // ── 3. FRESNEL RIM ────────────────────────────────────────
                float  fresnel = pow(1.0 - NdotV, _RimPower);
                half3  rim     = _RimColor.rgb * fresnel * _RimIntensity;

                // ── 4. PRISM DISPERSION ───────────────────────────────────
                // A very narrow Fresnel mask (inner edge of the rim only) is
                // sampled at three slightly different angular widths, each
                // hue-rotated by 60°. This creates a thin RGB rainbow fringe
                // at the very silhouette edge — like light splitting through
                // a glass prism.
                float  fresnelTight = pow(1.0 - NdotV, _RimPower * 1.8);
                float  dMask        = saturate(fresnelTight / max(_DispersionBand, 0.001));
                half3  disp         = (hueRotate(_RimColor.rgb, -1.05) * dMask
                                     + hueRotate(_RimColor.rgb,  0.0)  * dMask * 0.7
                                     + hueRotate(_RimColor.rgb,  1.05) * dMask)
                                     * _DispersionIntensity;

                // ── 5. SPARKLES ───────────────────────────────────────────
                // Object-space lattice: every cell has a stable random ID and an
                // independent time phase so cells flash without synchronising.
                //
                // Object-space means sparkles stay fixed to the gem's surface even
                // as it rotates — they feel like real facet-corner light catches.
                //
                // Smoothstep around the threshold creates a sharp but anti-aliased
                // point flash. _SparkleSharpness controls how needle-sharp it is.
                float3 spkCell  = floor(IN.positionOS * _SparkleScale);
                float  spkID    = hash(spkCell);
                float  spkPhase = sin(t * _SparkleSpeed + spkID * 6.28318) * 0.5 + 0.5;
                float  spkVal   = spkID * spkPhase;
                float  sparkle  = smoothstep(_SparkleThreshold, _SparkleThreshold + _SparkleSharpness, spkVal);
                half3  sparks   = _SparkleColor.rgb * sparkle * _SparkleIntensity;

                // ── 6. SPECULAR STAR BURST ────────────────────────────────
                // Two Blinn-Phong lobes: one very tight (mimics a needle-sharp
                // facet reflection), one wider (soft halo around it).  Using two
                // lobes together creates the "star" that real diamonds produce
                // when catching a point light source.
                float3 H         = normalize(L + V);
                float  NdotH     = saturate(dot(N, H));
                float  specSharp = pow(NdotH, _SpecPowerSharp) * _SpecIntensitySharp;
                float  specWide  = pow(NdotH, _SpecPowerWide)  * _SpecIntensityWide;
                half3  specular  = _SpecColor2.rgb * (specSharp + specWide);

                // ── 7. EMISSION PULSE ─────────────────────────────────────
                // Gentle sine heartbeat — the magic tint underneath the clear
                // crystal effect. Kept at low intensity so it doesn't overpower
                // the sharp specular and sparkle that sell the crystal look.
                float pulse    = lerp(_PulseMin, _PulseMax,
                                      saturate(sin(t * _PulseSpeed) * 0.5 + 0.5));
                half3 emission = _EmissionColor.rgb * _EmissionIntensity * pulse;

                // ── COMPOSITE ─────────────────────────────────────────────
                half3 col = baseCol.rgb + sss + rim + disp + sparks + specular + emission;

                // Alpha: base transparency + Fresnel thickens the silhouette edge
                // so the crystal reads as solid glass at the rim.
                half  alpha = saturate(baseCol.a + fresnel * 0.4);

                return half4(col, alpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex   vert_d
            #pragma fragment frag_d
            #pragma target   3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attr { float4 p : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Vary { float4 p : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };

            Vary vert_d(Attr IN)
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                Vary OUT;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.p = TransformObjectToHClip(IN.p.xyz);
                return OUT;
            }
            half4 frag_d(Vary IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}