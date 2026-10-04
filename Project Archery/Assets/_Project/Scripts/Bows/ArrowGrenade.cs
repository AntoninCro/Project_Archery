using Archery.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Archery.Bows
{
    /// <summary>
    /// Grenade de flèches (GDD, section 23) : on la lance, et elle éclate au premier choc, ou au bout d'un court délai,
    /// en projetant des flèches tout autour d'elle. Elle se prend en bas du dos (<see cref="GrenadeHolster"/>).
    /// </summary>
    /// <remarks>
    /// À placer sur un objet lançable : <see cref="XRGrabInteractable"/> avec <c>Throw On Detach</c> et un <see cref="Rigidbody"/>.
    /// Les flèches font les dégâts d'un tir orange de l'arc et ne cassent pas le combo.
    /// </remarks>
    [RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody))]
    public class ArrowGrenade : MonoBehaviour
    {
        [Tooltip("Nombre de flèches projetées.")]
        [SerializeField]
        int m_ArrowCount = 24;

        [Tooltip("Vitesse des flèches (m/s).")]
        [SerializeField]
        float m_ArrowSpeed = 25f;

        [Tooltip("Les flèches font les dégâts d'un tir de cette qualité.")]
        [SerializeField]
        ShotGrade m_Grade = ShotGrade.Good;

        [Tooltip("Délai (s) entre le lancer et l'explosion, si elle ne touche rien avant.")]
        [SerializeField]
        float m_FuseTime = 1.5f;

        [Tooltip("Couleur de la traînée des flèches.")]
        [SerializeField]
        Color m_TrailColor = new Color(1f, 0.55f, 0.15f);

        [Tooltip("Optionnel : effet créé à l'explosion (particules), détruit au bout de 3 s.")]
        [SerializeField]
        GameObject m_ExplosionEffect;

        [SerializeField]
        AudioClip m_ExplosionClip;

        XRGrabInteractable m_Grab;
        float m_Fuse = -1f;
        float m_Armed;
        bool m_Exploded;

        void Awake()
        {
            m_Grab = GetComponent<XRGrabInteractable>();
            m_Grab.selectExited.AddListener(OnReleased);
        }

        void OnDestroy()
        {
            if (m_Grab != null)
                m_Grab.selectExited.RemoveListener(OnReleased);
        }

        // Lâchée ou lancée : le compte à rebours commence.
        void OnReleased(SelectExitEventArgs args)
        {
            if (!m_Exploded)
                m_Fuse = m_FuseTime;
        }

        void Update()
        {
            if (m_Fuse < 0f || m_Exploded)
                return;

            m_Armed += Time.deltaTime;
            m_Fuse -= Time.deltaTime;
            if (m_Fuse <= 0f)
                Explode();
        }

        // Au premier choc après le lancer (pas contre la main qui vient de la lâcher).
        void OnCollisionEnter(Collision collision)
        {
            if (m_Fuse >= 0f && m_Armed > 0.15f && !ArrowIgnore.IsIgnored(collision.collider))
                Explode();
        }

        void Explode()
        {
            if (m_Exploded)
                return;

            m_Exploded = true;
            var center = transform.position;
            var pool = ArrowPool.Instance;
            var bow = FindAnyObjectByType<Bow>();
            var damage = bow != null ? bow.MeleeDamage(m_Grade) : 10f;

            // Deux couronnes de flèches : l'une presque à l'horizontale, l'autre un peu plus haut, décalée.
            if (pool != null)
            {
                for (var i = 0; i < m_ArrowCount; i++)
                {
                    var upper = i % 2 == 1;
                    var yaw = 360f * i / m_ArrowCount;
                    var direction = Quaternion.Euler(upper ? -22f : -6f, yaw, 0f) * Vector3.forward;
                    var arrow = pool.Get();
                    arrow.transform.SetPositionAndRotation(center + direction * 0.3f, Quaternion.LookRotation(direction));
                    arrow.Launch(new ArrowLaunch
                    {
                        Velocity = direction * m_ArrowSpeed,
                        Damage = damage,
                        Grade = m_Grade,
                        TrailColor = m_TrailColor,
                        IsShot = true,
                        IsExtra = true,
                    });
                }
            }

            Sfx.Play(m_ExplosionClip, center, 1f, Random.Range(0.95f, 1.05f));
            if (m_ExplosionEffect != null)
                Destroy(Instantiate(m_ExplosionEffect, center, Quaternion.identity), 3f);
            Destroy(gameObject);
        }
    }
}
