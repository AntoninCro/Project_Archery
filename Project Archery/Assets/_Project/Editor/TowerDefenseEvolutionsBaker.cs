using Archery.Building;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archery.EditorTools
{
    /// <summary>
    /// Les évolutions des tours (deuxième brainstorm du tower defense, 29 octobre ; <c>Docs/Refonte_TowerDefense.md</c>,
    /// partie 12.6) : chaque tour passe à 5 niveaux, chacun une capacité, quand ses évolutions sont codées. Pour chaque tour, une
    /// fois (tant que son prefab n'a que les pièces des niveaux 2 et 3) : son prefab reçoit les pièces des niveaux 3 à 5
    /// (des modèles provisoires, exagérés : l'effort d'art viendra ensuite), et son réglage (<c>Data/Structures</c>) passe à 5
    /// niveaux, avec leurs textes (proposés ; les niveaux 4 et 5 coûtent 2 plans). Les valeurs déjà retouchées ne bougent pas.
    /// <list type="bullet">
    /// <item><b>Tremplin</b> (29 octobre) : la catapulte au niveau 3 (un bras qui bascule), trois godets au niveau 4, la rampe de
    /// lancement au niveau 5 (des rails de 4 m qui luisent, des pylônes, des rayures jaunes et noires).</item>
    /// <item><b>Les 9 autres</b> (29 octobre au soir) : barricade, piques, baliste, mortier, givre, lance-filet, aimant, bulle,
    /// phare ; pour celles qui n'avaient pas de pièces de niveau (piques, givre), des pièces « Niveau 2 » et « Niveau 3 » sont
    /// créées. Leurs tableaux par niveau passent à 5 valeurs seulement s'ils sont encore ceux d'avant.</item>
    /// </list>
    /// </summary>
    [InitializeOnLoad]
    static class TowerDefenseEvolutionsBaker
    {
        const string k_PrefabFolder = "Assets/_Project/Prefabs/Structures/";
        const string k_DataFolder = "Assets/_Project/Data/Structures/";
        const string k_ArtFolder = "Assets/_Project/Art/TowerDefense";
        const string k_Kit = "Assets/kenney_tower-defense-kit/Models/FBX format/";

        static TowerDefenseEvolutionsBaker()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            Evolve("Tremplin", BuildTrampoline, k_TrampolineLevels);
            Retext("Tremplin", 5, k_TrampolineRampFirst, k_TrampolineLevels[3]);
            FixTrampolineOverlaps();

            // Les 9 autres tours (29 octobre au soir, « oui, enchaîne », le joueur, après la rampe validée).
            Evolve("Barricade", BuildBarricade, k_BarricadeLevels);
            Evolve("Pieges", BuildSpikes, k_SpikeLevels);
            Evolve("Baliste", BuildBallista, k_BallistaLevels);
            Evolve("Mortier", BuildMortar, k_MortarLevels);
            Evolve("Givre", BuildFrost, k_FrostLevels);
            Evolve("Filet", BuildNet, k_NetLevels);
            Evolve("Aimant", BuildMagnet, k_MagnetLevels);
            Evolve("Bulle", BuildBubble, k_BubbleLevels);
            Evolve("Phare", BuildLighthouse, k_LighthouseLevels);
            Retouch();
            Retouch2();
        }

        // Deux pièces du tremplin se chevauchaient et scintillaient (le joueur, 29 octobre) : dès le niveau 2, la flèche de bois et
        // la plaque de métal avaient le dessus à la même hauteur ; au niveau 5, les rayures et le liseré vert du niveau 3 avaient
        // une face commune. La flèche dépasse maintenant de la plaque, et les rayures englobent le liseré. Une fois (tant que la
        // flèche a sa taille du 28 octobre).
        static void FixTrampolineOverlaps()
        {
            var path = k_PrefabFolder + "Tremplin.prefab";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var arrow = asset != null ? asset.transform.Find("Planche/Flèche") : null;
            if (arrow == null || !Mathf.Approximately(arrow.localScale.y, 0.3f))
                return;

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                // Dans le repère de la planche (2,1 × 0,1 × 2,1) : 4,5 cm de haut au lieu de 3, le dessus 1,5 cm au-dessus de la plaque.
                arrow = root.transform.Find("Planche/Flèche");
                arrow.localPosition = new Vector3(arrow.localPosition.x, 0.675f, arrow.localPosition.z);
                arrow.localScale = new Vector3(arrow.localScale.x, 0.45f, arrow.localScale.z);

                // Les huit rayures, un peu plus larges et plus profondes : de -1,24 à 1,24 m en X, de 1,13 à 1,29 m en Z.
                var level5 = root.transform.Find("Niveau 5");
                var index = 0;
                if (level5 != null)
                {
                    foreach (Transform child in level5)
                    {
                        if (child.name != "Rayure")
                            continue;
                        child.localPosition = new Vector3(-1.085f + index * 0.31f, child.localPosition.y, 1.21f);
                        child.localScale = new Vector3(0.31f, child.localScale.y, 0.16f);
                        index++;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"Évolutions des tours : le tremplin ne scintille plus (la flèche au-dessus de la plaque, {index} rayures " +
                          $"autour du liseré ; {path}).", asset);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Un texte revu après un essai au casque : seulement s'il est encore celui d'avant (le joueur a pu le retoucher).
        static void Retext(string file, int level, string old, string text)
        {
            var definition = AssetDatabase.LoadAssetAtPath<StructureDefinition>(k_DataFolder + file + ".asset");
            if (definition == null || definition.levels == null || definition.levels.Length < level ||
                definition.levels[level - 1].description != old)
                return;

            var settings = new SerializedObject(definition);
            settings.FindProperty("levels").GetArrayElementAtIndex(level - 1).FindPropertyRelative("description").stringValue = text;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(definition);
            Debug.Log($"Évolutions des tours : texte du niveau {level} de {file} revu ({k_DataFolder}{file}.asset).", definition);
        }

        // Une tour : son prefab (s'il n'a pas encore de pièce « Niveau 5 »), puis son réglage (s'il n'a encore que trois niveaux).
        static void Evolve(string file, System.Func<GameObject, Structure, bool> build, string[] descriptions)
        {
            var path = k_PrefabFolder + file + ".prefab";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var structure = asset != null ? asset.GetComponent<Structure>() : null;
            var changed = false;
            if (structure != null && asset.transform.Find("Niveau 5") == null)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    if (build(root, root.GetComponent<Structure>()))
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        changed = true;
                    }
                }
                catch (System.Exception exception)
                {
                    // Une tour qui échoue n'empêche pas les suivantes ; son prefab reste tel quel (rien n'est enregistré).
                    Debug.LogError($"Évolutions des tours : {file} n'a pas pu passer à 5 niveaux ({exception.Message}).", asset);
                    Debug.LogException(exception);
                    return;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            var definition = AssetDatabase.LoadAssetAtPath<StructureDefinition>(k_DataFolder + file + ".asset");
            if (definition != null && definition.levels != null && definition.levels.Length == 3)
            {
                var settings = new SerializedObject(definition);
                var levels = settings.FindProperty("levels");
                levels.arraySize = 5;
                for (var i = 1; i < 5 && i - 1 < descriptions.Length; i++)
                    levels.GetArrayElementAtIndex(i).FindPropertyRelative("description").stringValue = descriptions[i - 1];
                settings.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(definition);
                changed = true;
            }

            if (changed)
                Debug.Log($"Évolutions des tours : {file} passe à 5 niveaux (prefab {path} et réglage {k_DataFolder}{file}.asset ; " +
                          "les niveaux 4 et 5 coûtent 2 plans).", asset);
        }

        // ---------------------------------------------------------------- le tremplin

        // Les textes des niveaux 2 à 5 (proposés ; le niveau 1 garde le sien).
        static readonly string[] k_TrampolineLevels =
        {
            "Projette plus haut : l'ennemi reste plus longtemps en l'air (le tir au vol rapporte plus).",
            "Catapulte : un bras attrape l'ennemi et le lance sur le groupe le plus serré ; il devient un boulet qui blesse et renverse ceux qu'il percute.",
            "Trois godets : trois ennemis lancés d'un coup.",
            "Rampe de lancement : les ennemis partent à 370 m dans le ciel et retombent 5 s plus tard, en météores, sur le groupe le plus serré : l'atterrissage les tue et blesse ceux qu'ils écrasent. Les boss y perdent 10 % de leurs PV.",
        };

        // Le texte du niveau 5 avant l'essai au casque du 29 octobre (dix fois plus haut, et il retombe sur un groupe).
        const string k_TrampolineRampFirst =
            "Rampe de lancement : les ennemis partent dans le ciel à une vitesse incroyable et retombent 5 à 6 s plus tard : l'atterrissage les tue. Les boss y perdent 10 % de leurs PV.";

        static bool BuildTrampoline(GameObject root, Structure structure)
        {
            var parts = new SerializedObject(structure).FindProperty("m_LevelParts");
            var level2 = parts.arraySize == 2 ? parts.GetArrayElementAtIndex(0).objectReferenceValue as GameObject : null;
            var level3 = parts.arraySize == 2 ? parts.GetArrayElementAtIndex(1).objectReferenceValue as GameObject : null;
            if (!(structure is StructureTrampoline) || level2 == null || level3 == null)
                return false;

            var wood = MakeMaterial("Bois", new Color(0.55f, 0.36f, 0.2f), 0.2f, 0f);
            var darkWood = MakeMaterial("Bois sombre", new Color(0.32f, 0.2f, 0.12f), 0.15f, 0f);
            var metal = MakeMaterial("Metal", new Color(0.6f, 0.62f, 0.66f), 0.6f, 0.8f);
            var yellow = MakeMaterial("Rayures jaunes", new Color(1f, 0.78f, 0.1f), 0.35f, 0f);
            var black = MakeMaterial("Noir mat", new Color(0.08f, 0.08f, 0.09f), 0.2f, 0f);
            var fire = MakeMaterial("Lueur orange", new Color(1f, 0.55f, 0.15f), 0.5f, 0f, new Color(1f, 0.5f, 0.1f) * 3f);

            // Niveau 3, la catapulte : le modèle du kit derrière la planche, et un bras de bois qui bascule (m_Arm), son godet
            // au-dessus de la planche.
            var catapult = Model(k_Kit + "weapon-catapult.fbx", level3.transform, "Catapulte");
            if (catapult != null)
            {
                Fit(catapult, 1.4f, new Vector3(0f, 0f, -1.55f));
                catapult.transform.localRotation = Quaternion.identity;
            }

            var arm = new GameObject("Bras").transform;
            arm.SetParent(level3.transform, false);
            arm.localPosition = new Vector3(0f, 0.75f, -1.1f);
            Box(arm, "Poutre", wood, new Vector3(0f, 0f, 0.8f), new Vector3(0.16f, 0.16f, 1.7f));
            Primitive(PrimitiveType.Cylinder, arm, "Godet", darkWood, new Vector3(0f, 0.08f, 1.55f), new Vector3(0.6f, 0.08f, 0.6f));
            Primitive(PrimitiveType.Cylinder, level3.transform, "Axe", metal, new Vector3(0f, 0.75f, -1.1f), new Vector3(0.18f, 0.5f, 0.18f))
                .transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            // Niveau 4, trois godets : deux de plus au bout du bras, sur des traverses.
            var level4 = Part(arm.gameObject, "Niveau 4");
            Box(level4.transform, "Traverse", wood, new Vector3(0f, 0f, 1.35f), new Vector3(1.3f, 0.12f, 0.14f));
            foreach (var side in new[] { -1f, 1f })
                Primitive(PrimitiveType.Cylinder, level4.transform, "Godet", darkWood, new Vector3(0.6f * side, 0.08f, 1.35f), new Vector3(0.5f, 0.08f, 0.5f));

            // Niveau 5, la rampe de lancement : deux rails inclinés de 4 m qui luisent, deux pylônes à feu, un ressort géant sous
            // la planche, et des rayures jaunes et noires autour du cadre. On la voit de loin.
            var level5 = Part(root, "Niveau 5");
            foreach (var side in new[] { -1f, 1f })
            {
                var rail = Box(level5.transform, "Rail", metal, Vector3.zero, new Vector3(0.14f, 0.14f, 4f));
                rail.transform.localPosition = new Vector3(1.05f * side, 1.9f, -0.35f);
                rail.transform.localRotation = Quaternion.Euler(-70f, 0f, 0f);
                var glow = Box(level5.transform, "Rail lumineux", fire, Vector3.zero, new Vector3(0.06f, 0.06f, 3.8f));
                glow.transform.localPosition = new Vector3(0.96f * side, 1.9f, -0.32f);
                glow.transform.localRotation = Quaternion.Euler(-70f, 0f, 0f);

                Primitive(PrimitiveType.Cylinder, level5.transform, "Pylône", black, new Vector3(1.15f * side, 1.6f, -1.15f), new Vector3(0.22f, 1.6f, 0.22f));
                Primitive(PrimitiveType.Sphere, level5.transform, "Feu du pylône", fire, new Vector3(1.15f * side, 3.35f, -1.15f), new Vector3(0.45f, 0.45f, 0.45f));
            }

            for (var i = 0; i < 4; i++)
                Primitive(PrimitiveType.Cylinder, level5.transform, "Spire", metal, new Vector3(0f, 0.12f + i * 0.07f, 0f), new Vector3(1.5f - i * 0.2f, 0.03f, 1.5f - i * 0.2f));

            for (var i = 0; i < 8; i++)
            {
                var stripe = Box(level5.transform, "Rayure", i % 2 == 0 ? yellow : black, Vector3.zero, new Vector3(0.3f, 0.14f, 0.12f));
                var x = -1.05f + i * 0.3f;
                stripe.transform.localPosition = new Vector3(x, 0.14f, 1.2f);
            }

            var settings = new SerializedObject(structure);
            var levelParts = settings.FindProperty("m_LevelParts");
            levelParts.arraySize = 4;
            levelParts.GetArrayElementAtIndex(0).objectReferenceValue = level2;
            levelParts.GetArrayElementAtIndex(1).objectReferenceValue = level3;
            levelParts.GetArrayElementAtIndex(2).objectReferenceValue = level4;
            levelParts.GetArrayElementAtIndex(3).objectReferenceValue = level5;
            settings.FindProperty("m_Arm").objectReferenceValue = arm;
            settings.FindProperty("m_TrailMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Materials/Arrow_Trail.mat");
            settings.FindProperty("m_LaunchClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/650564__h2p34__lightsaber-whoosh.wav");
            settings.FindProperty("m_ImpactClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Placeholder/explosion.wav");
            settings.FindProperty("m_ImpactEffect").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Effects/Death Smoke.prefab");

            // Les valeurs par niveau, seulement si elles sont encore celles du 28 octobre (3 niveaux).
            SetIfStill(settings.FindProperty("m_UpSpeedPerLevel"), new[] { 8f, 9.5f, 11f }, new[] { 8f, 11f });
            SetIfStill(settings.FindProperty("m_CooldownPerLevel"), new[] { 3f, 2.2f, 1.5f }, new[] { 3f, 2.2f, 3.5f, 3f, 2f });
            settings.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // ---------------------------------------------------------------- les 9 autres tours (29 octobre au soir)

        const string k_Audio = "Assets/_Project/Audio/Placeholder/";
        const string k_WatchTower = "Assets/construction/Watch Tower by Quaternius - cMxuj2gt7D/WatchTower_SecondAge_Level3.fbx";
        const string k_Landmine = "Assets/construction/Landmine by Quaternius - PtqkseZo9O/Landmine.fbx";
        const string k_Barrel = "Assets/construction/Ultimate Fantasy RTS by Quaternius/FBX/Barrel.fbx";

        // Les textes des niveaux 2 à 5 (proposés ; le niveau 1 garde le sien).
        static readonly string[] k_BarricadeLevels =
        {
            "Rempart : PV ×2, et des pointes blessent ceux qui la frappent.",
            "Pendule : un portique au-dessus d'elle, d'où un boulet hérissé se balance en travers de la voie et fauche ceux qui l'assiègent.",
            "Herse : toutes les 8 s, elle s'abat devant la barricade et étourdit ceux qui frappent. Ta flèche qui traverse la barricade devient perforante.",
            "Forteresse : un mur géant, deux tours de guet qui criblent la voie de carreaux ; chaque coup reçu lâche une onde qui renvoie les assaillants 10 m en arrière, en vol.",
        };

        static readonly string[] k_SpikeLevels =
        {
            "Barbelées : les blessés saignent encore 3 s.",
            "Champ de pieux : deux fois plus large.",
            "Une mine cachée explose au premier passage et se réarme toutes les 20 s ; ta flèche la fait sauter.",
            "Mâchoire de la terre : toutes les 12 s, la voie s'ouvre et des pieux de 4 m jaillissent sur 10 m : les petits sont tués net, les autres embrochés en l'air.",
        };

        static readonly string[] k_BallistaLevels =
        {
            "Carreaux perforants : chacun traverse 3 ennemis.",
            "Scorpion : double arc, deux cibles à la fois.",
            "Harpon : le carreau cloue sa cible au sol 1 s, et fait tomber les volants.",
            "Baliste titan : toutes les 6 s, un pieu géant traverse toute la voie, embroche les ennemis et les emporte avec lui.",
        };

        static readonly string[] k_MortarLevels =
        {
            "Obus à fragmentation : quatre éclats retombent autour de chaque explosion.",
            "Mortier de siège : deux tubes, plus de portée.",
            "Obus de goudron : une flaque qui ralentit ; ta flèche enflammée l'embrase.",
            "Bombardement : toutes les 15 s, 12 obus pleuvent tout le long de la voie, le sol reste en feu et les ennemis volent dans tous les sens.",
        };

        static readonly string[] k_FrostLevels =
        {
            "Le givre reste 2 s sur ceux qui sortent de la zone.",
            "Pic de glace : toutes les 4 s, elle gèle net un ennemi ; ta flèche brise la glace et lui coûte 30 % de ses PV.",
            "Verglas : le sol gèle, les ennemis glissent et tombent.",
            "Cœur de l'hiver : toutes les 15 s, une tempête change tous les ennemis de la zone en statues de glace pendant 3 s ; le moindre coup les fait voler en éclats.",
        };

        static readonly string[] k_NetLevels =
        {
            "Le volant tombé reste 5 s au sol.",
            "Barillet : deux filets à la fois.",
            "Filet électrique : il étourdit et blesse, et prend aussi les ennemis au sol.",
            "Toile géante : toutes les 15 s, un filet immense couvre le ciel au-dessus de la voie pendant 6 s ; tous les volants qui le touchent s'écrasent au sol.",
        };

        static readonly string[] k_MagnetLevels =
        {
            "Elle attire aussi les volants vers le sol.",
            "Électro-aimant : elle attire en continu dans sa zone.",
            "Tes flèches qui passent dans sa zone sont courbées vers les ennemis attirés.",
            "Singularité : toutes les 15 s, un trou noir aspire tous les ennemis à 15 m en une boule qui tournoie 3 s, puis les recrache en l'air.",
        };

        static readonly string[] k_BubbleLevels =
        {
            "Deux bulles à la fois.",
            "Bulle géante : chacune enferme un petit groupe.",
            "Ta flèche qui crève une bulle rebondit vers la suivante, et la crève aussi.",
            "Orbe céleste : toutes les 15 s, une bulle immense emporte tout un groupe à 30 m ; crevée par ta flèche (ou au bout de 6 s), ils s'écrasent et meurent.",
        };

        static readonly string[] k_LighthouseLevels =
        {
            "Cadence : les bâtiments de son rayon tirent 15 % plus vite (en plus des 20 % de dégâts).",
            "Grand phare : rayon, renforts et faisceau plus grands.",
            "Ta flèche qui traverse le faisceau s'enflamme : sa cible brûle.",
            "Phare du jugement : toutes les 10 s, un rayon géant tombe du ciel sur l'ennemi le plus fort, et tous les bâtiments du rayon tirent deux fois plus vite pendant 5 s.",
        };

        // Barricade : le portique et le boulet du pendule devant le mur (niveau 3), la herse et ses glissières (niveau 4), un mur
        // de pierre géant crénelé et deux tours de guet (niveau 5). Tout reste dans sa largeur de 4 m (le joueur l'a voulue étroite).
        static bool BuildBarricade(GameObject root, Structure structure)
        {
            if (!(structure is StructureBarricade))
                return false;

            GetParts(root, structure, out var level2, out var level3);
            var wood = MakeMaterial("Bois", new Color(0.55f, 0.36f, 0.2f), 0.2f, 0f);
            var iron = MakeMaterial("Fonte", new Color(0.2f, 0.2f, 0.22f), 0.4f, 0.7f);
            var stone = MakeMaterial("Pierre", new Color(0.55f, 0.53f, 0.5f), 0.1f, 0f);
            var banner = MakeMaterial("Fanion", new Color(0.75f, 0.12f, 0.1f), 0.2f, 0f);

            // Niveau 3 : deux poteaux derrière le plan du boulet, une poutre, une potence, et le balancier (chaîne et boulet hérissé).
            var pendulum = Anchor(level3, root, "Pendule");
            foreach (var side in new[] { -1f, 1f })
                Box(pendulum.transform, "Poteau", wood, new Vector3(1.95f * side, 2.1f, 0.9f), new Vector3(0.22f, 4.2f, 0.22f));
            Box(pendulum.transform, "Poutre", wood, new Vector3(0f, 4.25f, 0.9f), new Vector3(4.1f, 0.24f, 0.24f));
            Box(pendulum.transform, "Potence", wood, new Vector3(0f, 4.25f, 1.25f), new Vector3(0.2f, 0.2f, 0.7f));
            var arm = new GameObject("Balancier").transform;
            arm.SetParent(pendulum.transform, false);
            arm.localPosition = new Vector3(0f, 4.15f, 1.6f);
            Primitive(PrimitiveType.Cylinder, arm, "Chaîne", iron, new Vector3(0f, -1.2f, 0f), new Vector3(0.07f, 1.2f, 0.07f));
            var ball = Primitive(PrimitiveType.Sphere, arm, "Boulet", iron, new Vector3(0f, -2.5f, 0f), new Vector3(0.9f, 0.9f, 0.9f)).transform;
            foreach (var direction in new[] { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back })
            {
                var spike = Box(arm, "Pointe", iron, new Vector3(0f, -2.5f, 0f) + direction * 0.5f, new Vector3(0.14f, 0.14f, 0.14f));
                spike.transform.localRotation = Quaternion.LookRotation(direction == Vector3.up || direction == Vector3.down ? Vector3.forward : direction) *
                                                Quaternion.Euler(45f, 45f, 0f);
            }

            // Niveau 4 : la herse (barreaux de fonte), levée au-dessus du mur côté forêt ; elle tombe de 1,6 m.
            var level4 = Part(root, "Niveau 4");
            var portcullis = new GameObject("Herse").transform;
            portcullis.SetParent(level4.transform, false);
            portcullis.localPosition = new Vector3(0f, 2.5f, 0.55f);
            for (var i = 0; i < 9; i++)
                Box(portcullis, "Barreau", iron, new Vector3(-1.8f + i * 0.45f, 0f, 0f), new Vector3(0.08f, 1.8f, 0.08f));
            foreach (var y in new[] { -0.5f, 0.5f })
                Box(portcullis, "Traverse", iron, new Vector3(0f, y, 0f), new Vector3(3.8f, 0.08f, 0.08f));
            foreach (var side in new[] { -1f, 1f })
                Box(level4.transform, "Glissière", wood, new Vector3(2f * side, 1.75f, 0.55f), new Vector3(0.14f, 3.5f, 0.18f));

            // Niveau 5 : la forteresse, un mur de pierre de 3 m crénelé, deux tours de guet du kit (ou des tours de pierre) avec leurs
            // fanions ; les carreaux partent de leur sommet.
            var level5 = Part(root, "Niveau 5");
            Box(level5.transform, "Mur géant", stone, new Vector3(0f, 1.6f, -0.35f), new Vector3(4.1f, 3.2f, 0.9f));
            for (var i = 0; i < 5; i++)
                Box(level5.transform, "Créneau", stone, new Vector3(-1.7f + i * 0.85f, 3.45f, -0.35f), new Vector3(0.5f, 0.5f, 0.9f));
            var muzzles = new Transform[2];
            for (var s = 0; s < 2; s++)
            {
                var side = s == 0 ? -1f : 1f;
                var tower = Model(k_WatchTower, level5.transform, "Tour de guet");
                if (tower != null)
                    Fit(tower, 1.3f, new Vector3(1.4f * side, 0f, -1.2f));
                else
                    Primitive(PrimitiveType.Cylinder, level5.transform, "Tour de guet", stone, new Vector3(1.4f * side, 2.6f, -1.2f), new Vector3(1.1f, 2.6f, 1.1f));
                Box(level5.transform, "Mât", wood, new Vector3(1.4f * side, 5.9f, -1.2f), new Vector3(0.06f, 1.4f, 0.06f));
                Box(level5.transform, "Bannière", banner, new Vector3(1.4f * side + 0.3f * side, 6.2f, -1.2f), new Vector3(0.55f, 0.4f, 0.03f));
                var muzzle = new GameObject("Archère").transform;
                muzzle.SetParent(level5.transform, false);
                muzzle.localPosition = new Vector3(1.4f * side, 4.6f, -0.9f);
                muzzles[s] = muzzle;
            }

            SetParts(structure, level2, level3, level4, level5);
            var settings = new SerializedObject(structure);
            settings.FindProperty("m_PendulumArm").objectReferenceValue = arm;
            settings.FindProperty("m_PendulumBall").objectReferenceValue = ball;
            settings.FindProperty("m_Portcullis").objectReferenceValue = portcullis;
            var towers = settings.FindProperty("m_WatchTowers");
            towers.arraySize = muzzles.Length;
            for (var i = 0; i < muzzles.Length; i++)
                towers.GetArrayElementAtIndex(i).objectReferenceValue = muzzles[i];
            settings.FindProperty("m_BoltPrefab").objectReferenceValue = Bolt();
            settings.FindProperty("m_PendulumClip").objectReferenceValue = Clip("impact_wood.wav");
            settings.FindProperty("m_PortcullisClip").objectReferenceValue = Clip("projectile_impact.wav");
            settings.FindProperty("m_WaveClip").objectReferenceValue = Clip("explosion.wav");
            settings.FindProperty("m_WaveEffect").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Effects/Death Smoke.prefab");
            SetIfStill(settings.FindProperty("m_HealthPerLevel"), new[] { 400f, 700f, 1000f }, new[] { 400f, 800f, 1000f, 1300f, 2500f });
            var thorns = settings.FindProperty("m_ThornsFromLevel");
            if (thorns.intValue == 3)
                thorns.intValue = 2;
            settings.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // Piques : des barbelés autour du plateau (niveau 2), deux plateaux de plus sur les côtés (niveau 3, deux fois plus large),
        // la mine au centre (niveau 4), quatre crocs de pierre et des runes rouges (niveau 5, la mâchoire).
        static bool BuildSpikes(GameObject root, Structure structure)
        {
            if (!(structure is SpikeTrap))
                return false;

            GetParts(root, structure, out var level2, out var level3);
            var iron = MakeMaterial("Fonte", new Color(0.2f, 0.2f, 0.22f), 0.4f, 0.7f);
            var darkWood = MakeMaterial("Bois sombre", new Color(0.32f, 0.2f, 0.12f), 0.15f, 0f);
            var stone = MakeMaterial("Pierre", new Color(0.55f, 0.53f, 0.5f), 0.1f, 0f);
            var red = MakeMaterial("Lueur rouge", new Color(1f, 0.2f, 0.1f), 0.5f, 0f, new Color(1f, 0.15f, 0.05f) * 3f);

            // Niveau 2 : quatre piquets et des fils de fer barbelés à 25 cm.
            var barbed = Anchor(level2, root, "Barbelés");
            var field = Anchor(level3, root, "Champ de pieux");
            foreach (var x in new[] { -1.35f, 1.35f })
            {
                foreach (var z in new[] { -1.35f, 1.35f })
                    Box(barbed.transform, "Piquet", darkWood, new Vector3(x, 0.2f, z), new Vector3(0.08f, 0.4f, 0.08f));
            }

            foreach (var side in new[] { -1f, 1f })
            {
                Box(barbed.transform, "Barbelé", iron, new Vector3(1.35f * side, 0.3f, 0f), new Vector3(0.03f, 0.03f, 2.7f));
                Box(barbed.transform, "Barbelé", iron, new Vector3(0f, 0.3f, 1.35f * side), new Vector3(2.7f, 0.03f, 0.03f));
            }

            // Niveau 3 : deux plateaux de plus (les piques générées couvrent alors 5,2 m de large).
            foreach (var side in new[] { -1f, 1f })
                Box(field.transform, "Plateau", darkWood, new Vector3(1.95f * side, 0.04f, 0f), new Vector3(1.3f, 0.08f, 2.6f));

            // Niveau 4 : la mine, à demi enterrée au centre.
            var level4 = Part(root, "Niveau 4");
            var mine = Model(k_Landmine, level4.transform, "Mine");
            if (mine != null)
                Fit(mine, 0.55f, new Vector3(0f, 0.02f, 0f));

            // Niveau 5 : quatre grands crocs de pierre aux coins, penchés vers la voie, et des runes rouges.
            var level5 = Part(root, "Niveau 5");
            foreach (var x in new[] { -2.4f, 2.4f })
            {
                foreach (var z in new[] { -1.15f, 1.15f })
                {
                    var fang = Box(level5.transform, "Croc", stone, new Vector3(x, 0.8f, z), new Vector3(0.35f, 1.7f, 0.35f));
                    fang.transform.localRotation = Quaternion.Euler(z > 0f ? -15f : 15f, 45f, x > 0f ? 15f : -15f);
                }
            }

            for (var i = 0; i < 3; i++)
                Box(level5.transform, "Rune", red, new Vector3(-1.6f + i * 1.6f, 0.13f, 0f), new Vector3(0.5f, 0.02f, 0.5f)).transform.localRotation =
                    Quaternion.Euler(0f, 45f, 0f);

            SetParts(structure, level2, level3, level4, level5);
            var settings = new SerializedObject(structure);
            settings.FindProperty("m_Mine").objectReferenceValue = mine != null ? mine.transform : null;
            settings.FindProperty("m_JawClip").objectReferenceValue = Clip("barricade_break.wav");
            SetIfStill(settings.FindProperty("m_DamagePerLevel"), new[] { 12f, 18f, 27f }, new[] { 12f, 18f, 27f, 30f, 34f });
            settings.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // Baliste : un deuxième arc sur la tourelle (niveau 3), un harpon et sa corde (niveau 4), des bras géants, un pieu chargé et
        // un socle de pierre (niveau 5). Les pièces de la tourelle tournent avec elle.
        static bool BuildBallista(GameObject root, Structure structure)
        {
            if (!(structure is Ballista) || structure is StructureNetLauncher)
                return false;

            var turret = new SerializedObject(structure).FindProperty("m_Turret").objectReferenceValue as Transform;
            if (turret == null)
                return false;

            GetParts(root, structure, out var level2, out var level3);
            var wood = MakeMaterial("Bois", new Color(0.55f, 0.36f, 0.2f), 0.2f, 0f);
            var metal = MakeMaterial("Metal", new Color(0.6f, 0.62f, 0.66f), 0.6f, 0.8f);
            var stone = MakeMaterial("Pierre", new Color(0.55f, 0.53f, 0.5f), 0.1f, 0f);
            var rope = MakeMaterial("Corde", new Color(0.7f, 0.6f, 0.4f), 0.1f, 0f);
            var gold = MakeMaterial("Lueur jaune", new Color(1f, 0.85f, 0.3f), 0.5f, 0f, new Color(1f, 0.8f, 0.2f) * 2.5f);

            var level3Mount = Mount(Part(level3, "Scorpion").transform, turret);
            Box(level3Mount, "Deuxième arc", wood, new Vector3(0f, 0.75f, 0.45f), new Vector3(1.3f, 0.08f, 0.1f)).transform.localRotation =
                Quaternion.Euler(0f, 0f, 0f);
            Box(level3Mount, "Fût", wood, new Vector3(0f, 0.72f, 0.1f), new Vector3(0.1f, 0.08f, 0.8f));

            var level4 = Part(root, "Niveau 4");
            var level4Mount = Mount(Part(level4, "Harpon").transform, turret);
            Box(level4Mount, "Pointe du harpon", metal, new Vector3(0f, 0.45f, 0.85f), new Vector3(0.12f, 0.12f, 0.3f)).transform.localRotation =
                Quaternion.Euler(0f, 0f, 45f);
            Primitive(PrimitiveType.Cylinder, level4Mount, "Corde enroulée", rope, new Vector3(0.38f, 0.35f, -0.1f), new Vector3(0.3f, 0.12f, 0.3f));

            var level5 = Part(root, "Niveau 5");
            Primitive(PrimitiveType.Cylinder, level5.transform, "Socle", stone, new Vector3(0f, 0.15f, 0f), new Vector3(2.3f, 0.15f, 2.3f));
            var level5Mount = Mount(Part(level5, "Titan").transform, turret);
            foreach (var side in new[] { -1f, 1f })
            {
                var limb = Box(level5Mount, "Bras géant", wood, new Vector3(0.75f * side, 0.55f, 0.55f), new Vector3(1.5f, 0.16f, 0.16f));
                limb.transform.localRotation = Quaternion.Euler(0f, -18f * side, 0f);
            }

            Box(level5Mount, "Pieu géant", wood, new Vector3(0f, 0.6f, 0.4f), new Vector3(0.16f, 0.16f, 2.2f));
            Box(level5Mount, "Pointe du pieu", metal, new Vector3(0f, 0.6f, 1.55f), new Vector3(0.22f, 0.22f, 0.3f)).transform.localRotation =
                Quaternion.Euler(0f, 0f, 45f);
            Box(level5Mount, "Liseré", gold, new Vector3(0f, 0.42f, 0.2f), new Vector3(0.5f, 0.04f, 1.2f));

            SetParts(structure, level2, level3, level4, level5);
            var settings = new SerializedObject(structure);
            SetIntsIfStill(settings.FindProperty("m_PiercePerLevel"), new[] { 0, 0, 2 }, new[] { 0, 2, 2, 2, 2 });
            SetIfStill(settings.FindProperty("m_DamagePerLevel"), new[] { 10f, 14.67f, 21.33f }, new[] { 10f, 14.67f, 21.33f, 26f, 32f });
            SetIfStill(settings.FindProperty("m_IntervalPerLevel"), new[] { 3f, 2f, 1.25f }, new[] { 3f, 2f, 1.25f, 1.1f, 1f });
            settings.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // Mortier : des tonneaux de goudron (niveau 4), un tube géant, une pile d'obus et des rayures (niveau 5).
        static bool BuildMortar(GameObject root, Structure structure)
        {
            if (!(structure is StructureMortar))
                return false;

            var turret = new SerializedObject(structure).FindProperty("m_Turret").objectReferenceValue as Transform;
            GetParts(root, structure, out var level2, out var level3);
            var iron = MakeMaterial("Fonte", new Color(0.2f, 0.2f, 0.22f), 0.4f, 0.7f);
            var tar = MakeMaterial("Goudron", new Color(0.05f, 0.04f, 0.035f), 0.92f, 0f);
            var yellow = MakeMaterial("Rayures jaunes", new Color(1f, 0.78f, 0.1f), 0.35f, 0f);
            var fire = MakeMaterial("Lueur orange", new Color(1f, 0.55f, 0.15f), 0.5f, 0f, new Color(1f, 0.5f, 0.1f) * 3f);

            var level4 = Part(root, "Niveau 4");
            for (var i = 0; i < 2; i++)
            {
                var barrel = Model(k_Barrel, level4.transform, "Tonneau de goudron");
                if (barrel != null)
                    Fit(barrel, 0.55f, new Vector3(i == 0 ? -0.95f : 0.95f, 0f, -0.75f));
            }

            Primitive(PrimitiveType.Cylinder, level4.transform, "Flaque", tar, new Vector3(0f, 0.01f, -0.8f), new Vector3(1.6f, 0.01f, 0.8f));

            var level5 = Part(root, "Niveau 5");
            if (turret != null)
            {
                var mount = Mount(Part(level5, "Tube géant").transform, turret);
                var tube = Primitive(PrimitiveType.Cylinder, mount, "Tube géant", iron, new Vector3(0f, 1.1f, 0.15f), new Vector3(0.75f, 0.8f, 0.75f));
                tube.transform.localRotation = Quaternion.Euler(-35f, 0f, 0f);
                var mouth = Primitive(PrimitiveType.Cylinder, mount, "Bouche rougeoyante", fire, new Vector3(0f, 1.75f, 0.6f), new Vector3(0.6f, 0.02f, 0.6f));
                mouth.transform.localRotation = Quaternion.Euler(-35f, 0f, 0f);
                for (var i = 0; i < 3; i++)
                    Primitive(PrimitiveType.Cylinder, mount, "Cerclage", yellow, new Vector3(0f, 0.85f + i * 0.3f, -0.03f + i * 0.21f), new Vector3(0.8f, 0.04f, 0.8f))
                        .transform.localRotation = Quaternion.Euler(-35f, 0f, 0f);
            }

            for (var i = 0; i < 6; i++)
            {
                var row = i < 3 ? 0 : i < 5 ? 1 : 2;
                var x = 1.05f + (i < 3 ? i * 0.32f : i < 5 ? 0.16f + (i - 3) * 0.32f : 0.32f);
                Primitive(PrimitiveType.Sphere, level5.transform, "Obus", iron, new Vector3(x, 0.15f + row * 0.27f, 0.8f), new Vector3(0.3f, 0.3f, 0.3f));
            }

            SetParts(structure, level2, level3, level4, level5);
            var settings = new SerializedObject(structure);
            settings.FindProperty("m_TarMaterial").objectReferenceValue = tar;
            settings.FindProperty("m_IgniteClip").objectReferenceValue = Clip("fire_loop.wav");
            SetIfStill(settings.FindProperty("m_DamagePerLevel"), new[] { 30f, 42f, 60f }, new[] { 30f, 42f, 60f, 70f, 85f });
            SetIfStill(settings.FindProperty("m_IntervalPerLevel"), new[] { 4.5f, 3.8f, 3.2f }, new[] { 4.5f, 3.8f, 3.2f, 3f, 2.8f });
            settings.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // Tour de givre : un grand pic de glace au sommet (niveau 3), le sol verglacé (niveau 4), un cœur de glace flottant et une
        // couronne d'éclats (niveau 5).
        static bool BuildFrost(GameObject root, Structure structure)
        {
            if (!(structure is FrostTower))
                return false;

            GetParts(root, structure, out var level2, out var level3);
            var top = Top(root);
            var ice = MakeTransparentMaterial("Glace", new Color(0.7f, 0.9f, 1f, 0.55f), 0.95f, new Color(0.3f, 0.6f, 1f) * 0.6f);
            var glow = MakeMaterial("Lueur bleue", new Color(0.4f, 0.75f, 1f), 0.5f, 0f, new Color(0.35f, 0.7f, 1f) * 3f);
            var frost = MakeMaterial("Givre", new Color(0.85f, 0.95f, 1f), 0.95f, 0f);

            var peak = Anchor(level3, root, "Pic de glace").transform;
            var spike = Box(peak, "Pic de glace", ice, new Vector3(0f, top + 0.7f, 0f), new Vector3(0.4f, 1.4f, 0.4f));
            spike.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            foreach (var side in new[] { -1f, 1f })
                Box(peak, "Éclat", ice, new Vector3(0.3f * side, top + 0.35f, 0.1f), new Vector3(0.2f, 0.7f, 0.2f)).transform.localRotation =
                    Quaternion.Euler(0f, 30f, 20f * side);

            var level4 = Part(root, "Niveau 4");
            Primitive(PrimitiveType.Cylinder, level4.transform, "Verglas", frost, new Vector3(0f, 0.02f, 0f), new Vector3(3f, 0.02f, 3f));

            var level5 = Part(root, "Niveau 5");
            var heart = Box(level5.transform, "Cœur de l'hiver", glow, new Vector3(0f, top + 2.2f, 0f), new Vector3(0.7f, 0.7f, 0.7f));
            heart.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            Box(level5.transform, "Gangue", ice, new Vector3(0f, top + 2.2f, 0f), new Vector3(1.1f, 1.1f, 1.1f)).transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            for (var i = 0; i < 6; i++)
            {
                var angle = i * 60f;
                var place = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 1.1f + Vector3.up * (top + 1.8f);
                Box(level5.transform, "Éclat", ice, place, new Vector3(0.18f, 0.9f, 0.18f)).transform.localRotation = Quaternion.Euler(0f, angle, 25f);
            }

            SetParts(structure, level2, level3, level4, level5);
            var settings = new SerializedObject(structure);
            settings.FindProperty("m_IceMaterial").objectReferenceValue = ice;
            settings.FindProperty("m_BreakClip").objectReferenceValue = Clip("projectile_break.wav");
            settings.FindProperty("m_StormClip").objectReferenceValue = Clip("ice_zone.wav");
            SetIfStill(settings.FindProperty("m_CrystalScalePerLevel"), new[] { 1f, 1.15f, 1.3f }, new[] { 1f, 1.15f, 1.3f, 1.45f, 1.7f });
            SetIfStill(settings.FindProperty("m_LightIntensityPerLevel"), new[] { 1.5f, 2.2f, 3f }, new[] { 1.5f, 2.2f, 3f, 3.5f, 4.5f });
            settings.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // Lance-filet : des bobines électriques sur le mât (niveau 4), une grande couronne de filet au sommet (niveau 5).
        static bool BuildNet(GameObject root, Structure structure)
        {
            if (!(structure is StructureNetLauncher))
                return false;

            GetParts(root, structure, out var level2, out var level3);
            var top = Top(root);
            var copper = MakeMaterial("Cuivre", new Color(0.8f, 0.45f, 0.25f), 0.6f, 0.8f);
            var cyan = MakeMaterial("Lueur cyan", new Color(0.5f, 0.85f, 1f), 0.5f, 0f, new Color(0.4f, 0.8f, 1f) * 3f);
            var shockNet = MakeTransparentMaterial("Filet electrique", new Color(0.5f, 0.85f, 1f, 0.6f), 0.6f, new Color(0.4f, 0.8f, 1f) * 2f);
            var net = AssetDatabase.LoadAssetAtPath<Material>(k_ArtFolder + "/Filet.mat");

            var level4 = Part(root, "Niveau 4");
            for (var i = 0; i < 3; i++)
                Primitive(PrimitiveType.Cylinder, level4.transform, "Bobine", copper, new Vector3(0f, 0.6f + i * 0.35f, -0.35f), new Vector3(0.32f, 0.1f, 0.32f));
            Primitive(PrimitiveType.Sphere, level4.transform, "Éclateur", cyan, new Vector3(0f, 1.75f, -0.35f), new Vector3(0.22f, 0.22f, 0.22f));

            var level5 = Part(root, "Niveau 5");
            for (var i = 0; i < 12; i++)
            {
                var angle = i * 30f;
                var place = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 1.1f + Vector3.up * (top + 0.5f);
                Box(level5.transform, "Couronne", copper, place, new Vector3(0.6f, 0.08f, 0.08f)).transform.localRotation = Quaternion.Euler(0f, angle + 90f, 0f);
            }

            Primitive(PrimitiveType.Cylinder, level5.transform, "Toile", net != null ? net : shockNet, new Vector3(0f, top + 0.5f, 0f), new Vector3(2.1f, 0.01f, 2.1f));
            Primitive(PrimitiveType.Sphere, level5.transform, "Cœur", cyan, new Vector3(0f, top + 0.75f, 0f), new Vector3(0.3f, 0.3f, 0.3f));

            SetParts(structure, level2, level3, level4, level5);
            var settings = new SerializedObject(structure);
            settings.FindProperty("m_ShockNetMaterial").objectReferenceValue = shockNet;
            settings.FindProperty("m_WebClip").objectReferenceValue = Clip("lightning.wav");
            SetIfStill(settings.FindProperty("m_HoldPerLevel"), new[] { 3f, 4f, 5f }, new[] { 3f, 5f, 5f, 5f, 6f });
            settings.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // Tour-aimant : un anneau violet lumineux (niveau 4), une sphère noire qui flotte entre deux anneaux (niveau 5).
        static bool BuildMagnet(GameObject root, Structure structure)
        {
            if (!(structure is StructureMagnet))
                return false;

            GetParts(root, structure, out var level2, out var level3);
            var top = Top(root);
            var purple = MakeMaterial("Lueur violette", new Color(0.85f, 0.4f, 1f), 0.5f, 0f, new Color(0.8f, 0.3f, 1f) * 3f);
            var hole = MakeMaterial("Trou noir", new Color(0.02f, 0.01f, 0.03f), 0.95f, 0f, new Color(0.35f, 0.1f, 0.5f) * 0.6f);

            var level4 = Part(root, "Niveau 4");
            Ring(level4.transform, "Anneau", purple, new Vector3(0f, top * 0.6f, 0f), 0.8f, 14, 0.07f, Quaternion.identity);

            var level5 = Part(root, "Niveau 5");
            Primitive(PrimitiveType.Sphere, level5.transform, "Trou noir", hole, new Vector3(0f, top + 1.3f, 0f), new Vector3(0.9f, 0.9f, 0.9f));
            Ring(level5.transform, "Anneau", purple, new Vector3(0f, top + 1.3f, 0f), 0.85f, 16, 0.06f, Quaternion.Euler(70f, 0f, 0f));
            Ring(level5.transform, "Anneau", purple, new Vector3(0f, top + 1.3f, 0f), 1.05f, 18, 0.05f, Quaternion.Euler(-60f, 40f, 0f));

            SetParts(structure, level2, level3, level4, level5);
            var settings = new SerializedObject(structure);
            settings.FindProperty("m_SingularityMaterial").objectReferenceValue = hole;
            settings.FindProperty("m_SingularityClip").objectReferenceValue = Clip("teleport_charge.wav");
            SetIfStill(settings.FindProperty("m_RangePerLevel"), new[] { 8f, 9f, 10f }, new[] { 8f, 9f, 10f, 11f, 12f });
            settings.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // Bulle : un anneau à bulles au-dessus du chaudron (niveau 4), une bulle immense couronnée d'or (niveau 5).
        static bool BuildBubble(GameObject root, Structure structure)
        {
            if (!(structure is StructureBubble))
                return false;

            GetParts(root, structure, out var level2, out var level3);
            var top = Top(root);
            var film = AssetDatabase.LoadAssetAtPath<Material>(k_ArtFolder + "/Bulle.mat");
            var gold = MakeMaterial("Lueur jaune", new Color(1f, 0.85f, 0.3f), 0.5f, 0f, new Color(1f, 0.8f, 0.2f) * 2.5f);
            var copper = MakeMaterial("Cuivre", new Color(0.8f, 0.45f, 0.25f), 0.6f, 0.8f);

            var level4 = Part(root, "Niveau 4");
            Ring(level4.transform, "Anneau à bulles", copper, new Vector3(0.6f, top + 0.3f, 0f), 0.35f, 12, 0.05f, Quaternion.Euler(0f, 0f, 70f));
            for (var i = 0; i < 3; i++)
                Primitive(PrimitiveType.Sphere, level4.transform, "Bulle", film, new Vector3(0.9f + i * 0.2f, top + 0.5f + i * 0.35f, 0.1f * i), Vector3.one * (0.22f - i * 0.04f));

            var level5 = Part(root, "Niveau 5");
            Primitive(PrimitiveType.Sphere, level5.transform, "Orbe", film, new Vector3(0f, top + 1.2f, 0f), new Vector3(1.6f, 1.6f, 1.6f));
            Ring(level5.transform, "Couronne", gold, new Vector3(0f, top + 1.2f, 0f), 0.9f, 18, 0.06f, Quaternion.identity);

            SetParts(structure, level2, level3, level4, level5);
            var settings = new SerializedObject(structure);
            SetIntsIfStill(settings.FindProperty("m_BubblesPerLevel"), new[] { 1, 1, 2 }, new[] { 1, 2, 2, 2, 2 });
            settings.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // Phare : un brasier autour de la lanterne (niveau 4), un halo et une couronne d'or au-dessus (niveau 5).
        static bool BuildLighthouse(GameObject root, Structure structure)
        {
            if (!(structure is StructureLighthouse))
                return false;

            GetParts(root, structure, out var level2, out var level3);
            var lantern = root.transform.Find("Lanterne");
            var top = lantern != null ? lantern.localPosition.y : Top(root);
            var fire = MakeMaterial("Lueur orange", new Color(1f, 0.55f, 0.15f), 0.5f, 0f, new Color(1f, 0.5f, 0.1f) * 3f);
            var gold = MakeMaterial("Lueur doree", new Color(1f, 0.9f, 0.55f), 0.6f, 0f, new Color(1f, 0.85f, 0.45f) * 4f);
            var judgment = MakeTransparentMaterial("Rayon du jugement", new Color(1f, 0.95f, 0.7f, 0.55f), 0.5f, new Color(1f, 0.9f, 0.6f) * 4f);

            var level4 = Part(root, "Niveau 4");
            Ring(level4.transform, "Brasier", fire, new Vector3(0f, top - 0.25f, 0f), 0.45f, 12, 0.07f, Quaternion.identity);
            for (var i = 0; i < 4; i++)
                Box(level4.transform, "Flamme", fire, Quaternion.Euler(0f, i * 90f, 0f) * Vector3.forward * 0.45f + Vector3.up * (top - 0.1f),
                    new Vector3(0.1f, 0.3f, 0.1f));

            var level5 = Part(root, "Niveau 5");
            Ring(level5.transform, "Halo", gold, new Vector3(0f, top + 1f, 0f), 0.8f, 20, 0.06f, Quaternion.identity);
            for (var i = 0; i < 6; i++)
                Box(level5.transform, "Pointe de couronne", gold, Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward * 0.4f + Vector3.up * (top + 0.55f),
                    new Vector3(0.08f, 0.35f, 0.08f));
            Primitive(PrimitiveType.Sphere, level5.transform, "Soleil", gold, new Vector3(0f, top + 1f, 0f), new Vector3(0.35f, 0.35f, 0.35f));

            SetParts(structure, level2, level3, level4, level5);
            var settings = new SerializedObject(structure);
            settings.FindProperty("m_JudgmentMaterial").objectReferenceValue = judgment;
            settings.FindProperty("m_JudgmentClip").objectReferenceValue = Clip("lightning.wav");
            SetIfStill(settings.FindProperty("m_RadiusPerLevel"), new[] { 12f, 14f, 16f }, new[] { 12f, 14f, 20f, 20f, 24f });
            SetIfStill(settings.FindProperty("m_BonusPerLevel"), new[] { 0.2f, 0.35f, 0.5f }, new[] { 0.2f, 0.35f, 0.5f, 0.5f, 0.6f });
            settings.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // Un objet sous cette pièce de niveau, calé sur la racine du prefab (même place, même orientation, échelle 1) : on y pose
        // les formes dans le repère du bâtiment, où que soit la pièce.
        static GameObject Anchor(GameObject part, GameObject root, string name)
        {
            var anchor = Part(part, name);
            var scale = part.transform.lossyScale;
            anchor.transform.localScale = new Vector3(1f / Mathf.Max(1e-4f, scale.x), 1f / Mathf.Max(1e-4f, scale.y), 1f / Mathf.Max(1e-4f, scale.z));
            anchor.transform.SetPositionAndRotation(root.transform.position, root.transform.rotation);
            return anchor;
        }

        // Les pièces des niveaux 2 et 3 du prefab, ou deux pièces vides (les piques et le givre n'en avaient pas).
        static void GetParts(GameObject root, Structure structure, out GameObject level2, out GameObject level3)
        {
            var parts = new SerializedObject(structure).FindProperty("m_LevelParts");
            level2 = parts.arraySize >= 1 ? parts.GetArrayElementAtIndex(0).objectReferenceValue as GameObject : null;
            level3 = parts.arraySize >= 2 ? parts.GetArrayElementAtIndex(1).objectReferenceValue as GameObject : null;
            if (level2 == null)
                level2 = Part(root, "Niveau 2");
            if (level3 == null)
                level3 = Part(root, "Niveau 3");
        }

        static void SetParts(Structure structure, params GameObject[] parts)
        {
            var settings = new SerializedObject(structure);
            var levelParts = settings.FindProperty("m_LevelParts");
            levelParts.arraySize = parts.Length;
            for (var i = 0; i < parts.Length; i++)
                levelParts.GetArrayElementAtIndex(i).objectReferenceValue = parts[i];
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        // Une monture qui suit la tourelle (elle pivote) sans prendre son échelle : on y pose les formes en mètres, depuis son pivot.
        // Elle reste sous sa pièce de niveau (montrée ou cachée avec elle) ; FollowPart la recale sur la tourelle à chaque image.
        static Transform Mount(Transform part, Transform turret)
        {
            var mount = new GameObject("Sur la tourelle").transform;
            mount.SetParent(part, false);
            var scale = part.lossyScale;
            mount.localScale = new Vector3(1f / Mathf.Max(1e-4f, scale.x), 1f / Mathf.Max(1e-4f, scale.y), 1f / Mathf.Max(1e-4f, scale.z));
            mount.SetPositionAndRotation(turret.position, turret.rotation);
            var follow = new SerializedObject(mount.gameObject.AddComponent<FollowPart>());
            follow.FindProperty("m_Target").objectReferenceValue = turret;
            follow.ApplyModifiedPropertiesWithoutUndo();
            return mount;
        }

        // Le haut du modèle (m), dans le repère du prefab (posé à l'origine).
        static float Top(GameObject root)
        {
            var top = 1f;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                top = Mathf.Max(top, renderer.bounds.max.y - root.transform.position.y);
            return top;
        }

        // Un anneau de petites barres, à plat dans le plan XZ de sa rotation.
        static void Ring(Transform parent, string name, Material material, Vector3 center, float radius, int count, float thickness, Quaternion rotation)
        {
            var ring = new GameObject(name).transform;
            ring.SetParent(parent, false);
            ring.localPosition = center;
            ring.localRotation = rotation;
            var length = 2f * Mathf.PI * radius / count * 1.1f;
            for (var i = 0; i < count; i++)
            {
                var angle = i * 360f / count;
                var bar = Box(ring, "Maillon", material, Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius, new Vector3(length, thickness, thickness));
                bar.transform.localRotation = Quaternion.Euler(0f, angle, 0f);
            }
        }

        static BallistaBolt Bolt() =>
            AssetDatabase.LoadAssetAtPath<GameObject>(k_PrefabFolder + "Ballista Bolt.prefab") is GameObject go ? go.GetComponent<BallistaBolt>() : null;

        static AudioClip Clip(string file) => AssetDatabase.LoadAssetAtPath<AudioClip>(k_Audio + file);

        static void SetIntsIfStill(SerializedProperty array, int[] old, int[] values)
        {
            if (array == null || !array.isArray || array.arraySize != old.Length)
                return;
            for (var i = 0; i < old.Length; i++)
            {
                if (array.GetArrayElementAtIndex(i).intValue != old[i])
                    return;
            }

            array.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                array.GetArrayElementAtIndex(i).intValue = values[i];
        }

        static Material MakeTransparentMaterial(string name, Color color, float smoothness, Color emission = default)
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
            if (emission.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // ---------------------------------------------------------------- retouches après les essais du 29 octobre au soir

        const string k_RetouchMark = "Retouche du 29 octobre";

        // Les retours du joueur sur les 9 tours (29 octobre au soir) : les modèles de la baliste, du mortier (en hauteur, plus
        // grand, une batterie au niveau 5), du lance-filet (niveau 5) et des tours de guet de la forteresse (trop petites) ; le
        // goudron moins brillant (il paraissait bleu de loin) ; un rayon du phare dans le mauvais sens ; le phare au bord de la voie
        // aussi ; le gel du cœur de l'hiver à 10 s ; la mâchoire des piques sur 20 m. Une fois par prefab (une marque vide).
        static void Retouch()
        {
            RetouchPrefab("Baliste", RetouchBallista);
            RetouchPrefab("Mortier", RetouchMortar);
            RetouchPrefab("Filet", RetouchNet);
            RetouchPrefab("Barricade", RetouchBarricade);
            RetouchPrefab("Phare", RetouchLighthouse);
            RetouchPrefab("Givre", (root, structure) => SetFloatIfStill(structure, "m_StormFreeze", 3f, 10f));
            RetouchPrefab("Pieges", (root, structure) => SetFloatIfStill(structure, "m_JawLength", 10f, 20f));

            var lighthouse = AssetDatabase.LoadAssetAtPath<StructureDefinition>(k_DataFolder + "Phare.asset");
            if (lighthouse != null && lighthouse.slot == BuildSlot.Height)
            {
                lighthouse.slot = BuildSlot.Roadside;
                EditorUtility.SetDirty(lighthouse);
                AssetDatabase.SaveAssetIfDirty(lighthouse);
                Debug.Log("Évolutions des tours : le phare se construit aussi au bord de la voie (son soutien y sert plus qu'en hauteur).", lighthouse);
            }
        }

        // Une retouche, une fois (la marque vide mark) ; après une autre si requires est donné (sa marque doit être là).
        static void RetouchPrefab(string file, System.Func<GameObject, Structure, bool> retouch, string mark = k_RetouchMark, string requires = null)
        {
            var path = k_PrefabFolder + file + ".prefab";
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null || asset.transform.Find("Niveau 5") == null || asset.transform.Find(mark) != null ||
                (requires != null && asset.transform.Find(requires) == null))
                return;

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (!retouch(root, root.GetComponent<Structure>()))
                    return;

                var marker = new GameObject(mark);
                marker.transform.SetParent(root.transform, false);
                marker.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"Évolutions des tours : {file}, {mark.ToLowerInvariant()} ({path}).", asset);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"Évolutions des tours : la retouche de {file} a échoué ({exception.Message}).", asset);
                Debug.LogException(exception);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static bool SetFloatIfStill(Structure structure, string property, float old, float value)
        {
            var settings = new SerializedObject(structure);
            var field = settings.FindProperty(property);
            if (field != null && Mathf.Approximately(field.floatValue, old))
            {
                field.floatValue = value;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            return true;
        }

        // Baliste : au niveau 3, une deuxième baliste du kit sur un cadre au-dessus de la première (on voyait mal le « double
        // arc ») ; au niveau 4, un harpon barbelé sur chacune et un treuil ; au niveau 5, une baliste géante sur une plate-forme de
        // pierre au-dessus des deux, d'où part le pieu. Tout est mesuré sur le modèle de la tourelle et la suit.
        static bool RetouchBallista(GameObject root, Structure structure)
        {
            var turret = new SerializedObject(structure).FindProperty("m_Turret").objectReferenceValue as Transform;
            if (turret == null)
                return false;

            GetParts(root, structure, out var level2, out var level3);
            DestroyNamed(level3.transform, "Scorpion");
            DestroyChild(root.transform, "Niveau 4");
            DestroyChild(root.transform, "Niveau 5");

            var bounds = LocalBounds(turret, turret);
            var rotation = ModelRotation(turret);
            var width = Mathf.Max(bounds.size.x, bounds.size.z);
            var wood = MakeMaterial("Bois", new Color(0.55f, 0.36f, 0.2f), 0.2f, 0f);
            var metal = MakeMaterial("Metal", new Color(0.6f, 0.62f, 0.66f), 0.6f, 0.8f);
            var stone = MakeMaterial("Pierre", new Color(0.55f, 0.53f, 0.5f), 0.1f, 0f);
            var rope = MakeMaterial("Corde", new Color(0.7f, 0.6f, 0.4f), 0.1f, 0f);
            var gold = MakeMaterial("Lueur jaune", new Color(1f, 0.85f, 0.3f), 0.5f, 0f, new Color(1f, 0.8f, 0.2f) * 2.5f);
            var center = bounds.center;

            // Niveau 3 : le cadre et la deuxième baliste.
            var scorpion = Mount(Part(level3, "Scorpion").transform, turret);
            var deck = bounds.max.y + 0.12f;
            foreach (var x in new[] { -1f, 1f })
            {
                foreach (var z in new[] { -1f, 1f })
                {
                    var post = new Vector3(center.x + x * bounds.extents.x * 0.85f, (bounds.min.y + deck) * 0.5f, center.z + z * bounds.extents.z * 0.6f);
                    Box(scorpion, "Montant", wood, post, new Vector3(0.08f, deck - bounds.min.y, 0.08f));
                }
            }

            Box(scorpion, "Plancher", wood, new Vector3(center.x, deck - 0.03f, center.z), new Vector3(bounds.size.x * 0.95f, 0.06f, bounds.size.z * 0.7f));
            var second = Model(k_Kit + "weapon-ballista.fbx", scorpion, "Deuxième baliste");
            if (second != null)
            {
                second.transform.localRotation = rotation;
                Fit(second, width, new Vector3(center.x, deck, center.z));
            }

            var top = deck + bounds.size.y;

            // Niveau 4 : un harpon barbelé devant chaque baliste, un treuil et sa corde.
            var level4 = Part(root, "Niveau 4");
            var harpoons = Mount(Part(level4, "Harpons").transform, turret);
            foreach (var y in new[] { center.y, deck + bounds.extents.y })
            {
                var tip = new Vector3(center.x, y, bounds.max.z + 0.2f);
                Box(harpoons, "Pointe de harpon", metal, tip, new Vector3(0.16f, 0.16f, 0.3f)).transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                foreach (var side in new[] { -1f, 1f })
                    Box(harpoons, "Barbelure", metal, tip + new Vector3(0.1f * side, 0f, -0.1f), new Vector3(0.04f, 0.04f, 0.2f)).transform.localRotation =
                        Quaternion.Euler(0f, 35f * side, 0f);
            }

            var drum = new Vector3(bounds.max.x + 0.2f, center.y, center.z);
            Primitive(PrimitiveType.Cylinder, harpoons, "Treuil", rope, drum, new Vector3(0.34f, 0.16f, 0.34f)).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Cord(harpoons, rope, drum, new Vector3(center.x + 0.05f, center.y, bounds.max.z + 0.05f), 0.03f);

            // Niveau 5 : la plate-forme sur quatre piliers, la baliste géante, la bouche du pieu.
            var level5 = Part(root, "Niveau 5");
            var titan = Mount(Part(level5, "Titan").transform, turret);
            var platform = top + 0.15f;
            foreach (var x in new[] { -1f, 1f })
            {
                foreach (var z in new[] { -1f, 1f })
                {
                    var pillar = new Vector3(center.x + x * (bounds.extents.x + 0.2f), (bounds.min.y + platform) * 0.5f, center.z + z * (bounds.extents.z + 0.15f));
                    Box(titan, "Pilier", stone, pillar, new Vector3(0.2f, platform - bounds.min.y, 0.2f));
                }
            }

            Box(titan, "Plate-forme", stone, new Vector3(center.x, platform - 0.08f, center.z), new Vector3(width * 2.2f, 0.16f, width * 2.2f));
            Box(titan, "Liseré", gold, new Vector3(center.x, platform + 0.01f, center.z), new Vector3(width * 2.25f, 0.03f, 0.12f));
            var giant = Model(k_Kit + "weapon-ballista.fbx", titan, "Baliste titan");
            var mouth = new GameObject("Bouche du titan").transform;
            mouth.SetParent(titan, false);
            if (giant != null)
            {
                giant.transform.localRotation = rotation;
                Fit(giant, width * 2.6f, new Vector3(center.x, platform, center.z));
                var giantBounds = LocalBounds(titan, giant.transform);
                mouth.localPosition = new Vector3(giantBounds.center.x, giantBounds.center.y + giantBounds.extents.y * 0.3f, giantBounds.max.z);
            }
            else
            {
                mouth.localPosition = new Vector3(center.x, platform + 0.5f, bounds.max.z);
            }

            SetParts(structure, level2, level3, level4, level5);
            var settings = new SerializedObject(structure);
            settings.FindProperty("m_TitanMuzzle").objectReferenceValue = mouth;
            settings.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // Mortier : posé sur une tour carrée du kit, une fois et demie plus grand (« peu imposant », le joueur) ; des tonneaux de
        // goudron au pied (niveau 4) ; au niveau 5, la batterie : quatre tubes de plus de chaque côté du sien, une pile d'obus et des
        // rayures, qui disent le bombardement. Le goudron passe à un noir brun mat (il paraissait bleu de loin).
        static bool RetouchMortar(GameObject root, Structure structure)
        {
            var turret = new SerializedObject(structure).FindProperty("m_Turret").objectReferenceValue as Transform;
            if (turret == null)
                return false;

            GetParts(root, structure, out var level2, out var level3);
            DestroyChild(root.transform, "Niveau 4");
            DestroyChild(root.transform, "Niveau 5");
            const float scale = 1.5f;
            var moved = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in root.transform)
            {
                if (!child.name.StartsWith("Retouche"))
                    moved.Add(child);
            }

            var pedestal = Model(k_Kit + "tower-square-bottom-a.fbx", root.transform, "Socle");
            var height = 2.2f;
            if (pedestal != null)
            {
                Fit(pedestal, 2.4f, Vector3.zero);
                height = MaxY(pedestal) - root.transform.position.y;
            }
            else
            {
                Box(root.transform, "Socle", MakeMaterial("Pierre", new Color(0.55f, 0.53f, 0.5f), 0.1f, 0f), new Vector3(0f, height * 0.5f, 0f), new Vector3(2.4f, height, 2.4f));
            }

            foreach (var child in moved)
            {
                child.localPosition = child.localPosition * scale + Vector3.up * height;
                child.localScale *= scale;
            }

            var tar = MakeMaterial("Goudron mat", new Color(0.08f, 0.055f, 0.035f), 0.3f, 0f);
            var iron = MakeMaterial("Fonte", new Color(0.2f, 0.2f, 0.22f), 0.4f, 0.7f);
            var yellow = MakeMaterial("Rayures jaunes", new Color(1f, 0.78f, 0.1f), 0.35f, 0f);
            var black = MakeMaterial("Noir mat", new Color(0.08f, 0.08f, 0.09f), 0.2f, 0f);

            var level4 = Part(root, "Niveau 4");
            for (var i = 0; i < 3; i++)
            {
                var barrel = Model(k_Barrel, level4.transform, "Tonneau de goudron");
                if (barrel != null)
                    Fit(barrel, 0.6f, new Vector3(-1.5f + i * 0.65f, 0f, -1.55f));
            }

            Primitive(PrimitiveType.Cylinder, level4.transform, "Flaque", tar, new Vector3(-0.85f, 0.01f, -1.6f), new Vector3(2.2f, 0.01f, 1f));

            var level5 = Part(root, "Niveau 5");
            var battery = Mount(Part(level5, "Batterie").transform, turret);
            Transform tube = null;
            foreach (var candidate in turret.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == "Tube" && !UnderLevelPart(candidate, turret))
                {
                    tube = candidate;
                    break;
                }
            }

            if (tube != null)
            {
                var tubeBounds = LocalBounds(turret, tube);
                var step = Mathf.Max(0.3f, tubeBounds.size.x * 1.15f);
                foreach (var offset in new[] { -2f, -1f, 1f, 2f })
                {
                    var copy = Object.Instantiate(tube.gameObject, tube.parent);
                    copy.name = "Tube de la batterie";
                    copy.transform.SetParent(battery, true);
                    copy.transform.position += turret.right * (offset * step);
                }
            }

            for (var i = 0; i < 10; i++)
            {
                var row = i < 4 ? 0 : i < 7 ? 1 : i < 9 ? 2 : 3;
                var index = i < 4 ? i : i < 7 ? i - 4 : i < 9 ? i - 7 : 0;
                var x = 0.4f + row * 0.17f + index * 0.34f;
                Primitive(PrimitiveType.Sphere, level5.transform, "Obus", iron, new Vector3(x - 0.9f, height + 0.17f + row * 0.29f, -1f), Vector3.one * 0.34f);
            }

            for (var i = 0; i < 8; i++)
            {
                var side = i < 4 ? -1f : 1f;
                Box(level5.transform, "Rayure", i % 2 == 0 ? yellow : black, new Vector3(side * 1.22f, height - 0.15f, -0.9f + (i % 4) * 0.6f),
                    new Vector3(0.04f, 0.3f, 0.6f));
            }

            SetParts(structure, level2, level3, level4, level5);
            var settings = new SerializedObject(structure);
            settings.FindProperty("m_TarMaterial").objectReferenceValue = tar;
            settings.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // Lance-filet, niveau 5 : un canon à filet du kit sur la tourelle, un filet enroulé derrière, une bobine électrique autour du
        // canon et deux antennes (le joueur : le niveau 5 n'était pas beau).
        static bool RetouchNet(GameObject root, Structure structure)
        {
            var turret = new SerializedObject(structure).FindProperty("m_Turret").objectReferenceValue as Transform;
            if (turret == null)
                return false;

            GetParts(root, structure, out var level2, out var level3);
            var level4 = root.transform.Find("Niveau 4");
            DestroyChild(root.transform, "Niveau 5");
            var bounds = LocalBounds(turret, turret);
            var width = Mathf.Max(bounds.size.x, bounds.size.z);
            var cyan = MakeMaterial("Lueur cyan", new Color(0.5f, 0.85f, 1f), 0.5f, 0f, new Color(0.4f, 0.8f, 1f) * 3f);
            var copper = MakeMaterial("Cuivre", new Color(0.8f, 0.45f, 0.25f), 0.6f, 0.8f);
            var net = AssetDatabase.LoadAssetAtPath<Material>(k_ArtFolder + "/Filet.mat");

            var level5 = Part(root, "Niveau 5");
            var mount = Mount(Part(level5, "Canon à filet").transform, turret);
            var cannon = Model(k_Kit + "weapon-cannon.fbx", mount, "Canon à filet");
            var barrelHeight = bounds.max.y + 0.35f;
            if (cannon != null)
            {
                cannon.transform.localRotation = ModelRotation(turret);
                Fit(cannon, width * 1.3f, new Vector3(bounds.center.x, bounds.max.y + 0.02f, bounds.center.z));
                var cannonBounds = LocalBounds(mount, cannon.transform);
                barrelHeight = cannonBounds.center.y;
            }

            Primitive(PrimitiveType.Cylinder, mount, "Filet enroulé", net, new Vector3(bounds.center.x, barrelHeight + 0.25f, bounds.min.z - 0.15f),
                new Vector3(0.45f, width * 0.45f, 0.45f)).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Ring(mount, "Bobine", copper, new Vector3(bounds.center.x, barrelHeight, bounds.center.z + 0.15f), width * 0.45f, 14, 0.05f, Quaternion.Euler(90f, 0f, 0f));
            Ring(mount, "Bobine", cyan, new Vector3(bounds.center.x, barrelHeight, bounds.center.z + 0.35f), width * 0.4f, 14, 0.04f, Quaternion.Euler(90f, 0f, 0f));
            foreach (var side in new[] { -1f, 1f })
            {
                Box(level5.transform, "Antenne", copper, new Vector3(1f * side, 1.1f, -0.6f), new Vector3(0.06f, 2.2f, 0.06f));
                Primitive(PrimitiveType.Sphere, level5.transform, "Éclateur", cyan, new Vector3(1f * side, 2.25f, -0.6f), Vector3.one * 0.18f);
            }

            SetParts(structure, level2, level3, level4 != null ? level4.gameObject : Part(root, "Niveau 4"), level5);
            return true;
        }

        // Forteresse : des tours de guet bien plus grandes (trop petites, le joueur), aux deux bouts du mur.
        static bool RetouchBarricade(GameObject root, Structure structure)
        {
            var level5 = root.transform.Find("Niveau 5");
            if (level5 == null)
                return false;

            foreach (var name in new[] { "Tour de guet", "Mât", "Bannière", "Archère" })
                DestroyNamed(level5, name);

            var wood = MakeMaterial("Bois", new Color(0.55f, 0.36f, 0.2f), 0.2f, 0f);
            var stone = MakeMaterial("Pierre", new Color(0.55f, 0.53f, 0.5f), 0.1f, 0f);
            var banner = MakeMaterial("Fanion", new Color(0.75f, 0.12f, 0.1f), 0.2f, 0f);
            var muzzles = new Transform[2];
            for (var s = 0; s < 2; s++)
            {
                var side = s == 0 ? -1f : 1f;
                var place = new Vector3(2.5f * side, 0f, -1.5f);
                var tower = Model(k_WatchTower, level5, "Tour de guet");
                float top;
                if (tower != null)
                {
                    Fit(tower, 2.3f, place);
                    top = MaxY(tower) - root.transform.position.y;
                }
                else
                {
                    top = 6.5f;
                    Primitive(PrimitiveType.Cylinder, level5, "Tour de guet", stone, place + Vector3.up * top * 0.5f, new Vector3(2f, top * 0.5f, 2f));
                }

                Box(level5, "Mât", wood, place + Vector3.up * (top + 0.8f), new Vector3(0.08f, 1.6f, 0.08f));
                Box(level5, "Bannière", banner, place + new Vector3(0.4f * side, top + 1.2f, 0f), new Vector3(0.75f, 0.55f, 0.03f));
                var muzzle = new GameObject("Archère").transform;
                muzzle.SetParent(level5, false);
                muzzle.localPosition = place + new Vector3(0f, top * 0.8f, 0.6f);
                muzzles[s] = muzzle;
            }

            var settings = new SerializedObject(structure);
            var towers = settings.FindProperty("m_WatchTowers");
            towers.arraySize = muzzles.Length;
            for (var i = 0; i < muzzles.Length; i++)
                towers.GetArrayElementAtIndex(i).objectReferenceValue = muzzles[i];
            settings.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // Phare : le deuxième rayon (niveau 3) était tourné à l'envers (son dégradé partait du bout) ; l'anneau de feu (niveau 4).
        static bool RetouchLighthouse(GameObject root, Structure structure)
        {
            var ray = root.transform.Find("Faisceau/Niveau 3/Rayon");
            if (ray != null && Mathf.Abs(Mathf.DeltaAngle(ray.localEulerAngles.z, 90f)) < 1f)
                ray.localRotation = Quaternion.Euler(0f, 0f, -90f);

            var settings = new SerializedObject(structure);
            settings.FindProperty("m_RingMaterial").objectReferenceValue =
                MakeMaterial("Lueur orange", new Color(1f, 0.55f, 0.15f), 0.5f, 0f, new Color(1f, 0.5f, 0.1f) * 3f);
            settings.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // Le haut (y, dans le monde) des rendus de ce modèle.
        static float MaxY(GameObject model)
        {
            var top = model.transform.position.y;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                top = Mathf.Max(top, renderer.bounds.max.y);
            return top;
        }

        // ---------------------------------------------------------------- deuxième retouche : les modèles des kits

        const string k_Castle = "Assets/kenney_castle-kit/Models/FBX format/";
        const string k_Town = "Assets/kenney_fantasy-town-kit/Models/FBX format/";
        const string k_Generator = "Assets/construction/Large Electric Generator by miro_art_studio - ZPlQHwiqTp/large_electric-generator.fbx";
        const string k_RetouchMark2 = "Retouche 2 du 29 octobre";

        // Les modèles refaits avec de vrais modèles (29 octobre au soir, « oui, refais-les », le joueur, après l'import du Fantasy
        // Town Kit) : la herse en métal et la forteresse (rempart, tours hexagonales, bannières) du Castle Kit, la baliste titan en
        // baliste de siège, le mortier sur une tour carrée de château avec une charrette de goudron, un générateur électrique pour
        // le lance-filet (niveau 4) et l'électro-aimant (niveau 3), une fontaine pour la bulle (niveau 4), des lanternes pour le
        // grand phare (niveau 3). Une fois par prefab, après la première retouche ; rien tant que les kits ne sont pas importés.
        static void Retouch2()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(k_Castle + "metal-gate.fbx") == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(k_Town + "lantern.fbx") == null)
                return;

            RetouchPrefab("Barricade", RebuildFortress, k_RetouchMark2, k_RetouchMark);
            RetouchPrefab("Baliste", RebuildTitan, k_RetouchMark2, k_RetouchMark);
            RetouchPrefab("Mortier", RebuildMortarTower, k_RetouchMark2, k_RetouchMark);
            RetouchPrefab("Filet", (root, structure) => AddModel(root, "Niveau 4", k_Generator, "Générateur", 1.1f, new Vector3(0.95f, 0f, -0.75f)),
                          k_RetouchMark2, k_RetouchMark);
            RetouchPrefab("Aimant", AddMagnetGenerator, k_RetouchMark2);
            RetouchPrefab("Bulle", (root, structure) => AddModel(root, "Niveau 4", k_Town + "fountain-round.fbx", "Fontaine", 2.7f, Vector3.zero),
                          k_RetouchMark2);
            RetouchPrefab("Phare", AddLighthouseLanterns, k_RetouchMark2, k_RetouchMark);
        }

        // Barricade : la herse du château à la place des barreaux (niveau 4) ; au niveau 5, un rempart crénelé et deux tours
        // hexagonales de quatre étages, surmontées d'une bannière, d'où partent les carreaux.
        static bool RebuildFortress(GameObject root, Structure structure)
        {
            var level4 = root.transform.Find("Niveau 4");
            var level5 = root.transform.Find("Niveau 5");
            if (level4 == null || level5 == null)
                return false;

            var portcullis = level4.Find("Herse");
            if (portcullis != null)
            {
                DestroyChildren(portcullis);
                var gate = Model(k_Castle + "metal-gate.fbx", portcullis, "Herse du château");
                if (gate != null)
                    Fit(gate, 4f, new Vector3(0f, -0.9f, 0f));
            }

            foreach (var name in new[] { "Mur géant", "Créneau", "Tour de guet", "Mât", "Bannière", "Archère" })
                DestroyNamed(level5, name);

            var wall = Model(k_Castle + "wall.fbx", level5, "Rempart");
            if (wall != null)
                Fit(wall, 4.3f, new Vector3(0f, 0f, -0.45f));

            var muzzles = new Transform[2];
            for (var s = 0; s < 2; s++)
            {
                var side = s == 0 ? -1f : 1f;
                var place = new Vector3(2.55f * side, 0f, -1.5f);
                var height = 0f;
                foreach (var piece in new[] { "tower-hexagon-base", "tower-hexagon-mid", "tower-hexagon-mid", "tower-hexagon-top" })
                {
                    var floor = Model(k_Castle + piece + ".fbx", level5, "Tour de guet");
                    if (floor == null)
                        continue;
                    Fit(floor, 2f, place + Vector3.up * height);
                    height = MaxY(floor) - root.transform.position.y;
                }

                var flag = Model(k_Castle + "flag-banner-long.fbx", level5, "Bannière");
                if (flag != null)
                    Fit(flag, 0.9f, place + Vector3.up * height);

                var muzzle = new GameObject("Archère").transform;
                muzzle.SetParent(level5, false);
                muzzle.localPosition = place + new Vector3(0f, Mathf.Max(2f, height - 0.6f), 0.9f);
                muzzles[s] = muzzle;
            }

            var settings = new SerializedObject(structure);
            var towers = settings.FindProperty("m_WatchTowers");
            towers.arraySize = muzzles.Length;
            for (var i = 0; i < muzzles.Length; i++)
                towers.GetArrayElementAtIndex(i).objectReferenceValue = muzzles[i];
            settings.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }

        // Baliste titan : la baliste de siège du Castle Kit à la place de la baliste agrandie, à la même place ; la bouche suit.
        static bool RebuildTitan(GameObject root, Structure structure)
        {
            var level5 = root.transform.Find("Niveau 5");
            Transform old = null;
            if (level5 != null)
            {
                foreach (var child in level5.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == "Baliste titan")
                    {
                        old = child;
                        break;
                    }
                }
            }

            if (old == null)
                return false;

            var mount = old.parent;
            var bounds = LocalBounds(mount, old);
            var rotation = old.localRotation;
            Object.DestroyImmediate(old.gameObject);
            var siege = Model(k_Castle + "siege-ballista.fbx", mount, "Baliste titan");
            if (siege == null)
                return false;

            siege.transform.localRotation = rotation;
            Fit(siege, Mathf.Max(bounds.size.x, bounds.size.z), new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
            var mouth = mount.Find("Bouche du titan");
            if (mouth != null)
            {
                var siegeBounds = LocalBounds(mount, siege.transform);
                mouth.localPosition = new Vector3(siegeBounds.center.x, siegeBounds.center.y + siegeBounds.extents.y * 0.3f, siegeBounds.max.z);
            }

            return true;
        }

        // Mortier : une tour carrée de château (base et étage) à la place du socle du kit ; le mortier remonte d'autant. Une
        // charrette de goudron au niveau 4 ; au niveau 5, des bannières aux coins de la tour (les rayures partent).
        static bool RebuildMortarTower(GameObject root, Structure structure)
        {
            var pedestal = root.transform.Find("Socle");
            if (pedestal == null)
                return false;

            var oldTop = MaxY(pedestal.gameObject) - root.transform.position.y;
            Object.DestroyImmediate(pedestal.gameObject);
            var tower = Part(root, "Socle");
            var height = 0f;
            foreach (var piece in new[] { "tower-square-base", "tower-square-mid" })
            {
                var floor = Model(k_Castle + piece + ".fbx", tower.transform, "Tour");
                if (floor == null)
                    continue;
                Fit(floor, 2.6f, Vector3.up * height);
                height = MaxY(floor) - root.transform.position.y;
            }

            var delta = height - oldTop;
            foreach (Transform child in root.transform)
            {
                if (child != tower.transform && child.name != "Niveau 4" && child.name != "Niveau 5" && !child.name.StartsWith("Retouche"))
                    child.localPosition += Vector3.up * delta;
            }

            var level4 = root.transform.Find("Niveau 4");
            if (level4 != null)
            {
                var cart = Model(k_Town + "cart.fbx", level4, "Charrette de goudron");
                if (cart != null)
                    Fit(cart, 1.5f, new Vector3(1.5f, 0f, -1.6f));
            }

            var level5 = root.transform.Find("Niveau 5");
            if (level5 != null)
            {
                DestroyNamed(level5, "Rayure");
                foreach (Transform child in level5)
                {
                    if (child.name == "Obus")
                        child.localPosition += Vector3.up * delta;
                }

                foreach (var x in new[] { -1.15f, 1.15f })
                {
                    foreach (var z in new[] { -1.15f, 1.15f })
                    {
                        var flag = Model(k_Castle + "flag-banner-short.fbx", level5, "Bannière");
                        if (flag != null)
                            Fit(flag, 0.45f, new Vector3(x, height, z));
                    }
                }
            }

            return true;
        }

        // L'électro-aimant (niveau 3) : le générateur électrique au pied de la tour.
        static bool AddMagnetGenerator(GameObject root, Structure structure)
        {
            GetParts(root, structure, out _, out var level3);
            var anchor = Anchor(level3, root, "Générateur");
            var generator = Model(k_Generator, anchor.transform, "Générateur");
            if (generator == null)
                return false;
            Fit(generator, 1f, new Vector3(-0.95f, 0f, -0.7f));
            return true;
        }

        // Le grand phare (niveau 3) : quatre lanternes autour de la lanterne du haut.
        static bool AddLighthouseLanterns(GameObject root, Structure structure)
        {
            GetParts(root, structure, out _, out var level3);
            var lantern = root.transform.Find("Lanterne");
            var top = lantern != null ? lantern.localPosition.y : Top(root);
            var anchor = Anchor(level3, root, "Lanternes");
            for (var i = 0; i < 4; i++)
            {
                var model = Model(k_Town + "lantern.fbx", anchor.transform, "Lanterne");
                if (model != null)
                    Fit(model, 0.3f, Quaternion.Euler(0f, 45f + i * 90f, 0f) * Vector3.forward * 0.55f + Vector3.up * (top - 0.45f));
            }

            return true;
        }

        // Un modèle ajouté à une pièce de niveau existante (ses noms dans le prefab), à cette largeur et cette place.
        static bool AddModel(GameObject root, string part, string path, string name, float width, Vector3 place)
        {
            var level = root.transform.Find(part);
            if (level == null)
                return false;
            var model = Model(path, level, name);
            if (model == null)
                return false;
            Fit(model, width, place);
            return true;
        }

        static void DestroyChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        static void DestroyChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null)
                Object.DestroyImmediate(child.gameObject);
        }

        // Tous les descendants de ce nom (les pièces d'une première version).
        static void DestroyNamed(Transform parent, string name)
        {
            var found = new System.Collections.Generic.List<GameObject>();
            foreach (var child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child != parent && child.name == name)
                    found.Add(child.gameObject);
            }

            foreach (var go in found)
            {
                if (go != null)
                    Object.DestroyImmediate(go);
            }
        }

        static bool UnderLevelPart(Transform transform, Transform stop)
        {
            for (var t = transform; t != null && t != stop; t = t.parent)
            {
                if (t.name.StartsWith("Niveau") || t.name == "Sur la tourelle")
                    return true;
            }

            return false;
        }

        // Le volume des rendus de ce modèle (sans les pièces de niveau), en mètres, dans le repère de space (sa place et son
        // orientation, sans son échelle).
        static Bounds LocalBounds(Transform space, Transform model)
        {
            var inverse = Quaternion.Inverse(space.rotation);
            var bounds = new Bounds(Vector3.up * 0.5f, Vector3.one);
            var any = false;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer || renderer is SkinnedMeshRenderer) || UnderLevelPart(renderer.transform, model))
                    continue;

                var world = renderer.bounds;
                for (var i = 0; i < 8; i++)
                {
                    var corner = world.center + Vector3.Scale(world.extents, new Vector3(i % 2 == 0 ? -1f : 1f, (i / 2) % 2 == 0 ? -1f : 1f, i < 4 ? -1f : 1f));
                    var local = inverse * (corner - space.position);
                    if (any)
                    {
                        bounds.Encapsulate(local);
                    }
                    else
                    {
                        bounds = new Bounds(local, Vector3.zero);
                        any = true;
                    }
                }
            }

            return bounds;
        }

        // L'orientation du modèle principal de la tourelle (son plus grand rendu), par rapport à elle : les copies du kit la prennent.
        static Quaternion ModelRotation(Transform turret)
        {
            Renderer biggest = null;
            var size = 0f;
            foreach (var renderer in turret.GetComponentsInChildren<Renderer>(true))
            {
                if (UnderLevelPart(renderer.transform, turret))
                    continue;
                var extent = renderer.bounds.size.sqrMagnitude;
                if (extent > size)
                {
                    size = extent;
                    biggest = renderer;
                }
            }

            var model = biggest != null ? biggest.transform : turret;
            while (model.parent != null && model.parent != turret && model != turret)
                model = model.parent;
            return Quaternion.Inverse(turret.rotation) * model.rotation;
        }

        // Une corde : un cylindre fin de a à b.
        static void Cord(Transform parent, Material material, Vector3 a, Vector3 b, float thickness)
        {
            var along = b - a;
            if (along.sqrMagnitude < 1e-4f)
                return;
            var cord = Primitive(PrimitiveType.Cylinder, parent, "Corde", material, (a + b) * 0.5f, new Vector3(thickness, along.magnitude * 0.5f, thickness));
            cord.transform.localRotation = Quaternion.FromToRotation(Vector3.up, along.normalized);
        }

        // ---------------------------------------------------------------- outils (comme TowerDefenseStep5Baker)

        static void SetIfStill(SerializedProperty array, float[] old, float[] values)
        {
            if (array == null || !array.isArray || array.arraySize != old.Length)
                return;
            for (var i = 0; i < old.Length; i++)
            {
                if (!Mathf.Approximately(array.GetArrayElementAtIndex(i).floatValue, old[i]))
                    return;
            }

            array.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++)
                array.GetArrayElementAtIndex(i).floatValue = values[i];
        }

        static GameObject Part(GameObject parent, string name)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent.transform, false);
            return part;
        }

        static GameObject Model(string path, Transform parent, string name)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null)
            {
                Debug.LogWarning("Évolutions des tours : modèle introuvable, " + path);
                return null;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent.gameObject.scene);
            go.name = name;
            go.transform.SetParent(parent, false);
            // Les bâtiments n'ont pas de collider : on vise leur emplacement.
            foreach (var collider in go.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider);
            return go;
        }

        // Le modèle à la largeur voulue (sa plus grande dimension au sol), posé au sol à cette place.
        static void Fit(GameObject go, float width, Vector3 place)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);
            var size = Mathf.Max(bounds.size.x, bounds.size.z);
            var scale = size > 1e-4f ? width / size : 1f;
            go.transform.localScale *= scale;
            var parent = go.transform.parent;
            var bottom = (bounds.min.y - go.transform.position.y) * scale;
            var center = (bounds.center - go.transform.position) * scale;
            go.transform.localPosition = place - (parent != null ? parent.InverseTransformVector(new Vector3(center.x, bottom, center.z)) : new Vector3(center.x, bottom, center.z));
        }

        static GameObject Box(Transform parent, string name, Material material, Vector3 position, Vector3 size) =>
            Primitive(PrimitiveType.Cube, parent, name, material, position, size);

        // Une forme simple, en mètres dans le repère du parent (les parents ici ne sont pas mis à l'échelle), sans collider.
        static GameObject Primitive(PrimitiveType type, Transform parent, string name, Material material, Vector3 position, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            SceneManager.MoveGameObjectToScene(go, parent.gameObject.scene);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
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
    }
}
