// Les Akimbow : du métal sombre en plaques le long du canon (deux teintes), des joints sombres dont certains s'allument en
// néon, une bande néon sur les flancs (coupée par endroits), une ligne sur le dessus, quelques LED, un léger contour néon.
// Le métal n'est pas uni : des taches plus claires ou plus sombres, et des touches de couleur par-ci par-là (des plaques
// peintes en partie, de petites étiquettes sur les flancs, des rayures de danger sur certaines branches), sur la couleur de
// base, qui reste dominante.
// Le modèle n'a pas d'UV : le motif est calculé dans son repère (canon vers +Z), il ne bouge pas. Deux matériaux (couleurs,
// densité des plaques, graine) donnent deux armes différentes.
// _Glitch (0 à 1) est mis par le jeu pendant les bugs : des tranches du modèle glissent, des bandes prennent la couleur des
// bugs, des lignes de balayage passent. À 0, rien ne bouge.
// _BaseColor et _EmissionColor (blancs) multiplient le métal et les néons : le menu des armes en fait une silhouette sombre.
Shader "Archery/Akimbow"
{
    Properties
    {
        _BodyColor ("Métal", Color) = (0.05, 0.06, 0.08, 1)
        _PanelColor ("Plaques", Color) = (0.11, 0.12, 0.15, 1)
        [HDR] _NeonColor ("Néon", Color) = (0.2, 1.4, 2.2, 1)
        [HDR] _NeonColorAlt ("Néon (deuxième couleur)", Color) = (0.4, 0.5, 2.4, 1)
        _PanelDensity ("Plaques par mètre (repère du modèle)", Float) = 22
        _Seed ("Graine du motif", Float) = 0
        _StripeHeight ("Hauteur de la bande des flancs (repère du modèle)", Float) = 0.09
        _RimStrength ("Contour néon", Range(0, 1)) = 0.35
        _AccentColor ("Touches de couleur", Color) = (0.55, 0.32, 0.05, 1)
        _AccentColorAlt ("Touches de couleur (deuxième)", Color) = (0.4, 0.42, 0.45, 1)
        _Wear ("Taches du métal", Range(0, 1)) = 0.4
        _BaseColor ("Teinte (multiplie le métal)", Color) = (1, 1, 1, 1)
        _EmissionColor ("Lueur (multiplie les néons)", Color) = (1, 1, 1, 1)
        _Glitch ("Bug (0 = stable)", Range(0, 1)) = 0
        _Tint ("Couleur des bugs", Color) = (1, 0.1, 0.25, 1)
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
            half4 _BodyColor;
            half4 _PanelColor;
            half4 _NeonColor;
            half4 _NeonColorAlt;
            float _PanelDensity;
            float _Seed;
            float _StripeHeight;
            half _RimStrength;
            half4 _AccentColor;
            half4 _AccentColorAlt;
            half _Wear;
            half4 _BaseColor;
            half4 _EmissionColor;
            half _Glitch;
            half4 _Tint;
        CBUFFER_END

        float AkimbowHash(float n)
        {
            return frac(sin(n) * 43758.5453);
        }

        // Un bruit doux (0 à 1) : des taches dans le métal.
        float AkimbowNoise(float3 x)
        {
            float3 i = floor(x);
            float3 f = frac(x);
            f = f * f * (3.0 - 2.0 * f);
            float n = i.x + i.y * 57.0 + i.z * 113.0;
            return lerp(lerp(lerp(AkimbowHash(n), AkimbowHash(n + 1.0), f.x), lerp(AkimbowHash(n + 57.0), AkimbowHash(n + 58.0), f.x), f.y),
                        lerp(lerp(AkimbowHash(n + 113.0), AkimbowHash(n + 114.0), f.x), lerp(AkimbowHash(n + 170.0), AkimbowHash(n + 171.0), f.x), f.y),
                        f.z);
        }

        // Pendant un bug : des tranches horizontales du modèle glissent sur le côté, tirées au sort 12 fois par seconde.
        float3 AkimbowGlitch(float3 positionOS)
        {
            if (_Glitch <= 0.001)
                return float3(0.0, 0.0, 0.0);
            float tick = floor(_Time.y * 12.0);
            float slice = floor(positionOS.y * 45.0);
            float roll = AkimbowHash(slice * 13.1 + tick * 7.7);
            float shift = roll > 1.0 - 0.5 * _Glitch ? (AkimbowHash(slice + tick * 3.3) - 0.5) * 0.06 * _Glitch : 0.0;
            return float3(shift, 0.0, shift * 0.5);
        }
        ENDHLSL

        Pass
        {
            Name "Akimbow"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

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
                float3 positionWS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz + AkimbowGlitch(input.positionOS.xyz));
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.positionOS = input.positionOS.xyz;
                output.normalOS = input.normalOS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            // Une ligne de « width » (m du modèle) de large autour de 0, au bord adouci.
            float AkimbowLine(float distance, float width)
            {
                return 1.0 - smoothstep(width * 0.5, width, abs(distance));
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 p = input.positionOS;
                float3 n = normalize(input.normalOS);

                // Les plaques, le long du canon : deux teintes de métal, tirées au sort pour chacune.
                float cell = p.z * _PanelDensity + _Seed;
                float id = floor(cell);
                float f = frac(cell);
                half3 albedo = lerp(_BodyColor.rgb, _PanelColor.rgb, step(0.45, AkimbowHash(id * 1.37 + _Seed * 3.1)));

                // Les joints entre les plaques : sombres ; un sur trois environ s'allume, d'une des deux couleurs.
                float seam = AkimbowLine(min(f, 1.0 - f) / _PanelDensity, 0.004);

                // Le métal n'est pas uni : des taches plus claires ou plus sombres, à deux échelles.
                float stains = AkimbowNoise(p * 55.0 + _Seed) * 0.6 + AkimbowNoise(p * 170.0 + _Seed * 2.0) * 0.4;
                albedo *= 1.0 + _Wear * (stains - 0.5) * 1.6;

                float side = step(0.6, abs(n.x));
                float upward = step(0.6, n.y);

                // Des touches de couleur, par-ci par-là. Sur une plaque sur cinq environ : le haut des flancs peint.
                float panelLength = 1.0 / _PanelDensity;
                float painted = step(0.8, AkimbowHash(id * 6.7 + _Seed * 2.3)) * side * step(_StripeHeight + 0.006, p.y);
                albedo = lerp(albedo, _AccentColor.rgb * (0.85 + 0.3 * stains), painted);

                // Sur une plaque sur trois : une petite étiquette sur les flancs, à une hauteur tirée au sort.
                float labelHeight = lerp(0.03, 0.075, AkimbowHash(id * 8.3 + _Seed * 1.1));
                float2 label = abs(float2((f - 0.5) * panelLength, p.y - labelHeight));
                float hasLabel = step(0.66, AkimbowHash(id * 3.9 + _Seed * 0.9)) * side;
                float labelMask = hasLabel * step(label.x, panelLength * 0.28) * step(label.y, 0.006);
                albedo = lerp(albedo, _AccentColorAlt.rgb, labelMask);

                // Sur une branche sur deux (le dessus, loin du rail) : des rayures de danger.
                float hazard = upward * step(0.06, abs(p.x)) * step(0.5, AkimbowHash(floor(p.x * 12.0) * 2.3 + id * 0.7 + _Seed));
                float stripes = step(0.5, frac((p.x + p.z) * 90.0));
                albedo = lerp(albedo, lerp(_AccentColor.rgb, _BodyColor.rgb * 0.6, stripes), hazard);

                albedo *= 1.0 - 0.65 * seam;
                float seamLit = seam * step(0.62, AkimbowHash(id * 2.71 + _Seed));
                half3 seamColor = AkimbowHash(id * 4.4 + _Seed * 0.3) > 0.5 ? _NeonColor.rgb : _NeonColorAlt.rgb;

                // La bande des flancs, coupée sur certaines plaques ; la ligne du milieu sur le dessus ; une LED sur quelques plaques.
                float stripe = AkimbowLine(p.y - _StripeHeight, 0.005) * side * step(0.3, AkimbowHash(id * 5.3 + _Seed * 1.9));
                float top = AkimbowLine(p.x, 0.005) * upward;
                float2 ledOffset = float2((f - 0.5) / _PanelDensity, p.y - (_StripeHeight - 0.022));
                float led = side * step(0.7, AkimbowHash(id * 9.1 + _Seed * 0.7)) * (1.0 - smoothstep(0.0025, 0.0045, length(ledOffset)));

                half3 emission = seamColor * seamLit + _NeonColor.rgb * saturate(stripe + top) + _NeonColorAlt.rgb * led;

                // Un éclairage simple : diffus, ambiant, un reflet de métal, et un contour néon discret.
                float3 normalWS = normalize(input.normalWS);
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                Light light = GetMainLight();
                half lambert = saturate(dot(normalWS, light.direction));
                half specular = pow(saturate(dot(normalWS, normalize(light.direction + viewDir))), 48.0) * 0.5;
                half3 color = albedo * _BaseColor.rgb * (lambert * light.color + SampleSH(normalWS) * 0.8 + 0.15);
                color += specular * light.color * _BaseColor.rgb;
                half rim = pow(1.0 - saturate(dot(normalWS, viewDir)), 3.0) * _RimStrength;
                color += (emission + _NeonColor.rgb * rim * 0.5) * _EmissionColor.rgb;

                // Le bug : des bandes de la couleur des bugs, et des lignes de balayage qui défilent.
                if (_Glitch > 0.001)
                {
                    float tick = floor(_Time.y * 12.0);
                    float band = AkimbowHash(floor(p.y * 60.0) + tick * 3.1);
                    color = band > 1.0 - 0.35 * _Glitch ? _Tint.rgb * 2.0 : color;
                    color *= 0.8 + 0.2 * sin(p.y * 900.0 + _Time.y * 40.0);
                }

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
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz + AkimbowGlitch(input.positionOS.xyz));
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
