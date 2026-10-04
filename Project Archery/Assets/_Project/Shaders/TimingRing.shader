// Anneau de timing : bandes concentriques symétriques autour du vert (parfait), puis orange (bon), puis rouge (raté),
// et le cercle d'approche. Toutes les largeurs sont en fraction du rayon du disque (0 = centre, 1 = bord).
Shader "Archery/TimingRing"
{
    Properties
    {
        _GoldCenter ("Rayon du vert", Range(0, 1)) = 0.5
        _GoldHalfWidth ("Demi-largeur du vert", Range(0, 0.5)) = 0.06
        _GoodWidth ("Largeur de l'orange", Range(0, 0.5)) = 0.14
        _Approach ("Rayon du cercle d'approche", Range(0, 1)) = 1
        _ApproachThickness ("Épaisseur du cercle d'approche", Range(0, 0.2)) = 0.05
        _Alpha ("Opacité", Range(0, 1)) = 1
        _RedColor ("Rouge (raté)", Color) = (0.86, 0.16, 0.12, 0.75)
        _GoodColor ("Orange (bon)", Color) = (1, 0.5, 0.08, 0.9)
        _PerfectColor ("Vert (parfait)", Color) = (0.3, 0.92, 0.35, 1)
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
                float _GoodWidth;
                float _Approach;
                float _ApproachThickness;
                float _Alpha;
                half4 _RedColor;
                half4 _GoodColor;
                half4 _PerfectColor;
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

                // Bandes symétriques autour du vert : rouge, orange, vert, orange, rouge.
                float d = abs(r - _GoldCenter);
                float perfectEdge = _GoldHalfWidth;
                float goodEdge = perfectEdge + _GoodWidth;

                half4 color = _RedColor;
                color = lerp(color, _GoodColor, 1.0 - smoothstep(goodEdge - aa, goodEdge + aa, d));
                color = lerp(color, _PerfectColor, 1.0 - smoothstep(perfectEdge - aa, perfectEdge + aa, d));

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
