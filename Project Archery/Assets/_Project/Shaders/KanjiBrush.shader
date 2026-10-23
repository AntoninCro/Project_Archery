// Le symbole à tracer au katana (22 octobre) : chaque trait est un coup de pinceau lumineux, plein au milieu, effiloché
// sur les bords et vers la fin du trait (des poils de pinceau secs). Une lueur parcourt le trait du début à la fin pour
// montrer dans quel sens le tracer.
// UV0 : X de 0 (début du trait) à 1 (fin), Y en travers (-1 et 1 sur les bords). Couleur des sommets : teinte et alpha.
// _Pulse (bloc de propriétés) : la position de la lueur le long du trait (négatif : pas de lueur).
Shader "Archery/KanjiBrush"
{
    Properties
    {
        [HDR] _Color ("Encre", Color) = (1.6, 1.8, 1.7, 0.6)
        [HDR] _PulseColor ("Lueur", Color) = (1.5, 3.2, 2.0, 1)
        _Pulse ("Position de la lueur", Float) = -1
        _Bristles ("Poils", Float) = 18
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "KanjiBrush"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _PulseColor;
                float _Pulse;
                float _Bristles;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float BrushHash(float2 p)
            {
                return frac(sin(dot(p, float2(41.3, 289.1))) * 43758.5453);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float along = input.uv.x;
                float across = abs(input.uv.y);

                // Les poils : chaque bande en travers s'arrête à sa propre longueur vers la fin du trait, et les bords
                // sont plus clairsemés que le milieu.
                float band = floor((input.uv.y * 0.5 + 0.5) * _Bristles);
                float reach = lerp(0.72, 1.02, BrushHash(float2(band, 3.1)));
                float bristle = smoothstep(reach, reach - 0.08, along);
                float gaps = lerp(1.0, step(0.3, BrushHash(float2(band, floor(along * 9.0)))), smoothstep(0.35, 1.0, across));
                float body = smoothstep(1.0, 0.72, across) * bristle * gaps;

                // La lueur qui montre le sens du trait.
                float pulse = _Pulse >= 0.0 ? exp(-pow((along - _Pulse) / 0.07, 2.0)) : 0.0;

                half3 color = _Color.rgb * input.color.rgb + _PulseColor.rgb * pulse;
                half alpha = saturate(body * (_Color.a + pulse * 0.6)) * input.color.a;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
