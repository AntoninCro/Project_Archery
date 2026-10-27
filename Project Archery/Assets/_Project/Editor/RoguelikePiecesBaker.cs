using System.Collections.Generic;
using Archery.Shop;
using Archery.Upgrades;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le troisième lot de pièces et les choix du joueur du 26 octobre au soir (<c>Docs/Refonte_Roguelike.md</c>, partie 14),
    /// chacun une seule fois, avec sa condition de garde :
    /// <list type="bullet">
    /// <item>le catalogue de la boutique (tant qu'aucune carte n'a l'effet de la corde de siège) : Perçage et Flèche de glace
    /// passent au fût, Vampirisme au talisman, Chasseur de têtes à la corde ; Déluge devient rare ; les 7 pièces nouvelles sont
    /// ajoutées à la fin (textes provisoires, icônes reprises de cartes qui leur ressemblent) ;</item>
    /// <item>la scène ouverte (tant que <c>Special Arrows</c> n'a pas le matériau du feu de la vraie explosion) : les deux
    /// matériaux de l'explosion (feu, fumée), le multitir resserré (2° au lieu de 4°), et l'explosion affaiblie si elle a
    /// encore les valeurs du matin (4/5/6 m, 100/150/200 %). La scène est alors à enregistrer (Ctrl+S).</item>
    /// </list>
    /// Le fonctionnement des pièces est dans <c>Scripts/Upgrades/ArrowPieces.Lot3.cs</c>.
    /// </summary>
    [InitializeOnLoad]
    static class RoguelikePiecesBaker
    {
        const string k_CatalogPath = "Assets/_Project/Data/Shop/ShopCatalog.asset";
        const string k_IconFolder = "Assets/_Project/UI/Icones/";
        const string k_FireMaterialPath = "Assets/_Project/Materials/Fire_Particles.mat";
        const string k_SmokeMaterialPath = "Assets/_Project/Art/Grenade/Grenade fumee.mat";
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
            new Piece { Name = "Corde de siège", Description = "Garde la tension au bout de l'anneau : le tir se charge (jusqu'à ×2,5)",
                        Rarity = UpgradeRarity.Rare, Effect = UpgradeEffect.Siege, Slot = ArrowSlot.String, Icon = "Icone_ChargeRapide" },
            new Piece { Name = "Corde de pluie", Description = "Un tir vers le ciel retombe en pluie de flèches",
                        Rarity = UpgradeRarity.Rare, Effect = UpgradeEffect.Rain, Slot = ArrowSlot.String, Icon = "Icone_Deluge" },
            new Piece { Name = "Corde fantôme", Description = "Juste après un tir, tire la corde à vide : une flèche fantôme part",
                        Rarity = UpgradeRarity.Legendary, Effect = UpgradeEffect.Ghost, Slot = ArrowSlot.String, Icon = "Icone_Echo" },
            new Piece { Name = "Empennage piqué", Description = "Tirée vers le ciel, la flèche pique sur l'ennemi le plus proche",
                        Rarity = UpgradeRarity.Rare, Effect = UpgradeEffect.Dive, Slot = ArrowSlot.Fletching, Icon = "Icone_AutoVisee" },
            new Piece { Name = "Empennage de rappel", Description = "Ferme le poing : tes flèches plantées reviennent en traversant les ennemis",
                        Rarity = UpgradeRarity.Legendary, Effect = UpgradeEffect.Recall, Slot = ArrowSlot.Fletching, Icon = "Icone_Ricochet" },
            new Piece { Name = "Fût-sentinelle", Description = "Plantée au sol, la flèche tire sur l'ennemi le plus proche",
                        Rarity = UpgradeRarity.Rare, Effect = UpgradeEffect.Sentinel, Slot = ArrowSlot.Shaft, Icon = "Icone_Baliste" },
            new Piece { Name = "Talisman de l'avare", Description = "+1 % de dégâts par tranche de 20 or gardés (+100 % au plus)",
                        Rarity = UpgradeRarity.Rare, Effect = UpgradeEffect.Miser, Slot = ArrowSlot.Talisman, Icon = "Icone_Butin" },
        };

        // Les pièces qui changent d'emplacement (partie 14.2 de Refonte_Roguelike.md).
        static readonly (UpgradeEffect effect, ArrowSlot slot)[] k_Moves =
        {
            (UpgradeEffect.Piercing, ArrowSlot.Shaft), (UpgradeEffect.Frost, ArrowSlot.Shaft),
            (UpgradeEffect.Vampirism, ArrowSlot.Talisman), (UpgradeEffect.HeadHunter, ArrowSlot.String),
        };

        static RoguelikePiecesBaker()
        {
            EditorSceneManager.sceneOpened += (scene, mode) => Schedule();
            Schedule();
        }

        static void Schedule()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            UpdateCatalog();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    UpdateScene(scene);
            }
        }

        static void UpdateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(k_CatalogPath);
            if (catalog == null || !catalog.upgrades.Exists(upgrade => upgrade != null && upgrade.kind == UpgradeKind.Scroll) ||
                catalog.upgrades.Exists(upgrade => upgrade != null && upgrade.effect == UpgradeEffect.Siege))
                return;

            var moved = new List<string>();
            foreach (var (effect, slot) in k_Moves)
            {
                foreach (var upgrade in catalog.upgrades)
                {
                    if (upgrade == null || upgrade.effect != effect || upgrade.kind != UpgradeKind.Object)
                        continue;
                    upgrade.slot = slot;
                    moved.Add($"{upgrade.displayName} → {ShopCatalog.NameOf(slot)}");
                }
            }

            var deluge = catalog.upgrades.Find(upgrade => upgrade != null && upgrade.effect == UpgradeEffect.Deluge);
            if (deluge != null)
                deluge.rarity = UpgradeRarity.Rare;

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
            Debug.Log($"Refonte roguelike, troisième lot : pièces déplacées : {string.Join(", ", moved)} ; Déluge passe en rare ; " +
                      $"ajoutées (niveau {k_MaxLevel} au plus) : {string.Join(", ", added)}. Textes et icônes provisoires.", catalog);
        }

        static void UpdateScene(Scene scene)
        {
            var fire = AssetDatabase.LoadAssetAtPath<Material>(k_FireMaterialPath);
            var smoke = AssetDatabase.LoadAssetAtPath<Material>(k_SmokeMaterialPath);
            if (fire == null || smoke == null)
                return;

            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var arrows in root.GetComponentsInChildren<SpecialArrows>(true))
                {
                    var serialized = new SerializedObject(arrows);
                    var fireProperty = serialized.FindProperty("m_ExplosionFireMaterial");
                    if (fireProperty == null || fireProperty.objectReferenceValue != null)
                        continue;

                    var log = new List<string> { "la vraie explosion (feu et fumée)" };
                    fireProperty.objectReferenceValue = fire;
                    serialized.FindProperty("m_ExplosionSmokeMaterial").objectReferenceValue = smoke;

                    var split = serialized.FindProperty("m_SplitAngle");
                    if (Mathf.Approximately(split.floatValue, 4f))
                    {
                        split.floatValue = 2f;
                        log.Add("le multitir resserré (2° entre deux flèches)");
                    }

                    var radius = serialized.FindProperty("m_ExplosionRadiusByLevel");
                    if (radius.vector3Value == new Vector3(4f, 5f, 6f))
                    {
                        radius.vector3Value = new Vector3(3.5f, 4f, 4.5f);
                        log.Add("l'explosion à 3,5/4/4,5 m");
                    }

                    var damage = serialized.FindProperty("m_ExplosionDamageByLevel");
                    if (damage.vector3Value == new Vector3(1f, 1.5f, 2f))
                    {
                        damage.vector3Value = new Vector3(0.75f, 1f, 1.25f);
                        log.Add("et à 75/100/125 % des dégâts");
                    }

                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorSceneManager.MarkSceneDirty(scene);
                    Debug.Log($"Refonte roguelike, troisième lot (scène {scene.name}) : {string.Join(", ", log)}. Enregistre la scène (Ctrl+S).", arrows);
                }
            }
        }
    }
}
