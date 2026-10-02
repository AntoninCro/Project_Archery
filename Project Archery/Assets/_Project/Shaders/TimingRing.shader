// Anneau de timing : bandes concentriques symétriques autour du doré + cercle d'approche.
// Toutes les largeurs sont en fraction du rayon du disque (0 = centre, 1 = bord).
Shader "Archery/TimingRing"
{
    Properties
    {
        _GoldCenter ("Rayon du doré", Range(0, 1)) = 0.5
        _GoldHalfWidth ("Demi-largeur du doré", Range(0, 0.5)) = 0.06
        _GreenWidth ("Largeur du vert", Range(0, 0.5)) = 0.08
        _OrangeWidth ("Largeur de l'orange", Range(0, 0.5)) = 0.1
        _Approach ("Rayon du cercle d'approche", Range(0, 1)) = 1
        _ApproachThickness ("Épaisseur du cercle d'approche", Range(0, 0.2)) = 0.05
        _Alpha ("Opacité", Range(0, 1)) = 1
        _RedColor ("Rouge", Color) = (0.86, 0.16, 0.12, 0.75)
        _OrangeColor ("Orange", Color) = (1, 0.55, 0.1, 0.85)
        _GreenColor ("Vert", Color) = (0.3, 0.82, 0.3, 0.9)
        _GoldColor ("Doré", Color) = (1, 0.82, 0.15, 1)
        _ApproachColor ("Cercle d'approche", Color) = (1, 1, 1, 1)
        _FlashColor ("Couleur du résultat", Color) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+10"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "TimingRing"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Le disque n'occupe pas tout le quad, pour que le cercle d'approche ne soit jamais coupé.
            #define DISC_SCALE 0.92

            CBUFFER_START(UnityPerMaterial)
                float _GoldCenter;
                float _GoldHalfWidth;
                float _GreenWidth;
                float _OrangeWidth;
                float _Approach;
                float _ApproachThickness;
                float _Alpha;
                half4 _RedColor;
                half4 _OrangeColor;
                half4 _GreenColor;
                half4 _GoldColor;
                half4 _ApproachColor;
                half4 _FlashColor;
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
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float r = length(input.uv * 2.0 - 1.0) / DISC_SCALE;
                float aa = max(fwidth(r), 1e-4);

                // Bandes symétriques autour du doré : rouge, orange, vert, doré, vert, orange, rouge.
                float d = abs(r - _GoldCenter);
                float goldEdge = _GoldHalfWidth;
                float greenEdge = goldEdge + _GreenWidth;
                float orangeEdge = greenEdge + _OrangeWidth;

                half4 color = _RedColor;
                color = lerp(color, _OrangeColor, 1.0 - smoothstep(orangeEdge - aa, orangeEdge + aa, d));
                color = lerp(color, _GreenColor, 1.0 - smoothstep(greenEdge - aa, greenEdge + aa, d));
                color = lerp(color, _GoldColor, 1.0 - smoothstep(goldEdge - aa, goldEdge + aa, d));

                // Teinte du résultat après le tir.
                color.rgb = lerp(color.rgb, _FlashColor.rgb, _FlashColor.a * 0.65);

                // Disque, puis cercle d'approche par-dessus.
                color.a *= 1.0 - smoothstep(1.0 - aa, 1.0 + aa, r);
                float halfThickness = _ApproachThickness * 0.5;
                float approach = 1.0 - smoothstep(halfThickness - aa, halfThickness + aa, abs(r - _Approach));
                color = lerp(color, _ApproachColor, approach);

                color.a *= _Alpha;
                return color;
            }
            ENDHLSL
        }
    }
}
