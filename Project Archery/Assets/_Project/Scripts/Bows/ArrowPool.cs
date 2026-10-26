using System.Collections.Generic;
using UnityEngine;

namespace Archery.Bows
{
    /// <summary>
    /// Réserve de flèches réutilisables, pour ne pas créer et détruire un objet à chaque tir.
    /// </summary>
    [DisallowMultipleComponent]
    public class ArrowPool : MonoBehaviour
    {
        [SerializeField]
        Arrow m_ArrowPrefab;

        [Tooltip("Nombre de flèches créées au lancement.")]
        [SerializeField]
        int m_Prewarm = 12;

        [Tooltip("Au-delà, les flèches plantées les plus anciennes disparaissent.")]
        [SerializeField]
        int m_MaxStuckArrows = 40;

        static ArrowPool s_Instance;

        readonly Stack<Arrow> m_Free = new Stack<Arrow>();
        readonly List<Arrow> m_Stuck = new List<Arrow>();
        int m_Created;

        public static ArrowPool Instance => s_Instance;

        /// <summary>Le modèle des flèches (26 octobre : l'atelier en montre une copie, sans script, qui tourne).</summary>
        public Arrow Prefab => m_ArrowPrefab;

        /// <summary>Une flèche sort de la réserve (vers une main, un tir, une volée) : 26 octobre, elle prend les couleurs des pièces.</summary>
        public static event System.Action<Arrow> Taken;

        /// <summary>Les flèches sorties de la réserve : en main, en vol ou plantées (pour <c>Core/PerfProbe</c>).</summary>
        public int InUse => m_Created - m_Free.Count;

        /// <summary>Les flèches plantées dans le décor ou dans un ennemi.</summary>
        public int StuckCount => m_Stuck.Count;

        void Awake()
        {
            if (s_Instance != null && s_Instance != this)
                Debug.LogWarning("Il y a plusieurs ArrowPool dans la scène, seul le dernier est utilisé.", this);

            s_Instance = this;
            if (m_ArrowPrefab == null)
            {
                Debug.LogError("ArrowPool : aucun prefab de flèche assigné.", this);
                return;
            }

            for (var i = 0; i < m_Prewarm; i++)
                m_Free.Push(CreateArrow());
        }

        void OnDestroy()
        {
            if (s_Instance == this)
                s_Instance = null;
        }

        public Arrow Get()
        {
            var arrow = m_Free.Count > 0 ? m_Free.Pop() : CreateArrow();
            arrow.gameObject.SetActive(true);
            Taken?.Invoke(arrow);
            return arrow;
        }

        /// <summary>Toutes les flèches créées, en réserve ou sorties.</summary>
        public Arrow[] AllArrows() => GetComponentsInChildren<Arrow>(true);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Taken = null;

        public void Release(Arrow arrow)
        {
            if (arrow == null)
                return;

            m_Stuck.Remove(arrow);
            arrow.gameObject.SetActive(false);
            arrow.transform.SetParent(transform, false);
            if (!m_Free.Contains(arrow))
                m_Free.Push(arrow);
        }

        internal void NotifyStuck(Arrow arrow)
        {
            m_Stuck.Add(arrow);
            while (m_Stuck.Count > m_MaxStuckArrows)
            {
                var oldest = m_Stuck[0];
                m_Stuck.RemoveAt(0);
                if (oldest != null)
                    oldest.Despawn();
            }
        }

        Arrow CreateArrow()
        {
            var arrow = Instantiate(m_ArrowPrefab, transform);
            m_Created++;
            arrow.name = m_ArrowPrefab.name;
            arrow.Pool = this;
            arrow.gameObject.SetActive(false);
            return arrow;
        }
    }
}
