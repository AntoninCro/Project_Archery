// Cristaux du plafond de l'arène du boss final : non éclairés, ils brillent de leur couleur (couleur des sommets).
// _Intensity règle leur éclat (0 = éteints ; au-dessus de 1, le bloom les fait rayonner), _Tint teinte le tout
// (rouge quand le dragon est enragé). Chaque cristal scintille un peu, à son rythme (phase dans uv.x).
Shader "Archery/Crystal"
{
    Properties
    {
        _Intensity ("Éclat", Float) = 1
        _Tint ("Teinte", Color) = (1, 1, 1, 1)
        _Twinkle ("Scintillement", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Crystal"
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Intensity;
                half4 _Tint;
                half _Twinkle;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionHCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // La pointe (uv.y = 1) est plus claire ; chaque cristal scintille avec sa propre phase (uv.x).
                half tip = lerp(1.0, 1.7, saturate(input.uv.y));
                half twinkle = 1.0 + _Twinkle * sin(_Time.y * 2.7 + input.uv.x * 43.0);
                half3 color = input.color.rgb * _Tint.rgb * (_Intensity * tip * twinkle);

                // Éteint, un cristal reste très sombre (il ne disparaît pas tout à fait sous la lumière des autres).
                color += input.color.rgb * 0.015;
                color = MixFog(color, input.fogFactor * 0.6);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // Profondeur seule (prépasse de profondeur d'URP) : sans elle, les cristaux pourraient disparaître.
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half _Intensity;
                half4 _Tint;
                half _Twinkle;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }
    }
}
