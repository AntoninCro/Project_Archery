using System;
using System.Collections.Generic;
using Archery.Enemies;
using UnityEngine;

namespace Archery.Waves
{
    /// <summary>Un type d'ennemi qui peut apparaître dans les vagues.</summary>
    [Serializable]
    public class WaveEnemy
    {
        public Enemy prefab;

        [Tooltip("Coût de cet ennemi dans le budget de la vague.")]
        public float cost = 1f;

        [Tooltip("Première vague où il peut apparaître.")]
        public int firstWave = 1;

        [Tooltip("Chance relative d'être choisi face aux autres types.")]
        public float weight = 1f;
    }

    /// <summary>
    /// Réglages des vagues (GDD, sections 3 et 3 bis) : durée, accalmie, budget d'ennemis, boss de fin de vague,
    /// récompenses de fin de vague.
    /// </summary>
    [CreateAssetMenu(fileName = "WaveSettings", menuName = "Archery/Wave Settings")]
    public class WaveSettings : ScriptableObject
    {
        [Header("Partie")]
        [Tooltip("Nombre de vagues à survivre pour gagner ; la partie peut ensuite continuer en mode infini.")]
        public int wavesToWin = 5;

        [Header("Durée des vagues")]
        [Tooltip("Durée (s) du chrono de la vague 1, avant l'arrivée du boss.")]
        public float firstWaveDuration = 120f;

        [Tooltip("Secondes ajoutées au chrono à chaque nouvelle vague.")]
        public float durationIncrease = 15f;

        [Tooltip("Part du chrono (de 0 à 1) occupée par l'accalmie du début : peu d'ennemis, le temps de récolter et de construire.")]
        [Range(0f, 0.9f)]
        public float calmFraction = 0.4f;

        [Header("Ennemis")]
        [Tooltip("Budget de la vague 1 : chaque ennemi coûte son « Cost ».")]
        public float firstWaveBudget = 20f;

        [Tooltip("Budget ajouté à chaque nouvelle vague.")]
        public float budgetIncrease = 10f;

        [Tooltip("Part des ennemis de la vague (de 0 à 1) qui arrivent pendant l'accalmie. Le reste arrive pendant la montée, de plus en plus vite.")]
        [Range(0f, 0.5f)]
        public float calmBudgetShare = 0.15f;

        [Tooltip("Nombre maximal d'ennemis vivants en même temps, à la vague 1. Il augmente avec la difficulté et les vagues.")]
        public int maxAlive = 12;

        [Tooltip("Plafond absolu d'ennemis vivants en même temps, pour que le jeu reste fluide dans le casque.")]
        public int maxAliveCap = 40;

        [Tooltip("Plafond de la hausse de vitesse due aux vagues (2 = au plus deux fois plus rapides qu'à la vague 1).")]
        [Min(1f)]
        public float maxSpeedScale = 2f;

        public List<WaveEnemy> enemies = new List<WaveEnemy>();

        [Header("Mode infini")]
        [Tooltip("Après la dernière vague, la courbe s'accélère : chaque vague multiplie encore les PV des ennemis par cette valeur.")]
        [Min(1f)]
        public float endlessHealthGrowth = 1.25f;

        [Tooltip("En mode infini, chaque vague multiplie encore les dégâts des ennemis par cette valeur.")]
        [Min(1f)]
        public float endlessDamageGrowth = 1.14f;

        [Tooltip("En mode infini, chaque vague multiplie encore le nombre d'ennemis par cette valeur.")]
        [Min(1f)]
        public float endlessCountGrowth = 1.19f;

        [Tooltip("En mode infini, chaque vague multiplie encore la vitesse des ennemis par cette valeur (jusqu'au plafond « Max Speed Scale »).")]
        [Min(1f)]
        public float endlessSpeedGrowth = 1.07f;

        [Tooltip("En mode infini, un boss de plus toutes les N vagues (2 : deux boss aux vagues 6 et 7, trois aux vagues 8 et 9…).")]
        [Min(1)]
        public int endlessBossesEvery = 2;

