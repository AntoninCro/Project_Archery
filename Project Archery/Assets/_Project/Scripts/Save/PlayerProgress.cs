using System;
using UnityEngine;

namespace Archery.Save
{
    /// <summary>Contenu du fichier progress.json.</summary>
    [Serializable]
    public class ProgressData
    {
        /// <summary>Expérience totale gagnée, partie après partie.</summary>
        public int xp;

        /// <summary>Nom de l'asset de l'arc choisi au menu (arcs comme classes).</summary>
        public string selectedBow = "";

        public int gamesPlayed;
    }

    /// <summary>
    /// Progression gardée d'une partie à l'autre (GDD, section 23) : l'expérience qui débloque les arcs,
    /// et l'arc choisi. Enregistrée dans <c>progress.json</c>, à côté du classement.
    /// </summary>
    public static class PlayerProgress
    {
        public const string FileName = "progress.json";

        static ProgressData s_Data;

        /// <summary>Progression actuelle (lue sur le disque à la première lecture).</summary>
        public static ProgressData Data
        {
            get
            {
                if (s_Data == null && (!JsonStorage.TryLoad(FileName, out s_Data) || s_Data == null))
                    s_Data = new ProgressData();
                return s_Data;
            }
        }

        public static void Save() => JsonStorage.Save(FileName, Data);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_Data = null;
    }
}
