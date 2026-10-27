using System.Collections.Generic;
using System.Linq;
using Archery.Combat;
using Archery.Enemies;
using Archery.Shop;
using Archery.Training;
using Archery.Upgrades;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archery.EditorTools
{
    /// <summary>
    /// Les réglages du joueur du 26 octobre, après ses tests au casque (<c>Docs/Refonte_Roguelike.md</c>, partie « Avancement »),
    /// chacun une seule fois, avec sa condition de garde :
    /// <list type="bullet">
    /// <item>le catalogue de la boutique (tant qu'aucune carte n'a l'effet de la super grenade) : les objets à pourcentage ne
    /// tirent plus au sort (la foudre, la chaîne d'éclairs, la glace et le déluge jouent à chaque tir parfait ; l'explosion, le
    /// perçage, le multitir et l'écho à chaque tir) et leurs valeurs changent ; l'empennage spectral, Grenade sauvage et
    /// Grenade rapide sont retirés ; le talisman de la super grenade est ajouté. Les textes des cartes de l'équipe ne sont pas
    /// réécrits ; ceux des pièces nouvelles (provisoires, écrits par Claude) le sont s'ils n'ont pas été changés ;</item>
    /// <item>le mannequin d'entraînement (tant que son prefab n'a pas de <see cref="TrainingDummy"/>) : il devient un ennemi entre
    /// les vagues, avec sa fiche <c>Data/Enemies/Enemy_Mannequin</c> (créée si elle manque) ;</item>
    /// <item>la scène ouverte (tant que le groupe « Training » a moins de 6 mannequins) : un groupe de 3 mannequins serrés et
    /// un mannequin loin, pour le talisman du sniper. La scène est alors à enregistrer (Ctrl+S).</item>
    /// </list>
    /// </summary>
    [InitializeOnLoad]
    static class RoguelikeTuningBaker
    {
        const string k_CatalogPath = "Assets/_Project/Data/Shop/ShopCatalog.asset";
        const string k_DummyPrefabPath = "Assets/_Project/Prefabs/Training Dummy.prefab";
        const string k_DummyDefinitionPath = "Assets/_Project/Data/Enemies/Enemy_Mannequin.asset";

        // Les PV d'un mannequin, comme un orc (110), avant la difficulté et la vague.
        const float k_DummyHealth = 110f;

        const int k_WantedDummies = 6;

        // Les nouveaux mannequins : distance à la tour (m) et décalage sur le côté (m, positif à droite vu de la tour), comme dans
        // TrainingLayoutBaker. Le groupe de 3, serré (1,1 m), puis un mannequin loin.
        static readonly Vector2[] k_NewDummySlots =
        {
            new Vector2(27f, -8f), new Vector2(28f, -7.1f), new Vector2(26.9f, -6.6f), new Vector2(45f, 1f),
        };

        static readonly string[] k_NewDummyNames = { "Mannequin groupe 1", "Mannequin groupe 2", "Mannequin groupe 3", "Mannequin loin" };

        // Les objets dont la valeur ou le déclencheur change, et leurs nouvelles valeurs (par exemplaire).
        struct Tuning
        {
            public UpgradeEffect Effect;
            public float Value;
            public UpgradeTrigger Trigger;
            public string What;
        }

        static readonly Tuning[] k_Tunings =
        {
            new Tuning { Effect = UpgradeEffect.Lightning, Value = 1f, Trigger = UpgradeTrigger.PerfectShot,
                         What = "chaque tir parfait, 50/100/150 % des dégâts" },
            new Tuning { Effect = UpgradeEffect.ChainLightning, Value = 3f, Trigger = UpgradeTrigger.PerfectShot,
                         What = "chaque tir parfait, 25/50/75 % des dégâts par éclair" },
            new Tuning { Effect = UpgradeEffect.Frost, Value = 1f, Trigger = UpgradeTrigger.PerfectShot,
                         What = "chaque tir parfait, 40/50/60 % plus lents sur 3/4/5 m" },
            new Tuning { Effect = UpgradeEffect.Explosive, Value = 1f, Trigger = UpgradeTrigger.Always,
                         What = "chaque tir, 100/150/200 % sur 4/5/6 m" },
            new Tuning { Effect = UpgradeEffect.Piercing, Value = 1f, Trigger = UpgradeTrigger.Always,
                         What = "chaque tir, 2/4/99 ennemis" },
            new Tuning { Effect = UpgradeEffect.HeadHunter, Value = 0.5f, Trigger = UpgradeTrigger.Always,
                         What = "+50/100/150 % à la tête" },
            new Tuning { Effect = UpgradeEffect.Multishot, Value = 1f, Trigger = UpgradeTrigger.Always,
                         What = "2/3/4 flèches à chaque tir" },
            new Tuning { Effect = UpgradeEffect.Echo, Value = 0.5f, Trigger = UpgradeTrigger.Always,
                         What = "50/100/150 % de chance à chaque tir" },
            new Tuning { Effect = UpgradeEffect.Deluge, Value = 1f, Trigger = UpgradeTrigger.PerfectShot,
                         What = "chaque tir parfait, 1/2/3 flèches en plus" },
        };

        static readonly UpgradeEffect[] k_Retired = { UpgradeEffect.Spectral, UpgradeEffect.GrenadeArrows, UpgradeEffect.GrenadeCooldown };

        // Les textes provisoires des pièces nouvelles (étapes 3 et 5) que leurs nouveaux réglages rendent faux : remplacés
        // seulement s'ils n'ont pas été changés à la main.
        static readonly (UpgradeEffect effect, string oldText, string newText)[] k_Texts =
        {
            (UpgradeEffect.Bounce, "La flèche rebondit 2 fois sur le sol et les murs", "La flèche rebondit 3 fois sur le sol et les murs"),
            (UpgradeEffect.Falcon, "À pleine tension, le temps ralentit (toutes les 8 s)", "À pleine tension, le temps ralentit (une fois par tension)"),
            (UpgradeEffect.Executioner, "Tir à la tête sous 25 % de PV : l'ennemi meurt net (pas les boss)",
             "Tir à la tête qui laisse l'ennemi sous 25 % de PV : il meurt net (un boss sous 10 %)"),
            (UpgradeEffect.Engineer, "Une flèche dans ta baliste ou ta tour de givre la surcharge 5 s",
             "Une flèche dans ta baliste la fait tirer 2 fois plus vite 5 s ; un tir parfait répare une barricade"),
            (UpgradeEffect.Telekinesis, "Garde la gâchette de la main de l'arc : la flèche suit ton arc",
             "Garde la gâchette de la main de l'arc : la flèche ralentit et tourne comme ta main"),
            (UpgradeEffect.Suspended, "La flèche s'arrête en l'air ; la gâchette de l'autre main les relâche",
             "La flèche s'arrête en l'air ; la gâchette de l'autre main les envoie là où elle vise"),
        };

        static RoguelikeTuningBaker()
        {
            EditorSceneManager.sceneOpened += (scene, mode) => Schedule();
            Schedule();
        }

        static void Schedule()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            TuneCatalog();
            var prefabReady = MakeDummyAnEnemy();
            if (!prefabReady)
                return;

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    AddDummies(scene);
            }
        }

        // ---------------------------------------------------------------- le catalogue

        static void TuneCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(k_CatalogPath);
            if (catalog == null || !catalog.upgrades.Exists(upgrade => upgrade != null && upgrade.kind == UpgradeKind.Scroll) ||
                catalog.upgrades.Exists(upgrade => upgrade != null && upgrade.effect == UpgradeEffect.SuperGrenade))
                return;

            var log = new List<string>();
            foreach (var tuning in k_Tunings)
            {
                foreach (var upgrade in catalog.upgrades)
                {
                    if (upgrade == null || upgrade.effect != tuning.Effect || upgrade.kind != UpgradeKind.Object)
                        continue;

                    upgrade.value = tuning.Value;
                    upgrade.trigger = tuning.Trigger;
                    log.Add($"{upgrade.displayName} ({tuning.What})");
                }
            }

            var retired = new List<string>();
            Sprite grenadeIcon = null;
            foreach (var upgrade in catalog.upgrades)
            {
                if (upgrade == null || System.Array.IndexOf(k_Retired, upgrade.effect) < 0)
                    continue;

                if (upgrade.effect == UpgradeEffect.GrenadeArrows)
                    grenadeIcon = upgrade.icon;
                upgrade.kind = UpgradeKind.Retired;
                retired.Add(upgrade.displayName);
            }

            var texts = new List<string>();
            foreach (var (effect, oldText, newText) in k_Texts)
            {
                var upgrade = catalog.upgrades.Find(candidate => candidate != null && candidate.effect == effect);
                if (upgrade != null && upgrade.description == oldText)
                {
                    upgrade.description = newText;
                    texts.Add(upgrade.displayName);
                }
            }

            catalog.upgrades.Add(new Upgrade("Super grenade", "La grenade à flèches en projette 12 de plus et recharge 50 % plus vite",
                                             UpgradeRarity.Legendary, UpgradeEffect.SuperGrenade, 1f, 3)
            {
                kind = UpgradeKind.Object,
                slot = ArrowSlot.Talisman,
                icon = grenadeIcon,
            });

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log("Réglages roguelike du 26 octobre : catalogue de la boutique modifié. Objets : " + string.Join(", ", log) +
                      $". Retirés : {string.Join(", ", retired)}. Ajouté : Super grenade (talisman légendaire, niveau 3 au plus). " +
                      (texts.Count > 0 ? $"Textes provisoires mis à jour : {string.Join(", ", texts)}. " : "") +
                      "Les textes des anciennes cartes ne sont pas réécrits : à reprendre à la main.", catalog);
        }

        // ---------------------------------------------------------------- le mannequin

        // Le prefab du mannequin reçoit un Enemy et un TrainingDummy. Vrai si le prefab est prêt (avant ou maintenant).
        static bool MakeDummyAnEnemy()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_DummyPrefabPath);
            if (prefab == null)
                return false;
            if (prefab.GetComponent<TrainingDummy>() != null)
                return true;

            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(k_DummyDefinitionPath);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<EnemyDefinition>();
                definition.displayName = "Mannequin";
                definition.maxHealth = k_DummyHealth;
                definition.moveSpeed = 0f;
                definition.attackDamage = 0f;
                definition.playerAggroRange = 0f;
                definition.targetsPlayer = false;
                definition.points = 0;
                AssetDatabase.CreateAsset(definition, k_DummyDefinitionPath);
                AssetDatabase.SaveAssets();
            }

            var root = PrefabUtility.LoadPrefabContents(k_DummyPrefabPath);
            try
            {
                var enemy = root.GetComponent<Enemy>() != null ? root.GetComponent<Enemy>() : root.AddComponent<Enemy>();
                var serialized = new SerializedObject(enemy);
                serialized.FindProperty("m_Definition").objectReferenceValue = definition;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                root.AddComponent<TrainingDummy>();
                PrefabUtility.SaveAsPrefabAsset(root, k_DummyPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            Debug.Log($"Réglages roguelike du 26 octobre : le mannequin d'entraînement compte comme un ennemi entre les vagues " +
                      $"({k_DummyHealth:0} PV avant la difficulté et la vague, fiche {k_DummyDefinitionPath}) ; il se relève quand " +
                      "on le tue, ne rapporte rien, et les tourelles l'ignorent.", AssetDatabase.LoadAssetAtPath<GameObject>(k_DummyPrefabPath));
            return true;
        }

        // ---------------------------------------------------------------- les mannequins en plus

        static void AddDummies(Scene scene)
        {
            var group = TrainingLayoutBaker.GroupOf(scene);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_DummyPrefabPath);
            if (group == null || prefab == null)
                return;

            // Les mannequins : les éléments du groupe qui ne sont ni des cibles ni la cible mobile (comme TrainingLayoutBaker).
            var dummies = Enumerable.Range(0, group.childCount).Select(group.GetChild)
                .Count(child => child.GetComponentInChildren<TrainingTarget>(true) == null &&
                                child.GetComponentInChildren<PingPongMover>(true) == null &&
                                child.GetComponentInChildren<Health>(true) != null);
            if (dummies >= k_WantedDummies)
                return;

            var count = Mathf.Min(k_WantedDummies - dummies, k_NewDummySlots.Length);
            var items = new List<Transform>();
            var slots = new List<Vector2>();
            for (var i = 0; i < count; i++)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.transform.SetParent(group, false);
                instance.name = k_NewDummyNames[i];
                Undo.RegisterCreatedObjectUndo(instance, "Mannequins d'entraînement");
                items.Add(instance.transform);
                slots.Add(k_NewDummySlots[i]);
            }

            TrainingLayoutBaker.PlaceNew(scene, group, items, slots);
            Debug.Log($"Réglages roguelike du 26 octobre : {items.Count} mannequin(s) ajouté(s) au centre d'entraînement " +
                      $"({string.Join(", ", items.Select(item => item.name))}). Enregistre la scène (Ctrl+S).", group);
        }
    }
}
