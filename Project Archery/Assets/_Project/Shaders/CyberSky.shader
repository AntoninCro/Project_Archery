// Le ciel du toit cyberpunk (décor 4, 24 octobre) : celui du mode Difficile (beaucoup d'étoiles, une aurore, des étoiles
// filantes, une lune), aux couleurs et aux effets du cyberpunk. Un violet presque noir en haut, un halo rose à l'horizon
// (la ville éclaire le ciel) ; des étoiles blanches, cyan et roses, sur deux couches ; une aurore néon, rose à son pied et
// cyan en haut ; une nébuleuse violette en travers du ciel ; un smog bas, éclairé en rose par en dessous, qui cache les
// étoiles près de l'horizon ; une grosse lune, rose en bas, coupée de bandes ; de fines lignes de balayage, comme sur un
// écran ; et de temps en temps, une bande du ciel qui saute de côté en se teintant (le monde bugue). Sous l'horizon, la
// couleur du brouillard. Les bugs changent au plus 2,5 fois par seconde, et restent discrets (confort en VR).
Shader "Archery/CyberSky"
{
    Properties
    {
        _ZenithColor ("Zénith", Color) = (0.02, 0.01, 0.06, 1)
        _HorizonColor ("Horizon (la ville)", Color) = (0.42, 0.09, 0.36, 1)
        _GroundColor ("Sous l'horizon (le brouillard)", Color) = (0.1, 0.035, 0.13, 1)
        _HorizonSharpness ("Finesse de l'horizon", Range(0.5, 8)) = 2.6
        _Stars ("Étoiles", Range(0, 1)) = 1
        [HDR] _StarColorA ("Étoiles : première teinte", Color) = (0.55, 1.4, 1.8, 1)
        [HDR] _StarColorB ("Étoiles : deuxième teinte", Color) = (1.8, 0.55, 1.4, 1)
        _Nebula ("Nébuleuse", Range(0, 1)) = 0.6
        [HDR] _NebulaColor ("Nébuleuse : couleur", Color) = (0.35, 0.08, 0.5, 1)
        [HDR] _NebulaCore ("Nébuleuse : cœur", Color) = (0.6, 0.15, 0.55, 1)
        _NebulaAxis ("Nébuleuse : axe (perpendiculaire à sa bande)", Vector) = (0.3, 0.55, -0.78, 0)
        _Aurora ("Aurore néon", Range(0, 1)) = 0.7
        [HDR] _AuroraLow ("Aurore : pied", Color) = (1.5, 0.12, 1, 1)
        [HDR] _AuroraHigh ("Aurore : haut", Color) = (0.12, 1.1, 1.5, 1)
        _Smog ("Smog à l'horizon", Range(0, 1)) = 0.8
        _SmogColor ("Smog", Color) = (0.05, 0.015, 0.07, 1)
        [HDR] _SmogGlow ("Smog : dessous éclairé", Color) = (0.75, 0.12, 0.48, 1)
        _ShootingStars ("Étoiles filantes", Range(0, 1)) = 1
        [HDR] _ShootingColor ("Étoiles filantes : couleur", Color) = (0.4, 1.6, 2, 1)
        _DiscDirection ("Direction de la lune", Vector) = (-0.35, 0.32, 0.88, 0)
        [HDR] _DiscColor ("Lune : haut", Color) = (1.3, 1, 1.25, 1)
        [HDR] _DiscColorLow ("Lune : bas", Color) = (1.6, 0.3, 0.85, 1)
        _DiscSize ("Rayon de la lune (radians)", Range(0.005, 0.3)) = 0.09
        [HDR] _GlowColor ("Halo de la lune", Color) = (0.45, 0.1, 0.4, 1)
        _GlowSize ("Étendue du halo (radians)", Range(0.01, 1)) = 0.3
        _MoonSurface ("Taches de la lune", Range(0, 1)) = 0.6
        _MoonStripes ("Bandes de la lune", Range(0, 1)) = 1
        _Glitch ("Bugs (bandes qui sautent)", Range(0, 1)) = 0.5
        _Scanlines ("Lignes de balayage", Range(0, 1)) = 0.35
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
            Name "CyberSky"
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
                float _Stars;
                half4 _StarColorA;
                half4 _StarColorB;
                float _Nebula;
                half4 _NebulaColor;
                half4 _NebulaCore;
                float4 _NebulaAxis;
                float _Aurora;
                half4 _AuroraLow;
                half4 _AuroraHigh;
                float _Smog;
                half4 _SmogColor;
                half4 _SmogGlow;
                float _ShootingStars;
                half4 _ShootingColor;
                float4 _DiscDirection;
                half4 _DiscColor;
                half4 _DiscColorLow;
                float _DiscSize;
                half4 _GlowColor;
                float _GlowSize;
                float _MoonSurface;
                float _MoonStripes;
                float _Glitch;
                float _Scanlines;
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

            // Nombres pseudo-aléatoires entre 0 et 1 (hash sans sinus, stables sur tous les GPU).
            float Hash13(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.zyx + 31.32);
                return frac((p.x + p.y) * p.z);
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

            // Une couche d'étoiles : quelques cellules d'une grille 3D contiennent une étoile (« density » : la part des
            // cellules), blanche, ou de l'une des deux teintes.
            half3 StarLayer(float3 direction, float scale, float density, half strength)
            {
                float3 p = direction * scale;
                float3 cell = floor(p);
                float h = Hash13(cell);
                float threshold = 1.0 - density;
                if (h < threshold)
                    return 0;

                float3 offset = float3(Hash13(cell + 11.7), Hash13(cell + 23.3), Hash13(cell + 37.9)) - 0.5;
                float distanceToStar = length(frac(p) - 0.5 - offset * 0.5);
                float brightness = (h - threshold) / density;
                float twinkle = 0.7 + 0.3 * sin(_Time.y * (1.5 + 3.0 * brightness) + h * 80.0);
                half shape = smoothstep(0.22, 0.0, distanceToStar) * lerp(0.3, 1.0, brightness) * twinkle * strength;
                float hue = Hash13(cell + 51.7);
                half3 tint = hue < 0.45 ? half3(1.0, 1.0, 1.0) : (hue < 0.72 ? _StarColorA.rgb : _StarColorB.rgb);
                return shape * tint;
            }

            // La nébuleuse : une bande de nuages violets en travers du ciel (autour du grand cercle perpendiculaire à son
            // axe), plus vive en son cœur.
            half3 Nebula(float3 direction)
            {
                float3 axis = normalize(_NebulaAxis.xyz);
                float across = dot(direction, axis);
                half band = saturate(1.0 - abs(across) / 0.32);
                if (band <= 0.0)
                    return 0;

                float3 tangent = normalize(cross(axis, abs(axis.y) > 0.9 ? float3(1, 0, 0) : float3(0, 1, 0)));
                float3 bitangent = cross(axis, tangent);
                float2 q = float2(dot(direction, tangent), dot(direction, bitangent)) * 3.0 + float2(across * 2.0, 0.0);
                float clouds = Noise2(q * 1.6) * 0.55 + Noise2(q * 4.1 + 7.3) * 0.3 + Noise2(q * 9.7 - 3.1) * 0.15;
                half shape = smoothstep(0.35, 0.85, clouds) * band * band;
                half core = smoothstep(0.6, 0.95, clouds) * band;
                return _NebulaColor.rgb * shape + _NebulaCore.rgb * core * 0.6;
            }

            // L'aurore néon : des voiles verticaux qui ondulent lentement, roses à leur pied, cyan en haut.
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
                    float wave = sin(q.x * 1.6 + q.y * 0.5 + warp);
                    curtains += smoothstep(0.35, 1.0, wave) * (0.65 - layer * 0.15) * (0.6 + 0.4 * Noise2(q * 2.0 + time * 4.0));
                }

                half fade = saturate((height - 0.05) / 0.2) * saturate((0.95 - height) / 0.45);
                half shimmer = 0.85 + 0.15 * sin(_Time.y * 0.8 + p.x * 2.0);
                half3 tint = lerp(_AuroraLow.rgb, _AuroraHigh.rgb, saturate((height - 0.1) / 0.5));
                return tint * curtains * fade * shimmer;
            }

            // Étoiles filantes : de temps en temps, un trait lumineux traverse le ciel en moins d'une seconde.
            half ShootingStars(float3 direction)
            {
                float height = direction.y;
                if (height < 0.15)
                    return 0;

                float2 p = direction.xz / height;
                half glow = 0;
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
                    glow += smoothstep(0.012, 0.0, distanceToTrail) * along * fade;
                }

                return glow;
            }

            // Le smog : des nuages bas, près de l'horizon, sombres, éclairés en rose par en dessous (la ville). Rend leur
            // épaisseur (0 à 1) ; « lit » : leur part éclairée.
            half Smog(float3 direction, out half lit)
            {
                float height = direction.y;
                lit = saturate(1.0 - height / 0.3);
                if (_Smog <= 0.001 || height > 0.3 || height < -0.05)
                    return 0;

                float2 p = direction.xz / (max(height, 0.0) + 0.12) * 0.35;
                float time = _Time.y * 0.01;
                float clouds = Noise2(p * 1.3 + time) * 0.6 + Noise2(p * 3.1 - time * 1.7) * 0.4;
                return smoothstep(0.42, 0.8, clouds) * saturate((0.3 - height) / 0.2) * _Smog;
            }

            // Un bug : de temps en temps (2,5 tirages par seconde), une bande horizontale du ciel saute de côté (« shift », en
            // radians) et se teinte un peu de cyan ou de rose.
            void Glitch(float height, out float shift, out half3 tint)
            {
                shift = 0;
                tint = 0;
                if (_Glitch <= 0.001)
                    return;

                float band = floor(height * 30.0);
                float tick = floor(_Time.y * 2.5);
                if (Hash12(float2(band, tick)) < 1.0 - 0.06 * _Glitch)
                    return;

                float side = Hash12(float2(band + 17.0, tick)) - 0.5;
                shift = side * 0.08;
                tint = (side > 0.0 ? half3(0.0, 0.12, 0.16) : half3(0.16, 0.0, 0.11)) * _Glitch;
            }

            // Tache sombre de la lune, dans le repère du disque (de -1 à 1).
            half Patch(float2 uv, float2 center, float radius, half darkness)
            {
                return darkness * (1.0 - smoothstep(0.7, 1.0, length(uv - center) / radius));
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 direction = normalize(input.direction);
                float height = direction.y;

                // La bande qui bugue : tout ce qui est dessiné dedans (étoiles, lune…) est décalé de côté.
                float shift;
                half3 glitchTint;
                Glitch(height, shift, glitchTint);
                float3 view = direction;
                if (shift != 0.0)
                {
                    float s = sin(shift);
                    float c = cos(shift);
                    view.xz = float2(c * direction.x - s * direction.z, s * direction.x + c * direction.z);
                }

                // Dégradé : de l'horizon au zénith au-dessus, de l'horizon au sol en dessous.
                half horizon = pow(saturate(1.0 - height), _HorizonSharpness);
                half3 color = lerp(_ZenithColor.rgb, _HorizonColor.rgb, horizon);
                color = lerp(color, _GroundColor.rgb, smoothstep(0.0, 0.08, -height));

                half lit;
                half smog = Smog(view, lit);
                half clear = 1.0 - smog;
                half above = saturate(height * 6.0);

                if (_Nebula > 0.001)
                    color += Nebula(view) * _Nebula * above * clear;
                if (_Stars > 0.001)
                {
                    half3 stars = StarLayer(view, 150.0, 0.045, 1.0) + StarLayer(view, 330.0, 0.03, 0.4);
                    color += stars * _Stars * above * clear;
                }
                if (_Aurora > 0.001)
                    color += Aurora(view) * _Aurora * (1.0 - smog * 0.7);
                if (_ShootingStars > 0.001)
                    color += ShootingStars(view) * _ShootingStars * _ShootingColor.rgb * clear;

                // Le halo de la lune, puis son disque : rose en bas, coupé de bandes de plus en plus larges vers le bas.
                float3 discDirection = normalize(_DiscDirection.xyz);
                float angle = length(view - discDirection);
                color += _GlowColor.rgb * exp(-angle / max(_GlowSize, 0.001)) * clear;

                float3 up = abs(discDirection.y) > 0.99 ? float3(0, 0, 1) : float3(0, 1, 0);
                float3 right = normalize(cross(up, discDirection));
                float3 top = cross(discDirection, right);
                float2 uv = float2(dot(view, right), dot(view, top)) / _DiscSize;
                float radius = length(uv);
                float edge = max(fwidth(radius), 0.0001);
                float stripeEdge = max(fwidth(uv.y * 6.0), 0.0001);
                half disc = 1.0 - smoothstep(1.0 - edge, 1.0 + edge, radius);
                // Pas de deuxième lune à l'opposé (là aussi, uv est presque nul).
                disc *= smoothstep(-0.02, 0.0, height) * step(0.0, dot(view, discDirection));
                if (disc > 0.0)
                {
                    half gap = saturate((0.15 - uv.y) * 0.32) * _MoonStripes;
                    if (gap > 0.001)
                    {
                        float stripe = frac(uv.y * 6.0 + _Time.y * 0.12);
                        disc *= smoothstep(gap - stripeEdge, gap + stripeEdge, stripe);
                    }

                    half shade = 1.0;
                    shade -= Patch(uv, float2(-0.35, 0.3), 0.32, 0.22);
                    shade -= Patch(uv, float2(0.3, -0.05), 0.26, 0.18);
                    shade -= Patch(uv, float2(-0.05, -0.45), 0.22, 0.2);
                    shade -= Patch(uv, float2(0.45, 0.42), 0.14, 0.15);
                    shade *= 1.0 - 0.18 * saturate(dot(uv, uv));
                    half3 moon = lerp(_DiscColorLow.rgb, _DiscColor.rgb, saturate(uv.y * 0.5 + 0.5)) * lerp(1.0, shade, _MoonSurface);
                    color = lerp(color, moon, disc * lerp(1.0, 0.35, smog));
                }

                // Le smog par-dessus : sombre en haut, rose par en dessous.
                half3 smogColor = lerp(_SmogColor.rgb, _SmogGlow.rgb, lit * lit);
                color = lerp(color, smogColor, smog);

                // Les lignes de balayage, très fines, qui descendent lentement.
                color *= 1.0 - _Scanlines * 0.12 * (0.5 + 0.5 * sin(height * 420.0 + _Time.y * 1.5));

                color += glitchTint;
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
