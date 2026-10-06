using Archery.Core;
using Archery.Player;
using UnityEngine;

namespace Archery.Defense
{
    /// <summary>Forme de la zone d'un <see cref="TowerTeleporter"/>.</summary>
    public enum TeleporterShape
    {
        /// <summary>Un cercle sur lequel se tenir.</summary>
        Disc,

        /// <summary>Une bande tout autour de la tour : on monte de n'importe quel côté.</summary>
        AroundTower,
    }

    /// <summary>
    /// Téléporteur de la tour (GDD, section 9) : rester un instant debout sur la zone emmène au point d'arrivée,
    /// avec un fondu au noir. La zone du pied de la tour, tout autour d'elle, mène au sommet.
    /// </summary>
    /// <remarks>
    /// Tant que la tour est détruite, la zone s'éteint. Quand elle s'effondre, la zone du pied de la tour
    /// fait descendre le joueur qui était en haut (« Evacuate On Collapse »).
    /// Après une téléportation, ou un saut depuis le haut de la tour, il faut sortir de la zone avant de pouvoir repartir.
    /// La boutique peut aussi envoyer le joueur au point d'arrivée (<see cref="SendPlayer"/>).
    /// </remarks>
    [DisallowMultipleComponent]
    public class TowerTeleporter : MonoBehaviour
    {
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");

        // Points par coin arrondi du contour de la bande.
        const int k_CornerSegments = 8;

        [Tooltip("Point d'arrivée, aux pieds du joueur. Son axe Z (flèche bleue) donne la direction du regard à l'arrivée.")]
        [SerializeField]
        Transform m_Destination;

        [Tooltip("Tourne le regard du joueur dans la direction du point d'arrivée.")]
        [SerializeField]
        bool m_FaceDestinationForward = true;

        [Tooltip("Disc : un cercle de rayon « Radius ». Around Tower : une bande tout autour de la tour ; " +
                 "cet objet se place alors au pied de la tour, en son milieu.")]
        [SerializeField]
        TeleporterShape m_Shape = TeleporterShape.Disc;

        [Tooltip("Disc : rayon (m) du cercle sur lequel se tenir.")]
        [SerializeField]
        float m_Radius = 0.75f;

        [Tooltip("Around Tower : largeur (X) et profondeur (Z) de la tour (m), dans les axes de cet objet.")]
        [SerializeField]
        Vector2 m_TowerSize = new Vector2(5f, 5f);

        [Tooltip("Around Tower : arrondi (m) des coins de la tour. 0 pour une tour carrée, la moitié de sa largeur pour une tour ronde.")]
        [SerializeField]
        float m_TowerCornerRadius;

        [Tooltip("Around Tower : largeur (m) de la bande, à partir du mur.")]
        [SerializeField]
        float m_BandWidth = 1.5f;

        [Tooltip("Temps (s) à rester sur la zone avant de partir.")]
        [SerializeField]
        float m_HoldTime = 1f;

        [Tooltip("Rayon (m) autour du point d'arrivée où le joueur y est déjà (en haut de la tour) : la boutique cache alors son bouton.")]
        [SerializeField]
        float m_ArrivalRadius = 4f;

        [Tooltip("La zone ne marche plus tant que la tour est détruite.")]
        [SerializeField]
        bool m_DisabledWhileTowerDestroyed = true;

        [Tooltip("Pour la zone du pied de la tour : quand la tour s'effondre, le joueur qui est en haut est envoyé au sol, " +
                 "à côté de la zone (ou au point « Evacuation Point »).")]
        [SerializeField]
        bool m_EvacuateOnCollapse = true;

        [Tooltip("Optionnel : point d'arrivée au sol quand la tour s'effondre. Vide : 1,6 m devant le cercle (son axe Z), " +
                 "ou juste à côté de la bande, du côté du joueur.")]
        [SerializeField]
        Transform m_EvacuationPoint;

