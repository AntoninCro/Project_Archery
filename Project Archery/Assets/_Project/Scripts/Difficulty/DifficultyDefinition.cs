using Archery.World;
using UnityEngine;

namespace Archery.Difficulty
{
    /// <summary>
    /// Une difficulté (GDD, section 11) : multiplicateurs appliqués aux ennemis, au tir et au score,
    /// et le ciel qui va avec. Un asset par difficulté.
    /// </summary>
    /// <remarks>
    /// Le menu ⋮ de l'asset (en haut à droite de l'Inspector) propose « Valeurs du GDD : … »
    /// pour remplir toutes les valeurs d'une des quatre difficultés du GDD.
    /// </remarks>
    [CreateAssetMenu(fileName = "Difficulty", menuName = "Archery/Difficulty Definition")]
    public class DifficultyDefinition : ScriptableObject
    {
        public string displayName = "Normal";

        [Tooltip("Couleur de la difficulté : panneau de choix, montre.")]
        public Color color = new Color(1f, 0.65f, 0.3f);

        [Tooltip("Résumé affiché sur le panneau de choix.")]
        [TextArea(2, 4)]
        public string description = "L'expérience prévue\nAide à la visée\nScore ×1";

        [Header("Ennemis")]
        [Tooltip("Multiplie les PV des ennemis.")]
        [Min(0.1f)]
        public float enemyHealth = 1f;

        [Tooltip("Multiplie la vitesse des ennemis.")]
        [Min(0.1f)]
        public float enemySpeed = 1f;

        [Tooltip("Multiplie la taille de la zone de touche de la tête.")]
        [Min(0.1f)]
        public float headSize = 1f;

        [Tooltip("Multiplie les dégâts des ennemis, sur le joueur comme sur la tour.")]
        [Min(0f)]
        public float damageTaken = 1f;

        [Tooltip("Multiplie le nombre d'ennemis de chaque vague (et le nombre maximal en même temps).")]
        [Min(0.1f)]
        public float enemyCount = 1f;

        [Header("Tir")]
        [Tooltip("Multiplie la largeur de la bande verte (tir parfait) de l'anneau de timing.")]
        [Min(0.1f)]
        public float goldBandWidth = 1f;

        [Tooltip("Affiche l'aide à la visée (ligne droite dans l'axe de la flèche).")]
        public bool aimGuide = true;

        [Header("Progression d'une vague à l'autre (exponentielle)")]
        [Tooltip("Multiplie les PV des ennemis à chaque nouvelle vague, en se cumulant (1,08 : ×2 à la vague 10).")]
        [Min(1f)]
        public float healthGrowthPerWave = 1.08f;

        [Tooltip("Multiplie les dégâts des ennemis à chaque nouvelle vague, en se cumulant.")]
        [Min(1f)]
        public float damageGrowthPerWave = 1.04f;

        [Tooltip("Multiplie le nombre d'ennemis à chaque nouvelle vague, en se cumulant.")]
        [Min(1f)]
        public float countGrowthPerWave = 1.05f;

        [Header("Score")]
        [Tooltip("Multiplie tous les points gagnés.")]
        [Min(0f)]
        public float scoreMultiplier = 1f;

        [Header("Ciel")]
        public SkySettings sky = SkySettings.Sunset();

        [ContextMenu("Valeurs du GDD : Facile")]
        void FillEasy()
        {
            BeginFill();
            displayName = "Facile";
            color = new Color(0.45f, 0.85f, 0.4f);
            description = "Ennemis lents et fragiles\nGrosse tête, aide à la visée\nScore ×0,75";
            enemyHealth = 0.7f;
            enemySpeed = 0.8f;
            headSize = 1.3f;
            damageTaken = 0.6f;
            enemyCount = 0.75f;
            goldBandWidth = 1.3f;
            aimGuide = true;
            scoreMultiplier = 0.75f;
            healthGrowthPerWave = 1.06f;
            damageGrowthPerWave = 1.03f;
            countGrowthPerWave = 1.04f;
            sky = SkySettings.Noon();
            EndFill();
        }

        [ContextMenu("Valeurs du GDD : Normal")]
        void FillNormal()
        {
            BeginFill();
            displayName = "Normal";
            color = new Color(1f, 0.65f, 0.3f);
            description = "L'expérience prévue\nAide à la visée\nScore ×1";
            enemyHealth = 1f;
            enemySpeed = 1f;
            headSize = 1f;
            damageTaken = 1f;
            enemyCount = 1f;
            goldBandWidth = 1f;
            aimGuide = true;
            scoreMultiplier = 1f;
            healthGrowthPerWave = 1.08f;
            damageGrowthPerWave = 1.04f;
            countGrowthPerWave = 1.05f;
            sky = SkySettings.Sunset();
            EndFill();
        }

        [ContextMenu("Valeurs du GDD : Difficile")]
        void FillHard()
        {
            BeginFill();
            displayName = "Difficile";
            color = new Color(0.45f, 0.6f, 1f);
            description = "La nuit, ennemis plus résistants\nSans aide à la visée\nScore ×1,5";
            enemyHealth = 1.4f;
            enemySpeed = 1.15f;
            headSize = 0.85f;
            damageTaken = 1.3f;
            enemyCount = 1.25f;
            goldBandWidth = 0.85f;
            aimGuide = false;
            scoreMultiplier = 1.5f;
            healthGrowthPerWave = 1.11f;
            damageGrowthPerWave = 1.06f;
            countGrowthPerWave = 1.07f;
            sky = SkySettings.Night();
            EndFill();
        }

        [ContextMenu("Valeurs du GDD : Impossible")]
        void FillImpossible()
        {
            BeginFill();
            displayName = "Impossible";
            color = new Color(0.95f, 0.22f, 0.2f);
            description = "Lune de sang, ennemis en masse\nVert étroit, sans aide\nScore ×2";
            enemyHealth = 2f;
            enemySpeed = 1.3f;
            headSize = 0.75f;
            damageTaken = 1.8f;
            enemyCount = 1.6f;
            goldBandWidth = 0.7f;
            aimGuide = false;
            scoreMultiplier = 2f;
            healthGrowthPerWave = 1.14f;
            damageGrowthPerWave = 1.08f;
            countGrowthPerWave = 1.09f;
            sky = SkySettings.BloodMoon();
            EndFill();
        }

        // Permet d'annuler (Ctrl+Z) et enregistre l'asset modifié.
        void BeginFill()
        {
#if UNITY_EDITOR
            UnityEditor.Undo.RecordObject(this, "Valeurs du GDD");
#endif
        }

        void EndFill()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}
