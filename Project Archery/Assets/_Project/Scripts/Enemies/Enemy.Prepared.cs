using System;
using Archery.Bows;
using Archery.Combat;
using UnityEngine;

namespace Archery.Enemies
{
    /// <summary>
    /// Ce qui « prépare » un ennemi pour les coups du joueur (l'ennemi préparé, lot 3 de la refonte du tower defense,
    /// 30 octobre). Les numéros sont enregistrés dans la scène (les mots et les sons du <c>ScoreManager</c>) : ajouter les
    /// nouvelles à la fin, sans en retirer ni en déplacer.
    /// </summary>
    public enum Preparation
    {
        None,

        /// <summary>En l'air : projeté (tremplin, chute d'une bulle…), dans une bulle, tenu par la singularité.</summary>
        Airborne,

        /// <summary>Pris dans la glace de la tour de givre : le coup la brise.</summary>
        Frozen,

        /// <summary>Cloué au sol par le harpon de la baliste.</summary>
        Pinned,

        /// <summary>Un volant pris au filet (lance-filet, harpon, tour-aimant).</summary>
        Netted,

        /// <summary>Étourdi : la herse, le filet électrique, il se relève d'une chute.</summary>
        Stunned,

        /// <summary>Englué dans le goudron du mortier.</summary>
        Glued,

        /// <summary>Attiré par la tour-aimant.</summary>
        Pulled,

        /// <summary>Dans le rayon du phare : ses points faibles brillent.</summary>
        Marked,
    }

    // L'ennemi préparé (lot 3 de la refonte du tower defense, 30 octobre ; décidé le 29, Docs/Refonte_TowerDefense.md, 12.3,
    // piste A) : les bâtiments préparent les tirs du joueur. Un coup du joueur (flèche, katana, faille) sur un ennemi en l'air,
    // gelé, cloué, pris au filet, étourdi, englué, attiré ou marqué fait plus de dégâts (selon la sorte, depuis le 31 octobre)
    // et rapporte 1,5 fois plus de points, avec un mot et un son pour chaque sorte (ScoreManager). Le tir au vol du 27 octobre
    // en devient un cas parmi d'autres. Seuls les bâtiments préparent : la flèche de glace du joueur (Freeze) ne compte pas,
    // sinon il se préparerait lui-même ses tirs.
    public partial class Enemy
    {
        // Les préparations que signale un bâtiment (gelé, cloué, englué, attiré, marqué) : jusqu'à quand (Time.time). Les autres
        // se lisent sur l'ennemi (en l'air, pris au filet, étourdi).
        static readonly int s_PreparationCount = Enum.GetValues(typeof(Preparation)).Length;
        readonly float[] m_PreparedUntil = new float[s_PreparationCount];

        // Les dégâts d'un coup du joueur, par sorte (multiplicateurs ; None : 1). Réglés par le ScoreManager (Prepared Cues).
        static readonly float[] s_PreparedDamage = DefaultDamageTable();

        /// <summary>
        /// Les dégâts proposés pour chaque sorte : plus elle est difficile à obtenir et à exploiter, plus ils montent (31 octobre,
        /// le joueur : « les dégâts augmentés dépendent de la difficulté à appliquer l'effet » ; avant, +50 % pour toutes).
        /// Le réglage du ScoreManager fait foi.
        /// </summary>
        public static float DefaultPreparedDamage(Preparation kind) => kind switch
        {
            // Une cible qui vole, vite et peu de temps : il faut le tremplin, la bulle ou la singularité, et viser juste.
            Preparation.Airborne => 2f,
            // Un seul coup en profite (il brise la glace), et le pic de glace ne gèle qu'un ennemi toutes les 4 s.
            Preparation.Frozen => 2f,
            // Le harpon : la baliste au niveau 4, et il ne cloue qu'1 s.
            Preparation.Pinned => 1.75f,
            // Un volant au sol, immobile, 1,5 à 3 s.
            Preparation.Netted => 1.5f,
            // De courts moments : la herse toutes les 8 s, le filet électrique, le relevé d'une chute.
            Preparation.Stunned => 1.5f,
            // La flaque dure plusieurs secondes, et l'ennemi ralenti est plus facile à toucher.
            Preparation.Glued => 1.3f,
            // L'aimant attire tout seul, en continu dès le niveau 3.
            Preparation.Pulled => 1.25f,
            // Le phare marque tout son rayon, sans rien faire.
            Preparation.Marked => 1.15f,
            _ => 1f,
        };

