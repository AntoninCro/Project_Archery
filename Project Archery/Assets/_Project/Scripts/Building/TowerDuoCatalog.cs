using System;
using System.Collections.Generic;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Les duos du tower defense (lot 7 de la refonte, 31 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, parties 12.14 et 12.15) :
    /// deux tours bâties, chacune au moins au niveau 3, qu'on fusionne en déplaçant l'une sur l'autre entre les vagues. Elles
    /// deviennent une tour nouvelle (<see cref="TowerDuo"/>), à l'attaque à elle.
    /// </summary>
    /// <remarks>
    /// Un seul catalogue, chargé de <c>Resources/TowerDuos</c> : rien à poser dans la scène. <c>Editor/TowerDefenseLookBaker</c> y
    /// ajoute chaque duo de <c>Editor/TowerDefenseLook.json</c> (partie « duos »), avec sa définition (<c>Data/Duos</c>), une fois :
    /// les textes et les couleurs changés à la main restent.
    /// </remarks>
    [CreateAssetMenu(menuName = "Archery/Duos de tours", fileName = "TowerDuos")]
    public class TowerDuoCatalog : ScriptableObject
    {
        const string k_Resource = "TowerDuos";

        /// <summary>Un duo : les deux tours qui le font, et la tour qu'il devient.</summary>
        [Serializable]
        public class Entry
        {
            [Tooltip("Le nom annoncé à la fusion (« Duo : Boulet fou ! »).")]
            public string displayName = "Duo";

            [Tooltip("Les deux tours, dans n'importe quel ordre.")]
            public StructureKind first;

            public StructureKind second;

            [Tooltip("La tour qu'il devient (Data/Duos) : son prefab (une TowerDuo), ses 3 niveaux, leurs plans et leurs textes.")]
            public StructureDefinition definition;

            public Color color = new Color(1f, 0.78f, 0.3f);

            [Tooltip("Ce qu'il fait, annoncé à la fusion.")]
            [TextArea(2, 5)]
            public string description;

            public bool Joins(StructureKind a, StructureKind b) => (first == a && second == b) || (first == b && second == a);
        }

        [SerializeField]
        List<Entry> m_Duos = new List<Entry>();

        static TowerDuoCatalog s_Instance;
        static bool s_Loaded;

        public IReadOnlyList<Entry> Duos => m_Duos;

        /// <summary>Le catalogue du projet (<c>Resources/TowerDuos</c>), ou null s'il n'existe pas encore.</summary>
        public static TowerDuoCatalog Instance
        {
            get
            {
                if (!s_Loaded)
                {
                    s_Instance = Resources.Load<TowerDuoCatalog>(k_Resource);
                    s_Loaded = true;
                }

                return s_Instance;
            }
        }

        /// <summary>Le duo de ces deux sortes de tours (dans n'importe quel ordre), s'il a sa tour ; null sinon.</summary>
        public static Entry Find(StructureKind a, StructureKind b)
        {
            var catalog = Instance;
            if (catalog == null)
                return null;

            foreach (var entry in catalog.m_Duos)
            {
                if (entry != null && entry.definition != null && entry.definition.prefab is TowerDuo && entry.Joins(a, b))
                    return entry;
            }

            return null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Instance = null;
            s_Loaded = false;
        }
    }
}
