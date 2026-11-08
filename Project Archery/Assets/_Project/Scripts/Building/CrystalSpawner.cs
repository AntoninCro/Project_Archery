using System.Collections.Generic;
using Archery.Chests;
using Archery.Core;
using Archery.Defense;
using Archery.Enemies;
using Archery.Player;
using Archery.UI;
using Archery.Waves;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace Archery.Building
{
    /// <summary>
    /// Les coffres de la vague (GDD, section 13) : tous ensemble, au début de la vague (au gong), d'un seul côté de la forêt, vu de la
    /// tour face aux ennemis ; l'autre côté à la vague suivante. Ceux qui ne sont pas ouverts disparaissent à la fin de la vague.
    /// Un coffre : un parchemin, une tourelle, un objet (<c>ChestRewards.Roll</c>).
    /// </summary>
    /// <remarks>
    /// Le 2 novembre au soir, après le premier essai du joueur (« un simulateur de randonnée » : 5 à 8 coffres par vague, dont deux
    /// pendant la montée, et l'on ne voyait plus ses tours) : 4 coffres, d'un seul côté, aucun pendant la montée ; les emplacements
    /// du <see cref="ChestSpawner"/> s'ajoutent aux siens (il ne garde que le coffre du boss). Avant : 3 coffres, un de plus toutes
    /// les 2 vagues ; du 27 octobre au 2 novembre, les caisses de l'ingénieur (des lots de plans). Le nom vient des gisements de
    /// cristaux qu'il faisait apparaître avant la refonte du tower defense (GDD, section 9), aux mêmes emplacements.
    /// </remarks>
    [DisallowMultipleComponent]
    public class CrystalSpawner : MonoBehaviour
    {
        [Tooltip("Prefab du coffre (Chest). Vide : celui du Chest Spawner de la scène.")]
        [SerializeField]
        Chest m_CratePrefab;

        [Tooltip("Emplacements possibles (objets vides, dans la jungle et sur les terrasses). Ceux du Chest Spawner s'y ajoutent.")]
        [SerializeField]
        List<Transform> m_Spots = new List<Transform>();

        [Tooltip("Coffres par vague (2 novembre au soir, le joueur ; avant, 3, puis un de plus toutes les 2 vagues, 6 au plus).")]
        [Min(1)]
        [SerializeField]
        int m_Count = 4;

        [Tooltip("Distance minimale (m) entre la tour et un coffre.")]
        [SerializeField]
        float m_MinTowerDistance = 15f;

        [Tooltip("Délai (s) après le début de la vague avant l'apparition des coffres.")]
        [SerializeField]
        float m_SpawnDelay = 3f;

        [Header("Les gardiens (4 novembre)")]
        [Tooltip("Chaque coffre de la vague a ses gardiens : des ennemis de la vague (au sol), là dès l'apparition du coffre, qui " +
                 "vadrouillent autour au hasard en faisant des pauses, et attaquent le joueur qui approche (l'idée du joueur, 4 novembre : " +
                 "les coffres étaient « une randonnée »). Le coffre n'est pas verrouillé : on peut foncer dessus, au prix de ses PV.")]
        [SerializeField]
        bool m_Guards = true;

        [Tooltip("Gardiens par coffre, vague par vague (le dernier vaut pour les suivantes).")]
        [SerializeField]
        int[] m_GuardsPerWave = { 2, 2, 3, 3, 4 };

        [Tooltip("Ils vadrouillent à tant de mètres du coffre (Guard Radius), attaquent le joueur à tant de mètres d'eux (Guard Aggro), et " +
                 "ne le poursuivent pas quand il est à plus de tant de mètres du coffre (Guard Leash).")]
        [SerializeField]
        float m_GuardRadius = 5f;

        [SerializeField]
        float m_GuardAggro = 15f;

        [SerializeField]
        float m_GuardLeash = 30f;

        [Header("Retours")]
        [Tooltip("Optionnel : son de l'apparition des coffres.")]
        [SerializeField]
        AudioClip m_AppearClip;

        [SerializeField]
        Color m_MessageColor = new Color(0.72f, 0.55f, 1f);

        readonly List<Chest> m_Crates = new List<Chest>();
        readonly List<WaveEnemy> m_GuardChoices = new List<WaveEnemy>();
        readonly List<Transform> m_AllSpots = new List<Transform>();
        readonly List<Transform> m_FreeSpots = new List<Transform>();
        bool m_HasAllSpots;
        WaveManager m_Waves;
        float m_SpawnTimer = -1f;

        // Le côté des coffres de la dernière vague : -1 à gauche, 1 à droite, 0 pas encore de coffres.
        int m_Side;

        public static CrystalSpawner Instance { get; private set; }

        /// <summary>Un coffre vient d'apparaître avec ses gardiens (combien, où) : le journal de partie le note.</summary>
        public static event System.Action<int, Vector3> GuardsCameOut;

        /// <summary>
        /// Tous les emplacements de coffres, les siens et ceux du Chest Spawner (la grille de construction n'y pose pas
        /// d'emplacement).
        /// </summary>
        public IReadOnlyList<Transform> Spots
        {
            get
            {
                if (m_HasAllSpots)
                    return m_AllSpots;

                m_HasAllSpots = true;
                m_AllSpots.Clear();
                foreach (var spot in m_Spots)
                {
                    if (spot != null)
                        m_AllSpots.Add(spot);
                }

                var chests = FindAnyObjectByType<ChestSpawner>();
                if (chests != null)
                {
                    foreach (var spot in chests.SpawnPoints)
                    {
                        if (spot != null && !m_AllSpots.Contains(spot))
                            m_AllSpots.Add(spot);
                    }
                }

                return m_AllSpots;
            }
        }

        /// <summary>Coffres encore à ouvrir sur la carte.</summary>
        public int Remaining
        {
            get
            {
                var count = 0;
                foreach (var crate in m_Crates)
                {
                    if (crate != null && !crate.IsOpened)
                        count++;
                }

                return count;
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs Crystal Spawner dans la scène.", this);
            Instance = this;
        }

        void Start()
        {
            if (m_CratePrefab == null)
            {
                var chests = FindAnyObjectByType<ChestSpawner>();
                m_CratePrefab = chests != null ? chests.ChestPrefab : null;
            }

            if (m_CratePrefab == null)
                Debug.LogWarning("CrystalSpawner : aucun prefab de coffre (ni dans le Chest Spawner).", this);

            m_Waves = WaveManager.Instance;
            if (m_Waves == null)
            {
                Debug.LogWarning("CrystalSpawner : aucun Wave Manager, les coffres n'apparaîtront jamais.", this);
                return;
            }

            m_Waves.WaveStarted += OnWaveStarted;
            m_Waves.WaveEnded += OnWaveEnded;
            m_Waves.GameOver += ClearAll;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            if (m_Waves == null)
                return;

            m_Waves.WaveStarted -= OnWaveStarted;
            m_Waves.WaveEnded -= OnWaveEnded;
            m_Waves.GameOver -= ClearAll;
        }

        void OnWaveStarted(int wave)
        {
            // Pas de coffre pendant la vague du boss final : la carte est vide.
            if (m_Waves.IsFinalBossWave)
                return;

            m_SpawnTimer = m_SpawnDelay;
        }

        void OnWaveEnded(int wave) => ClearAll();

        void ClearAll()
        {
            m_SpawnTimer = -1f;
            foreach (var crate in m_Crates)
            {
                if (crate != null)
                    crate.Vanish();
            }

            m_Crates.Clear();
        }

        void Update()
        {
            UpdateDebugKey();
            if (m_SpawnTimer < 0f || m_Waves == null || m_Waves.Phase != WavePhase.Wave)
                return;

            m_SpawnTimer -= Time.deltaTime;
            if (m_SpawnTimer > 0f)
                return;

            m_SpawnTimer = -1f;
            SpawnWave();
        }

        // Les coffres de la vague, sur le côté opposé à celui de la vague d'avant (le premier au hasard) ; sur l'autre côté s'il
        // n'y a plus de place libre de celui-là.
        void SpawnWave()
        {
            if (m_CratePrefab == null)
                return;

            var tower = TowerPosition();
            var axis = EnemyAxis(tower);
            m_Side = m_Side == 0 ? (Random.value < 0.5f ? -1 : 1) : -m_Side;
            CollectFreeSpots(tower, axis, m_Side);
            if (m_FreeSpots.Count == 0)
            {
                m_Side = -m_Side;
                CollectFreeSpots(tower, axis, m_Side);
            }

            var spawned = 0;
            while (spawned < m_Count && m_FreeSpots.Count > 0)
            {
                var index = Random.Range(0, m_FreeSpots.Count);
                var spot = m_FreeSpots[index];
                m_FreeSpots.RemoveAt(index);
                var crate = SpawnAt(spot.position, tower);
                spawned++;
                if (m_Guards && crate != null)
                    SpawnGuards(crate.transform.position);
            }

            if (spawned == 0)
            {
                Debug.LogWarning("CrystalSpawner : aucun emplacement à plus de " + m_MinTowerDistance + " m de la tour.", this);
                return;
            }

            if (m_AppearClip != null)
                Sfx.Play(m_AppearClip, PlayerPosition() + Vector3.up, 0.8f, 1f, 0f);
            var side = m_Side < 0 ? "gauche" : "droit";
            var title = spawned == 1 ? $"Un coffre est apparu, côté {side} !" : $"{spawned} coffres sont apparus, côté {side} !";
            Announce(title + "\n<size=70%>Vu de la tour, face aux ennemis ; un parchemin, une tourelle, un objet</size>");
        }

        // ---------------------------------------------------------------- les gardiens (4 novembre)

        // Ses gardiens apparaissent avec le coffre, au hasard autour de lui (le joueur, 4 novembre : « ils sont là dès le début et
        // vadrouillent un peu au hasard en prenant des pauses de temps en temps » ; d'abord, ils sortaient des fourrés à son approche).
        void SpawnGuards(Vector3 chest)
        {
            var wave = Mathf.Max(1, m_Waves.WaveNumber);
            var count = m_GuardsPerWave != null && m_GuardsPerWave.Length > 0
                ? m_GuardsPerWave[Mathf.Clamp(wave - 1, 0, m_GuardsPerWave.Length - 1)]
                : 2;

            var spawned = 0;
            for (var i = 0; i < count; i++)
            {
                var prefab = PickGuard(wave);
                if (prefab == null)
                    break;
                var offset = Random.insideUnitCircle.normalized * Random.Range(2f, Mathf.Max(2.5f, m_GuardRadius));
                var point = chest + new Vector3(offset.x, 0f, offset.y);
                if (!NavMesh.SamplePosition(point, out var hit, 3f, NavMesh.AllAreas) &&
                    !NavMesh.SamplePosition(chest, out hit, 4f, NavMesh.AllAreas))
                    continue;

                var enemy = Instantiate(prefab, hit.position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                if (!enemy.Guard(chest, m_GuardRadius, m_GuardAggro, m_GuardLeash, false))
                {
                    Destroy(enemy.gameObject);
                    continue;
                }

                spawned++;
            }

            if (spawned > 0)
                GuardsCameOut?.Invoke(spawned, chest);
        }

        // Un ennemi de cette vague, au sol (pas un volant, ni un boss), tiré au sort selon son poids, comme ceux des voies.
        Enemy PickGuard(int wave)
        {
            var settings = m_Waves.Settings;
            if (settings == null)
                return null;

            m_GuardChoices.Clear();
            var total = 0f;
            foreach (var entry in settings.enemies)
            {
                if (entry == null || entry.prefab == null || entry.firstWave > wave || entry.weight <= 0f ||
                    entry.prefab.GetComponent<EnemyBehaviour>() != null || entry.prefab.GetComponent<Boss>() != null)
                    continue;
                m_GuardChoices.Add(entry);
                total += entry.weight;
            }

            var roll = Random.value * total;
            foreach (var entry in m_GuardChoices)
            {
                roll -= entry.weight;
                if (roll <= 0f)
                    return entry.prefab;
            }

            return m_GuardChoices.Count > 0 ? m_GuardChoices[m_GuardChoices.Count - 1].prefab : null;
        }

        // Les emplacements libres de ce côté, assez loin de la tour.
        void CollectFreeSpots(Vector3 tower, Vector3 axis, int side)
        {
            m_FreeSpots.Clear();
            foreach (var spot in Spots)
            {
                var flat = spot.position - tower;
                flat.y = 0f;
                if (flat.magnitude >= m_MinTowerDistance && SideOf(flat, axis) == side && !IsOccupied(spot.position))
                    m_FreeSpots.Add(spot);
            }
        }

        // Le côté d'un point, vu de la tour face aux ennemis : 1 à droite, -1 à gauche.
        static int SideOf(Vector3 fromTower, Vector3 axis) => Vector3.Cross(axis, fromTower).y >= 0f ? 1 : -1;

        // De la tour vers le milieu des points d'apparition des ennemis (là d'où ils viennent).
        static Vector3 EnemyAxis(Vector3 tower)
        {
            var spawner = FindAnyObjectByType<EnemySpawner>();
            var sum = Vector3.zero;
            var count = 0;
            if (spawner != null)
            {
                foreach (var point in spawner.SpawnPoints)
                {
                    if (point == null)
                        continue;
                    sum += point.position;
                    count++;
                }
            }

            var axis = count > 0 ? sum / count - tower : Vector3.forward;
            axis.y = 0f;
            return axis.sqrMagnitude > 0.01f ? axis.normalized : Vector3.forward;
        }

        // Un coffre pas encore ouvert occupe déjà cet emplacement.
        bool IsOccupied(Vector3 position)
        {
            foreach (var crate in m_Crates)
            {
                if (crate != null && !crate.IsOpened && (crate.transform.position - position).sqrMagnitude < 1f)
                    return true;
            }

            return false;
        }

        // Le coffre est tourné vers la tour : on arrive par l'avant.
        Chest SpawnAt(Vector3 position, Vector3 lookAt)
        {
            var toTarget = lookAt - position;
            toTarget.y = 0f;
            var rotation = toTarget.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toTarget) : Quaternion.identity;

            var crate = Instantiate(m_CratePrefab, position, rotation);
            crate.name = "Coffre";
            m_Crates.Add(crate);
            return crate;
        }

        // Raccourci de test au clavier (éditeur) : V pose un coffre 3 m devant le joueur (une caisse de l'ingénieur du 27 octobre
        // au 2 novembre, un gisement de cristaux avant).
        void UpdateDebugKey()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var keyboard = Keyboard.current;
            var rig = PlayerRig.Instance;
            if (keyboard == null || !keyboard.vKey.wasPressedThisFrame || rig == null || m_CratePrefab == null)
                return;

            var position = rig.BodyPosition + rig.HeadYaw * Vector3.forward * 3f;
            if (Physics.Raycast(position + Vector3.up * 2f, Vector3.down, out var hit, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                position = hit.point;
            SpawnAt(position, rig.BodyPosition);
#endif
        }

        void Announce(string text)
        {
            HeadMessages.Show(text, m_MessageColor, 5f);
        }

        Vector3 PlayerPosition()
        {
            var rig = PlayerRig.Instance;
            return rig != null ? rig.BodyPosition : transform.position;
        }

        Vector3 TowerPosition()
        {
            var tower = Tower.Instance;
            return tower != null ? tower.transform.position : transform.position;
        }
    }
}
