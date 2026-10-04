using System;
using System.IO;
using UnityEngine;

namespace Archery.Save
{
    /// <summary>
    /// Lit et écrit des fichiers JSON dans <see cref="Application.persistentDataPath"/> (GDD, section 15).
    /// Sous Windows : <c>%USERPROFILE%\AppData\LocalLow\&lt;entreprise&gt;\&lt;produit&gt;</c>.
    /// </summary>
    public static class JsonStorage
    {
        public static string PathOf(string fileName) => Path.Combine(Application.persistentDataPath, fileName);

        /// <summary>Lit un fichier. Renvoie faux s'il n'existe pas encore ou s'il est illisible.</summary>
        public static bool TryLoad<T>(string fileName, out T data) where T : class
        {
            data = null;
            var path = PathOf(fileName);
            if (!File.Exists(path))
                return false;

            try
            {
                data = JsonUtility.FromJson<T>(File.ReadAllText(path));
                return data != null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Sauvegarde : impossible de lire {path} ({exception.Message}).");
                return false;
            }
        }

        /// <summary>
        /// Écrit un fichier. Le texte passe d'abord par un fichier temporaire : un arrêt brutal ne laisse jamais
        /// un fichier à moitié écrit.
        /// </summary>
        public static bool Save<T>(string fileName, T data)
        {
            var path = PathOf(fileName);
            var temporary = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
                if (File.Exists(path))
                    File.Delete(path);
                File.Move(temporary, path);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Sauvegarde : impossible d'écrire {path} ({exception.Message}).");
                return false;
            }
        }
    }
}
