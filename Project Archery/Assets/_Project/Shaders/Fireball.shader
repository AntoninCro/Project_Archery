// Boule de feu du dragon : non éclairée, elle brille d'elle-même. Un bruit en 3D, dans le repère de la sphère, monte
// et tourbillonne : des flammes jaunes et blanches sur un fond orange, avec des veines rouge sombre. Le bord est plus
// sombre et plus rouge, comme une boule de feu vue de près. _Glow règle son éclat (le bloom la fait rayonner).
Shader "Archery/Fireball"
{
    Properties
    {
        [HDR] _HotColor ("Couleur des flammes", Color) = (1.6, 1.25, 0.6, 1)
        [HDR] _MidColor ("Couleur du feu", Color) = (1.4, 0.45, 0.06, 1)
        _DarkColor ("Couleur des veines", Color) = (0.35, 0.04, 0.02, 1)
        _Glow ("Éclat", Float) = 2.5
        _Speed ("Vitesse des flammes", Float) = 1.2
        _Scale ("Taille des motifs", Float) = 3.2
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
            Name "Fireball"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _HotColor;
                half4 _MidColor;
                half4 _DarkColor;
                half _Glow;
                half _Speed;
                half _Scale;
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
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float Noise(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(Hash(i), Hash(i + float3(1, 0, 0)), u.x),
                                 lerp(Hash(i + float3(0, 1, 0)), Hash(i + float3(1, 1, 0)), u.x), u.y),
                            lerp(lerp(Hash(i + float3(0, 0, 1)), Hash(i + float3(1, 0, 1)), u.x),
                                 lerp(Hash(i + float3(0, 1, 1)), Hash(i + float3(1, 1, 1)), u.x), u.y), u.z);
            }

            float Fbm(float3 p)
            {
                float value = 0.0;
                float amplitude = 0.55;
                for (int octave = 0; octave < 4; octave++)
                {
                    value += Noise(p) * amplitude;
                    p = p * 2.07 + 13.7;
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
                output.positionOS = input.positionOS.xyz;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Des flammes qui montent, déformées par un second bruit plus lent : elles tourbillonnent.
                float time = _Time.y * _Speed;
                float3 p = input.positionOS * _Scale;
                float3 warp = float3(Fbm(p * 0.6 + time * 0.35), Fbm(p * 0.6 - time * 0.3 + 5.2), 0.0);
                float n = Fbm(p + warp * 1.6 + float3(0.0, -time, 0.0));

                // Le centre (face à nous) est plus chaud que le bord.
                float3 normal = normalize(input.normalWS);
                float3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                float facing = saturate(dot(normal, view));
                float heat = saturate(n * 1.25 + facing * 0.35 - 0.2);

                half3 color = lerp(_DarkColor.rgb, _MidColor.rgb, smoothstep(0.2, 0.5, heat));
                color = lerp(color, _HotColor.rgb, smoothstep(0.55, 0.85, heat));
                color *= _Glow * (0.85 + 0.15 * sin(_Time.y * 9.0 + n * 6.0));
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // Profondeur seule (prépasse de profondeur d'URP) : sans elle, la boule pourrait disparaître.
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _HotColor;
                half4 _MidColor;
                half4 _DarkColor;
                half _Glow;
                half _Speed;
                half _Scale;
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
