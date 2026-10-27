using System.Collections.Generic;
using Archery.Shop;
using Archery.Upgrades;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Les textes de cartes proposées par Claude, revues le 27 octobre après les tests du joueur au casque : l'empennage boomerang
    /// (rattrapé à la poignée, il arrive dans la main et c'est lui qui est parfait), la corde de pluie (elle sème des flèches le long
    /// de sa traînée), puis, au deuxième tour, la corde du faucon (sans « une fois par tension ») et le parchemin de Vigueur
    /// (+25 PV). Chaque texte n'est remplacé que s'il est encore l'ancien : une seule fois, et jamais un texte réécrit par le
    /// joueur. Le fonctionnement est dans <c>Scripts/Upgrades/ArrowPieces.cs</c> et <c>ArrowPieces.Lot3.cs</c>.
    /// </summary>
    [InitializeOnLoad]
    static class RoguelikeTextsBaker
    {
        const string k_CatalogPath = "Assets/_Project/Data/Shop/ShopCatalog.asset";

        static readonly (UpgradeEffect effect, string oldText, string newText)[] k_Texts =
        {
            (UpgradeEffect.Boomerang, "La flèche revient vers toi ; rattrape-la : tir suivant parfait",
             "La flèche revient vers toi ; attrape-la (poignée) : elle est dans ta main, et parfaite"),
            (UpgradeEffect.Rain, "Un tir vers le ciel retombe en pluie de flèches",
             "En vol, la flèche sème des flèches tout le long de sa traînée"),

            // Le deuxième tour du 27 octobre (le patch note rempli par le joueur) : la recharge du faucon ne se dit plus sur la carte
            // (elle reste dans le jeu) ; la Vigueur passe à +25 PV.
            (UpgradeEffect.Falcon, "À pleine tension, le temps ralentit (une fois par tension)", "À pleine tension, le temps ralentit"),
            (UpgradeEffect.Vigor, "+15 PV max", "+25 PV max"),
        };

        static RoguelikeTextsBaker()
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

            var changed = new List<string>();
            foreach (var (effect, oldText, newText) in k_Texts)
            {
                foreach (var upgrade in catalog.upgrades)
                {
                    if (upgrade == null || upgrade.kind == UpgradeKind.Retired || upgrade.effect != effect || upgrade.description != oldText)
                        continue;

                    upgrade.description = newText;
                    changed.Add($"{upgrade.displayName} : « {newText} »");
                }
            }

            if (changed.Count == 0)
                return;

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log($"Réglages roguelike du 27 octobre, textes revus : {string.Join(" ; ", changed)}.", catalog);
        }
    }
}
