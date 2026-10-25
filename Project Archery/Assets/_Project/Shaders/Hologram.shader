// Les hologrammes du toit cyberpunk (décor 3, 24 octobre) : une lumière ajoutée à l'image, traversée de lignes qui
// défilent vers le haut, qui vacille par à-coups comme un affichage qui bugue, avec une bande plus vive qui monte de temps
// en temps. Sur un quad, « Cadre » allume ses bords (un écran) ; sur une forme (le mannequin géant), « Contour » ne
// garde que ses bords vus de biais. Au loin, il s'efface vers le noir (il est ajouté), pas vers la couleur du brouillard.
Shader "Archery/Hologram"
{
    Properties
    {
        [HDR] _Color ("Couleur", Color) = (0.2, 1.4, 2, 1)
        _Intensity ("Éclat", Float) = 1
        _ScanDensity ("Lignes par mètre", Float) = 5
        _ScanSpeed ("Défilement des lignes (m/s)", Float) = 0.5
        _ScanDepth ("Force des lignes (0 à 1)", Range(0, 1)) = 0.55
        _Flicker ("Vacillement (0 à 1)", Range(0, 1)) = 0.35
        _Frame ("Cadre (quads : bords allumés)", Range(0, 4)) = 0
        _Fresnel ("Contour (0 = plein, 1 = seulement les bords)", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Hologram"
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                float _ScanDensity;
                float _ScanSpeed;
                half _ScanDepth;
                half _Flicker;
                half _Frame;
                half _Fresnel;
            CBUFFER_END

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
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                float seed : TEXCOORD4;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Nombre pseudo-aléatoire entre 0 et 1 (hash sans sinus, stable sur tous les GPU).
            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.positionWS = positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionHCS.z);
                // Chaque hologramme vacille à son rythme : une graine tirée de sa place.
                float3 origin = TransformObjectToWorld(float3(0, 0, 0));
                output.seed = Hash11(dot(origin, float3(0.731, 0.317, 0.513)));
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Les lignes, dans le monde (elles ne s'étirent pas avec la taille de l'hologramme).
                float lines = sin((input.positionWS.y - _Time.y * _ScanSpeed) * _ScanDensity * 6.2831853);
                half scan = 1.0 - _ScanDepth * (0.5 + 0.5 * lines);

                // Une bande plus vive qui monte, de temps en temps.
                float band = frac(input.positionWS.y * 0.06 - _Time.y * 0.12 + input.seed);
                half bright = 1.0 + 1.2 * smoothstep(0.94, 1.0, band);

                // Le vacillement : un tirage douze fois par seconde ; une fois sur dix, il s'éteint presque.
                float tick = floor(_Time.y * 12.0) + input.seed * 97.0;
                half noise = Hash11(tick);
                half flicker = 1.0 - _Flicker * (0.25 * noise + 0.65 * step(0.9, noise));

                // Le cadre d'un écran : ses bords allumés (le quad, uv de 0 à 1).
                float2 uv = input.uv;
                float edge = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
                half frame = 1.0 + _Frame * (1.0 - smoothstep(0.0, 0.025, edge));

                // Le contour d'une forme : vue de face, elle s'efface ; vue de biais, elle brille.
                float3 view = normalize(GetWorldSpaceViewDir(input.positionWS));
                half facing = abs(dot(normalize(input.normalWS), view));
                half rim = lerp(1.0, saturate((1.0 - facing) * (1.0 - facing) * 2.5 + 0.12), _Fresnel);

                half3 color = _Color.rgb * (_Intensity * scan * bright * flicker * frame * rim);
                // Ajouté à l'image : au loin, il s'efface vers le noir.
                color = MixFogColor(color, half3(0, 0, 0), input.fogFactor);
                return half4(color, 0);
            }
            ENDHLSL
        }
    }
}
