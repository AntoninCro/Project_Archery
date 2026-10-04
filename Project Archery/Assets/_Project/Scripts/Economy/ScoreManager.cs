using System;
using System.Collections.Generic;
using Archery.Bows;
using Archery.Combat;
using Archery.Core;
using Archery.Defense;
using Archery.Difficulty;
using Archery.Enemies;
using Archery.UI;
using Archery.Waves;
using UnityEngine;

namespace Archery.Economy
{
    /// <summary>
    /// Score, combo et argent de la partie (GDD, section 8).
    /// Points = valeur de base × qualité du tir × distance × combo × difficulté.
    /// </summary>
    [DisallowMultipleComponent]
    public class ScoreManager : MonoBehaviour
    {
        [SerializeField]
        ShotTuning m_ShotTuning;

        [Header("Points par touche")]
        [SerializeField]
        int m_BodyHitPoints = 5;

        [SerializeField]
        int m_HeadHitPoints = 10;

        [SerializeField]
        int m_WeakPointHitPoints = 15;

        [Tooltip("Les points d'élimination sont multipliés par cette valeur quand l'ennemi meurt d'un tir à la tête.")]
        [SerializeField]
        float m_HeadshotKillMultiplier = 2f;

        [Header("Bonus de distance")]
        [Tooltip("Distance (m) à partir de laquelle le bonus commence.")]
        [SerializeField]
        float m_DistanceBonusFrom = 10f;

        [SerializeField]
        float m_DistanceBonusPerMeter = 0.01f;

        [SerializeField]
        float m_MaxDistanceBonus = 0.5f;

        [Header("Combo")]
        [Tooltip("Bonus ajouté à chaque touche consécutive (0,1 = +10 %).")]
        [SerializeField]
        float m_ComboStep = 0.1f;

        [SerializeField]
        float m_MaxComboMultiplier = 2f;