        [Header("Rendu")]
        [Tooltip("Optionnel : rendu de la zone (matériau avec _BaseColor), coloré selon l'état. " +
                 "Around Tower : son maillage est remplacé en jeu par la bande.")]
        [SerializeField]
        Renderer m_Visual;

        [SerializeField]
        Color m_IdleColor = new Color(0.25f, 0.6f, 1f);

        [Tooltip("Couleur atteinte à la fin de la charge.")]
        [SerializeField]
        Color m_ChargedColor = new Color(0.75f, 1f, 1f);

        [SerializeField]
        Color m_DisabledColor = new Color(0.25f, 0.25f, 0.28f);

        [Tooltip("Joué quand le joueur met le pied sur la zone.")]
        [SerializeField]
        AudioClip m_ChargeClip;

        MaterialPropertyBlock m_PropertyBlock;
        Mesh m_BandMesh;
        Tower m_Tower;
        float m_Charge;
        bool m_Armed = true;
        bool m_WasCharging;

        /// <summary>La zone marche : point d'arrivée réglé, et tour debout.</summary>
        public bool IsUsable => m_Destination != null && !(m_DisabledWhileTowerDestroyed && m_Tower != null && !m_Tower.IsStanding);

        /// <summary>Le joueur est déjà au point d'arrivée (en haut de la tour, pour la zone du pied).</summary>
        public bool IsPlayerAtDestination
        {
            get
            {
                var rig = PlayerRig.Instance;
                if (rig == null || m_Destination == null)
                    return false;

                var offset = rig.BodyPosition - m_Destination.position;
                if (Mathf.Abs(offset.y) > 1.5f)
                    return false;

                offset.y = 0f;
                return offset.sqrMagnitude <= m_ArrivalRadius * m_ArrivalRadius;
            }
        }

        void Awake()
        {
            m_PropertyBlock = new MaterialPropertyBlock();
            if (m_Destination == null)
                Debug.LogError("TowerTeleporter : aucun point d'arrivée (Destination).", this);
            if (m_Shape == TeleporterShape.AroundTower)
                BuildBandVisual();
        }

        void OnEnable() => PlayerTeleport.Teleported += OnPlayerTeleported;

        void OnDisable() => PlayerTeleport.Teleported -= OnPlayerTeleported;

        void Start()
        {
            m_Tower = Tower.Instance;
            if (m_Tower != null)
                m_Tower.Destroyed += OnTowerDestroyed;
        }

        void OnDestroy()
        {
            if (m_Tower != null)
                m_Tower.Destroyed -= OnTowerDestroyed;
            if (m_BandMesh != null)
                Destroy(m_BandMesh);
        }

        /// <summary>Envoie le joueur au point d'arrivée sans passer par la zone (bouton de la boutique). Faux si c'est impossible.</summary>
        public bool SendPlayer()
        {
            var teleport = PlayerTeleport.Instance;
            if (!IsUsable || teleport == null || teleport.IsTeleporting)
                return false;

            return Go(teleport);
        }

        void Update()
        {
            var rig = PlayerRig.Instance;
            var teleport = PlayerTeleport.Instance;
            var feet = rig != null ? rig.BodyPosition : transform.position;
            var onPad = rig != null && IsOnPad(feet);

            // En haut de la tour, la zone du pied se désarme : en sautant, on retombe dessus sans remonter aussitôt.
            if (rig != null && feet.y - transform.position.y > 1.5f)
                m_Armed = false;
            else if (!onPad)
                m_Armed = true;

            var charging = IsUsable && onPad && m_Armed && teleport != null && !teleport.IsTeleporting;
            if (charging && !m_WasCharging)
                Sfx.Play(m_ChargeClip, feet + Vector3.up, 0.7f);
            m_WasCharging = charging;

            m_Charge = charging ? m_Charge + Time.deltaTime / Mathf.Max(0.05f, m_HoldTime) : 0f;
            UpdateVisual();

            if (m_Charge >= 1f)
            {
                m_Charge = 0f;
                Go(teleport);
            }
        }

