using System;
using Archery.Difficulty;
using UnityEngine;

namespace Archery.World
{
    /// <summary>
    /// Affiche des objets seulement quand le ciel de la difficulté est un ciel de nuit (GDD, section 11) :
    /// la lanterne de la tour, les yeux qui brillent des ennemis…
    /// </summary>
    /// <remarks>
    /// À placer sur un objet qui reste actif (la tour, la racine de l'ennemi), pas sur les objets à allumer.
    /// </remarks>
    public class NightOnly : MonoBehaviour
    {
        [Tooltip("Objets allumés la nuit et cachés le jour.")]
        [SerializeField]
        GameObject[] m_Targets = Array.Empty<GameObject>();

        [Tooltip("Inverse : objets visibles seulement le jour.")]
        [SerializeField]
        bool m_DayOnly;

        void Awake()
        {
            if (m_Targets.Length == 0)
                Debug.LogWarning("NightOnly : la liste « Targets » est vide, rien ne sera allumé la nuit.", this);
        }

        void OnEnable()
        {
            DifficultyManager.Changed += OnDifficultyChanged;
            Refresh();
        }

        void OnDisable()
        {
            DifficultyManager.Changed -= OnDifficultyChanged;
        }

        void OnDifficultyChanged(DifficultyDefinition difficulty) => Refresh();

        void Refresh()
        {
            var visible = DifficultyManager.Current.sky.night != m_DayOnly;
            foreach (var target in m_Targets)
            {
                if (target != null && target != gameObject)
                    target.SetActive(visible);
            }
        }
    }
}
