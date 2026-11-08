using System.Collections.Generic;
using Archery.Building;
using Archery.Combat;
using Archery.Enemies;
using Archery.Player;
using Archery.Shop;
using Archery.Upgrades;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Après le troisième essai (3 novembre, le joueur, une partie lue dans le journal de partie ; <c>Docs/Refonte_TowerDefense.md</c>,
    /// 12.60). Chaque réglage n'est changé que s'il a encore sa valeur d'avant : une seule fois, et jamais un réglage retouché à la main.
    /// <list type="bullet">
    /// <item>les volants « presque invincibles » face à un jeu de rebonds et de réactions en chaîne : PV −50 % (Volant 20 → 10, Guêpe
    /// 12 → 6, Gargouille 35 → 18), et ils touchent un peu plus loin en piqué (+0,3 m : leur piqué allait rarement au bout) ;</item>
    /// <item>les boss des vagues : PV −30 %, et ils frappent le joueur à moins de 7 m (Player Aggro Range 0 → 7 : on leur fonçait
    /// dessus pour les tirer à bout portant sans risque) ; MK ne change pas ;</item>
    /// <item>le Grand Manège faisait deux fois les dégâts du joueur : tous ses dégâts −35 % (prefab <c>Ultime_Manege</c>) ;</item>
    /// <item>le joueur : PV ÷3 (200 → 67, « on ne ressent pas assez les coups »), et une grande régénération (après 3 s sans être
    /// touché au lieu de 5, 10 % des PV max par seconde au lieu de 4) : la scène, à enregistrer (Ctrl+S) ;</item>
    /// <item>la Vigueur suit : +8 PV au lieu de +25 (la même part des PV de base, que la tour suit aussi), le nombre de sa carte
    /// avec ;</item>
    /// <item>le rythme (4 novembre, le joueur : « les premières vagues sont bien rythmées, puis on s'ennuie ») : toutes les vagues ont
    /// le chrono de la première (<c>WaveSettings.durationIncrease</c> 12 → 0 s ; la difficulté passe par le nombre et la force des
    /// ennemis) ; les coffres ont leurs gardiens (<c>CrystalSpawner</c>, dans le code).</item>
    /// </list>
    /// Le reste est dans le code : la couronne du duo « 360 no scope headshot » (<c>SpecialArrows</c>, 5 flèches au lieu de 7), la
    /// fin du piqué des volants (<c>FlyingEnemy</c>).
    /// </summary>
    [InitializeOnLoad]
    static class ThirdTestBaker
    {
        const string k_Enemies = "Assets/_Project/Data/Enemies/";
        const string k_Prefabs = "Assets/_Project/Prefabs/";
        const string k_Manege = "Assets/_Project/Prefabs/Structures/Duos/Ultime_Manege.prefab";
        const string k_Catalog = "Assets/_Project/Data/Shop/ShopCatalog.asset";
        const string k_Waves = "Assets/_Project/Data/Waves/WaveSettings.asset";
        const float k_BossAggro = 7f;
        const float k_ManegeScale = 0.65f;

        static readonly (string Asset, float Old, float New)[] k_FlyerHealth =
        {
            ("Enemy_Volant", 20f, 10f),
            ("Enemy_Guepe", 12f, 6f),
            ("Enemy_Gargouille", 35f, 18f),
        };

        static readonly (string Prefab, float Old, float New)[] k_FlyerReach =
        {
            ("Enemy_Volant", 1f, 1.3f),
            ("Enemy_Guepe", 0.8f, 1.1f),
            ("Enemy_Gargouille", 1.2f, 1.5f),
        };

        static readonly (string Asset, float Old, float New)[] k_BossHealth =
        {
            ("Boss_Champignon", 272f, 190f),
            ("Boss_Mimique", 383f, 268f),
            ("Boss_Tortue", 592f, 414f),
            ("Boss_ChefOrc", 787f, 551f),
            ("Boss_Liche", 868f, 608f),
        };

        // Les dégâts du Grand Manège et leur valeur d'avant (×0,65, arrondi).
        static readonly (string Field, float Old, float New)[] k_ManegeDamage =
        {
            ("m_WakeDamage", 100f, 65f),
            ("m_CatchDamage", 60f, 39f),
            ("m_RideDamage", 70f, 46f),
            ("m_EjectDamage", 100f, 65f),
            ("m_EjectSelfDamage", 200f, 130f),
            ("m_HorseDamage", 160f, 104f),
            ("m_ChargeDamage", 90f, 59f),
        };

        static ThirdTestBaker()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            var log = new List<string>();
            UpdateEnemies(log);
            UpdateFlyerReach(log);
            UpdateManege(log);
            UpdateVigor(log);
            UpdateWaves(log);
            var scene = UpdatePlayer(log);
            if (log.Count == 0)
                return;
            Debug.Log($"Troisième essai (3 novembre) : {log.Count} réglage(s) changé(s) :\n" + string.Join("\n", log) +
                      (scene ? "\n→ La scène a changé : enregistre-la (Ctrl+S)." : ""));
        }

        static void UpdateEnemies(List<string> log)
        {
            foreach (var flyer in k_FlyerHealth)
                UpdateEnemy(flyer.Asset, flyer.Old, flyer.New, false, log);
            foreach (var boss in k_BossHealth)
                UpdateEnemy(boss.Asset, boss.Old, boss.New, true, log);
        }

        static void UpdateEnemy(string name, float oldHealth, float newHealth, bool boss, List<string> log)
        {
            var asset = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(k_Enemies + name + ".asset");
            if (asset == null)
                return;

            var serialized = new SerializedObject(asset);
            var changed = Replace(serialized, "maxHealth", oldHealth, newHealth, $"{name}.maxHealth : {oldHealth:0} → {newHealth:0}", log);
            if (boss)
                changed |= Replace(serialized, "playerAggroRange", 0f, k_BossAggro, $"{name}.playerAggroRange : 0 → {k_BossAggro:0} m", log);
            Save(serialized, asset, changed);
        }

        static void UpdateFlyerReach(List<string> log)
        {
            foreach (var flyer in k_FlyerReach)
            {
                var path = k_Prefabs + flyer.Prefab + ".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                    continue;

                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var behaviour = root.GetComponent<FlyingEnemy>();
                    if (behaviour == null)
                        continue;
                    var serialized = new SerializedObject(behaviour);
                    if (!Replace(serialized, "m_HitRadius", flyer.Old, flyer.New, $"{flyer.Prefab}.m_HitRadius : {flyer.Old:0.#} → {flyer.New:0.#} m", log))
                        continue;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        static void UpdateManege(List<string> log)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(k_Manege) == null)
                return;

            var root = PrefabUtility.LoadPrefabContents(k_Manege);
            try
            {
                var manege = root.GetComponent<UltimeManege>();
                if (manege == null)
                    return;
                var serialized = new SerializedObject(manege);
                var changed = false;
                foreach (var damage in k_ManegeDamage)
                    changed |= Replace(serialized, damage.Field, damage.Old, damage.New, $"Ultime_Manege.{damage.Field} : {damage.Old:0} → {damage.New:0}", log);
                if (!changed)
                    return;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, k_Manege);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // La Vigueur : +8 PV au lieu de +25 (la même part des 67 PV que des 200), et le nombre de sa carte avec (le reste du texte, celui
        // du joueur, ne change pas).
        static void UpdateVigor(List<string> log)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(k_Catalog);
            if (catalog == null)
                return;
            var vigor = catalog.upgrades.Find(upgrade => upgrade != null && upgrade.effect == UpgradeEffect.Vigor);
            if (vigor == null || !Mathf.Approximately(vigor.value, 25f))
                return;

            vigor.value = 8f;
            if (vigor.description != null)
                vigor.description = vigor.description.Replace("+25 PV", "+8 PV");
            if (vigor.levelUpText != null)
                vigor.levelUpText = vigor.levelUpText.Replace("+25 PV", "+8 PV");
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            log.Add($"Vigueur : +25 → +8 PV max (« {vigor.description} »)");
        }

        // Le rythme (4 novembre) : le chrono de la vague 1 pour toutes les vagues.
        static void UpdateWaves(List<string> log)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(k_Waves);
            if (asset == null)
                return;

            var serialized = new SerializedObject(asset);
            Save(serialized, asset, Replace(serialized, "durationIncrease", 12f, 0f,
                                            "WaveSettings.durationIncrease : 12 → 0 s (toutes les vagues ont le chrono de la première)", log));
        }

        // Le joueur de la scène ouverte : ses PV et sa régénération. Vrai si la scène a changé.
        static bool UpdatePlayer(List<string> log)
        {
            var player = Object.FindAnyObjectByType<PlayerHealth>(FindObjectsInactive.Include);
            if (player == null || EditorUtility.IsPersistent(player))
                return false;

            var changed = false;
            if (player.TryGetComponent<Health>(out var health))
            {
                var serialized = new SerializedObject(health);
                if (Replace(serialized, "m_MaxHealth", 200f, 67f, "Joueur (Health.m_MaxHealth) : 200 → 67 PV", log))
                {
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }
            }

            var regen = new SerializedObject(player);
            var regenChanged = Replace(regen, "m_RegenDelay", 5f, 3f, "Joueur (PlayerHealth.m_RegenDelay) : 5 → 3 s sans être touché", log);
            regenChanged |= Replace(regen, "m_RegenPercentPerSecond", 4f, 10f, "Joueur (PlayerHealth.m_RegenPercentPerSecond) : 4 → 10 % par seconde", log);
            if (regenChanged)
                regen.ApplyModifiedPropertiesWithoutUndo();

            changed |= regenChanged;
            if (changed)
                EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            return changed;
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

        static void Save(SerializedObject serialized, Object asset, bool changed)
        {
            if (!changed)
                return;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }
    }
}
