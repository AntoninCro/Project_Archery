// La lame du katana (22 octobre), dans l'esprit des Akimbow et du katana de Katana Zero : de l'acier sombre où court un
// fil néon cyan, qu'un balayage de lumière remonte de la garde à la pointe, avec de fines lignes numériques. De temps en
// temps, la lame bugue : des tranches se décalent, passent au magenta ou disparaissent une fraction de seconde. _Glitch
// (bloc de propriétés, posé par le script) déclenche un bug tout de suite (un trait réussi, un signe lancé).
// La lame est repérée dans l'espace du maillage : _Axis (sa direction) et _Range (de la garde à la pointe, le long de
// _Axis) ; le baker les mesure sur le modèle.
Shader "Archery/KatanaBlade"
{
    Properties
    {
        _BaseColor ("Acier", Color) = (0.05, 0.06, 0.08, 1)
        [HDR] _GlowColor ("Néon", Color) = (0.25, 2.2, 3.0, 1)
        [HDR] _GlowColorAlt ("Néon (bug)", Color) = (2.8, 0.25, 1.9, 1)
        _Axis ("Axe de la lame (maillage)", Vector) = (0, 1, 0, 0)
        _Range ("Garde et pointe (le long de l'axe)", Vector) = (0, 1, 0, 0)
        _Lines ("Lignes numériques", Float) = 140
        _Glitch ("Bug (script)", Range(0, 1)) = 0
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
            Name "KatanaBlade"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _GlowColor;
                half4 _GlowColorAlt;
                float4 _Axis;
                float4 _Range;
                float _Lines;
                half _Glitch;
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
                float along : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewWS : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float BladeHash(float2 p)
            {
                return frac(sin(dot(p, float2(91.7, 263.3))) * 43758.5453);
            }

            // Le bug : rarement tout seul (une rafale de 0,25 s toutes les 3 à 6 s environ), ou quand le script le demande.
            float Burst()
            {
                float slot = floor(_Time.y / 0.25);
                float automatic = step(0.94, BladeHash(float2(slot, 4.1)));
                return saturate(max(automatic, _Glitch));
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 axis = normalize(_Axis.xyz);
                float along = saturate((dot(input.positionOS.xyz, axis) - _Range.x) / max(1e-4, _Range.y - _Range.x));

                // Pendant un bug, des tranches de la lame sautent de côté d'un coup.
                float burst = Burst();
                float band = floor(along * 14.0);
                float tick = floor(_Time.y * 30.0);
                float3 side = normalize(cross(axis, abs(axis.x) < 0.9 ? float3(1, 0, 0) : float3(0, 1, 0)));
                float jump = (BladeHash(float2(band, tick)) - 0.5) * 0.012 * burst * step(0.6, BladeHash(float2(band, tick + 9.0)));
                float3 position = input.positionOS.xyz + side * jump;

                VertexPositionInputs positions = GetVertexPositionInputs(position);
                output.positionHCS = positions.positionCS;
                output.along = along;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewWS = GetWorldSpaceViewDir(positions.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float along = input.along;
                float burst = Burst();
                float tick = floor(_Time.y * 30.0);
                float band = floor(along * 14.0);

                // Pendant un bug, des tranches disparaissent un instant.
                clip(0.5 - burst * step(0.82, BladeHash(float2(band, tick + 3.0))));

                // Le néon : plus vif près du tranchant (là où la normale est de profil), un balayage qui monte vers la pointe,
                // et de fines lignes numériques qui défilent.
                float3 normal = normalize(input.normalWS);
                float3 view = normalize(input.viewWS);
                float rim = pow(1.0 - saturate(abs(dot(normal, view))), 2.0);
                float sweep = exp(-pow((along - frac(_Time.y * 0.6)) / 0.06, 2.0));
                float lines = step(0.5, frac(along * _Lines - _Time.y * 3.0)) * 0.35 + 0.65;
                float tipFade = smoothstep(0.0, 0.05, along);
                float glow = (0.35 + 0.65 * rim) * lines * tipFade + sweep * 1.4;

                // Le bug : des tranches passent au magenta et la lueur scintille.
                float alt = burst * step(0.55, BladeHash(float2(band, tick + 1.0)));
                float flicker = 1.0 - burst * 0.5 * BladeHash(float2(tick, 2.0));
                half3 neon = lerp(_GlowColor.rgb, _GlowColorAlt.rgb, alt);

                // Un reflet discret, comme de l'acier poli sous le néon.
                float shine = pow(saturate(dot(reflect(-view, normal), float3(0, 1, 0))), 12.0) * 0.25;
                half3 color = _BaseColor.rgb + neon * glow * flicker + shine;
                return half4(color, 1);
            }
            ENDHLSL
        }

        // Pour la profondeur (ombres portées par d'autres, profondeur de la caméra) : la lame sans ses bugs.
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex depthVert
            #pragma fragment depthFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _GlowColor;
                half4 _GlowColorAlt;
                float4 _Axis;
                float4 _Range;
                float _Lines;
                half _Glitch;
            CBUFFER_END

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

            DepthVaryings depthVert(DepthAttributes input)
            {
                DepthVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half depthFrag(DepthVaryings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
