using Archery.Player;
using UnityEngine;

namespace Archery.FinalBoss
{
    /// <summary>
    /// Flèche rouge au bord de la vue, qui montre où est MK quand il sort du champ de vision (en vol surtout) : on
    /// tourne la tête vers elle pour le retrouver. Elle disparaît dès qu'il revient devant les yeux.
    /// </summary>
    /// <remarks>
    /// Créée par l'<see cref="ArenaDirector"/> au début du combat, accrochée à la tête du joueur. Elle se détruit d'elle-même
    /// quand le dragon n'existe plus (l'arène se ferme).
    /// </remarks>
    public class DragonPointer : MonoBehaviour
    {
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");

        // Distance devant les yeux, et angle (depuis le centre de la vue) où la flèche se place : bien dans le champ du
        // casque, sans gêner la visée.
        const float k_Distance = 1.4f;
        const float k_EdgeAngle = 24f;

        // Le dragon à moins de cet angle du regard : il est visible, pas de flèche.
        const float k_HideAngle = 34f;
        const float k_Size = 0.08f;

        static readonly Color k_Color = new Color(2.2f, 0.35f, 0.12f, 0.9f);

        DragonBoss m_Dragon;
        MeshRenderer m_Renderer;
        MaterialPropertyBlock m_Block;
        float m_Visible;
        float m_Age;

        /// <summary>Accroche la flèche à la tête du joueur ; null sans joueur ou sans matériau.</summary>
        public static DragonPointer Create(DragonBoss dragon, Material material)
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (dragon == null || material == null || head == null)
                return null;

            var go = new GameObject("Flèche vers le dragon");
            go.transform.SetParent(head, false);
            go.AddComponent<MeshFilter>().sharedMesh = BuildMesh();
            var pointer = go.AddComponent<DragonPointer>();
            pointer.m_Dragon = dragon;
            pointer.m_Renderer = go.AddComponent<MeshRenderer>();
            pointer.m_Renderer.sharedMaterial = material;
            pointer.m_Renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            pointer.m_Renderer.receiveShadows = false;
            pointer.m_Renderer.enabled = false;
            return pointer;
        }

        void OnDestroy()
        {
            var filter = GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null)
                Destroy(filter.sharedMesh);
        }

        // Après la caméra : la flèche suit la tête, et se tourne vers le dragon.
        void LateUpdate()
        {
            if (m_Dragon == null || transform.parent == null)
            {
                Destroy(gameObject);
                return;
            }

            var deltaTime = Time.deltaTime;
            m_Age += deltaTime;
            var local = transform.parent.InverseTransformPoint(m_Dragon.BodyPoint);
            var fighting = m_Dragon.Phase != DragonPhase.Asleep && m_Dragon.Phase != DragonPhase.Dead && ArenaPlayer.IsAlive;
            var hidden = !fighting || Vector3.Angle(Vector3.forward, local) < k_HideAngle;
            m_Visible = Mathf.MoveTowards(m_Visible, hidden ? 0f : 1f, deltaTime * 5f);
            m_Renderer.enabled = m_Visible > 0.01f;
            if (!m_Renderer.enabled)
                return;

            // Vers le dragon, dans le plan de la vue ; juste derrière, la flèche se met sur le côté où il penche.
            var flat = new Vector2(local.x, local.y);
            if (flat.sqrMagnitude < 1e-4f)
                flat = Vector2.right;
            flat.Normalize();
            var edge = Mathf.Tan(k_EdgeAngle * Mathf.Deg2Rad) * k_Distance;
            transform.localPosition = new Vector3(flat.x * edge, flat.y * edge * 0.8f, k_Distance);
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(flat.y, flat.x) * Mathf.Rad2Deg);

            // Elle bat, et pousse un peu vers le dragon.
            var beat = 0.5f + 0.5f * Mathf.Sin(m_Age * 7f);
            transform.localScale = Vector3.one * (k_Size * (1f + 0.15f * beat));
            transform.localPosition += new Vector3(flat.x, flat.y * 0.8f, 0f) * (0.03f * beat);

            m_Block ??= new MaterialPropertyBlock();
            m_Renderer.GetPropertyBlock(m_Block);
            var color = k_Color;
            color.a *= m_Visible * (0.75f + 0.25f * beat);
            m_Block.SetColor(k_BaseColorId, color);
            m_Renderer.SetPropertyBlock(m_Block);
        }

        // Deux chevrons (››) pointés vers +X, dans le plan XY : le second, plus petit et plus pâle, derrière le premier.
        static Mesh BuildMesh()
        {
            var builder = new MeshBuilder();
            AddChevron(builder, 0f, 1f, Color.white);
            AddChevron(builder, -0.85f, 0.75f, new Color(1f, 1f, 1f, 0.55f));
            return builder.ToMesh("Flèche vers le dragon");
        }

        static void AddChevron(MeshBuilder builder, float x, float size, Color color)
        {
            var tip = new Vector3(x + 1f * size, 0f, 0f);
            var top = new Vector3(x - 0.6f * size, 0.75f * size, 0f);
            var notch = new Vector3(x - 0.15f * size, 0f, 0f);
            var bottom = new Vector3(x - 0.6f * size, -0.75f * size, 0f);
            builder.Triangle(tip, top, notch, color);
            builder.Triangle(tip, notch, bottom, color);
        }
    }
}
