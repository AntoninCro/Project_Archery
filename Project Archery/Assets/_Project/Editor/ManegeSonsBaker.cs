using System.Collections.Generic;
using Archery.Building;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Les sons du Grand Manège (2 novembre, tard ; <c>Docs/Refonte_TowerDefense.md</c>, 12.51) : l'orgue en boucle, la fanfare et sa
    /// première phrase, le galop en boucle, le hennissement, la fusée qui monte, le bouquet qui éclate. Choisis par le joueur sur
    /// freesound.org (CC0) et téléchargés dans <c>Audio/</c> ; découpés par Claude en <c>Audio/Manege_*.wav</c> (2 novembre, tard ;
    /// <c>Docs/Credits_Licences.md</c>). Le fichier d'allure les nomme déjà ; ce baker les pose sur le prefab
    /// <c>Prefabs/Structures/Duos/Ultime_Manege</c> s'il a été fait avant eux, chacun une seule fois : seulement si son fichier existe et
    /// que le réglage est encore vide, ou encore le son provisoire du projet.
    /// </summary>
    /// <remarks>
    /// Comme <see cref="ColosseSonsBaker"/> (la leçon de 12.46 : Unity fait le prefab avant que les sons soient là) : il passe un tour
    /// d'éditeur après le baker de l'allure, et quand l'un des sons est importé (<see cref="Imports"/>).
    /// </remarks>
    [InitializeOnLoad]
    static class ManegeSonsBaker
    {
        // Un son du Manège importé (ou réimporté) : le baker passe au tour suivant.
        sealed class Imports : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
            {
                foreach (var path in imported)
                {
                    if (System.Array.Exists(k_Clips, c => c.Clip == path))
                    {
                        EditorApplication.delayCall -= Run;
                        EditorApplication.delayCall += Run;
                        return;
                    }
                }
            }
        }

        const string k_Prefab = "Assets/_Project/Prefabs/Structures/Duos/Ultime_Manege.prefab";
        const string k_Audio = "Assets/_Project/Audio/";

        // Le réglage, son fichier, et le son provisoire qu'il remplace (null : le réglage est vide en attendant).
        static readonly (string Field, string Clip, string Placeholder)[] k_Clips =
        {
            ("m_OrganClip", k_Audio + "Manege_Orgue.wav", null),
            ("m_FanfareClip", k_Audio + "Manege_Fanfare.wav", k_Audio + "Placeholder/wave_start_horn.wav"),
            ("m_FlourishClip", k_Audio + "Manege_Fanfare_Courte.wav", null),
            ("m_GallopClip", k_Audio + "Manege_Galop.wav", null),
            ("m_NeighClip", k_Audio + "Manege_Hennissement.wav", null),
            ("m_RocketClip", k_Audio + "Manege_Fusee.wav", "Assets/650564__h2p34__lightsaber-whoosh.wav"),
            ("m_BurstClip", k_Audio + "Manege_Bouquet.wav", k_Audio + "Placeholder/explosion.wav"),
        };

        static ManegeSonsBaker()
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

            var log = new List<string>();
            var root = PrefabUtility.LoadPrefabContents(k_Prefab);
            try
            {
                var manege = root.GetComponent<UltimeManege>();
                if (manege == null)
                    return;

                var serialized = new SerializedObject(manege);
                foreach (var (field, path, placeholder) in k_Clips)
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    var property = serialized.FindProperty(field);
                    if (clip == null || property == null || property.objectReferenceValue == clip)
                        continue;
                    var current = property.objectReferenceValue;
                    if (current != null && (placeholder == null || current != AssetDatabase.LoadAssetAtPath<AudioClip>(placeholder)))
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

            Debug.Log("Les sons du Grand Manège (2 novembre) : " + string.Join(" ; ", log) + ".",
                      AssetDatabase.LoadAssetAtPath<GameObject>(k_Prefab));
        }
    }
}
