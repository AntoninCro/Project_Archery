using System;
using System.Collections.Generic;
using Archery.Waves;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace Archery.Difficulty
{
    /// <summary>
    /// Difficulté de la partie (GDD, section 11). Elle se choisit avant la première vague,
    /// et reste la même quand la scène recommence après une fin de partie.
    /// </summary>
    /// <remarks>
    /// Les autres scripts lisent <see cref="Current"/> au moment où ils en ont besoin :
    /// les ennemis à leur apparition, les vagues, l'arc, l'aide à la visée, le score et le ciel.
    /// </remarks>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public class DifficultyManager : MonoBehaviour
    {
        [Tooltip("Les difficultés proposées, de la plus facile à la plus dure.")]
        [SerializeField]
        DifficultyDefinition[] m_Difficulties = Array.Empty<DifficultyDefinition>();

        [Tooltip("Position dans la liste de la difficulté choisie au premier lancement (0 = la première).")]
        [SerializeField]
        int m_DefaultIndex = 1;

        // Statique : survit au rechargement de la scène après une fin de partie.
        static DifficultyDefinition s_Selected;
        static DifficultyDefinition s_Neutral;

        public static DifficultyManager Instance { get; private set; }

        /// <summary>
        /// Difficulté en cours. Jamais nulle : sans Difficulty Manager, ce sont les valeurs par défaut (×1).
        /// </summary>
        public static DifficultyDefinition Current
        {
            get
            {
                if (s_Selected != null)
                    return s_Selected;
                if (s_Neutral == null)
                {
                    s_Neutral = ScriptableObject.CreateInstance<DifficultyDefinition>();
                    s_Neutral.hideFlags = HideFlags.HideAndDontSave;
                }

                return s_Neutral;
            }
        }

        /// <summary>On ne change de difficulté qu'avant la première vague.</summary>
        public static bool CanChange => WaveManager.Instance == null || WaveManager.Instance.Phase == WavePhase.Ready;

        /// <summary>La difficulté vient de changer : le ciel, les panneaux et la montre se mettent à jour.</summary>
        public static event Action<DifficultyDefinition> Changed;

        public IReadOnlyList<DifficultyDefinition> Difficulties => m_Difficulties;

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs Difficulty Manager dans la scène.", this);
            Instance = this;

            if (m_Difficulties.Length == 0)
            {
                Debug.LogError("DifficultyManager : la liste « Difficulties » est vide.", this);
                return;
            }

            if (s_Selected == null || Array.IndexOf(m_Difficulties, s_Selected) < 0)
                s_Selected = m_Difficulties[Mathf.Clamp(m_DefaultIndex, 0, m_Difficulties.Length - 1)];
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Raccourci de test au clavier : les touches 1 à 4 choisissent la difficulté (avant la première vague).
        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.digit1Key.wasPressedThisFrame)
                SelectIndex(0);
            else if (keyboard.digit2Key.wasPressedThisFrame)
                SelectIndex(1);
            else if (keyboard.digit3Key.wasPressedThisFrame)
                SelectIndex(2);
            else if (keyboard.digit4Key.wasPressedThisFrame)
                SelectIndex(3);
        }
#endif

        /// <summary>Choisit une difficulté. Renvoie faux si la partie a déjà commencé.</summary>
        public bool TrySelect(DifficultyDefinition difficulty)
        {
            if (difficulty == null)
                return false;
            if (difficulty == s_Selected)
                return true;
            if (!CanChange)
                return false;

            s_Selected = difficulty;
            Changed?.Invoke(difficulty);
            return true;
        }

        /// <summary>Pour un événement dans l'Inspector (bouton d'interface…).</summary>
        public void Select(DifficultyDefinition difficulty) => TrySelect(difficulty);

        /// <summary>Pour un événement dans l'Inspector : 0 = la première difficulté de la liste.</summary>
        public void SelectIndex(int index)
        {
            if (index >= 0 && index < m_Difficulties.Length)
                TrySelect(m_Difficulties[index]);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Selected = null;
            Changed = null;
        }
    }
}
