// Le corps de l'arc cosmique : une fenêtre sur l'espace. La nébuleuse n'est pas peinte sur l'arc : elle est vue à
// travers lui, très loin, dans la direction du regard (comme un ciel), donc elle glisse quand on tourne l'arc et reste
// juste dans chaque œil du casque. Ses étoiles scintillent ; un liseré violet souligne la forme de l'arc.
Shader "Archery/CosmicSurface"
{
    Properties
    {
        _MainTex ("Nébuleuse (en boucle)", 2D) = "black" {}
        _Tiling ("Répétitions autour du regard (entier)", Float) = 2
        _Brightness ("Éclat de la nébuleuse", Float) = 1.8
        _Drift ("Dérive (tours par seconde)", Float) = 0.004
        _Twinkle ("Scintillement des étoiles", Range(0, 1)) = 0.7
        [HDR] _RimColor ("Liseré", Color) = (1.2, 0.7, 2.4, 1)
        _RimPower ("Finesse du liseré", Float) = 3
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
            Name "CosmicSurface"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Tiling;
                float _Brightness;
                float _Drift;
                float _Twinkle;
                half4 _RimColor;
                float _RimPower;
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
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                const float tau = 6.2831853;
                float3 toCamera = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 look = -toCamera;

                // La direction du regard en coordonnées de ciel ; la texture boucle, et _Tiling entier efface la couture.
                float2 uv = float2(atan2(look.x, look.z) / tau, asin(clamp(look.y, -1.0, 1.0)) / (tau * 0.5));
                uv = uv * _Tiling + float2(_Time.y * _Drift, 0.0);
                half3 sky = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, uv, 0).rgb * _Brightness;

                // Les points brillants (les étoiles) scintillent, chacun à son rythme.
                float2 cell = floor(uv * 512.0);
                float seed = frac(sin(dot(cell, float2(12.9898, 78.233))) * 43758.5453);
                float wave = 0.5 + 0.5 * sin(_Time.y * (2.0 + seed * 3.0) + seed * tau);
                float luminance = dot(sky, float3(0.299, 0.587, 0.114));
                sky *= lerp(1.0, 0.35 + 1.3 * wave, _Twinkle * saturate(luminance * 2.0 - 0.4));

                // Le liseré : plus fort sur les bords, là où la surface fuit le regard.
                float facing = saturate(dot(normalize(input.normalWS), toCamera));
                half3 rim = _RimColor.rgb * pow(1.0 - facing, _RimPower);
                return half4(sky + rim, 1.0);
            }
            ENDHLSL
        }
    }
}
