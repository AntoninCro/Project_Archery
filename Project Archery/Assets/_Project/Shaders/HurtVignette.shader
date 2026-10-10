// Voile rouge autour de la vision quand le joueur a perdu des PV. Dessiné sur une sphère centrée sur la tête :
// la couleur dépend de l'angle entre la direction du regard et l'avant de la tête, ce qui reste juste pour les deux yeux.
// Il passe par-dessus la scène, mais sous le fondu au noir.
Shader "Archery/HurtVignette"
{
    Properties
    {
        _BaseColor ("Couleur", Color) = (0.75, 0, 0, 1)
        _Intensity ("Intensité", Range(0, 1)) = 0
        _Start ("Début du voile (1 - cosinus de l'angle depuis le centre)", Range(0, 1)) = 0.42
        _Softness ("Largeur du dégradé", Range(0.01, 1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Overlay+50"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "HurtVignette"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Intensity;
                float _Start;
                float _Softness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 directionOS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.directionOS = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // 0 au centre de la vue, 1 sur le côté (90°).
                float3 direction = normalize(input.directionOS);
                float t = 1.0 - direction.z;
                float amount = smoothstep(_Start, _Start + _Softness, t) * _Intensity;
                return half4(_BaseColor.rgb, _BaseColor.a * amount);
            }
            ENDHLSL
        }
    }
}