        bool IsOnPad(Vector3 feet)
        {
            var offset = feet - transform.position;
            if (Mathf.Abs(offset.y) > 1f)
                return false;

            if (m_Shape == TeleporterShape.AroundTower)
                return DistanceFromTower(offset) <= m_BandWidth;

            offset.y = 0f;
            return offset.sqrMagnitude <= m_Radius * m_Radius;
        }

        bool Go(PlayerTeleport teleport)
        {
            if (teleport == null || m_Destination == null)
                return false;

            float? yaw = m_FaceDestinationForward ? m_Destination.eulerAngles.y : (float?)null;
            return teleport.TryTeleport(m_Destination.position, yaw);
        }

        // Après n'importe quelle téléportation, il faut sortir de la zone avant de repartir.
        void OnPlayerTeleported()
        {
            m_Armed = false;
            m_Charge = 0f;
        }

        // La tour s'effondre : le joueur qui était en haut descend au sol.
        void OnTowerDestroyed()
        {
            var rig = PlayerRig.Instance;
            var teleport = PlayerTeleport.Instance;
            if (!m_EvacuateOnCollapse || rig == null || teleport == null || m_Tower == null)
                return;
            if (rig.BodyPosition.y < m_Tower.TopHeight - 1.5f)
                return;

            if (m_EvacuationPoint != null)
                teleport.TryTeleport(m_EvacuationPoint.position, m_EvacuationPoint.eulerAngles.y);
            else if (m_Shape == TeleporterShape.AroundTower)
                teleport.TryTeleport(BesideBand(rig.BodyPosition));
            else
                teleport.TryTeleport(transform.position + transform.forward * 1.6f, transform.eulerAngles.y);
        }

        // Juste à l'extérieur de la bande, du côté où se trouve le joueur.
        Vector3 BesideBand(Vector3 feet)
        {
            var direction = feet - transform.position;
            direction.y = 0f;
            direction = direction.sqrMagnitude > 0.01f ? direction.normalized : transform.forward;

            // On avance depuis le milieu de la tour jusqu'à 0,8 m au-delà de la bande.
            var target = m_BandWidth + 0.8f;
            var distance = 0f;
            for (var i = 0; i < 32; i++)
            {
                var step = target - DistanceFromTower(direction * distance);
                if (step < 0.01f)
                    break;
                distance += step;
            }

            return transform.position + direction * distance;
        }

        // Distance (m), à plat, entre ce point (décalage depuis cet objet) et le mur de la tour. Négative dans la tour.
        float DistanceFromTower(Vector3 offset)
        {
            GetTowerShape(out var half, out var corner);
            var local = Quaternion.Inverse(transform.rotation) * offset;
            var q = new Vector2(Mathf.Abs(local.x) - half.x + corner, Mathf.Abs(local.z) - half.y + corner);
            var outside = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - corner;
        }

        void GetTowerShape(out Vector2 half, out float corner)
        {
            half = new Vector2(Mathf.Max(0f, m_TowerSize.x * 0.5f), Mathf.Max(0f, m_TowerSize.y * 0.5f));
            corner = Mathf.Clamp(m_TowerCornerRadius, 0f, Mathf.Min(half.x, half.y));
        }

        // Point du contour de la tour élargi de « offset » m, dans les axes de cet objet.
        // Le contour fait le tour des 4 coins dans le sens inverse des aiguilles d'une montre (vu du dessus).
        Vector3 OutlinePoint(int index, float offset)
        {
            GetTowerShape(out var half, out var corner);
            var perCorner = k_CornerSegments + 1;
            var cornerIndex = index / perCorner;
            var signX = cornerIndex == 0 || cornerIndex == 3 ? 1f : -1f;
            var signZ = cornerIndex < 2 ? 1f : -1f;
            var center = new Vector3(signX * (half.x - corner), 0f, signZ * (half.y - corner));
            var angle = (cornerIndex + (index % perCorner) / (float)k_CornerSegments) * 0.5f * Mathf.PI;
            return center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (corner + offset);
        }

