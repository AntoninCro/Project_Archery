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

        // Les noms choisis par le joueur le 27 octobre au soir (Docs/Cartes_A_Refaire.xlsx) : Conduction ++ (Tempête avant), Here we
        // go again (Faux), Donut (Essaim), Focus (Chasse à l'homme), Bombe atomique (Billard), Éclair givrant (Clôture de givre),
        // 360 no scope headshot (Étoile filante), Tir de lumière (Rail), Flèche à fragmentation + (Fragmentation). Les noms des
        // champs du code et les commentaires gardent les anciens noms.

        // Revu le 26 octobre (demandé par le joueur) : un vrai trou noir se forme sur la flèche plantée (BlackHole), aspire, puis explose.
        public static readonly Duo BlackHole = new Duo("Trou noir", "La flèche plantée devient un trou noir qui aspire les ennemis, puis explose deux fois plus large et plus fort",
                                                       new[] { UpgradeEffect.Magnet }, new[] { UpgradeEffect.Fuse });

        public static readonly Duo Tempest = new Duo("Conduction ++", "Les arcs électriques appellent la foudre sur ce qui les traverse",
                                                     new[] { UpgradeEffect.Lightning, UpgradeEffect.ChainLightning }, new[] { UpgradeEffect.Conductor });

        public static readonly Duo Scythe = new Duo("Here we go again", "La flèche boomerang fait deux allers-retours, avec une pause devant toi",
                                                    new[] { UpgradeEffect.Boomerang }, new[] { UpgradeEffect.Piercing });

        public static readonly Duo Swarm = new Duo("Donut", "Les flèches suspendues tournent autour de toi et partent seules sur les ennemis",
                                                   new[] { UpgradeEffect.Suspended }, new[] { UpgradeEffect.Homing });

        public static readonly Duo Blizzard = new Duo("Blizzard", "Un ennemi gelé gèle ses voisins",
                                                      new[] { UpgradeEffect.FrostTip }, new[] { UpgradeEffect.Triple });

        public static readonly Duo HawkEye = new Duo("Œil de faucon", "Pendant le ralenti du faucon, les points faibles brillent à travers le décor ; ils prennent ×1,5",
                                                     new[] { UpgradeEffect.Falcon }, new[] { UpgradeEffect.Sniper });

        // Les duos du 26 octobre au soir (proposés par Claude, gardés par le joueur ; ce qu'ils font est dans ArrowPieces.Lot3, et
        // Supernova et Étoile filante dans SpecialArrows).

        public static readonly Duo MagneticStorm = new Duo("Orage magnétique", "À la fin de l'aspiration, un éclair frappe chaque ennemi attiré",
                                                           new[] { UpgradeEffect.Magnet }, new[] { UpgradeEffect.Lightning, UpgradeEffect.ChainLightning });

        public static readonly Duo GrenadeLauncher = new Duo("Lance-grenades", "La flèche explose à chaque rebond",
                                                             new[] { UpgradeEffect.Bounce }, new[] { UpgradeEffect.Fuse });

        public static readonly Duo Supernova = new Duo("Supernova", "Dès le 5e tir parfait d'affilée, chaque flèche dorée explose deux fois plus fort et plus large",
                                                       new[] { UpgradeEffect.Trance }, new[] { UpgradeEffect.Explosive });

        public static readonly Duo Manhunt = new Duo("Focus", "La cible marquée s'exécute sous un seuil ×1,5, et la marque saute",
                                                     new[] { UpgradeEffect.HunterMark }, new[] { UpgradeEffect.Executioner });

        public static readonly Duo IronRain = new Duo("Pluie de fer", "Chaque onde de choc du plomb plante 3 pieux autour d'elle",
                                                      new[] { UpgradeEffect.Lead }, new[] { UpgradeEffect.Spike });

        public static readonly Duo Billiard = new Duo("Bombe atomique", "Les flèches des détonations font éclater les autres détonateurs : réaction en chaîne !",
                                                      new[] { UpgradeEffect.Detonator }, new[] { UpgradeEffect.Ricochet });

        public static readonly Duo FrostFence = new Duo("Éclair givrant", "Les arcs électriques gèlent ce qui les traverse",
                                                        new[] { UpgradeEffect.Conductor }, new[] { UpgradeEffect.FrostTip });

        public static readonly Duo Network = new Duo("Réseau", "Les arcs électriques relient tes flèches à tes balistes, qu'ils surchargent",
                                                     new[] { UpgradeEffect.Conductor }, new[] { UpgradeEffect.Engineer });

        public static readonly Duo Starfall = new Duo("360 no scope headshot", "En l'air ou en glissade, la corde triple tire trois fois plus de flèches, en couronne autour du point visé",
                                                      new[] { UpgradeEffect.Acrobat }, new[] { UpgradeEffect.Triple });

        public static readonly Duo Rail = new Duo("Tir de lumière", "La flèche devient un trait de lumière, trois fois plus rapide, qui traverse tout et laisse une traînée qui blesse",
                                                  new[] { UpgradeEffect.Feather }, new[] { UpgradeEffect.Piercing });

        // Revu le 27 octobre (la pointe de givre gèle à chaque flèche) ; avant : « un ennemi sur la glace gèle au premier tir ».
        public static readonly Duo Glacier = new Duo("Glacier", "Un ennemi qui éclate sur la glace gèle tous ceux qui s'y trouvent",
                                                     new[] { UpgradeEffect.Frost }, new[] { UpgradeEffect.FrostTip });

        // Les duos du rebond (27 octobre, demandé par le joueur : le rebond fait un duo avec les pièces qui agissent plantées, pas
        // seulement la mèche ; noms et textes proposés par Claude). Ce qu'ils font : ArrowPieces.Lot3, OnBounce. Le rappel est aussi
        // un empennage : pas de duo possible avec le rebond.

        public static readonly Duo Patrol = new Duo("Patrouille", "À chaque rebond, la flèche tire des petites flèches sur les ennemis les plus proches",
                                                    new[] { UpgradeEffect.Bounce }, new[] { UpgradeEffect.Sentinel });

        public static readonly Duo Caltrops = new Duo("Chausse-trappes", "Chaque rebond au sol sème deux pieux autour de lui, à pleine force",
                                                      new[] { UpgradeEffect.Bounce }, new[] { UpgradeEffect.Spike });

        public static readonly Duo HighVoltage = new Duo("Haute tension", "Chaque rebond laisse un point électrique : le trajet devient une clôture",
                                                         new[] { UpgradeEffect.Bounce }, new[] { UpgradeEffect.Conductor });

        public static readonly Duo Pinball = new Duo("Flipper", "Chaque rebond attire un instant les ennemis vers lui",
                                                     new[] { UpgradeEffect.Bounce }, new[] { UpgradeEffect.Magnet });

        public static readonly Duo Fragmentation = new Duo("Flèche à fragmentation +", "À chaque rebond, des flèches du détonateur partent sur les ennemis proches",
                                                           new[] { UpgradeEffect.Bounce }, new[] { UpgradeEffect.Detonator });

        public static readonly Duo IceRink = new Duo("Patinoire", "Chaque rebond laisse une zone de glace",
                                                     new[] { UpgradeEffect.Bounce }, new[] { UpgradeEffect.Frost });

        // Les duos du boomerang (27 octobre, demandé par le joueur : comme ceux du rebond ; la flèche boomerang ne se plante presque
        // jamais, ses partenaires agissent donc au demi-tour ; noms et textes proposés par Claude). Ce qu'ils font : ArrowPieces.Lot3,
        // OnBoomerangTurn.

        public static readonly Duo Backfire = new Duo("Retour de flamme", "Au demi-tour, la flèche boomerang explose comme la mèche",
                                                      new[] { UpgradeEffect.Boomerang }, new[] { UpgradeEffect.Fuse });

        public static readonly Duo Whirlwind = new Duo("Tourbillon", "Au demi-tour, un aimant attire les ennemis sous la flèche",
                                                       new[] { UpgradeEffect.Boomerang }, new[] { UpgradeEffect.Magnet });

        public static readonly Duo Escort = new Duo("Escorte", "Au demi-tour, la flèche tire des petites flèches sur les ennemis proches",
                                                    new[] { UpgradeEffect.Boomerang }, new[] { UpgradeEffect.Sentinel });

        public static readonly Duo LiveWire = new Duo("Fil électrique", "Au demi-tour, un arc électrique relie la flèche à son point de départ",
                                                      new[] { UpgradeEffect.Boomerang }, new[] { UpgradeEffect.Conductor });

        public static readonly Duo Sower = new Duo("Semeur", "Au demi-tour, la flèche sème trois pieux au sol",
                                                   new[] { UpgradeEffect.Boomerang }, new[] { UpgradeEffect.Spike });

        public static readonly Duo Shrapnel = new Duo("Mitraille", "Au demi-tour, des flèches du détonateur partent sur les ennemis proches",
                                                      new[] { UpgradeEffect.Boomerang }, new[] { UpgradeEffect.Detonator });

        // Les derniers duos de compatibilité (27 octobre, choisis par le joueur parmi les propositions de Docs/Refonte_Roguelike.md,
        // partie 15 ; ce qu'ils font : ArrowPieces.Lot3).

        public static readonly Duo Cloud = new Duo("Nuage", "Tirée vers le ciel, la flèche plume s'arrête et fait pleuvoir des flèches un moment",
                                                   new[] { UpgradeEffect.Feather }, new[] { UpgradeEffect.Rain });

        public static readonly Duo ExplosiveRecall = new Duo("Retour explosif", "Rappelées, les flèches à mèche explosent et les détonateurs éclatent, puis elles reviennent",
                                                             new[] { UpgradeEffect.Recall }, new[] { UpgradeEffect.Fuse, UpgradeEffect.Detonator });

        public static readonly Duo Downpour = new Duo("Averse", "Relâchées, les flèches suspendues font chacune pleuvoir sur le point visé",
                                                      new[] { UpgradeEffect.Suspended }, new[] { UpgradeEffect.Rain });

        static readonly Duo[] s_All =
        {
            BlackHole, Tempest, Scythe, Swarm, Blizzard, HawkEye, MagneticStorm, GrenadeLauncher, Supernova, Manhunt, IronRain, Billiard,
            FrostFence, Network, Starfall, Rail, Glacier, Patrol, Caltrops, HighVoltage, Pinball, Fragmentation, IceRink,
            Backfire, Whirlwind, Escort, LiveWire, Sower, Shrapnel, Cloud, ExplosiveRecall, Downpour,
        };

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
        /// La pièce possédée (équipée ou rangée dans l'atelier) qui forme <paramref name="duo"/> avec <paramref name="piece"/> :
        /// les cartes, les orbes et les messages la nomment (26 octobre, demandé par le joueur). Null si aucune.
        /// </summary>
        public static Upgrade PartnerOf(Duo duo, Upgrade piece, PlayerUpgrades owned)
        {
            if (duo == null || piece == null || owned == null)
                return null;

            var first = duo.InFirst(piece.effect);
            foreach (var upgrade in owned.Owned)
            {
                if (upgrade != null && upgrade.effect != piece.effect && (first ? duo.InSecond(upgrade.effect) : duo.InFirst(upgrade.effect)))
                    return upgrade;
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
