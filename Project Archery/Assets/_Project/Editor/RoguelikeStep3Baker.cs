using System.Collections.Generic;
using Archery.Shop;
using Archery.Upgrades;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Refonte de la partie roguelike, étape 3 (26 octobre, <c>Docs/Refonte_Roguelike.md</c>) : le premier lot de pièces
    /// nouvelles, ajouté à la fin du catalogue de la boutique (<c>Data/Shop/ShopCatalog.asset</c>), une seule fois (tant
    /// qu'aucune carte n'a l'effet de la corde triple) et quand l'étape 1 est faite (il y a des parchemins).
    /// Corde triple, empennages boomerang et télékinésie, fûts aimant et conducteur, pointes à mèche et de givre, talisman
    /// d'acrobate : leur fonctionnement est dans <c>Scripts/Upgrades/ArrowPieces.cs</c> (et <c>SpecialArrows</c> pour la corde
    /// triple). Chacune monte jusqu'au niveau 3. Les textes des cartes sont des propositions, que le joueur réécrira ; les
    /// icônes reprennent celles des cartes qui leur ressemblent, en attendant les leurs. L'asset est enregistré tout seul.
    /// </summary>
    [InitializeOnLoad]
    static class RoguelikeStep3Baker
    {
        const string k_CatalogPath = "Assets/_Project/Data/Shop/ShopCatalog.asset";
        const string k_IconFolder = "Assets/_Project/UI/Icones/";
        const int k_MaxLevel = 3;

        struct Piece
        {
            public string Name;
            public string Description;
            public UpgradeRarity Rarity;
            public UpgradeEffect Effect;
            public ArrowSlot Slot;
            public float Value;
            public string Icon;
        }

        static readonly Piece[] k_Pieces =
        {
            new Piece { Name = "Corde triple", Description = "3 flèches en éventail, dans le plan de l'arc : penche l'arc pour le tourner",
                        Rarity = UpgradeRarity.Rare, Effect = UpgradeEffect.Triple, Slot = ArrowSlot.String, Value = 2f, Icon = "Icone_Multitir" },
            new Piece { Name = "Empennage boomerang", Description = "La flèche revient vers toi ; rattrape-la : tir suivant parfait",
                        Rarity = UpgradeRarity.Rare, Effect = UpgradeEffect.Boomerang, Slot = ArrowSlot.Fletching, Value = 1f, Icon = "Icone_Ricochet" },
            new Piece { Name = "Empennage télékinésie", Description = "Garde la gâchette de la main de l'arc : la flèche suit ton arc",
                        Rarity = UpgradeRarity.Legendary, Effect = UpgradeEffect.Telekinesis, Slot = ArrowSlot.Fletching, Value = 1f, Icon = "Icone_AutoVisee" },
            new Piece { Name = "Fût aimant", Description = "La flèche plantée attire les ennemis autour d'elle",
                        Rarity = UpgradeRarity.Rare, Effect = UpgradeEffect.Magnet, Slot = ArrowSlot.Shaft, Value = 1f, Icon = "Icone_Deplacement" },
            new Piece { Name = "Fût conducteur", Description = "Deux flèches plantées proches : un arc électrique entre elles",
                        Rarity = UpgradeRarity.Legendary, Effect = UpgradeEffect.Conductor, Slot = ArrowSlot.Shaft, Value = 1f, Icon = "Icone_ChaineEclairs" },
            new Piece { Name = "Pointe à mèche", Description = "La flèche plantée explose une seconde plus tard",
                        Rarity = UpgradeRarity.Rare, Effect = UpgradeEffect.Fuse, Slot = ArrowSlot.Tip, Value = 1f, Icon = "Icone_Explosive" },
            new Piece { Name = "Pointe de givre", Description = "Tir parfait : l'ennemi gèle. Touché gelé, il éclate en glace",
                        Rarity = UpgradeRarity.Rare, Effect = UpgradeEffect.FrostTip, Slot = ArrowSlot.Tip, Value = 1f, Icon = "Icone_Glace" },
            new Piece { Name = "Talisman d'acrobate", Description = "En l'air ou en glissade : ralenti, et chaque tir est parfait",
                        Rarity = UpgradeRarity.Legendary, Effect = UpgradeEffect.Acrobat, Slot = ArrowSlot.Talisman, Value = 1f, Icon = "Icone_Precision" },
        };

        static RoguelikeStep3Baker()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(k_CatalogPath);
            if (catalog == null || !catalog.upgrades.Exists(upgrade => upgrade != null && upgrade.kind == UpgradeKind.Scroll) ||
                catalog.upgrades.Exists(upgrade => upgrade != null && upgrade.effect == UpgradeEffect.Triple))
                return;

            var added = new List<string>();
            foreach (var piece in k_Pieces)
            {
                catalog.upgrades.Add(new Upgrade(piece.Name, piece.Description, piece.Rarity, piece.Effect, piece.Value, k_MaxLevel)
                {
                    kind = UpgradeKind.Object,
                    slot = piece.Slot,
                    icon = AssetDatabase.LoadAssetAtPath<Sprite>(k_IconFolder + piece.Icon + ".png"),
                });
                added.Add($"{piece.Name} ({ShopCatalog.NameOf(piece.Slot)}, {ShopCatalog.NameOf(piece.Rarity)})");
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log("Refonte roguelike, étape 3 : les pièces nouvelles ajoutées au catalogue de la boutique (niveau " +
                      $"{k_MaxLevel} au plus) : {string.Join(", ", added)}. Textes et icônes provisoires.", catalog);
        }
    }
}
