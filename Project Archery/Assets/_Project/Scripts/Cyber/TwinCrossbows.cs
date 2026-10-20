using System;
using Archery.BowClasses;
using Archery.Bows;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Archery.Cyber
{
    /// <summary>
    /// Les arbalètes de l'épreuve du toit : des <see cref="Akimbow"/> (une arbalète dans chaque main, un anneau et une aide
    /// à la visée chacune, le style néon et ses bugs), mais chaque flèche fait <see cref="k_Damage"/> (un tireur tombe en une
    /// flèche), ne rapporte pas de points et ne compte pas comme un tir de l'arc.
    /// </summary>
    /// <remarks>
    /// Ce sont les Akimbow de la liste des armes, s'il y en a (mêmes modèles, matériaux, néons, anneau, vitesse) ; sinon,
    /// le modèle, le matériau, le son et la pointe donnés par le toit.
    /// </remarks>
    public static class TwinCrossbows
    {
        const float k_Damage = 100f;

        static Akimbow s_Akimbow;
        static BowDefinition s_Definition;

        /// <summary>Une arbalète vient de tirer (départ et direction de la flèche).</summary>
        public static event Action<Vector3, Vector3> Fired;

        /// <summary>Met une arbalète dans chaque main.</summary>
        /// <param name="model">Le modèle d'une arbalète : la poignée à l'origine, le canon vers +Z.</param>
        /// <param name="tip">La pointe, d'où partent les flèches, dans le repère du modèle.</param>
        public static void Equip(GameObject model, Material material, AudioClip fireClip, Vector3 tip)
        {
            Unequip();
            s_Definition = TrialDefinition(model, material, fireClip, tip);
            s_Akimbow = Akimbow.CreateTrial(Object.FindAnyObjectByType<Bow>(), s_Definition, k_Damage);
            s_Akimbow.Fired += OnFired;
        }

        /// <summary>Retire les arbalètes des mains.</summary>
        public static void Unequip()
        {
            if (s_Akimbow != null)
            {
                s_Akimbow.Fired -= OnFired;
                Object.Destroy(s_Akimbow.gameObject);
            }

            if (s_Definition != null)
                Object.Destroy(s_Definition);
            s_Akimbow = null;
            s_Definition = null;
        }

        static void OnFired(Vector3 origin, Vector3 direction) => Fired?.Invoke(origin, direction);

        // Une copie des Akimbow de la liste des armes (ou des réglages par défaut), avec le modèle du toit.
        static BowDefinition TrialDefinition(GameObject model, Material material, AudioClip fireClip, Vector3 tip)
        {
            BowDefinition source = null;
            var loadout = BowLoadout.Instance;
            if (loadout != null)
            {
                foreach (var entry in loadout.Entries)
                {
                    if (entry != null && entry.bow != null && entry.bow.akimbo)
                        source = entry.bow;
                }
            }

            // Les Akimbow de la liste : exactement les mêmes armes (modèle, matériaux, néons) que dans le jeu.
            if (source != null)
            {
                var copy = Object.Instantiate(source);
                copy.name = "Akimbow (épreuve du toit)";
                return copy;
            }

            var definition = ScriptableObject.CreateInstance<BowDefinition>();
            definition.name = "Akimbow (épreuve du toit)";
            definition.akimbo = true;
            definition.arrowSpeed = 42f;
            definition.ringDuration = 0.6f;
            definition.akimboScale = 0.7f;
            definition.akimboModel = model;
            definition.akimboMaterial = material;
            definition.akimboFireClip = fireClip;
            definition.akimboTip = tip;
            return definition;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Akimbow = null;
            s_Definition = null;
            Fired = null;
        }
    }
}
