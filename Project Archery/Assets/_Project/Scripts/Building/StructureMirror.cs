using Archery.Bows;
using Archery.Core;
using Archery.Defense;
using Archery.Enemies;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Miroir (refonte du tower defense, étape 5, lot 5 ; 28 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, parties 6.1 et 10) :
    /// au bord de la voie, tourné vers la tour du joueur. Une flèche tirée qui le touche repart vers l'ennemi le plus proche du
    /// miroir (en visant devant lui) ; sans ennemi à portée, elle rebondit comme sur un miroir. Plus loin et plus fort à chaque
    /// niveau ; au niveau 3, la flèche renvoyée traverse un ennemi de plus. Rare.
    /// </summary>
    /// <remarks>
    /// Le seul bâtiment qui garde un collider : la glace (<c>Pane</c>), que les flèches touchent. Le renvoi passe par
    /// <see cref="Arrow.Deflect"/> pendant <see cref="Arrow.AnyHit"/>.
    /// </remarks>
    public class StructureMirror : Structure
    {
        [Tooltip("Portée (m) du renvoi, depuis le miroir, par niveau.")]
        [SerializeField]
        float[] m_RangePerLevel = { 25f, 30f, 35f };

        [Tooltip("Multiplie les dégâts de la flèche renvoyée, par niveau.")]
        [SerializeField]
        float[] m_DamagePerLevel = { 1f, 1.25f, 1.5f };

        [Tooltip("Ennemis en plus que la flèche renvoyée traverse, par niveau.")]
        [SerializeField]
        int[] m_PiercePerLevel = { 0, 0, 1 };

        [Tooltip("Partie qui se tourne vers la tour du joueur (autour de la verticale).")]
        [SerializeField]
        Transform m_Pivot;

        [Tooltip("Ce que les flèches touchent : la glace.")]
        [SerializeField]
        Collider m_Pane;

        [SerializeField]
        Color m_ReflectColor = new Color(0.85f, 0.95f, 1f, 0.9f);

        [SerializeField]
        AudioClip m_ReflectClip;

        void OnEnable() => Arrow.AnyHit += OnArrowHit;

        void OnDisable() => Arrow.AnyHit -= OnArrowHit;

        protected override void Update()
        {
            base.Update();
            if (!IsBuilt || m_Pivot == null)
                return;

            var tower = Tower.Instance;
            if (tower == null)
                return;

            var toTower = tower.transform.position - m_Pivot.position;
            toTower.y = 0f;
            if (toTower.sqrMagnitude > 1e-3f)
                m_Pivot.rotation = Quaternion.RotateTowards(m_Pivot.rotation, Quaternion.LookRotation(toTower), 90f * Time.deltaTime);
        }

        void OnArrowHit(ArrowHit hit)
        {
            if (!IsBuilt || !hit.IsShot || hit.Arrow == null || m_Pane == null || hit.Collider != m_Pane)
                return;

            var arrow = hit.Arrow;
            var level = Mathf.Max(1, Level);
            var speed = Mathf.Max(20f, hit.Speed);
            var target = Nearest(hit.Point, ValueFor(m_RangePerLevel, level, 25f) * RangeScale * OverchargeRange);
            if (target == null)
            {
                arrow.Deflect(Vector3.Reflect(hit.Direction, hit.Normal) * speed);
                return;
            }

            // Là où l'ennemi sera, un peu plus haut pour la chute de la flèche.
            var center = target.Center;
            var time = Vector3.Distance(hit.Point, center) / speed;
            var aim = center + target.Velocity * time - 0.5f * time * time * arrow.GravityScale * Physics.gravity;
            arrow.Deflect((aim - hit.Point).normalized * speed);
            arrow.Damage *= ValueFor(m_DamagePerLevel, level, 1f);
            arrow.AddPierce(ValueFor(m_PiercePerLevel, level, 0));
            arrow.SetTrailColor(m_ReflectColor);
            Sfx.Play(m_ReflectClip, hit.Point, 0.9f, Random.Range(0.95f, 1.1f));
        }

        static Enemy Nearest(Vector3 from, float range)
        {
            Enemy best = null;
            var bestDistance = range * range;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive)
                    continue;

                var distance = (enemy.Center - from).sqrMagnitude;
                if (distance < bestDistance)
                {
                    best = enemy;
                    bestDistance = distance;
                }
            }

            return best;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.85f, 0.95f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, ValueFor(m_RangePerLevel, Mathf.Max(1, Level), 25f));
        }
    }
}
