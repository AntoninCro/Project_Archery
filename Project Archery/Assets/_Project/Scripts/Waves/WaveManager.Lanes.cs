using System.Collections.Generic;
using Archery.Building;
using Archery.Enemies;
using Archery.UI;
using UnityEngine;

namespace Archery.Waves
{
    /// <summary>La famille d'ennemis d'une voie (les menaces par voie, 3 novembre) ; None : le mélange.</summary>
    public enum LaneThreat
    {
        None,
        Volants,
        Costauds,
        Essaim,
        Tireurs,
    }

    /// <summary>
    /// Les menaces par voie (le lot 5 de la refonte du tower defense, la piste C du 29 octobre ; codé le 3 novembre à la demande du
    /// joueur ; <c>Docs/Refonte_TowerDefense.md</c>, 12.56) : à partir de la vague 2, chaque voie a sa famille d'ennemis, tirée au
    /// hasard parmi celles de la vague (volants, costauds, essaim, tireurs ; une voie de trop : le mélange), et une voie est la plus
    /// chargée. Annoncé pendant la pause qui précède la vague par un message, puis dans le titre du début de la vague. Les bâtiments
    /// se déplacent gratuitement entre les vagues : on s'adapte.
    /// <para><b>En sommeil depuis le 3 novembre</b> (le joueur, après l'essai : les grands mots au-dessus des voies tombaient dans
    /// les murs ; « retirons cette mécanique pour le moment ») : <see cref="s_LaneThreats"/> est faux, rien n'est tiré, chaque ennemi
    /// sort d'un point au hasard, comme avant. Les mots au-dessus des voies sont retirés ; les idées pour une autre annonce (les
    /// fantômes des ennemis sur la voie, des panneaux sur la tour…) sont dans 12.56.</para>
    /// </summary>
    /// <remarks>
    /// Ce qui change (la règle de la complexité : rien de plus à apprendre que lire le mot de la voie) : avant, chaque ennemi sortait
    /// d'un point d'apparition tiré au hasard. Maintenant, il sort 3 fois sur 4 par la voie de sa famille ; sinon (ou si sa famille
    /// n'a pas de voie), par une voie au hasard, la plus chargée deux fois plus souvent que les autres. La vague elle-même (combien,
    /// lesquels) ne change pas : seulement par où ils arrivent. Les boss des vagues sortent toujours au hasard. La famille d'un
    /// ennemi : volant (<see cref="FlyingEnemy"/>), tireur (<see cref="EnemyRangedAttack"/>), sinon costaud s'il coûte au moins 1,8
    /// dans le budget des vagues (l'orc, la tortue, le cactus, le fantassin, la mimique), essaim sinon (les gluants, les
    /// champignons, les étoiles de mer, les rampants).
    /// </remarks>
    public partial class WaveManager
    {
        // Les menaces par voie, en sommeil (3 novembre) : vrai pour les remettre (et leur trouver une annonce qui se voie).
        static readonly bool s_LaneThreats = false;

        // Part des ennemis qui sortent par la voie de leur famille ; poids de la voie la plus chargée pour les autres.
        const float k_FaithfulShare = 0.75f;
        const float k_LoadedWeight = 2f;

        // Un ennemi au sol qui coûte au moins tant dans le budget des vagues est un costaud.
        const float k_SturdyCost = 1.8f;

        // La vague 1 garde ses ennemis de partout : la première, on apprend à jouer.
        const int k_FirstThreatWave = 2;

        static readonly Color k_ThreatTitle = new Color(1f, 0.85f, 0.45f);

        readonly List<Transform> m_ThreatPoints = new List<Transform>();
        readonly List<string> m_ThreatNames = new List<string>();
        readonly List<LaneThreat> m_Threats = new List<LaneThreat>();
        readonly List<LaneThreat> m_Pool = new List<LaneThreat>();
        readonly Dictionary<Enemy, LaneThreat> m_Families = new Dictionary<Enemy, LaneThreat>();
        int m_ThreatWave = -1;
        int m_Loaded = -1;

        /// <summary>La vague dont les menaces sont tirées, si elle en a (sinon -1).</summary>
        public int ThreatWave => m_ThreatPoints.Count > 1 ? m_ThreatWave : -1;

        /// <summary>Les points d'apparition, de gauche à droite vus de la tour (vides sans menaces).</summary>
        public IReadOnlyList<Transform> ThreatPoints => m_ThreatPoints;

        /// <summary>La famille de la voie <paramref name="index"/> (dans l'ordre de <see cref="ThreatPoints"/>).</summary>
        public LaneThreat ThreatOf(int index) => index >= 0 && index < m_Threats.Count ? m_Threats[index] : LaneThreat.None;

        /// <summary>La voie <paramref name="index"/> est la plus chargée.</summary>
        public bool IsLoaded(int index) => index == m_Loaded;

        /// <summary>« volants », « costauds », « essaim », « tireurs », « mélange ».</summary>
        public static string ThreatName(LaneThreat threat) => threat switch
        {
            LaneThreat.Volants => "volants",
            LaneThreat.Costauds => "costauds",
            LaneThreat.Essaim => "essaim",
            LaneThreat.Tireurs => "tireurs",
            _ => "mélange",
        };

        public static Color ThreatColor(LaneThreat threat) => threat switch
        {
            LaneThreat.Volants => new Color(0.55f, 0.82f, 1f),
            LaneThreat.Costauds => new Color(1f, 0.5f, 0.32f),
            LaneThreat.Essaim => new Color(0.6f, 1f, 0.45f),
            LaneThreat.Tireurs => new Color(0.86f, 0.6f, 1f),
            _ => new Color(0.9f, 0.9f, 0.9f),
        };

