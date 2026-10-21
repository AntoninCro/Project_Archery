// Lueur additive du laser de l'arc cosmique : le rayon (LineRenderer) et ses particules. Pas de texture : la forme
// vient des UV. _Round = 0 : un rayon, adouci sur sa largeur (uv.y) ; _Round = 1 : une particule ronde et floue.
// La couleur (HDR) et la couleur des sommets donnent l'éclat : au-dessus de 1, le bloom le fait rayonner.
Shader "Archery/LaserGlow"
{
    Properties
    {
        [HDR] _BaseColor ("Couleur", Color) = (1, 1, 1, 1)
        _Round ("Rond (particules)", Float) = 0
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
            Name "LaserGlow"
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Round;
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
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 centered = input.uv * 2.0 - 1.0;
                float distanceToAxis = _Round > 0.5 ? length(centered) : abs(centered.y);
                float glow = saturate(1.0 - distanceToAxis);
                glow *= glow;
                half3 color = input.color.rgb * _BaseColor.rgb * (input.color.a * _BaseColor.a * glow);
                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
