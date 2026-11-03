using System.Collections.Generic;
using Archery.Building;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le nouveau niveau 3 du Beffroi, <b>le Phare du passé</b> (2 novembre au soir, choisi par le joueur ;
    /// <c>Docs/Refonte_TowerDefense.md</c>, 12.45) : Minuit, l'arrêt du temps, passe à la Grande Horloge (l'ultime). Chaque étape une
    /// seule fois, avec sa condition de garde :
    /// <list type="bullet">
    /// <item>le matériau du faisceau, <c>Art/TowerDefense/Faisceau_du_passe.mat</c> (s'il manque) : la lumière transparente du
    /// Rayon du jugement, en sépia ;</item>
    /// <item>le prefab du Beffroi : ce matériau et la lanterne du sommet (l'enfant « Lanterne »), s'ils sont vides ;</item>
    /// <item>la fiche du Beffroi (<c>Data/Duos/Trio_Beffroi</c>) : le texte du niveau 3, s'il est encore celui de Minuit.</item>
    /// </list>
    /// </summary>
    [InitializeOnLoad]
    static class BeffroiPhareBaker
    {
        const string k_Prefab = "Assets/_Project/Prefabs/Structures/Duos/Trio_Beffroi.prefab";
        const string k_Definition = "Assets/_Project/Data/Duos/Trio_Beffroi.asset";
        const string k_Source = "Assets/_Project/Art/TowerDefense/Rayon_du_jugement.mat";
        const string k_Material = "Assets/_Project/Art/TowerDefense/Faisceau_du_passe.mat";
        const string k_LanternName = "Lanterne";

        const string k_OldText = "Minuit : douze coups ; le temps s'arrête 6 s dans tout le rayon, sauf pour toi. Tes flèches s'arrêtent " +
                                 "en l'air devant leurs cibles et, au douzième coup, repartent toutes ensemble.";

        const string k_NewText = "Le Phare du passé : la lanterne s'allume et son faisceau fait le tour de toute la carte ; chaque ennemi " +
                                 "qu'il balaie rembobine son chemin, puis reste figé.";

        static readonly Color k_Sepia = new Color(0.95f, 0.72f, 0.42f, 0.3f);
        static readonly Color k_SepiaGlow = new Color(1.6f, 1.05f, 0.5f, 1f);

        static BeffroiPhareBaker()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            var log = new List<string>();
            var material = BeamMaterial(log);
            UpdatePrefab(material, log);
            UpdateText(log);
            if (log.Count > 0)
                Debug.Log("Le Beffroi, son nouveau niveau 3, le Phare du passé (2 novembre au soir) : " + string.Join(" ; ", log) +
                          ". Minuit (l'arrêt du temps) passe à la Grande Horloge, l'ultime.", AssetDatabase.LoadAssetAtPath<GameObject>(k_Prefab));
        }

        static Material BeamMaterial(List<string> log)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(k_Material);
            if (material != null)
                return material;

            var source = AssetDatabase.LoadAssetAtPath<Material>(k_Source);
            if (source == null)
                return null;

            material = new Material(source) { name = "Faisceau_du_passe" };
            material.SetColor("_BaseColor", k_Sepia);
            material.SetColor("_EmissionColor", k_SepiaGlow);
            AssetDatabase.CreateAsset(material, k_Material);
            AssetDatabase.SaveAssets();
            log.Add("matériau du faisceau créé (" + k_Material + ")");
            return material;
        }

        static void UpdatePrefab(Material material, List<string> log)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(k_Prefab) == null)
                return;

            var root = PrefabUtility.LoadPrefabContents(k_Prefab);
            try
            {
                var beffroi = root.GetComponent<TrioBeffroi>();
                if (beffroi == null)
                    return;

                var serialized = new SerializedObject(beffroi);
                var changed = false;
                var beam = serialized.FindProperty("m_BeamMaterial");
                if (material != null && beam != null && beam.objectReferenceValue == null)
                {
                    beam.objectReferenceValue = material;
                    log.Add("Trio_Beffroi.m_BeamMaterial : Faisceau_du_passe");
                    changed = true;
                }

                var lantern = serialized.FindProperty("m_Lantern");
                var found = Find(root.transform, k_LanternName);
                if (lantern != null && lantern.objectReferenceValue == null && found != null)
                {
                    lantern.objectReferenceValue = found;
                    log.Add("Trio_Beffroi.m_Lantern : " + k_LanternName);
                    changed = true;
                }

                if (!changed)
                    return;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, k_Prefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static Transform Find(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;
                var deeper = Find(child, name);
                if (deeper != null)
                    return deeper;
            }

            return null;
        }

        static void UpdateText(List<string> log)
        {
            var definition = AssetDatabase.LoadAssetAtPath<ScriptableObject>(k_Definition);
            if (definition == null)
                return;

            var serialized = new SerializedObject(definition);
            var text = serialized.FindProperty("levels.Array.data[2].description");
            if (text == null || text.stringValue != k_OldText)
                return;

            text.stringValue = k_NewText;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssetIfDirty(definition);
            log.Add("texte du niveau 3 : « " + k_NewText + " »");
        }
    }
}
