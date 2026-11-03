using System.Collections.Generic;
using Archery.Building;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Les sons du Colosse de forge (2 novembre au soir, trouvés sur freesound.org, CC0 ; <c>Docs/Refonte_TowerDefense.md</c>, 12.46 et
    /// 12.47, et <c>Docs/Credits_Licences.md</c>) : la fournaise qui déborde, le sol qui grésille, la coulée en boucle ; puis le
    /// tonnerre des éclairs et le grand impact. Posés sur le prefab <c>Prefabs/Structures/Duos/Ultime_Colosse</c>, chacun une seule
    /// fois : seulement si son fichier existe et que le réglage est encore vide, ou encore un son provisoire (<c>fire_loop</c>,
    /// <c>explosion</c>). Le grand impact se cherche par son numéro freesound dans <c>Audio/</c> (le joueur l'a téléchargé tel quel).
    /// </summary>
    /// <remarks>
    /// Le prefab est fait par <see cref="TowerDefenseLookBaker"/> ; ce baker passe après lui (un tour d'éditeur plus tard), pour le
    /// cas où le prefab a été fait avant que les sons soient dans le projet (c'est arrivé le 2 novembre au soir : Unity a fait le
    /// prefab pendant que le joueur téléchargeait les sons). Il passe aussi quand l'un des sons est importé (<see cref="Imports"/>) :
    /// ajouter un son ne recharge pas les scripts.
    /// </remarks>
    [InitializeOnLoad]
    static class ColosseSonsBaker
    {
        // Un son du Colosse importé (ou réimporté) : le baker passe au tour suivant.
        sealed class Imports : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
            {
                foreach (var path in imported)
                {
                    if (System.Array.Exists(k_Clips, c => Matches(c.Clip, path)))
                    {
                        EditorApplication.delayCall -= Run;
                        EditorApplication.delayCall += Run;
                        return;
                    }
                }
            }
        }

        const string k_Prefab = "Assets/_Project/Prefabs/Structures/Duos/Ultime_Colosse.prefab";
        const string k_Audio = "Assets/_Project/Audio/";

        static readonly string[] k_Placeholders =
        {
            "Assets/_Project/Audio/Placeholder/fire_loop.wav",
            "Assets/_Project/Audio/Placeholder/explosion.wav",
        };

        // Le chemin du son, ou « Audio/<numéro>__* » : le premier fichier de Audio/ qui commence ainsi.
        static readonly (string Field, string Clip)[] k_Clips =
        {
            ("m_PourClip", k_Audio + "Colosse_Fournaise.wav"),
            ("m_SizzleClip", k_Audio + "Colosse_Gresillement.wav"),
            ("m_FlowClip", k_Audio + "Colosse_Coulee.wav"),
            ("m_ThunderClip", k_Audio + "Colosse_Tonnerre.wav"),
            ("m_BoomClip", k_Audio + "513110__*"),
        };

        static bool Matches(string clip, string path) =>
            clip.EndsWith("*") ? path.StartsWith(clip.TrimEnd('*')) && !path.EndsWith(".meta") : path == clip;

        static string Resolve(string clip)
        {
            if (!clip.EndsWith("*"))
                return clip;
            var folder = System.IO.Path.GetDirectoryName(clip);
            if (!System.IO.Directory.Exists(folder))
                return null;
            foreach (var file in System.IO.Directory.GetFiles(folder, System.IO.Path.GetFileName(clip)))
            {
                if (!file.EndsWith(".meta"))
                    return file.Replace('\\', '/');
            }

            return null;
        }

        static ColosseSonsBaker()
        {
            EditorApplication.delayCall -= Later;
            EditorApplication.delayCall += Later;
        }

        // Après le baker de l'allure, qui fait le prefab au même tour.
        static void Later()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer ||
                AssetDatabase.LoadAssetAtPath<GameObject>(k_Prefab) == null)
                return;

            var placeholders = new List<Object>();
            foreach (var path in k_Placeholders)
                placeholders.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(path));
            var log = new List<string>();
            var root = PrefabUtility.LoadPrefabContents(k_Prefab);
            try
            {
                var colosse = root.GetComponent<UltimeColosse>();
                if (colosse == null)
                    return;

                var serialized = new SerializedObject(colosse);
                foreach (var (field, wanted) in k_Clips)
                {
                    var path = Resolve(wanted);
                    var clip = path != null ? AssetDatabase.LoadAssetAtPath<AudioClip>(path) : null;
                    var property = serialized.FindProperty(field);
                    if (clip == null || property == null || property.objectReferenceValue == clip)
                        continue;
                    if (property.objectReferenceValue != null && !placeholders.Contains(property.objectReferenceValue))
                        continue;

                    property.objectReferenceValue = clip;
                    log.Add(field + " : " + System.IO.Path.GetFileName(path));
                }

                if (log.Count == 0)
                    return;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, k_Prefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            Debug.Log("Les sons du Colosse de forge (2 novembre au soir) : " + string.Join(" ; ", log) + ".",
                      AssetDatabase.LoadAssetAtPath<GameObject>(k_Prefab));
        }
    }
}
