#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using Archery.Building;
using Archery.Defense;
using Archery.Player;
using Archery.Shop;
using Archery.UI;
using Archery.Upgrades;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Archery.Core
{
    /// <summary>
    /// Raccourci de la démo du 23 octobre, pour que le professeur puisse affronter le boss final : la touche F donne
    /// toutes les pièces de la flèche au niveau maximal (la première de chaque emplacement équipée, les autres dans l'atelier
    /// de la flèche), les objets hors emplacement et 5 exemplaires de chaque parchemin (depuis la refonte roguelike des 25 et
    /// 26 octobre ; avant, 2 exemplaires de chaque amélioration), et remplit les emplacements de tourelle (au bord de la voie)
    /// de tourelles niveau 2, balistes et tours de givre en alternance autour de la tour. Une fois par partie.
    /// </summary>
    /// <remarks>
    /// Seulement dans l'éditeur et les builds de développement, comme les autres raccourcis de démo. Il se crée tout
    /// seul au lancement : rien à poser dans la scène. Une construction en cours est d'abord terminée ; une tourelle déjà
    /// là monte au niveau 2, les emplacements de la voie (barricades, pièges) ne changent pas.
    /// </remarks>
    [DisallowMultipleComponent]
    public class DemoBoost : MonoBehaviour
    {
        // Les parchemins remplacent les anciennes cartes de statistiques (dont F donnait 2 exemplaires).
        const int k_ScrollCopies = 5;
        const int k_TurretLevel = 2;
        static readonly Color k_Color = new Color(1f, 0.85f, 0.3f);

        bool m_Used;
        int m_NextPiece;

        // Le niveau que la touche Tab a donné à toutes les pièces pendant cette partie (0 : pas encore).
        int m_AllLevel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            var go = new GameObject("[Demo Boost]");
            DontDestroyOnLoad(go);
            go.AddComponent<DemoBoost>();
        }

        void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

        void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        // La scène recommence (nouvelle partie) : le raccourci resservira.
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single)
            {
                m_Used = false;
                m_AllLevel = 0;
            }
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || DesktopHand.IsTypingText)
                return;

            if (keyboard.rKey.wasPressedThisFrame)
                GiveNextNewPiece();
            if (keyboard.tabKey.wasPressedThisFrame)
                GiveAllNextLevel();
            if (!keyboard.fKey.wasPressedThisFrame)
                return;

            if (m_Used)
            {
                HeadMessages.Show("Démo : déjà fait pour cette partie", k_Color, 2.5f);
                return;
            }

            m_Used = true;
            var upgrades = GiveUpgrades();
            var turrets = FillTurrets();
            HeadMessages.Show($"Démo : {upgrades} améliorations, {turrets} tourelles niveau {k_TurretLevel}", k_Color, 4f);
        }

        // Raccourci de test (26 octobre) : R donne la pièce nouvelle suivante des étapes 3 et 5 (de la corde triple au talisman
        // du bourreau, 24 pièces), un niveau de plus, et l'équipe : appuyer plusieurs fois pour essayer les combinaisons.
        void GiveNextNewPiece()
        {
            var player = PlayerUpgrades.Instance;
            var catalog = ShopManager.Instance != null ? ShopManager.Instance.Catalog : null;
            if (player == null || catalog == null)
                return;

            var pieces = catalog.upgrades.FindAll(upgrade => upgrade != null && upgrade.kind == UpgradeKind.Object &&
                                                             upgrade.effect >= UpgradeEffect.Triple);
            if (pieces.Count == 0)
            {
                HeadMessages.Show("Essai : aucune pièce nouvelle dans le catalogue", k_Color, 2.5f);
                return;
            }

            var piece = pieces[m_NextPiece++ % pieces.Count];
            if (player.CanTake(piece))
                player.Add(piece);
            player.Equip(piece);
            var level = player.Level(piece.effect);
            HeadMessages.Show($"Essai : {piece.displayName}\n<size=60%>{ShopCatalog.NameOf(piece.slot)} · niveau {level}</size>", k_Color, 2.5f);
        }

        // Raccourci de test (26 octobre au soir, demandé par le joueur) : Tab donne toutes les pièces de la flèche au niveau 1,
        // un deuxième appui au niveau 2, un troisième au niveau 3 (la première de chaque emplacement s'équipe, les autres
        // attendent dans l'atelier). Pas les parchemins ni les tourelles (F les donne) ; autant de fois qu'on veut.
        void GiveAllNextLevel()
        {
            var player = PlayerUpgrades.Instance;
            var catalog = ShopManager.Instance != null ? ShopManager.Instance.Catalog : null;
            if (player == null || catalog == null)
                return;

            var level = m_AllLevel + 1;
            var given = 0;
            var pieces = 0;
            foreach (var upgrade in catalog.upgrades)
            {
                if (upgrade == null || upgrade.kind != UpgradeKind.Object)
                    continue;

                pieces++;
                while (player.Level(upgrade.effect) < level && player.CanTake(upgrade))
                {
                    player.Add(upgrade);
                    given++;
                }
            }

            if (given == 0 && level > 1)
            {
                HeadMessages.Show("Essai : toutes les pièces sont déjà au niveau maximal", k_Color, 2.5f);
                return;
            }

            m_AllLevel = level;
            HeadMessages.Show($"Essai : les {pieces} pièces au niveau {level}\n<size=60%>Tab encore : niveau suivant · l'atelier pour équiper</size>",
                              k_Color, 3f);
        }

        static int GiveUpgrades()
        {
            var player = PlayerUpgrades.Instance;
            var shop = ShopManager.Instance;
            var catalog = shop != null ? shop.Catalog : null;
            if (player == null || catalog == null)
                return 0;

            // Toutes les pièces, au niveau maximal (26 octobre) : la première de chaque emplacement s'équipe, les autres attendent
            // dans l'atelier, pour essayer les combinaisons.
            var given = 0;
            foreach (var upgrade in catalog.upgrades)
            {
                if (upgrade == null || upgrade.kind == UpgradeKind.Retired)
                    continue;

                var copies = upgrade.kind == UpgradeKind.Scroll ? k_ScrollCopies : Mathf.Max(1, upgrade.maxStacks);
                for (var i = 0; i < copies && player.CanTake(upgrade); i++)
                {
                    player.Add(upgrade);
                    given++;
                }
            }

            return given;
        }

        static int FillTurrets()
        {
            var manager = BuildManager.Instance;
            if (manager == null)
                return 0;

            manager.FinishNow();
            var choices = new List<StructureDefinition>();
            manager.GetChoices(BuildSlot.Roadside, choices);

            // Les balistes et les tours de givre alternent (depuis le 28 octobre, bien d'autres bâtiments se posent au bord de la
            // voie : on garde ces deux-là, comme avant).
            choices.RemoveAll(definition => definition.kind != StructureKind.Ballista && definition.kind != StructureKind.FrostTower);
            if (choices.Count == 0)
                manager.GetChoices(BuildSlot.Roadside, choices);
            if (choices.Count == 0)
                return 0;

            // Depuis la refonte du 27 octobre (étape 3), 6 places au bord de la voie, les plus proches de la tour, à 8 m au moins
            // les unes des autres (avant : les 6 socles au bord de la voie posés à la main) ; depuis le 28 octobre, leur emplacement
            // est créé sur la grille avec les cases du bâtiment. Les socles posés à la main qui portent un bâtiment montent aussi.
            var grid = BuildGrid.Instance;
            var sockets = new List<BuildSocket>();
            var definitions = new List<StructureDefinition>();
            var taken = new List<Vector3>();
            for (var i = 0; i < 6 && grid != null && grid.IsReady; i++)
            {
                var definition = choices[i % choices.Count];
                var socket = grid.ClaimFree(definition, taken, 8f);
                if (socket == null)
                    break;
                sockets.Add(socket);
                definitions.Add(definition);
            }

            foreach (var socket in BuildSocket.All)
            {
                if (socket != null && socket.isActiveAndEnabled && !socket.IsCandidate && socket.Slot == BuildSlot.Roadside && socket.Structure != null)
                {
                    sockets.Add(socket);
                    definitions.Add(null);
                }
            }

            var count = 0;
            for (var i = 0; i < sockets.Count; i++)
            {
                var socket = sockets[i];
                var structure = socket.Structure;
                if (structure == null)
                {
                    var definition = definitions[i];
                    if (definition == null || definition.MaxLevel < 1)
                    {
                        grid.Release(socket);
                        continue;
                    }
                    structure = socket.Place(definition);
                }

                var target = Mathf.Min(k_TurretLevel, structure.MaxLevel);
                if (structure.Level >= target)
                    continue;

                // Niveau par niveau, comme au marteau, mais sans plan bleu ni coups.
                for (var level = structure.Level + 1; level <= target; level++)
                {
                    structure.BeginConstruction(level, null);
                    structure.FinishConstruction();
                }

                count++;
            }

            return count;
        }
    }
}
#endif
