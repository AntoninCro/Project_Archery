using System.Collections.Generic;
using Archery.Building;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archery.EditorTools
{
    /// <summary>
    /// Refonte du tower defense, étape 5 (28 octobre, <c>Docs/Refonte_TowerDefense.md</c>) : lot 1, la mine, le goudron, le
    /// tremplin et la tour de foudre ; lot 2, l'arbalète à répétition et le mortier. À l'ouverture de Unity, une seule fois
    /// par construction, ce baker :
    /// <list type="number">
    /// <item>crée les matériaux qui manquent (<c>Art/TowerDefense</c> : goudron, bois, métal, lueur) ;</item>
    /// <item>assemble le prefab de chaque construction qui manque (<c>Prefabs/Structures</c>), avec ses modèles
    /// (<c>Assets/construction</c>, Tower Defense Kit de Kenney, ou des formes simples) et les pièces des niveaux 2 et 3 ;</item>
    /// <item>crée son réglage s'il manque (<c>Data/Structures</c> : nom, emplacement, rareté, lot, textes des niveaux) ;</item>
    /// <item>l'ajoute à la liste <c>Structures</c> du Build Manager de la scène ouverte (à enregistrer, Ctrl+S).</item>
    /// </list>
    /// Ce qui existe déjà n'est jamais refait ni retouché : les réglages du joueur gardent la main.
    /// </summary>
    [InitializeOnLoad]
    static class TowerDefenseStep5Baker
    {
        const string k_PrefabFolder = "Assets/_Project/Prefabs/Structures/";
        const string k_DataFolder = "Assets/_Project/Data/Structures/";
        const string k_ArtFolder = "Assets/_Project/Art/TowerDefense";
        const string k_Landmine = "Assets/construction/Landmine by Quaternius - PtqkseZo9O/Landmine.fbx";
        const string k_ExplodingBarrel = "Assets/construction/Exploding Barrel by Quaternius - 1orHe0kCc1/ExplodingBarrel.fbx";
        const string k_Barrel = "Assets/construction/Ultimate Fantasy RTS by Quaternius/FBX/Barrel.fbx";
        const string k_SpikeMine = "Assets/construction/Spike Mine by Aaron Clifford - 4VRVeCH1sHm/model.obj";
        const string k_WatchTower = "Assets/construction/Watch Tower by Quaternius - cMxuj2gt7D/WatchTower_SecondAge_Level3.fbx";
        const string k_Mirror = "Assets/construction/Mirror by MilkAndBanana - HIeVDCNgbA/tools_mirror.fbx";
        const string k_Ghost = "Assets/construction/Ghost by Quaternius - Iip30bDHmu/Ghost.fbx";
        const string k_Kit = "Assets/kenney_tower-defense-kit/Models/FBX format/";

        struct Entry
        {
            public string File;
            public StructureKind Kind;
            public string Name;
            public BuildSlot Slot;
            public StructureRarity Rarity;
            public int Lot;
            public Color Color;
            public bool Anywhere;
            public RingElement Element;
            public string[] Levels;
        }

        static readonly Entry[] k_Entries =
        {
            new Entry
            {
                File = "Mine", Kind = StructureKind.Mine, Name = "Mine", Slot = BuildSlot.Lane, Rarity = StructureRarity.Common, Lot = 3,
                Color = new Color(1f, 0.55f, 0.3f),
                Levels = new[]
                {
                    "Explose sous le premier ennemi au sol qui passe : blesse tous ceux autour (3 m). Se réarme à chaque vague.",
                    "Dégâts ×1,5, explosion de 3,5 m.",
                    "Dégâts ×2,25, explosion de 4 m.\nProjette les ennemis en l'air.",
                },
            },
            new Entry
            {
                File = "Goudron", Kind = StructureKind.Tar, Name = "Goudron", Slot = BuildSlot.Lane, Rarity = StructureRarity.Common, Lot = 2,
                Color = new Color(0.7f, 0.58f, 0.45f),
                Levels = new[]
                {
                    "Une flaque qui ralentit de 40 % les ennemis au sol qui la traversent.",
                    "Ralentit de 50 %, flaque plus grande.",
                    "Ralentit de 60 %, une grande mare.",
                },
            },
            new Entry
            {
                File = "Tremplin", Kind = StructureKind.Trampoline, Name = "Tremplin", Slot = BuildSlot.Lane, Rarity = StructureRarity.Rare, Lot = 1,
                Color = new Color(0.6f, 1f, 0.5f),
                Levels = new[]
                {
                    "Projette en l'air, vers la forêt, un ennemi au sol qui passe dessus, toutes les 3 s. Sa chute le blesse, et le toucher en vol rapporte plus.",
                    "Plus haut, toutes les 2,2 s.",
                    "Encore plus haut, toutes les 1,5 s.",
                },
            },
            new Entry
            {
                File = "Foudre", Kind = StructureKind.LightningTower, Name = "Tour de foudre", Slot = BuildSlot.Roadside, Rarity = StructureRarity.Rare,
                Lot = 1, Color = new Color(0.55f, 0.8f, 1f),
                Levels = new[]
                {
                    "Un éclair saute entre 3 ennemis à 14 m, volants compris, toutes les 2,6 s.",
                    "Dégâts ×1,5, 16 m, toutes les 2,1 s.",
                    "Dégâts ×2, 18 m, toutes les 1,6 s.",
                },
            },

            // Lot 2.
            new Entry
            {
                File = "Arbalete", Kind = StructureKind.Repeater, Name = "Arbalète à répétition", Slot = BuildSlot.Roadside,
                Rarity = StructureRarity.Common, Lot = 1, Color = new Color(0.9f, 0.72f, 0.45f),
                Levels = new[]
                {
                    "Tire seule des rafales de 3 carreaux sur l'ennemi le plus proche à 18 m, toutes les 2,2 s. Vise d'abord les volants, plus blessés (×1,5).",
                    "Rafales de 4 carreaux, plus forts (×1,33), toutes les 1,9 s.",
                    "Rafales de 5 carreaux, encore plus forts (×1,8), toutes les 1,6 s.",
                },
            },
            new Entry
            {
                File = "Mortier", Kind = StructureKind.Mortar, Name = "Mortier", Slot = BuildSlot.Roadside, Rarity = StructureRarity.Rare,
                Lot = 1, Color = new Color(0.78f, 0.78f, 0.85f),
                Levels = new[]
                {
                    "Tire en cloche sur le groupe d'ennemis au sol le plus serré, de 6 à 24 m, toutes les 4,5 s : l'explosion blesse tous ceux autour (2,5 m). Les volants passent au-dessus.",
                    "Dégâts ×1,4, explosion de 3 m, 28 m, toutes les 3,8 s.",
                    "Deux tubes : deux obus par salve. Dégâts ×2, explosion de 3,5 m, 32 m, toutes les 3,2 s.",
                },
            },

            // Lot 3 : les constructions physiques (raretés proposées).
            new Entry
            {
                File = "Geyser", Kind = StructureKind.Geyser, Name = "Geyser", Slot = BuildSlot.Lane, Rarity = StructureRarity.Common, Lot = 1,
                Color = new Color(0.55f, 0.85f, 1f),
                Levels = new[]
                {
                    "Chargé, il jaillit dès qu'un ennemi au sol passe dessus : tous ceux qui sont dessus (1,5 m) sont projetés à la verticale. Recharge 4 s.",
                    "Plus haut, plus large (1,75 m), recharge 3,4 s.",
                    "Encore plus haut, 2 m, recharge 2,8 s.",
                },
            },
            new Entry
            {
                File = "Soufflerie", Kind = StructureKind.Blower, Name = "Soufflerie", Slot = BuildSlot.Roadside, Rarity = StructureRarity.Common,
                Lot = 1, Color = new Color(0.8f, 0.95f, 1f),
                Levels = new[]
                {
                    "Par bouffées de 2,5 s, un cône de vent de 9 m repousse vers la forêt les ennemis qui s'y trouvent, volants compris.",
                    "10,5 m, plus fort, bouffées de 3 s.",
                    "12 m, encore plus fort, bouffées de 3,5 s.",
                },
            },
            new Entry
            {
                File = "Aimant", Kind = StructureKind.Magnet, Name = "Tour-aimant", Slot = BuildSlot.Roadside, Rarity = StructureRarity.Rare,
                Lot = 1, Color = new Color(0.85f, 0.4f, 1f),
                Levels = new[]
                {
                    "Toutes les 3,5 s, attire vers elle de 3 m les ennemis à 8 m, volants compris : le groupe se resserre.",
                    "Attire de 3,5 m, à 9 m, toutes les 3 s.",
                    "Attire de 4 m, à 10 m, toutes les 2,5 s.",
                },
            },
            new Entry
            {
                File = "Catapulte", Kind = StructureKind.Catapult, Name = "Catapulte", Slot = BuildSlot.Roadside, Rarity = StructureRarity.Rare,
                Lot = 1, Color = new Color(1f, 0.75f, 0.4f),
                Levels = new[]
                {
                    "Toutes les 5 s, attrape l'ennemi au sol le plus proche (6 m) et le lance sur le groupe le plus serré à 12 m : il blesse ceux sur qui il retombe. Sans groupe, elle le renvoie vers la forêt.",
                    "Toutes les 4,2 s, 14 m, impact ×1,5.",
                    "Toutes les 3,5 s, 16 m, impact ×2,1.",
                },
            },

            // Lot 4 : les autres constructions physiques (raretés proposées).
            new Entry
            {
                File = "Trappe", Kind = StructureKind.Trapdoor, Name = "Trappe", Slot = BuildSlot.Lane, Rarity = StructureRarity.Rare, Lot = 1,
                Color = new Color(0.65f, 0.48f, 0.32f),
                Levels = new[]
                {
                    "Chargée, elle s'ouvre dès qu'un ennemi au sol passe dessus : tous ceux qui sont dessus (1,5 m) ressortent 8 m plus loin en arrière sur la voie, étourdis. Recharge 6 s.",
                    "1,75 m, 10 m en arrière, recharge 5 s.",
                    "2 m, 12 m en arrière, recharge 4 s.",
                },
            },
            new Entry
            {
                File = "Pendule", Kind = StructureKind.Pendulum, Name = "Pendule", Slot = BuildSlot.Lane, Rarity = StructureRarity.Rare, Lot = 1,
                Color = new Color(0.78f, 0.72f, 0.66f),
                Levels = new[]
                {
                    "Un boulet se balance en travers de la voie (un aller-retour en 3 s) : il fauche les ennemis au sol, les blesse et les projette sur le côté.",
                    "Dégâts ×1,5, plus rapide (2,6 s), projette plus loin.",
                    "Dégâts ×2,25, 2,2 s : un boulet hérissé.",
                },
            },
            new Entry
            {
                File = "Bulle", Kind = StructureKind.Bubble, Name = "Bulle", Slot = BuildSlot.Roadside, Rarity = StructureRarity.Rare, Lot = 1,
                Color = new Color(0.7f, 0.9f, 1f),
                Levels = new[]
                {
                    "Toutes les 7 s, enferme l'ennemi au sol le plus proche (10 m) dans une bulle qui monte à 4 m : crève-la d'une flèche, il retombe et sa chute le blesse. Sinon, elle crève seule au bout de 5 s.",
                    "Toutes les 6 s, 11 m, monte à 5 m, crève seule au bout de 6 s.",
                    "Deux bulles à la fois, toutes les 5 s, 12 m, monte à 6 m, 7 s.",
                },
            },
            new Entry
            {
                File = "Filet", Kind = StructureKind.NetLauncher, Name = "Lance-filet", Slot = BuildSlot.Roadside, Rarity = StructureRarity.Common,
                Lot = 1, Color = new Color(0.75f, 0.85f, 0.5f),
                Levels = new[]
                {
                    "Toutes les 5 s, lance un filet sur le volant le plus proche (16 m) : il tombe au sol, blessé par sa chute, et y reste 3 s.",
                    "Au sol 4 s, toutes les 4,2 s.",
                    "Deux filets à la fois, au sol 5 s, toutes les 3,5 s.",
                },
            },

            // Lot 5 : le soutien. Les anneaux élémentaires : un anneau par effet, trois bâtiments.
            new Entry
            {
                File = "LanceFlammes", Kind = StructureKind.Flamethrower, Name = "Lance-flammes", Slot = BuildSlot.Roadside,
                Rarity = StructureRarity.Rare, Lot = 1, Color = new Color(1f, 0.5f, 0.2f),
                Levels = new[]
                {
                    "Quand des ennemis sont devant lui (6 m), volants compris, il crache un jet de feu toutes les 3 s : ils brûlent (12 de dégâts sur 3 s).",
                    "7 m, brûlure ×1,5, toutes les 2,6 s.",
                    "8 m, brûlure ×2,25, toutes les 2,2 s.",
                },
            },
            new Entry
            {
                File = "Phare", Kind = StructureKind.Lighthouse, Name = "Phare", Slot = BuildSlot.Height, Rarity = StructureRarity.Rare, Lot = 1,
                Color = new Color(1f, 0.95f, 0.6f),
                Levels = new[]
                {
                    "En hauteur. Dans son rayon (12 m), la tête des ennemis brille (le point faible des boss) : une flèche qui y touche fait 20 % de dégâts en plus.",
                    "14 m, +35 %.",
                    "16 m, +50 %, deux faisceaux.",
                },
            },
            new Entry
            {
                File = "Miroir", Kind = StructureKind.Mirror, Name = "Miroir", Slot = BuildSlot.Roadside, Rarity = StructureRarity.Rare, Lot = 1,
                Color = new Color(0.85f, 0.95f, 1f),
                Levels = new[]
                {
                    "Une flèche tirée sur le miroir repart vers l'ennemi le plus proche (25 m). Il se tourne vers toi.",
                    "30 m, la flèche renvoyée fait 25 % de dégâts en plus.",
                    "35 m, +50 %, et elle traverse un ennemi de plus.",
                },
            },
            new Entry
            {
                File = "AnneauFeu", Kind = StructureKind.Ring, Name = "Anneau de feu", Slot = BuildSlot.Roadside, Rarity = StructureRarity.Rare, Lot = 1,
                Anywhere = true, Element = RingElement.Fire, Color = new Color(1f, 0.55f, 0.2f),
                Levels = new[]
                {
                    "Partout. Une flèche tirée à travers l'anneau s'enflamme : sa cible brûle (30 % de ses dégâts en plus, sur 3 s). Il se tourne vers toi.",
                    "La flèche fait 15 % de dégâts en plus, la brûlure 40 %.",
                    "La flèche fait 30 % de dégâts en plus, la brûlure 50 %.",
                },
            },
            new Entry
            {
                File = "AnneauFoudre", Kind = StructureKind.Ring, Name = "Anneau de foudre", Slot = BuildSlot.Roadside, Rarity = StructureRarity.Rare,
                Lot = 1, Anywhere = true, Element = RingElement.Lightning, Color = new Color(0.45f, 0.75f, 1f),
                Levels = new[]
                {
                    "Partout. Une flèche tirée à travers l'anneau se charge : à l'impact, un éclair saute vers 2 ennemis proches (la moitié de ses dégâts). Il se tourne vers toi.",
                    "La flèche fait 15 % de dégâts en plus ; l'éclair saute vers 3 ennemis.",
                    "La flèche fait 30 % de dégâts en plus ; 4 ennemis.",
                },
            },
            new Entry
            {
                File = "AnneauDouble", Kind = StructureKind.Ring, Name = "Anneau double", Slot = BuildSlot.Roadside, Rarity = StructureRarity.Rare,
                Lot = 1, Anywhere = true, Element = RingElement.Split, Color = new Color(0.8f, 0.45f, 1f),
                Levels = new[]
                {
                    "Partout. Une flèche tirée à travers l'anneau se dédouble : une flèche en plus part à côté d'elle (60 % de ses dégâts). Il se tourne vers toi.",
                    "La flèche fait 15 % de dégâts en plus ; la flèche en plus, 80 %.",
                    "La flèche fait 30 % de dégâts en plus ; deux flèches en plus.",
                },
            },

            // Lot 6 : sur la tour (le seau d'huile, idée du joueur), et les deux légendaires.
            new Entry
            {
                File = "SeauHuile", Kind = StructureKind.OilBucket, Name = "Seau d'huile", Slot = BuildSlot.Tower, Rarity = StructureRarity.Rare,
                Lot = 1, Color = new Color(0.9f, 0.7f, 0.2f),
                Levels = new[]
                {
                    "Sur la tour. Touche sa cible (une flèche tirée, ou tenue en main) : le seau renverse son huile bouillante sur tous les ennemis au sol à 8 m du pied de la tour (40 de dégâts) et les ralentit. Il se remplit en 20 s.",
                    "60 de dégâts, 9 m, se remplit en 16 s.",
                    "90 de dégâts, 10 m, se remplit en 12 s.",
                },
            },
            new Entry
            {
                File = "BalisteTour", Kind = StructureKind.TowerBallista, Name = "Baliste sur la tour", Slot = BuildSlot.Tower,
                Rarity = StructureRarity.Legendary, Lot = 1, Color = new Color(1f, 0.82f, 0.4f),
                Levels = new[]
                {
                    "Sur la tour, à côté de toi. Une grosse baliste qui tire seule sur l'ennemi le plus proche à 35 m (30 de dégâts, toutes les 2,5 s) ; le carreau traverse un ennemi.",
                    "Dégâts ×1,5, toutes les 2,1 s ; il traverse 2 ennemis.",
                    "Dégâts ×2,2, toutes les 1,7 s ; il traverse 3 ennemis.",
                },
            },
            new Entry
            {
                File = "Esprit", Kind = StructureKind.Spirit, Name = "Esprit gardien", Slot = BuildSlot.Roadside, Rarity = StructureRarity.Legendary,
                Lot = 1, Anywhere = true, Color = new Color(0.6f, 1f, 0.85f),
                Levels = new[]
                {
                    "Partout. Un esprit chasse les ennemis isolés à 25 m de son autel, volants compris : il fond sur eux et les frappe (15 de dégâts par coup), puis passe au suivant.",
                    "30 m, coups ×1,5, plus rapides.",
                    "35 m, coups ×2,1, encore plus rapides.",
                },
            },
        };

        static TowerDefenseStep5Baker()
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

            var managers = FindManagers();
            var definitions = new List<StructureDefinition>();
            var created = new List<string>();
            foreach (var entry in k_Entries)
            {
                var definition = AssetDatabase.LoadAssetAtPath<StructureDefinition>(k_DataFolder + entry.File + ".asset");
                if (definition == null)
                {
                    var prefab = LoadOrBuildPrefab(entry, created);
                    if (prefab == null)
                        continue;
                    definition = CreateDefinition(entry, prefab);
                    created.Add("réglage " + entry.File);
                }

                definitions.Add(definition);
            }

            var registered = 0;
            foreach (var manager in managers)
                registered += Register(manager, definitions);

            created.AddRange(ReplaceCrossbows());
            if (created.Count == 0 && registered == 0)
                return;

            AssetDatabase.SaveAssets();
            Debug.Log("Refonte du tower defense, étape 5 : " +(created.Count > 0 ? string.Join(", ", created) : "rien de créé") +
                      (registered > 0 ? $" ; {registered} bâtiment(s) ajouté(s) au Build Manager de la scène : enregistre-la (Ctrl+S)." : "."));
        }

        static List<BuildManager> FindManagers()
        {
            var managers = new List<BuildManager>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;
                foreach (var root in scene.GetRootGameObjects())
                    managers.AddRange(root.GetComponentsInChildren<BuildManager>(true));
            }

            return managers;
        }

        // Ajoute à la liste du Build Manager les bâtiments qui n'y sont pas encore.
        static int Register(BuildManager manager, List<StructureDefinition> definitions)
        {
            var settings = new SerializedObject(manager);
            var list = settings.FindProperty("m_Structures");
            if (list == null)
                return 0;

            var added = 0;
            foreach (var definition in definitions)
            {
                var present = false;
                for (var i = 0; i < list.arraySize && !present; i++)
                    present = list.GetArrayElementAtIndex(i).objectReferenceValue == definition;
                if (present)
                    continue;

                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = definition;
                added++;
            }

            if (added == 0)
                return 0;

            settings.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            return added;
        }

        static StructureDefinition CreateDefinition(Entry entry, Structure prefab)
        {
            var definition = ScriptableObject.CreateInstance<StructureDefinition>();
            definition.kind = entry.Kind;
            definition.displayName = entry.Name;
            definition.slot = entry.Slot;
            definition.prefab = prefab;
            definition.color = entry.Color;
            definition.rarity = entry.Rarity;
            definition.lotSize = entry.Lot;
            definition.anywhere = entry.Anywhere;
            definition.levels = new StructureDefinition.Level[entry.Levels.Length];
            for (var i = 0; i < entry.Levels.Length; i++)
                definition.levels[i] = new StructureDefinition.Level { cost = 0, description = entry.Levels[i] };
            AssetDatabase.CreateAsset(definition, k_DataFolder + entry.File + ".asset");
            return definition;
        }

        // ---------------------------------------------------------------- prefabs

        static Structure LoadOrBuildPrefab(Entry entry, List<string> created)
        {
            var path = k_PrefabFolder + entry.File + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing.GetComponent<Structure>();

            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject(entry.File);
                SceneManager.MoveGameObjectToScene(root, preview);
                var built = entry.Kind switch
                {
                    StructureKind.Mine => BuildMine(root, preview),
                    StructureKind.Tar => BuildTar(root, preview),
                    StructureKind.Trampoline => BuildTrampoline(root),
                    StructureKind.Repeater => BuildRepeater(root, preview),
                    StructureKind.Mortar => BuildMortar(root, preview),
                    StructureKind.Geyser => BuildGeyser(root, preview),
                    StructureKind.Blower => BuildBlower(root, preview),
                    StructureKind.Magnet => BuildMagnet(root, preview),
                    StructureKind.Catapult => BuildCatapult(root, preview),
                    StructureKind.Trapdoor => BuildTrapdoor(root),
                    StructureKind.Pendulum => BuildPendulum(root, preview),
                    StructureKind.Bubble => BuildBubble(root, preview),
                    StructureKind.NetLauncher => BuildNetLauncher(root, preview),
                    StructureKind.Flamethrower => BuildFlamethrower(root, preview),
                    StructureKind.Lighthouse => BuildLighthouse(root, preview),
                    StructureKind.Mirror => BuildMirror(root, preview),
                    StructureKind.Ring => BuildRing(root, preview, entry),
                    StructureKind.OilBucket => BuildOilBucket(root),
                    StructureKind.TowerBallista => BuildTowerBallista(root, preview),
                    StructureKind.Spirit => BuildSpirit(root, preview, entry),
                    _ => BuildLightningTower(root, preview),
                };
                if (!built)
                {
                    Debug.LogWarning($"Refonte du tower defense, étape 5 : le prefab « {entry.File} » n'a pas pu être monté (un modèle manque).");
                    return null;
                }

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                created.Add("prefab " + entry.File);
                return prefab != null ? prefab.GetComponent<Structure>() : null;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        // La mine de Quaternius au centre (cachée quand elle a explosé), deux de plus au niveau 2, un baril explosif au niveau 3.
        static bool BuildMine(GameObject root, Scene scene)
        {
            var mine = root.AddComponent<StructureMine>();
            var armed = Model(k_Landmine, root.transform, scene, "Mine armée");
            if (armed == null)
                return false;
            Fit(armed, 0.9f, Vector3.zero);

            var level2 = Part(root, "Niveau 2");
            foreach (var x in new[] { -0.8f, 0.8f })
            {
                var extra = Model(k_Landmine, level2.transform, scene, "Mine");
                if (extra != null)
                    Fit(extra, 0.6f, new Vector3(x, 0f, 0.5f));
            }

            var level3 = Part(root, "Niveau 3");
            var barrel = Model(k_ExplodingBarrel, level3.transform, scene, "Baril explosif");
            if (barrel != null)
                Fit(barrel, 0.6f, new Vector3(0f, 0f, -0.9f));

            Set(mine, "m_ArmedVisual", armed);
            SetLevelParts(mine, level2, level3);
            return true;
        }

        // La flaque est faite par le jeu ; un tonneau renversé au bord, un deuxième au niveau 2, un troisième au niveau 3.
        static bool BuildTar(GameObject root, Scene scene)
        {
            var tar = root.AddComponent<StructureTar>();
            Set(tar, "m_PuddleMaterial", MakeMaterial("Goudron", new Color(0.05f, 0.04f, 0.035f), 0.92f, 0f));

            var fallen = Model(k_Barrel, root.transform, scene, "Tonneau renversé");
            if (fallen == null)
                return false;
            fallen.transform.localRotation = Quaternion.Euler(0f, 30f, 90f);
            Fit(fallen, 0.75f, new Vector3(1.15f, 0f, 0.3f));

            var level2 = Part(root, "Niveau 2");
            var second = Model(k_Barrel, level2.transform, scene, "Tonneau");
            if (second != null)
                Fit(second, 0.6f, new Vector3(-1.1f, 0f, -0.7f));

            var level3 = Part(root, "Niveau 3");
            var third = Model(k_Barrel, level3.transform, scene, "Tonneau");
            if (third != null)
                Fit(third, 0.6f, new Vector3(-0.5f, 0f, 1.25f));

            SetLevelParts(tar, level2, level3);
            return true;
        }

        // En formes simples : un cadre de bois, quatre ressorts de métal, la planche qui rebondit (une flèche indique la forêt) ;
        // une plaque de métal au niveau 2, un liseré lumineux au niveau 3.
        static bool BuildTrampoline(GameObject root)
        {
            var trampoline = root.AddComponent<StructureTrampoline>();
            var wood = MakeMaterial("Bois", new Color(0.55f, 0.36f, 0.2f), 0.2f, 0f);
            var darkWood = MakeMaterial("Bois sombre", new Color(0.32f, 0.2f, 0.12f), 0.15f, 0f);
            var metal = MakeMaterial("Metal", new Color(0.6f, 0.62f, 0.66f), 0.6f, 0.8f);
            var glow = MakeMaterial("Lueur verte", new Color(0.4f, 1f, 0.45f), 0.5f, 0f, new Color(0.4f, 1f, 0.45f) * 2.5f);

            Box(root.transform, "Cadre", darkWood, new Vector3(0f, 0.06f, 0f), new Vector3(2.3f, 0.12f, 2.3f));
            foreach (var x in new[] { -0.75f, 0.75f })
            {
                foreach (var z in new[] { -0.75f, 0.75f })
                    Primitive(PrimitiveType.Cylinder, root.transform, "Ressort", metal, new Vector3(x, 0.26f, z), new Vector3(0.2f, 0.12f, 0.2f));
            }

            var plate = Box(root.transform, "Planche", wood, new Vector3(0f, 0.42f, 0f), new Vector3(2.1f, 0.1f, 2.1f));
            Box(plate.transform, "Flèche", darkWood, new Vector3(0f, 0.6f, 0.15f), new Vector3(0.12f, 0.3f, 0.5f), true);

            var level2 = Part(plate, "Niveau 2");
            Box(level2.transform, "Plaque", metal, new Vector3(0f, 0.6f, 0f), new Vector3(0.9f, 0.3f, 0.9f), true);

            var level3 = Part(root, "Niveau 3");
            foreach (var side in new[] { -1f, 1f })
            {
                Box(level3.transform, "Liseré", glow, new Vector3(1.17f * side, 0.13f, 0f), new Vector3(0.06f, 0.04f, 2.34f));
                Box(level3.transform, "Liseré", glow, new Vector3(0f, 0.13f, 1.17f * side), new Vector3(2.34f, 0.04f, 0.06f));
            }

            Set(trampoline, "m_Plate", plate.transform);
            SetLevelParts(trampoline, level2, level3);
            return true;
        }

        // Les tours rondes du Tower Defense Kit, empilées : la base et ses cristaux, un étage au niveau 2, le sommet au niveau 3,
        // chacun couronné de cristaux.
        static bool BuildLightningTower(GameObject root, Scene scene)
        {
            var tower = root.AddComponent<StructureLightningTower>();
            var bottom = Model(k_Kit + "tower-round-bottom-a.fbx", root.transform, scene, "Base");
            if (bottom == null)
                return false;
            var scale = Fit(bottom, 2f, Vector3.zero);
            var top = BoundsOf(bottom).max.y;
            Crystals(root.transform, scene, scale, top);

            var level2 = Part(root, "Niveau 2");
            var middle = Model(k_Kit + "tower-round-middle-a.fbx", level2.transform, scene, "Étage");
            if (middle != null)
            {
                middle.transform.localScale *= scale;
                Ground(middle, top);
                top = BoundsOf(middle).max.y;
                Crystals(level2.transform, scene, scale, top);
            }

            var level3 = Part(root, "Niveau 3");
            var crown = Model(k_Kit + "tower-round-top-a.fbx", level3.transform, scene, "Sommet");
            if (crown != null)
            {
                crown.transform.localScale *= scale;
                Ground(crown, top);
                Crystals(level3.transform, scene, scale * 1.3f, BoundsOf(crown).max.y);
            }

            SetLevelParts(tower, level2, level3);
            return true;
        }

        // Une petite baliste du Tower Defense Kit, un chargeur de carreaux sur le dos, sur une charpente de bois du même kit ; une
        // deuxième au niveau 2, une troisième et un fanion au niveau 3. Les carreaux et le son sont ceux de la baliste. (28 octobre :
        // à la place de l'arbalète de CreativeTrio, qui n'allait pas avec le style des autres tourelles, retour du joueur.)
        static bool BuildRepeater(GameObject root, Scene scene)
        {
            var repeater = root.AddComponent<StructureRepeater>();
            var structure = Model(k_Kit + "wood-structure.fbx", root.transform, scene, "Charpente");
            if (structure == null)
                return false;
            Fit(structure, 1.4f, Vector3.zero);
            var top = BoundsOf(structure).max.y;

            var turret = Part(root, "Tourelle");
            turret.transform.position = new Vector3(0f, top, 0f);
            var crossbow = SmallBallista(turret.transform, scene, k_RepeaterPlaces[0], true);
            if (crossbow == null)
                return false;

            var level2 = Part(turret, "Niveau 2");
            SmallBallista(level2.transform, scene, k_RepeaterPlaces[1], true);

            // Le niveau 3 : une troisième baliste et un fanion à l'arrière de la tourelle (il tourne avec elle).
            var level3 = Part(turret, "Niveau 3");
            SmallBallista(level3.transform, scene, k_RepeaterPlaces[2], true);
            Flag(level3.transform, new Vector3(0f, 0f, -0.55f));

            // La bouche : au bout de la baliste, un peu au-dessus de son milieu (repère de la tourelle, en mètres).
            var muzzle = MuzzleOf(turret.transform, crossbow);

            var ballista = AssetDatabase.LoadAssetAtPath<GameObject>(k_PrefabFolder + "Baliste.prefab");
            var source = ballista != null ? ballista.GetComponent<Ballista>() : null;
            if (source != null)
            {
                Copy(source, repeater, "m_BoltPrefab");
                Copy(source, repeater, "m_FireClip");
            }
            else
            {
                Debug.LogWarning("Refonte du tower defense, étape 5 : pas de prefab Baliste, l'arbalète à répétition n'a pas de carreaux.");
            }

            Set(repeater, "m_Turret", turret.transform);
            SetValues(repeater, "m_MuzzleOffset", muzzle);
            SetValues(repeater, "m_Range", 18f);
            SetValues(repeater, "m_DamagePerLevel", 3f, 4f, 5.5f);
            SetValues(repeater, "m_IntervalPerLevel", 2.2f, 1.9f, 1.6f);
            SetInts(repeater, "m_PiercePerLevel", 0, 0, 0);
            SetValues(repeater, "m_BoltSpeed", 55f);
            SetValues(repeater, "m_TurnSpeed", 300f);
            SetValues(repeater, "m_MaxPitch", 70f);
            SetLevelParts(repeater, level2, level3);
            return true;
        }

        // La place (repère du parent, en mètres) et la largeur de chaque petite baliste : l'arbalète à répétition (tourelle, niveau 2,
        // niveau 3) et le lance-filet (tourelle, niveau 3), le 28 octobre.
        static readonly (Vector3 Place, float Width)[] k_RepeaterPlaces =
        {
            (Vector3.zero, 0.8f), (new Vector3(0.62f, 0f, -0.12f), 0.5f), (new Vector3(-0.62f, 0f, -0.12f), 0.5f),
        };

        static readonly (Vector3 Place, float Width)[] k_NetLauncherPlaces =
        {
            (Vector3.zero, 0.8f), (new Vector3(0f, 0.3f, -0.05f), 0.55f),
        };

        // Une petite baliste du Tower Defense Kit (la même que la baliste, en petit), tournée comme la tourelle (son axe Z est la
        // direction de tir) ; avec un chargeur de carreaux sur le dos pour l'arbalète à répétition.
        static GameObject SmallBallista(Transform parent, Scene scene, (Vector3 Place, float Width) place, bool magazine)
        {
            var weapon = Model(k_Kit + "weapon-ballista.fbx", parent, scene, "Petite baliste");
            if (weapon == null)
                return null;

            Fit(weapon, place.Width, place.Place);
            if (!magazine)
                return weapon;

            var wood = MakeMaterial("Bois sombre", new Color(0.32f, 0.2f, 0.12f), 0.15f, 0f);
            var metal = MakeMaterial("Metal", new Color(0.6f, 0.62f, 0.66f), 0.6f, 0.8f);
            var bounds = BoundsOf(weapon);
            var size = place.Width;
            var top = parent.InverseTransformPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z - bounds.extents.z * 0.25f));
            var box = new Vector3(size * 0.22f, size * 0.1f, size * 0.36f);
            Box(parent, "Chargeur", wood, top + Vector3.up * box.y * 0.5f, box);
            for (var i = -1; i <= 1; i++)
            {
                var bolt = Primitive(PrimitiveType.Cylinder, parent, "Carreau", metal,
                                     top + new Vector3(i * box.x * 0.3f, box.y + size * 0.015f, 0f), new Vector3(size * 0.03f, box.z * 0.55f, size * 0.03f));
                bolt.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            return weapon;
        }

        // La bouche d'une tourelle : au bout de son arme, un peu au-dessus de son milieu (repère de la tourelle, en mètres).
        static Vector3 MuzzleOf(Transform turret, GameObject weapon)
        {
            var bounds = BoundsOf(weapon);
            return turret.InverseTransformPoint(new Vector3(bounds.center.x, bounds.center.y + bounds.extents.y * 0.3f, bounds.max.z));
        }

        // Les prefabs de l'arbalète à répétition et du lance-filet montés avant le 28 octobre avec l'arbalète de CreativeTrio :
        // elle est remplacée par la petite baliste du Tower Defense Kit, une seule fois (il reste un objet « Arbalète »), sans
        // toucher aux autres réglages du joueur, sauf la bouche (au bout du nouveau modèle).
        static List<string> ReplaceCrossbows()
        {
            var replaced = new List<string>();
            foreach (var (file, places, magazine) in new[] { ("Arbalete", k_RepeaterPlaces, true), ("Filet", k_NetLauncherPlaces, false) })
            {
                var path = k_PrefabFolder + file + ".prefab";
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                    continue;

                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var olds = new List<Transform>();
                    foreach (var child in root.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.name == "Arbalète")
                            olds.Add(child);
                    }

                    var structure = root.GetComponent<Ballista>();
                    var turret = root.transform.Find("Tourelle");
                    if (olds.Count == 0 || structure == null || turret == null)
                        continue;

                    foreach (var old in olds)
                        Object.DestroyImmediate(old.gameObject);

                    // La tourelle, puis les pièces des niveaux 2 et 3 qui en ont une (le lance-filet n'en a pas au niveau 2).
                    var parents = new List<Transform> { turret };
                    foreach (var level in new[] { "Niveau 2", "Niveau 3" })
                    {
                        var part = turret.Find(level);
                        if (part != null && (magazine || level == "Niveau 3"))
                            parents.Add(part);
                    }

                    GameObject main = null;
                    for (var i = 0; i < parents.Count && i < places.Length; i++)
                    {
                        var weapon = SmallBallista(parents[i], root.scene, places[i], magazine);
                        if (i == 0)
                            main = weapon;
                    }

                    if (main != null)
                        SetValues(structure, "m_MuzzleOffset", MuzzleOf(turret, main));
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    replaced.Add("prefab " + file + " (petite baliste à la place de l'arbalète)");
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            return replaced;
        }

        // Un canon du Tower Defense Kit, levé vers le ciel, sur un socle rond : des boulets empilés au niveau 2, un deuxième
        // tube au niveau 3. Les obus sont les boulets du kit.
        static bool BuildMortar(GameObject root, Scene scene)
        {
            var mortar = root.AddComponent<StructureMortar>();
            var pedestal = Model(k_Kit + "tower-round-base.fbx", root.transform, scene, "Socle");
            if (pedestal == null)
                return false;
            Fit(pedestal, 1.8f, Vector3.zero);
            var top = BoundsOf(pedestal).max.y;

            var turret = Part(root, "Affût");
            turret.transform.position = new Vector3(0f, top, 0f);
            var muzzle = Tube(turret.transform, scene, Vector3.zero, 1.1f, "Bouche");
            if (muzzle == null)
                return false;

            var level2 = Part(root, "Niveau 2");
            var ball = 0.3f;
            var pile = new[]
            {
                new Vector3(0.62f, 0f, -0.45f), new Vector3(0.62f + ball, 0f, -0.45f), new Vector3(0.62f + ball * 0.5f, 0f, -0.45f + ball * 0.87f),
                new Vector3(0.62f + ball * 0.5f, ball * 0.8f, -0.45f + ball * 0.29f),
            };
            foreach (var place in pile)
            {
                var cannonball = Model(k_Kit + "weapon-ammo-cannonball.fbx", level2.transform, scene, "Boulet");
                if (cannonball != null)
                    Fit(cannonball, ball, new Vector3(place.x, top + place.y, place.z));
            }

            var level3 = Part(turret, "Niveau 3");
            var second = Tube(level3.transform, scene, new Vector3(0.6f, 0f, -0.15f), 0.9f, "Bouche 2");

            Set(mortar, "m_Turret", turret.transform);
            SetObjects(mortar, "m_Muzzles", muzzle, second);
            Set(mortar, "m_ShellModel", AssetDatabase.LoadAssetAtPath<GameObject>(k_Kit + "weapon-ammo-cannonball.fbx"));
            var bolt = AssetDatabase.LoadAssetAtPath<GameObject>(k_PrefabFolder + "Ballista Bolt.prefab");
            var boltScript = bolt != null ? bolt.GetComponent<BallistaBolt>() : null;
            if (boltScript != null)
                Copy(boltScript, mortar, "m_TrailMaterial");
            SetLevelParts(mortar, level2, level3);
            return true;
        }

        // Un tube levé de 40° vers l'avant de l'affût (axe Z), et l'objet vide de sa bouche, au bout.
        static Transform Tube(Transform parent, Scene scene, Vector3 place, float width, string muzzleName)
        {
            var tilt = new GameObject("Tube").transform;
            tilt.SetParent(parent, false);
            tilt.localPosition = place;
            tilt.localRotation = Quaternion.Euler(-40f, 0f, 0f);
            var cannon = Model(k_Kit + "weapon-cannon.fbx", tilt, scene, "Canon");
            if (cannon == null)
                return null;
            Fit(cannon, width, Vector3.zero);

            var bounds = BoundsOf(cannon);
            var muzzle = new GameObject(muzzleName).transform;
            muzzle.SetParent(tilt, false);
            muzzle.position = bounds.center + tilt.forward * Mathf.Max(bounds.extents.y, bounds.extents.z);
            return muzzle;
        }

        // Un trou sombre entre trois rochers du Tower Defense Kit, d'où jaillit la colonne d'eau (un cylindre au matériau des
        // rayons de lumière) ; des cristaux au niveau 2, un gros rocher et un gros cristal au niveau 3.
        static bool BuildGeyser(GameObject root, Scene scene)
        {
            var geyser = root.AddComponent<StructureGeyser>();
            Primitive(PrimitiveType.Cylinder, root.transform, "Trou", MakeMaterial("Trou", new Color(0.07f, 0.09f, 0.11f), 0.6f, 0f),
                      new Vector3(0f, 0.015f, 0f), new Vector3(1.3f, 0.015f, 1.3f));
            for (var i = 0; i < 3; i++)
            {
                var rock = Model(k_Kit + "detail-rocks.fbx", root.transform, scene, "Rochers");
                if (rock == null)
                    return false;
                rock.transform.localRotation = Quaternion.Euler(0f, 40f + 120f * i, 0f);
                Fit(rock, 0.75f, Quaternion.Euler(0f, 120f * i, 0f) * new Vector3(0f, 0f, 1.1f));
            }

            var level2 = Part(root, "Niveau 2");
            foreach (var angle in new[] { 60f, 300f })
            {
                var crystal = Model(k_Kit + "detail-crystal.fbx", level2.transform, scene, "Cristal");
                if (crystal != null)
                    Fit(crystal, 0.45f, Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, 1.2f));
            }

            var level3 = Part(root, "Niveau 3");
            var bigRock = Model(k_Kit + "detail-rocks-large.fbx", level3.transform, scene, "Gros rocher");
            if (bigRock != null)
                Fit(bigRock, 0.9f, new Vector3(0f, 0f, -1.35f));
            var bigCrystal = Model(k_Kit + "detail-crystal-large.fbx", level3.transform, scene, "Gros cristal");
            if (bigCrystal != null)
                Fit(bigCrystal, 0.6f, new Vector3(0.9f, 0f, -0.9f));

            var column = new GameObject("Colonne d'eau").transform;
            column.SetParent(root.transform, false);
            var beam = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Light_Beam.mat");
            var water = Primitive(PrimitiveType.Cylinder, column, "Eau", beam, new Vector3(0f, 0.5f, 0f), new Vector3(1.3f, 0.5f, 1.3f));
            water.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            Set(geyser, "m_WaterColumn", column);
            SetLevelParts(geyser, level2, level3);
            return true;
        }

        // Une charpente de bois du Tower Defense Kit, une nacelle et des pales (formes simples) tournées vers la voie ; 4 pales
        // de plus au niveau 2 ; un cadre de métal autour des pales et un fanion au niveau 3.
        static bool BuildBlower(GameObject root, Scene scene)
        {
            var blower = root.AddComponent<StructureBlower>();
            var structure = Model(k_Kit + "wood-structure.fbx", root.transform, scene, "Charpente");
            if (structure == null)
                return false;
            Fit(structure, 1.4f, Vector3.zero);
            var top = BoundsOf(structure).max.y;

            var wood = MakeMaterial("Bois", new Color(0.55f, 0.36f, 0.2f), 0.2f, 0f);
            var darkWood = MakeMaterial("Bois sombre", new Color(0.32f, 0.2f, 0.12f), 0.15f, 0f);
            var metal = MakeMaterial("Metal", new Color(0.6f, 0.62f, 0.66f), 0.6f, 0.8f);
            var hub = new Vector3(0f, top + 0.65f, 0.4f);
            Box(root.transform, "Nacelle", darkWood, new Vector3(0f, top + 0.65f, 0f), new Vector3(0.4f, 0.4f, 0.6f));
            Box(root.transform, "Pied", darkWood, new Vector3(0f, top + 0.22f, 0f), new Vector3(0.16f, 0.45f, 0.16f));

            var rotor = new GameObject("Rotor").transform;
            rotor.SetParent(root.transform, false);
            rotor.localPosition = hub;
            var axle = Primitive(PrimitiveType.Cylinder, rotor, "Moyeu", metal, Vector3.zero, new Vector3(0.18f, 0.08f, 0.18f));
            axle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Blades(rotor, wood, 0f);

            var level2 = Part(rotor.gameObject, "Niveau 2");
            Blades(level2.transform, wood, 45f);

            var level3 = Part(root, "Niveau 3");
            foreach (var side in new[] { -1f, 1f })
            {
                Box(level3.transform, "Cadre", metal, new Vector3(1.05f * side, hub.y, hub.z + 0.05f), new Vector3(0.06f, 2.16f, 0.06f));
                Box(level3.transform, "Cadre", metal, new Vector3(0f, hub.y + 1.05f * side, hub.z + 0.05f), new Vector3(2.16f, 0.06f, 0.06f));
            }

            Flag(level3.transform, new Vector3(-0.55f, top, -0.55f));
            Set(blower, "m_Rotor", rotor);
            SetLevelParts(blower, level2, level3);
            return true;
        }

        // Quatre pales autour de l'axe Z du rotor, décalées de cet angle.
        static void Blades(Transform rotor, Material material, float offset)
        {
            for (var i = 0; i < 4; i++)
            {
                var angle = offset + 90f * i;
                var blade = Box(rotor, "Pale", material, Quaternion.Euler(0f, 0f, angle) * new Vector3(0f, 0.55f, 0f), new Vector3(0.2f, 0.9f, 0.04f));
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, angle) * Quaternion.Euler(0f, 20f, 0f);
            }
        }

        // Une tour ronde du Tower Defense Kit couronnée d'un aimant en U (formes simples, rouge à bouts de métal) ; des
        // bobines de cuivre au niveau 2 ; des cristaux et une lueur entre les pôles au niveau 3.
        static bool BuildMagnet(GameObject root, Scene scene)
        {
            var magnet = root.AddComponent<StructureMagnet>();
            var bottom = Model(k_Kit + "tower-round-bottom-b.fbx", root.transform, scene, "Base");
            if (bottom == null)
                return false;
            var scale = Fit(bottom, 1.7f, Vector3.zero);
            var top = BoundsOf(bottom).max.y;

            var red = MakeMaterial("Aimant", new Color(0.78f, 0.12f, 0.12f), 0.5f, 0.2f);
            var metal = MakeMaterial("Metal", new Color(0.6f, 0.62f, 0.66f), 0.6f, 0.8f);
            var copper = MakeMaterial("Cuivre", new Color(0.85f, 0.45f, 0.2f), 0.7f, 0.9f);
            var glow = MakeMaterial("Lueur violette", new Color(0.8f, 0.4f, 1f), 0.5f, 0f, new Color(0.8f, 0.4f, 1f) * 2.5f);

            Box(root.transform, "Aimant", red, new Vector3(0f, top + 0.11f, 0f), new Vector3(0.84f, 0.22f, 0.24f));
            foreach (var x in new[] { -0.31f, 0.31f })
            {
                Box(root.transform, "Branche", red, new Vector3(x, top + 0.5f, 0f), new Vector3(0.22f, 0.6f, 0.24f));
                Box(root.transform, "Pôle", metal, new Vector3(x, top + 0.9f, 0f), new Vector3(0.24f, 0.2f, 0.26f));
            }

            var level2 = Part(root, "Niveau 2");
            foreach (var x in new[] { -0.31f, 0.31f })
            {
                foreach (var y in new[] { 0.35f, 0.5f, 0.65f })
                    Primitive(PrimitiveType.Cylinder, level2.transform, "Bobine", copper, new Vector3(x, top + y, 0f), new Vector3(0.3f, 0.04f, 0.3f));
            }

            var level3 = Part(root, "Niveau 3");
            Box(level3.transform, "Champ", glow, new Vector3(0f, top + 0.9f, 0f), new Vector3(0.36f, 0.06f, 0.06f));
            var crystals = Model(k_Kit + "tower-round-crystals.fbx", level3.transform, scene, "Cristaux");
            if (crystals != null)
            {
                crystals.transform.localScale *= scale;
                Ground(crystals, top);
            }

            SetLevelParts(magnet, level2, level3);
            return true;
        }

        // La catapulte du Tower Defense Kit (elle bascule au lancer) ; des rochers en réserve au niveau 2 ; plus de rochers et un
        // fanion au niveau 3.
        static bool BuildCatapult(GameObject root, Scene scene)
        {
            var catapult = root.AddComponent<StructureCatapult>();
            var arm = new GameObject("Bras").transform;
            arm.SetParent(root.transform, false);
            var model = Model(k_Kit + "weapon-catapult.fbx", arm, scene, "Catapulte");
            if (model == null)
                return false;
            Fit(model, 1.7f, Vector3.zero);

            var level2 = Part(root, "Niveau 2");
            Boulders(level2.transform, scene, new[] { new Vector3(-0.85f, 0f, -0.6f), new Vector3(-0.5f, 0f, -0.8f) });

            var level3 = Part(root, "Niveau 3");
            Boulders(level3.transform, scene, new[] { new Vector3(-0.68f, 0.28f, -0.7f), new Vector3(0.85f, 0f, -0.7f) });
            Flag(level3.transform, new Vector3(0.75f, 0f, 0.55f));

            Set(catapult, "m_Arm", arm);
            SetLevelParts(catapult, level2, level3);
            return true;
        }

        static void Boulders(Transform parent, Scene scene, Vector3[] places)
        {
            foreach (var place in places)
            {
                var boulder = Model(k_Kit + "weapon-ammo-boulder.fbx", parent, scene, "Rocher");
                if (boulder != null)
                    Fit(boulder, 0.38f, place);
            }
        }

        // En formes simples : un cadre de bois sur un trou noir, deux battants qui s'ouvrent vers le bas (pivots aux bords) ;
        // des barres de métal sur les battants au niveau 2 ; un cadre de métal au niveau 3.
        static bool BuildTrapdoor(GameObject root)
        {
            var trapdoor = root.AddComponent<StructureTrapdoor>();
            var wood = MakeMaterial("Bois", new Color(0.55f, 0.36f, 0.2f), 0.2f, 0f);
            var darkWood = MakeMaterial("Bois sombre", new Color(0.32f, 0.2f, 0.12f), 0.15f, 0f);
            var metal = MakeMaterial("Metal", new Color(0.6f, 0.62f, 0.66f), 0.6f, 0.8f);
            var hole = MakeMaterial("Trou", new Color(0.07f, 0.09f, 0.11f), 0.6f, 0f);

            Box(root.transform, "Trou", hole, new Vector3(0f, 0.01f, 0f), new Vector3(2f, 0.02f, 2f));
            foreach (var side in new[] { -1f, 1f })
            {
                Box(root.transform, "Cadre", darkWood, new Vector3(1.08f * side, 0.06f, 0f), new Vector3(0.16f, 0.12f, 2.32f));
                Box(root.transform, "Cadre", darkWood, new Vector3(0f, 0.06f, 1.08f * side), new Vector3(2.32f, 0.12f, 0.16f));
            }

            // Battant 1 : pivot au bord +X, il s'étend vers -X et s'ouvre en tournant de + autour de Z (vers le bas) ; battant 2 : l'inverse.
            var door1 = Door(root.transform, "Battant 1", wood, 1f);
            var door2 = Door(root.transform, "Battant 2", wood, -1f);

            // Le niveau 2 : des ferrures de métal sur le cadre (fixes : une pièce de niveau ne peut pas suivre les deux battants).
            var level2 = Part(root, "Niveau 2");
            foreach (var x in new[] { -1f, 1f })
            {
                Box(level2.transform, "Charnière", metal, new Vector3(x * 1.02f, 0.14f, 0f), new Vector3(0.08f, 0.06f, 1.9f));
                foreach (var z in new[] { -1.08f, 1.08f })
                    Box(level2.transform, "Coin", metal, new Vector3(x * 1.08f, 0.13f, z), new Vector3(0.22f, 0.04f, 0.22f));
            }

            var level3 = Part(root, "Niveau 3");
            foreach (var side in new[] { -1f, 1f })
            {
                Box(level3.transform, "Cadre de métal", metal, new Vector3(1.2f * side, 0.08f, 0f), new Vector3(0.08f, 0.16f, 2.48f));
                Box(level3.transform, "Cadre de métal", metal, new Vector3(0f, 0.08f, 1.2f * side), new Vector3(2.48f, 0.16f, 0.08f));
            }

            Set(trapdoor, "m_LeftDoor", door1);
            Set(trapdoor, "m_RightDoor", door2);
            SetLevelParts(trapdoor, level2, level3);
            return true;
        }

        // Un battant de 1 × 2 m : son pivot au bord (x = side), la planche vers le centre.
        static Transform Door(Transform parent, string name, Material material, float side)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false);
            pivot.localPosition = new Vector3(side, 0.1f, 0f);
            Box(pivot, "Planche", material, new Vector3(-0.5f * side, 0f, 0f), new Vector3(0.98f, 0.06f, 1.98f));
            return pivot;
        }

        // Un portique de bois en travers de la voie (poteaux devant et derrière, poutre le long de la voie), une chaîne et un
        // boulet du Tower Defense Kit qui se balance en travers ; des cerclages de métal au niveau 2 ; un boulet hérissé
        // (*Spike Mine*) au niveau 3.
        static bool BuildPendulum(GameObject root, Scene scene)
        {
            var pendulum = root.AddComponent<StructurePendulum>();
            var darkWood = MakeMaterial("Bois sombre", new Color(0.32f, 0.2f, 0.12f), 0.15f, 0f);
            var metal = MakeMaterial("Metal", new Color(0.6f, 0.62f, 0.66f), 0.6f, 0.8f);
            const float pivotHeight = 4.4f;
            const float chain = 3.4f;

            foreach (var z in new[] { -1.4f, 1.4f })
            {
                Box(root.transform, "Poteau", darkWood, new Vector3(0f, pivotHeight * 0.5f + 0.1f, z), new Vector3(0.26f, pivotHeight + 0.2f, 0.26f));
                Box(root.transform, "Pied", darkWood, new Vector3(0f, 0.1f, z), new Vector3(0.9f, 0.2f, 0.4f));
            }

            Box(root.transform, "Poutre", darkWood, new Vector3(0f, pivotHeight + 0.15f, 0f), new Vector3(0.3f, 0.3f, 3.1f));

            var arm = new GameObject("Balancier").transform;
            arm.SetParent(root.transform, false);
            arm.localPosition = new Vector3(0f, pivotHeight, 0f);
            Primitive(PrimitiveType.Cylinder, arm, "Chaîne", metal, new Vector3(0f, -chain * 0.5f, 0f), new Vector3(0.07f, chain * 0.5f, 0.07f));

            var ball = new GameObject("Boulet").transform;
            ball.SetParent(arm, false);
            // Le centre du boulet à 0,7 m du sol au bas de sa course : à hauteur de la taille des ennemis.
            ball.localPosition = new Vector3(0f, -chain - 0.3f, 0f);
            var boulder = Model(k_Kit + "weapon-ammo-boulder.fbx", ball, scene, "Rocher");
            if (boulder == null)
                return false;
            Fit(boulder, 1f, new Vector3(0f, -0.5f, 0f));

            var level2 = Part(ball.gameObject, "Niveau 2");
            var band = Primitive(PrimitiveType.Cylinder, level2.transform, "Cerclage", metal, Vector3.zero, new Vector3(1.06f, 0.05f, 1.06f));
            var cross = Primitive(PrimitiveType.Cylinder, level2.transform, "Cerclage", metal, Vector3.zero, new Vector3(1.06f, 0.05f, 1.06f));
            band.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            cross.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            var level3 = Part(ball.gameObject, "Niveau 3");
            var spikes = Model(k_SpikeMine, level3.transform, scene, "Boulet hérissé");
            if (spikes != null)
                Fit(spikes, 1.35f, new Vector3(0f, -0.62f, 0f));

            Set(pendulum, "m_Arm", arm);
            Set(pendulum, "m_Ball", ball);
            SetLevelParts(pendulum, level2, level3);
            return true;
        }

        // Un chaudron de fonte (formes simples) au liquide bleu lumineux, une bulle qui flotte au-dessus ; des rochers au
        // niveau 2 ; une deuxième bulle et un gros cristal au niveau 3. La bulle est faite par le jeu, au matériau « Bulle ».
        static bool BuildBubble(GameObject root, Scene scene)
        {
            var bubble = root.AddComponent<StructureBubble>();
            var iron = MakeMaterial("Fonte", new Color(0.15f, 0.15f, 0.17f), 0.4f, 0.6f);
            var liquid = MakeMaterial("Lueur bleue", new Color(0.4f, 0.8f, 1f), 0.8f, 0f, new Color(0.4f, 0.8f, 1f) * 2f);
            var film = MakeTransparentMaterial("Bulle", new Color(0.75f, 0.9f, 1f, 0.28f), 0.95f);

            Primitive(PrimitiveType.Cylinder, root.transform, "Chaudron", iron, new Vector3(0f, 0.35f, 0f), new Vector3(0.9f, 0.3f, 0.9f));
            Primitive(PrimitiveType.Cylinder, root.transform, "Bord", iron, new Vector3(0f, 0.66f, 0f), new Vector3(1.02f, 0.03f, 1.02f));
            Primitive(PrimitiveType.Cylinder, root.transform, "Liquide", liquid, new Vector3(0f, 0.665f, 0f), new Vector3(0.84f, 0.02f, 0.84f));
            foreach (var angle in new[] { 0f, 120f, 240f })
                Box(root.transform, "Pied", iron, Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0.06f, 0.38f), new Vector3(0.14f, 0.12f, 0.14f));
            Primitive(PrimitiveType.Sphere, root.transform, "Bulle", film, new Vector3(0f, 1.15f, 0f), new Vector3(0.4f, 0.4f, 0.4f));

            var level2 = Part(root, "Niveau 2");
            foreach (var angle in new[] { 60f, 200f })
            {
                var rock = Model(k_Kit + "detail-rocks.fbx", level2.transform, scene, "Rochers");
                if (rock != null)
                    Fit(rock, 0.55f, Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, 0.95f));
            }

            var level3 = Part(root, "Niveau 3");
            Primitive(PrimitiveType.Sphere, level3.transform, "Bulle", film, new Vector3(0.25f, 1.6f, 0.1f), new Vector3(0.28f, 0.28f, 0.28f));
            var crystal = Model(k_Kit + "detail-crystal-large.fbx", level3.transform, scene, "Gros cristal");
            if (crystal != null)
                Fit(crystal, 0.55f, new Vector3(-0.8f, 0f, -0.6f));

            Set(bubble, "m_BubbleMaterial", film);
            SetLevelParts(bubble, level2, level3);
            return true;
        }

        // Une petite baliste du Tower Defense Kit (une arbalète de CreativeTrio avant le 28 octobre) et un filet roulé sur une base
        // de pierre du même kit ; un deuxième filet au niveau 2 ; une deuxième baliste et un fanion au niveau 3 (deux filets par
        // tir). Le son est celui de la baliste.
        static bool BuildNetLauncher(GameObject root, Scene scene)
        {
            var launcher = root.AddComponent<StructureNetLauncher>();
            var pedestal = Model(k_Kit + "tower-square-bottom-a.fbx", root.transform, scene, "Base");
            if (pedestal == null)
                return false;
            Fit(pedestal, 1.4f, Vector3.zero);
            var top = BoundsOf(pedestal).max.y;

            var net = MakeMaterial("Filet", new Color(0.45f, 0.38f, 0.25f), 0.1f, 0f);
            var turret = Part(root, "Tourelle");
            turret.transform.position = new Vector3(0f, top, 0f);
            var crossbow = SmallBallista(turret.transform, scene, k_NetLauncherPlaces[0], false);
            if (crossbow == null)
                return false;
            NetRoll(turret.transform, net, new Vector3(0.38f, 0.12f, -0.15f));

            var level2 = Part(turret, "Niveau 2");
            NetRoll(level2.transform, net, new Vector3(-0.38f, 0.12f, -0.15f));

            var level3 = Part(turret, "Niveau 3");
            SmallBallista(level3.transform, scene, k_NetLauncherPlaces[1], false);
            Flag(level3.transform, new Vector3(0f, 0f, -0.5f));

            var muzzle = MuzzleOf(turret.transform, crossbow);
            var ballista = AssetDatabase.LoadAssetAtPath<GameObject>(k_PrefabFolder + "Baliste.prefab");
            var source = ballista != null ? ballista.GetComponent<Ballista>() : null;
            if (source != null)
            {
                // Le carreau ne sert pas (il lance un filet), mais la baliste en réclame un.
                Copy(source, launcher, "m_BoltPrefab");
                Copy(source, launcher, "m_FireClip");
            }

            Set(launcher, "m_Turret", turret.transform);
            Set(launcher, "m_NetMaterial", net);
            SetValues(launcher, "m_MuzzleOffset", muzzle);
            SetValues(launcher, "m_Range", 16f);
            SetValues(launcher, "m_IntervalPerLevel", 5f, 4.2f, 3.5f);
            SetValues(launcher, "m_TurnSpeed", 260f);
            SetValues(launcher, "m_MaxPitch", 75f);
            SetLevelParts(launcher, level2, level3);
            return true;
        }

        // Un tonneau du pack RTS (le réservoir), un tuyau et une buse de métal tournés vers l'avant (la voie) ; un deuxième
        // tonneau au niveau 2 ; une bague lumineuse sur la buse et un fanion au niveau 3. Le jet est fait par le jeu.
        static bool BuildFlamethrower(GameObject root, Scene scene)
        {
            var flamethrower = root.AddComponent<StructureFlamethrower>();
            var tank = Model(k_Barrel, root.transform, scene, "Réservoir");
            if (tank == null)
                return false;
            Fit(tank, 0.8f, new Vector3(0f, 0f, -0.25f));
            var top = BoundsOf(tank).max.y;

            var metal = MakeMaterial("Metal", new Color(0.6f, 0.62f, 0.66f), 0.6f, 0.8f);
            var glow = MakeMaterial("Lueur orange", new Color(1f, 0.5f, 0.15f), 0.5f, 0f, new Color(1f, 0.5f, 0.15f) * 2.5f);
            Box(root.transform, "Tuyau", metal, new Vector3(0f, top + 0.08f, 0f), new Vector3(0.12f, 0.16f, 0.6f));
            var barrel = Primitive(PrimitiveType.Cylinder, root.transform, "Buse", metal, new Vector3(0f, top + 0.12f, 0.45f), new Vector3(0.16f, 0.25f, 0.16f));
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var nozzle = new GameObject("Bouche").transform;
            nozzle.SetParent(root.transform, false);
            nozzle.localPosition = new Vector3(0f, top + 0.12f, 0.72f);

            var level2 = Part(root, "Niveau 2");
            var second = Model(k_Barrel, level2.transform, scene, "Réservoir");
            if (second != null)
                Fit(second, 0.65f, new Vector3(-0.65f, 0f, -0.45f));

            var level3 = Part(root, "Niveau 3");
            var ring = Primitive(PrimitiveType.Cylinder, level3.transform, "Bague", glow, new Vector3(0f, top + 0.12f, 0.62f), new Vector3(0.22f, 0.04f, 0.22f));
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Flag(level3.transform, new Vector3(0.5f, 0f, -0.5f));

            Set(flamethrower, "m_Nozzle", nozzle);
            SetLevelParts(flamethrower, level2, level3);
            return true;
        }

        // La tour de guet de Quaternius, une lanterne au sommet et un faisceau (le matériau des rayons de lumière) qui tourne ;
        // un fanion au niveau 2 ; un deuxième faisceau, à l'opposé, au niveau 3.
        static bool BuildLighthouse(GameObject root, Scene scene)
        {
            var lighthouse = root.AddComponent<StructureLighthouse>();
            var tower = Model(k_WatchTower, root.transform, scene, "Tour de guet");
            if (tower == null)
                return false;
            Fit(tower, 2.2f, Vector3.zero);
            var top = BoundsOf(tower).max.y;

            var lamp = MakeMaterial("Lueur jaune", new Color(1f, 0.9f, 0.55f), 0.5f, 0f, new Color(1f, 0.9f, 0.55f) * 3f);
            var beamMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Light_Beam.mat");
            Primitive(PrimitiveType.Sphere, root.transform, "Lanterne", lamp, new Vector3(0f, top + 0.3f, 0f), new Vector3(0.45f, 0.45f, 0.45f));

            var beam = new GameObject("Faisceau").transform;
            beam.SetParent(root.transform, false);
            beam.localPosition = new Vector3(0f, top + 0.3f, 0f);
            Beam(beam, beamMaterial, 1f);

            var level2 = Part(root, "Niveau 2");
            Flag(level2.transform, new Vector3(0.6f, top, 0.6f));

            var level3 = Part(beam.gameObject, "Niveau 3");
            Beam(level3.transform, beamMaterial, -1f);

            Set(lighthouse, "m_Beam", beam);
            Set(lighthouse, "m_MarkerMaterial", MakeMaterial("Lueur point faible", new Color(1f, 0.95f, 0.4f), 0.5f, 0f, new Color(1f, 0.95f, 0.4f) * 3f));
            SetLevelParts(lighthouse, level2, level3);
            return true;
        }

        // Un faisceau couché de 6 m, d'un côté (side = 1 : +X, -1 : -X).
        static void Beam(Transform parent, Material material, float side)
        {
            var ray = Primitive(PrimitiveType.Cylinder, parent, "Rayon", material, new Vector3(3f * side, 0f, 0f), new Vector3(0.45f, 3f, 0.45f));
            ray.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            ray.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // Un miroir (MilkAndBanana) sur un pied de bois ; il pivote vers la tour. La glace est le seul collider d'un bâtiment.
        // Un cadre de bois au niveau 2 ; des cristaux au niveau 3.
        static bool BuildMirror(GameObject root, Scene scene)
        {
            var mirror = root.AddComponent<StructureMirror>();
            var darkWood = MakeMaterial("Bois sombre", new Color(0.32f, 0.2f, 0.12f), 0.15f, 0f);
            Box(root.transform, "Pied", darkWood, new Vector3(0f, 0.05f, 0f), new Vector3(0.7f, 0.1f, 0.5f));
            Box(root.transform, "Montant", darkWood, new Vector3(0f, 0.45f, 0f), new Vector3(0.12f, 0.8f, 0.12f));

            var pivot = Part(root, "Pivot");
            var glass = Model(k_Mirror, pivot.transform, scene, "Miroir");
            if (glass == null)
                return false;
            Fit(glass, 1.2f, new Vector3(0f, 0.8f, 0f));

            // La glace : une boîte à la taille du miroir, au moins 15 cm d'épaisseur (les flèches vont vite).
            var bounds = BoundsOf(glass);
            var pane = pivot.AddComponent<BoxCollider>();
            pane.center = pivot.transform.InverseTransformPoint(bounds.center);
            pane.size = new Vector3(Mathf.Max(0.15f, bounds.size.x), Mathf.Max(0.15f, bounds.size.y), Mathf.Max(0.15f, bounds.size.z));

            var level2 = Part(pivot, "Niveau 2");
            var frame = new Vector3(bounds.size.x + 0.16f, bounds.size.y + 0.16f, 0.08f);
            var center = pivot.transform.InverseTransformPoint(bounds.center);
            foreach (var side in new[] { -1f, 1f })
            {
                Box(level2.transform, "Cadre", darkWood, center + new Vector3(frame.x * 0.5f * side, 0f, 0f), new Vector3(0.08f, frame.y, 0.08f));
                Box(level2.transform, "Cadre", darkWood, center + new Vector3(0f, frame.y * 0.5f * side, 0f), new Vector3(frame.x, 0.08f, 0.08f));
            }

            var level3 = Part(pivot, "Niveau 3");
            foreach (var side in new[] { -1f, 1f })
            {
                var crystal = Model(k_Kit + "detail-crystal.fbx", level3.transform, scene, "Cristal");
                if (crystal != null)
                    Fit(crystal, 0.3f, center + new Vector3(frame.x * 0.5f * side, frame.y * 0.5f, 0f));
            }

            Set(mirror, "m_Pivot", pivot.transform);
            Set(mirror, "m_Pane", pane);
            SetLevelParts(mirror, level2, level3);
            return true;
        }

        // Un anneau lumineux debout sur un pied, à la couleur de son effet (un tore fait par le baker) ; un deuxième anneau qui
        // tourne en sens contraire au niveau 2 ; des cristaux autour au niveau 3.
        static bool BuildRing(GameObject root, Scene scene, Entry entry)
        {
            var ring = root.AddComponent<StructureRing>();
            var metal = MakeMaterial("Metal", new Color(0.6f, 0.62f, 0.66f), 0.6f, 0.8f);
            var glow = MakeMaterial("Lueur " + entry.File, entry.Color, 0.5f, 0f, entry.Color * 2.5f);
            Primitive(PrimitiveType.Cylinder, root.transform, "Socle", metal, new Vector3(0f, 0.05f, 0f), new Vector3(0.6f, 0.05f, 0.6f));
            Primitive(PrimitiveType.Cylinder, root.transform, "Pied", metal, new Vector3(0f, 0.38f, 0f), new Vector3(0.08f, 0.33f, 0.08f));

            var hoop = new GameObject("Anneau").transform;
            hoop.SetParent(root.transform, false);
            hoop.localPosition = new Vector3(0f, 1.6f, 0f);
            var outer = Torus(hoop, "Cercle", glow, 0.85f);

            var level2 = Part(hoop.gameObject, "Niveau 2");
            var inner = Torus(level2.transform, "Cercle intérieur", glow, 0.65f);

            var level3 = Part(hoop.gameObject, "Niveau 3");
            for (var i = 0; i < 3; i++)
            {
                var crystal = Model(k_Kit + "detail-crystal.fbx", level3.transform, scene, "Cristal");
                if (crystal != null)
                    Fit(crystal, 0.22f, Quaternion.Euler(0f, 0f, 120f * i + 90f) * new Vector3(1.1f, 0f, 0f) + Vector3.down * 0.15f);
            }

            var settings = new SerializedObject(ring);
            settings.FindProperty("m_Element").enumValueIndex = (int)entry.Element;
            settings.FindProperty("m_Color").colorValue = entry.Color;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Set(ring, "m_Hoop", hoop);
            SetObjects(ring, "m_Spinners", outer, inner);
            SetLevelParts(ring, level2, level3);
            return true;
        }

        // En formes simples (la place est comptée sur la tour) : un seau de bois sur un axe entre deux montants, qui bascule vers
        // l'extérieur ; à côté, une cible tournée vers le joueur (dorée quand le seau est plein). Des cerclages de fer au niveau 2 ;
        // un fanion au niveau 3. Le filet d'huile est caché jusqu'au versement.
        static bool BuildOilBucket(GameObject root)
        {
            var bucket = root.AddComponent<StructureOilBucket>();
            var wood = MakeMaterial("Bois", new Color(0.55f, 0.36f, 0.2f), 0.2f, 0f);
            var darkWood = MakeMaterial("Bois sombre", new Color(0.32f, 0.2f, 0.12f), 0.15f, 0f);
            var metal = MakeMaterial("Metal", new Color(0.6f, 0.62f, 0.66f), 0.6f, 0.8f);
            var oil = MakeMaterial("Huile", new Color(0.22f, 0.15f, 0.04f), 0.9f, 0f);
            var full = MakeMaterial("Cible pleine", new Color(1f, 0.8f, 0.3f), 0.5f, 0f, new Color(1f, 0.8f, 0.3f) * 2.5f);
            var empty = MakeMaterial("Cible vide", new Color(0.35f, 0.35f, 0.35f), 0.2f, 0f);
            var red = MakeMaterial("Fanion", new Color(0.75f, 0.14f, 0.12f), 0.2f, 0f);

            foreach (var x in new[] { -0.32f, 0.32f })
                Box(root.transform, "Montant", darkWood, new Vector3(x, 0.45f, 0f), new Vector3(0.08f, 0.9f, 0.08f));
            var axle = Primitive(PrimitiveType.Cylinder, root.transform, "Axe", metal, new Vector3(0f, 0.8f, 0f), new Vector3(0.05f, 0.36f, 0.05f));
            axle.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            var tilt = new GameObject("Seau").transform;
            tilt.SetParent(root.transform, false);
            tilt.localPosition = new Vector3(0f, 0.8f, 0f);
            Primitive(PrimitiveType.Cylinder, tilt, "Cuve", wood, new Vector3(0f, -0.05f, 0f), new Vector3(0.5f, 0.22f, 0.5f));
            Primitive(PrimitiveType.Cylinder, tilt, "Huile", oil, new Vector3(0f, 0.16f, 0f), new Vector3(0.44f, 0.01f, 0.44f));

            var stream = new GameObject("Filet d'huile").transform;
            stream.SetParent(root.transform, false);
            stream.localPosition = new Vector3(0f, 0.75f, 0.42f);
            Primitive(PrimitiveType.Cylinder, stream, "Huile qui coule", oil, new Vector3(0f, -0.5f, 0f), new Vector3(0.1f, 0.5f, 0.1f));

            // La cible, à côté du seau, face au joueur (derrière l'emplacement : il est tourné vers l'extérieur de la tour).
            Box(root.transform, "Poteau de la cible", darkWood, new Vector3(0.6f, 0.55f, -0.1f), new Vector3(0.06f, 1.1f, 0.06f));
            var target = new GameObject("Cible").transform;
            target.SetParent(root.transform, false);
            target.localPosition = new Vector3(0.6f, 1.2f, -0.14f);
            var face = Primitive(PrimitiveType.Cylinder, target, "Cible", full, Vector3.zero, new Vector3(0.45f, 0.02f, 0.45f));
            face.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var bullseye = Primitive(PrimitiveType.Cylinder, target, "Centre", red, new Vector3(0f, 0f, -0.025f), new Vector3(0.16f, 0.01f, 0.16f));
            bullseye.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var collider = target.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.5f, 0.5f, 0.12f);

            var level2 = Part(tilt.gameObject, "Niveau 2");
            foreach (var y in new[] { -0.2f, 0.08f })
                Primitive(PrimitiveType.Cylinder, level2.transform, "Cerclage", metal, new Vector3(0f, y, 0f), new Vector3(0.52f, 0.02f, 0.52f));

            var level3 = Part(root, "Niveau 3");
            Flag(level3.transform, new Vector3(-0.42f, 0.4f, -0.1f));

            Set(bucket, "m_Target", collider);
            Set(bucket, "m_TargetFace", face.GetComponent<Renderer>());
            Set(bucket, "m_FullMaterial", full);
            Set(bucket, "m_EmptyMaterial", empty);
            Set(bucket, "m_Bucket", tilt);
            Set(bucket, "m_Stream", stream);
            SetLevelParts(bucket, level2, level3);
            return true;
        }

        // La baliste du Tower Defense Kit, plus grosse, sur un socle rond bas (pour ne pas boucher la vue du joueur) ; des
        // plaques de métal au niveau 2 ; un fanion au niveau 3. Une Ballista réglée plus fort : portée, perçage.
        static bool BuildTowerBallista(GameObject root, Scene scene)
        {
            var ballista = root.AddComponent<Ballista>();
            var pedestal = Model(k_Kit + "tower-round-base.fbx", root.transform, scene, "Socle");
            if (pedestal == null)
                return false;
            Fit(pedestal, 1.1f, Vector3.zero);
            var top = BoundsOf(pedestal).max.y;

            var turret = Part(root, "Tourelle");
            turret.transform.position = new Vector3(0f, top, 0f);
            var weapon = Model(k_Kit + "weapon-ballista.fbx", turret.transform, scene, "Baliste");
            if (weapon == null)
                return false;
            Fit(weapon, 1.2f, Vector3.zero);

            var metal = MakeMaterial("Metal", new Color(0.6f, 0.62f, 0.66f), 0.6f, 0.8f);
            var level2 = Part(root, "Niveau 2");
            foreach (var x in new[] { -0.5f, 0.5f })
                Box(level2.transform, "Plaque", metal, new Vector3(x, top * 0.5f, 0f), new Vector3(0.06f, Mathf.Max(0.2f, top * 0.8f), 0.6f));

            var level3 = Part(root, "Niveau 3");
            Flag(level3.transform, new Vector3(-0.45f, top, -0.45f));

            var bounds = BoundsOf(weapon);
            var muzzle = turret.transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.center.y + bounds.extents.y * 0.3f, bounds.max.z));
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(k_PrefabFolder + "Baliste.prefab");
            var model = source != null ? source.GetComponent<Ballista>() : null;
            if (model != null)
            {
                Copy(model, ballista, "m_BoltPrefab");
                Copy(model, ballista, "m_FireClip");
            }
            else
            {
                Debug.LogWarning("Refonte du tower defense, étape 5 : pas de prefab Baliste, la baliste sur la tour n'a pas de carreaux.");
            }

            Set(ballista, "m_Turret", turret.transform);
            SetValues(ballista, "m_MuzzleOffset", muzzle);
            SetValues(ballista, "m_Range", 35f);
            SetValues(ballista, "m_DamagePerLevel", 30f, 45f, 66f);
            SetValues(ballista, "m_IntervalPerLevel", 2.5f, 2.1f, 1.7f);
            SetInts(ballista, "m_PiercePerLevel", 1, 2, 3);
            SetValues(ballista, "m_BoltSpeed", 60f);
            SetValues(ballista, "m_TurnSpeed", 200f);
            SetValues(ballista, "m_MaxPitch", 60f);
            SetLevelParts(ballista, level2, level3);
            return true;
        }

        // Un autel (un rocher et un cristal du Tower Defense Kit) et, au-dessus, le fantôme de Quaternius : l'esprit, qui part
        // chasser. Deux cristaux de plus au niveau 2 ; une couronne de cristaux et une lueur au niveau 3.
        static bool BuildSpirit(GameObject root, Scene scene, Entry entry)
        {
            var spirit = root.AddComponent<StructureSpirit>();
            var rock = Model(k_Kit + "detail-rocks.fbx", root.transform, scene, "Autel");
            if (rock == null)
                return false;
            Fit(rock, 0.9f, Vector3.zero);
            var top = BoundsOf(rock).max.y;
            var crystal = Model(k_Kit + "detail-crystal-large.fbx", root.transform, scene, "Cristal");
            if (crystal != null)
                Fit(crystal, 0.35f, new Vector3(0f, top - 0.05f, 0f));

            var ghostRoot = new GameObject("Esprit").transform;
            ghostRoot.SetParent(root.transform, false);
            ghostRoot.localPosition = new Vector3(0f, 1.8f, 0f);
            var ghost = Model(k_Ghost, ghostRoot, scene, "Fantôme");
            if (ghost == null)
                return false;
            Fit(ghost, 0.8f, new Vector3(0f, -0.45f, 0f));

            var level2 = Part(root, "Niveau 2");
            foreach (var x in new[] { -0.55f, 0.55f })
            {
                var small = Model(k_Kit + "detail-crystal.fbx", level2.transform, scene, "Cristal");
                if (small != null)
                    Fit(small, 0.25f, new Vector3(x, 0f, 0.2f));
            }

            var level3 = Part(root, "Niveau 3");
            for (var i = 0; i < 4; i++)
            {
                var small = Model(k_Kit + "detail-crystal.fbx", level3.transform, scene, "Cristal");
                if (small != null)
                    Fit(small, 0.22f, Quaternion.Euler(0f, 45f + 90f * i, 0f) * new Vector3(0f, 0f, 0.75f));
            }

            var glow = MakeMaterial("Lueur " + entry.File, entry.Color, 0.5f, 0f, entry.Color * 2.5f);
            Primitive(PrimitiveType.Cylinder, level3.transform, "Lueur", glow, new Vector3(0f, 0.01f, 0f), new Vector3(1.6f, 0.01f, 1.6f));

            Set(spirit, "m_Spirit", ghostRoot);
            var settings = new SerializedObject(spirit);
            settings.FindProperty("m_Color").colorValue = entry.Color;
            settings.ApplyModifiedPropertiesWithoutUndo();
            SetLevelParts(spirit, level2, level3);
            return true;
        }

        // Un anneau de ce rayon (m), dans le plan XY du parent.
        static Transform Torus(Transform parent, string name, Material material, float radius)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * radius;
            go.AddComponent<MeshFilter>().sharedMesh = TorusMesh();
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go.transform;
        }

        // Le tore des anneaux (rayon 1, tube de 7 cm, axe Z), créé une fois dans Art/TowerDefense.
        static Mesh TorusMesh()
        {
            var path = k_ArtFolder + "/Anneau.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null)
                return mesh;

            if (!AssetDatabase.IsValidFolder(k_ArtFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "TowerDefense");

            const int segments = 40;
            const int sides = 10;
            const float tube = 0.07f;
            var vertices = new Vector3[segments * sides];
            var normals = new Vector3[segments * sides];
            var triangles = new int[segments * sides * 6];
            for (var i = 0; i < segments; i++)
            {
                var u = i * Mathf.PI * 2f / segments;
                var radial = new Vector3(Mathf.Cos(u), Mathf.Sin(u), 0f);
                for (var j = 0; j < sides; j++)
                {
                    var v = j * Mathf.PI * 2f / sides;
                    var normal = radial * Mathf.Cos(v) + Vector3.forward * Mathf.Sin(v);
                    var index = i * sides + j;
                    vertices[index] = radial + normal * tube;
                    normals[index] = normal;

                    var next = (i + 1) % segments * sides;
                    var a = i * sides + j;
                    var b = next + j;
                    var c = next + (j + 1) % sides;
                    var d = i * sides + (j + 1) % sides;
                    // Sens d'Unity : la face avant a pour normale Cross(p1 - p0, p2 - p0), ici vers l'extérieur du tube.
                    var t = index * 6;
                    triangles[t] = a;
                    triangles[t + 1] = b;
                    triangles[t + 2] = c;
                    triangles[t + 3] = a;
                    triangles[t + 4] = c;
                    triangles[t + 5] = d;
                }
            }

            mesh = new Mesh { name = "Anneau", vertices = vertices, normals = normals, triangles = triangles };
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // Un filet roulé : un cylindre couché.
        static void NetRoll(Transform parent, Material material, Vector3 place)
        {
            var roll = Primitive(PrimitiveType.Cylinder, parent, "Filet roulé", material, place, new Vector3(0.22f, 0.2f, 0.22f));
            roll.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        // Un matériau transparent (URP Lit, mélange alpha), créé une fois dans Art/TowerDefense.
        static Material MakeTransparentMaterial(string name, Color color, float smoothness)
        {
            if (!AssetDatabase.IsValidFolder(k_ArtFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "TowerDefense");

            var path = k_ArtFolder + "/" + name.Replace(' ', '_') + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // Un mât de bois et un fanion rouge, posés à cette place (repère du parent, en mètres).
        static void Flag(Transform parent, Vector3 place)
        {
            var red = MakeMaterial("Fanion", new Color(0.75f, 0.14f, 0.12f), 0.2f, 0f);
            var darkWood = MakeMaterial("Bois sombre", new Color(0.32f, 0.2f, 0.12f), 0.15f, 0f);
            Primitive(PrimitiveType.Cylinder, parent, "Mât", darkWood, place + new Vector3(0f, 0.55f, 0f), new Vector3(0.05f, 0.55f, 0.05f));
            Box(parent, "Fanion", red, place + new Vector3(0.22f, 0.92f, 0f), new Vector3(0.42f, 0.26f, 0.02f));
        }

        static void Crystals(Transform parent, Scene scene, float scale, float y)
        {
            var crystals = Model(k_Kit + "tower-round-crystals.fbx", parent, scene, "Cristaux");
            if (crystals == null)
                return;
            crystals.transform.localScale *= scale;
            Ground(crystals, y);
        }

        // ---------------------------------------------------------------- outils

        static GameObject Model(string path, Transform parent, Scene scene, string name)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogWarning("Refonte du tower defense, étape 5 : modèle introuvable, " + path);
                return null;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
            go.name = name;
            go.transform.SetParent(parent, false);
            // Les bâtiments n'ont pas de collider (comme la baliste) : on vise leur emplacement.
            foreach (var collider in go.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
            return go;
        }

        static GameObject Part(GameObject parent, string name)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent.transform, false);
            return part;
        }

        // Met le modèle à la largeur voulue (sa plus grande dimension au sol) et le pose sur le sol, à cette place. Renvoie l'échelle.
        static float Fit(GameObject go, float width, Vector3 place)
        {
            var bounds = BoundsOf(go);
            var size = Mathf.Max(bounds.size.x, bounds.size.z);
            var scale = size > 1e-4f ? width / size : 1f;
            go.transform.localScale *= scale;
            var parent = go.transform.parent;
            var target = parent != null ? parent.TransformPoint(place) : place;
            bounds = BoundsOf(go);
            go.transform.position += new Vector3(target.x - bounds.center.x, target.y - bounds.min.y, target.z - bounds.center.z);
            return scale;
        }

        // Pose le bas du modèle à cette hauteur (monde).
        static void Ground(GameObject go, float y) => go.transform.position += Vector3.up * (y - BoundsOf(go).min.y);

        static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(go.transform.position, Vector3.zero);

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        static GameObject Box(Transform parent, string name, Material material, Vector3 position, Vector3 size, bool localToParentScale = false) =>
            Primitive(PrimitiveType.Cube, parent, name, material, position, size, localToParentScale);

        static GameObject Primitive(PrimitiveType type, Transform parent, string name, Material material, Vector3 position, Vector3 size,
                                    bool localToParentScale = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            SceneManager.MoveGameObjectToScene(go, parent.gameObject.scene);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            if (!localToParentScale)
            {
                // Taille en mètres, quelle que soit l'échelle du parent.
                var parentScale = parent.lossyScale;
                go.transform.localScale = new Vector3(size.x / parentScale.x, size.y / parentScale.y, size.z / parentScale.z);
            }

            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        static Material MakeMaterial(string name, Color color, float smoothness, float metallic, Color emission = default)
        {
            if (!AssetDatabase.IsValidFolder(k_ArtFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "TowerDefense");

            var file = name.Replace(' ', '_');
            var path = k_ArtFolder + "/" + file + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = file };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            if (emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void Set(Object component, string property, Object value)
        {
            var settings = new SerializedObject(component);
            var field = settings.FindProperty(property);
            if (field == null)
            {
                Debug.LogWarning($"Refonte du tower defense, étape 5 : pas de réglage {property} sur {component}.");
                return;
            }

            field.objectReferenceValue = value;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        // Un réglage nombre, ou un tableau de nombres (un par niveau).
        static void SetValues(Object component, string property, params float[] values)
        {
            var settings = new SerializedObject(component);
            var field = settings.FindProperty(property);
            if (field == null)
            {
                Debug.LogWarning($"Refonte du tower defense, étape 5 : pas de réglage {property} sur {component}.");
                return;
            }

            if (field.isArray)
            {
                field.arraySize = values.Length;
                for (var i = 0; i < values.Length; i++)
                    field.GetArrayElementAtIndex(i).floatValue = values[i];
            }
            else
            {
                field.floatValue = values[0];
            }

            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetValues(Object component, string property, Vector3 value)
        {
            var settings = new SerializedObject(component);
            var field = settings.FindProperty(property);
            if (field == null)
                return;
            field.vector3Value = value;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetInts(Object component, string property, params int[] values)
        {
            var settings = new SerializedObject(component);
            var field = settings.FindProperty(property);
            if (field == null || !field.isArray)
                return;
            field.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                field.GetArrayElementAtIndex(i).intValue = values[i];
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetObjects(Object component, string property, params Object[] values)
        {
            var settings = new SerializedObject(component);
            var field = settings.FindProperty(property);
            if (field == null || !field.isArray)
                return;
            field.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                field.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        // Recopie une référence (carreau, son, matériau) d'un bâtiment existant.
        static void Copy(Object from, Object to, string property)
        {
            var source = new SerializedObject(from).FindProperty(property);
            if (source != null)
                Set(to, property, source.objectReferenceValue);
        }

        static void SetLevelParts(Structure structure, GameObject level2, GameObject level3)
        {
            var settings = new SerializedObject(structure);
            var parts = settings.FindProperty("m_LevelParts");
            parts.arraySize = 2;
            parts.GetArrayElementAtIndex(0).objectReferenceValue = level2;
            parts.GetArrayElementAtIndex(1).objectReferenceValue = level3;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
