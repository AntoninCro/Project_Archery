using Archery.Bows;
using Archery.Enemies;
using Archery.Upgrades;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Building
{
    /// <summary>
    /// Tour de givre sur un socle au bord de la voie (GDD, section 9) : elle ralentit les ennemis autour d'elle,
    /// volants compris (30 / 40 / 50 % dans un rayon de 9 / 13 / 17 m). Un disque bleuté montre sa portée au sol,
    /// ses cristaux grandissent avec le niveau. Dans l'arène du boss final, elle tire plutôt des boules de glace dont le
    /// ralentissement s'estompe vite (<see cref="FiresIceBalls"/>).
    /// </summary>
    public class FrostTower : Structure
    {
        const float k_TickInterval = 0.2f;
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("Rayon (m) de la zone ralentie, par niveau. La hauteur ne compte pas : les volants au-dessus sont ralentis.")]
        [SerializeField]
        float[] m_RadiusPerLevel = { 9f, 13f, 17f };

        [Tooltip("Vitesse des ennemis dans la zone, par niveau (0,7 = 30 % plus lents).")]
        [SerializeField]
        float[] m_SpeedPerLevel = { 0.7f, 0.6f, 0.5f };

        [Tooltip("Hauteur maximale (m) au-dessus du pied de la tour où un ennemi est encore ralenti.")]
        [SerializeField]
        float m_MaxHeight = 15f;

        [Header("Apparence")]
        [Tooltip("Matériau du disque de portée (shader Archery/UnlitVertexColor, comme la flèche de glace).")]
        [SerializeField]
        Material m_RangeMaterial;

        [SerializeField]
        Color m_RangeColor = new Color(0.55f, 0.85f, 1f, 0.3f);

        [Tooltip("Optionnel : objet agrandi à chaque niveau (les cristaux).")]
        [SerializeField]
        Transform m_Crystals;

        [SerializeField]
        float[] m_CrystalScalePerLevel = { 1f, 1.15f, 1.3f };

        [Tooltip("Optionnel : lumière des cristaux, plus forte à chaque niveau.")]
        [SerializeField]
        Light m_Light;

        [SerializeField]
        float[] m_LightIntensityPerLevel = { 1.5f, 2.2f, 3f };

        [Header("Boules de glace (arène du boss final)")]
        [Tooltip("Portée (m) des boules de glace, avant la portée doublée de l'arène.")]
        [SerializeField]
        float m_IceBallRange = 20f;

        [Tooltip("Temps (s) entre deux boules de glace d'une même tour, par niveau. À plusieurs, les tours se relaient.")]
        [SerializeField]
        float[] m_IceBallIntervalPerLevel = { 3f, 2.5f, 2f };

        [Tooltip("Durée (s) du ralentissement d'une boule de glace, par niveau : fort au coup, il s'estompe vite.")]
        [SerializeField]
        float[] m_IceBallSlowTimePerLevel = { 2f, 2.5f, 3f };

        static readonly RaycastHit[] s_Hits = new RaycastHit[16];

        MeshRenderer m_RangeDisc;
        MaterialPropertyBlock m_Block;
        Vector3 m_CrystalBaseScale = Vector3.one;
        float m_TickTimer;
        float m_Age;

        public float Radius => ValueFor(m_RadiusPerLevel, Mathf.Max(1, Level), 9f) * RangeScale;

        /// <summary>
        /// Dans l'arène du boss final : au lieu de ralentir tout ce qui l'entoure, la tour tire des boules de glace (c'est
        /// l'arène qui décide quand, pour que plusieurs tours se relaient).
        /// </summary>
        public bool FiresIceBalls { get; set; }

        /// <summary>Temps (s) entre deux boules de glace de cette tour, à son niveau.</summary>
        public float IceBallInterval => ValueFor(m_IceBallIntervalPerLevel, Level, 3f);

        /// <summary>Vitesse de l'ennemi au coup (0,7 = 30 % plus lent), à son niveau.</summary>
        public float IceBallSlow => ValueFor(m_SpeedPerLevel, Level, 0.7f);

        /// <summary>Durée (s) du ralentissement d'une boule de glace, à son niveau.</summary>
        public float IceBallSlowTime => ValueFor(m_IceBallSlowTimePerLevel, Level, 2f);

        /// <summary>D'où partent les boules de glace : au-dessus des cristaux.</summary>
        public Vector3 IceBallMuzzle => (m_Crystals != null ? m_Crystals.position : transform.position + Vector3.up * 2.5f) + Vector3.up * 0.6f;

        /// <summary>L'ennemi vivant le plus proche à portée des boules de glace, et visible depuis le haut de la tour.</summary>
        public bool TryFindIceBallTarget(out Enemy target)
        {
            target = null;
            var muzzle = IceBallMuzzle;
            var range = m_IceBallRange * RangeScale;
            var best = range * range;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive)
                    continue;

                var distance = (enemy.Center - muzzle).sqrMagnitude;
                if (distance < best && IsVisible(muzzle, enemy.Center))
                {
                    target = enemy;
                    best = distance;
                }
            }

            return target != null;
        }

        // Rien entre le haut de la tour et l'ennemi (une falaise, le bord du plateau), sauf l'ennemi lui-même.
        bool IsVisible(Vector3 from, Vector3 to)
        {
            var direction = to - from;
            var distance = direction.magnitude;
            if (distance < 0.01f)
                return true;

            var count = Physics.RaycastNonAlloc(from, direction / distance, s_Hits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var collider = s_Hits[i].collider;
                if (ArrowIgnore.IsIgnored(collider) || collider.transform.IsChildOf(transform) || collider.GetComponentInParent<Enemy>() != null)
                    continue;
                return false;
            }

            return true;
        }

        protected override void Awake()
        {
            if (m_Crystals != null)
                m_CrystalBaseScale = m_Crystals.localScale;
            base.Awake();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsBuilt)
                return;

            m_Age += Time.deltaTime;
            if (m_RangeDisc != null)
            {
                // Le disque respire doucement.
                var color = m_RangeColor;
                color.a *= 0.8f + 0.2f * Mathf.Sin(m_Age * 1.6f);
                m_RangeDisc.GetPropertyBlock(m_Block);
                m_Block.SetColor(k_BaseColorId, color);
                m_RangeDisc.SetPropertyBlock(m_Block);
            }

            // Dans l'arène du boss final, elle tire des boules de glace à la place.
            if (FiresIceBalls)
                return;

            m_TickTimer -= Time.deltaTime;
            if (m_TickTimer > 0f)
                return;

            // Le ralentissement dure un peu plus qu'un intervalle : il continue tant que l'ennemi reste dans la zone.
            m_TickTimer = k_TickInterval;
            var center = transform.position;
            var radius = Radius;
            var speed = ValueFor(m_SpeedPerLevel, Level, 0.7f);
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive)
                    continue;

                var offset = enemy.transform.position - center;
                if (offset.y < -2f - Elevation || offset.y > m_MaxHeight)
                    continue;

                offset.y = 0f;
                if (offset.sqrMagnitude <= radius * radius)
                    enemy.Slow(speed, k_TickInterval * 1.5f);
            }
        }

        protected override void OnLevelChanged(int level)
        {
            base.OnLevelChanged(level);
            if (m_RangeDisc == null)
                CreateRangeDisc();
            m_RangeDisc.transform.localScale = new Vector3(Radius, 1f, Radius);

            if (m_Crystals != null)
                m_Crystals.localScale = m_CrystalBaseScale * ValueFor(m_CrystalScalePerLevel, level, 1f);
            if (m_Light != null)
                m_Light.intensity = ValueFor(m_LightIntensityPerLevel, level, 1.5f);
        }

        // En hauteur (plateau de l'arène du boss), le disque flotterait au-dessus du sol des ennemis : on le cache.
        protected override void OnRangeChanged()
        {
            base.OnRangeChanged();
            if (m_RangeDisc == null)
                return;
            m_RangeDisc.transform.localScale = new Vector3(Radius, 1f, Radius);
            m_RangeDisc.enabled = Elevation <= 0.01f;
        }

        // Le disque de portée n'est pas une partie du bâtiment : il reste tel quel pendant une amélioration.
        protected override bool IsBlueprintPart(Renderer renderer) => renderer != m_RangeDisc;

        void CreateRangeDisc()
        {
            var go = new GameObject("Range");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * 0.04f;
            go.AddComponent<MeshFilter>().sharedMesh = IceZone.Disc;
            m_RangeDisc = go.AddComponent<MeshRenderer>();
            m_RangeDisc.sharedMaterial = m_RangeMaterial;
            m_RangeDisc.shadowCastingMode = ShadowCastingMode.Off;
            m_RangeDisc.receiveShadows = false;
            m_Block = new MaterialPropertyBlock();
            if (m_RangeMaterial == null)
                Debug.LogWarning("FrostTower : renseigne « Range Material » (shader Archery/UnlitVertexColor).", this);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.55f, 0.85f, 1f, 0.8f);
            var radius = ValueFor(m_RadiusPerLevel, Mathf.Max(1, Level), 9f);
            var previous = transform.position + Vector3.right * radius;
            for (var i = 1; i <= 48; i++)
            {
                var angle = i * Mathf.PI * 2f / 48;
                var point = transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                Gizmos.DrawLine(previous, point);
                previous = point;
            }
        }
    }
}
