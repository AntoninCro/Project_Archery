using Archery.Bows;
using Archery.Core;
using Archery.Enemies;
using UnityEngine;

namespace Archery.Defense
{
    /// <summary>
    /// Brasero du sommet de la tour (GDD, sections 6.2 et 9), allumé dès le début de la partie. On y trempe la pointe
    /// d'une flèche tenue en main pour l'enflammer : sa cible brûle pendant 3 s et perd en plus 30 % des dégâts
    /// de la flèche, étalés sur la brûlure. Les flèches en plus d'un tir enflammé (multitir, écho, déluge) brûlent aussi.
    /// </summary>
    [DisallowMultipleComponent]
    public class Brazier : MonoBehaviour
    {
        [Tooltip("Le feu : flammes, lumière, son.")]
        [SerializeField]
        GameObject m_Fire;

        [Tooltip("Point au cœur des flammes, où tremper la pointe de la flèche. Vide : 0,4 m au-dessus du brasero.")]
        [SerializeField]
        Transform m_DipPoint;

        [Tooltip("Distance (m) entre la pointe de la flèche et ce point pour l'enflammer.")]
        [SerializeField]
        float m_DipRadius = 0.3f;

        [Tooltip("Effet attaché à la pointe d'une flèche enflammée (petites flammes).")]
        [SerializeField]
        GameObject m_ArrowFireEffect;

        [Tooltip("Effet attaché à un ennemi qui brûle.")]
        [SerializeField]
        GameObject m_BurnEffect;

        [Tooltip("Durée (s) de la brûlure.")]
        [SerializeField]
        float m_BurnDuration = 3f;

        [Tooltip("Dégâts de la brûlure, en part des dégâts de la flèche (0,3 = 30 % en plus), étalés sur toute sa durée.")]
        [SerializeField]
        float m_BurnDamage = 0.3f;

        [SerializeField]
        AudioClip m_IgniteClip;

        public static Brazier Instance { get; private set; }

        Vector3 DipPoint => m_DipPoint != null ? m_DipPoint.position : transform.position + Vector3.up * 0.4f;

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs Brazier dans la scène.", this);
            Instance = this;

            if (m_Fire != null)
                m_Fire.SetActive(true);
        }

        void OnEnable() => Arrow.AnyHit += OnArrowHit;

        void OnDisable() => Arrow.AnyHit -= OnArrowHit;

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // Une flèche tenue en main dont la pointe touche les flammes s'enflamme.
        void Update()
        {
            var dipPoint = DipPoint;
            foreach (var arrow in Arrow.HeldArrows)
            {
                if (arrow == null || arrow.IsOnFire || (arrow.TipPosition - dipPoint).sqrMagnitude > m_DipRadius * m_DipRadius)
                    continue;

                IgniteArrow(arrow);
                Haptics.Pulse(arrow.Hand, 0.5f, 0.1f);
                Sfx.Play(m_IgniteClip, dipPoint, 0.9f, Random.Range(0.95f, 1.05f));
            }
        }

        /// <summary>Enflamme cette flèche (trempée dans le feu, ou flèche en plus d'un tir enflammé).</summary>
        public void IgniteArrow(Arrow arrow)
        {
            if (arrow != null)
                arrow.Ignite(m_ArrowFireEffect);
        }

        void OnArrowHit(ArrowHit hit)
        {
            if (!hit.IsShot || hit.Arrow == null || !hit.Arrow.IsOnFire || hit.Collider == null)
                return;

            var enemy = hit.Collider.GetComponentInParent<Enemy>();
            if (enemy != null)
                Burning.Apply(enemy, hit.Damage * m_BurnDamage, m_BurnDuration, hit.Grade, hit.TravelDistance, m_BurnEffect);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.1f);
            Gizmos.DrawWireSphere(DipPoint, m_DipRadius);
        }
    }
}
