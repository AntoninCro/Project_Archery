// Des 0 et des 1 verts (24 octobre) : un ennemi tué par une faille du katana se défait en chiffres de la matrice. Pour un
// système de particules : chaque particule est un chiffre, 0 ou 1 tiré au sort, qui change de temps en temps et grésille,
// de la couleur des particules (alpha = effacement). Le script donne aux particules le flux StableRandom.x (TEXCOORD0.z) ;
// sans lui, tous les chiffres sont des 0.
Shader "Archery/MatrixDigits"
{
    Properties
    {
        _Intensity ("Éclat", Float) = 2.2
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
            Name "MatrixDigits"
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Intensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float3 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color : COLOR;
                float3 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float DigitHash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // Un chiffre dans sa case (0 à 1 dans les deux sens), comme dans les failles (MatrixTear).
            float Digit(float2 p, float one, float soft)
            {
                float zero = abs(length((p - 0.5) / float2(0.3, 0.4)) - 1.0) * 0.3;
                float bar = max(abs(p.x - 0.55) - 0.07, abs(p.y - 0.5) - 0.38);
                float2 flagA = float2(0.55, 0.88);
                float2 flagB = float2(0.32, 0.7);
                float2 ba = flagB - flagA;
                float h = saturate(dot(p - flagA, ba) / dot(ba, ba));
                float flag = length(p - flagA - ba * h) - 0.06;
                float foot = max(abs(p.x - 0.55) - 0.2, abs(p.y - 0.12) - 0.05);
                float d = lerp(zero - 0.05, min(min(bar, flag), foot), one);
                return 1.0 - smoothstep(-soft, soft, d);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Chaque particule a son chiffre, qui change de temps en temps, et grésille.
                float random = input.uv.z;
                float change = floor(_Time.y * (3.0 + 5.0 * random) + random * 11.0);
                float one = step(0.5, DigitHash(float2(random * 91.3, change)));
                float flicker = 0.7 + 0.3 * DigitHash(float2(random * 17.1, floor(_Time.y * 24.0)));
                float2 p = input.uv.xy;
                float soft = max(fwidth(p.x), fwidth(p.y)) * 1.5 + 0.02;
                float glyph = Digit(p, one, soft);
                return half4(input.color.rgb * _Intensity * flicker, glyph * input.color.a);
            }
            ENDHLSL
        }
    }
}
