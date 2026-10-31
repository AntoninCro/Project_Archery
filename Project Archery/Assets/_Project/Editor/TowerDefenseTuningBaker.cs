using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Les réglages du tower defense après les tests du joueur au casque (refonte du tower defense, <c>Docs/Refonte_TowerDefense.md</c>,
    /// partie « Avancement »), chacun une fois, seulement s'il a encore sa valeur d'avant (un réglage retouché à la main n'est
    /// pas touché).
    /// <list type="bullet">
    /// <item>29 octobre : la barricade passe de 6 à 4 m de large (son prefab était étiré ×1,5 en X ; il revient à ×1) : à
    /// 6 cases, elle ne tenait pas sur les voies de gauche et de droite, qui n'ont que 4 ou 5 cases de terre de large. Elle
    /// arrête toujours les ennemis de toute la voie (une zone de 5 m de rayon devant elle, pas sa largeur).</item>
    /// <item>29 octobre (deuxième brainstorm, partie 12) : il ne reste que 10 tours ; les 17 autres bâtiments sont marqués
    /// « retirés » (<c>retired</c> de leur réglage dans <c>Data/Structures</c>) : les caisses, B et le menu des plans les
    /// ignorent. Une fois, tant qu'aucun bâtiment n'est retiré : en décocher un plus tard le remet pour de bon.</item>
    /// </list>
    /// </summary>
    [InitializeOnLoad]
    static class TowerDefenseTuningBaker
    {
        const string k_BarricadePath = "Assets/_Project/Prefabs/Structures/Barricade.prefab";
        const float k_OldBarricadeWidth = 1.5f;
        const float k_BarricadeWidth = 1f;
        const string k_StructuresFolder = "Assets/_Project/Data/Structures";

        // Les bâtiments retirés le 29 octobre (les 10 gardés : barricade, piques, tremplin, baliste, mortier, tour de givre,
        // lance-filet, tour-aimant, bulle, phare). La mine, le goudron et l'anneau reviennent en niveaux 4, la catapulte et le
        // pendule en niveaux 3, la baliste sur la tour en baliste titan, l'esprit gardien en ultime (Refonte_TowerDefense, 12).
        static readonly string[] k_Retired =
        {
            "Arbalete", "Foudre", "LanceFlammes", "Miroir", "Catapulte", "Soufflerie", "Pendule", "Geyser", "Trappe",
            "AnneauFeu", "AnneauFoudre", "AnneauDouble", "Mine", "Goudron", "BalisteTour", "Esprit", "SeauHuile",
        };

        static TowerDefenseTuningBaker()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            NarrowBarricade();
            RetireStructures();
        }

        static void RetireStructures()
        {
            var definitions = new System.Collections.Generic.List<Archery.Building.StructureDefinition>();
            foreach (var guid in AssetDatabase.FindAssets("t:StructureDefinition", new[] { k_StructuresFolder }))
            {
                var definition = AssetDatabase.LoadAssetAtPath<Archery.Building.StructureDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (definition == null)
                    continue;
                if (definition.retired)
                    return;
                definitions.Add(definition);
            }

            var retired = new System.Collections.Generic.List<string>();
            foreach (var definition in definitions)
            {
                if (System.Array.IndexOf(k_Retired, definition.name) < 0)
                    continue;

                var settings = new SerializedObject(definition);
                settings.FindProperty("retired").boolValue = true;
                settings.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.SaveAssetIfDirty(definition);
                retired.Add(definition.displayName);
            }

            if (retired.Count > 0)
                Debug.Log($"Tower defense : {retired.Count} bâtiment(s) retiré(s) du jeu ({string.Join(", ", retired)}) ; restent " +
                          $"{definitions.Count - retired.Count} tours (case « Retired » de leur réglage dans {k_StructuresFolder}).");
        }

        static void NarrowBarricade()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_BarricadePath);
            if (prefab == null || !Mathf.Approximately(prefab.transform.localScale.x, k_OldBarricadeWidth))
                return;

            var root = PrefabUtility.LoadPrefabContents(k_BarricadePath);
            try
            {
                var scale = root.transform.localScale;
                root.transform.localScale = new Vector3(k_BarricadeWidth, scale.y, scale.z);
                PrefabUtility.SaveAsPrefabAsset(root, k_BarricadePath);
                Debug.Log("Tower defense : la barricade passe de 6 à 4 m de large (4 cases au lieu de 6) : elle tient sur toutes les voies " +
                          $"({k_BarricadePath}, échelle X {k_OldBarricadeWidth} → {k_BarricadeWidth}).",
                          AssetDatabase.LoadAssetAtPath<GameObject>(k_BarricadePath));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
