// Shader transparent non éclairé qui utilise la couleur des sommets (traînées des flèches).
// _NightGlow : la nuit (_ArcheryNight, réglé par le ciel) et dans l'arène du dragon (_ArcheryArena), la couleur est
// multipliée par 1 + _NightGlow : les traînées des flèches deviennent des traceurs lumineux (le bloom les fait rayonner).
Shader "Archery/UnlitVertexColor"
{
    Properties
    {
        _BaseColor ("Couleur", Color) = (1, 1, 1, 1)
        _NightGlow ("Éclat la nuit", Float) = 0
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
            Name "UnlitVertexColor"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _NightGlow;
            CBUFFER_END

            float _ArcheryNight;
            float _ArcheryArena;

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color * _BaseColor;
                output.color.rgb *= 1.0 + _NightGlow * saturate(max(_ArcheryNight, _ArcheryArena));
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return input.color;
            }
            ENDHLSL
        }
    }
}
