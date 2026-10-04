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
    /// Réglages des vagues (GDD, sections 3 et 10) : durée, budget d'ennemis, bonus de fin de vague.
    /// </summary>
    [CreateAssetMenu(fileName = "WaveSettings", menuName = "Archery/Wave Settings")]
    public class WaveSettings : ScriptableObject
    {
        [Header("Partie")]
        [Tooltip("Nombre de vagues à survivre pour gagner ; la partie continue ensuite en mode infini.")]
        public int wavesToWin = 10;

        [Header("Durée des vagues")]
        [Tooltip("Durée (s) de la vague 1.")]
        public float firstWaveDuration = 30f;

        [Tooltip("Secondes ajoutées à chaque nouvelle vague.")]
        public float durationIncrease = 5f;

        [Header("Ennemis")]
        [Tooltip("Budget de la vague 1 : chaque ennemi coûte son « Cost ».")]
        public float firstWaveBudget = 6f;

        [Tooltip("Budget ajouté à chaque nouvelle vague.")]
        public float budgetIncrease = 3f;

        [Tooltip("Part du chrono (de 0 à 1) pendant laquelle les ennemis apparaissent. Sur la fin, plus d'arrivées.")]
        [Range(0.2f, 1f)]
        public float spawnWindow = 0.8f;

        [Tooltip("Nombre maximal d'ennemis vivants en même temps, à la vague 1. Il augmente avec la difficulté et les vagues.")]
        public int maxAlive = 12;

        [Tooltip("Plafond absolu d'ennemis vivants en même temps, pour que le jeu reste fluide dans le casque.")]
        public int maxAliveCap = 40;

        public List<WaveEnemy> enemies = new List<WaveEnemy>();

        [Header("Mode infini")]
        [Tooltip("Après la dernière vague, la courbe s'accélère : chaque vague multiplie encore les PV des ennemis par cette valeur.")]
        [Min(1f)]
        public float endlessHealthGrowth = 1.12f;

        [Tooltip("En mode infini, chaque vague multiplie encore les dégâts des ennemis par cette valeur.")]
        [Min(1f)]
        public float endlessDamageGrowth = 1.06f;

        [Tooltip("En mode infini, chaque vague multiplie encore le nombre d'ennemis par cette valeur.")]
        [Min(1f)]
        public float endlessCountGrowth = 1.08f;

        [Header("Boss")]
        [Tooltip("Boss des vagues de boss. Vide : pas de boss.")]
        public Enemy bossPrefab;

        [Tooltip("Une vague de boss toutes les N vagues (5 : vagues 5, 10, 15…).")]
        [Min(1)]
        public int bossEvery = 5;

        [Tooltip("Délai (s) entre le début de la vague de boss et l'arrivée du boss.")]
        public float bossSpawnDelay = 4f;

        [Tooltip("Multiplie le budget d'ennemis ordinaires d'une vague de boss.")]
        [Range(0f, 1f)]
        public float bossWaveBudget = 0.6f;

        [Header("Score")]
        [Tooltip("Bonus de fin de vague : cette valeur × le numéro de la vague.")]
        public int clearBonusPerWave = 50;

        public float DurationFor(int wave) => firstWaveDuration + durationIncrease * Mathf.Max(0, wave - 1);

        public float BudgetFor(int wave) =>
            (firstWaveBudget + budgetIncrease * Mathf.Max(0, wave - 1)) * (IsBossWave(wave) ? bossWaveBudget : 1f);

        public bool IsBossWave(int wave) => bossPrefab != null && bossEvery > 0 && wave > 0 && wave % bossEvery == 0;

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
