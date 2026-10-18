// Lueur ajoutée à l'image (runes des emplacements de construction, veines de l'antre, arc-en-ciel…) : non éclairée,
// de la couleur des sommets (alpha = force), plus douce sur les bords (uv.x de 0 à 1 en travers d'une bande).
// _Intensity règle l'éclat (au-dessus de 1, le bloom la fait rayonner) ; le brouillard l'efface au loin.
Shader "Archery/Glow"
{
    Properties
    {
        [HDR] _Color ("Couleur", Color) = (1, 1, 1, 1)
        _Intensity ("Éclat", Float) = 1
        _Softness ("Douceur des bords", Range(0.01, 0.5)) = 0.35
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
            Name "Glow"
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
                half _Softness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float fogFactor : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionHCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                half edge = smoothstep(0.0, _Softness, input.uv.x) * smoothstep(0.0, _Softness, 1.0 - input.uv.x);
                half3 color = input.color.rgb * _Color.rgb * (_Intensity * input.color.a * edge);
                // Ajoutée à l'image : au loin, elle s'efface vers le noir plutôt que vers la couleur du brouillard.
                color = MixFogColor(color, half3(0, 0, 0), input.fogFactor);
                return half4(color, 0);
            }
            ENDHLSL
        }
    }
}
