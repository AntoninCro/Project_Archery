// La grille de construction au sol (refonte du tower defense, 28 octobre, demandée par le joueur) : chaque case est un carré
// teinté par la couleur de ses sommets (vert là où le bâtiment se pose, rouge ailleurs), bordé d'une ligne plus vive et plus
// opaque, nette à toute distance (lissée par fwidth). Les fonds des cartes du menu des plans s'en servent aussi.
// UV0 : la place dans le carré (0 à 1). UV1 : la taille du carré (m), l'épaisseur de la ligne (m), l'opacité du fond.
// La couleur des sommets donne la teinte, et son alpha l'opacité de l'ensemble (les cases s'effacent vers le bord de la grille).
Shader "Archery/BuildGrid"
{
    Properties
    {
        _BaseColor ("Couleur", Color) = (1, 1, 1, 1)
        _LineBoost ("Éclat de la ligne", Float) = 1.35
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
            Name "BuildGrid"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            // Au ras du sol : légèrement tirée vers la caméra pour ne pas clignoter avec lui.
            Offset -1, -4

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half _LineBoost;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 cell : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 cell : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color * _BaseColor;
                output.uv = input.uv;
                output.cell = input.cell;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Distance (m) au bord le plus proche du carré, puis la ligne, lissée sur un pixel.
                float2 size = max(input.cell.xy, 0.001);
                float2 edges = min(input.uv, 1.0 - input.uv) * size;
                float toEdge = min(edges.x, edges.y);
                float width = input.cell.z;
                float blur = max(fwidth(toEdge), 1e-5);
                float border = 1.0 - smoothstep(width - blur, width + blur, toEdge);

                half alpha = lerp(input.cell.w, 1.0, border) * input.color.a;
                half3 rgb = input.color.rgb * lerp(1.0, _LineBoost, border);
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
