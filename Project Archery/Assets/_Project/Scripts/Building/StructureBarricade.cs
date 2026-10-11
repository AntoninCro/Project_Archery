using Archery.Combat;
using Archery.Core;
using Archery.Defense;
using Archery.Enemies;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Barricade construite sur un socle de voie (GDD, section 9) : une <see cref="Barricade"/> comme celles de départ,
    /// que les ennemis au sol frappent avant de passer. Ses PV montent avec le niveau ; au niveau 3, des pointes blessent
    /// ceux qui la frappent. La réparation de la boutique la remet debout avec les autres.
    /// </summary>
    /// <remarks>
    /// La <see cref="Barricade"/> reste désactivée tant que le plan n'est pas construit : un plan n'arrête personne.
    /// L'avant du bâtiment (axe Z) pointe vers la forêt, comme le socle.
    /// </remarks>
    [RequireComponent(typeof(Barricade), typeof(Health))]
    public class StructureBarricade : Structure
    {
        [Tooltip("PV de la barricade à chaque niveau.")]
        [SerializeField]
        float[] m_HealthPerLevel = { 400f, 700f, 1000f };

        [Tooltip("Niveau à partir duquel des pointes blessent les ennemis qui frappent la barricade.")]
        [SerializeField]
        int m_ThornsFromLevel = 3;

        [Tooltip("Dégâts des pointes à chaque coup reçu au corps à corps, à la vague 1 en Normal (ils suivent ensuite la force des ennemis).")]
        [SerializeField]
        float m_ThornsDamage = 10f;

        [SerializeField]
        AudioClip m_ThornsClip;

        Barricade m_Barricade;
        Health m_Health;

        protected override void Awake()
        {
            base.Awake();
            m_Barricade = GetComponent<Barricade>();
            m_Health = GetComponent<Health>();

            // Posée à la main dans la scène, elle est debout dès le départ (Structure.Start).
            m_Barricade.enabled = false;
        }

        void OnEnable() => m_Health.Damaged += OnDamaged;

        void OnDisable() => m_Health.Damaged -= OnDamaged;

        protected override void OnLevelChanged(int level)
        {
            base.OnLevelChanged(level);
            m_Barricade.enabled = true;
            m_Health.ResetHealth(ValueFor(m_HealthPerLevel, level, 400f));
            m_Barricade.Rebuild(false);
        }

        // Les pointes : un ennemi qui frappe la barricade (corps à corps) se blesse. Les projectiles ne comptent pas.
        void OnDamaged(Health health, DamageInfo info)
        {
            if (Level < m_ThornsFromLevel || !(info.Source is Enemy enemy) || !enemy.IsAlive)
                return;

            var direction = enemy.Center - info.Point;
            enemy.Health.TakeDamage(new DamageInfo
            {
                Amount = m_ThornsDamage * Strength,
                Zone = HitZone.Body,
                Point = enemy.Center,
                Direction = direction.sqrMagnitude > 1e-4f ? direction.normalized : transform.forward,
                Source = this,
            });
            Sfx.Play(m_ThornsClip, info.Point, 0.7f, Random.Range(0.9f, 1.1f));
        }
    }
}
