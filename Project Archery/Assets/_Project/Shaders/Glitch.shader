// Ennemi « corrompu » du portail des étoiles : la texture d'origine, mais l'image bugue.
// Des tranches horizontales du modèle se décalent par à-coups, les couleurs se séparent (rouge, vert, bleu décalés),
// des lignes de balayage défilent, et de rares bandes prennent la couleur des bugs. _Glitch règle la force (0 à 1).
// Les à-coups changent 12 fois par seconde au plus, et ne touchent que le modèle (jamais toute la vue).
// _Silhouette (0 à 1) assombrit le modèle jusqu'à _ShadowColor et ajoute un contour qui grésille, rouge ou cyan selon
// les tranches : on ne voit plus qu'une forme sombre.
Shader "Archery/Glitch"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor ("Couleur", Color) = (1, 1, 1, 1)
        _Glitch ("Force du bug", Range(0, 1)) = 0.5
        _Tint ("Couleur des bugs", Color) = (1, 0.1, 0.25, 1)
        _Silhouette ("Silhouette (0 = texture, 1 = forme sombre)", Range(0, 1)) = 0
        _ShadowColor ("Couleur de la silhouette", Color) = (0.012, 0.01, 0.018, 1)
        [HDR] _RimColor ("Contour", Color) = (2.4, 0.12, 0.18, 1)
        [HDR] _RimColorAlt ("Contour (autres tranches)", Color) = (0.15, 1.8, 2.4, 1)
        _RimPower ("Finesse du contour", Float) = 3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Glitch;
            half4 _Tint;
            half _Silhouette;
            half4 _ShadowColor;
            half4 _RimColor;
            half4 _RimColorAlt;
            half _RimPower;
        CBUFFER_END

        float GlitchHash(float n)
        {
            return frac(sin(n) * 43758.5453);
        }

        // Décalage d'une tranche horizontale (monde), tiré au sort 12 fois par seconde.
        float3 GlitchOffset(float3 positionWS)
        {
            float tick = floor(_Time.y * 12.0);
            float slice = floor(positionWS.y * 6.0);
            float roll = GlitchHash(slice * 13.1 + tick * 7.7);
            float shift = roll > 1.0 - 0.3 * _Glitch ? (GlitchHash(slice + tick * 3.3) - 0.5) * 0.4 * _Glitch : 0.0;
            return float3(shift, 0.0, shift * 0.6);
        }
        ENDHLSL

        Pass
        {
            Name "Glitch"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS += GlitchOffset(positionWS);
                output.positionWS = positionWS;
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Couleurs séparées : le rouge et le bleu glissent de part et d'autre.
                float tick = floor(_Time.y * 12.0);
                float2 split = float2(0.004 + 0.012 * _Glitch, 0.0) * (GlitchHash(tick) * 2.0 - 1.0);
                half red = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv + split).r;
                half green = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).g;
                half blue = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv - split).b;
                half3 color = half3(red, green, blue) * _BaseColor.rgb;

                // Un éclairage simple, pour garder le volume.
                Light light = GetMainLight();
                half lambert = saturate(dot(normalize(input.normalWS), light.direction));
                color *= 0.45 + 0.55 * lambert * light.color + SampleSH(normalize(input.normalWS)) * 0.4;

                // Lignes de balayage qui défilent, et de rares bandes de la couleur des bugs.
                color *= 0.82 + 0.18 * sin(input.positionWS.y * 140.0 + _Time.y * 25.0);

                // La silhouette : le corps presque noir, et un contour qui grésille (rouge ou cyan, par tranches qui changent).
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                half rim = pow(1.0 - saturate(dot(normalize(input.normalWS), viewDir)), _RimPower);
                half3 rimColor = GlitchHash(floor(input.positionWS.y * 7.0) + tick * 1.7) > 0.5 ? _RimColor.rgb : _RimColorAlt.rgb;
                color = lerp(color, _ShadowColor.rgb, _Silhouette) + rimColor * rim * _Silhouette;

                float band = GlitchHash(floor(input.positionWS.y * 9.0) + tick * 3.1);
                color = band > 1.0 - 0.1 * _Glitch ? _Tint.rgb * 2.5 : color;
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

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
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(positionWS + GlitchOffset(positionWS));
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
