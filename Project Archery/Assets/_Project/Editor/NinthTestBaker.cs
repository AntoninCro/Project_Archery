using System.Collections.Generic;
using Archery.Building;
using Archery.Menus;
using Archery.Shop;
using Archery.Upgrades;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le 12 novembre, le joueur, après sa partie (victoire à la vague 7 ; <c>Docs/Refonte_TowerDefense.md</c>, 12.74). Chaque réglage
    /// n'est changé que s'il a encore sa valeur d'avant : une seule fois, et jamais un réglage retouché à la main.
    /// <list type="bullet">
    /// <item>« nerf quand même les dégâts des tourelles de 25 % » : la puissance des bâtiments (<i>Structure Power</i>) de chaque
    /// difficulté ×0,75, duos, trios et ultimes compris ;</item>
    /// <item>« la force devrait peut-être être nerf (vers la prochaine valeur ronde) » : le parchemin de Force, +15 % → +10 % de dégâts ;
    /// puis « vitesse de l'anneau à 10 % aussi » : l'anneau 7,5 % → 10 % plus rapide (PlayerUpgrades.StrengthRingShare) ; le texte de
    /// sa carte suit ;</item>
    /// <item>les raretés (« prends 70 / 20 / 7 / 3 pour tout et augmente progressivement au cours de la partie, qu'en moyenne on arrive
    /// au même nombre qu'actuellement ») : les chances du catalogue, 60 / 24 / 11 / 5 toute la partie → de 70 / 20 / 7 / 3 à la vague 1
    /// à 50 / 28 / 15 / 7 à la vague 5 (la même moyenne sur les 5 vagues à coffres) ; les plans des tourelles (BuildManager de la scène
    /// ouverte, puis Ctrl+S) suivent la même montée ;</item>
    /// <item>la Grande Horloge : « nerf par 3 ses dégâts à la fin du dégel, c'est une tourelle de contrôle avant tout » (sur la carte,
    /// et sa part des PV des boss et de MK) ;</item>
    /// <item>le tutoriel « Ouvrir les coffres » : les coffres scellés par leurs gardiens, le chantier du début sans la Barricade.</item>
    /// </list>
    /// Il passe un tour d'éditeur après les autres, comme <c>UltimesTowerBaker</c>.
    /// </summary>
    [InitializeOnLoad]
    static class NinthTestBaker
    {
        const string k_Catalog = "Assets/_Project/Data/Shop/ShopCatalog.asset";
        const string k_Horloge = "Assets/_Project/Prefabs/Structures/Duos/Ultime_Horloge.prefab";
        const string k_Tutorial = "Assets/_Project/Resources/TutorialContent.asset";

        // La puissance des bâtiments de chaque difficulté, avant et après (×0,75, arrondi au centième).
        static readonly (string Path, float Old, float New)[] k_StructurePower =
        {
            ("Assets/_Project/Data/Difficulties/Difficulty_facile.asset", 0.5f, 0.38f),
            ("Assets/_Project/Data/Difficulties/Difficulty_Normal.asset", 0.6f, 0.45f),
            ("Assets/_Project/Data/Difficulties/Difficulty_Difficile.asset", 0.97f, 0.73f),
            ("Assets/_Project/Data/Difficulties/Difficulty_Impossible.asset", 1.43f, 1.07f),
        };

        const float k_ForceOld = 0.15f;
        const float k_ForceNew = 0.1f;
        const string k_ForceDescription = "+10 % ➶, anneau 10 % plus rapide";
        const string k_ForceLevelUp = "+10 % ➶ de plus, anneau 10 % plus rapide";

        // Les chances des raretés (commune, rare, épique, légendaire) : avant, une ligne pour toute la partie ; après, une par vague,
        // de la vague 1 à la vague 5 (la dernière vaut pour les suivantes). La moyenne des cinq est l'ancienne ligne.
        static readonly float[] k_OddsOld = { 60f, 24f, 11f, 5f };
        static readonly float[][] k_OddsNew =
        {
            new[] { 70f, 20f, 7f, 3f },
            new[] { 65f, 22f, 9f, 4f },
            new[] { 60f, 24f, 11f, 5f },
            new[] { 55f, 26f, 13f, 6f },
            new[] { 50f, 28f, 15f, 7f },
        };

        // Les chances de 1, 2, 3 et 4 plans d'une orbe de tourelle, par vague, du 4 au 12 novembre (la scène) ; après : k_OddsNew.
        static readonly float[][] k_PlansOld =
        {
            new[] { 40f, 45f, 10f, 5f },
            new[] { 30f, 45f, 20f, 5f },
            new[] { 25f, 40f, 25f, 10f },
            new[] { 20f, 35f, 30f, 15f },
            new[] { 15f, 30f, 35f, 20f },
        };

        static readonly string[] k_OddsFields = { "common", "rare", "epic", "legendary" };
        static readonly string[] k_PlansFields = { "one", "two", "three", "four" };

        // Le dégel de l'Horloge (÷3) : ses dégâts sur la carte (après −30 % le 4 novembre), sa part des PV d'un boss, celle de MK.
        static readonly (string Field, float Old, float New)[] k_Thaw =
        {
            ("m_ShatterDamage", 350f, 117f),
            ("m_ShatterBossShare", 0.02f, 0.0067f),
            ("m_ArenaShatterShare", 0.03f, 0.01f),
        };

        // Le texte du tutoriel du 3 novembre (StartingTrioBaker), remplacé seulement s'il est encore là mot pour mot.
        const string k_OldChestText =
            "Au début de chaque vague, des coffres apparaissent d'un côté de la jungle, sous un rayon doré (l'autre côté à la vague " +
            "suivante). Vise-en un et appuie sur la gâchette (ou la gâchette de prise) : choisis un parchemin, deux plans d'un bâtiment " +
            "de ton chantier, ou un objet. Ton chantier : 4 bâtiments au début (de quoi bâtir un trio), un de plus après chaque boss.";

        static NinthTestBaker()
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

            var log = new List<string>();
            UpdateStructurePower(log);
            UpdateForce(log);
            UpdateOdds(log);
            UpdateThaw(log);
            UpdateTutorial(log);
            var sceneChanged = UpdatePlanChances(log);
            if (log.Count == 0)
                return;

            AssetDatabase.SaveAssets();
            Debug.Log($"Le neuvième essai (12 novembre) : {log.Count} réglage(s) changé(s) :\n" + string.Join("\n", log) +
                      (sceneChanged ? "\n→ La scène a changé : enregistre-la (Ctrl+S)." : ""));
        }

        // Les chances du catalogue : seulement s'il a encore une seule ligne, à 60 / 24 / 11 / 5.
        static void UpdateOdds(List<string> log)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(k_Catalog);
            if (catalog == null)
                return;

            var serialized = new SerializedObject(catalog);
            var odds = serialized.FindProperty("rarityOdds");
            if (odds == null || odds.arraySize != 1 || !Matches(odds.GetArrayElementAtIndex(0), k_OddsFields, k_OddsOld))
                return;

            odds.arraySize = k_OddsNew.Length;
            for (var i = 0; i < k_OddsNew.Length; i++)
            {
                var row = odds.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("fromWave").intValue = i + 1;
                Write(row, k_OddsFields, k_OddsNew[i]);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            log.Add("Chances des raretés (objets, parchemins) : 60 / 24 / 11 / 5 toute la partie → 70 / 20 / 7 / 3 à la vague 1, " +
                    "65 / 22 / 9 / 4, 60 / 24 / 11 / 5, 55 / 26 / 13 / 6, 50 / 28 / 15 / 7 à la vague 5 (la même moyenne)");
        }

        // Les plans des tourelles (BuildManager de la scène ouverte) : seulement s'ils ont encore les chances du 4 novembre.
        static bool UpdatePlanChances(List<string> log)
        {
            var builds = Object.FindAnyObjectByType<BuildManager>(FindObjectsInactive.Include);
            if (builds == null || EditorUtility.IsPersistent(builds))
                return false;

            var serialized = new SerializedObject(builds);
            var chances = serialized.FindProperty("m_PlanChances");
            if (chances == null || chances.arraySize != k_PlansOld.Length)
                return false;
            for (var i = 0; i < k_PlansOld.Length; i++)
            {
                if (!Matches(chances.GetArrayElementAtIndex(i), k_PlansFields, k_PlansOld[i]))
                    return false;
            }

            for (var i = 0; i < k_OddsNew.Length; i++)
                Write(chances.GetArrayElementAtIndex(i), k_PlansFields, k_OddsNew[i]);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(builds.gameObject.scene);
            log.Add("Plans des tourelles (1, 2, 3, 4 plans) : les mêmes chances que les raretés, de 70 / 20 / 7 / 3 à la vague 1 à " +
                    "50 / 28 / 15 / 7 à la vague 5 (1,6 plan par orbe en moyenne, 2,2 avant)");
            return true;
        }

        static bool Matches(SerializedProperty row, string[] fields, float[] values)
        {
            for (var i = 0; i < fields.Length; i++)
            {
                var property = row.FindPropertyRelative(fields[i]);
                if (property == null || !Mathf.Approximately(property.floatValue, values[i]))
                    return false;
            }

            return true;
        }

        static void Write(SerializedProperty row, string[] fields, float[] values)
        {
            for (var i = 0; i < fields.Length; i++)
                row.FindPropertyRelative(fields[i]).floatValue = values[i];
        }

        static void UpdateStructurePower(List<string> log)
        {
            foreach (var difficulty in k_StructurePower)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(difficulty.Path);
                if (asset == null)
                    continue;

                var serialized = new SerializedObject(asset);
                var name = System.IO.Path.GetFileNameWithoutExtension(difficulty.Path);
                if (!Replace(serialized, "structurePower", difficulty.Old, difficulty.New,
                             $"{name} : puissance des bâtiments {difficulty.Old:0.##} → {difficulty.New:0.##} (−25 %)", log))
                    continue;

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }
        }

        static void UpdateForce(List<string> log)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(k_Catalog);
            if (catalog == null)
                return;

            var serialized = new SerializedObject(catalog);
            var upgrades = serialized.FindProperty("upgrades");
            var changed = false;
            for (var i = 0; upgrades != null && i < upgrades.arraySize; i++)
            {
                var entry = upgrades.GetArrayElementAtIndex(i);
                var effect = entry.FindPropertyRelative("effect");
                var kind = entry.FindPropertyRelative("kind");
                if (effect == null || kind == null || effect.intValue != (int)UpgradeEffect.Strength || kind.intValue != (int)UpgradeKind.Scroll)
                    continue;
                if (!Replace(entry, "value", k_ForceOld, k_ForceNew, "Force : +15 % → +10 % de dégâts, anneau 7,5 % → 10 % plus rapide (la commune)", log))
                    continue;

                entry.FindPropertyRelative("description").stringValue = k_ForceDescription;
                entry.FindPropertyRelative("levelUpText").stringValue = k_ForceLevelUp;
                changed = true;
            }

            if (!changed)
                return;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        static void UpdateThaw(List<string> log)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(k_Horloge) == null)
                return;

            var root = PrefabUtility.LoadPrefabContents(k_Horloge);
            try
            {
                var horloge = root.GetComponent<UltimeHorloge>();
                if (horloge == null)
                    return;

                var serialized = new SerializedObject(horloge);
                var changed = false;
                foreach (var thaw in k_Thaw)
                    changed |= Replace(serialized, thaw.Field, thaw.Old, thaw.New, $"Ultime_Horloge.{thaw.Field} : {thaw.Old:0.####} → {thaw.New:0.####} (÷3)", log);
                if (!changed)
                    return;

                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, k_Horloge);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void UpdateTutorial(List<string> log)
        {
            var content = AssetDatabase.LoadAssetAtPath<TutorialContent>(k_Tutorial);
            if (content == null)
                return;

            var changed = false;
            foreach (var section in content.sections)
            {
                if (section == null)
                    continue;
                foreach (var window in section.windows)
                {
                    if (window == null || window.text != k_OldChestText)
                        continue;
                    window.text = TutorialContent.ChestText;
                    changed = true;
                }
            }

            if (!changed)
                return;
            EditorUtility.SetDirty(content);
            log.Add($"Tutoriel, « {TutorialContent.ChestTitle} » : les coffres scellés, le chantier du début (3 bâtiments)");
        }

        // Une valeur, si elle a encore l'ancienne.
        static bool Replace(SerializedObject serialized, string field, float old, float value, string label, List<string> log)
        {
            var property = serialized.FindProperty(field);
            if (property == null || !Mathf.Approximately(property.floatValue, old))
                return false;

            property.floatValue = value;
            log.Add(label);
            return true;
        }

        static bool Replace(SerializedProperty entry, string field, float old, float value, string label, List<string> log)
        {
            var property = entry.FindPropertyRelative(field);
            if (property == null || !Mathf.Approximately(property.floatValue, old))
                return false;

            property.floatValue = value;
            log.Add(label);
            return true;
        }
    }
}
