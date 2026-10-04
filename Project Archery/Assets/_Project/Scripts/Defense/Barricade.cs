using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using UnityEngine;

namespace Archery.Defense
{
    /// <summary>
    /// Barricade à une entrée de la clairière (GDD, section 9) : un mur qui arrête les ennemis au sol.
    /// Ceux qui arrivent devant elle (côté forêt) s'arrêtent pour la frapper, et ne passent qu'une fois qu'elle est détruite.
    /// Elle se répare en boutique, toutes les barricades d'un coup.
    /// </summary>
    /// <remarks>
    /// L'avant de la barricade (axe Z, bleu) doit pointer vers la forêt, d'où viennent les ennemis.
    /// Les volants passent au-dessus.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public class Barricade : MonoBehaviour
    {
        static readonly List<Barricade> s_All = new List<Barricade>();

        [Tooltip("Ce qui s'affiche quand la barricade est debout (les planches et leurs colliders). Caché quand elle est détruite.")]
        [SerializeField]
        GameObject m_Intact;

        [Tooltip("Optionnel : ce qui s'affiche quand elle est détruite (débris).")]
        [SerializeField]
        GameObject m_Broken;

        [Tooltip("Rayon (m) de la zone devant la barricade : un ennemi au sol qui y entre s'arrête pour la frapper.")]
        [SerializeField]
        float m_BlockRadius = 5f;

        [Tooltip("La barricade est debout au début de la partie.")]
        [SerializeField]
        bool m_StartBuilt = true;

        [SerializeField]
        AudioClip m_BreakClip;

        [SerializeField]
        AudioClip m_BuildClip;

        Health m_Health;
        Collider[] m_Colliders = System.Array.Empty<Collider>();
        bool m_Silent;

        /// <summary>Toutes les barricades de la scène.</summary>
        public static IReadOnlyList<Barricade> All => s_All;

        public Health Health => m_Health;
        public bool IsStanding => m_Health != null && m_Health.IsAlive;

        void Awake()
        {
            m_Health = GetComponent<Health>();
            if (m_Intact != null)
                m_Colliders = m_Intact.GetComponentsInChildren<Collider>(true);
            else
                Debug.LogWarning("Barricade : renseigne « Intact » (les planches).", this);
        }

        void OnEnable()
        {
            s_All.Add(this);
            m_Health.Died += OnDied;
        }

        void OnDisable()
        {
            s_All.Remove(this);
            m_Health.Died -= OnDied;
        }

        void Start()
        {
            if (m_StartBuilt)
            {
                ShowIntact(true);
                return;
            }

            // Détruite au départ, sans bruit : il faudra l'acheter en boutique.
            m_Silent = true;
            m_Health.TakeDamage(new DamageInfo { Amount = m_Health.Max * 10f, Point = transform.position, Source = this });
            m_Silent = false;
        }

        /// <summary>Remet la barricade debout, avec tous ses PV.</summary>
        public void Rebuild()
        {
            m_Health.ResetHealth(m_Health.Max);
            ShowIntact(true);
            Sfx.Play(m_BuildClip, transform.position + Vector3.up, 0.9f);
        }

        /// <summary>Répare ou reconstruit toutes les barricades (achat en boutique).</summary>
        public static void RebuildAll()
        {
            foreach (var barricade in s_All)
            {
                if (barricade != null && barricade.m_Health.Current < barricade.m_Health.Max)
                    barricade.Rebuild();
            }
        }

        /// <summary>PV de toutes les barricades : actuels et maximum.</summary>
        public static void TotalHealth(out float current, out float max)
        {
            current = 0f;
            max = 0f;
            foreach (var barricade in s_All)
            {
                if (barricade == null)
                    continue;
                current += barricade.m_Health.Current;
                max += barricade.m_Health.Max;
            }
        }

        /// <summary>
        /// La barricade debout qui arrête un ennemi au sol à cette position : il est dans sa zone, du côté de la forêt.
        /// Renvoie null si aucune ne l'arrête.
        /// </summary>
        public static Barricade FindBlocking(Vector3 position)
        {
            foreach (var barricade in s_All)
            {
                if (barricade == null || !barricade.IsStanding)
                    continue;

                var offset = position - barricade.transform.position;
                offset.y = 0f;
                if (offset.sqrMagnitude > barricade.m_BlockRadius * barricade.m_BlockRadius)
                    continue;

                // Les ennemis déjà entrés dans la clairière (derrière la barricade) ne sont pas concernés.
                if (Vector3.Dot(offset, barricade.transform.forward) < -0.5f)
                    continue;

                return barricade;
            }

            return null;
        }

        /// <summary>Point de la barricade le plus proche : c'est là que les ennemis frappent.</summary>
        public Vector3 ClosestPoint(Vector3 from)
        {
            var best = transform.position;
            var bestDistance = float.MaxValue;
            foreach (var collider in m_Colliders)
            {
                if (collider == null || !collider.enabled)
                    continue;

                var point = collider.ClosestPoint(from);
                var distance = (point - from).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = point;
                }
            }

            return best;
        }

        void OnDied(Health health, DamageInfo info)
        {
            ShowIntact(false);
            if (!m_Silent)
                Sfx.Play(m_BreakClip, transform.position + Vector3.up, 1f);
        }

        void ShowIntact(bool intact)
        {
            if (m_Intact != null)
                m_Intact.SetActive(intact);
            if (m_Broken != null)
                m_Broken.SetActive(!intact);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, m_BlockRadius);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * m_BlockRadius);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_All.Clear();
    }
}
