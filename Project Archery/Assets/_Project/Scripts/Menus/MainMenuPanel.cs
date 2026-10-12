using System;
using Archery.Core;
using Archery.Difficulty;
using Archery.Waves;
using TMPro;
using UnityEngine;

namespace Archery.Menus
{
    /// <summary>
    /// Menu principal (GDD, section 14), en haut de la tour avant la première vague : jouer et choisir la difficulté,
    /// paramètres, classement, quitter. Il disparaît dès que la partie commence.
    /// </summary>
    /// <remarks>
    /// Chaque page est un objet enfant (accueil, paramètres, classement…). Les boutons appellent les méthodes
    /// publiques dans leur événement On Click : <see cref="ShowPage"/>, <see cref="Play"/>, <see cref="Quit"/>…
    /// </remarks>
    [DisallowMultipleComponent]
    public class MainMenuPanel : MonoBehaviour
    {
        [Tooltip("Tout le menu : affiché seulement avant la première vague.")]
        [SerializeField]
        GameObject m_Content;

        [Tooltip("Les pages du menu ; la première (0) est l'accueil.")]
        [SerializeField]
        GameObject[] m_Pages = Array.Empty<GameObject>();

        [Tooltip("Optionnel : nom de la difficulté choisie, dans sa couleur.")]
        [SerializeField]
        TMP_Text m_DifficultyText;

        [Tooltip("Optionnel : son des boutons.")]
        [SerializeField]
        AudioClip m_ClickClip;

        void Start()
        {
            SetPage(0);
            RefreshDifficulty(DifficultyManager.Current);
        }

        void OnEnable() => DifficultyManager.Changed += RefreshDifficulty;

        void OnDisable() => DifficultyManager.Changed -= RefreshDifficulty;

        void Update()
        {
            var waves = WaveManager.Instance;
            var visible = waves == null || waves.Phase == WavePhase.Ready;
            if (m_Content != null && m_Content.activeSelf != visible)
                m_Content.SetActive(visible);
        }

        /// <summary>Affiche une page (0 = accueil) et cache les autres.</summary>
        public void ShowPage(int index)
        {
            SetPage(index);
            Click();
        }

        void SetPage(int index)
        {
            for (var i = 0; i < m_Pages.Length; i++)
            {
                if (m_Pages[i] != null)
                    m_Pages[i].SetActive(i == index);
            }
        }

        /// <summary>Lance la première vague, comme le gong.</summary>
        public void Play()
        {
            Click();
            var waves = WaveManager.Instance;
            if (waves != null)
                waves.StartNextWave();
        }

        public void NextDifficulty() => ChangeDifficulty(1);

        public void PreviousDifficulty() => ChangeDifficulty(-1);

        public void Quit()
        {
            Click();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void ChangeDifficulty(int step)
        {
            var manager = DifficultyManager.Instance;
            if (manager == null || manager.Difficulties.Count == 0)
                return;

            var count = manager.Difficulties.Count;
            var index = 0;
            for (var i = 0; i < count; i++)
            {
                if (manager.Difficulties[i] == DifficultyManager.Current)
                    index = i;
            }

            manager.SelectIndex(((index + step) % count + count) % count);
            Click();
        }

        void RefreshDifficulty(DifficultyDefinition difficulty)
        {
            if (m_DifficultyText == null || difficulty == null)
                return;

            m_DifficultyText.text = difficulty.displayName;
            m_DifficultyText.color = difficulty.color;
        }

        void Click() => Sfx.Play(m_ClickClip, transform.position, 0.6f);
    }
}
