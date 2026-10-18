// Lave de l'arène du boss final : non éclairée, elle brille d'elle-même. Des veines claires coulent le long des
// coordonnées de texture (vers le bas pour les coulées, autour de l'anneau pour le pied des parois).
// _Glow règle son éclat : 0 = éteinte, 1 et plus = brûlante (le bloom la fait rayonner). Il est multiplié par
// _ArenaLavaGlow, réglé pour toute l'arène par l'Arena Director (0 pendant l'intro dans le noir).
// Avec _FallReveal = 1 (coulées le long des parois), seule la partie au-dessus de _LavaFallCutoffY (hauteur dans le
// monde, réglée pour toute l'arène) est visible : la coulée « descend » quand cette hauteur baisse.
Shader "Archery/Lava"
{
    Properties
    {
        [HDR] _HotColor ("Couleur des veines", Color) = (1, 0.8, 0.3, 1)
        [HDR] _MidColor ("Couleur de la lave", Color) = (1, 0.28, 0.04, 1)
        _ColdColor ("Couleur de la croûte", Color) = (0.22, 0.03, 0.02, 1)
        _Glow ("Éclat", Float) = 1
        _Speed ("Vitesse d'écoulement", Float) = 0.18
        _Scale ("Taille des motifs", Float) = 2.5
        [ToggleUI] _FallReveal ("Coulée (se dévoile du haut vers le bas)", Float) = 0
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
            Name "Lava"
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
                half _FallReveal;
            CBUFFER_END

            // Réglages communs à toute l'arène (Arena Director).
            float _ArenaLavaGlow;
            float _LavaFallCutoffY;

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

                // La lave coule le long des UV ; un second motif, en coordonnées du monde, casse les répétitions.
                float time = _Time.y * _Speed;
                float2 flow = input.uv * _Scale + float2(0.0, -time * 2.0);
                float2 drift = input.positionWS.xz * 0.35 + float2(time * 0.6, time * 0.4);
                float n = Fbm(flow + Fbm(drift) * 1.5);

                half3 color = lerp(_ColdColor.rgb, _MidColor.rgb, smoothstep(0.32, 0.58, n));
                color = lerp(color, _HotColor.rgb, smoothstep(0.62, 0.82, n));

                // Respiration lente de toute la surface.
                color *= 0.85 + 0.15 * sin(_Time.y * 1.7 + input.positionWS.x * 0.2);
                if (_FallReveal > 0.5)
                {
                    // Coulée : rien sous la hauteur de coupe ; le front de la coulée est plus chaud.
                    float above = input.positionWS.y - _LavaFallCutoffY;
                    clip(above);
                    color = lerp(color, _HotColor.rgb * 1.5, saturate(1.0 - above / 0.6));
                }

                color *= _Glow * _ArenaLavaGlow;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        // Profondeur seule (prépasse de profondeur d'URP) : sans elle, la lave pourrait disparaître.
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
                half _FallReveal;
            CBUFFER_END

            // Réglages communs à toute l'arène (Arena Director).
            float _ArenaLavaGlow;
            float _LavaFallCutoffY;

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                // Une coulée cachée ne doit pas masquer la paroi derrière elle.
                if (_FallReveal > 0.5)
                    clip(input.positionWS.y - _LavaFallCutoffY);
                return 0;
            }
            ENDHLSL
        }
    }
}
