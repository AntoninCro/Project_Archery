using System.Collections.Generic;
using Archery.Shop;
using Archery.Upgrades;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Refonte de la partie roguelike, étape 5 (26 octobre, <c>Docs/Refonte_Roguelike.md</c>) : le deuxième lot de pièces,
    /// ajouté à la fin du catalogue de la boutique (<c>Data/Shop/ShopCatalog.asset</c>), une seule fois (tant qu'aucune carte
    /// n'a l'effet de la pointe éclatée) et quand l'étape 1 est faite (il y a des parchemins).
    /// Leur fonctionnement est dans <c>Scripts/Upgrades/ArrowPieces.Lot2.cs</c> ; les duos qu'elles forment, dans
    /// <c>ArrowDuos</c>. Chacune monte jusqu'au niveau 3. Les textes des cartes sont des propositions, que le joueur réécrira ;
    /// les icônes reprennent celles des cartes qui leur ressemblent, en attendant les leurs. L'asset est enregistré tout seul.
    /// </summary>
    [InitializeOnLoad]
    static class RoguelikeStep5Baker
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
            public string Icon;
        }

        static readonly Piece[] k_Pieces =
        {
            // Pointes
            new Piece { Name = "Pointe éclatée", Description = "À l'impact, la flèche se brise en 4 éclats qui filent derrière l'ennemi",
                        Rarity = UpgradeRarity.Rare, Effect = UpgradeEffect.Splinter, Slot = ArrowSlot.Tip, Icon = "Icone_Deluge" },
            new Piece { Name = "Pointe de chasseur", Description = "Marque la cible : elle prend double dégâts. À sa mort, la marque saute",
                        Rarity = UpgradeRarity.Rare, Effect = UpgradeEffect.HunterMark, Slot = ArrowSlot.Tip, Icon = "Icone_ChasseurDeTetes" },

            // Empennages
            new Piece { Name = "Empennage rebond", Description = "La flèche rebondit 2 fois sur le sol et les murs",
                        Rarity = UpgradeRarity.Rare, Effect = UpgradeEffect.Bounce, Slot = ArrowSlot.Fletching, Icon = "Icone_Ricochet" },
            new Piece { Name = "Empennage plume", Description = "La flèche ne tombe plus, et va plus vite",
                        Rarity = UpgradeRarity.Rare, Effect = UpgradeEffect.Feather, Slot = ArrowSlot.Fletching, Icon = "Icone_FlechesRapides" },
            new Piece { Name = "Empennage de plomb", Description = "Flèche lourde : une onde de choc là où elle touche le décor",
                        Rarity = UpgradeRarity.Rare, Effect = UpgradeEffect.Lead, Slot = ArrowSlot.Fletching, Icon = "Icone_Gong" },
            new Piece { Name = "Empennage spectral", Description = "La flèche traverse les murs et le décor",
                        Rarity = UpgradeRarity.Legendary, Effect = UpgradeEffect.Spectral, Slot = ArrowSlot.Fletching, Icon = "Icone_Percage" },

            // Fûts
            new Piece { Name = "Fût-pieu", Description = "Plantée au sol, elle blesse et ralentit les ennemis qui passent dessus",
                        Rarity = UpgradeRarity.Rare, Effect = UpgradeEffect.Spike, Slot = ArrowSlot.Shaft, Icon = "Icone_Pieges" },
            new Piece { Name = "Fût détonateur", Description = "Tire sur une de tes flèches plantées : elle éclate en 6 flèches",
                        Rarity = UpgradeRarity.Legendary, Effect = UpgradeEffect.Detonator, Slot = ArrowSlot.Shaft, Icon = "Icone_Explosive" },
            new Piece { Name = "Fût suspendu", Description = "La flèche s'arrête en l'air ; la gâchette de l'autre main les relâche",
                        Rarity = UpgradeRarity.Rare, Effect = UpgradeEffect.Suspended, Slot = ArrowSlot.Shaft, Icon = "Icone_Echo" },

            // Cordes
            new Piece { Name = "Corde de transe", Description = "Chaque tir parfait d'affilée frappe plus fort ; au 5e, la flèche traverse tout",
                        Rarity = UpgradeRarity.Rare, Effect = UpgradeEffect.Trance, Slot = ArrowSlot.String, Icon = "Icone_ChargeRapide" },
            new Piece { Name = "Corde du faucon", Description = "À pleine tension, le temps ralentit (toutes les 8 s)",
                        Rarity = UpgradeRarity.Rare, Effect = UpgradeEffect.Falcon, Slot = ArrowSlot.String, Icon = "Icone_Precision" },

            // Talismans
            new Piece { Name = "Talisman du sniper", Description = "+2 % de dégâts par mètre au-delà de 15 m, moitié moins tout près",
                        Rarity = UpgradeRarity.Rare, Effect = UpgradeEffect.Sniper, Slot = ArrowSlot.Talisman, Icon = "Icone_Arc" },
            new Piece { Name = "Pacte de verre", Description = "Dégâts ×2, mais chaque tir raté coûte 5 PV",
                        Rarity = UpgradeRarity.Legendary, Effect = UpgradeEffect.GlassPact, Slot = ArrowSlot.Talisman, Icon = "Icone_Vitalite" },
            new Piece { Name = "Talisman de l'ingénieur", Description = "Une flèche dans ta baliste ou ta tour de givre la surcharge 5 s",
                        Rarity = UpgradeRarity.Rare, Effect = UpgradeEffect.Engineer, Slot = ArrowSlot.Talisman, Icon = "Icone_Baliste" },
            new Piece { Name = "Talisman du duelliste", Description = "Coup de flèche au corps à corps ×4, et le tir suivant est parfait",
                        Rarity = UpgradeRarity.Rare, Effect = UpgradeEffect.Duelist, Slot = ArrowSlot.Talisman, Icon = "Icone_Degats" },
            new Piece { Name = "Talisman du bourreau", Description = "Tir à la tête sous 25 % de PV : l'ennemi meurt net (pas les boss)",
                        Rarity = UpgradeRarity.Legendary, Effect = UpgradeEffect.Executioner, Slot = ArrowSlot.Talisman, Icon = "Icone_Vampirisme" },
        };

        static RoguelikeStep5Baker()
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
                catalog.upgrades.Exists(upgrade => upgrade != null && upgrade.effect == UpgradeEffect.Splinter))
                return;

            var added = new List<string>();
            foreach (var piece in k_Pieces)
            {
                catalog.upgrades.Add(new Upgrade(piece.Name, piece.Description, piece.Rarity, piece.Effect, 1f, k_MaxLevel)
                {
                    kind = UpgradeKind.Object,
                    slot = piece.Slot,
                    icon = AssetDatabase.LoadAssetAtPath<Sprite>(k_IconFolder + piece.Icon + ".png"),
                });
                added.Add($"{piece.Name} ({ShopCatalog.NameOf(piece.Slot)}, {ShopCatalog.NameOf(piece.Rarity)})");
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log("Refonte roguelike, étape 5 : le deuxième lot de pièces ajouté au catalogue de la boutique (niveau " +
                      $"{k_MaxLevel} au plus) : {string.Join(", ", added)}. Textes et icônes provisoires.", catalog);
        }
    }
}
