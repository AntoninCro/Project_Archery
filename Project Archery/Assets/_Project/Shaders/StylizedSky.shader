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
        _Aurora ("Aurore", Range(0, 1)) = 0
        [HDR] _AuroraColor ("Couleur de l'aurore", Color) = (0.25, 1, 0.6, 1)
        _ShootingStars ("Étoiles filantes", Range(0, 1)) = 0
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
                float _Aurora;
                half4 _AuroraColor;
                float _ShootingStars;
            CBUFFER_END

            // Éclair en cours (0 à 1), réglé pour tout le jeu par le SkyController : le ciel blanchit un instant.
            float _ArcheryFlash;

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

            float Hash12(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            // Bruit de valeur lissé en 2D, entre 0 et 1.
            float Noise2(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash12(i), Hash12(i + float2(1, 0)), u.x), lerp(Hash12(i + float2(0, 1)), Hash12(i + float2(1, 1)), u.x), u.y);
            }

            // Aurore : des voiles verticaux qui ondulent lentement, haut dans le ciel, plus vifs à leur pied.
            half3 Aurora(float3 direction)
            {
                float height = direction.y;
                if (height < 0.05)
                    return 0;

                // Projetés sur un plafond : les voiles s'étirent vers l'horizon.
                float2 p = direction.xz / (height + 0.25) * 1.4;
                float time = _Time.y * 0.03;
                half curtains = 0;
                for (int layer = 0; layer < 3; layer++)
                {
                    float2 q = p * (1.0 + layer * 0.55) + float2(layer * 3.7, layer * 1.3);
                    float warp = Noise2(q * 0.6 + float2(time * (1.0 + layer), -time)) * 5.0;
                    float band = sin(q.x * 1.6 + q.y * 0.5 + warp);
                    curtains += smoothstep(0.35, 1.0, band) * (0.65 - layer * 0.15) * (0.6 + 0.4 * Noise2(q * 2.0 + time * 4.0));
                }

                half fade = saturate((height - 0.05) / 0.2) * saturate((0.95 - height) / 0.45);
                half shimmer = 0.85 + 0.15 * sin(_Time.y * 0.8 + p.x * 2.0);
                return _AuroraColor.rgb * curtains * fade * shimmer;
            }

            // Étoiles filantes : de temps en temps, un trait lumineux traverse le ciel en moins d'une seconde.
            half ShootingStars(float3 direction)
            {
                float height = direction.y;
                if (height < 0.15)
                    return 0;

                float2 p = direction.xz / height;
                half light = 0;
                for (int slot = 0; slot < 2; slot++)
                {
                    float period = 5.0 + slot * 2.3;
                    float t = _Time.y / period + slot * 0.37;
                    float index = floor(t);
                    float phase = frac(t) * period;
                    if (phase > 0.9 || Hash12(float2(index, slot)) > _ShootingStars)
                        continue;

                    // Départ et sens tirés au hasard ; la tête avance, la queue s'efface derrière elle.
                    float2 start = (float2(Hash12(float2(index, 7.1 + slot)), Hash12(float2(index, 3.3 + slot))) - 0.5) * 2.4;
                    float angle = Hash12(float2(index, 9.7 + slot)) * 6.2831853;
                    float2 velocity = float2(cos(angle), sin(angle)) * 1.6;
                    float2 head = start + velocity * phase;
                    float2 tail = head - velocity * 0.18;
                    float2 segment = head - tail;
                    float along = saturate(dot(p - tail, segment) / dot(segment, segment));
                    float distanceToTrail = length(p - (tail + segment * along));
                    half fade = smoothstep(0.0, 0.1, phase) * smoothstep(0.9, 0.6, phase);
                    light += smoothstep(0.012, 0.0, distanceToTrail) * along * fade;
                }

                return light;
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
                if (_Aurora > 0.001)
                    color += Aurora(direction) * _Aurora;
                if (_ShootingStars > 0.001 && _Stars > 0.001)
                    color += ShootingStars(direction) * _Stars * half3(1.0, 0.95, 0.85) * 1.5;

                // Éclair : tout le ciel s'éclaire d'un coup, surtout près de l'horizon.
                color += _ArcheryFlash * half3(0.75, 0.78, 0.95) * (0.6 + 0.4 * (1.0 - saturate(height)));

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
