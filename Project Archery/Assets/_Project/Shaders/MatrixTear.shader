// La fissure du katana (22 octobre) : la lame fend la matrice du jeu le long de son geste. Une fissure aux bords
// déchiquetés (le maillage les donne), qui brillent et grésillent ; dedans, le vide, où tombent des colonnes de 0 et de 1
// verts, comme dans Matrix : chaque colonne a sa goutte, une tête presque blanche suivie d'une traîne qui s'éteint, et
// les chiffres changent de temps en temps.
// UV0 : X le long de la fissure (en mètres), Y en travers (-1 et 1 sur les bords). UV1 : la position dans le plan du
// signe (en mètres), pour la grille des chiffres. Couleur des sommets : teinte des bords (la qualité du tir), alpha.
// _Fade et _Flash (bloc de propriétés) : l'effacement du signe, et son éclat quand il se forme ou part.
Shader "Archery/MatrixTear"
{
    Properties
    {
        [HDR] _EdgeColor ("Bord", Color) = (0.9, 2.2, 1.7, 1)
        _VoidColor ("Vide", Color) = (0.0, 0.012, 0.006, 1)
        [HDR] _CodeColor ("Chiffres", Color) = (0.25, 1.9, 0.55, 1)
        [HDR] _HeadColor ("Tête des gouttes", Color) = (1.4, 2.6, 1.5, 1)
        _CellSize ("Taille d'un chiffre (m)", Float) = 0.011
        _Speed ("Vitesse de chute", Float) = 9
        _EdgeWidth ("Largeur du bord", Range(0.05, 0.8)) = 0.4
        _Fade ("Effacement", Range(0, 1)) = 1
        _Flash ("Éclat", Range(0, 1)) = 0
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
                half4 _VoidColor;
                half4 _CodeColor;
                half4 _HeadColor;
                float _CellSize;
                float _Speed;
                half _EdgeWidth;
                half _Fade;
                half _Flash;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 plane : TEXCOORD1;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 plane : TEXCOORD1;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float CrackHash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // Un chiffre dans sa case (0 à 1 dans les deux sens) : 1 sur le trait, 0 ailleurs, adouci au bord.
            float Digit(float2 p, float one, float soft)
            {
                // « 0 » : un anneau ovale. « 1 » : un trait vertical, un petit drapeau en haut et un pied.
                float zero = abs(length((p - 0.5) / float2(0.3, 0.4)) - 1.0) * 0.3;
                float bar = max(abs(p.x - 0.55) - 0.07, abs(p.y - 0.5) - 0.38);
                float2 flagA = float2(0.55, 0.88);
                float2 flagB = float2(0.32, 0.7);
                float2 ba = flagB - flagA;
                float h = saturate(dot(p - flagA, ba) / dot(ba, ba));
                float flag = length(p - flagA - ba * h) - 0.06;
                float foot = max(abs(p.x - 0.55) - 0.2, abs(p.y - 0.12) - 0.05);
                float d = lerp(zero - 0.05, min(min(bar, flag), foot), one);
                return 1.0 - smoothstep(-soft, soft, d);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.plane = input.plane;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float across = abs(input.uv.y);
                float tick = floor(_Time.y * 14.0);

                // Les bords grésillent : par endroits, la lueur s'allume plus fort ou s'éteint un instant.
                float sparkle = CrackHash(float2(floor(input.uv.x * 70.0), tick));
                float edge = smoothstep(1.0 - _EdgeWidth, 1.0, across);
                float rimGlow = edge * (0.75 + 0.6 * sparkle);

                // La pluie de chiffres, dans le plan du signe : une colonne par chiffre, chacune à sa vitesse.
                float2 cellSize = float2(_CellSize * 0.62, _CellSize);
                float2 grid = input.plane / cellSize;
                float column = floor(grid.x);
                float columnHash = CrackHash(float2(column, 7.3));
                float speed = _Speed * (0.55 + columnHash);
                float fall = grid.y + _Time.y * speed;
                float row = floor(fall);
                float2 inCell = float2(frac(grid.x), frac(fall));

                // La goutte : une tête claire qui descend, suivie (au-dessus) d'une traîne qui s'éteint.
                float trail = frac(columnHash * 13.7 - grid.y / 24.0 - _Time.y * speed / 24.0);
                float brightness = pow(trail, 3.0);
                float head = smoothstep(0.93, 1.0, trail);

                float change = floor(_Time.y * (2.0 + 6.0 * columnHash) + CrackHash(float2(column, row)) * 5.0);
                float one = step(0.5, CrackHash(float2(column * 1.7 + change, row * 3.1)));
                float soft = max(fwidth(inCell.x), fwidth(inCell.y)) * 1.5 + 0.02;
                float glyph = Digit(inCell, one, soft) * step(0.15, brightness + head);

                half3 color = _VoidColor.rgb;
                color += _CodeColor.rgb * glyph * brightness;
                color = lerp(color, _HeadColor.rgb, glyph * head);

                // Le bord : la lueur de la fissure, teintée par la qualité du tir, plus vive quand le signe éclate.
                half3 rim = _EdgeColor.rgb * lerp(half3(1, 1, 1), input.color.rgb, 0.65);
                color = lerp(color, rim * (1.0 + 1.5 * _Flash), saturate(rimGlow));
                color += rim * _Flash * 0.35;

                half alpha = lerp(0.93, 1.0, edge) * input.color.a * _Fade;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
