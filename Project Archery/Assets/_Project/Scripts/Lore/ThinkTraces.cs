using System.Collections.Generic;
using Archery.Save;
using UnityEngine;

namespace Archery.Lore
{
    /// <summary>
    /// Les lignes que le jeu écrit lui-même au classement (Lore.md, canon des 24 et 25 octobre 2026) :
    /// <list type="bullet">
    /// <item><b>THINK</b>, en tête, avec un score impossible : elle a caché son nom derrière THUNK, mais n'a pas résisté à se
    /// mettre première. C'est le premier indice.</item>
    /// <item><b>Ton reflet</b>, la première copie de toi, la seule consciente : ton nom à l'envers, juste sous le score de ta
    /// dernière partie.</item>
    /// <item><b>Les copies de copies</b>, qui se dégradent : ton nom à l'envers, de plus en plus abîmé, à des scores de plus
    /// en plus bas. Une de plus à chaque partie : la sauvegarde grossit.</item>
    /// </list>
    /// Elles arrivent au fil des parties finies (<see cref="GameMemory"/>) et sont refaites à chaque partie enregistrée :
    /// effacées à la main du fichier, elles reviennent.
    /// </summary>
    public static class ThinkTraces
    {
        public const string Think = "think";
        public const string Reflection = "reflet";
        public const string Copy = "copie";

        // Parties finies avant chaque trace (25 octobre, provisoire : à fixer avec la condition d'apparition des runes).
        const int k_ThinkAfter = 1;
        const int k_ReflectionAfter = 3;

        // Une copie de plus à chaque partie après la naissance du reflet, 12 au plus : le classement reste lisible.
        const int k_MaxCopies = 12;

        // Chaque copie garde 80 % du score de celle qu'elle copie.
        const float k_CopyDecay = 0.8f;

        // Le reflet s'inscrit de 1 à 9 points sous ta partie.
        const int k_ReflectionGap = 9;

        // Le score de THINK : le compteur au maximum, vingt fois les meilleures parties (5 000 à 46 000 points le 25 octobre),
        // et assez court pour tenir dans sa colonne du tableau.
        const int k_ThinkScore = 999999;
        const int k_ThinkWave = 999;
        const string k_ThinkDifficulty = "Impossible";

        // Sa date : le temps zéro des ordinateurs, bien avant le jeu. Elle n'a tué personne : elle n'a pas joué, elle a écrit
        // son score.
        const string k_ThinkDate = "1970-01-01 00:00";

        // Ce que devient une lettre abîmée : le chiffre qui lui ressemble, sinon un de ces signes.
        static readonly char[] k_Damaged = { '_', '#', '%', '?', '/' };

        /// <summary>
        /// Les lignes du jeu après la partie <paramref name="entry"/>, qui vient de finir (déjà comptée dans
        /// <see cref="GameMemory"/>), à passer à <see cref="Leaderboard.Add"/> avant de l'y ajouter. Vide aux premières parties.
        /// </summary>
        public static List<LeaderboardEntry> After(LeaderboardEntry entry)
        {
            var traces = new List<LeaderboardEntry>();
            var games = GameMemory.GamesFinished;
            if (entry == null || games < k_ThinkAfter)
                return traces;

            traces.Add(new LeaderboardEntry
            {
                name = "THINK",
                score = k_ThinkScore,
                difficulty = k_ThinkDifficulty,
                wave = k_ThinkWave,
                kills = 0,
                date = k_ThinkDate,
                kind = Think,
            });

            if (games < k_ReflectionAfter)
                return traces;

            var mirrored = Mirror(entry.name);
            var reflection = new LeaderboardEntry
            {
                name = mirrored,
                score = ReflectionScore(entry),
                difficulty = entry.difficulty,
                wave = entry.wave,
                kills = entry.kills,
                date = entry.date,
                kind = Reflection,
            };
            traces.Add(reflection);

            var copies = Mathf.Min(games - k_ReflectionAfter, k_MaxCopies);
            var score = (float)reflection.score;
            var kills = (float)entry.kills;
            for (var generation = 1; generation <= copies; generation++)
            {
                score *= k_CopyDecay;
                kills *= k_CopyDecay;
                traces.Add(new LeaderboardEntry
                {
                    name = Damage(mirrored, generation),
                    score = Mathf.FloorToInt(score),
                    difficulty = entry.difficulty,
                    wave = Mathf.Max(1, entry.wave - generation / 3),
                    kills = Mathf.FloorToInt(kills),
                    date = entry.date,
                    kind = Copy,
                });
            }

            return traces;
        }

        /// <summary>Le nom du reflet : ton nom à l'envers.</summary>
        public static string Mirror(string name)
        {
            var letters = (name ?? "").ToCharArray();
            System.Array.Reverse(letters);
            return new string(letters);
        }

        // Juste sous ta partie : quelques points de moins, sans passer sous la vraie partie suivante du classement.
        static int ReflectionScore(LeaderboardEntry entry)
        {
            var below = 0;
            foreach (var other in Leaderboard.Entries)
            {
                if (other.IsPlayer && other.score <= entry.score && other.score > below)
                    below = other.score;
            }

            return Mathf.Max(below, entry.score - Random.Range(1, k_ReflectionGap + 1));
        }

        // Une copie de copie s'abîme : à chaque génération, une lettre de plus devient un chiffre qui lui ressemble ou un
        // signe ; quand tout est abîmé, le nom raccourcit. Toujours le même tirage pour un même nom : une copie garde son
        // visage d'une partie à l'autre, seules les nouvelles sont pires.
        static string Damage(string mirrored, int generation)
        {
            var letters = new List<char>(mirrored);
            var damaged = new List<bool>(new bool[letters.Count]);
            var intact = new List<int>();
            var seed = Hash(mirrored);
            for (var step = 1; step <= generation; step++)
            {
                seed = (seed ^ (uint)step) * 16777619u;
                intact.Clear();
                for (var i = 0; i < letters.Count; i++)
                {
                    if (!damaged[i] && !char.IsWhiteSpace(letters[i]))
                        intact.Add(i);
                }

                if (intact.Count > 0)
                {
                    var index = intact[(int)(seed % (uint)intact.Count)];
                    letters[index] = Damage(letters[index], seed);
                    damaged[index] = true;
                }
                else if (letters.Count > 1)
                {
                    var index = (int)(seed % (uint)letters.Count);
                    letters.RemoveAt(index);
                    damaged.RemoveAt(index);
                }
            }

            return new string(letters.ToArray());
        }

        static char Damage(char letter, uint seed)
        {
            switch (char.ToLowerInvariant(letter))
            {
                case 'a': return '4';
                case 'e': return '3';
                case 'i':
                case 'l': return '1';
                case 'o': return '0';
                case 's': return '5';
                case 't': return '7';
                case 'b': return '8';
                case 'g': return '9';
                case 'z': return '2';
                default: return k_Damaged[(int)(seed / 7u % (uint)k_Damaged.Length)];
            }
        }

        // FNV-1a : le même nombre pour le même nom, sur toutes les plateformes (string.GetHashCode peut changer d'un lancement
        // à l'autre).
        static uint Hash(string text)
        {
            var hash = 2166136261u;
            foreach (var letter in text)
                hash = (hash ^ letter) * 16777619u;
            return hash;
        }
    }
}
