using System.Collections.Generic;
using Archery.Building;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Les sons de la Grande Horloge (2 novembre, tard ; <c>Docs/Refonte_TowerDefense.md</c>, 12.52) : le tic, un coup de la cloche (les
    /// douze de Minuit), le temps qui s'arrête et qui reprend, la glace qui éclate (en série, et un craquement seul), le carillon. Choisis sur freesound.org (CC0),
    /// téléchargés par le joueur dans <c>Audio/</c>, découpés par Claude en <c>Audio/Horloge_*.wav</c> (<c>Tools/sons/</c> ;
    /// <c>Docs/Credits_Licences.md</c>). Posés sur le prefab <c>Prefabs/Structures/Duos/Ultime_Horloge</c>, chacun une seule fois :
    /// seulement si son fichier existe et que le réglage est encore vide, ou encore le son provisoire du projet que le fichier
    /// d'allure y a mis.
    /// </summary>
    /// <remarks>
    /// Comme <see cref="ManegeSonsBaker"/> (la leçon de 12.46 : Unity fait le prefab avant que les sons soient là) : il passe un tour
    /// d'éditeur après le baker de l'allure, et quand l'un des sons est importé (<see cref="Imports"/>).
    /// </remarks>
    [InitializeOnLoad]
    static class HorlogeSonsBaker
    {
        // Un son de l'Horloge importé (ou réimporté) : le baker passe au tour suivant.
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

        const string k_Prefab = "Assets/_Project/Prefabs/Structures/Duos/Ultime_Horloge.prefab";
        const string k_Audio = "Assets/_Project/Audio/";

        // Le réglage, son fichier, et le son provisoire qu'il remplace.
        static readonly (string Field, string Clip, string Placeholder)[] k_Clips =
        {
            ("m_TickClip", k_Audio + "Horloge_Tic.wav", k_Audio + "Placeholder/countdown_tick.wav"),
            ("m_BellClip", k_Audio + "Horloge_Cloche.wav", k_Audio + "Placeholder/gong.wav"),
            ("m_StopClip", k_Audio + "Horloge_Arret.wav", k_Audio + "Placeholder/teleport_charge.wav"),
            ("m_ResumeClip", k_Audio + "Horloge_Reprise.wav", k_Audio + "Placeholder/teleport.wav"),
            ("m_ShatterClip", k_Audio + "Horloge_Bris.wav", "Assets/kenney_impact-sounds/Audio/impactGlass_heavy_001.ogg"),
            ("m_ImpactClip", k_Audio + "Horloge_Eclat.wav", "Assets/kenney_impact-sounds/Audio/impactGlass_medium_001.ogg"),
            ("m_ChimeClip", k_Audio + "Horloge_Carillon.wav", k_Audio + "Placeholder/perfect_chime.wav"),
        };

        static HorlogeSonsBaker()
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
                var horloge = root.GetComponent<UltimeHorloge>();
                if (horloge == null)
                    return;

                var serialized = new SerializedObject(horloge);
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

            Debug.Log("Les sons de la Grande Horloge (2 novembre) : " + string.Join(" ; ", log) + ".",
                      AssetDatabase.LoadAssetAtPath<GameObject>(k_Prefab));
        }
    }
}
