Shader "Shader Graphs/UIGradient"
{
    Properties
    {
        [NoScaleOffset]_GradientTex("_GradientTex", 2D) = "white" {}
        _GradientColor("_GradientColor", Color) = (1,1,1,1)
        _Strength("_Strength", Float) = 0.2
        _Falloff("_Falloff", Range(0.1,10)) = 1
        _Direction("_Direction", Vector) = (0,1,0,0)
        _EndPower("_EndPower", Range(1,10)) = 3
        _EdgePower("_EdgePower", Range(1,10)) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "UniversalMaterialType"="Unlit"
        }

        Pass
        {
            Name "Universal Forward"

            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"

            TEXTURE2D(_GradientTex);
            SAMPLER(sampler_GradientTex);
            float4 _GradientColor;
            float _Strength;
            float _Falloff;
            float2 _Direction;
            float _EndPower;
            float _EdgePower;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 tangentOS : TANGENT;
                float3 normalOS : NORMAL;
                float4 uv0 : TEXCOORD0;
            };

            struct Varyings
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.pos = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv0.xy;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // -----------------------------
                // Directional gradient along _Direction
                // -----------------------------
                float2 dirNorm = normalize(_Direction);

                // Center the UV space around 0.5
                float2 centeredUV = uv - 0.5;
                
                // Project centered UV onto direction vector
                float proj = dot(centeredUV, dirNorm);
                
                // Map projection from [-0.707, 0.707] range to [0, 1]
                // (0.707 is the max distance from center to corner)
                float gradient = saturate(proj / 0.707 + 0.5);

                // Apply end falloff (fade at the end of gradient)
                float endSoft = pow(gradient, _EndPower);

                // -----------------------------
                // Edge feathering for all 4 sides
                // -----------------------------
                // Calculate distance to each edge (0 at edge, 1 at opposite edge)
                float edgeLeft = uv.x;
                float edgeRight = 1.0 - uv.x;
                float edgeBottom = uv.y;
                float edgeTop = 1.0 - uv.y;
                
                // Apply power to create feathering effect on each edge
                float edgeSoftLeft = pow(saturate(edgeLeft), _EdgePower);
                float edgeSoftRight = pow(saturate(edgeRight), _EdgePower);
                float edgeSoftBottom = pow(saturate(edgeBottom), _EdgePower);
                float edgeSoftTop = pow(saturate(edgeTop), _EdgePower);
                
                // Combine all edge feathering
                float edgeSoft = edgeSoftLeft * edgeSoftRight * edgeSoftBottom * edgeSoftTop;

                // -----------------------------
                // Combine directional and edge feathering
                // -----------------------------
                float alphaSoft = endSoft * edgeSoft;

                // -----------------------------
                // Texture alpha
                // -----------------------------
                float texAlpha = SAMPLE_TEXTURE2D(_GradientTex, sampler_GradientTex, uv).a;
                texAlpha = pow(texAlpha, _Falloff);

                // -----------------------------
                // Final alpha
                // -----------------------------
                float finalAlpha = alphaSoft * texAlpha * _Strength;
                finalAlpha = pow(finalAlpha, 1.0 / 2.2); // gamma correction

                return half4(_GradientColor.rgb, finalAlpha);
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/Transparent"
}