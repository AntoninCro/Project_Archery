using System.Collections.Generic;
using Archery.Shop;
using Archery.Upgrades;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore;

namespace Archery.EditorTools
{
    /// <summary>
    /// Les cartes refaites par le joueur le 27 octobre au soir (<c>Docs/Cartes_A_Refaire.xlsx</c>), une seule fois par carte :
    /// <list type="bullet">
    /// <item>le sprite des symboles du texte des cartes, <c>Resources/Sprite Assets/Symboles</c> (la flèche des dégâts d'une flèche
    /// de base, « ➶ », et « → », que la police Cinzel n'a pas), fait à partir de <c>UI/Icones/Symboles_Cartes.png</c> ;</item>
    /// <item>pour chaque pièce et parchemin : son nom (orthographe corrigée ; sans « Pointe », « Empennage », « Fût »… quand le
    /// joueur n'en a pas donné), son texte, qui suit le niveau (« {50 %|100 %|150 %} »), le texte de sa montée de niveau
    /// (« 50 % → 100 % ➶ »), la rareté choisie par le joueur et l'icône de sa catégorie (pointe, empennage, fût, corde, talisman,
    /// parchemin).</item>
    /// </list>
    /// Une carte n'est changée que si elle n'a pas encore de texte de montée de niveau : ensuite, ce qui est réglé à la main reste.
    /// Les noms et textes des duos sont dans le code (<c>Upgrades/ArrowDuos</c>).
    /// </summary>
    [InitializeOnLoad]
    static class RoguelikeCardsBaker
    {
        const string k_CatalogPath = "Assets/_Project/Data/Shop/ShopCatalog.asset";
        const string k_SymbolsTexture = "Assets/_Project/UI/Icones/Symboles_Cartes.png";
        const string k_SymbolsFolder = "Assets/_Project/Resources/Sprite Assets";
        const string k_SymbolsPath = k_SymbolsFolder + "/Symboles.asset";
        const string k_IconFolder = "Assets/_Project/UI/Icones/";

        const int k_Keep = -1;
        const int k_Common = (int)UpgradeRarity.Common;
        const int k_Rare = (int)UpgradeRarity.Rare;
        const int k_Legendary = (int)UpgradeRarity.Legendary;

