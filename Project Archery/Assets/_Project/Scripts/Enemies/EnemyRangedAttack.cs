using System.Collections;
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
    /// Avec un <c>Charge Time</c> (liches), il charge d'abord une grosse boule au bout de son bâton, qui grossit, puis
    /// la lance au moment de son coup de bâton : on la voit venir, et on peut l'abattre dès la charge.
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

        [Header("Boule chargée (liche)")]
        [Tooltip("Durée (s) de la charge avant le lancer. 0 : tir immédiat (tireur).")]
        [SerializeField]
        float m_ChargeTime;

        [Tooltip("Taille de la boule chargée, en fois celle du projectile (sa zone de touche grandit d'autant).")]
        [SerializeField]
        float m_ChargedSize = 2.2f;

        [Tooltip("Optionnel : son de la charge (joué grave au début).")]
        [SerializeField]
        AudioClip m_ChargeClip;

        Enemy m_Enemy;
        EnemyProjectile m_Charging;

        /// <summary>Il charge ses boules avant de les lancer : c'est lui qui lance l'animation d'attaque, au lancer.</summary>
        public bool Charges => m_ChargeTime > 0f;

        void Awake() => m_Enemy = GetComponent<Enemy>();

        void OnDisable()
        {
            StopAllCoroutines();
            if (m_Charging != null)
                Destroy(m_Charging.gameObject);
            m_Charging = null;
        }

        Vector3 Origin => m_Muzzle != null ? m_Muzzle.position : transform.position + Vector3.up * 1.6f;

        /// <summary>Lance un projectile vers la cible (la tête du joueur, ou le haut de la tour), ou commence à le charger.</summary>
        public void Fire(Health target, float damage)
        {
            if (m_ProjectilePrefab == null || target == null)
                return;

            if (Charges)
            {
                if (m_Charging == null)
                    StartCoroutine(Charge(target, damage));
                return;
            }

            var origin = Origin;
            if (!TryAim(target, origin, out var direction))
                return;

            var projectile = Instantiate(m_ProjectilePrefab, origin, Quaternion.LookRotation(direction));
            projectile.Launch(direction * m_ProjectileSpeed, damage, m_Enemy);
            Sfx.Play(m_FireClip, origin, 0.9f, Random.Range(0.95f, 1.05f));
        }

        // La boule grossit au bout du bâton ; le coup de bâton part juste avant la fin, et la boule avec lui (visée au
        // dernier moment : on peut se décaler pendant la charge). Le lanceur mort, la boule se dissipe ; abattue d'une
        // flèche, l'attaque est perdue.
        IEnumerator Charge(Health target, float damage)
        {
            var ball = Instantiate(m_ProjectilePrefab, Origin, Quaternion.identity);
            m_Charging = ball;
            ball.Charge(Origin, 0f, m_ChargedSize);
            Sfx.Play(m_ChargeClip != null ? m_ChargeClip : m_FireClip, Origin, 0.7f, m_ChargeClip != null ? 1f : 0.55f);

            var windup = m_Enemy != null && m_Enemy.Definition != null ? m_Enemy.Definition.attackWindup : 0.4f;
            var elapsed = 0f;
            var swung = false;
            while (elapsed < m_ChargeTime)
            {
                if (ball == null || !ball.IsHeld)
                {
                    m_Charging = null;
                    yield break;
                }

                if (m_Enemy == null || !m_Enemy.IsAlive || m_Enemy.IsFleeing)
                {
                    ball.Fizzle();
                    m_Charging = null;
                    yield break;
                }

                // Ralenti par la glace, il charge plus lentement.
                elapsed += Time.deltaTime * m_Enemy.SlowMultiplier;
                ball.Charge(Origin, Mathf.Clamp01(elapsed / m_ChargeTime), m_ChargedSize);
                if (!swung && elapsed >= m_ChargeTime - windup)
                {
                    swung = true;
                    m_Enemy.PlayAttackAnimation();
                }

                yield return null;
            }

            m_Charging = null;
            var origin = Origin;
            if (target == null || !target.IsAlive || !TryAim(target, origin, out var direction))
            {
                ball.Fizzle();
                yield break;
            }

            ball.Launch(direction * m_ProjectileSpeed, damage, m_Enemy);
            Sfx.Play(m_FireClip, origin, 1f, Random.Range(0.8f, 0.9f));
        }

        bool TryAim(Health target, Vector3 origin, out Vector3 direction)
        {
            direction = AimPoint(target) - origin;
            if (direction.sqrMagnitude < 1e-4f)
                return false;

            direction = Quaternion.Euler(Random.Range(-m_Spread, m_Spread), Random.Range(-m_Spread, m_Spread), 0f) * direction.normalized;
            return true;
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
