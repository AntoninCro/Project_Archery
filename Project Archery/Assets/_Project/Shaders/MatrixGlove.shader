// Le gant de la matrice (24 octobre) : avec le katana, la main de l'arc garde le gant d'archer, mais comme s'il était fait
// de la matrice du jeu. Un gant presque noir où tombent des colonnes de 0 et de 1 verts (comme dans les failles), un liseré
// vert sur ses contours, de fines lignes de balayage qui montent ; de temps en temps, il bugue (des tranches sautent de côté
// et la lueur scintille). Il pulse quand une faille attend d'être frappée.
// Bloc de propriétés (posé par le script) : _Pulse (0 à 1, une faille attend : plus vif, il bat), _Glitch (un bug tout de
// suite : une faille lancée).
// Les chiffres suivent la surface du gant dans l'espace du maillage, sur la face la plus tournée vers chaque axe.
Shader "Archery/MatrixGlove"
{
    Properties
    {
        _BaseColor ("Fond", Color) = (0.01, 0.03, 0.02, 1)
        [HDR] _CodeColor ("Chiffres", Color) = (0.2, 1.6, 0.45, 1)
        [HDR] _HeadColor ("Tête des gouttes", Color) = (1.2, 2.4, 1.3, 1)
        [HDR] _RimColor ("Liseré", Color) = (0.3, 2.0, 0.8, 1)
        _CellSize ("Taille d'un chiffre (m)", Float) = 0.006
        _Speed ("Vitesse de chute", Float) = 7
        _ScanLines ("Lignes de balayage (par m)", Float) = 260
        _Pulse ("Faille qui attend (script)", Range(0, 1)) = 0
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
            Name "MatrixGlove"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _CodeColor;
                half4 _HeadColor;
                half4 _RimColor;
                float _CellSize;
                float _Speed;
                float _ScanLines;
                half _Pulse;
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
                float3 positionOS : TEXCOORD0;
                float3 normalOS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewWS : TEXCOORD3;
                float heightWS : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float GloveHash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // Le bug : rarement tout seul (une rafale de 0,2 s toutes les 4 à 8 s environ), ou quand le script le demande.
            float Burst()
            {
                float slot = floor(_Time.y / 0.2);
                float automatic = step(0.965, GloveHash(float2(slot, 8.3)));
                return saturate(max(automatic, _Glitch));
            }

            // Un chiffre dans sa case (0 à 1 dans les deux sens), comme dans les failles (MatrixTear).
            float Digit(float2 p, float one, float soft)
            {
                float zero = abs(length((p - 0.5) / float2(0.3, 0.4)) - 1.0) * 0.3;
                float bar = max(abs(p.x - 0.55) - 0.07, abs(p.y - 0.5) - 0.38);
                float2 flagA = float2(0.55, 0.88);
                float2 flagB = float2(0.32, 0.7);
                float2 ba = flagB - flagA;
                float h = saturate(dot(p - flagA, ba) / dot(ba, ba));
                float flag = length(p - flagA - ba * h) - 0.06;
                float foot = max(abs(p.x - 0.55) - 0.2, abs(p.y - 0.12) - 0.05);
                float d = lerp(zero - 0.05, min(min(bar, flag), foot), one);
                return 1.0 - smoothstep(-soft, soft, d);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Pendant un bug, des tranches du gant sautent de côté d'un coup.
                float burst = Burst();
                float band = floor(input.positionOS.y * 90.0);
                float tick = floor(_Time.y * 30.0);
                float jump = (GloveHash(float2(band, tick)) - 0.5) * 0.008 * burst * step(0.6, GloveHash(float2(band, tick + 9.0)));
                float3 position = input.positionOS.xyz + float3(jump, 0, 0);

                float3 positionWS = TransformObjectToWorld(position);
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.positionOS = input.positionOS.xyz;
                output.normalOS = input.normalOS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewWS = GetWorldSpaceViewDir(positionWS);
                output.heightWS = positionWS.y;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // La face du maillage la plus tournée vers chaque axe : les chiffres s'y posent à plat.
                float3 n = abs(normalize(input.normalOS));
                float2 plane = n.x > n.y && n.x > n.z ? input.positionOS.zy : (n.y > n.z ? input.positionOS.xz : input.positionOS.xy);

                float2 cellSize = float2(_CellSize * 0.62, _CellSize);
                float2 grid = plane / cellSize;
                float column = floor(grid.x);
                float columnHash = GloveHash(float2(column, 7.3));
                float speed = _Speed * (0.55 + columnHash);
                float fall = grid.y + _Time.y * speed;
                float row = floor(fall);
                float2 inCell = float2(frac(grid.x), frac(fall));
                float trail = frac(columnHash * 13.7 - grid.y / 20.0 - _Time.y * speed / 20.0);
                float brightness = pow(trail, 3.0);
                float head = smoothstep(0.92, 1.0, trail);
                float change = floor(_Time.y * (2.0 + 6.0 * columnHash) + GloveHash(float2(column, row)) * 5.0);
                float one = step(0.5, GloveHash(float2(column * 1.7 + change, row * 3.1)));
                float soft = max(fwidth(inCell.x), fwidth(inCell.y)) * 1.5 + 0.02;
                float glyph = Digit(inCell, one, soft) * step(0.15, brightness + head);

                // Une faille attend : le gant bat, plus vif.
                float beat = _Pulse * (0.6 + 0.4 * sin(_Time.y * 14.0));
                float burst = Burst();
                float tick = floor(_Time.y * 30.0);
                float flicker = 1.0 - burst * 0.6 * GloveHash(float2(tick, 2.0));

                float3 normal = normalize(input.normalWS);
                float3 view = normalize(input.viewWS);
                float rim = pow(1.0 - saturate(dot(normal, view)), 2.5);
                float scan = step(0.5, frac(input.heightWS * _ScanLines - _Time.y * 2.0)) * 0.12 + 0.88;

                half3 color = _BaseColor.rgb;
                color += _CodeColor.rgb * glyph * brightness * (1.0 + 1.2 * beat);
                color = lerp(color, _HeadColor.rgb, glyph * head);
                color += _RimColor.rgb * rim * (0.8 + 1.6 * beat);
                color *= scan * flicker;
                return half4(color, 1);
            }
            ENDHLSL
        }

        // Pour la profondeur (occlusion ambiante, profondeur de la caméra) : le gant sans ses bugs.
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
                half4 _CodeColor;
                half4 _HeadColor;
                half4 _RimColor;
                float _CellSize;
                float _Speed;
                float _ScanLines;
                half _Pulse;
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
