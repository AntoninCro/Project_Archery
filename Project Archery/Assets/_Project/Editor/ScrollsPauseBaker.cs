using System.Collections.Generic;
using Archery.Shop;
using Archery.Upgrades;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le 8 novembre, le joueur (<c>Docs/Refonte_TowerDefense.md</c>, 12.69) : l'or est retiré, la pause devient un choix (un
    /// parchemin, un objet, une tourelle parmi trois ; <c>ShopManager</c>), et on prend aussi les parchemins dans les coffres.
    /// « Puisqu'il n'y a plus autant de possibilités d'avoir des parchemins, on peut modifier grandement leurs valeurs », chacun
    /// à deux effets (le code, <c>PlayerUpgrades</c>) ; ici, la valeur et le texte de leurs cartes, que le joueur a donnés :
    /// <list type="bullet">
    /// <item>Force : +12 % → +30 % de dégâts, et l'anneau 15 % plus rapide (la vitesse de charge) ;</item>
    /// <item>Adresse : 10 % → 20 % : la bande verte 20 % plus large, les tirs parfaits +20 % de dégâts ;</item>
    /// <item>Vigueur : +12 → +35 PV (35 % des PV de base), et les flèches 35 % plus loin.</item>
    /// </list>
    /// Chaque carte n'est changée que si elle a encore sa valeur d'avant : une seule fois, et jamais une carte retouchée à la main.
    /// </summary>
    [InitializeOnLoad]
    static class ScrollsPauseBaker
    {
        const string k_Catalog = "Assets/_Project/Data/Shop/ShopCatalog.asset";

        // Les valeurs du choix de la pause (Force 0,3, Adresse 0,2, Vigueur 35) sont remplacées le même soir par celles des raretés
        // (RaretesBaker, divisées par deux) : plus rien ici. Sinon l'Adresse faisait la navette à chaque chargement de Unity
        // (0,1 → 0,2 ici, 0,2 → 0,1 là : sa valeur d'avant le choix de la pause est aussi sa valeur des raretés).
        static readonly (string Name, float Old, float New, string Description, string LevelUp)[] k_Scrolls =
            System.Array.Empty<(string, float, float, string, string)>();

        static ScrollsPauseBaker()
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

            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(k_Catalog);
            if (catalog == null)
                return;

            var serialized = new SerializedObject(catalog);
            var upgrades = serialized.FindProperty("upgrades");
            if (upgrades == null || !upgrades.isArray)
                return;

            var log = new List<string>();
            for (var i = 0; i < upgrades.arraySize; i++)
            {
                var entry = upgrades.GetArrayElementAtIndex(i);
                var name = entry.FindPropertyRelative("displayName");
                var value = entry.FindPropertyRelative("value");
                if (name == null || value == null)
                    continue;

                foreach (var scroll in k_Scrolls)
                {
                    if (name.stringValue != scroll.Name || !Mathf.Approximately(value.floatValue, scroll.Old))
                        continue;

                    value.floatValue = scroll.New;
                    var description = entry.FindPropertyRelative("description");
                    var levelUp = entry.FindPropertyRelative("levelUpText");
                    if (description != null)
                        description.stringValue = scroll.Description;
                    if (levelUp != null)
                        levelUp.stringValue = scroll.LevelUp;
                    log.Add($"{scroll.Name} : {scroll.Old:0.##} → {scroll.New:0.##} (« {scroll.Description} »)");
                }
            }

            // Le talisman de l'avare ne valait que par l'or gardé : retiré (gardé dans le catalogue avec ses réglages, plus jamais
            // proposé). Le butin l'est déjà.
            for (var i = 0; i < upgrades.arraySize; i++)
            {
                var entry = upgrades.GetArrayElementAtIndex(i);
                var effect = entry.FindPropertyRelative("effect");
                var kind = entry.FindPropertyRelative("kind");
                if (effect == null || kind == null || effect.intValue != (int)UpgradeEffect.Miser || kind.intValue != (int)UpgradeKind.Object)
                    continue;

                kind.intValue = (int)UpgradeKind.Retired;
                log.Add("Talisman de l'avare : retiré (il comptait l'or gardé)");
            }

            if (log.Count == 0)
                return;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("Les parchemins du choix de la pause (8 novembre) :\n" + string.Join("\n", log));
        }
    }
}
