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
    /// les textes et les couleurs changés à la main restent. Les trios (1er novembre, partie « trios » du fichier) ont leur liste à
    /// eux : un duo plus une tour (<see cref="TowerTrio"/>) ; les ultimes aussi (2 novembre, partie « ultimes ») : le niveau 4 d'un
    /// trio de leur famille (<see cref="TowerUltimate"/>).
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

            [Tooltip("Un trio (1er novembre) : le duo qui le fait (sa définition, Data/Duos), avec la tour « First ». Vide pour un duo.")]
            public StructureDefinition duo;

            [Tooltip("Une ultime (2 novembre) : les trios de sa famille (leurs définitions, Data/Duos) ; l'un d'eux au niveau 3 devient " +
                     "elle, son niveau 4. Vide pour un duo ou un trio.")]
            public List<StructureDefinition> trios = new List<StructureDefinition>();

            /// <summary>Un trio (un duo plus une tour), pas un duo.</summary>
            public bool IsTrio => duo != null;

            /// <summary>Une ultime (le niveau 4 d'un trio), ni un duo ni un trio.</summary>
            public bool IsUltimate => trios != null && trios.Count > 0;

            /// <summary>« Duo », « Trio » ou « Ultime », pour les messages.</summary>
            public string Label => IsUltimate ? "Ultime" : IsTrio ? "Trio" : "Duo";

            public bool Joins(StructureKind a, StructureKind b) => (first == a && second == b) || (first == b && second == a);
        }

        [SerializeField]
        List<Entry> m_Duos = new List<Entry>();

        [Tooltip("Les trios (1er novembre ; Docs/Refonte_TowerDefense.md, 12.19) : un duo plus une tour, au moins au niveau 3.")]
        [SerializeField]
        List<Entry> m_Trios = new List<Entry>();

        [Tooltip("Les ultimes (2 novembre ; Docs/Refonte_TowerDefense.md, 12.44) : une par famille de trios, le niveau 4 d'un trio au " +
                 "niveau 3 ; une seule par partie.")]
        [SerializeField]
        List<Entry> m_Ultimates = new List<Entry>();

        static TowerDuoCatalog s_Instance;
        static bool s_Loaded;

        public IReadOnlyList<Entry> Duos => m_Duos;

        public IReadOnlyList<Entry> Trios => m_Trios;

        public IReadOnlyList<Entry> Ultimates => m_Ultimates;

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

        /// <summary>Le trio de ce duo (sa définition) et de cette sorte de tour, s'il a sa tour ; null sinon.</summary>
        public static Entry FindTrio(StructureDefinition duo, StructureKind tower)
        {
            var catalog = Instance;
            if (catalog == null || duo == null)
                return null;

            foreach (var entry in catalog.m_Trios)
            {
                if (entry != null && entry.duo == duo && entry.first == tower && entry.definition != null && entry.definition.prefab is TowerTrio)
                    return entry;
            }

            return null;
        }

        /// <summary>L'ultime de ce trio (sa définition) : son niveau 4, s'il a sa tour ; null sinon.</summary>
        public static Entry FindUltimate(StructureDefinition trio)
        {
            var catalog = Instance;
            if (catalog == null || trio == null)
                return null;

            foreach (var entry in catalog.m_Ultimates)
            {
                if (entry != null && entry.IsUltimate && entry.trios.Contains(trio) && entry.definition != null &&
                    entry.definition.prefab is TowerUltimate)
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
