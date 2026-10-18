// Nuages en blocs, à la Minecraft (CloudLayer) : non éclairés, ombrés par face (la couleur des sommets : dessus clair,
// côtés un peu plus sombres, dessous plus sombre), de la couleur du ciel (_TopColor, réglée par le SkyController).
// Un peu transparents ; ils s'effacent avec la distance (fondu de _FadeStart à _FadeEnd, en mètres à l'horizontale) :
// on ne voit jamais le bord de la couche.
Shader "Archery/Clouds"
{
    Properties
    {
        _TopColor ("Couleur du dessus", Color) = (1, 1, 1, 1)
        _Alpha ("Opacité", Range(0, 1)) = 0.88
        _FadeStart ("Début du fondu (m)", Float) = 150
        _FadeEnd ("Fin du fondu (m)", Float) = 230
        _FogAmount ("Part du brouillard", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent-50"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "Clouds"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half _Alpha;
                float _FadeStart;
                float _FadeEnd;
                half _FogAmount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half shade : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
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
                output.shade = input.color.r;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Ombré par face, un peu fondu dans le brouillard (couleur de l'horizon).
                half3 color = _TopColor.rgb * input.shade;
                color = lerp(color, MixFog(color, input.fogFactor), _FogAmount);

                // De plus en plus transparent au loin.
                float distance = length(input.positionWS.xz - _WorldSpaceCameraPos.xz);
                half alpha = _Alpha * (1.0 - smoothstep(_FadeStart, _FadeEnd, distance));
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
