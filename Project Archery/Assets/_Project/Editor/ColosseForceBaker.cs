using System.Collections.Generic;
using Archery.Building;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// La force du Colosse de forge (2 novembre, tard ; <c>Docs/Refonte_TowerDefense.md</c>, 12.48) : après son essai, le joueur
    /// trouve ses marteaux bien trop faibles (« les marteaux doivent être bien plus imposants comme la précédente version et garder
    /// les mêmes dégâts ; compare avec les autres trios du Excel »). Posé sur le prefab <c>Prefabs/Structures/Duos/Ultime_Colosse</c>,
    /// une seule fois (tant que ses marteaux ont encore leurs 40 de dégâts du premier jet de 12.48), et seulement les réglages qui ont
    /// encore leur valeur d'avant : les marteaux de 12.47 (160 dans 6 m, aussi grands que le sien), la forge renforcée (au tableau :
    /// seule, elle vaut un trio moyen au niveau 3 ; avec tes flèches, deux trios et plus), et la coulée de 12.47.
    /// </summary>
    /// <remarks>
    /// Le baker de l'allure (<see cref="TowerDefenseLookBaker"/>) refait le modèle du Colosse à chaque nouvelle marque, mais garde les
    /// réglages déjà posés sur le prefab : les valeurs changées dans le code ne l'atteignent pas. C'est ce qui est arrivé en 12.47 (le
    /// marteau à 160 dans 6 m et la coulée plus forte, restés à 140 dans 5 m et 80) : elles sont posées ici aussi. Ce baker passe
    /// après celui de l'allure (un tour d'éditeur plus tard), qui fait le prefab au même tour.
    /// </remarks>
    [InitializeOnLoad]
    static class ColosseForceBaker
    {
        const string k_Prefab = "Assets/_Project/Prefabs/Structures/Duos/Ultime_Colosse.prefab";

        // Le réglage, ses valeurs d'avant (celle du prefab, celle du code de 12.47), la nouvelle.
        static readonly (string Field, float[] Before, float Now)[] k_Values =
        {
            // Les marteaux, comme celui d'avant (12.47).
            ("m_HammerDamage", new[] { 40f }, 160f),
            ("m_HammerRadius", new[] { 3f }, 6f),
            ("m_HammerPush", new[] { 3f }, 8f),
            ("m_HammerLift", new[] { 5f }, 12f),
            ("m_HammerScale", new[] { 0.55f }, 1f),
            // La forge : les éclats et le lingot.
            ("m_ChunkDamage", new[] { 30f }, 70f),
            ("m_ChunkRadius", new[] { 2.5f }, 3f),
            ("m_Damage", new[] { 140f, 160f }, 450f),
            ("m_Radius", new[] { 5f, 6f }, 7f),
            ("m_Push", new[] { 6f, 8f }, 10f),
            ("m_Lift", new[] { 9f, 12f }, 14f),
            ("m_ScorchRadius", new[] { 2f, 2.2f }, 2.5f),
            ("m_ScorchBurn", new[] { 10f, 12f }, 15f),
            // La coulée de 12.47.
            ("m_FlowSpeed", new[] { 14f }, 12f),
            ("m_FlowDamage", new[] { 80f }, 90f),
            ("m_FlowBurn", new[] { 40f }, 50f),
            ("m_FlowPush", new[] { 10f }, 12f),
            ("m_PourTime", new[] { 1.2f }, 1.4f),
            ("m_FlowStay", new[] { 4f }, 6f),
        };

        static ColosseForceBaker()
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
                var colosse = root.GetComponent<UltimeColosse>();
                if (colosse == null)
                    return;

                // Une seule fois : les marteaux du premier jet de 12.48 (40 de dégâts) ; sinon, c'est fait, ou réglé à la main.
                var serialized = new SerializedObject(colosse);
                var gate = serialized.FindProperty("m_HammerDamage");
                if (gate == null || !Mathf.Approximately(gate.floatValue, 40f))
                    return;

                foreach (var (field, before, now) in k_Values)
                {
                    var property = serialized.FindProperty(field);
                    if (property == null || !System.Array.Exists(before, b => Mathf.Approximately(property.floatValue, b)))
                        continue;
                    log.Add(field + " " + property.floatValue + " → " + now);
                    property.floatValue = now;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, k_Prefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            if (log.Count > 0)
                Debug.Log("La force du Colosse de forge (2 novembre, tard ; 12.48) : " + string.Join(" ; ", log) + ".",
                          AssetDatabase.LoadAssetAtPath<GameObject>(k_Prefab));
        }
    }
}
