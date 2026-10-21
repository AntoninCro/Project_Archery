using System.Collections.Generic;
using UnityEngine;

namespace Archery.Portal
{
    /// <summary>
    /// Une dimension secrète, vue de ses panneaux d'erreur (<see cref="ErrorPanels"/>) : la dimension des étoiles
    /// (<see cref="StarDimension"/>), le toit cyberpunk (<c>Archery.Cyber.CyberDimension</c>).
    /// </summary>
    public interface IGlitchDimension
    {
        /// <summary>La tête du joueur (sa caméra).</summary>
        Transform Head { get; }

        /// <summary>L'épreuve est en cours (découverte ou compte à rebours).</summary>
        bool Running { get; }

        /// <summary>Secondes depuis l'arrivée du joueur (pendant la découverte : la position dans la musique).</summary>
        float Elapsed { get; }

        /// <summary>Secondes à tenir encore.</summary>
        float TimeLeft { get; }

        /// <summary>La hauteur du sol, sous laquelle aucun panneau ne s'ouvre.</summary>
        float FloorY { get; }

        AudioClip RandomGlitchClip();

        /// <summary>Ajoute à la liste les points qu'un panneau ne doit pas cacher (les monstres, les tireurs).</summary>
        void GetThreats(List<Vector3> points);
    }
}