        [Header("Boss de fin de vague")]
        [Tooltip("Le boss de chaque vague, dans l'ordre : le premier à la vague 1, le deuxième à la vague 2… " +
                 "Au-delà de la liste (mode infini), ils reviennent tour à tour. Vide : pas de boss, la vague se termine avec le chrono.")]
        public List<Enemy> bosses = new List<Enemy>();

        [Tooltip("Pendant le combat contre le boss, un renfort arrive toutes les N secondes (moins avec la difficulté et les vagues), " +
                 "une fois les ennemis prévus tous sortis.")]
        [Min(0.5f)]
        public float bossReinforcementInterval = 6f;

        [Header("Récompenses de fin de vague")]
        [Tooltip("Or de fin de vague : cette base + « Reward Per Wave » × le numéro de la vague, × le multiplicateur de score de la difficulté.")]
        public int waveRewardBase = 80;

        public int waveRewardPerWave = 110;

        [Tooltip("Chaque bonus de performance (tour intacte, boss tué vite, combo) ajoute cette part de la récompense.")]
        [Range(0f, 1f)]
        public float performanceBonus = 0.2f;

        [Tooltip("Bonus « boss express » : le boss meurt moins de N secondes après son arrivée.")]
        public float fastBossTime = 30f;

        [Tooltip("Bonus « combo » : un combo d'au moins N touches pendant la vague.")]
        public int comboTarget = 10;

        [Header("Score")]
        [Tooltip("Points de fin de vague : cette valeur × le numéro de la vague.")]
        public int clearBonusPerWave = 100;

        public float DurationFor(int wave) => firstWaveDuration + durationIncrease * Mathf.Max(0, wave - 1);

        public float BudgetFor(int wave) => firstWaveBudget + budgetIncrease * Mathf.Max(0, wave - 1);

        /// <summary>Nombre de boss à la fin de la vague : un seul, puis de plus en plus en mode infini.</summary>
        public int BossCountFor(int wave)
        {
            if (!HasBosses || wave <= 0)
                return 0;
            if (wave <= wavesToWin)
                return 1;
            return 1 + (wave - wavesToWin + 1) / Mathf.Max(1, endlessBossesEvery);
        }

        public bool HasBosses => bosses.Exists(boss => boss != null);

        /// <summary>
        /// Boss numéro <paramref name="index"/> (0 pour le premier) de la vague : celui de la vague, puis les suivants
        /// de la liste quand il en vient plusieurs (mode infini). Les cases vides de la liste sont sautées.
        /// </summary>
        public Enemy BossFor(int wave, int index = 0)
        {
            if (!HasBosses)
                return null;

            var count = bosses.Count;
            var slot = Mathf.Max(0, wave - 1) + index;
            for (var tries = 0; tries < count; tries++)
            {
                var boss = bosses[(slot + tries) % count];
                if (boss != null)
                    return boss;
            }

            return null;
        }

        /// <summary>Or de fin de vague, avant le multiplicateur de la difficulté et les bonus.</summary>
        public int WaveRewardFor(int wave) => waveRewardBase + waveRewardPerWave * Mathf.Max(1, wave);

        /// <summary>
        /// Croissance exponentielle à la vague <paramref name="wave"/> : <paramref name="growthPerWave"/> puissance (vague − 1),
        /// puis encore <paramref name="endlessGrowth"/> par vague en mode infini.
        /// </summary>
        public float Growth(int wave, float growthPerWave, float endlessGrowth)
        {
            var factor = Mathf.Pow(Mathf.Max(1f, growthPerWave), Mathf.Max(0, wave - 1));
            var endlessWaves = wave - wavesToWin;
            if (endlessWaves > 0)
                factor *= Mathf.Pow(Mathf.Max(1f, endlessGrowth), endlessWaves);
            return factor;
        }
    }
}
