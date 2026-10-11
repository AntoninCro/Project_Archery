// Plan bleu d'un bâtiment en construction (GDD, section 9) : un hologramme transparent parcouru de lignes qui montent.
// Sous _FillHeight (hauteur dans le monde), la partie déjà construite est plus pleine ; une ligne lumineuse marque
// le niveau atteint. _Flash fait briller tout le plan au moment d'un coup de marteau.
Shader "Archery/Blueprint"
{
    Properties
    {
        [HDR] _BaseColor ("Couleur du plan vide", Color) = (0.25, 0.6, 1, 0.22)
        [HDR] _FilledColor ("Couleur de la partie construite", Color) = (0.35, 0.75, 1, 0.7)
        [HDR] _EdgeColor ("Couleur des bords et de la ligne de remplissage", Color) = (0.6, 0.9, 1, 1)
        _FillHeight ("Hauteur remplie (monde)", Float) = -1000
        _FillBand ("Épaisseur de la ligne de remplissage (m)", Float) = 0.06
        _LineSpacing ("Espacement des lignes (m)", Float) = 0.1
        _LineSpeed ("Défilement des lignes", Float) = 0.15
        _RimPower ("Finesse des bords", Range(0.5, 8)) = 2.5
        _Flash ("Éclat du dernier coup", Range(0, 1)) = 0
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

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _FilledColor;
            half4 _EdgeColor;
            float _FillHeight;
            float _FillBand;
            float _LineSpacing;
            float _LineSpeed;
            half _RimPower;
            half _Flash;
        CBUFFER_END
        ENDHLSL

        // D'abord la profondeur seule : ensuite, seules les faces les plus proches se dessinent,
        // sans que l'intérieur du modèle transparaisse.
        Pass
        {
            Name "BlueprintDepth"
            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth
            #pragma multi_compile_instancing

            struct DepthAttributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            DepthVaryings vertDepth(DepthAttributes input)
            {
                DepthVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 fragDepth(DepthVaryings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Blueprint"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Les bords du modèle, vus de biais, brillent.
                float3 normal = normalize(input.normalWS);
                float3 view = normalize(input.viewDirWS);
                half rim = pow(1.0 - saturate(abs(dot(normal, view))), _RimPower);

                // Lignes horizontales qui montent doucement, comme sur un hologramme.
                float y = input.positionWS.y;
                half lines = smoothstep(0.75, 1.0, frac(y / max(_LineSpacing, 0.01) - _Time.y * _LineSpeed));

                // La partie construite (sous la hauteur remplie), et la ligne lumineuse à cette hauteur.
                half filled = step(y, _FillHeight);
                half band = saturate(1.0 - abs(y - _FillHeight) / max(_FillBand, 0.001));

                half4 empty = _BaseColor;
                empty.a = saturate(_BaseColor.a * (0.6 + lines * 0.8) + rim * 0.5);
                half4 full = _FilledColor;
                full.a = saturate(_FilledColor.a + rim * 0.25);
                half4 color = lerp(empty, full, filled);

                half glow = saturate(band + rim * 0.35 + _Flash * 0.6);
                color.rgb += _EdgeColor.rgb * glow;
                color.a = saturate(color.a + band * 0.6 + _Flash * 0.3);
                return color;
            }
            ENDHLSL
        }
    }
}