        // Tire les menaces de cette vague : la famille de chaque voie, et la plus chargée. Rien à la vague 1, ni au boss final.
        void PlanThreats(int wave)
        {
            m_ThreatWave = wave;
            m_ThreatPoints.Clear();
            m_ThreatNames.Clear();
            m_Threats.Clear();
            m_Loaded = -1;
            if (!s_LaneThreats || wave < k_FirstThreatWave || m_Settings == null || m_Spawner == null || m_Settings.IsFinalBossWave(wave))
                return;

            CollectThreatPoints();
            if (m_ThreatPoints.Count < 2)
            {
                m_ThreatPoints.Clear();
                m_ThreatNames.Clear();
                return;
            }

            // Les familles de la vague, chacune une fois ; chaque voie la sienne, au hasard (une voie de trop : le mélange).
            m_Pool.Clear();
            foreach (var enemy in m_Settings.enemies)
            {
                if (enemy == null || enemy.prefab == null || enemy.cost <= 0f || enemy.weight <= 0f || enemy.firstWave > wave)
                    continue;
                var family = FamilyOf(enemy.prefab);
                if (!m_Pool.Contains(family))
                    m_Pool.Add(family);
            }

            Shuffle(m_Pool);
            for (var i = 0; i < m_ThreatPoints.Count; i++)
                m_Threats.Add(i < m_Pool.Count ? m_Pool[i] : LaneThreat.None);
            Shuffle(m_Threats);
            m_Loaded = UnityEngine.Random.Range(0, m_ThreatPoints.Count);
        }

        // Les points d'apparition de gauche à droite (l'ordre des voies vues de la tour), avec leur nom ; dans l'ordre du spawner
        // sans grille.
        void CollectThreatPoints()
        {
            var grid = BuildGrid.Instance;
            if (grid != null && grid.IsReady)
            {
                foreach (var lane in grid.SortedLanes)
                {
                    if (lane.Spawn == null)
                        continue;
                    m_ThreatPoints.Add(lane.Spawn);
                    m_ThreatNames.Add(grid.LaneName(lane, true));
                }
            }

            if (m_ThreatPoints.Count > 0)
                return;
            foreach (var point in m_Spawner.SpawnPoints)
            {
                if (point == null)
                    continue;
                m_ThreatPoints.Add(point);
                m_ThreatNames.Add($"voie {m_ThreatPoints.Count}");
            }
        }

        static void Shuffle<T>(List<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        LaneThreat FamilyOf(Enemy prefab)
        {
            if (m_Families.TryGetValue(prefab, out var family))
                return family;

            var cost = 1f;
            foreach (var enemy in m_Settings.enemies)
            {
                if (enemy != null && enemy.prefab == prefab)
                    cost = enemy.cost;
            }

            family = prefab.GetComponent<FlyingEnemy>() != null ? LaneThreat.Volants
                : prefab.GetComponent<EnemyRangedAttack>() != null ? LaneThreat.Tireurs
                : cost >= k_SturdyCost ? LaneThreat.Costauds
                : LaneThreat.Essaim;
            m_Families[prefab] = family;
            return family;
        }

        // Par où sort cet ennemi : 3 fois sur 4 par la voie de sa famille, sinon par une voie au hasard (la plus chargée deux fois
        // plus souvent) ; null (un point au hasard, comme avant) sans menaces.
        Transform PointFor(Enemy prefab)
        {
            if (prefab == null || m_ThreatWave != WaveNumber || m_ThreatPoints.Count < 2)
                return null;

            var family = FamilyOf(prefab);
            if (family != LaneThreat.None && UnityEngine.Random.value < k_FaithfulShare)
            {
                var matches = 0;
                foreach (var threat in m_Threats)
                {
                    if (threat == family)
                        matches++;
                }

                if (matches > 0)
                {
                    var pick = UnityEngine.Random.Range(0, matches);
                    for (var i = 0; i < m_Threats.Count; i++)
                    {
                        if (m_Threats[i] == family && pick-- == 0)
                            return m_ThreatPoints[i];
                    }
                }
            }

            var total = m_ThreatPoints.Count - 1 + (m_Loaded >= 0 ? k_LoadedWeight : 1f);
            var roll = UnityEngine.Random.value * total;
            for (var i = 0; i < m_ThreatPoints.Count; i++)
            {
                roll -= i == m_Loaded ? k_LoadedWeight : 1f;
                if (roll <= 0f)
                    return m_ThreatPoints[i];
            }

            return m_ThreatPoints[m_ThreatPoints.Count - 1];
        }

        /// <summary>« Gauche : volants · Milieu : costauds · Droite : essaim (la plus chargée) » ; vide sans menaces.</summary>
        public string ThreatLine()
        {
            if (m_ThreatPoints.Count < 2)
                return "";

            var parts = new List<string>();
            for (var i = 0; i < m_ThreatPoints.Count; i++)
            {
                var name = m_ThreatNames[i];
                name = name.Length > 0 ? char.ToUpperInvariant(name[0]) + name.Substring(1) : name;
                parts.Add($"{name} : {ThreatName(m_Threats[i])}" + (i == m_Loaded ? " (la plus chargée)" : ""));
            }

            return string.Join(" · ", parts);
        }

        // Pendant la pause : les menaces de la vague qui vient, en message.
        void AnnounceThreats()
        {
            if (ThreatWave != WaveNumber + 1)
                return;
            HeadMessages.Show($"Vague {WaveNumber + 1} : par où ils viendront\n<size=70%>{ThreatLine()}</size>", k_ThreatTitle, 8f);
        }
    }
}
