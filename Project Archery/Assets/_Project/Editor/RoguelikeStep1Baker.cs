using System.Collections.Generic;
using System.Text;
using Archery.Shop;
using Archery.Upgrades;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Archery.EditorTools
{
    /// <summary>
    /// Refonte de la partie roguelike, étape 1 (25 octobre, <c>Docs/Refonte_Roguelike.md</c>, proposition B choisie par le
    /// joueur). Chaque partie ne se fait qu'une fois, et la console dit ce qui a changé.
    /// <list type="bullet">
    /// <item><b>Le catalogue</b> (<c>Data/Shop/ShopCatalog.asset</c>, tant qu'il n'a aucun parchemin) : les cartes de
    /// statistiques (Dégâts, Puissance, Force du géant, Flèches rapides, Charge rapide, Précision, Vitalité), le Butin et la
    /// Chance sont retirées (gardées avec leurs réglages, plus jamais proposées) ; les autres deviennent des objets, pris une
    /// seule fois (Max Stacks 1), sans changer leurs valeurs, raretés ni textes (le joueur réécrira les descriptions) ; trois
    /// parchemins s'ajoutent à la fin (Force, Adresse, Vigueur, avec les icônes de Dégâts, Charge rapide et Vitalité) ; les
    /// objets sont tirés à 20 / 60 / 20 % (au lieu de 70 / 25 / 5 : il ne reste qu'un objet commun, Chasseur de têtes) ; un
    /// parchemin coûte 25, avec la hausse par achat du catalogue (+5 %).</item>
    /// <item><b>Les objets de la boutique</b> (retouche du 25 octobre au soir, demandée par le joueur) : 3 objets par pause,
    /// tous achetables, chers : 120 / 160 / 220 selon la rareté (70 / 100 / 140 dans la première version, un seul objet),
    /// +15 % par objet acheté. Seulement si le catalogue a encore les valeurs de la première version.</item>
    /// <item><b>Le panneau de la boutique</b>, dans la scène ouverte, tant qu'il a moins de 6 cartes : la relance et la carte
    /// des arcs disparaissent, les cartes de la tour et des barricades deviennent les cartes 5 et 6, la grille passe à
    /// 3 colonnes (les parchemins en haut, les objets en bas), avec le nom de chaque ligne à sa gauche. La scène est marquée
    /// comme modifiée : il suffit de l'enregistrer (Ctrl+S) ; Ctrl+Z annule.</item>
    /// </list>
    /// </summary>
    [InitializeOnLoad]
    static class RoguelikeStep1Baker
    {
        const string k_CatalogPath = "Assets/_Project/Data/Shop/ShopCatalog.asset";
        const int k_ShopCards = 6;
        const int k_ShopColumns = 3;

        // Les statistiques passent dans les parchemins ; le Butin et la Chance ne changent pas le tir.
        static readonly HashSet<UpgradeEffect> k_Retired = new HashSet<UpgradeEffect>
        {
            UpgradeEffect.Damage,
            UpgradeEffect.ArrowSpeed,
            UpgradeEffect.QuickCharge,
            UpgradeEffect.Precision,
            UpgradeEffect.Vitality,
            UpgradeEffect.Loot,
            UpgradeEffect.Luck,
        };

        static RoguelikeStep1Baker()
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

            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(k_CatalogPath);
            if (catalog != null)
            {
                ConvertCatalog(catalog);
                RaiseObjectPrices(catalog);
            }

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    ArrangeShopPanel(scene);
            }
        }

        // --- Le catalogue ---

        static void ConvertCatalog(ShopCatalog catalog)
        {
            if (catalog.upgrades.Exists(upgrade => upgrade != null && upgrade.kind == UpgradeKind.Scroll))
                return;

            var retired = new List<string>();
            var objects = new List<string>();
            foreach (var upgrade in catalog.upgrades)
            {
                if (upgrade == null)
                    continue;

                if (k_Retired.Contains(upgrade.effect))
                {
                    upgrade.kind = UpgradeKind.Retired;
                    retired.Add(upgrade.displayName);
                }
                else
                {
                    upgrade.kind = UpgradeKind.Object;
                    upgrade.maxStacks = 1;
                    objects.Add(upgrade.displayName);
                }
            }

            catalog.upgrades.Add(Scroll("Force", "+12 % de dégâts, flèches un peu plus rapides", UpgradeEffect.Strength, 0.12f,
                                        IconOf(catalog, UpgradeEffect.Damage)));
            catalog.upgrades.Add(Scroll("Adresse", "Anneau 10 % plus rapide, bande verte 10 % plus large", UpgradeEffect.Dexterity, 0.1f,
                                        IconOf(catalog, UpgradeEffect.QuickCharge)));
            catalog.upgrades.Add(Scroll("Vigueur", "+15 PV max", UpgradeEffect.Vigor, 15f, IconOf(catalog, UpgradeEffect.Vitality)));

            var oldPrices = $"{catalog.commonPrice} / {catalog.rarePrice} / {catalog.legendaryPrice}";
            var oldOdds = catalog.rarityOdds.Count > 0
                ? $"{catalog.rarityOdds[0].common} / {catalog.rarityOdds[0].rare} / {catalog.rarityOdds[0].legendary}"
                : "aucune";

            SetObjectOffers(catalog);
            catalog.rarityOdds = new List<RarityOdds> { new RarityOdds(1, 20f, 60f, 20f) };
            catalog.scrollPrice = 25;

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);

            var log = new StringBuilder("Refonte roguelike, étape 1 : catalogue de la boutique converti (Data/Shop/ShopCatalog).\n");
            log.Append("Objets (une seule fois) : ").Append(string.Join(", ", objects)).Append('\n');
            log.Append("Retirées (gardées, jamais proposées) : ").Append(string.Join(", ", retired)).Append('\n');
            log.Append("Parchemins ajoutés : Force (+12 % de dégâts), Adresse (+10 % d'anneau), Vigueur (+15 PV), à 25 or (+")
               .Append(Mathf.RoundToInt(catalog.priceIncreasePerPurchase * 100f)).Append(" % par achat).\n");
            log.Append($"Boutique : 3 objets par pause, à 120 / 160 / 220 or (avant, une amélioration : {oldPrices}) ; ")
               .Append($"chances des raretés 20 / 60 / 20 (avant : {oldOdds}).");
            Debug.Log(log.ToString(), catalog);
        }

        // La première version (25 octobre) proposait un seul objet, à 70 / 100 / 140 : le joueur les veut plus nombreux et plus chers.
        static void RaiseObjectPrices(ShopCatalog catalog)
        {
            if (catalog.objectOffers != 1 || catalog.objectsPerShop != 1 || catalog.commonPrice != 70 || catalog.rarePrice != 100 ||
                catalog.legendaryPrice != 140)
                return;

            SetObjectOffers(catalog);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log("Boutique : 3 objets par pause, tous achetables, à 120 / 160 / 220 or selon la rareté (avant : un seul, à " +
                      $"70 / 100 / 140), +{Mathf.RoundToInt(catalog.objectPriceIncreasePerPurchase * 100f)} % par objet acheté.", catalog);
        }

        static void SetObjectOffers(ShopCatalog catalog)
        {
            catalog.objectOffers = 3;
            catalog.objectsPerShop = 3;
            catalog.commonPrice = 120;
            catalog.rarePrice = 160;
            catalog.legendaryPrice = 220;
        }

        static Upgrade Scroll(string name, string description, UpgradeEffect effect, float value, Sprite icon) =>
            new Upgrade(name, description, UpgradeRarity.Common, effect, value)
            {
                kind = UpgradeKind.Scroll,
                icon = icon,
            };

        // L'icône d'une amélioration du catalogue qui a cet effet (les parchemins reprennent celles des cartes qu'ils remplacent).
        static Sprite IconOf(ShopCatalog catalog, UpgradeEffect effect)
        {
            foreach (var upgrade in catalog.upgrades)
            {
                if (upgrade != null && upgrade.effect == effect && upgrade.icon != null)
                    return upgrade.icon;
            }

            return null;
        }

        // --- Le panneau de la boutique ---

        static void ArrangeShopPanel(Scene scene)
        {
            ShopPanel panel = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                panel = root.GetComponentInChildren<ShopPanel>(true);
                if (panel != null)
                    break;
            }

            if (panel == null)
                return;

            var settings = new SerializedObject(panel);
            var cardsProperty = settings.FindProperty("m_UpgradeCards");
            if (cardsProperty == null || cardsProperty.arraySize >= k_ShopCards || cardsProperty.arraySize == 0)
                return;

            var cards = new List<ShopCard>();
            for (var i = 0; i < cardsProperty.arraySize; i++)
            {
                if (cardsProperty.GetArrayElementAtIndex(i).objectReferenceValue is ShopCard card)
                    cards.Add(card);
            }

            if (cards.Count == 0)
                return;

            var grid = cards[0].transform.parent;
            var changes = new List<string>();

            // Les anciennes cartes de la grille : celle des arcs disparaît, celles de la tour et des barricades servent aux objets.
            var spare = new List<ShopCard>();
            foreach (Transform child in grid)
            {
                var card = child.GetComponent<ShopCard>();
                if (card == null || cards.Contains(card))
                    continue;
                if (child.name.Contains("Bow"))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                    changes.Add("carte des arcs retirée");
                    break;
                }
            }

            foreach (Transform child in grid)
            {
                var card = child.GetComponent<ShopCard>();
                if (card != null && !cards.Contains(card))
                    spare.Add(card);
            }

            // La relance disparaît (un bouton sans carte, dans la grille).
            var reroll = grid.Find("Reroll");
            if (reroll != null)
            {
                Undo.DestroyObjectImmediate(reroll.gameObject);
                changes.Add("bouton de relance retiré");
            }

            foreach (var card in spare)
            {
                if (cards.Count >= k_ShopCards)
                    break;

                Undo.RecordObject(card.gameObject, "Boutique : cartes des objets");
                changes.Add($"« {card.name} » devient « Card {cards.Count + 1} »");
                card.gameObject.name = $"Card {cards.Count + 1}";
                cards.Add(card);
            }

            cardsProperty.arraySize = cards.Count;
            for (var i = 0; i < cards.Count; i++)
                cardsProperty.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            settings.ApplyModifiedProperties();

            var layout = grid.GetComponent<GridLayoutGroup>();
            if (layout != null)
            {
                var gridSettings = new SerializedObject(layout);
                gridSettings.FindProperty("m_ConstraintCount").intValue = k_ShopColumns;
                gridSettings.ApplyModifiedProperties();
                AddRowLabels(panel, (RectTransform)grid, layout);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Boutique (scène {scene.name}) : {cards.Count} cartes sur {k_ShopColumns} colonnes, les parchemins en haut, " +
                      $"les objets en bas ; {string.Join(", ", changes)}. Enregistre la scène (Ctrl+S).", panel);
        }

        // « Parchemins » et « Objets » à gauche de leur ligne, dans le style du montant d'or (copié).
        static void AddRowLabels(ShopPanel panel, RectTransform grid, GridLayoutGroup layout)
        {
            var model = new SerializedObject(panel).FindProperty("m_MoneyText").objectReferenceValue as TMP_Text;
            if (model == null || grid.parent == null)
                return;

            var width = k_ShopColumns * layout.cellSize.x + (k_ShopColumns - 1) * layout.spacing.x;
            var right = grid.anchoredPosition.x - width * 0.5f - 18f;
            var top = grid.anchoredPosition.y + grid.sizeDelta.y * 0.5f - layout.padding.top;
            AddRowLabel(model, grid.parent, "Ligne parchemins", "Parchemins", right, top - layout.cellSize.y * 0.5f);
            AddRowLabel(model, grid.parent, "Ligne objets", "Objets", right, top - layout.cellSize.y * 1.5f - layout.spacing.y);
        }

        static void AddRowLabel(TMP_Text model, Transform parent, string name, string text, float right, float y)
        {
            if (parent.Find(name) != null)
                return;

            var label = Object.Instantiate(model.gameObject, parent);
            Undo.RegisterCreatedObjectUndo(label, "Boutique : noms des lignes");
            label.name = name;
            var rect = (RectTransform)label.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.sizeDelta = new Vector2(150f, 80f);
            rect.anchoredPosition = new Vector2(right, y);

            var tmp = label.GetComponent<TMP_Text>();
            tmp.text = text;
            tmp.margin = Vector4.zero;
            tmp.alignment = TextAlignmentOptions.MidlineRight;
            tmp.fontSizeMax = Mathf.Max(18f, tmp.fontSize);
            tmp.fontSizeMin = 16f;
            tmp.enableAutoSizing = true;
        }
    }
}
