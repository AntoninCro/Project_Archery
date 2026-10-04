using System.Globalization;
using Archery.Defense;
using Archery.Difficulty;
using Archery.Economy;
using Archery.Enemies;
using Archery.Player;
using Archery.Waves;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Archery.UI
{
    /// <summary>
    /// Affiche l'état de la partie dans des textes TextMeshPro : score, chrono, vague, PV, argent, combo, difficulté.
    /// Tous les champs sont optionnels, on ne remplit que ceux qui sont branchés : le même script
    /// sert pour la montre au poignet et pour un grand panneau dans le décor.
    /// </summary>
    public class HudDisplay : MonoBehaviour
    {
        [SerializeField]
        TMP_Text m_ScoreText;

        [SerializeField]
        TMP_Text m_TimerText;

        [SerializeField]
        TMP_Text m_WaveText;

        [SerializeField]
        TMP_Text m_PlayerHealthText;

        [SerializeField]
        TMP_Text m_TowerHealthText;

        [SerializeField]
        TMP_Text m_MoneyText;

        [SerializeField]
        TMP_Text m_ComboText;

        [Tooltip("Nom de la difficulté, dans sa couleur.")]
        [SerializeField]
        TMP_Text m_DifficultyText;

        [Tooltip("Optionnel : Image de type « Filled » pour la barre de PV du joueur.")]
        [SerializeField]
        Image m_PlayerHealthBar;

        [Tooltip("Optionnel : Image de type « Filled » pour la barre de PV de la tour.")]
        [SerializeField]
        Image m_TowerHealthBar;

        [Tooltip("Optionnel : nom et PV du boss en vie (vide sans boss).")]
        [SerializeField]
        TMP_Text m_BossText;

        [Tooltip("Optionnel : Image de type « Filled » pour les PV du boss (cachée sans boss).")]
        [SerializeField]
        Image m_BossHealthBar;

        [Tooltip("Couleur du chrono pendant les 5 dernières secondes.")]
        [SerializeField]
        Color m_TimerWarningColor = new Color(1f, 0.35f, 0.25f);

        // Dernières valeurs affichées : on ne reconstruit un texte que s'il change.
        int m_Score = -1;
        int m_Money = -1;
        int m_Combo = -1;
        int m_PlayerHealth = -1;
        int m_TowerHealth = -1;
        int m_Seconds = -1;
        int m_Wave = -1;
        int m_BossHealth = -2;
        bool m_BossWave;
        WavePhase? m_Phase;
        DifficultyDefinition m_Difficulty;
        Color m_TimerColor;

        void Awake()
        {
            if (m_TimerText != null)
                m_TimerColor = m_TimerText.color;
        }

        void Update()
        {
            UpdateScore();
            UpdateWave();
            UpdateBoss();
            UpdatePlayerHealth();
            UpdateTowerHealth();
            UpdateDifficulty();
        }

        void UpdateScore()
        {
            var score = ScoreManager.Instance;
            if (score == null)
                return;

            if (m_ScoreText != null && score.Score != m_Score)
            {
                m_Score = score.Score;
                m_ScoreText.text = "Score " + FormatNumber(m_Score);
            }

            if (m_MoneyText != null && score.Money != m_Money)
            {
                m_Money = score.Money;
                m_MoneyText.text = "Or " + FormatNumber(m_Money);
            }

            if (m_ComboText != null && score.Combo != m_Combo)
            {
                m_Combo = score.Combo;
                m_ComboText.text = m_Combo >= 2 ? "Combo ×" + score.ComboMultiplier.ToString("0.0", CultureInfo.GetCultureInfo("fr-FR")) : "";
            }
        }

        void UpdateWave()
        {
            var waves = WaveManager.Instance;
            if (waves == null)
                return;

            var phase = waves.Phase;
            var seconds = Mathf.CeilToInt(waves.TimeRemaining);
            var bossWave = waves.IsBossWave;
            if (phase == m_Phase && seconds == m_Seconds && waves.WaveNumber == m_Wave && bossWave == m_BossWave)
                return;

            m_Phase = phase;
            m_Seconds = seconds;
            m_Wave = waves.WaveNumber;
            m_BossWave = bossWave;

            if (m_TimerText != null)
            {
                m_TimerText.text = phase switch
                {
                    WavePhase.Wave when waves.IsWaitingForBoss => "Boss",
                    WavePhase.Wave => $"{seconds / 60}:{seconds % 60:00}",
                    WavePhase.GameOver => "Fin",
                    _ => "Pause",
                };
                var warning = phase == WavePhase.Wave && ((seconds <= 5 && !bossWave) || waves.IsWaitingForBoss);
                m_TimerText.color = warning ? m_TimerWarningColor : m_TimerColor;
            }

            if (m_WaveText != null)
            {
                var wave = Mathf.Max(1, phase == WavePhase.Ready ? 1 : waves.WaveNumber);
                m_WaveText.text = wave > waves.WavesToWin ? $"Vague {wave} (infini)" : $"Vague {wave} / {waves.WavesToWin}";
                if (bossWave)
                    m_WaveText.text += " · Boss";
            }
        }

        // PV du boss en vie (texte et barre optionnels), cachés sans boss.
        void UpdateBoss()
        {
            if (m_BossText == null && m_BossHealthBar == null)
                return;

            var boss = Boss.Current;
            var health = boss != null && boss.Enemy.IsAlive ? Mathf.CeilToInt(boss.Health.Current) : -1;
            if (health == m_BossHealth)
                return;

            m_BossHealth = health;
            if (m_BossText != null)
                m_BossText.text = health >= 0 ? $"{boss.DisplayName} {health}/{Mathf.CeilToInt(boss.Health.Max)}" : "";
            if (m_BossHealthBar != null)
            {
                m_BossHealthBar.enabled = health >= 0;
                if (health >= 0)
                    m_BossHealthBar.fillAmount = boss.Health.Normalized;
            }
        }

        void UpdatePlayerHealth()
        {
            var player = PlayerHealth.Instance;
            if (player == null)
                return;

            var health = player.Health;
            var current = Mathf.CeilToInt(health.Current);
            if (current == m_PlayerHealth)
                return;

            m_PlayerHealth = current;
            if (m_PlayerHealthText != null)
                m_PlayerHealthText.text = $"PV {current}/{Mathf.CeilToInt(health.Max)}";
            if (m_PlayerHealthBar != null)
                m_PlayerHealthBar.fillAmount = health.Normalized;
        }

        void UpdateTowerHealth()
        {
            var tower = Tower.Instance;
            if (tower == null)
                return;

            var health = tower.Health;
            var current = Mathf.CeilToInt(health.Current);
            if (current == m_TowerHealth)
                return;

            m_TowerHealth = current;
            if (m_TowerHealthText != null)
                m_TowerHealthText.text = tower.IsStanding ? $"Tour {current}/{Mathf.CeilToInt(health.Max)}" : "Tour détruite";
            if (m_TowerHealthBar != null)
                m_TowerHealthBar.fillAmount = health.Normalized;
        }

        void UpdateDifficulty()
        {
            var difficulty = DifficultyManager.Current;
            if (m_DifficultyText == null || difficulty == m_Difficulty)
                return;

            m_Difficulty = difficulty;
            m_DifficultyText.text = difficulty.displayName;
            m_DifficultyText.color = difficulty.color;
        }

        // 12345 → « 12 345 », avec une espace ordinaire que toutes les polices savent afficher.
        static string FormatNumber(int value) => value.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', ' ');
    }
}
