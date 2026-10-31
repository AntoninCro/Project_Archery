using Archery.Bows;
using Archery.Combat;
using Archery.Core;
using Archery.Defense;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Seau d'huile (idée du joueur ; refonte du tower defense, étape 5, lot 6 ; 28 octobre ; <c>Docs/Refonte_TowerDefense.md</c>,
    /// partie 6.1) : sur la tour, à côté du joueur. Toucher sa cible (une flèche tirée, ou tenue en main) renverse le seau : l'huile
    /// bouillante blesse tous les ennemis au sol autour du pied de la tour et les ralentit. Puis il se remplit. Plus fort, plus
    /// large et plus vite rempli à chaque niveau. Rare (proposée).
    /// </summary>
    /// <remarks>
    /// La cible (<c>Target</c>) est un collider que les flèches touchent (<see cref="Arrow.AnyHit"/>) ; elle brille quand le
    /// seau est plein. Le seau (<c>Bucket</c>) bascule autour de son axe X ; le filet d'huile (<c>Stream</c>) descend de sa bouche
    /// jusqu'au pied de la tour pendant le versement.
    /// </remarks>
    public class StructureOilBucket : Structure
    {
        [Tooltip("Dégâts de l'huile, par niveau, à la vague 1 en Normal (ils suivent ensuite la force des ennemis).")]
        [SerializeField]
        float[] m_DamagePerLevel = { 40f, 60f, 90f };

        [Tooltip("Rayon (m) autour du pied de la tour, par niveau.")]
        [SerializeField]
        float[] m_RadiusPerLevel = { 8f, 9f, 10f };

        [Tooltip("Temps (s) pour que le seau se remplisse, par niveau.")]
        [SerializeField]
        float[] m_RefillPerLevel = { 20f, 16f, 12f };

        [Tooltip("Ralentissement des ennemis touchés (0,6 = 40 % plus lents) et sa durée (s).")]
        [SerializeField]
        float m_SlowMultiplier = 0.6f;

        [SerializeField]
        float m_SlowDuration = 3f;

        [Tooltip("La cible à toucher : un collider.")]
        [SerializeField]
        Collider m_Target;

        [Tooltip("Ce qui change de matériau selon que le seau est plein (il brille) ou vide.")]
        [SerializeField]
        Renderer m_TargetFace;

        [SerializeField]
        Material m_FullMaterial;

        [SerializeField]
        Material m_EmptyMaterial;

        [Tooltip("Le seau : il bascule autour de son axe X (vers l'avant, l'extérieur de la tour).")]
        [SerializeField]
        Transform m_Bucket;

        [Tooltip("Le filet d'huile : posé à la bouche du seau, son échelle Y donne sa longueur (m) vers le bas.")]
        [SerializeField]
        Transform m_Stream;

        [SerializeField]
        AudioClip m_PourClip;

        Quaternion m_BucketRest = Quaternion.identity;
        float m_Refill;
        float m_Pour;
        bool m_ShownFull;

        /// <summary>Plein : toucher la cible le renverse.</summary>
        public bool IsFull => m_Refill <= 0f;

        protected override void Awake()
        {
            if (m_Bucket != null)
                m_BucketRest = m_Bucket.localRotation;
            if (m_Stream != null)
                m_Stream.gameObject.SetActive(false);
            base.Awake();
        }

        void OnEnable() => Arrow.AnyHit += OnArrowHit;

        void OnDisable() => Arrow.AnyHit -= OnArrowHit;

        // Le filet d'huile n'est pas une pièce du bâtiment.
        protected override bool IsBlueprintPart(Renderer renderer) => m_Stream == null || !renderer.transform.IsChildOf(m_Stream);

        protected override void Update()
        {
            base.Update();
            if (!IsBuilt)
                return;

            if (m_Refill > 0f)
                m_Refill -= Time.deltaTime * OverchargeRate;
            ShowFull(IsFull);

            // Le seau bascule d'un coup, reste penché pendant que l'huile coule, puis se redresse.
            m_Pour = Mathf.MoveTowards(m_Pour, 0f, Time.deltaTime * 0.6f);
            var tilt = Mathf.Clamp01((1f - m_Pour) * 10f) * Mathf.Clamp01(m_Pour * 3f);
            if (m_Bucket != null)
                m_Bucket.localRotation = m_BucketRest * Quaternion.Euler(110f * tilt, 0f, 0f);
            UpdateStream(m_Pour > 0.35f && m_Pour < 0.9f);
        }

        void ShowFull(bool full)
        {
            if (m_TargetFace == null || m_ShownFull == full)
                return;

            m_ShownFull = full;
            var material = full ? m_FullMaterial : m_EmptyMaterial;
            if (material != null)
                m_TargetFace.sharedMaterial = material;
        }

        void UpdateStream(bool visible)
        {
            if (m_Stream == null)
                return;

            if (m_Stream.gameObject.activeSelf != visible)
                m_Stream.gameObject.SetActive(visible);
            if (!visible)
                return;

            var tower = Tower.Instance;
            var ground = tower != null ? tower.transform.position.y : m_Stream.position.y - 6f;
            var length = Mathf.Max(0.5f, m_Stream.position.y - ground);
            m_Stream.rotation = Quaternion.identity;
            m_Stream.localScale = new Vector3(1f, length, 1f);
        }

        void OnArrowHit(ArrowHit hit)
        {
            if (IsBuilt && IsFull && m_Target != null && hit.Collider == m_Target)
                Pour();
        }

        /// <summary>Renverse le seau, s'il est plein.</summary>
        public void Pour()
        {
            if (!IsBuilt || !IsFull)
                return;

            var level = Mathf.Max(1, Level);
            m_Refill = ValueFor(m_RefillPerLevel, level, 20f);
            m_Pour = 1f;

            var tower = Tower.Instance;
            var center = tower != null ? tower.transform.position : transform.position;
            var radius = ValueFor(m_RadiusPerLevel, level, 8f);
            var damage = ValueFor(m_DamagePerLevel, level, 40f) * Strength;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive || enemy.IsFlyer)
                    continue;

                var offset = enemy.transform.position - center;
                offset.y = 0f;
                if (offset.sqrMagnitude > radius * radius)
                    continue;

                enemy.Health.TakeDamage(new DamageInfo
                {
                    Amount = damage,
                    Zone = HitZone.Body,
                    Point = enemy.Center,
                    Direction = Vector3.down,
                    Source = this,
                });
                if (enemy.IsAlive)
                    enemy.Slow(m_SlowMultiplier, m_SlowDuration);
            }

            Sfx.Play(m_PourClip, m_Bucket != null ? m_Bucket.position : transform.position, 1f, Random.Range(0.9f, 1.05f));
        }

        void OnDrawGizmosSelected()
        {
            var tower = Tower.Instance;
            Gizmos.color = new Color(0.9f, 0.7f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(tower != null ? tower.transform.position : transform.position, ValueFor(m_RadiusPerLevel, Mathf.Max(1, Level), 8f));
        }
    }
}
