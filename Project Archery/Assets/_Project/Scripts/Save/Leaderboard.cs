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

        /// <summary>Ajoute une partie, trie et enregistre. Renvoie son rang (0 = première place).</summary>
        public static int Add(LeaderboardEntry entry)
        {
            if (entry == null)
                return -1;

            EnsureLoaded();
            s_Data.entries.Add(entry);
            s_Data.entries.Sort(Compare);
            var rank = s_Data.entries.IndexOf(entry);
            if (s_Data.entries.Count > k_MaxEntries)
                s_Data.entries.RemoveRange(k_MaxEntries, s_Data.entries.Count - k_MaxEntries);

            JsonStorage.Save(FileName, s_Data);
            return rank < k_MaxEntries ? rank : -1;
        }

        static int Compare(LeaderboardEntry a, LeaderboardEntry b)
        {
            var byScore = b.score.CompareTo(a.score);
            if (byScore != 0)
                return byScore;

            var byWave = b.wave.CompareTo(a.wave);
            return byWave != 0 ? byWave : string.CompareOrdinal(a.date, b.date);
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