        // Around Tower : le rendu devient une bande plate autour de la tour, à la hauteur du rendu.
        void BuildBandVisual()
        {
            if (m_Visual == null || !m_Visual.TryGetComponent<MeshFilter>(out var filter))
                return;

            var visual = m_Visual.transform;
            var count = 4 * (k_CornerSegments + 1);
            var vertices = new Vector3[count * 2];
            var uvs = new Vector2[count * 2];
            var triangles = new int[count * 6];
            for (var i = 0; i < count; i++)
            {
                vertices[i * 2] = ToVisual(visual, OutlinePoint(i, 0f));
                vertices[i * 2 + 1] = ToVisual(visual, OutlinePoint(i, m_BandWidth));
                uvs[i * 2] = new Vector2(i / (float)count, 0f);
                uvs[i * 2 + 1] = new Vector2(i / (float)count, 1f);

                // Deux triangles entre ce point et le suivant, tournés vers le haut.
                var inner = i * 2;
                var next = (i + 1) % count * 2;
                var t = i * 6;
                triangles[t] = inner;
                triangles[t + 1] = next + 1;
                triangles[t + 2] = inner + 1;
                triangles[t + 3] = inner;
                triangles[t + 4] = next;
                triangles[t + 5] = next + 1;
            }

            m_BandMesh = new Mesh { name = "Teleporter Band" };
            m_BandMesh.SetVertices(vertices);
            m_BandMesh.SetUVs(0, uvs);
            m_BandMesh.SetTriangles(triangles, 0);
            m_BandMesh.RecalculateNormals();
            m_BandMesh.RecalculateBounds();
            filter.sharedMesh = m_BandMesh;
        }

        Vector3 ToVisual(Transform visual, Vector3 local)
        {
            var world = transform.position + transform.rotation * local;
            world.y = visual.position.y;
            return visual.InverseTransformPoint(world);
        }

        void UpdateVisual()
        {
            if (m_Visual == null)
                return;

            Color color;
            if (!IsUsable)
                color = m_DisabledColor;
            else if (m_Charge > 0f)
                color = Color.Lerp(m_IdleColor, m_ChargedColor, m_Charge) * (0.85f + 0.15f * Mathf.Sin(Time.time * 20f));
            else
                color = m_IdleColor * (0.8f + 0.2f * Mathf.Sin(Time.time * 2f));

            color.a = 1f;
            m_Visual.GetPropertyBlock(m_PropertyBlock);
            m_PropertyBlock.SetColor(k_BaseColorId, color);
            m_Visual.SetPropertyBlock(m_PropertyBlock);
        }

        // La zone dans la scène, objet sélectionné.
        void OnDrawGizmosSelected()
        {
            Gizmos.color = m_IdleColor;
            if (m_Shape == TeleporterShape.AroundTower)
            {
                var count = 4 * (k_CornerSegments + 1);
                for (var i = 0; i < count; i++)
                {
                    var next = (i + 1) % count;
                    Gizmos.DrawLine(transform.position + transform.rotation * OutlinePoint(i, 0f),
                                    transform.position + transform.rotation * OutlinePoint(next, 0f));
                    Gizmos.DrawLine(transform.position + transform.rotation * OutlinePoint(i, m_BandWidth),
                                    transform.position + transform.rotation * OutlinePoint(next, m_BandWidth));
                }

                return;
            }

            const int segments = 32;
            for (var i = 0; i < segments; i++)
            {
                var a = i * 2f * Mathf.PI / segments;
                var b = (i + 1) * 2f * Mathf.PI / segments;
                Gizmos.DrawLine(transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * m_Radius,
                                transform.position + new Vector3(Mathf.Cos(b), 0f, Mathf.Sin(b)) * m_Radius);
            }
        }
    }
}
