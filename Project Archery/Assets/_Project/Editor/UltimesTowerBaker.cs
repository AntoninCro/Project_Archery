using System.Collections.Generic;
using Archery.Building;
using Archery.Combat;
using Archery.Defense;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le 4 novembre, le joueur, après l'avis général sur le jeu (<c>Docs/Refonte_TowerDefense.md</c>, 12.62). Chaque réglage n'est
    /// changé que s'il a encore sa valeur d'avant : une seule fois, et jamais un réglage retouché à la main.
    /// <list type="bullet">
    /// <item>« nerf les dégâts sur toutes les ultimes de 30 %, pas que le carrousel » : le Colosse de forge et la Grande Horloge
    /// −30 % sur tous leurs dégâts (le Grand Manège a déjà eu −35 % au troisième essai, <c>ThirdTestBaker</c>) ; les parts des PV
    /// des boss ne changent pas, comme pour le Manège (MK ne change pas) ;</item>
    /// <item>« la tour devrait avoir 2 fois moins de PV » (dans la partie du 3 novembre, elle a perdu 77 PV sur 4 875 : aucun
    /// enjeu) : 3 000 → 1 500 PV de base, dans la scène, à enregistrer (Ctrl+S). La Vigueur s'y ajoute toujours en part.</item>
    /// </list>
    /// Il passe un tour d'éditeur après les autres, comme <c>ColosseForceBaker</c> (le baker de l'allure refait les prefabs au
    /// premier tour).
    /// </summary>
    [InitializeOnLoad]
    static class UltimesTowerBaker
    {
        const string k_Colosse = "Assets/_Project/Prefabs/Structures/Duos/Ultime_Colosse.prefab";
        const string k_Horloge = "Assets/_Project/Prefabs/Structures/Duos/Ultime_Horloge.prefab";
        const float k_TowerOld = 3000f;
        const float k_TowerNew = 1500f;

        // Les dégâts du Colosse et leur valeur d'avant (×0,7, arrondi).
        static readonly (string Field, float Old, float New)[] k_ColosseDamage =
        {
            ("m_WakeDamage", 100f, 70f),
            ("m_ChunkDamage", 70f, 49f),
            ("m_Damage", 450f, 315f),
            ("m_ScorchBurn", 15f, 11f),
            ("m_HammerDamage", 160f, 112f),
            ("m_FlowDamage", 90f, 63f),
            ("m_FlowBurn", 50f, 35f),
            ("m_RiverBurn", 30f, 21f),
        };

        // Ceux de l'Horloge.
        static readonly (string Field, float Old, float New)[] k_HorlogeDamage =
        {
            ("m_WakeDamage", 100f, 70f),
            ("m_NumeralDamage", 220f, 154f),
            ("m_ShatterDamage", 500f, 350f),
            ("m_EchoDamage", 160f, 112f),
        };

        static UltimesTowerBaker()
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
            UpdateUltimate<UltimeColosse>(k_Colosse, "Ultime_Colosse", k_ColosseDamage, log);
            UpdateUltimate<UltimeHorloge>(k_Horloge, "Ultime_Horloge", k_HorlogeDamage, log);
            var scene = UpdateTower(log);
            if (log.Count == 0)
                return;
            Debug.Log($"Les ultimes et la tour (4 novembre) : {log.Count} réglage(s) changé(s) :\n" + string.Join("\n", log) +
                      (scene ? "\n→ La scène a changé : enregistre-la (Ctrl+S)." : ""));
        }

        static void UpdateUltimate<T>(string path, string name, (string Field, float Old, float New)[] values, List<string> log)
            where T : Component
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                return;

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var ultimate = root.GetComponent<T>();
                if (ultimate == null)
                    return;
                var serialized = new SerializedObject(ultimate);
                var changed = false;
                foreach (var damage in values)
                    changed |= Replace(serialized, damage.Field, damage.Old, damage.New, $"{name}.{damage.Field} : {damage.Old:0} → {damage.New:0}", log);
                if (!changed)
                    return;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // La tour de la scène ouverte : ses PV de base. Vrai si la scène a changé.
        static bool UpdateTower(List<string> log)
        {
            var tower = Object.FindAnyObjectByType<Tower>(FindObjectsInactive.Include);
            if (tower == null || EditorUtility.IsPersistent(tower) || !tower.TryGetComponent<Health>(out var health))
                return false;

            var serialized = new SerializedObject(health);
            if (!Replace(serialized, "m_MaxHealth", k_TowerOld, k_TowerNew, $"Tour (Health.m_MaxHealth) : {k_TowerOld:0} → {k_TowerNew:0} PV", log))
                return false;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(tower.gameObject.scene);
            return true;
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
    }
}
