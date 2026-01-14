Shader "UI/SplotchyFadeout"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Fade Settings)]
        _FadeAmount ("Fade Amount", Range(0, 1)) = 0
        
        [Header(Splotch Appearance)]
        _SplotchScale ("Splotch Size", Range(0.5, 10)) = 3
        _EdgeSoftness ("Edge Softness", Range(0, 1)) = 0.15
        _NoiseSpeed ("Animation Speed", Range(0, 2)) = 0
        
        [Header(Advanced)]
        _NoiseScale2 ("Secondary Noise Scale", Range(0.5, 10)) = 5
        _NoiseMix ("Noise Layer Mix", Range(0, 1)) = 0.5
        
        // Required for UI
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }
        
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 worldPosition : TEXCOORD1;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            
            float _FadeAmount;
            float _SplotchScale;
            float _EdgeSoftness;
            float _NoiseSpeed;
            float _NoiseScale2;
            float _NoiseMix;
            
            // Simple 2D noise function
            float hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }
            
            // Smooth noise
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f); // Smoothstep
                
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            // Fractal noise with multiple octaves for more organic look
            float fractalNoise(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                
                for(int i = 0; i < 3; i++)
                {
                    value += amplitude * noise(p);
                    p *= 2.0;
                    amplitude *= 0.5;
                }
                
                return value;
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // Sample the texture
                fixed4 color = (tex2D(_MainTex, i.uv) + _TextureSampleAdd) * i.color;
                
                // Calculate animated UV coordinates
                float2 noiseUV = i.uv * _SplotchScale;
                float timeOffset = _Time.y * _NoiseSpeed;
                
                // Generate layered noise for organic splotches
                float noise1 = fractalNoise(noiseUV + float2(timeOffset, timeOffset * 0.7));
                float noise2 = fractalNoise(noiseUV * (_NoiseScale2 / _SplotchScale) + float2(timeOffset * -0.5, timeOffset * 0.3));
                
                // Combine noise layers
                float combinedNoise = lerp(noise1, noise2, _NoiseMix);
                
                // Create the fade threshold with softness
                float fadeThreshold = _FadeAmount;
                float alpha = smoothstep(
                    fadeThreshold - _EdgeSoftness,
                    fadeThreshold + _EdgeSoftness,
                    combinedNoise
                );
                
                // Apply the splotchy fade to alpha
                color.a *= alpha;
                
                // Apply UI clipping
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                
                // Discard fully transparent pixels
                clip(color.a - 0.001);
                
                return color;
            }
            ENDCG
        }
    }
}