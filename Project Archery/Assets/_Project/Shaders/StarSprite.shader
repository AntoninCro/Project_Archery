// Étoiles, galaxies et nébuleuses de la dimension du portail : chaque carré devient un point de lumière rond, qui
// s'ajoute à ce qui est derrière (les lueurs qui se chevauchent s'additionnent, le bloom les fait rayonner).
// La couleur des sommets (HDR) donne la teinte et l'éclat ; uv2.x donne la netteté : 0 = un nuage flou (nébuleuse,
// bras de galaxie), 1 = une étoile au cœur net entourée d'un petit halo.
Shader "Archery/StarSprite"
{
    Properties
    {
        _BaseColor ("Couleur (multipliée)", Color) = (1, 1, 1, 1)
        _Intensity ("Éclat", Float) = 1
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
            Name "StarSprite"
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Intensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 shape : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color : COLOR;
                float3 uvSharp : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color * _BaseColor;
                output.color.rgb *= output.color.a * _Intensity;
                output.uvSharp = float3(input.uv * 2.0 - 1.0, input.shape.x);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float d2 = dot(input.uvSharp.xy, input.uvSharp.xy);
                if (d2 >= 1.0)
                    discard;

                // Flou : une cloche douce. Net : un cœur serré et un halo léger. Les deux s'éteignent au bord du carré.
                float soft = exp(-d2 * 3.5);
                float sharp = exp(-d2 * 30.0) + 0.18 * exp(-d2 * 6.0);
                float shape = lerp(soft, sharp, saturate(input.uvSharp.z)) * (1.0 - d2);
                return half4(input.color.rgb * shape, 1.0);
            }
            ENDHLSL
        }
    }
}
