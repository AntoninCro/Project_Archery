using System;
using UnityEngine;

namespace Archery.Save
{
    /// <summary>Contenu du fichier memory.json.</summary>
    [Serializable]
    public class GameMemoryData
    {
        /// <summary>Parties finies (mort ou victoire), depuis toujours.</summary>
        public int gamesFinished;

        /// <summary>Date de la première partie finie, « 2026-10-25 14:05 ».</summary>
        public string firstGame;

        /// <summary>Date de la dernière partie finie.</summary>
        public string lastGame;
    }

    /// <summary>
    /// La mémoire du jeu d'une partie à l'autre, enregistrée dans <c>memory.json</c>, à côté du classement : ce que THINK
    /// sait du joueur (Lore.md ; GDD, sections 15 et 24). Pour l'instant, le nombre de parties finies, qui fait apparaître
    /// ses traces peu à peu (dossier <c>Lore</c>) : les premières parties restent un jeu d'arcade innocent.
    /// </summary>
    public static class GameMemory
    {
        public const string FileName = "memory.json";

        static GameMemoryData s_Data;

        public static int GamesFinished
        {
            get
            {
                EnsureLoaded();
                return s_Data.gamesFinished;
            }
        }

        /// <summary>Une partie vient de finir (l'écran de fin apparaît) : on la compte et on enregistre.</summary>
        public static void RecordGameFinished()
        {
            EnsureLoaded();
            s_Data.gamesFinished++;
            var now = LeaderboardEntry.Now();
            if (string.IsNullOrEmpty(s_Data.firstGame))
                s_Data.firstGame = now;
            s_Data.lastGame = now;
            JsonStorage.Save(FileName, s_Data);
        }

        static void EnsureLoaded()
        {
            if (s_Data != null)
                return;

            if (JsonStorage.TryLoad(FileName, out s_Data))
                return;

            // Pas encore de mémoire (première partie, ou jeu installé avant le 25 octobre) : le jeu se souvient déjà des
            // parties enregistrées au classement.
            s_Data = new GameMemoryData();
            foreach (var entry in Leaderboard.Entries)
            {
                if (entry.IsPlayer)
                    s_Data.gamesFinished++;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_Data = null;
    }
}