        // (effet, nom, texte de la carte, texte de la montée de niveau, rareté ou k_Keep). « ➶ » : les dégâts d'une flèche de base.
        static readonly (UpgradeEffect effect, string name, string description, string levelUp, int rarity)[] k_Cards =
        {
            // Pointes.
            (UpgradeEffect.Lightning, "Foudroiement", "Fait apparaître un éclair : {50 %|100 %|150 %} ➶", "{50 %|100 %|150 %} ➶", k_Common),
            (UpgradeEffect.ChainLightning, "Chaîne d'éclairs", "Un éclair rebondit sur {3|6|9} ennemis : {30 %|60 %|90 %} ➶",
             "{3|6|9} ennemis ; {30 %|60 %|90 %} ➶", k_Keep),
            (UpgradeEffect.Explosive, "Tir explosif", "La flèche explose : {75 %|100 %|125 %} ➶ dans un rayon de {3,5|4|4,5} m",
             "{75 %|100 %|125 %} ➶ ; rayon {3,5 m|4 m|4,5 m}", k_Keep),
            (UpgradeEffect.Fuse, "Tir détonant", "La flèche explose une seconde plus tard : {100 %|125 %|150 %} ➶ au centre, rayon {3,5|4,25|5} m",
             "{100 %|125 %|150 %} ➶ ; rayon {3,5 m|4,25 m|5 m}", k_Keep),
            (UpgradeEffect.FrostTip, "Tir de givre", "L'ennemi touché gèle {1,5|2|2,5} s. Touché gelé, il éclate en glace : {80 %|110 %|140 %} ➶",
             "Gel {1,5 s|2 s|2,5 s} ; éclat {80 %|110 %|140 %} ➶", k_Keep),
            (UpgradeEffect.Splinter, "Flèche à fragmentation",
             "À l'impact, la flèche se brise en {4|5|6} éclats qui filent derrière l'ennemi : {40 %|50 %|60 %} ➶ chacun",
             "{4|5|6} éclats ; {40 %|50 %|60 %} ➶ chacun", k_Keep),
            (UpgradeEffect.HunterMark, "Pointe du chasseur", "Marque la cible : elle prend {200 %|250 %|300 %} ➶. À sa mort, la marque saute",
             "{200 %|250 %|300 %} ➶ sur la cible marquée", k_Common),

            // Empennages.
            (UpgradeEffect.Homing, "Auto-visée", "Les flèches se tournent vers l'ennemi proche, jusqu'à {12|15|18} m",
             "Portée {12 m|15 m|18 m} ; virage {30|60|90} °/s", k_Keep),
            (UpgradeEffect.Ricochet, "Tir ricochet", "La flèche rebondit sur {un autre ennemi|2 autres ennemis|3 autres ennemis}",
             "{1 ricochet|2 ricochets|3 ricochets}", k_Keep),
            (UpgradeEffect.Boomerang, "Boomerang",
             "La flèche revient vers toi ; attrape-la pour faire un tir parfait. Elle traverse {1 ennemi|2 ennemis|3 ennemis} à chaque passage",
             "{1 ennemi traversé|2 ennemis traversés|3 ennemis traversés} à chaque passage", k_Keep),
            (UpgradeEffect.Telekinesis, "Télékinésie",
             "Garder la gâchette de la main de l'arc appuyée permet de contrôler la flèche (elle ralentit à {50 %|40 %|30 %} de sa vitesse)",
             "Vitesse sous contrôle : {50 %|40 %|30 %}", k_Rare),
            (UpgradeEffect.Bounce, "Tir rebondissant", "La flèche rebondit {3|5|7} fois sur le sol et les murs", "{3|5|7} rebonds", k_Common),
            (UpgradeEffect.Feather, "Poids plume", "La flèche ne tombe plus et va {25 %|50 %|75 %} plus vite", "{+25 %|+50 %|+75 %} de vitesse",
             k_Common),
            (UpgradeEffect.Lead, "1 tonne", "La flèche tombe plus vite et provoque une onde de choc : {60 %|80 %|100 %} ➶ sur {2,5|3|3,5} m",
             "{60 %|80 %|100 %} ➶ ; rayon {2,5 m|3 m|3,5 m}", k_Common),
            (UpgradeEffect.Dive, "Death from the sky",
             "Tirée vers le ciel, la flèche pique sur l'ennemi le plus proche (jusqu'à {40|50|60} m) : {150 %|175 %|200 %} ➶",
             "{150 %|175 %|200 %} ➶ ; portée {40 m|50 m|60 m}", k_Keep),
            (UpgradeEffect.Recall, "Rappel de flèche",
             "Ferme le poing : tes flèches plantées reviennent et blessent les ennemis traversés ({80 %|100 %|120 %} ➶)",
             "{80 %|100 %|120 %} ➶ au retour", k_Keep),

            // Fûts.
            (UpgradeEffect.Piercing, "Perçage", "La flèche traverse {deux ennemis|quatre ennemis|tous les ennemis}",
             "{2|4|tous les} ennemis traversés", k_Common),
            (UpgradeEffect.Frost, "Marée de glace", "Gèle le sol : {40 %|50 %|60 %} de ralentissement, sur {3|4|5} m",
             "{40 %|50 %|60 %} de ralentissement ; zone de {3 m|4 m|5 m}", k_Common),
            (UpgradeEffect.Deluge, "Déluge", "La flèche se divise en vol en {1 autre flèche|2 autres flèches|3 autres flèches}",
             "{+1 flèche|+2 flèches|+3 flèches} en vol", k_Rare),
            (UpgradeEffect.Magnet, "Aimant", "La flèche plantée attire les ennemis autour d'elle ({6|7,5|9} m, pendant {2,5|3|3,5} s)",
             "Rayon {6 m|7,5 m|9 m} ; {2,5 s|3 s|3,5 s}", k_Keep),
            (UpgradeEffect.Conductor, "Arc conducteur",
             "Deux flèches plantées proches : un arc électrique entre elles, {80 %|120 %|160 %} ➶ par seconde pendant {6|7,5|9} s",
             "{80 %|120 %|160 %} ➶ par seconde ; {6 s|7,5 s|9 s}", k_Keep),
            (UpgradeEffect.Spike, "Sol piégé",
             "Plantée au sol, elle blesse et ralentit les ennemis qui passent à proximité : {17,5 %|25 %|32,5 %} ➶ toutes les 0,3 s, pendant {8|10|12} s",
             "{17,5 %|25 %|32,5 %} ➶ toutes les 0,3 s ; {8 s|10 s|12 s}", k_Keep),
            (UpgradeEffect.Detonator, "Flèche à détonation",
             "Tire sur une de tes flèches plantées : elle éclate en {6|8|10} flèches ({60 %|80 %|100 %} ➶ chacune)",
             "{6|8|10} flèches ; {60 %|80 %|100 %} ➶ chacune", k_Keep),
            (UpgradeEffect.Suspended, "ZA WARUDO",
             "La flèche s'arrête en l'air ({3|4|5} s au plus) ; la gâchette de l'autre main les envoie là où elle vise",
             "Attente : {3 s|4 s|5 s}", k_Legendary),
            (UpgradeEffect.Sentinel, "Flèche sentinelle",
             "Plantée au sol, la flèche tire sur l'ennemi le plus proche : {30 %|40 %|50 %} ➶ par seconde, pendant {5|6,5|8} s",
             "{30 %|40 %|50 %} ➶ par tir ; {5 s|6,5 s|8 s}", k_Keep),

            // Cordes.
            (UpgradeEffect.HeadHunter, "Chasseur de têtes", "{+50 %|+100 %|+150 %} ➶ à la tête", "{+50 %|+100 %|+150 %} ➶ à la tête", k_Keep),
            (UpgradeEffect.Multishot, "Multitir", "{+1 flèche|+2 flèches|+3 flèches} à chaque tir", "{+1 flèche|+2 flèches|+3 flèches} à chaque tir",
             k_Keep),
            (UpgradeEffect.Echo, "Tir écho", "Répète le tir {1 fois|2 fois|3 fois} après un court délai", "{1 écho|2 échos|3 échos}", k_Keep),
            (UpgradeEffect.Triple, "Tir triple", "{3 flèches|5 flèches|7 flèches} en éventail, dans le plan de l'arc : penche l'arc pour le tourner",
             "{3|5|7} flèches en éventail", k_Keep),
            (UpgradeEffect.Trance, "Transe",
             "Chaque tir parfait d'affilée frappe plus fort ({+15 %|+20 %|+25 %} ➶, 5 fois au plus) ; à partir du 5e, les flèches traversent tout",
             "{+15 %|+20 %|+25 %} ➶ par tir parfait d'affilée", k_Common),
            (UpgradeEffect.Falcon, "Patience", "À pleine tension, le temps ralentit pendant {1,5|2|2,5} s", "Ralenti : {1,5 s|2 s|2,5 s}", k_Common),
            (UpgradeEffect.Siege, "Surcharge",
             "Garde la tension au bout de l'anneau : le tir se charge, jusqu'à {×2,5|×3|×3,5}, et traverse {3|4|5} ennemis",
             "Charge {×2,5|×3|×3,5} ; {3|4|5} ennemis traversés", k_Keep),
            (UpgradeEffect.Rain, "Pluie de flèches",
             "En vol, la flèche sème des flèches tout le long de sa traînée : une tous les {4|3|2} m (jusqu'à {16|24|32}), 35 % ➶ chacune",
             "Une flèche tous les {4 m|3 m|2 m} ; {16|24|32} au plus", k_Legendary),
            (UpgradeEffect.Ghost, "Corde fantôme",
             "Juste après un tir, tire la corde à vide : une flèche fantôme part ({120 %|150 %|200 %} ➶)", "{120 %|150 %|200 %} ➶", k_Rare),

            // Talismans.
            (UpgradeEffect.Vampirism, "Vampirisme", "{+3|+6|+9} PV par tir à la tête", "{+3|+6|+9} PV par tir à la tête", k_Keep),
            (UpgradeEffect.Acrobat, "Le style, ça compte aussi",
             "En l'air ou en glissade : le temps ralentit ({×0,5|×0,4|×0,3}), et chaque tir est parfait", "Temps {×0,5|×0,4|×0,3} en l'air", k_Common),
            (UpgradeEffect.Sniper, "Talisman du sniper", "{+2 %|+3 %|+4 %} ➶ par mètre au-delà de 15 m, mais 50 % ➶ sous 10 m",
             "{+2 %|+3 %|+4 %} ➶ par mètre au-delà de 15 m", k_Keep),
            (UpgradeEffect.GlassPact, "Canon de verre", "➶ {×2|×2,5|×3}, mais chaque tir raté coûte 5 PV", "➶ {×2|×2,5|×3}", k_Keep),
            (UpgradeEffect.Engineer, "Talisman de l'ingénieur",
             "Une flèche dans ta baliste la fait tirer {2|4|6} fois plus vite pendant {5|7|9} s ; un tir dans la barricade la répare de {10 %|15 %|20 %}",
             "Cadence {×2|×4|×6} pendant {5 s|7 s|9 s} ; réparation {10 %|15 %|20 %}", k_Keep),
            (UpgradeEffect.Duelist, "Talisman du duelliste", "Coup de flèche au corps à corps : {×4|×8|×12} ➶, et le tir suivant est parfait",
             "Corps à corps {×4|×8|×12} ➶", k_Common),
            (UpgradeEffect.Executioner, "Talisman du bourreau",
             "Un tir à la tête qui laisse l'ennemi sous {25 %|37 %|50 %} de ses PV le tue net (un boss sous {10 %|15 %|20 %})",
             "Seuil {25 %|37 %|50 %} (boss {10 %|15 %|20 %})", k_Keep),
            (UpgradeEffect.SuperGrenade, "Super grenade",
             "La grenade à flèches en projette {12|24|36} de plus et recharge {50 %|60 %|70 %} plus vite",
             "{+12|+24|+36} flèches ; recharge {50 %|60 %|70 %} plus rapide", k_Rare),
            (UpgradeEffect.Miser, "Talisman de l'avare", "+1 % ➶ par tranche de {20|15|10} or gardés (jusqu'à {+100 %|+150 %|+200 %})",
             "+1 % ➶ par {20|15|10} or ; jusqu'à {+100 %|+150 %|+200 %}", k_Common),

            // Parchemins (sans limite : le même texte à chaque exemplaire).
            (UpgradeEffect.Strength, "Force", "+12 % ➶, flèches un peu plus rapides", "+12 % ➶ de plus, flèches un peu plus rapides", k_Keep),
            (UpgradeEffect.Dexterity, "Adresse", "Anneau 10 % plus rapide, bande verte 10 % plus large",
             "Anneau 10 % plus rapide, bande verte 10 % plus large", k_Keep),
            (UpgradeEffect.Vigor, "Vigueur", "+25 PV max", "+25 PV max de plus", k_Keep),
        };

