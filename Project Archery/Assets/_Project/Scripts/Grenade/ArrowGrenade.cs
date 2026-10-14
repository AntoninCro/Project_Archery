using Archery.Bows;
using Archery.Core;
using Archery.Upgrades;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Grenades
{
    /// <summary>
    /// Grenade à flèches : sortie du bas du dos par la <see cref="GrenadeBelt"/>, lancée à la main,
    /// elle éclate au premier impact et projette des flèches dans toutes les directions.
    /// Elle ne fait aucun dégât elle-même : ce sont les flèches, tirées de l'<see cref="ArrowPool"/>.
    /// </summary>
    /// <remarks>
    /// À placer sur le prefab de la grenade, à côté d'un XR Grab Interactable :
    /// c'est lui qui fait suivre la main et qui donne la vitesse du bras au lâcher.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(XRGrabInteractable))]
    public class ArrowGrenade : MonoBehaviour
    {
        [Header("Flèches projetées")]
        [Tooltip("Nombre de flèches, avant l'amélioration « Grenade sauvage ».")]
        [Min(1)]
        [SerializeField]
        int m_ArrowCount = 14;

        [Tooltip("Vitesse de départ des flèches (m/s). L'arc de base tire à 35.")]
        [SerializeField]
        float m_ArrowSpeed = 16f;

        [Tooltip("Dégâts d'une flèche, avant les améliorations de dégâts. L'arc de base fait 10.")]
        [SerializeField]
        float m_ArrowDamage = 6f;

        [Tooltip("Distance entre le centre de l'explosion et le départ des flèches : elles ne naissent pas dans le sol.")]
        [SerializeField]
        float m_SpawnRadius = 0.25f;

        [Tooltip("0 = flèches réparties en sphère parfaite. 0,3 = poussées vers le haut, elles retombent en pluie.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_UpwardBias = 0.3f;

        [Tooltip("Couleur de la traînée des flèches de la grenade.")]
        [SerializeField]
        Color m_TrailColor = new Color(1f, 0.55f, 0.15f);

        [Header("Explosion")]
        [Tooltip("Délai (s) après le lâcher avant que la grenade puisse éclater : elle ne saute pas dans la main.")]
        [SerializeField]
        float m_ArmDelay = 0.12f;

        [Tooltip("Éclate d'elle-même si elle n'a rien touché au bout de ce temps (0 = jamais).")]
        [SerializeField]
        float m_FuseTime = 6f;

        [Tooltip("Optionnel : effet visuel joué au point d'explosion.")]
        [SerializeField]
        GameObject m_ExplosionEffect;

        [SerializeField]
        AudioClip m_ExplosionClip;

        [Tooltip("Vibration de la manette qui vient de la lancer.")]
        [SerializeField]
        float m_ThrowHapticAmplitude = 0.35f;

        XRGrabInteractable m_Grab;
        IXRSelectInteractor m_Hand;
        bool m_Armed;
        bool m_Exploded;
        float m_ArmedAt;

        void Awake() => m_Grab = GetComponent<XRGrabInteractable>();

        void OnEnable()
        {
            m_Grab.selectEntered.AddListener(OnSelectEntered);
            m_Grab.selectExited.AddListener(OnSelectExited);
        }

        void OnDisable()
        {
            m_Grab.selectEntered.RemoveListener(OnSelectEntered);
            m_Grab.selectExited.RemoveListener(OnSelectExited);
        }

        /// <summary>
        /// Place la grenade dans la main et la fait saisir : la ceinture ne passe pas par un survol.
        /// </summary>
        public bool TryPutInHand(IXRSelectInteractor hand)
        {
            if (hand == null || m_Grab.interactionManager == null)
                return false;

            var attach = hand.GetAttachTransform(m_Grab);
            transform.SetPositionAndRotation(attach.position, attach.rotation);

            m_Grab.interactionManager.SelectEnter(hand, (IXRSelectInteractable)m_Grab);
            return m_Grab.isSelected;
        }

        void OnSelectEntered(SelectEnterEventArgs args) => m_Hand = args.interactorObject;

        // Lâchée : elle part avec la vitesse du bras (XRI s'en charge) et devient dangereuse.
        void OnSelectExited(SelectExitEventArgs args)
        {
            if (m_Armed)
                return;

            m_Armed = true;
            m_ArmedAt = Time.time;

            Haptics.Pulse(m_Hand, m_ThrowHapticAmplitude, 0.06f);
            m_Hand = null;

            if (GrenadeBelt.Instance != null)
                GrenadeBelt.Instance.OnGrenadeThrown(this);
        }

        void Update()
        {
            if (!m_Armed || m_Exploded || m_FuseTime <= 0f)
                return;

            if (Time.time - m_ArmedAt >= m_FuseTime)
                Explode();
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!m_Armed || m_Exploded || Time.time - m_ArmedAt < m_ArmDelay)
                return;

            Explode();
        }

        /// <summary>Fait éclater la grenade tout de suite.</summary>
        [ContextMenu("Faire éclater")]
        public void Explode()
        {
            if (m_Exploded)
                return;

            m_Exploded = true;
            var center = transform.position;
            SpawnArrows(center);

            if (m_ExplosionEffect != null)
            {
                var effect = Instantiate(m_ExplosionEffect, center, Quaternion.identity);
                Destroy(effect, 5f);
            }

            Sfx.Play(m_ExplosionClip, center, 0.9f, Random.Range(0.95f, 1.05f));
            Destroy(gameObject);
        }

        void SpawnArrows(Vector3 center)
        {
            var pool = ArrowPool.Instance;
            if (pool == null)
            {
                Debug.LogWarning("Pas d'Arrow Pool dans la scène : la grenade n'a pas projeté de flèches.", this);
                return;
            }

            var upgrades = PlayerUpgrades.Instance;
            var count = m_ArrowCount;
            var damage = m_ArrowDamage;
            if (upgrades != null)
            {
                count += Mathf.RoundToInt(upgrades.Total(UpgradeEffect.GrenadeArrows));
                damage *= upgrades.DamageMultiplier;
            }

            count = Mathf.Max(1, count);
            for (var i = 0; i < count; i++)
            {
                var direction = SphereDirection(i, count, m_UpwardBias);
                var arrow = pool.Get();
                arrow.transform.SetPositionAndRotation(center + direction * m_SpawnRadius, Quaternion.LookRotation(direction));
                arrow.Launch(new ArrowLaunch
                {
                    Velocity = direction * m_ArrowSpeed,
                    Damage = damage,
                    Grade = ShotGrade.Ok,
                    TrailColor = m_TrailColor,
                    IsShot = true,

                    // Une flèche de grenade qui ne touche rien ne casse pas le combo du joueur.
                    IsExtra = true,
                });
            }
        }

        /// <summary>
        /// Répartit régulièrement des directions sur une sphère (spirale de Fibonacci), puis les
        /// incline vers le haut : les flèches retombent en pluie au lieu de rentrer dans le sol.
        /// </summary>
        static Vector3 SphereDirection(int index, int count, float upwardBias)
        {
            const float goldenAngle = 2.39996323f;

            var y = count == 1 ? 0f : 1f - 2f * index / (count - 1f);
            var radius = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            var theta = goldenAngle * index;

            var direction = new Vector3(Mathf.Cos(theta) * radius, y, Mathf.Sin(theta) * radius);
            return (direction + Vector3.up * upwardBias).normalized;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, m_SpawnRadius);
        }
    }
}
