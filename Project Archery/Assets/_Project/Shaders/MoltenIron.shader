// Fonte en fusion du Colosse de forge (2 novembre au soir ; Docs/Refonte_TowerDefense.md, 12.47) : comme la lave de l'arène
// (Lava.shader), non éclairée, elle brille d'elle-même et ses veines coulent le long des coordonnées de texture (vers le bas
// pour le jet, vers la forêt pour la coulée) ; mais elle ne dépend pas des réglages de l'arène, et chaque morceau refroidit à
// son rythme. Réglés par morceau (MaterialPropertyBlock) :
// _Cool : 0 = blanche de chaleur, 1 = croûte noire (la coulée qui refroidit, le sol rougi sous le marteau) ;
// _Glow : l'éclat (le bloom la fait rayonner) ;
// _Edges : la fonte est plus froide sur les bords de la bande (la coulée : 1 ; les veines du géant : 0).
Shader "Archery/MoltenIron"
{
    Properties
    {
        [HDR] _HotColor ("Couleur des veines", Color) = (3, 2, 0.8, 1)
        [HDR] _MidColor ("Couleur de la fonte", Color) = (2, 0.55, 0.08, 1)
        _ColdColor ("Couleur de la croûte", Color) = (0.12, 0.04, 0.03, 1)
        _Glow ("Éclat", Float) = 1
        _Speed ("Vitesse d'écoulement", Float) = 0.35
        _Scale ("Taille des motifs", Float) = 2
        _Cool ("Refroidie (0 = brûlante, 1 = croûte)", Range(0, 1)) = 0
        _Edges ("Bords plus froids", Range(0, 1)) = 0
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
            Name "MoltenIron"
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _HotColor;
                half4 _MidColor;
                half4 _ColdColor;
                half _Glow;
                half _Speed;
                half _Scale;
                half _Cool;
                half _Edges;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

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

            float Fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.55;
                for (int octave = 0; octave < 3; octave++)
                {
                    value += Noise(p) * amplitude;
                    p = p * 2.03 + 17.1;
                    amplitude *= 0.5;
                }
                return value;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Elle coule le long des UV ; un second motif, en coordonnées du monde, casse les répétitions.
                float time = _Time.y * _Speed;
                float2 flow = input.uv * _Scale + float2(0.0, -time * 2.0);
                float2 drift = input.positionWS.xz * 0.3 + float2(time * 0.6, time * 0.4);
                float n = Fbm(flow + Fbm(drift) * 1.5);

                // En refroidissant, la croûte gagne : les veines claires se font rares, puis disparaissent.
                float cool = saturate(_Cool + _Edges * pow(abs(input.uv.x * 2.0 - 1.0), 3.0) * 0.6);
                float hot = n + 0.25 - cool * 0.9;
                half3 color = lerp(_ColdColor.rgb, _MidColor.rgb, smoothstep(0.32, 0.58, hot));
                color = lerp(color, _HotColor.rgb, smoothstep(0.62, 0.82, hot));

                // Elle palpite.
                color *= 0.85 + 0.15 * sin(_Time.y * 2.3 + input.positionWS.x * 0.3 + input.positionWS.z * 0.2);
                color *= _Glow;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // Profondeur seule (prépasse de profondeur d'URP) : sans elle, la fonte pourrait disparaître.
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _HotColor;
                half4 _MidColor;
                half4 _ColdColor;
                half _Glow;
                half _Speed;
                half _Scale;
                half _Cool;
                half _Edges;
            CBUFFER_END

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
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }
    }
}
