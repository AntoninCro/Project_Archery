using System.Collections.Generic;
using Archery.Shop;
using Archery.Upgrades;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Plus d'effet réservé aux tirs parfaits (27 octobre, demandé par le joueur après un test au casque : « retire tous les
    /// effets des parfaits et mets-les sur toutes les flèches ») : les cartes du catalogue qui ne jouaient qu'aux tirs parfaits
    /// (Flèche de foudre, Chaîne d'éclairs, Flèche de glace, Déluge) jouent à chaque flèche, et le texte de la Pointe de givre
    /// (une proposition de Claude) ne parle plus de tir parfait. Les textes des autres cartes ne changent pas.
    /// <para>Une seule fois : tant que la Pointe de givre a encore son texte « Tir parfait : l'ennemi gèle… ». Un déclencheur
    /// remis à la main ensuite reste comme le joueur l'a mis.</para>
    /// Ce qui était écrit en dur dans le code (le gel de la pointe de givre, la réparation de l'ingénieur) est changé dans
    /// <c>Scripts/Upgrades/ArrowPieces.cs</c> et <c>ArrowPieces.Lot2.cs</c>.
    /// </summary>
    [InitializeOnLoad]
    static class RoguelikeAllShotsBaker
    {
        const string k_CatalogPath = "Assets/_Project/Data/Shop/ShopCatalog.asset";
        const string k_OldFrostTipText = "Tir parfait : l'ennemi gèle. Touché gelé, il éclate en glace";
        const string k_NewFrostTipText = "L'ennemi touché gèle. Touché gelé, il éclate en glace";

        static RoguelikeAllShotsBaker()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(k_CatalogPath);
            if (catalog == null)
                return;

            var frostTip = catalog.upgrades.Find(upgrade => upgrade != null && upgrade.kind == UpgradeKind.Object &&
                                                            upgrade.effect == UpgradeEffect.FrostTip);
            if (frostTip == null || frostTip.description != k_OldFrostTipText)
                return;

            var changed = new List<string>();
            foreach (var upgrade in catalog.upgrades)
            {
                if (upgrade == null || upgrade.trigger != UpgradeTrigger.PerfectShot)
                    continue;

                upgrade.trigger = UpgradeTrigger.Always;
                changed.Add(upgrade.displayName);
            }

            frostTip.description = k_NewFrostTipText;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log("Réglages roguelike du 27 octobre : plus d'effet réservé aux tirs parfaits. Jouent maintenant à chaque flèche : " +
                      (changed.Count > 0 ? string.Join(", ", changed) : "(aucune carte n'était réservée aux tirs parfaits)") +
                      $". Texte de la Pointe de givre : « {k_NewFrostTipText} ».", catalog);
        }
    }
}
