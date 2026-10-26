using System;
using System.Collections.Generic;

namespace Archery.Upgrades
{
    /// <summary>
    /// Les duos (refonte roguelike, étape 5, 26 octobre ; <c>Docs/Refonte_Roguelike.md</c>, partie 9) : deux pièces qui, réunies
    /// sur la flèche, débloquent un pouvoir en plus, comme les duos de <i>Hades</i>. Le jeu les annonce (« Duo : Trou noir ! »),
    /// les cartes et les orbes disent quand une pièce pourrait en former un, l'atelier quand l'équiper le
    /// formerait, et la corde de l'arc bat d'or tant qu'un duo est actif. Les deux pièces doivent être équipées (une pièce
    /// rangée dans l'atelier ne compte pas). Ce que fait chaque duo est dans <see cref="ArrowPieces"/>.
    /// </summary>
    public static class ArrowDuos
    {
        /// <summary>Un duo : une pièce de chaque liste (la pointe d'orage, c'est la flèche de foudre ou la chaîne d'éclairs).</summary>
        public sealed class Duo
        {
            public readonly string Name;
            public readonly string Description;
            readonly UpgradeEffect[] m_First;
            readonly UpgradeEffect[] m_Second;

            public Duo(string name, string description, UpgradeEffect[] first, UpgradeEffect[] second)
            {
                Name = name;
                Description = description;
                m_First = first;
                m_Second = second;
            }

            public bool InFirst(UpgradeEffect effect) => Array.IndexOf(m_First, effect) >= 0;
            public bool InSecond(UpgradeEffect effect) => Array.IndexOf(m_Second, effect) >= 0;

            // Une pièce de la liste joue (équipée) ; ou on la possède, équipée ou rangée dans l'atelier.
            internal bool OwnsFirst(PlayerUpgrades owned) => Owns(owned, m_First, false);
            internal bool OwnsSecond(PlayerUpgrades owned) => Owns(owned, m_Second, false);
            internal bool PossessesFirst(PlayerUpgrades owned) => Owns(owned, m_First, true);
            internal bool PossessesSecond(PlayerUpgrades owned) => Owns(owned, m_Second, true);

            static bool Owns(PlayerUpgrades owned, UpgradeEffect[] effects, bool stored)
            {
                foreach (var effect in effects)
                {
                    if ((stored ? owned.Level(effect) : owned.Stacks(effect)) > 0)
                        return true;
                }

                return false;
            }
        }

        public static readonly Duo BlackHole = new Duo("Trou noir", "L'explosion aspire encore une seconde, et son rayon double",
                                                       new[] { UpgradeEffect.Magnet }, new[] { UpgradeEffect.Fuse });

        public static readonly Duo Tempest = new Duo("Tempête", "Les arcs électriques appellent la foudre sur ce qui les traverse",
                                                     new[] { UpgradeEffect.Lightning, UpgradeEffect.ChainLightning }, new[] { UpgradeEffect.Conductor });

        public static readonly Duo Scythe = new Duo("Faux", "La flèche boomerang fait deux allers-retours",
                                                    new[] { UpgradeEffect.Boomerang }, new[] { UpgradeEffect.Piercing });

        public static readonly Duo Swarm = new Duo("Essaim", "Les flèches suspendues tournent autour de toi et partent seules sur les ennemis",
                                                   new[] { UpgradeEffect.Suspended }, new[] { UpgradeEffect.Homing });

        public static readonly Duo Blizzard = new Duo("Blizzard", "Un ennemi gelé gèle ses voisins",
                                                      new[] { UpgradeEffect.FrostTip }, new[] { UpgradeEffect.Triple });

        public static readonly Duo HawkEye = new Duo("Œil de faucon", "Pendant le ralenti du faucon, les points faibles brillent à travers le décor",
                                                     new[] { UpgradeEffect.Falcon }, new[] { UpgradeEffect.Sniper });

        static readonly Duo[] s_All = { BlackHole, Tempest, Scythe, Swarm, Blizzard, HawkEye };

        public static IReadOnlyList<Duo> All => s_All;

        /// <summary>Les deux pièces du duo sont sur la flèche (pas pendant les épreuves, où les améliorations sont mises de côté).</summary>
        public static bool IsActive(Duo duo)
        {
            var owned = PlayerUpgrades.Instance;
            return owned != null && !owned.Suspended && duo.OwnsFirst(owned) && duo.OwnsSecond(owned);
        }

        /// <summary>Au moins un duo est actif.</summary>
        public static bool AnyActive()
        {
            foreach (var duo in s_All)
            {
                if (IsActive(duo))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Le duo que <paramref name="piece"/> pourrait former avec une pièce déjà possédée (équipée ou rangée dans l'atelier),
        /// pour les cartes de la boutique et les orbes des coffres ; null si aucun, ou s'il est déjà actif.
        /// </summary>
        public static Duo Completes(Upgrade piece, PlayerUpgrades owned)
        {
            if (piece == null || owned == null || piece.kind != UpgradeKind.Object)
                return null;

            foreach (var duo in s_All)
            {
                if (!IsActive(duo) && (duo.InFirst(piece.effect) ? duo.PossessesSecond(owned) : duo.InSecond(piece.effect) && duo.PossessesFirst(owned)))
                    return duo;
            }

            return null;
        }

        /// <summary>
        /// Le duo qu'équiper <paramref name="piece"/> (possédée, rangée dans l'atelier) rendrait actif, avec les pièces équipées
        /// aujourd'hui ; null sinon. Les deux moitiés d'un duo sont toujours dans deux emplacements différents.
        /// </summary>
        public static Duo WouldActivate(Upgrade piece, PlayerUpgrades owned)
        {
            if (piece == null || owned == null || owned.Suspended || owned.IsEquipped(piece))
                return null;

            foreach (var duo in s_All)
            {
                if (!IsActive(duo) && (duo.InFirst(piece.effect) ? duo.OwnsSecond(owned) : duo.InSecond(piece.effect) && duo.OwnsFirst(owned)))
                    return duo;
            }

            return null;
        }
    }
}
