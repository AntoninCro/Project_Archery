// Eau low-poly de la rivière et des cascades (URP).
// Facettes plates qui ondulent, couleur selon la profondeur (texture de profondeur d'URP), écume le long des berges,
// reflets et traînées d'écume qui défilent dans le sens du courant (UV1 : sens et vitesse, écrits par RiverWater).
// Cascade (_Fall à 1) : pas de vagues, l'eau défile vers le bas (UV0.y), bandes blanches, écume en haut et en bas.
// La nuit (_ArcheryNight, réglé par le ciel), la rivière luit : des points qui dérivent avec le courant, une lueur le
// long des berges, et des remous lumineux là où passent les ennemis (_ArcheryRipples, écrits par MapGameplayLights).
Shader "Archery/Water"
{
    Properties
    {
        _ShallowColor ("Eau peu profonde", Color) = (0.45, 0.86, 0.86, 0.55)
        _DeepColor ("Eau profonde", Color) = (0.12, 0.47, 0.66, 0.88)
        _FoamColor ("Écume", Color) = (1, 1, 1, 1)
        _DepthRange ("Profondeur de passage au bleu profond (m)", Float) = 0.35
        _ShoreFoam ("Largeur de l'écume des berges (m)", Float) = 0.1
        _FlowSpeed ("Vitesse du courant", Float) = 0.6
        _StreakScale ("Taille des traînées", Float) = 1.4
        _StreakAmount ("Quantité de traînées d'écume", Range(0, 1)) = 0.45
        _WaveHeight ("Hauteur des vagues (m)", Float) = 0.02
        _WaveLength ("Longueur des vagues (m)", Float) = 1.6
        _Specular ("Reflets du soleil", Range(0, 2)) = 0.7
        _Fall ("Cascade (0 ou 1)", Float) = 0
        [HDR] _BioColor ("Bioluminescence (la nuit)", Color) = (0.2, 1, 0.85, 1)
        _Biolum ("Force de la bioluminescence", Range(0, 3)) = 1
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
            Name "Water"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _FoamColor;
                float _DepthRange;
                float _ShoreFoam;
                float _FlowSpeed;
                float _StreakScale;
                float _StreakAmount;
                float _WaveHeight;
                float _WaveLength;
                float _Specular;
                float _Fall;
                half4 _BioColor;
                float _Biolum;
            CBUFFER_END

            // Réglés par le jeu pour toutes les eaux : la nuit (0 à 1), les remous (x, z, départ, force) et leur couleur.
            float _ArcheryNight;
            float4 _ArcheryRipples[8];
            float _ArcheryTime;
            half4 _ArcheryRippleColor;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 flow : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                float2 flow : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                float fogFactor : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Bruit de valeur lissé, entre 0 et 1.
            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Waves(float2 xz, float time)
            {
                float k = 6.2831853 / max(0.1, _WaveLength);
                return 0.5 * (sin(dot(xz, float2(0.8, 0.6)) * k + time * 1.3) + sin(dot(xz, float2(-0.5, 0.86)) * k * 1.7 - time * 1.1));
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                if (_Fall < 0.5)
                    positionWS.y += Waves(positionWS.xz, _Time.y) * _WaveHeight;

                output.positionWS = positionWS;
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.screenPos = ComputeScreenPos(output.positionHCS);
                output.uv = input.uv;
                output.flow = input.flow;
                output.fogFactor = ComputeFogFactor(output.positionHCS.z);
                return output;
            }

            // Traînées qui suivent le courant : deux couches décalées d'une demi-période, fondues l'une dans l'autre.
            // Chaque couche est invisible au moment où elle revient à son départ (poids nul), et le mélange garde
            // le même contraste tout le long du cycle : la boucle ne se voit pas.
            float Streaks(float2 p, float2 flow, float time)
            {
                float phase0 = frac(time);
                float phase1 = frac(time + 0.5);
                float w0 = 1.0 - abs(phase0 * 2.0 - 1.0);
                float w1 = 1.0 - w0;
                float n0 = Noise(p - flow * phase0 * 2.0) * 0.65 + Noise(p * 2.3 - flow * phase0 * 4.6) * 0.35;
                float n1 = Noise(p - flow * phase1 * 2.0 + 17.3) * 0.65 + Noise(p * 2.3 - flow * phase1 * 4.6 + 5.1) * 0.35;
                return ((n0 - 0.5) * w0 + (n1 - 0.5) * w1) * rsqrt(w0 * w0 + w1 * w1) + 0.5;
            }

            // Petits points lumineux, un sur deux cases environ, qui scintillent chacun à son rythme.
            float Specks(float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                float h = Hash(cell);
                float2 center = float2(Hash(cell + 3.1), Hash(cell + 7.7)) * 0.6 + 0.2;
                float twinkle = 0.55 + 0.45 * sin(_Time.y * (1.3 + h * 2.2) + h * 40.0);
                return step(0.5, h) * smoothstep(0.13, 0.0, length(f - center)) * twinkle;
            }

            // Comme les traînées : deux couches qui dérivent avec le courant, fondues l'une dans l'autre.
            float DriftingSpecks(float2 p, float2 flow, float time)
            {
                float phase0 = frac(time);
                float phase1 = frac(time + 0.5);
                float w0 = 1.0 - abs(phase0 * 2.0 - 1.0);
                return Specks(p - flow * phase0 * 2.0) * w0 + Specks(p - flow * phase1 * 2.0 + 11.7) * (1.0 - w0);
            }

            // Anneaux qui s'élargissent autour des ennemis dans l'eau (deux anneaux par remous, 2,2 s de vie).
            float Ripples(float2 xz)
            {
                float total = 0.0;
                [unroll]
                for (int i = 0; i < 8; i++)
                {
                    float4 ripple = _ArcheryRipples[i];
                    float age = _ArcheryTime - ripple.z;
                    if (ripple.w <= 0.0 || age < 0.0 || age > 2.2)
                        continue;

                    float fade = (1.0 - age / 2.2) * ripple.w;
                    float dist = length(xz - ripple.xy);
                    float ringA = (dist - age * 1.4) / 0.12;
                    float ringB = (dist - age * 0.85) / 0.1;
                    total += (exp(-ringA * ringA) + 0.6 * exp(-ringB * ringB)) * fade;
                }

                return total;
            }

            half4 frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Facettes plates : la normale vient des dérivées de la position.
                float3 normalWS = normalize(cross(ddy(input.positionWS), ddx(input.positionWS)));
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                if (dot(normalWS, viewDir) < 0.0)
                    normalWS = -normalWS;

                // Profondeur d'eau vue à travers la surface (0 près des berges).
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float waterDepth = max(0.0, sceneDepth - input.screenPos.w);

                float time = _Time.y * _FlowSpeed;
                half4 color;
                float foam;
                if (_Fall > 0.5)
                {
                    // Cascade : bandes verticales qui tombent, blanches en haut (rebord) et en bas.
                    float2 p = float2(input.uv.x * 2.5, input.uv.y * 0.9 - time * 2.2 * max(0.2, input.flow.y));
                    float bands = Noise(p) * 0.7 + Noise(p * float2(3.1, 1.7)) * 0.3;
                    color = lerp(_ShallowColor, _DeepColor, 0.35);
                    foam = smoothstep(0.55, 0.85, bands);
                    foam = max(foam, 1.0 - saturate(input.uv.y / 0.35));
                    foam = max(foam, saturate(1.0 - waterDepth / max(0.01, _ShoreFoam * 3.0)));
                    color.a = lerp(color.a, 1.0, 0.25);
                }
                else
                {
                    color = lerp(_ShallowColor, _DeepColor, saturate(waterDepth / max(0.01, _DepthRange)));
                    float speed = length(input.flow);
                    float streak = Streaks(input.uv * _StreakScale, input.flow * 1.5, time * 0.35);
                    foam = smoothstep(0.68, 0.86, streak) * _StreakAmount * saturate(speed);
                    foam = max(foam, 1.0 - saturate(waterDepth / max(0.01, _ShoreFoam)));
                }

                // Lumière principale : facettes éclairées, petits reflets du soleil.
                Light mainLight = GetMainLight();
                float diffuse = saturate(dot(normalWS, mainLight.direction));
                float3 halfDir = normalize(mainLight.direction + viewDir);
                // La nuit, le reflet de la lune est plus marqué.
                float night = saturate(_ArcheryNight);
                float specular = pow(saturate(dot(normalWS, halfDir)), 64.0) * _Specular * (1.0 + night);
                float3 ambient = SampleSH(normalWS);

                float3 rgb = color.rgb * (ambient * 0.6 + mainLight.color * (0.45 + 0.55 * diffuse));
                rgb += mainLight.color * specular;
                rgb = lerp(rgb, _FoamColor.rgb * (ambient * 0.5 + mainLight.color * 0.7), foam);
                float alpha = saturate(lerp(color.a, _FoamColor.a, foam * 0.85) + specular * 0.3);

                if (_Fall < 0.5)
                {
                    // Bioluminescence : des points qui dérivent, une lueur le long des berges, un fond très léger.
                    float glow = 0.0;
                    if (night > 0.001)
                    {
                        float specks = DriftingSpecks(input.positionWS.xz * 2.2, input.flow * 1.5, time * 0.35);
                        float shore = 1.0 - saturate(waterDepth / max(0.01, _ShoreFoam * 4.0));
                        glow = (specks * 1.5 + shore * 0.35 + 0.06) * night * _Biolum;
                        rgb += _BioColor.rgb * glow;
                    }

                    // Remous : un peu d'écume le jour, des anneaux lumineux la nuit.
                    float ripples = Ripples(input.positionWS.xz);
                    rgb += _ArcheryRippleColor.rgb * ripples * (0.25 + 1.25 * night);
                    alpha = saturate(alpha + ripples * 0.3 + glow * 0.1);
                }

                rgb = MixFog(rgb, input.fogFactor);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
