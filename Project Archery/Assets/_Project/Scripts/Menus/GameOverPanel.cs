using System.Collections;
using System.Globalization;
using Archery.Bows;
using Archery.Core;
using Archery.Difficulty;
using Archery.Economy;
using Archery.Player;
using Archery.Save;
using Archery.Waves;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archery.Menus
{
    /// <summary>
    /// Écran de fin de partie (GDD, sections 3 et 14) : il apparaît devant le joueur après sa mort.
    /// Il montre le résumé de la partie, fait saisir le nom (clavier virtuel d'XRI), enregistre le score
    /// dans le classement, puis propose de rejouer.
    /// </summary>
    /// <remarks>
    /// Il remplace le rechargement automatique de la scène par <see cref="PlayerHealth"/>.
    /// Les boutons appellent <see cref="SaveScore"/> et <see cref="Replay"/> ; l'événement « On Text Submitted »
    /// du clavier peut appeler <see cref="SubmitName"/>.
    /// </remarks>
    [DisallowMultipleComponent]
    public class GameOverPanel : MonoBehaviour
    {
        [Tooltip("Tout l'écran : caché pendant la partie.")]
        [SerializeField]
        GameObject m_Content;

        [SerializeField]
        TMP_Text m_TitleText;

        [SerializeField]
        TMP_Text m_SummaryText;

        [Tooltip("Partie « nom » : le champ du nom et le bouton Enregistrer, cachés une fois le score enregistré.")]
        [SerializeField]
        GameObject m_NameSection;

        [Tooltip("Champ du nom. Avec le Spatial Keyboard d'XRI : le prefab « Input Field Global Keyboard ».")]
        [SerializeField]
        TMP_InputField m_NameInput;

        [Tooltip("Optionnel : message après l'enregistrement (« 3e place ! »).")]
        [SerializeField]
        TMP_Text m_MessageText;

        [Tooltip("Optionnel, avec Bow Classes : expérience gagnée et arcs débloqués.")]
        [SerializeField]
        TMP_Text m_XpText;

        [SerializeField]
        LeaderboardView m_Leaderboard;

        [Tooltip("Délai (s) entre la mort et l'apparition de l'écran.")]
        [SerializeField]
        float m_ShowDelay = 3f;

        [Tooltip("Position de l'écran par rapport à la tête du joueur (X à droite, Z devant).")]
        [SerializeField]
        Vector3 m_OffsetFromHead = new Vector3(0f, -0.2f, 1.6f);

        [SerializeField]
        int m_MaxNameLength = 16;

        [Tooltip("Nom utilisé si le champ est vide.")]
        [SerializeField]
        string m_DefaultName = "Archer";

        [SerializeField]
        AudioClip m_SaveClip;

        // Le dernier nom saisi est proposé à la partie suivante.
        static string s_LastName = "";

        WaveManager m_Waves;
        bool m_Shown;
        bool m_Saved;
        bool m_Restarting;

        void Awake()
        {
            if (m_Content != null)
                m_Content.SetActive(false);
        }

        void Start()
        {
            m_Waves = WaveManager.Instance;
            if (m_Waves != null)
                m_Waves.GameOver += OnGameOver;
            else
                Debug.LogWarning("GameOverPanel : aucun Wave Manager dans la scène.", this);

            // C'est cet écran qui relance la partie, plus le rechargement automatique.
            if (PlayerHealth.Instance != null)
                PlayerHealth.Instance.ReloadSceneOnDeath = false;
        }

        void OnDestroy()
        {
            if (m_Waves != null)
                m_Waves.GameOver -= OnGameOver;
        }

        void OnGameOver() => Invoke(nameof(Show), m_ShowDelay);

        void Show()
        {
            if (m_Shown)
                return;

            m_Shown = true;
            PlaceInFrontOfPlayer();

            var victory = m_Waves != null && m_Waves.WaveNumber > m_Waves.WavesToWin;
            if (m_TitleText != null)
                m_TitleText.text = victory ? "Victoire !" : "Fin de partie";
            if (m_SummaryText != null)
                m_SummaryText.text = Summary();
            if (m_XpText != null)
                m_XpText.text = XpSummary();
            if (m_NameInput != null)
            {
                m_NameInput.characterLimit = m_MaxNameLength;
                m_NameInput.text = s_LastName;
            }

            if (m_NameSection != null)
                m_NameSection.SetActive(true);
            if (m_MessageText != null)
                m_MessageText.text = "";
            if (m_Content != null)
                m_Content.SetActive(true);
            if (m_Leaderboard != null)
                m_Leaderboard.Highlight(-1);

            StartCoroutine(FocusName());
        }

        // Le clavier virtuel s'ouvre tout seul : le champ du nom prend le focus, et son XR Keyboard Display affiche le clavier.
        // On attend une image, le temps que le champ, qui vient d'apparaître, s'abonne à son focus.
        IEnumerator FocusName()
        {
            yield return null;
            if (m_NameInput != null && m_NameInput.isActiveAndEnabled && !m_Saved)
                m_NameInput.Select();
        }

        /// <summary>Pour l'événement « On Text Submitted » du clavier : la touche Entrée enregistre le score.</summary>
        public void SubmitName(string playerName)
        {
            // Un texte vide (événement branché avec un argument fixe) ne doit pas effacer le nom tapé.
            if (m_NameInput != null && !string.IsNullOrWhiteSpace(playerName))
                m_NameInput.text = playerName;
            SaveScore();
        }

        /// <summary>Enregistre la partie dans le classement (une seule fois).</summary>
        public void SaveScore()
        {
            if (!m_Shown || m_Saved)
                return;

            m_Saved = true;
            var playerName = CleanName(m_NameInput != null ? m_NameInput.text : "");
            s_LastName = playerName;

            var score = ScoreManager.Instance;
            var rank = Leaderboard.Add(new LeaderboardEntry
            {
                name = playerName,
                score = score != null ? score.Score : 0,
                difficulty = DifficultyManager.Current.displayName,
                wave = m_Waves != null ? m_Waves.WaveNumber : 0,
                kills = score != null ? score.Kills : 0,
                date = LeaderboardEntry.Now(),
            });

            if (m_NameSection != null)
                m_NameSection.SetActive(false);
            if (m_Leaderboard != null)
                m_Leaderboard.Highlight(rank);
            if (m_MessageText != null)
                m_MessageText.text = rank == 0 ? "Nouveau record !" : rank > 0 ? $"{rank + 1}e place" : "";

            Sfx.Play(m_SaveClip, transform.position, 0.9f);
        }

        /// <summary>Recharge la scène : retour au menu, prêt pour une nouvelle partie.</summary>
        public void Replay()
        {
            if (!m_Restarting)
                StartCoroutine(Restart());
        }

        IEnumerator Restart()
        {
            m_Restarting = true;
            ScreenFader.FadeInOnNextLoad = true;
            var fader = ScreenFader.Instance;
            if (fader != null)
                yield return fader.Fade(1f, 0.5f);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        string Summary()
        {
            var score = ScoreManager.Instance;
            var wave = m_Waves != null ? m_Waves.WaveNumber : 0;
            var difficulty = DifficultyManager.Current.displayName;
            if (score == null)
                return $"Vague {wave} · {difficulty}";

            var points = score.Score.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', ' ');
            return $"Score : <b>{points}</b>\n" +
                   $"Vague {wave} · {difficulty}\n" +
                   $"Ennemis tués : {score.Kills} · Tirs à la tête : {score.Headshots} · Tirs parfaits : {score.PerfectShots}";
        }

        // « +120 XP (total 760) », puis « Nouvel arc : Arc long ! » pour chaque arc débloqué.
        static string XpSummary()
        {
            var classes = BowClasses.Instance;
            if (!BowClasses.IsActive)
                return "";

            var gained = classes.AwardGameXp();
            var text = $"+{gained} XP (total {classes.Xp})";
            foreach (var bow in classes.NewlyUnlocked)
                text += $"\nNouvel arc : {bow.displayName} !";
            return text;
        }

        // Sans balises de texte enrichi, sans espaces autour, pas trop long.
        string CleanName(string playerName)
        {
            playerName = (playerName ?? "").Replace("<", "").Replace(">", "").Trim();
            if (playerName.Length > m_MaxNameLength)
                playerName = playerName.Substring(0, m_MaxNameLength);
            return playerName.Length > 0 ? playerName : m_DefaultName;
        }

        void PlaceInFrontOfPlayer()
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (head == null)
                return;

            var yaw = rig.HeadYaw;
            transform.SetPositionAndRotation(head.position + yaw * m_OffsetFromHead, yaw);
        }
    }
}
