// La déchirure du katana : comme si la lame déchirait la matrice du jeu. Une fente aux bords déchiquetés (plus étroite aux
// deux bouts), qui tressaute par à-coups ; ses bords brillent, cyan d'un côté et magenta de l'autre (teintés par la couleur
// des sommets : la qualité du tir quand elle est lancée) ; dedans, le vide, où tombe du code vert, des caractères en
// pixels qui changent de temps en temps.
// UV : U le long de la déchirure (0 au début), V en travers (0 côté poignée). L'alpha des sommets : le temps qui reste.
Shader "Archery/MatrixTear"
{
    Properties
    {
        [HDR] _EdgeColor ("Bord", Color) = (0.6, 1.6, 2.4, 1)
        [HDR] _EdgeColorAlt ("Bord (autre côté)", Color) = (2.4, 0.25, 1.7, 1)
        _VoidColor ("Vide", Color) = (0.008, 0.0, 0.03, 1)
        [HDR] _CodeColor ("Code", Color) = (0.25, 1.7, 0.65, 1)
        _Columns ("Colonnes de code", Float) = 40
        _Rows ("Lignes de code", Float) = 8
        _Speed ("Vitesse du code", Float) = 5
        _EdgeWidth ("Largeur du bord", Range(0.01, 0.2)) = 0.07
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
            Name "MatrixTear"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _EdgeColor;
                half4 _EdgeColorAlt;
                half4 _VoidColor;
                half4 _CodeColor;
                float _Columns;
                float _Rows;
                float _Speed;
                half _EdgeWidth;
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

            float TearHash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
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

                float2 uv = input.uv;
                float tick = floor(_Time.y * 12.0);

                // La fente : plus étroite aux deux bouts, déchiquetée par blocs, et parfois décalée d'un coup (un bug).
                float block = floor(uv.x * 26.0);
                float jag = TearHash(float2(block, floor(tick * 0.5)));
                float glitch = step(0.82, TearHash(float2(tick, 3.7)));
                float shift = (TearHash(float2(block, tick + 17.0)) - 0.5) * 0.12 * glitch;
                float ends = smoothstep(0.0, 0.16, uv.x) * smoothstep(1.0, 0.84, uv.x);
                float halfWidth = ends * (0.3 + 0.14 * jag);
                float distance = abs(uv.y - 0.5 - shift);
                clip(halfWidth - distance);
                float edge = smoothstep(halfWidth - _EdgeWidth, halfWidth, distance);

                // Le vide, et le code qui y tombe : des colonnes de caractères en pixels (3 × 5), à des vitesses différentes.
                float2 grid = float2(uv.x * _Columns, uv.y * _Rows);
                float column = floor(grid.x);
                float fall = grid.y + _Time.y * _Speed * (0.4 + TearHash(float2(column, 1.3)));
                float2 cell = float2(column, floor(fall));
                float2 inCell = float2(frac(grid.x), frac(fall));
                float2 pixel = floor(inCell * float2(3.0, 5.0));
                float change = floor(_Time.y * 4.0 + TearHash(cell) * 4.0);
                float glyph = step(0.5, TearHash(cell * 1.7 + pixel * 0.37 + change * 0.11));
                float lit = step(0.55, TearHash(cell * 1.3 + 0.5)) * glyph * step(0.08, inCell.x) * step(0.08, inCell.y);
                float head = step(0.92, TearHash(float2(column, floor(fall) + 0.5)));
                half3 color = _VoidColor.rgb + _CodeColor.rgb * lit * (0.55 + 0.9 * head);

                // Quelques blocs du vide prennent la couleur du bord pendant un bug.
                float burst = glitch * step(0.9, TearHash(float2(block, floor(uv.y * 6.0) + tick)));
                color = lerp(color, _EdgeColorAlt.rgb * 0.6, burst);

                // Les bords : cyan d'un côté, magenta de l'autre, teintés par la couleur des sommets.
                half3 rim = (uv.y - 0.5 - shift) > 0.0 ? _EdgeColor.rgb : _EdgeColorAlt.rgb;
                color = lerp(color, rim * input.color.rgb, edge);
                half alpha = lerp(0.88, 1.0, edge) * input.color.a;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
