// La faille du katana : la lame fend la matrice du jeu le long de son geste (refaite le 24 octobre : elle s'ouvre au fil de
// la lame, et plus large au centre ; avant, elle s'ouvrait d'un coup, comme un œil).
// Le maillage ne donne que le tracé (MatrixRiftMesh) ; ici, à chaque image, la faille prend sa forme :
// - chaque bout du tracé s'ouvre juste après le passage de la lame (d'abord une fine fissure qui brille, aussitôt écartée),
//   puis la faille continue de s'élargir doucement (_GrowTime) : elle suit la lame, et grandit avec le temps ;
// - une plaie plus large au centre qu'aux bouts, pointus ; _Open referme ou rouvre toute la faille (le script l'anime :
//   refermée, ratée, lancée) ;
// - ses bords ondulent et restent déchiquetés, son tracé frémit (ancré aux deux bouts), elle respire doucement ;
// - ses fissures secondaires rampent : elles s'allongent et se rétractent, chacune à son rythme.
// Dedans, le vide, où tombent des colonnes de 0 et de 1 verts, comme dans Matrix (une tête presque blanche suivie d'une
// traîne qui s'éteint, des chiffres qui changent de temps en temps). Les bords brillent et grésillent, teintés par _Tint :
// la qualité du tir que donnerait la faille (vert, vert pastel, orange), puis celle du tir lancé.
// Sommets : position = point du tracé ; normale = le travers du tracé (perpendiculaire au tracé et au regard) ; tangente =
// direction (xyz) et longueur (w) d'une fissure secondaire ; UV0 = (le long du tracé en mètres, -1 / 0 / 1 en travers) ;
// UV1 = (avancée de 0 à 1, demi-largeur au centre en mètres, 0 tracé ou 1 fissure secondaire, phase) ; UV2.x = moment (s,
// depuis la naissance de la faille) où la lame est passée là ; couleur : alpha.
// La fusion se voit (24 octobre, après le test) : de chaque croisement (_Fuse0 à _Fuse3 : point dans le repère de la faille,
// et moment de la fusion en w), une onde de lumière part le long de tous les brins en gonflant la plaie à son passage, puis
// un nœud lumineux reste au croisement et bat ; une faille fusionnée (_Fusion) pulse.
// Bloc de propriétés (par faille) : _Age (s depuis sa naissance), _Open, _Fade (effacement), _Flash (éclat), _Tint, _Seed,
// _Fusion, _Fuse0 à _Fuse3.
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
        _Wobble ("Bords qui ondulent", Range(0, 1)) = 1
        _Jag ("Bords déchiquetés", Range(0, 1)) = 0.7
        _Shiver ("Tracé qui frémit (m)", Range(0, 0.03)) = 0.007
        _Bulge ("Plus large au centre", Range(0.5, 3)) = 1.4
        _SplitTime ("Ouverture derrière la lame (s)", Range(0.02, 0.5)) = 0.07
        _CrackShare ("Part ouverte tout de suite", Range(0.1, 1)) = 0.5
        _GrowTime ("Élargissement (s)", Range(0.1, 4)) = 1.4
        _Open ("Ouverture", Range(0, 1.3)) = 1
        _Age ("Âge (s)", Float) = 10
        _Tint ("Teinte (qualité)", Color) = (1, 1, 1, 1)
        _Seed ("Graine", Float) = 0
        _Fade ("Effacement", Range(0, 1)) = 1
        _Flash ("Éclat", Range(0, 1)) = 0
        [HDR] _FuseColor ("Fusion : lumière", Color) = (1.2, 3.2, 3.8, 1)
        _RippleSpeed ("Fusion : vitesse de l'onde (m/s)", Float) = 1.6
        _Fusion ("Fusion (script)", Range(0, 1)) = 0
        _Fuse0 ("Croisement 1 (script)", Vector) = (0, 0, 0, 1000000)
        _Fuse1 ("Croisement 2 (script)", Vector) = (0, 0, 0, 1000000)
        _Fuse2 ("Croisement 3 (script)", Vector) = (0, 0, 0, 1000000)
        _Fuse3 ("Croisement 4 (script)", Vector) = (0, 0, 0, 1000000)
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
                half _Wobble;
                half _Jag;
                float _Shiver;
                float _Bulge;
                float _SplitTime;
                float _CrackShare;
                float _GrowTime;
                float _Open;
                float _Age;
                half4 _Tint;
                float _Seed;
                half _Fade;
                half _Flash;
                half4 _FuseColor;
                float _RippleSpeed;
                half _Fusion;
                float4 _Fuse0;
                float4 _Fuse1;
                float4 _Fuse2;
                float4 _Fuse3;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float4 data : TEXCOORD1;
                float2 birth : TEXCOORD2;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 plane : TEXCOORD1;
                float fuse : TEXCOORD2;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float CrackHash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            // Un bruit lisse de -1 à 1.
            float Noise1(float x)
            {
                float i = floor(x);
                float f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(CrackHash(float2(i, 1.7)), CrackHash(float2(i + 1.0, 1.7)), f) * 2.0 - 1.0;
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

            // Un croisement de la fusion vu depuis le point p du tracé : l'onde (un anneau de lumière qui s'éloigne du
            // croisement à _RippleSpeed et s'éteint en 0,4 s environ) et le nœud (une lueur qui reste au croisement).
            void FuseAt(float4 fuse, float3 p, inout float ripple, inout float knot)
            {
                float since = _Age - fuse.w;
                float on = step(0.0, since);
                since = max(since, 0.0);
                float d = distance(p, fuse.xyz);
                float wave = (d - since * _RippleSpeed) / 0.05;
                float core = d / 0.045;
                ripple = max(ripple, on * exp(-wave * wave) * exp(-since * 2.5));
                knot = max(knot, on * exp(-core * core) * saturate(since * 8.0));
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float t = input.data.x;
                float baseHalf = input.data.y;
                float branch = step(0.5, input.data.z);
                float phase = input.data.w;
                float along = input.uv.x;
                float across = input.uv.y;
                float time = _Time.y + _Seed * 17.0;
                float3 spine = input.positionOS.xyz;
                float3 normal = normalize(input.normalOS);

                // L'ouverture au fil de la lame : là où elle vient de passer, une fine fissure qui brille ; elle s'écarte
                // aussitôt (une part de sa largeur en _SplitTime), puis la faille continue de s'élargir pendant _GrowTime.
                // Les fissures secondaires naissent un peu après le tracé, à l'endroit d'où elles partent.
                float age = _Age - input.birth.x - branch * 0.06;
                float passed = step(0.0, age);
                age = max(age, 0.0);
                float split = 1.0 - exp(-age / _SplitTime);
                float grow = smoothstep(0.0, _GrowTime, age);
                float local = split * lerp(_CrackShare, 1.0, grow);
                float open = local * max(_Open, 0.0);

                // Le profil : une plaie, nettement plus large au centre (sin^_Bulge), pointue aux bouts ; une fissure
                // secondaire s'affine vers sa pointe, qui rampe : elle s'allonge et se rétracte, et les sommets au-delà se
                // replient sur elle.
                float profile;
                if (branch > 0.5)
                {
                    float reach = saturate(0.55 + 0.45 * sin(time * (0.8 + phase) + phase * 6.2832)) * saturate(open);
                    spine -= input.tangentOS.xyz * (max(0.0, t - reach) * input.tangentOS.w);
                    profile = pow(saturate(1.0 - t / max(reach, 0.001)), 0.8);
                }
                else
                {
                    profile = pow(max(sin(3.14159265 * saturate(t)), 0.0), _Bulge);
                }

                // Les bords : déchiquetés (chaque sommet a son écart, propre à chaque bord), ondulants (un bruit qui court le
                // long du tracé), et la plaie respire doucement. Là où la lame est passée, au moins un fil de lumière.
                float jag = lerp(1.0 - _Jag * 0.5, 1.0 + _Jag * 0.35, CrackHash(float2(along * 91.7 + across * 13.1, _Seed * 3.7 + branch)));
                float wave = 1.0 + _Wobble * (0.35 * Noise1(along * 22.0 + time * 3.1 + across * 5.0)
                                             + 0.18 * sin(along * 57.0 - time * 7.3 + across * 2.0));
                float breathe = 1.0 + 0.07 * sin(time * 3.4 + phase * 6.2832);
                float thread = (1.0 - branch) * passed * 0.0012 * saturate(_Open);
                float halfWidth = max(baseHalf * profile * open * jag * max(wave, 0.2) * breathe, thread);

                // La fusion : l'onde gonfle la plaie à son passage, le nœud l'élargit un peu au croisement.
                float ripple = 0.0;
                float knot = 0.0;
                FuseAt(_Fuse0, input.positionOS.xyz, ripple, knot);
                FuseAt(_Fuse1, input.positionOS.xyz, ripple, knot);
                FuseAt(_Fuse2, input.positionOS.xyz, ripple, knot);
                FuseAt(_Fuse3, input.positionOS.xyz, ripple, knot);
                halfWidth *= 1.0 + 1.4 * ripple + 0.35 * knot;

                // Le tracé frémit : tout le travers se décale d'un bruit, ancré aux deux bouts.
                float anchor = branch > 0.5 ? 1.0 : sin(3.14159265 * saturate(t));
                float shiver = _Shiver * anchor * saturate(open)
                             * (Noise1(along * 8.0 + time * 2.3) + 0.4 * Noise1(along * 31.0 - time * 9.0));

                float3 position = spine + normal * (across * halfWidth + shiver);
                output.positionHCS = TransformObjectToHClip(position);
                output.uv = input.uv;
                output.plane = position.xy;
                output.fuse = ripple + knot * (0.65 + 0.35 * sin(time * 9.0));
                output.color = input.color;
                output.color.a *= max(passed, branch);
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

                // La pluie de chiffres, dans le repère de la faille : une colonne par chiffre, chacune à sa vitesse.
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

                // Le bord : la lueur de la faille, teintée par la qualité du tir, plus vive quand elle s'ouvre ou part.
                half3 rim = _EdgeColor.rgb * lerp(half3(1, 1, 1), _Tint.rgb, 0.65);
                color = lerp(color, rim * (1.0 + 1.5 * _Flash), saturate(rimGlow));
                color += rim * _Flash * 0.35;

                // La fusion : l'onde et le nœud brillent (plus fort sur les bords), et une faille fusionnée pulse.
                color += _FuseColor.rgb * input.fuse * (0.6 + 0.8 * edge);
                color += rim * edge * _Fusion * 0.5 * (0.5 + 0.5 * sin(_Time.y * 6.0));

                half alpha = lerp(0.93, 1.0, edge) * input.color.a * _Fade;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
