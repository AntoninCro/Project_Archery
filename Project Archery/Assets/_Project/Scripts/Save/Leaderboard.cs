using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Archery.Save
{
    /// <summary>Une partie du classement.</summary>
    [Serializable]
    public class LeaderboardEntry
    {
        public string name;
        public int score;
        public string difficulty;

        /// <summary>Dernière vague atteinte.</summary>
        public int wave;

        public int kills;

        /// <summary>Date de la partie, « 2026-10-23 14:05 ».</summary>
        public string date;

        /// <summary>L'épreuve du portail des étoiles a été réussie pendant la partie : une étoile à côté du nom.</summary>
        public bool stars;

        /// <summary>L'épreuve du toit cyberpunk a été réussie pendant la partie : un losange à côté du nom.</summary>
        public bool glitch;

        /// <summary>
        /// Vide pour une vraie partie. Sinon, une ligne que le jeu écrit lui-même (Lore/ThinkTraces) : « think », « reflet »
        /// ou « copie ».
        /// </summary>
        public string kind;

        public bool IsPlayer => string.IsNullOrEmpty(kind);

        public static string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
    }

    /// <summary>Contenu du fichier leaderboard.json.</summary>
    [Serializable]
    public class LeaderboardData
    {
        public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
    }

    /// <summary>
    /// Classement des parties (GDD, sections 14 et 15), enregistré dans <c>leaderboard.json</c>
    /// et trié du meilleur score au moins bon. À égalité de score, la vague la plus haute passe devant,
    /// puis la partie la plus ancienne.
    /// </summary>
    public static class Leaderboard
    {
        public const string FileName = "leaderboard.json";

        // Le fichier garde plus de parties que les 10 affichées.
        const int k_MaxEntries = 100;

        static LeaderboardData s_Data;

        public static IReadOnlyList<LeaderboardEntry> Entries
        {
            get
            {
                EnsureLoaded();
                return s_Data.entries;
            }
        }

        /// <summary>
        /// Ajoute une partie, trie et enregistre. Renvoie son rang (0 = première place).
        /// <paramref name="traces"/> : les lignes que le jeu écrit lui-même après cette partie (Lore/ThinkTraces) ; elles
        /// remplacent toutes les anciennes.
        /// </summary>
        public static int Add(LeaderboardEntry entry, List<LeaderboardEntry> traces = null)
        {
            if (entry == null)
                return -1;

            EnsureLoaded();
            if (traces != null)
            {
                s_Data.entries.RemoveAll(other => !other.IsPlayer);
                s_Data.entries.AddRange(traces);
            }

            s_Data.entries.Add(entry);
            s_Data.entries.Sort(Compare);
            var rank = s_Data.entries.IndexOf(entry);
            if (s_Data.entries.Count > k_MaxEntries)
                s_Data.entries.RemoveRange(k_MaxEntries, s_Data.entries.Count - k_MaxEntries);

            JsonStorage.Save(FileName, s_Data);
            return rank < k_MaxEntries ? rank : -1;
        }

        /// <summary>
        /// Aucune vraie partie ne passe devant celle de ce rang : c'est le record du joueur, même si une ligne écrite par
        /// le jeu (le score impossible de THINK) reste au-dessus.
        /// </summary>
        public static bool IsPlayerRecord(int rank)
        {
            EnsureLoaded();
            if (rank < 0 || rank >= s_Data.entries.Count)
                return false;

            for (var i = 0; i < rank; i++)
            {
                if (s_Data.entries[i].IsPlayer)
                    return false;
            }

            return true;
        }

        static int Compare(LeaderboardEntry a, LeaderboardEntry b)
        {
            var byScore = b.score.CompareTo(a.score);
            if (byScore != 0)
                return byScore;

            var byWave = b.wave.CompareTo(a.wave);
            if (byWave != 0)
                return byWave;

            // À égalité, les vraies parties passent devant les lignes du jeu : le reflet reste juste sous ta partie.
            var byKind = b.IsPlayer.CompareTo(a.IsPlayer);
            return byKind != 0 ? byKind : string.CompareOrdinal(a.date, b.date);
        }

        static void EnsureLoaded()
        {
            if (s_Data != null)
                return;

            if (!JsonStorage.TryLoad(FileName, out s_Data) || s_Data.entries == null)
                s_Data = new LeaderboardData();

            s_Data.entries.RemoveAll(entry => entry == null);
            s_Data.entries.Sort(Compare);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_Data = null;
    }
}
