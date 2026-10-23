using System;
using Archery.Core;
using Archery.Difficulty;
using Archery.Player;
using Archery.UI;
using Archery.Waves;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Archery.Menus
{
    /// <summary>
    /// Menu principal (GDD, section 14), en haut de la tour avant la première vague : paramètres, tutoriel, classement,
    /// quitter. On choisit la difficulté en tirant sur un panneau (ou en le cliquant), et on lance la partie avec le gong.
    /// Il disparaît dès que la partie commence ; ensuite, le bouton menu de la manette gauche (Tab sans casque) l'ouvre
    /// devant le joueur, le jeu en pause, et le referme.
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
        [Tooltip("Place fixe du grand écran, autour de la place du joueur (2,1 m devant le menu, au centre du haut de la " +
                 "tour) : de combien de degrés il est tourné depuis le menu. Négatif : à gauche, à l'opposé du choix des arcs.")]
        [SerializeField]
        float m_TutorialScreenYaw = -45f;

        [Tooltip("Distance (m) du grand écran à la place du joueur (le menu est à 2,1 m). Son bas est au niveau du bas du menu.")]
        [SerializeField]
        float m_TutorialScreenDistance = 2.5f;

        [Tooltip("Largeur (m) du grand écran.")]
        [SerializeField]
        float m_TutorialScreenSize = 2.4f;

        int m_TutorialPage = -1;
        GameObject m_Backing;

        // En cours de partie : le bouton menu de la manette gauche (ou Tab) ouvre le menu devant le joueur, le jeu en pause.
        // Il est à la même distance que devant la place du joueur en haut de la tour (le grand écran du tutoriel se place
        // d'après lui), un peu sous les yeux.
        const float k_InGameDistance = 2.1f;
        const float k_InGameDrop = 0.15f;

        // Ouvert en cours de partie, il suit le joueur sans gêner la visée de ses boutons : il ne bouge pas tant qu'il
        // reste à moins de 30° du regard et à sa distance (à 40 cm près), puis revient en douceur devant lui.
        const float k_FollowAngle = 30f;
        const float k_FollowSlack = 0.4f;
        const float k_FollowSpeed = 5f;
        bool m_Recentering;

        const float k_QuitConfirmTime = 3f;
        float m_QuitConfirmUntil = -1f;
        TMP_Text m_QuitLabel;
        string m_QuitText;

        InputAction m_MenuButton;
        bool m_OpenInGame;
        Vector3 m_HomePosition;
        Quaternion m_HomeRotation;
        GameObject m_ResumeHint;

        void Awake()
        {
            m_MenuButton = new InputAction("Menu en partie", InputActionType.Button);
            m_MenuButton.AddBinding("<XRController>{LeftHand}/{MenuButton}");
            m_MenuButton.AddBinding("<Keyboard>/tab");
        }

        void OnDestroy()
        {
            SlowMotion.End(this);
            m_MenuButton?.Dispose();
        }

        void Start()
        {
            MakeOpaque();
            BuildTutorial();
            SetPage(0);
            RefreshDifficulty(DifficultyManager.Current);

            // Tout le menu se clique au rayon de la main, ou se déclenche d'une flèche.
            MenuArrowTarget.AttachAll(m_Content);
        }

        // Le menu opaque (paramètres, classement… : on ne voit plus le décor à travers). Le thème peint ses panneaux à 94 % :
        // le panneau passe à 100 %, devant un fond plein qui cache aussi ce qui serait dessiné après lui (OpaqueBacking).
        void MakeOpaque()
        {
            if (m_Content == null || !m_Content.TryGetComponent<Image>(out var panel))
                return;

            var color = panel.color;
            color.a = 1f;
            panel.color = color;

            var content = (RectTransform)m_Content.transform;
            if (content.parent != null)
                m_Backing = OpaqueBacking.Create(content, color, 10f);
        }

        // La page du tutoriel, ajoutée aux autres pages.
        void BuildTutorial()
        {
            if (m_Content == null || m_TutorialPage >= 0)
                return;

            var tutorial = TutorialMenu.Create(m_Content.transform, this, m_TutorialScreenYaw, m_TutorialScreenDistance,
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

        void OnEnable()
        {
            DifficultyManager.Changed += RefreshDifficulty;
            m_MenuButton?.Enable();
        }

        void OnDisable()
        {
            DifficultyManager.Changed -= RefreshDifficulty;
            m_MenuButton?.Disable();
            SetOpenInGame(false);
        }

        void Update()
        {
            var waves = WaveManager.Instance;
            var ready = waves == null || waves.Phase == WavePhase.Ready;

            // Après le début de la partie : le bouton menu ouvre ou ferme le menu (pas une fois la partie finie).
            var playing = !ready && waves.Phase != WavePhase.GameOver;
            if (playing && m_MenuButton != null && m_MenuButton.WasPressedThisFrame())
                SetOpenInGame(!m_OpenInGame);
            else if (!playing && m_OpenInGame)
                SetOpenInGame(false);

            var visible = ready || m_OpenInGame;
            if (m_Content != null && m_Content.activeSelf != visible)
            {
                m_Content.SetActive(visible);
                if (m_Backing != null)
                    m_Backing.SetActive(visible);
            }

            if (m_OpenInGame)
                FollowPlayer(Time.unscaledDeltaTime);

            // Pas de deuxième appui sur « Quitter » à temps : le bouton redevient normal.
            if (m_QuitConfirmUntil > 0f && Time.unscaledTime > m_QuitConfirmUntil)
            {
                m_QuitConfirmUntil = -1f;
                SetQuitLabel(false);
            }
        }

        // Le jeu est en pause : le menu se déplace en temps réel.
        void FollowPlayer(float deltaTime)
        {
            if (!TryInFrontOfPlayer(out var target, out var head))
                return;

            var toMenu = Vector3.ProjectOnPlane(transform.position - head, Vector3.up);
            var toTarget = Vector3.ProjectOnPlane(target.position - head, Vector3.up);
            var off = Vector3.Angle(toMenu, toTarget) > k_FollowAngle || Mathf.Abs(toMenu.magnitude - toTarget.magnitude) > k_FollowSlack ||
                      Mathf.Abs(transform.position.y - target.position.y) > k_FollowSlack;
            if (off)
                m_Recentering = true;
            if (!m_Recentering)
                return;

            var blend = 1f - Mathf.Exp(-k_FollowSpeed * deltaTime);
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, target.position, blend),
                                             Quaternion.Slerp(transform.rotation, target.rotation, blend));
            if ((transform.position - target.position).sqrMagnitude < 0.0025f && Quaternion.Angle(transform.rotation, target.rotation) < 2f)
                m_Recentering = false;
        }

        // Ouvert en cours de partie : devant le joueur, à l'accueil, le jeu en pause, avec un rappel pour le refermer.
        // Refermé : il retourne à sa place en haut de la tour, et le temps repart en douceur.
        void SetOpenInGame(bool open)
        {
            if (open == m_OpenInGame)
                return;

            m_OpenInGame = open;
            if (open)
            {
                m_HomePosition = transform.position;
                m_HomeRotation = transform.rotation;
                PlaceInFrontOfPlayer();
                SetPage(0);
                ShowResumeHint(true);
                SlowMotion.Pause(this);
            }
            else
            {
                SetPage(0);
                transform.SetPositionAndRotation(m_HomePosition, m_HomeRotation);
                ShowResumeHint(false);
                SlowMotion.End(this);
            }

            Click();
        }

        void PlaceInFrontOfPlayer()
        {
            m_Recentering = false;
            if (TryInFrontOfPlayer(out var target, out _))
                transform.SetPositionAndRotation(target.position, target.rotation);
        }

        // Devant le joueur, dans la direction de son regard (à l'horizontale), un peu sous ses yeux, tourné vers lui.
        static bool TryInFrontOfPlayer(out Pose pose, out Vector3 headPosition)
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (head == null)
            {
                pose = default;
                headPosition = default;
                return false;
            }

            headPosition = head.position;
            var forward = rig.HeadYaw * Vector3.forward;
            pose = new Pose(headPosition + forward * k_InGameDistance + Vector3.down * k_InGameDrop, Quaternion.LookRotation(forward, Vector3.up));
            return true;
        }

        // « Bouton menu : reprendre la partie », en bas du menu, seulement quand il est ouvert en cours de partie.
        void ShowResumeHint(bool show)
        {
            if (m_ResumeHint == null && show && m_Content != null)
            {
                m_ResumeHint = new GameObject("Rappel : reprendre", typeof(RectTransform));
                var rect = (RectTransform)m_ResumeHint.transform;
                rect.SetParent(m_Content.transform, false);
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 12f);
                rect.sizeDelta = new Vector2(-40f, 44f);
                var text = m_ResumeHint.AddComponent<TextMeshProUGUI>();
                text.text = "Jeu en pause · bouton menu de la manette gauche : reprendre la partie";
                text.fontSize = 26f;
                text.enableAutoSizing = true;
                text.fontSizeMin = 14f;
                text.fontSizeMax = 26f;
                text.alignment = TextAlignmentOptions.Center;
                text.color = new Color(1f, 0.85f, 0.45f);
                text.raycastTarget = false;
            }

            if (m_ResumeHint != null)
                m_ResumeHint.SetActive(show);
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

        // « Quitter » demande une confirmation (23 octobre ; avant, seule une flèche la demandait, un clic de la main fermait
        // le jeu d'un coup) : au premier appui, le bouton affiche « Vraiment ? » et un message dit quoi faire ; un deuxième
        // appui dans les 3 s ferme le jeu.
        public void Quit()
        {
            Click();
            if (Time.unscaledTime > m_QuitConfirmUntil)
            {
                m_QuitConfirmUntil = Time.unscaledTime + k_QuitConfirmTime;
                SetQuitLabel(true);
                var again = MenuArrowTarget.IsShotClick ? "Tire encore une fois" : "Clique encore une fois";
                HeadMessages.Show(again + " sur « Quitter » pour fermer le jeu", new Color(1f, 0.55f, 0.5f), 2.5f);
                return;
            }

            m_QuitConfirmUntil = -1f;
            SetQuitLabel(false);
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

        // Le texte du bouton « Quitter » (son objet a « quit » dans son nom) : « Vraiment ? » le temps de confirmer.
        void SetQuitLabel(bool confirming)
        {
            if (m_QuitLabel == null && m_Content != null)
            {
                foreach (var button in m_Content.GetComponentsInChildren<Button>(true))
                {
                    if (!button.name.ToLowerInvariant().Contains("quit"))
                        continue;
                    m_QuitLabel = button.GetComponentInChildren<TMP_Text>(true);
                    if (m_QuitLabel != null)
                        m_QuitText = m_QuitLabel.text;
                    break;
                }
            }

            if (m_QuitLabel != null)
                m_QuitLabel.text = confirming ? "Vraiment ?" : m_QuitText;
        }
    }
}
