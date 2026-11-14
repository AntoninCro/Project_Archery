using Archery.Locomotion;
using Archery.Shop;
using Archery.Upgrades;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le dixième essai, 12 novembre au soir (<c>Docs/Refonte_TowerDefense.md</c>, 12.76). Chaque réglage n'est changé que s'il a
    /// encore sa valeur d'avant : une seule fois, et jamais un réglage retouché à la main.
    /// <list type="bullet">
    /// <item>« le saut est un peu trop sensible » : la vitesse des mains qu'il faut pour sauter (<i>Jump Hand Speed</i> de l'Arm
    /// Swing Locomotion de la scène ouverte), 1,8 → 2,3 m/s, au milieu de la nouvelle « Sensibilité du saut » des paramètres ;
    /// puis enregistrer la scène (Ctrl+S) ;</item>
    /// <item>la corde fantôme refaite (<c>Upgrades/ArrowPieces.Ghost.cs</c>) : le texte de sa carte, seulement s'il est encore
    /// celui d'avant mot pour mot (une proposition : le joueur peut le réécrire) ;</item>
    /// <item>« la partie m'a paru très fade […] peut-être augmenter légèrement les drops, en particulier en début de partie » (puis
    /// « Oui, applique » à la proposition de Claude) : le début des deux courbes remonte, pas leur fin. Les chances des raretés du
    /// catalogue (objets, parchemins), 70 / 20 / 7 / 3 → 65 / 22 / 9 / 4 à la vague 1, jusqu'à 45 / 30 / 17 / 8 à la vague 5 ; les plans
    /// des tourelles (BuildManager de la scène ouverte, puis Ctrl+S), 70 / 20 / 7 / 3 → 50 / 30 / 14 / 6 à la vague 1 (1,8 plan par
    /// orbe), jusqu'à 30 / 35 / 22 / 13 à la vague 5 (2,2) : 2 plans en moyenne, 1,6 avant.</item>
    /// </list>
    /// </summary>
    [InitializeOnLoad]
    static class TenthTestBaker
    {
        const string k_Catalog = "Assets/_Project/Data/Shop/ShopCatalog.asset";
        const float k_JumpOld = 1.8f;
        const float k_JumpNew = 2.3f;

        const string k_GhostOldDescription = "Juste après un tir, tire la corde à vide : une flèche fantôme part ({120 %|150 %|200 %} ➶)";
        const string k_GhostOldLevelUp = "{120 %|150 %|200 %} ➶";
        const string k_GhostDescription =
            "Après un tir, une flèche fantôme se pose sur la corde : prends-la et tends la corde, elle part comme ton dernier tir " +
            "(+20 % ➶) ; {1 fois|2 fois|sans fin}";
        const string k_GhostLevelUp = "{1 flèche fantôme|2 flèches fantômes|sans fin}";

        // Les chances par vague (commune, rare, épique, légendaire ; ou 1, 2, 3, 4 plans), de la vague 1 à la vague 5 : celles du
        // 12 novembre (NinthTestBaker), et les nouvelles. Les plans des tourelles de la scène peuvent aussi avoir encore celles du
        // 4 novembre (scène pas enregistrée après le neuvième essai) : elles passent tout de suite aux nouvelles.
        static readonly float[][] k_OddsOld =
        {
            new[] { 70f, 20f, 7f, 3f },
            new[] { 65f, 22f, 9f, 4f },
            new[] { 60f, 24f, 11f, 5f },
            new[] { 55f, 26f, 13f, 6f },
            new[] { 50f, 28f, 15f, 7f },
        };

        static readonly float[][] k_PlansNovember4 =
        {
            new[] { 40f, 45f, 10f, 5f },
            new[] { 30f, 45f, 20f, 5f },
            new[] { 25f, 40f, 25f, 10f },
            new[] { 20f, 35f, 30f, 15f },
            new[] { 15f, 30f, 35f, 20f },
        };

        static readonly float[][] k_OddsNew =
        {
            new[] { 65f, 22f, 9f, 4f },
            new[] { 60f, 24f, 11f, 5f },
            new[] { 55f, 26f, 13f, 6f },
            new[] { 50f, 28f, 15f, 7f },
            new[] { 45f, 30f, 17f, 8f },
        };

        static readonly float[][] k_PlansNew =
        {
            new[] { 50f, 30f, 14f, 6f },
            new[] { 45f, 31f, 16f, 8f },
            new[] { 40f, 33f, 18f, 9f },
            new[] { 35f, 34f, 20f, 11f },
            new[] { 30f, 35f, 22f, 13f },
        };

        static readonly string[] k_OddsFields = { "common", "rare", "epic", "legendary" };
        static readonly string[] k_PlansFields = { "one", "two", "three", "four" };

        static TenthTestBaker()
        {
            EditorApplication.delayCall -= Later;
            EditorApplication.delayCall += Later;
        }

        static void Later()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            UpdateGhostCard();
            UpdateOdds();
            UpdateJump();
            UpdatePlanChances();
        }

        // Les chances du catalogue : seulement s'il a encore les cinq lignes du 12 novembre.
        static void UpdateOdds()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(k_Catalog);
            if (catalog == null)
                return;

            var serialized = new SerializedObject(catalog);
            var odds = serialized.FindProperty("rarityOdds");
            if (odds == null || !Matches(odds, k_OddsFields, k_OddsOld))
                return;

            for (var i = 0; i < k_OddsNew.Length; i++)
                Write(odds.GetArrayElementAtIndex(i), k_OddsFields, k_OddsNew[i]);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log("Les drops du début de partie (12 novembre, le soir) : chances des raretés (objets, parchemins) 70 / 20 / 7 / 3 → " +
                      "65 / 22 / 9 / 4 à la vague 1, jusqu'à 45 / 30 / 17 / 8 à la vague 5.", catalog);
        }

        // Les plans des tourelles (BuildManager de la scène ouverte) : seulement s'ils ont encore les chances du 12 (ou du 4) novembre.
        static void UpdatePlanChances()
        {
            var builds = Object.FindAnyObjectByType<Archery.Building.BuildManager>(FindObjectsInactive.Include);
            if (builds == null || EditorUtility.IsPersistent(builds))
                return;

            var serialized = new SerializedObject(builds);
            var chances = serialized.FindProperty("m_PlanChances");
            if (chances == null || !(Matches(chances, k_PlansFields, k_OddsOld) || Matches(chances, k_PlansFields, k_PlansNovember4)))
                return;

            for (var i = 0; i < k_PlansNew.Length; i++)
                Write(chances.GetArrayElementAtIndex(i), k_PlansFields, k_PlansNew[i]);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(builds.gameObject.scene);
            Debug.Log("Les drops du début de partie (12 novembre, le soir) : plans des tourelles 50 / 30 / 14 / 6 à la vague 1 (1,8 plan " +
                      "par orbe), jusqu'à 30 / 35 / 22 / 13 à la vague 5 (2,2) ; 2 en moyenne.\n→ La scène a changé : enregistre-la (Ctrl+S).", builds);
        }

        // Le tableau a exactement ces lignes (autant, et chacune ces valeurs).
        static bool Matches(SerializedProperty array, string[] fields, float[][] rows)
        {
            if (array.arraySize != rows.Length)
                return false;
            for (var i = 0; i < rows.Length; i++)
            {
                var row = array.GetArrayElementAtIndex(i);
                for (var k = 0; k < fields.Length; k++)
                {
                    var property = row.FindPropertyRelative(fields[k]);
                    if (property == null || !Mathf.Approximately(property.floatValue, rows[i][k]))
                        return false;
                }
            }

            return true;
        }

        static void Write(SerializedProperty row, string[] fields, float[] values)
        {
            for (var k = 0; k < fields.Length; k++)
                row.FindPropertyRelative(fields[k]).floatValue = values[k];
        }

        static void UpdateGhostCard()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(k_Catalog);
            if (catalog == null)
                return;
            var ghost = catalog.upgrades.Find(upgrade => upgrade != null && upgrade.effect == UpgradeEffect.Ghost);
            if (ghost == null || ghost.description != k_GhostOldDescription)
                return;

            ghost.description = k_GhostDescription;
            if (ghost.levelUpText == k_GhostOldLevelUp)
                ghost.levelUpText = k_GhostLevelUp;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log($"La corde fantôme refaite (12 novembre) : le texte de sa carte, « {ghost.description} ».", catalog);
        }

        static void UpdateJump()
        {
            var locomotion = Object.FindAnyObjectByType<ArmSwingLocomotion>(FindObjectsInactive.Include);
            if (locomotion == null || EditorUtility.IsPersistent(locomotion))
                return;

            var serialized = new SerializedObject(locomotion);
            var property = serialized.FindProperty("m_JumpHandSpeed");
            if (property == null || !Mathf.Approximately(property.floatValue, k_JumpOld))
                return;

            property.floatValue = k_JumpNew;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(locomotion.gameObject.scene);
            Debug.Log($"Le saut aux bras (12 novembre) : vitesse des mains {k_JumpOld} → {k_JumpNew} m/s (moins sensible ; la " +
                      "« Sensibilité du saut » des paramètres règle autour).\n→ La scène a changé : enregistre-la (Ctrl+S).", locomotion);
        }
    }
}
