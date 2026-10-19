using System;
using Archery.Core;
using Archery.Difficulty;
using Archery.UI;
using Archery.Waves;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Archery.Menus
{
    /// <summary>
    /// Menu principal (GDD, section 14), en haut de la tour avant la première vague : paramètres, tutoriel, classement,
    /// quitter. On choisit la difficulté en tirant sur un panneau (ou en le cliquant), et on lance la partie avec le gong.
    /// Il disparaît dès que la partie commence.
    /// </summary>
    /// <remarks>
    /// Chaque page est un objet enfant (accueil, paramètres, classement…) ; la page du tutoriel est fabriquée en jeu
    /// (<see cref="TutorialMenu"/>), et ses fenêtres s'ouvrent sur un grand écran à droite du menu. Les boutons appellent les méthodes publiques dans leur événement On Click :
    /// <see cref="ShowPage"/>, <see cref="ShowTutorial"/>, <see cref="Quit"/>… Tout se clique au rayon de la main, ou se
    /// déclenche d'une flèche (<see cref="MenuArrowTarget"/>).
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

        [Header("Grand écran du tutoriel")]
        [Tooltip("Vu du joueur, de combien de degrés le grand écran est à droite du menu (centre à centre).")]
        [SerializeField]
        float m_TutorialScreenSide = 45f;

        [Tooltip("Distance (m) du grand écran à la tête du joueur (le menu est à environ 2,1 m). Son bas est au niveau du " +
                 "bas du menu.")]
        [SerializeField]
        float m_TutorialScreenReach = 2.5f;

        [Tooltip("Largeur (m) du grand écran.")]
        [SerializeField]
        float m_TutorialScreenSize = 2.4f;

        int m_TutorialPage = -1;
        GameObject m_Backing;

        void Start()
        {
            MakeOpaque();
            BuildTutorial();
            SetPage(0);
            RefreshDifficulty(DifficultyManager.Current);

            // Tout le menu se clique au rayon de la main, ou se déclenche d'une flèche.
            MenuArrowTarget.AttachAll(m_Content);
        }

        // Le menu opaque (paramètres, classement… : on ne voit plus le décor à travers). Le thème peint ses panneaux à 94 %,
        // et le motif du fond laisse un fin liseré transparent près du bord : le panneau passe à 100 %, sur un fond plein.
        void MakeOpaque()
        {
            if (m_Content == null || !m_Content.TryGetComponent<Image>(out var panel))
                return;

            var color = panel.color;
            color.a = 1f;
            panel.color = color;

            var content = (RectTransform)m_Content.transform;
            if (content.parent == null)
                return;

            m_Backing = new GameObject("Fond opaque", typeof(RectTransform));
            m_Backing.layer = m_Content.layer;
            var rect = (RectTransform)m_Backing.transform;
            rect.SetParent(content.parent, false);
            rect.SetSiblingIndex(content.GetSiblingIndex());
            rect.anchorMin = content.anchorMin;
            rect.anchorMax = content.anchorMax;
            rect.pivot = content.pivot;
            rect.anchoredPosition = content.anchoredPosition;
            rect.sizeDelta = content.sizeDelta - new Vector2(20f, 20f);
            var image = m_Backing.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            m_Backing.SetActive(m_Content.activeSelf);
        }

        // La page du tutoriel, ajoutée aux autres pages.
        void BuildTutorial()
        {
            if (m_Content == null || m_TutorialPage >= 0)
                return;

            var tutorial = TutorialMenu.Create(m_Content.transform, this, m_TutorialScreenSide, m_TutorialScreenReach,
                                               m_TutorialScreenSize);
            var pages = new GameObject[m_Pages.Length + 1];
            m_Pages.CopyTo(pages, 0);
            pages[m_Pages.Length] = tutorial.gameObject;
            m_Pages = pages;
            m_TutorialPage = m_Pages.Length - 1;
        }

        /// <summary>Ouvre le tutoriel (bouton « Tutoriel » de l'accueil).</summary>
        public void ShowTutorial()
        {
            if (m_TutorialPage >= 0)
                ShowPage(m_TutorialPage);
        }

        void OnEnable() => DifficultyManager.Changed += RefreshDifficulty;

        void OnDisable() => DifficultyManager.Changed -= RefreshDifficulty;

        void Update()
        {
            var waves = WaveManager.Instance;
            var visible = waves == null || waves.Phase == WavePhase.Ready;
            if (m_Content != null && m_Content.activeSelf != visible)
            {
                m_Content.SetActive(visible);
                if (m_Backing != null)
                    m_Backing.SetActive(visible);
            }
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
