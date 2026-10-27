using System.Collections.Generic;
using UnityEngine;

namespace Archery.Upgrades
{
    /// <summary>
    /// Ce que l'on voit du duo « Trou noir » (fût aimant et pointe à mèche ; refait le 26 octobre, demandé par le joueur) : sur
    /// la flèche plantée, une sphère noire qui grandit, cernée d'un anneau de lumière tourné vers le joueur, et deux disques de
    /// matière qui tournent autour ; des traits de lumière tombent dedans depuis le bord de l'aspiration. À la fin, il se
    /// referme d'un coup (<see cref="Collapse"/>), au moment où la mèche explose.
    /// </summary>
    /// <remarks>
    /// L'aspiration elle-même est un aimant plus fort, et l'explosion celle de la mèche, deux fois plus large (<see cref="ArrowPieces"/>).
    /// Léger pour le Quest : une sphère et trois lignes par trou noir, réutilisés, 6 au plus (au-delà, l'explosion a lieu sans
    /// le visuel). Les réglages sont les constantes en tête du script.
    /// </remarks>
    public class BlackHole : MonoBehaviour
    {
        // La sphère noire : son diamètre (m) une fois formée, le temps (s) pour se former et pour se refermer, son battement.
        const float k_CoreSize = 0.55f;
        const float k_GrowTime = 0.35f;
        const float k_CollapseTime = 0.15f;
        const float k_Pulse = 0.06f;

        // L'anneau de lumière (rayon en m, épaisseur), les disques de matière (rayons, inclinaison en °, rotation en °/s).
        const float k_RingRadius = 0.36f;
        const float k_RingWidth = 0.035f;
        const float k_InnerDisc = 0.55f;
        const float k_OuterDisc = 0.9f;
        const float k_DiscTilt = 18f;
        const float k_DiscSpin = 240f;
        const float k_DiscWidth = 0.05f;

        // Les traits de lumière aspirés : leur intervalle (s), la distance (m) d'où ils partent.
        const float k_StreakInterval = 0.06f;
        const float k_StreakDistance = 2.6f;

        const int k_Points = 40;
        const int k_MaxHoles = 6;

        static readonly Color k_RingColor = new Color(1f, 0.86f, 0.62f, 1f);
        static readonly Color k_InnerColor = new Color(1f, 0.55f, 0.2f, 0.85f);
        static readonly Color k_OuterColor = new Color(0.6f, 0.3f, 1f, 0.55f);
        static readonly Color k_StreakColor = new Color(0.78f, 0.55f, 1f, 1f);
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly List<BlackHole> s_Pool = new List<BlackHole>();

        Transform m_Core;
        LineRenderer m_Ring;
        LineRenderer m_Inner;
        LineRenderer m_Outer;
        Material m_Material;
        Camera m_Camera;
        float m_Age;
        float m_Duration;
        float m_CollapseAge = -1f;
        float m_NextStreak;

        /// <summary>
        /// Un trou noir à ce point, pendant <paramref name="duration"/> s (il attend ensuite <see cref="Collapse"/>, ou se referme
        /// seul une seconde plus tard). Null si les 6 sont déjà pris, ou sans matériau d'effet.
        /// </summary>
        public static BlackHole Spawn(Vector3 point, float duration, Material material, Transform parent)
        {
            if (material == null)
                return null;

            s_Pool.RemoveAll(hole => hole == null);
            BlackHole chosen = null;
            foreach (var hole in s_Pool)
            {
                if (!hole.gameObject.activeSelf)
                {
                    chosen = hole;
                    break;
                }
            }

            if (chosen == null)
            {
                if (s_Pool.Count >= k_MaxHoles)
                    return null;

                chosen = new GameObject("Trou noir").AddComponent<BlackHole>();
                chosen.transform.SetParent(parent, false);
                chosen.Build(material);
                s_Pool.Add(chosen);
            }

            chosen.transform.position = point;
            chosen.m_Age = 0f;
            chosen.m_Duration = duration;
            chosen.m_CollapseAge = -1f;
            chosen.m_NextStreak = 0f;
            chosen.gameObject.SetActive(true);
            chosen.Draw(0f);
            return chosen;
        }

