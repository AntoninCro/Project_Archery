using Archery.Combat;
using Archery.Core;
using Archery.Player;
using UnityEngine;

namespace Archery.Enemies
{
    /// <summary>
    /// Attaque à distance d'un tireur (GDD, section 10) : au lieu de frapper, l'ennemi lance un projectile lent
    /// vers la tête du joueur. On peut l'esquiver ou l'abattre d'une flèche.
    /// </summary>
    /// <remarks>
    /// À placer à côté d'<see cref="Enemy"/>. La portée, le rythme et les dégâts viennent de son <see cref="EnemyDefinition"/>
    /// (par exemple <c>Attack Range</c> 25 m : il s'arrête vers 20 m de sa cible et tire).
    /// </remarks>
    [RequireComponent(typeof(Enemy))]
    public class EnemyRangedAttack : MonoBehaviour
    {
        [SerializeField]
        EnemyProjectile m_ProjectilePrefab;

        [Tooltip("Point de départ des projectiles (la main, le bâton…). Vide : 1,6 m au-dessus de l'ennemi.")]
        [SerializeField]
        Transform m_Muzzle;

        [Tooltip("Vitesse des projectiles (m/s) : lente, pour qu'on puisse les esquiver.")]
        [SerializeField]
        float m_ProjectileSpeed = 9f;

        [Tooltip("Imprécision (°) de la visée.")]
        [SerializeField]
        float m_Spread = 2f;

        [Tooltip("Optionnel : son du tir.")]
        [SerializeField]
        AudioClip m_FireClip;

        /// <summary>Lance un projectile vers la cible (la tête du joueur, ou le haut de la tour).</summary>
        public void Fire(Health target, float damage)
        {
            if (m_ProjectilePrefab == null || target == null)
                return;

            var origin = m_Muzzle != null ? m_Muzzle.position : transform.position + Vector3.up * 1.6f;
            var aim = AimPoint(target);
            var direction = aim - origin;
            if (direction.sqrMagnitude < 1e-4f)
                return;

            direction = Quaternion.Euler(Random.Range(-m_Spread, m_Spread), Random.Range(-m_Spread, m_Spread), 0f) * direction.normalized;
            var projectile = Instantiate(m_ProjectilePrefab, origin, Quaternion.LookRotation(direction));
            projectile.Launch(direction * m_ProjectileSpeed, damage, GetComponent<Enemy>());
            Sfx.Play(m_FireClip, origin, 0.9f, Random.Range(0.95f, 1.05f));
        }

        static Vector3 AimPoint(Health target)
        {
            var player = PlayerHealth.Instance;
            var rig = PlayerRig.Instance;
            if (player != null && target == player.Health && rig != null && rig.Head != null)
                return rig.Head.position + Vector3.down * 0.15f;

            return target.transform.position + Vector3.up * 2f;
        }
    }
}
