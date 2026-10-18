// Voile de givre posé par-dessus un modèle (le dragon touché par une boule de glace) : une lueur bleutée ajoutée à ses
// couleurs, plus vive sur les bords, comme une fine couche de glace. _Amount règle sa force (0 : invisible).
// À ajouter en matériau supplémentaire sur le rendu du modèle : il redessine le maillage par-dessus, sans le cacher.
Shader "Archery/FrostOverlay"
{
    Properties
    {
        [HDR] _Color ("Couleur", Color) = (0.35, 0.65, 1, 1)
        _Amount ("Force", Range(0, 1)) = 0
        _RimPower ("Finesse du bord", Float) = 2.5
        _Base ("Part sur toute la surface", Range(0, 1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "FrostOverlay"
            Blend One One
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Amount;
                half _RimPower;
                half _Base;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 normal = normalize(input.normalWS);
                float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half rim = pow(1.0 - saturate(dot(normal, view)), _RimPower);
                half3 color = _Color.rgb * (_Base + rim) * _Amount;
                return half4(color, 0.0);
            }
            ENDHLSL
        }
    }
}
