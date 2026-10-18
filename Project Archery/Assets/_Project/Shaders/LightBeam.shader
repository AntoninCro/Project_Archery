// Rayon de lumière vertical qui signale un coffre de loin (GDD, section 13), et rayons du soleil, de la tour, de l'arène.
// À mettre sur un cylindre étiré : le centre du rayon brille, ses bords et son sommet s'effacent, et il pulse doucement.
// Les couvercles du cylindre ne sont pas dessinés, et le rayon s'efface en douceur là où il rentre dans le décor
// (sol, rochers) : pas de bord net. « Fondu du pied » l'efface aussi à sa base (rayons du soleil plantés dans le sol).
Shader "Archery/LightBeam"
{
    Properties
    {
        [HDR] _BaseColor ("Couleur", Color) = (1, 0.82, 0.35, 0.8)
        _Intensity ("Intensité", Float) = 1.5
        _EdgeSoftness ("Douceur des bords", Range(0.5, 8)) = 2.5
        _TopFade ("Hauteur du fondu (0 à 1)", Range(0.05, 1)) = 0.9
        _PulseSpeed ("Vitesse de pulsation", Float) = 2
        _BottomFade ("Fondu du pied (0 à 1)", Range(0, 1)) = 0
        _SoftDistance ("Fondu contre le décor (m)", Float) = 0.6
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
            Name "LightBeam"
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _Intensity;
                half _EdgeSoftness;
                half _TopFade;
                half _PulseSpeed;
                half _BottomFade;
                half _SoftDistance;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                float eyeDepth : TEXCOORD3;
                half side : TEXCOORD4;      // 1 sur le tour du cylindre, 0 sur ses couvercles
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                output.uv = input.uv;
                output.eyeDepth = -TransformWorldToView(positionWS).z;
                output.side = abs(input.normalOS.y) < 0.5 ? 1.0 : 0.0;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Face à la caméra, le rayon est plein ; vu de biais (ses bords), il s'efface.
                float3 normal = normalize(input.normalWS);
                float3 view = normalize(input.viewDirWS);
                half facing = pow(saturate(abs(dot(normal, view))), _EdgeSoftness);

                // Il s'efface vers le haut, et pulse doucement.
                half height = saturate(1.0 - input.uv.y / max(_TopFade, 0.01));
                half pulse = 0.85 + 0.15 * sin(_Time.y * _PulseSpeed);

                // Les couvercles du cylindre ne se voient pas : un rayon n'a pas de fond.
                clip(input.side - 0.5);

                // Il naît en douceur à sa base (si « Fondu du pied »), et s'efface là où il rentre dans le décor.
                half foot = _BottomFade > 0.001 ? smoothstep(0.0, _BottomFade, input.uv.y) : 1.0;
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionHCS);
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                half soft = saturate((sceneDepth - input.eyeDepth) / max(_SoftDistance, 0.01));

                half alpha = saturate(facing * height * foot * soft * pulse * _BaseColor.a);
                return half4(_BaseColor.rgb * _Intensity, alpha);
            }
            ENDHLSL
        }
    }
}
