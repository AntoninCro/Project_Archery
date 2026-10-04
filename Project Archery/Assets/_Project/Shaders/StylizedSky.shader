// Ciel stylisé : dégradé zénith / horizon / sol, soleil ou lune avec son halo, étoiles.
// Les valeurs sont réglées en jeu par le script SkyController, selon la difficulté.
// Les tailles sont en radians (le script convertit les degrés des réglages).
Shader "Archery/StylizedSky"
{
    Properties
    {
        _ZenithColor ("Zénith", Color) = (0.2, 0.27, 0.5, 1)
        _HorizonColor ("Horizon", Color) = (0.98, 0.6, 0.38, 1)
        _GroundColor ("Sol", Color) = (0.25, 0.2, 0.2, 1)
        _HorizonSharpness ("Finesse de l'horizon", Range(0.5, 8)) = 2.2
        _DiscDirection ("Direction de l'astre", Vector) = (-0.9, 0.12, 0.42, 0)
        _DiscColor ("Astre", Color) = (1, 0.8, 0.55, 1)
        _DiscSize ("Rayon de l'astre (radians)", Range(0.005, 0.2)) = 0.049
        _GlowColor ("Halo", Color) = (0.7, 0.35, 0.15, 1)
        _GlowSize ("Étendue du halo (radians)", Range(0.01, 1)) = 0.44
        _MoonSurface ("Taches de la lune", Range(0, 1)) = 0
        _Stars ("Étoiles", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "StylizedSky"
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _HorizonColor;
                half4 _GroundColor;
                float _HorizonSharpness;
                float4 _DiscDirection;
                half4 _DiscColor;
                float _DiscSize;
                half4 _GlowColor;
                float _GlowSize;
                float _MoonSurface;
                float _Stars;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 direction : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                // Le ciel est centré sur la caméra : la position du sommet donne la direction du regard.
                output.direction = input.positionOS.xyz;
                return output;
            }

            // Nombre pseudo-aléatoire entre 0 et 1 (hash sans sinus, stable sur tous les GPU).
            float Hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
            }

            // Étoiles : quelques cellules d'une grille 3D contiennent une étoile, placée au hasard dans la cellule.
            half StarField(float3 direction)
            {
                float3 p = direction * 140.0;
                float3 cell = floor(p);
                float h = Hash13(cell);
                if (h < 0.965)
                    return 0;

                float3 offset = float3(Hash13(cell + 11.7), Hash13(cell + 23.3), Hash13(cell + 37.9)) - 0.5;
                float distanceToStar = length(frac(p) - 0.5 - offset * 0.5);
                float brightness = (h - 0.965) / 0.035;
                float twinkle = 0.75 + 0.25 * sin(_Time.y * (1.5 + 3.0 * brightness) + h * 80.0);
                return smoothstep(0.22, 0.0, distanceToStar) * lerp(0.35, 1.0, brightness) * twinkle;
            }

            // Tache sombre de la lune, dans le repère du disque (de -1 à 1).
            half Patch(float2 uv, float2 center, float radius, half darkness)
            {
                return darkness * (1.0 - smoothstep(0.7, 1.0, length(uv - center) / radius));
            }

            // 1 pour un soleil uni ; des taches et un bord plus sombre pour la lune.
            half MoonShade(float3 direction, float3 discDirection)
            {
                float3 up = abs(discDirection.y) > 0.99 ? float3(0, 0, 1) : float3(0, 1, 0);
                float3 right = normalize(cross(up, discDirection));
                float3 top = cross(discDirection, right);
                float2 uv = float2(dot(direction, right), dot(direction, top)) / _DiscSize;

                half shade = 1.0;
                shade -= Patch(uv, float2(-0.35, 0.3), 0.32, 0.22);
                shade -= Patch(uv, float2(0.3, -0.05), 0.26, 0.18);
                shade -= Patch(uv, float2(-0.05, -0.45), 0.22, 0.2);
                shade -= Patch(uv, float2(0.45, 0.42), 0.14, 0.15);
                shade -= Patch(uv, float2(-0.55, -0.2), 0.12, 0.15);
                shade *= 1.0 - 0.18 * saturate(dot(uv, uv));
                return lerp(1.0, shade, _MoonSurface);
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 direction = normalize(input.direction);
                float height = direction.y;

                // Dégradé : de l'horizon au zénith au-dessus, de l'horizon au sol en dessous.
                half horizon = pow(saturate(1.0 - height), _HorizonSharpness);
                half3 color = lerp(_ZenithColor.rgb, _HorizonColor.rgb, horizon);
                color = lerp(color, _GroundColor.rgb, smoothstep(0.0, 0.08, -height));

                if (_Stars > 0.001)
                    color += StarField(direction) * _Stars * saturate(height * 6.0);

                // Halo, puis disque du soleil ou de la lune. Près de l'astre, cette distance vaut l'angle en radians.
                float3 discDirection = normalize(_DiscDirection.xyz);
                float angle = length(direction - discDirection);
                color += _GlowColor.rgb * exp(-angle / max(_GlowSize, 0.001));

                float edge = max(fwidth(angle), 0.0001);
                half disc = 1.0 - smoothstep(_DiscSize - edge, _DiscSize + edge, angle);
                disc *= smoothstep(-0.02, 0.0, height);
                if (disc > 0.0)
                    color = lerp(color, _DiscColor.rgb * MoonShade(direction, discDirection), disc);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