        /// <summary>Le trou noir se referme d'un coup (la mèche explose), puis disparaît.</summary>
        public void Collapse()
        {
            if (gameObject.activeSelf && m_CollapseAge < 0f)
                m_CollapseAge = 0f;
        }

        void Build(Material material)
        {
            // La sphère noire : le matériau d'effet (transparent, sans éclairage), teinté en noir opaque.
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            DestroyImmediate(core.GetComponent<Collider>());
            core.name = "Horizon";
            core.transform.SetParent(transform, false);
            var renderer = core.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            m_Material = new Material(material) { name = "Trou noir" };
            m_Material.SetColor(k_BaseColorId, new Color(0f, 0f, 0f, 1f));
            m_Material.renderQueue = material.renderQueue + 1;
            renderer.sharedMaterial = m_Material;
            m_Core = core.transform;

            m_Ring = Line("Anneau", material, k_RingWidth, k_RingColor);
            m_Inner = Line("Disque", material, k_DiscWidth, k_InnerColor);
            m_Outer = Line("Disque extérieur", material, k_DiscWidth * 0.7f, k_OuterColor);
        }

        LineRenderer Line(string lineName, Material material, float width, Color color)
        {
            var line = new GameObject(lineName).AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = k_Points;
            line.widthMultiplier = width;
            line.startColor = line.endColor = color;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = material;
            return line;
        }

        void Update()
        {
            m_Age += Time.deltaTime;
            if (m_CollapseAge < 0f && m_Age > m_Duration + 1f)
                Collapse();

            var size = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(m_Age / k_GrowTime));
            if (m_CollapseAge >= 0f)
            {
                m_CollapseAge += Time.deltaTime;
                var left = 1f - m_CollapseAge / k_CollapseTime;
                if (left <= 0f)
                {
                    gameObject.SetActive(false);
                    return;
                }

                size *= left * left;
            }
            else if (Time.time >= m_NextStreak)
            {
                // De la matière tombe dedans, depuis le bord de l'aspiration.
                m_NextStreak = Time.time + k_StreakInterval;
                var center = transform.position;
                var from = center + Random.onUnitSphere * k_StreakDistance;
                from.y = Mathf.Max(from.y, center.y - 0.3f);
                LightningBolt.Spawn(from, center, k_StreakColor, m_Ring.sharedMaterial, 0.025f, 0.14f);
            }

            Draw(size);
        }

        void Draw(float size)
        {
            var center = transform.position;
            var pulse = 1f + k_Pulse * Mathf.Sin(m_Age * 18f);
            m_Core.localScale = Vector3.one * (k_CoreSize * size * pulse);

            // L'anneau de lumière, toujours face au joueur.
            if (m_Camera == null)
                m_Camera = Camera.main;
            var toCamera = m_Camera != null ? (m_Camera.transform.position - center).normalized : Vector3.forward;
            var facing = Quaternion.LookRotation(toCamera);
            Circle(m_Ring, center, facing, k_RingRadius * size * pulse, 0f);

            // Les disques de matière tournent autour, un peu penchés.
            var spin = m_Age * k_DiscSpin;
            Circle(m_Inner, center, Quaternion.Euler(90f - k_DiscTilt, spin, 0f), k_InnerDisc * size, spin);
            Circle(m_Outer, center, Quaternion.Euler(90f + k_DiscTilt * 0.5f, -spin * 0.6f, 0f), k_OuterDisc * size, -spin);
        }

        // Un cercle dans le plan de <rotation> (son axe Z en est la normale), légèrement ondulé pour qu'il ait l'air de tourner.
        static void Circle(LineRenderer line, Vector3 center, Quaternion rotation, float radius, float phase)
        {
            for (var i = 0; i < k_Points; i++)
            {
                var angle = i * Mathf.PI * 2f / k_Points;
                var wobble = 1f + 0.06f * Mathf.Sin(angle * 3f + phase * Mathf.Deg2Rad);
                line.SetPosition(i, center + rotation * new Vector3(Mathf.Cos(angle) * radius * wobble, Mathf.Sin(angle) * radius * wobble, 0f));
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_Pool.Clear();
    }
}
