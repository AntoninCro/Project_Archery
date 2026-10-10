// Anneau de timing : un anneau épais, vide au centre, avec de l'extérieur vers l'intérieur des bandes rouge (raté),
// orange (bon), verte (parfait) et vert pastel (très bon), et le cercle d'approche par-dessus.
// Tous les rayons sont en fraction du rayon de l'anneau (0 = centre, 1 = bord extérieur).
Shader "Archery/TimingRing"
{
    Properties
    {
        _InnerRadius ("Bord intérieur (centre vide)", Range(0, 1)) = 0.3
        _PerfectInner ("Bord intérieur du vert", Range(0, 1)) = 0.44
        _PerfectOuter ("Bord extérieur du vert", Range(0, 1)) = 0.56
        _GoodOuter ("Bord extérieur de l'orange", Range(0, 1)) = 0.7
        _Approach ("Rayon du cercle d'approche", Range(0, 1)) = 1
        _ApproachThickness ("Épaisseur du cercle d'approche", Range(0, 0.2)) = 0.05
        _Alpha ("Opacité", Range(0, 1)) = 1
        _RedColor ("Rouge (raté)", Color) = (0.86, 0.16, 0.12, 0.75)
        _GoodColor ("Orange (bon)", Color) = (1, 0.5, 0.08, 0.9)
        _PerfectColor ("Vert (parfait)", Color) = (0.3, 0.92, 0.35, 1)
        _HeldColor ("Vert pastel (très bon)", Color) = (0.62, 0.95, 0.72, 0.95)
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

            // L'anneau n'occupe pas tout le quad, pour que le cercle d'approche ne soit jamais coupé.
            #define DISC_SCALE 0.92

            CBUFFER_START(UnityPerMaterial)
                float _InnerRadius;
                float _PerfectInner;
                float _PerfectOuter;
                float _GoodOuter;
                float _Approach;
                float _ApproachThickness;
                float _Alpha;
                half4 _RedColor;
                half4 _GoodColor;
                half4 _PerfectColor;
                half4 _HeldColor;
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

                // Du centre vers le bord : vert pastel, vert, orange, rouge.
                half4 color = _HeldColor;
                color = lerp(color, _PerfectColor, smoothstep(_PerfectInner - aa, _PerfectInner + aa, r));
                color = lerp(color, _GoodColor, smoothstep(_PerfectOuter - aa, _PerfectOuter + aa, r));
                color = lerp(color, _RedColor, smoothstep(_GoodOuter - aa, _GoodOuter + aa, r));

                // Teinte du résultat après le tir.
                color.rgb = lerp(color.rgb, _FlashColor.rgb, _FlashColor.a * 0.65);

                // Un anneau épais : vide au centre, net sur ses deux bords. Le cercle d'approche passe par-dessus.
                float ring = smoothstep(_InnerRadius - aa, _InnerRadius + aa, r) * (1.0 - smoothstep(1.0 - aa, 1.0 + aa, r));
                color.a *= ring;
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
