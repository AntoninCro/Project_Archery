using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// L'équilibrage des tours, des duos et des trios (2 novembre) : les changements proposés par Claude dans le tableau
    /// <c>Docs/Equilibrage_Tours.xlsx</c> (ligne « Équilibrage » de chaque bâtiment), retenus par le joueur (une case vide : ceux de
    /// Claude), avec les siens : les piques −30 % de dégâts en plus, la singularité toutes les 10 s, le phare et son anneau plus grand,
    /// le Feu d'artifice qui éblouit plus longtemps au lieu de faire plus de dégâts, le Beffroi sans dégâts mais avec sa bulle de Minuit
    /// (<c>Docs/Refonte_TowerDefense.md</c>, 12.35). Chaque réglage n'est changé que s'il a encore sa valeur d'avant : une seule fois,
    /// et jamais un réglage retouché à la main ensuite. Aussi la texture du trou noir de la singularité (<c>Tools/texture_trou_noir.py</c>)
    /// sur son matériau, s'il n'en a pas encore.
    /// </summary>
    [InitializeOnLoad]
    static class BalanceBaker
    {
        const string k_Folder = "Assets/_Project/Prefabs/Structures/";
        const string k_BlackHole = "Assets/_Project/Art/TowerDefense/Trou_noir.mat";
        const string k_BlackHoleTexture = "Assets/_Project/Art/TowerDefense/Trou_noir.png";
        const string k_BubbleMaterial = "Assets/_Project/Art/TowerDefense/Bulle.mat";

        // Un réglage : le prefab, le composant, le champ, sa valeur d'avant et la nouvelle (un nombre, ou un tableau par niveau).
        struct Change
        {
            public string Prefab;
            public string Component;
            public string Field;
            public float[] Old;
            public float[] New;
            public bool Array;
        }

        static Change A(string prefab, string component, string field, float[] old, float[] @new) =>
            new Change { Prefab = prefab, Component = component, Field = field, Old = old, New = @new, Array = true };

        static Change S(string prefab, string component, string field, float old, float @new) =>
            new Change { Prefab = prefab, Component = component, Field = field, Old = new[] { old }, New = new[] { @new } };

        static readonly Change[] k_Changes =
        {
            // Les tours.
            A("Pieges", "SpikeTrap", "m_DamagePerLevel", new[] { 12f, 18f, 27f, 30f, 34f }, new[] { 8.4f, 12.6f, 18.9f, 21f, 23.8f }),
            A("Pieges", "SpikeTrap", "m_IntervalPerLevel", new[] { 0.8f, 0.65f, 0.5f }, new[] { 0.8f, 0.7f, 0.6f }),
            S("Pieges", "SpikeTrap", "m_BleedDamage", 4f, 2.8f),
            S("Pieges", "SpikeTrap", "m_MineDamage", 60f, 42f),
            S("Pieges", "SpikeTrap", "m_JawDamage", 120f, 84f),
            A("Tremplin", "StructureTrampoline", "m_CooldownPerLevel", new[] { 3f, 2.2f, 3.5f, 3f, 2f }, new[] { 3f, 2.2f, 3.5f, 3f, 4f }),
            S("Tremplin", "StructureTrampoline", "m_RampMax", 6f, 4f),
            A("Baliste", "Ballista", "m_DamagePerLevel", new[] { 10f, 14.67f, 21.33f, 26f, 32f }, new[] { 18f, 24f, 26f, 30f, 32f }),
            S("Baliste", "Ballista", "m_TitanDamageScale", 6f, 3f),
            A("Mortier", "StructureMortar", "m_DamagePerLevel", new[] { 30f, 42f, 60f, 70f, 85f }, new[] { 30f, 42f, 42f, 50f, 60f }),
            A("Givre", "FrostTower", "m_SpeedPerLevel", new[] { 0.7f, 0.6f, 0.5f }, new[] { 0.7f, 0.55f, 0.5f }),
            S("Filet", "StructureNetLauncher", "m_ShockDamage", 15f, 35f),
            S("Filet", "StructureNetLauncher", "m_ShockStun", 2f, 2.5f),
            A("Filet", "StructureNetLauncher", "m_NetsPerLevel", new[] { 1f, 1f, 2f }, new[] { 1f, 1f, 2f, 2f, 3f }),
            A("Filet", "StructureNetLauncher", "m_IntervalPerLevel", new[] { 5f, 4.2f, 3.5f }, new[] { 5f, 4.2f, 3.5f, 3.5f, 3f }),
            A("Aimant", "StructureMagnet", "m_PullPerLevel", new[] { 3f, 3.5f, 4f }, new[] { 4f, 4.5f, 4f }),
            A("Aimant", "StructureMagnet", "m_IntervalPerLevel", new[] { 3.5f, 3f, 2.5f }, new[] { 3f, 2.5f, 2.5f }),
            S("Aimant", "StructureMagnet", "m_ContinuousPull", 1.6f, 2.2f),
            S("Aimant", "StructureMagnet", "m_SingularityInterval", 15f, 10f),
            A("Bulle", "StructureBubble", "m_IntervalPerLevel", new[] { 7f, 6f, 5f }, new[] { 6f, 6f, 5f }),
            A("Phare", "StructureLighthouse", "m_SupportDamagePerLevel", new[] { 1.2f, 1.2f, 1.3f, 1.3f, 1.4f },
              new[] { 1.2f, 1.2f, 1.3f, 1.35f, 1.4f }),
            A("Phare", "StructureLighthouse", "m_SupportRatePerLevel", new[] { 1f, 1.15f, 1.25f, 1.25f, 1.35f },
              new[] { 1f, 1.15f, 1.25f, 1.3f, 1.35f }),
            S("Phare", "StructureLighthouse", "m_JudgmentBoost.x", 2f, 1.5f),
            S("Phare", "StructureLighthouse", "m_RingRadius", 1.3f, 2.2f),
            // Les duos.
            A("Duos/Duo_Marteau_pilon", "DuoMarteauPilon", "m_DamagePerLevel", new[] { 45f, 60f, 70f }, new[] { 120f, 150f, 180f }),
            A("Duos/Duo_Marteau_pilon", "DuoMarteauPilon", "m_IntervalPerLevel", new[] { 3f, 2.4f, 0.8f }, new[] { 2f, 1.6f, 0.8f }),
            A("Duos/Duo_Yoyo", "DuoYoyo", "m_SlamDamagePerLevel", new[] { 30f, 40f, 45f }, new[] { 30f, 40f, 70f }),
            S("Duos/Duo_Yoyo", "DuoYoyo", "m_FlingSmashShare", 0.45f, 0.6f),
            A("Duos/Duo_Avalanche", "DuoAvalanche", "m_IntervalPerLevel", new[] { 6f, 5f, 11f }, new[] { 6f, 5f, 8f }),
            S("Duos/Duo_Avalanche", "DuoAvalanche", "m_GiantShare", 0.5f, 0.6f),
            A("Duos/Duo_Feu_d_artifice", "DuoFeuArtifice", "m_DazzleTimePerLevel", new[] { 2f, 2.2f, 3f }, new[] { 2.5f, 2.8f, 3.6f }),
            S("Duos/Duo_Feu_d_artifice", "DuoFeuArtifice", "m_DazzleRadius", 5f, 6f),
            A("Duos/Duo_Chant_des_sirenes", "DuoSirenes", "m_HitDamagePerLevel", new[] { 20f, 26f, 24f }, new[] { 45f, 55f, 65f }),
            A("Duos/Duo_Chant_des_sirenes", "DuoSirenes", "m_TargetsPerLevel", new[] { 1f, 2f, 2f }, new[] { 2f, 2f, 2f }),
            A("Duos/Duo_Carquois_geant", "DuoCarquoisGeant", "m_VolleyAtPerLevel", new[] { 10f, 10f, 30f }, new[] { 6f, 6f, 20f }),
            A("Duos/Duo_Carquois_geant", "DuoCarquoisGeant", "m_BoltDamagePerLevel", new[] { 30f, 32f, 34f }, new[] { 50f, 55f, 60f }),
            S("Duos/Duo_Chaine_de_forcats", "DuoChaineForcats", "m_Share", 0.6f, 0.4f),
            A("Duos/Duo_Boussole_folle", "DuoBoussole", "m_SpikeDamagePerLevel", new[] { 7f, 9f, 11f }, new[] { 10f, 14f, 18f }),
            A("Duos/Duo_Lanceur_baudruches", "DuoBaudruches", "m_ConfettiDamagePerLevel", new[] { 35f, 45f, 50f }, new[] { 90f, 110f, 130f }),
            A("Duos/Duo_Lanceur_baudruches", "DuoBaudruches", "m_IntervalPerLevel", new[] { 7f, 6f, 6f }, new[] { 6f, 5f, 5f }),
            S("Duos/Duo_Lanceur_baudruches", "DuoBaudruches", "m_FloatDamage", 60f, 100f),
            A("Duos/Duo_Boule_a_neige", "DuoBouleNeige", "m_CrackDamagePerLevel", new[] { 25f, 35f, 35f }, new[] { 70f, 90f, 100f }),
            A("Duos/Duo_Boule_a_neige", "DuoBouleNeige", "m_IntervalPerLevel", new[] { 9f, 8f, 8f }, new[] { 7f, 6f, 6f }),
            S("Duos/Duo_Boule_a_neige", "DuoBouleNeige", "m_GiantCrackShare", 0.4f, 0.6f),
            S("Duos/Duo_Boule_a_neige", "DuoBouleNeige", "m_GiantInterval", 18f, 12f),
            // Les trios.
            A("Duos/Trio_Enclume", "TrioEnclume", "m_DamagePerLevel", new[] { 40f, 55f, 70f }, new[] { 200f, 240f, 280f }),
            S("Duos/Trio_Enclume", "TrioEnclume", "m_SplashRadius", 1.4f, 2.5f),
            A("Duos/Trio_Enclume", "TrioEnclume", "m_IntervalPerLevel", new[] { 4f, 3.2f, 2.4f }, new[] { 2.5f, 2.2f, 2f }),
            A("Duos/Trio_Gonfleur", "TrioGonfleur", "m_HosesPerLevel", new[] { 1f, 2f, 2f }, new[] { 3f, 4f, 5f }),
            A("Duos/Trio_Gonfleur", "TrioGonfleur", "m_IntervalPerLevel", new[] { 3.5f, 3f, 2.5f }, new[] { 2.5f, 2f, 1.5f }),
            A("Duos/Trio_Gonfleur", "TrioGonfleur", "m_HarpoonDamagePerLevel", new[] { 15f, 20f, 25f }, new[] { 40f, 50f, 60f }),
            S("Duos/Trio_Gonfleur", "TrioGonfleur", "m_FloatHeight", 7f, 10f),
            S("Duos/Trio_Gonfleur", "TrioGonfleur", "m_ParadeHeight", 10f, 15f),
            A("Duos/Trio_Bowling", "TrioBowling", "m_DamagePerLevel", new[] { 45f, 60f, 75f }, new[] { 150f, 200f, 250f }),
            S("Duos/Trio_Beffroi", "TrioBeffroi", "m_RewindSeconds", 4f, 8f),
            A("Duos/Trio_Beffroi", "TrioBeffroi", "m_IntervalPerLevel", new[] { 10f, 8.5f, 7f }, new[] { 6f, 5f, 4f }),
            S("Duos/Trio_Beffroi", "TrioBeffroi", "m_ArrivalStun", 2f, 3f),
            S("Duos/Trio_Beffroi", "TrioBeffroi", "m_MidnightInterval", 30f, 20f),
            S("Duos/Trio_Beffroi", "TrioBeffroi", "m_MidnightTime", 6f, 9f),
            S("Duos/Trio_Beffroi", "TrioBeffroi", "m_MidnightRadius", 20f, 300f),
            S("Duos/Trio_Colisee", "TrioColisee", "m_ChampionTime", 30f, 12f),
            A("Duos/Trio_Colisee", "TrioColisee", "m_ChampionsPerLevel", new[] { 1f, 2f, 2f }, new[] { 2f, 3f, 4f }),
            A("Duos/Trio_Colisee", "TrioColisee", "m_IntervalPerLevel", new[] { 15f, 13f, 12f }, new[] { 10f, 8f, 6f }),
            A("Duos/Trio_Colisee", "TrioColisee", "m_ChampionDamagePerLevel", new[] { 30f, 40f, 50f }, new[] { 60f, 80f, 100f }),
            A("Duos/Trio_Bombarde", "TrioBombarde", "m_BombDamagePerLevel", new[] { 45f, 55f, 65f }, new[] { 120f, 140f, 160f }),
            S("Duos/Trio_Bombarde", "TrioBombarde", "m_KillCharge", 0.08f, 0.12f),
            A("Duos/Trio_Montagnes_russes", "TrioMontagnesRusses", "m_PickDamagePerLevel", new[] { 15f, 20f, 25f }, new[] { 40f, 50f, 60f }),
            S("Duos/Trio_Montagnes_russes", "TrioMontagnesRusses", "m_WhipDamage", 20f, 40f),
            S("Duos/Trio_Montagnes_russes", "TrioMontagnesRusses", "m_FlingLift", 4.5f, 7f),
        };

        static BalanceBaker()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            var log = new List<string>();
            var byPrefab = new Dictionary<string, List<Change>>();
            foreach (var change in k_Changes)
            {
                if (!byPrefab.TryGetValue(change.Prefab, out var list))
                    byPrefab[change.Prefab] = list = new List<Change>();
                list.Add(change);
            }

            foreach (var pair in byPrefab)
                ApplyToPrefab(pair.Key, pair.Value, log);

            BeffroiBubble(log);
            BlackHoleTexture(log);
            NoWaveScaling(log);
            DifficultyPower(log);
            if (log.Count > 0)
                Debug.Log($"Équilibrage du 2 novembre (Docs/Equilibrage_Tours.xlsx) : {log.Count} réglage(s) changé(s) :\n" + string.Join("\n", log));
        }

        static void ApplyToPrefab(string prefab, List<Change> changes, List<string> log)
        {
            var path = k_Folder + prefab + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                return;

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var changed = false;
                foreach (var change in changes)
                {
                    var component = root.GetComponent(change.Component);
                    if (component == null)
                        continue;

                    var serialized = new SerializedObject(component);
                    var property = serialized.FindProperty(change.Field);
                    if (property == null || !Matches(property, change))
                        continue;

                    Set(property, change);
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                    log.Add($"{prefab.Replace("Duos/", "")}.{change.Field} : {Text(change.Old)} → {Text(change.New)}");
                }

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static bool Matches(SerializedProperty property, Change change)
        {
            if (!change.Array)
                return Approximately(Value(property), change.Old[0]);

            if (!property.isArray || property.arraySize != change.Old.Length)
                return false;
            for (var i = 0; i < change.Old.Length; i++)
            {
                if (!Approximately(Value(property.GetArrayElementAtIndex(i)), change.Old[i]))
                    return false;
            }

            return true;
        }

        static void Set(SerializedProperty property, Change change)
        {
            if (!change.Array)
            {
                SetValue(property, change.New[0]);
                return;
            }

            property.arraySize = change.New.Length;
            for (var i = 0; i < change.New.Length; i++)
                SetValue(property.GetArrayElementAtIndex(i), change.New[i]);
        }

        static float Value(SerializedProperty property) =>
            property.propertyType == SerializedPropertyType.Integer ? property.intValue : property.floatValue;

        static void SetValue(SerializedProperty property, float value)
        {
            if (property.propertyType == SerializedPropertyType.Integer)
                property.intValue = Mathf.RoundToInt(value);
            else
                property.floatValue = value;
        }

        static bool Approximately(float a, float b) => Mathf.Abs(a - b) < 0.006f;

        static string Text(float[] values) => string.Join(" / ", System.Array.ConvertAll(values, v => v.ToString("0.##")));

        // Le joueur, le 2 novembre : « on enlève le scale des tourelles par vagues » ; elles grandissent avec ses parchemins
        // (Structure.ScrollStrength). Le suivi de la force des ennemis passe de 0,5 (sa racine carrée, 1er novembre) à 0.
        static void NoWaveScaling(List<string> log)
        {
            var settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Project/Data/Waves/WaveSettings.asset");
            if (settings == null)
                return;

            var serialized = new SerializedObject(settings);
            var property = serialized.FindProperty("structureStrengthExponent");
            if (property == null || !Approximately(property.floatValue, 0.5f))
                return;

            property.floatValue = 0f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
            log.Add("WaveSettings.structureStrengthExponent : 0,5 → 0 (les tourelles ne suivent plus les vagues)");
        }

        // Sans le suivi des vagues, les tours ne suivaient plus les PV des ennemis de la difficulté, qui grimpent bien plus vite en
        // Difficile et en Impossible (×6,6 et ×11,6 à la vague 5, ×3,2 en Normal) : à la vague 5, elles y faisaient 0,58 et 0,44 fois
        // leur force d'avant ; en Facile, 1,37. La puissance des bâtiments de chaque difficulté est revue (2 novembre, validé par le
        // joueur) : 0,5 / 0,8 / 1,3 / 1,9. Puis, le même soir, après la simulation des trois façons de jouer (le chemin des tours
        // écrasait celui de l'arc), le joueur : Normal 0,8 → 0,5, et Difficile et Impossible dans la même proportion (×0,625) ; le
        // Facile ne change pas. Chaque valeur d'avant (d'origine ou de la première étape) passe à la nouvelle.
        static readonly (string Asset, float[] Old, float New)[] k_Powers =
        {
            ("Difficulty_facile", new[] { 0.6f }, 0.5f),
            ("Difficulty_Normal", new[] { 0.8f }, 0.5f),
            ("Difficulty_Difficile", new[] { 1f, 1.3f }, 0.81f),
            ("Difficulty_Impossible", new[] { 1.28f, 1.9f }, 1.19f),
        };

        static void DifficultyPower(List<string> log)
        {
            foreach (var power in k_Powers)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/_Project/Data/Difficulties/" + power.Asset + ".asset");
                if (asset == null)
                    continue;

                var serialized = new SerializedObject(asset);
                var property = serialized.FindProperty("structurePower");
                if (property == null || !System.Array.Exists(power.Old, old => Approximately(property.floatValue, old)))
                    continue;

                var before = property.floatValue;
                property.floatValue = power.New;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssetIfDirty(asset);
                log.Add($"{power.Asset.Replace("Difficulty_", "")}.structurePower : {before:0.##} → {power.New:0.##}");
            }
        }

        // La bulle de Minuit du Beffroi : le verre de la bulle, si elle n'a pas encore de matériau.
        static void BeffroiBubble(List<string> log)
        {
            const string path = k_Folder + "Duos/Trio_Beffroi.prefab";
            var material = AssetDatabase.LoadAssetAtPath<Material>(k_BubbleMaterial);
            if (material == null || AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                return;

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var component = root.GetComponent("TrioBeffroi");
                if (component == null)
                    return;
                var serialized = new SerializedObject(component);
                var property = serialized.FindProperty("m_BubbleMaterial");
                if (property == null || property.objectReferenceValue != null)
                    return;

                property.objectReferenceValue = material;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                log.Add("Trio_Beffroi.m_BubbleMaterial : Bulle");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // Le trou noir de la singularité (le joueur : « une vraie texture ») : la texture en couleur et en émission, s'il n'en a pas.
        static void BlackHoleTexture(List<string> log)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(k_BlackHole);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(k_BlackHoleTexture);
            if (material == null || texture == null || !material.HasProperty("_BaseMap") || material.GetTexture("_BaseMap") != null)
                return;

            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", new Color(0.45f, 0.45f, 0.45f, 1f));
            if (material.HasProperty("_EmissionMap"))
            {
                material.SetTexture("_EmissionMap", texture);
                material.SetColor("_EmissionColor", new Color(1.6f, 1.6f, 1.6f, 1f));
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
            log.Add("Trou_noir.mat : la texture Trou_noir.png (couleur et émission)");
        }
    }
}