        /// <summary>Règle les dégâts d'un coup du joueur sur un ennemi préparé de cette sorte (2 = +100 %).</summary>
        public static void SetPreparedDamage(Preparation kind, float multiplier)
        {
            var index = (int)kind;
            if (index > 0 && index < s_PreparedDamage.Length)
                s_PreparedDamage[index] = Mathf.Max(0f, multiplier);
        }

        /// <summary>
        /// Ce qui le préparait au dernier coup du joueur (<see cref="Preparation.None"/> s'il ne l'était pas). Relevé avant que le
        /// coup ne compte : le coup peut le défaire (la glace se brise, il meurt). Seuls les coups du joueur le changent.
        /// </summary>
        public Preparation HitPreparation { get; private set; }

        /// <summary>Ce qui le prépare en ce moment, le plus marquant d'abord ; <see cref="Preparation.None"/> sinon.</summary>
        public Preparation CurrentPreparation
        {
            get
            {
                if (m_State == State.Dead)
                    return Preparation.None;
                if (IsPrepared(Preparation.Frozen))
                    return Preparation.Frozen;
                if (IsPrepared(Preparation.Pinned))
                    return Preparation.Pinned;
                if (m_Airborne)
                    return Preparation.Airborne;
                if (m_Netted)
                    return Preparation.Netted;
                if (IsStunned)
                    return Preparation.Stunned;
                if (IsPrepared(Preparation.Glued))
                    return Preparation.Glued;
                if (IsPrepared(Preparation.Pulled))
                    return Preparation.Pulled;
                return IsPrepared(Preparation.Marked) ? Preparation.Marked : Preparation.None;
            }
        }

        /// <summary>Un bâtiment le prépare pour ce temps (s) ; s'il l'était déjà, le plus long des deux.</summary>
        public void Prepare(Preparation kind, float duration)
        {
            var index = (int)kind;
            if (index <= 0 || index >= m_PreparedUntil.Length || m_State == State.Dead || duration <= 0f)
                return;

            m_PreparedUntil[index] = Mathf.Max(m_PreparedUntil[index], Time.time + duration);
        }

        /// <summary>Il ne l'est plus (la glace brisée ou fondue).</summary>
        public void Unprepare(Preparation kind)
        {
            var index = (int)kind;
            if (index > 0 && index < m_PreparedUntil.Length)
                m_PreparedUntil[index] = 0f;
        }

        /// <summary>Un coup du joueur : une flèche, le katana, une faille du katana.</summary>
        public static bool IsPlayerHit(in DamageInfo info) =>
            info.Source is Arrow || info.Source is IPlayerShot || info.Source is IMeleeSource;

        bool IsPrepared(Preparation kind) => m_PreparedUntil[(int)kind] > Time.time;

        static float[] DefaultDamageTable()
        {
            var table = new float[s_PreparationCount];
            for (var i = 0; i < table.Length; i++)
                table[i] = DefaultPreparedDamage((Preparation)i);
            return table;
        }

        // Le filtre des dégâts de son Health : avant que le coup ne compte, ce qui le prépare, et les dégâts de cette sorte. Les
        // coups des bâtiments ne changent pas HitPreparation : la glace qui se brise sous la flèche le blesse elle aussi, pendant
        // le coup de la flèche, et l'effacerait avant que le score ne compte l'élimination.
        float FilterPreparedDamage(DamageInfo info)
        {
            if (!IsPlayerHit(info))
                return info.Amount;

            HitPreparation = CurrentPreparation;
            return info.Amount * s_PreparedDamage[(int)HitPreparation];
        }

        void ClearPreparations()
        {
            Array.Clear(m_PreparedUntil, 0, m_PreparedUntil.Length);
            HitPreparation = Preparation.None;
        }
    }
}
