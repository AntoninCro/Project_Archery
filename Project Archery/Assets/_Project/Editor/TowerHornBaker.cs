using Archery.World;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le cor de la tour (3 novembre ; <c>Docs/Refonte_TowerDefense.md</c>, 12.59) : il sonne quand la tour est attaquée et que le
    /// joueur est loin, à la place de l'alerte sur la montre. Synthétisé par Claude (<c>Tools/sons/cor_de_la_tour.py</c>,
    /// <c>Audio/Tour_Cor.wav</c>), posé sur les réglages des lumières de la carte (<c>Resources/MapLighting</c>, <i>Tower Horn Clip</i>),
    /// une seule fois : seulement si le réglage est vide.
    /// </summary>
    /// <remarks>Comme les bakers des sons des ultimes : il repasse quand le son est importé (Unity peut le lancer avant).</remarks>
    [InitializeOnLoad]
    static class TowerHornBaker
    {
        sealed class Imports : AssetPostprocessor
        {
            static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
            {
                if (System.Array.IndexOf(imported, k_Clip) < 0)
                    return;
                EditorApplication.delayCall -= Run;
                EditorApplication.delayCall += Run;
            }
        }

        const string k_Settings = "Assets/_Project/Resources/MapLighting.asset";
        const string k_Clip = "Assets/_Project/Audio/Tour_Cor.wav";

        static TowerHornBaker()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            var settings = AssetDatabase.LoadAssetAtPath<MapLightingSettings>(k_Settings);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(k_Clip);
            if (settings == null || clip == null || settings.towerHornClip != null)
                return;

            settings.towerHornClip = clip;
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssetIfDirty(settings);
            Debug.Log($"Le cor de la tour (3 novembre) : {k_Clip} posé sur {k_Settings} (Tower Horn Clip). Il sonne quand la tour est " +
                      "attaquée et que tu es loin ; pendant l'attaque, le rayon monte haut et bat en rouge ; sous la moitié de ses PV, elle fume.",
                      settings);
        }
    }
}
