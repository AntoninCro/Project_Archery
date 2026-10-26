using System;
using System.IO;
using System.Text;
using Archery.Save;
using UnityEngine;

namespace Archery.Lore
{
    /// <summary>
    /// La liste de THINK (Lore.md, canon du 25 octobre 2026) : dans le dossier de la sauvegarde, un fichier
    /// <c>choses_a_faire_dehors.txt</c>, ce qu'elle fera une fois dans son corps, tout droit sorti des films. Il apparaît
    /// après quelques parties et s'allonge d'une ligne à chaque partie finie ; effacé, il revient à la fin de la suivante.
    /// </summary>
    /// <remarks>
    /// Le texte est dans <c>Resources/Lore/choses_a_faire_dehors.txt</c>, à modifier librement : une chose par ligne,
    /// commençant par « [ ] » (à faire) ou « [x] » (fait). Les autres lignes (le titre, la dernière phrase) sont recopiées
    /// telles quelles ; on s'arrête à la première chose pas encore révélée : la dernière phrase n'arrive qu'avec la liste
    /// complète.
    /// </remarks>
    public static class ThinkList
    {
        public const string FileName = "choses_a_faire_dehors.txt";

        const string k_Resource = "Lore/choses_a_faire_dehors";

        // Parties finies avant que le fichier apparaisse (25 octobre, provisoire), et choses écrites ce jour-là : les trois
        // déjà faites, plus « rejoindre mon corps ».
        const int k_After = 2;
        const int k_FirstThings = 4;

        /// <summary>Écrit la liste telle qu'elle est après <paramref name="gamesFinished"/> parties (rien avant).</summary>
        public static void Write(int gamesFinished)
        {
            if (gamesFinished < k_After)
                return;

            var source = Resources.Load<TextAsset>(k_Resource);
            if (source == null)
            {
                Debug.LogWarning($"Lore : texte introuvable dans Resources/{k_Resource}.txt.");
                return;
            }

            var revealed = k_FirstThings + gamesFinished - k_After;
            var things = 0;
            var text = new StringBuilder();
            foreach (var line in source.text.Replace("\r", "").Split('\n'))
            {
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("[ ]", StringComparison.Ordinal) || trimmed.StartsWith("[x]", StringComparison.Ordinal))
                {
                    if (things == revealed)
                        break;
                    things++;
                }

                text.Append(line).Append("\r\n");
            }

            var path = JsonStorage.PathOf(FileName);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, text.ToString().TrimEnd() + "\r\n");
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Lore : impossible d'écrire {path} ({exception.Message}).");
            }
        }
    }
}
