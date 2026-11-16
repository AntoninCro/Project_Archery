using System.IO;
using Archery.Building;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// La troisième bouche du Mortier (16 novembre, la refonte visuelle : <c>Docs/Refonte_Visuelle.md</c>). Le Mortier du joueur
    /// a un canon aux niveaux 1 et 2, remplacé au niveau 3 par deux canons côte à côte ; il fallait trois bouches : celle du canon
    /// du niveau 1 (« Bouche », qui disparaît au niveau 3 avec lui, <see cref="LevelHide"/>), et une par canon du niveau 3
    /// (« Bouche 2 », et « Bouche 3 », ajoutée ici à côté d'elle et à la liste des bouches du mortier). Une bouche cachée ne tire
    /// pas (<c>StructureMortar</c>). Leur place vient de Blender (les repères, posés par Claude à la bouche des canons du joueur) :
    /// le modèle est ensuite rebranché (<c>Editor/ModelesFaitMain</c>).
    /// </summary>
    /// <remarks>
    /// Ne touche pas au modèle (règle « Fait main ») : un objet vide du code et la liste qui le désigne, une seule fois (tant que
    /// « Bouche 3 » n'existe pas).
    /// </remarks>
    [InitializeOnLoad]
    static class MortierBoucheBaker
    {
        const string k_Prefab = "Assets/_Project/Prefabs/Structures/Mortier.prefab";
        const string k_Sidecar = "Assets/_Project/Art/Modeles/Mortier.json";
        const string k_Tube = "Affût/Niveau 3/Tube";
        const string k_Name = "Bouche 3";

        static MortierBoucheBaker()
        {
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(k_Prefab))
                return;
            var root = PrefabUtility.LoadPrefabContents(k_Prefab);
            var added = false;
            try
            {
                var mortar = root.GetComponent<StructureMortar>();
                var tube = root.transform.Find(k_Tube);
                if (mortar == null || tube == null)
                {
                    Debug.LogWarning("Mortier, troisième bouche : « " + k_Tube + " » ou le script du mortier introuvable, rien fait.");
                    return;
                }
                if (tube.Find(k_Name) != null)
                    return;

                var muzzle = new GameObject(k_Name).transform;
                muzzle.SetParent(tube, false);
                var second = tube.Find("Bouche 2");
                if (second != null)
                {
                    muzzle.localPosition = second.localPosition;
                    muzzle.localRotation = second.localRotation;
                }
                var so = new SerializedObject(mortar);
                var list = so.FindProperty("m_Muzzles");
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = muzzle;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, k_Prefab);
                added = true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            if (!added)
                return;
            Debug.Log("Mortier : troisième bouche ajoutée (« " + k_Name + " », pour le deuxième canon du niveau 3).");
            // le modèle du joueur rebranché : la bouche prend sa place, celle du canon du niveau 1 disparaît au niveau 3
            if (File.Exists(k_Sidecar))
                AssetDatabase.ImportAsset(k_Sidecar, ImportAssetOptions.ForceUpdate);
        }
    }
}