        [Header("Argent")]
        [Tooltip("Part des points gagnés qui devient de l'argent, à la vague 1.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_MoneyRate = 0.25f;

        [Tooltip("En fin de partie, l'argent gagné baisse à chaque vague, en se cumulant " +
                 "(0,91 = −9 % par vague : 25 % des points jusqu'à la vague 4, 14 % à la vague 10, 5,5 % à la vague 20).")]
        [Range(0.5f, 1f)]
        [SerializeField]
        float m_MoneyDecayPerWave = 0.91f;

        [Tooltip("Première vague où l'argent gagné baisse.")]
        [Min(1)]
        [SerializeField]
        int m_MoneyDecayFromWave = 5;

        [Tooltip("Multiplie l'argent gagné tant que la tour est détruite.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_TowerDestroyedMoneyFactor = 0.5f;

        [Header("Retours")]
        [SerializeField]
        Color m_KillPopupColor = new Color(1f, 0.85f, 0.25f);

        [SerializeField]
        AudioClip m_KillClip;

        readonly HashSet<Arrow> m_ArrowsThatHitEnemies = new HashSet<Arrow>();
        float m_Money;

        public static ScoreManager Instance { get; private set; }

        public int Score { get; private set; }
        public int Money => Mathf.FloorToInt(m_Money);

        /// <summary>Nombre de touches consécutives sur des ennemis.</summary>
        public int Combo { get; private set; }

        public float ComboMultiplier => Mathf.Min(m_MaxComboMultiplier, 1f + m_ComboStep * Mathf.Max(0, Combo - 1));

        /// <summary>Multiplicateur de score de la difficulté (GDD, section 11).</summary>
        public float DifficultyMultiplier => DifficultyManager.Current.scoreMultiplier;

        /// <summary>Part des points changée en or, ajoutée au taux de base (amélioration Butin : 0,05 = +5 points).</summary>
        public float MoneyRateBonus { get; set; }

        /// <summary>Multiplie l'or gagné (bonus temporaires, coffres…).</summary>
        public float MoneyMultiplier { get; set; } = 1f;

        /// <summary>Part des points changée en or en ce moment : baisse des vagues et tour détruite comprises.</summary>
        public float MoneyRate
        {
            get
            {
                var tower = Tower.Instance;
                var towerFactor = tower != null && !tower.IsStanding ? m_TowerDestroyedMoneyFactor : 1f;
                var waves = WaveManager.Instance;
                var wave = waves != null ? waves.WaveNumber : 1;
                var waveFactor = Mathf.Pow(m_MoneyDecayPerWave, Mathf.Max(0, wave - m_MoneyDecayFromWave + 1));
                return (m_MoneyRate + MoneyRateBonus) * waveFactor * MoneyMultiplier * towerFactor;
            }
        }

        // Statistiques pour le résumé de fin de partie.
        public int Kills { get; private set; }
        public int Headshots { get; private set; }
        public int ShotsFired { get; private set; }
        public int PerfectShots { get; private set; }

        /// <summary>Le score, l'argent ou le combo ont changé.</summary>
        public event Action Changed;

        ShotTuning Tuning => m_ShotTuning != null ? m_ShotTuning : ShotTuning.Fallback;

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs Score Manager dans la scène.", this);
            Instance = this;
        }

        void OnEnable()
        {
            Enemy.Damaged += OnEnemyDamaged;
            Enemy.Killed += OnEnemyKilled;
            Arrow.ShotEnded += OnShotEnded;
            Bow.ShotFired += OnShotFired;
        }

        void OnDisable()
        {
            Enemy.Damaged -= OnEnemyDamaged;
            Enemy.Killed -= OnEnemyKilled;
            Arrow.ShotEnded -= OnShotEnded;
            Bow.ShotFired -= OnShotFired;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Ajoute des points (et la part d'argent correspondante). Renvoie les points arrondis.</summary>
        public int AddPoints(float points)
        {
            var gained = Mathf.Max(0, Mathf.RoundToInt(points));
            if (gained == 0)
                return 0;

            Score += gained;
            m_Money += gained * MoneyRate;
            Changed?.Invoke();
            return gained;
        }

        /// <summary>Ajoute de l'argent sans points (tests, coffres).</summary>
        public void AddMoney(int amount)
        {
            if (amount <= 0)
                return;

            m_Money += amount;
            Changed?.Invoke();
        }

        /// <summary>Dépense de l'argent (boutique). Renvoie faux s'il n'y en a pas assez.</summary>
        public bool TrySpend(int amount)
        {
            if (amount < 0 || Money < amount)
                return false;

            m_Money -= amount;
            Changed?.Invoke();
            return true;
        }

        void OnShotFired(Bow bow, ShotInfo shot)
        {
            ShotsFired++;
            if (shot.Grade == ShotGrade.Perfect)
                PerfectShots++;
        }

        void OnEnemyDamaged(Enemy enemy, DamageInfo info)
        {
            // Les flèches en plus (multitir, écho, déluge) ne rapportent pas de points de touche et ne font pas
            // monter le combo : sinon l'or grimperait avec le nombre de flèches. Leurs éliminations comptent.
            if (!(info.Source is Arrow arrow) || arrow.IsExtra)
                return;

            m_ArrowsThatHitEnemies.Add(arrow);
            Combo++;

            var basePoints = info.Zone switch
            {
                HitZone.Head => m_HeadHitPoints,
                HitZone.WeakPoint => m_WeakPointHitPoints,
                _ => m_BodyHitPoints,
            };
            AddPoints(basePoints * ShotMultiplier(info));
        }

        void OnEnemyKilled(Enemy enemy, DamageInfo info)
        {
            Kills++;
            var headshot = info.Zone == HitZone.Head;
            if (headshot)
                Headshots++;

            var points = enemy.Definition.points * (headshot ? m_HeadshotKillMultiplier : 1f) * ShotMultiplier(info);
            var gained = AddPoints(points);
            if (gained <= 0)
                return;

            var position = enemy.transform.position + Vector3.up * 2.3f;
            FloatingText.Spawn(position, "+" + gained, m_KillPopupColor, 1.3f, 1.4f);
            Sfx.Play(m_KillClip, position, 0.8f);
        }

        // Une flèche qui finit son vol sans avoir touché d'ennemi casse le combo,
        // sauf les flèches en plus du multitir et du déluge.
        void OnShotEnded(Arrow arrow)
        {
            if (m_ArrowsThatHitEnemies.Remove(arrow) || arrow.IsExtra || Combo == 0)
                return;

            Combo = 0;
            Changed?.Invoke();
        }

        float ShotMultiplier(in DamageInfo info) =>
            Tuning.Get(info.Grade).points * DistanceMultiplier(info.Distance) * ComboMultiplier * DifficultyMultiplier;

        float DistanceMultiplier(float distance) =>
            1f + Mathf.Clamp((distance - m_DistanceBonusFrom) * m_DistanceBonusPerMeter, 0f, m_MaxDistanceBonus);
    }
}