        static RoguelikeCardsBaker()
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

            EnsureSymbols();
            UpdateCatalog();
        }

        // Deux cases carrées côte à côte (la flèche, puis « → ») ; à la taille de la police (sa ligne des majuscules), un peu sous
        // la ligne de base, de la couleur du texte (tint=1 dans la balise).
        static void EnsureSymbols()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(k_SymbolsPath) != null)
                return;

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(k_SymbolsTexture);
            var shader = Shader.Find("TextMeshPro/Sprite");
            if (texture == null || shader == null)
                return;

            if (!AssetDatabase.IsValidFolder(k_SymbolsFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "Sprite Assets");

            var asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
            asset.name = "Symboles";
            asset.spriteSheet = texture;
            var size = texture.height;
            string[] names = { "fleche", "vers" };
            for (var i = 0; i < names.Length; i++)
            {
                var glyph = new TMP_SpriteGlyph((uint)i, new GlyphMetrics(size, size, 0f, size * 0.85f, size), new GlyphRect(i * size, 0, size, size), 1f, 0);
                asset.spriteGlyphTable.Add(glyph);
                asset.spriteCharacterTable.Add(new TMP_SpriteCharacter(0xFFFE, glyph) { name = names[i] });
            }

            AssetDatabase.CreateAsset(asset, k_SymbolsPath);
            var material = new Material(shader) { name = "Symboles Material" };
            material.SetTexture(ShaderUtilities.ID_MainTex, texture);
            AssetDatabase.AddObjectToAsset(material, asset);
            asset.material = material;

            // La version des tables : sans elle, TMP croirait à un ancien sprite asset et viderait ses tables au chargement.
            var serialized = new SerializedObject(asset);
            var version = serialized.FindProperty("m_Version");
            if (version != null)
                version.stringValue = "1.1.0";
            serialized.ApplyModifiedPropertiesWithoutUndo();
            asset.UpdateLookupTables();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
            Debug.Log("Cartes du 27 octobre : le sprite des symboles (la flèche des dégâts, « → ») est créé : " + k_SymbolsPath + ".", asset);
        }

        static void UpdateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(k_CatalogPath);
            if (catalog == null)
                return;

            var icons = new Dictionary<ArrowSlot, Sprite>
            {
                { ArrowSlot.Tip, Icon("Icone_Pointe") },
                { ArrowSlot.Fletching, Icon("Icone_Empennage") },
                { ArrowSlot.Shaft, Icon("Icone_Fut") },
                { ArrowSlot.String, Icon("Icone_Corde") },
                { ArrowSlot.Talisman, Icon("Icone_Talisman") },
            };
            var scrollIcon = Icon("Icone_Parchemin");
            if (scrollIcon == null || icons.ContainsValue(null))
                return;

            var changed = new List<string>();
            foreach (var card in k_Cards)
            {
                foreach (var upgrade in catalog.upgrades)
                {
                    if (upgrade == null || upgrade.kind == UpgradeKind.Retired || upgrade.effect != card.effect ||
                        !string.IsNullOrEmpty(upgrade.levelUpText))
                        continue;

                    upgrade.displayName = card.name;
                    upgrade.description = card.description;
                    upgrade.levelUpText = card.levelUp;
                    if (card.rarity != k_Keep)
                        upgrade.rarity = (UpgradeRarity)card.rarity;
                    if (upgrade.kind == UpgradeKind.Scroll)
                        upgrade.icon = scrollIcon;
                    else if (icons.TryGetValue(upgrade.slot, out var icon))
                        upgrade.icon = icon;
                    changed.Add(card.name);
                }
            }

            if (changed.Count == 0)
                return;

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log($"Cartes du 27 octobre (catalogue) : {changed.Count} cartes refaites (nom, texte par niveau, montée de niveau, " +
                      $"rareté, icône de la catégorie) : {string.Join(", ", changed)}.", catalog);
        }

        static Sprite Icon(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(k_IconFolder + name + ".png");
    }
}
