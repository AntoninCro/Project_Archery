using System.Collections.Generic;
using Archery.Chests;
using Archery.Core;
using Archery.Defense;
using Archery.Player;
using Archery.UI;
using Archery.Waves;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace Archery.Building
{
    /// <summary>
    /// Fait apparaître les caisses de l'ingénieur de chaque vague (refonte du tower defense du 27 octobre,
    /// <c>Docs/Refonte_TowerDefense.md</c>, partie 2) sur des emplacements choisis, loin de la tour, au début de l'accalmie :
    /// 3 caisses, une de plus toutes les 2 vagues. Une caisse s'ouvre comme un coffre et propose des lots de plans de
    /// construction. Celles qui ne sont pas ouvertes disparaissent à la fin de la vague.
    /// </summary>
    /// <remarks>
    /// Le nom vient des gisements de cristaux qu'il faisait apparaître avant la refonte (GDD, section 9), aux mêmes
    /// emplacements et en même nombre ; les matériaux sont retirés. Une caisse est le coffre de la scène (celui du
    /// <see cref="ChestSpawner"/>) transformé par <see cref="Chest.MakeEngineerCrate"/>, en attendant son propre modèle.
    /// </remarks>
    [DisallowMultipleComponent]
    public class CrystalSpawner : MonoBehaviour
    {
        [Tooltip("Prefab de la caisse : un coffre (Chest). Vide : celui du Chest Spawner de la scène.")]
        [SerializeField]
        Chest m_CratePrefab;

        [Tooltip("Emplacements possibles (objets vides, dans la jungle et sur les terrasses).")]
        [SerializeField]
        List<Transform> m_Spots = new List<Transform>();

        [Header("Nombre")]
        [Tooltip("Caisses par série, à la vague 1.")]
        [SerializeField]
        int m_BaseCount = 3;

        [Tooltip("Une caisse de plus par série toutes les N vagues.")]
        [Min(1)]
        [SerializeField]
        int m_WavesPerExtra = 2;

        [Tooltip("Caisses par série, au plus.")]
        [SerializeField]
        int m_MaxCount = 6;

        [Tooltip("Une deuxième série de caisses au début de la montée (ou à l'arrivée du boss s'il est appelé plus tôt).")]
        [SerializeField]
        bool m_SecondBatch = true;

        [Tooltip("Distance minimale (m) entre la tour et une caisse.")]
        [SerializeField]
        float m_MinTowerDistance = 15f;

        [Tooltip("Délai (s) après le début de la vague avant l'apparition de la première série.")]
        [SerializeField]
        float m_SpawnDelay = 3f;

        [Header("Retours")]
        [Tooltip("Couleur du rayon et de la caisse : le violet des anciens cristaux, la couleur de la construction.")]
        [SerializeField]
        Color m_CrateColor = new Color(0.62f, 0.4f, 1f);

        [Tooltip("Optionnel : son de l'apparition des caisses.")]
        [SerializeField]
        AudioClip m_AppearClip;

        [SerializeField]
        Color m_MessageColor = new Color(0.72f, 0.55f, 1f);

        readonly List<Chest> m_Crates = new List<Chest>();
        readonly List<Transform> m_FreeSpots = new List<Transform>();
        WaveManager m_Waves;
        float m_SpawnTimer = -1f;
        bool m_SecondBatchPending;

        public static CrystalSpawner Instance { get; private set; }

        /// <summary>Les emplacements des caisses (la grille de construction n'y pose pas d'emplacement).</summary>
        public IReadOnlyList<Transform> Spots => m_Spots;

        /// <summary>Caisses encore à ouvrir sur la carte.</summary>
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
                Debug.LogWarning("CrystalSpawner : aucun prefab de caisse (ni de coffre dans le Chest Spawner).", this);

            m_Waves = WaveManager.Instance;
            if (m_Waves == null)
            {
                Debug.LogWarning("CrystalSpawner : aucun Wave Manager, les caisses n'apparaîtront jamais.", this);
                return;
            }

            m_Waves.WaveStarted += OnWaveStarted;
            m_Waves.WaveEnded += OnWaveEnded;
            m_Waves.StageChanged += OnStageChanged;
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
            m_Waves.StageChanged -= OnStageChanged;
            m_Waves.GameOver -= ClearAll;
        }

        void OnWaveStarted(int wave)
        {
            // Pas de caisse pendant la vague du boss final : la carte est vide.
            if (m_Waves.IsFinalBossWave)
                return;

            m_SpawnTimer = m_SpawnDelay;
            m_SecondBatchPending = m_SecondBatch;
        }

        // Deuxième série : au début de la montée, ou à l'arrivée du boss s'il a été appelé pendant l'accalmie.
        void OnStageChanged(WaveStage stage)
        {
            if (!m_SecondBatchPending || stage == WaveStage.Calm)
                return;

            m_SecondBatchPending = false;
            SpawnWave(m_Waves.WaveNumber);
        }

        void OnWaveEnded(int wave) => ClearAll();

        void ClearAll()
        {
            m_SpawnTimer = -1f;
            m_SecondBatchPending = false;
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
            SpawnWave(m_Waves.WaveNumber);
        }

        void SpawnWave(int wave)
        {
            if (m_CratePrefab == null)
                return;

            var count = Mathf.Min(m_MaxCount, m_BaseCount + (wave - 1) / m_WavesPerExtra);
            var tower = TowerPosition();
            m_FreeSpots.Clear();
            foreach (var spot in m_Spots)
            {
                var flat = spot != null ? spot.position - tower : Vector3.zero;
                flat.y = 0f;
                if (spot != null && flat.magnitude >= m_MinTowerDistance && !IsOccupied(spot.position))
                    m_FreeSpots.Add(spot);
            }

            var spawned = 0;
            while (spawned < count && m_FreeSpots.Count > 0)
            {
                var index = Random.Range(0, m_FreeSpots.Count);
                var spot = m_FreeSpots[index];
                m_FreeSpots.RemoveAt(index);
                SpawnAt(spot.position, tower);
                spawned++;
            }

            if (spawned == 0)
            {
                Debug.LogWarning("CrystalSpawner : aucun emplacement à plus de " + m_MinTowerDistance + " m de la tour.", this);
                return;
            }

            if (m_AppearClip != null)
                Sfx.Play(m_AppearClip, PlayerPosition() + Vector3.up, 0.8f, 1f, 0f);
            var title = spawned == 1 ? "Une caisse de l'ingénieur est apparue !" : $"{spawned} caisses de l'ingénieur sont apparues !";
            Announce(title + "\n<size=70%>Des plans de construction à prendre</size>");
        }

        // Une caisse de la série précédente, pas encore ouverte, occupe déjà cet emplacement.
        bool IsOccupied(Vector3 position)
        {
            foreach (var crate in m_Crates)
            {
                if (crate != null && !crate.IsOpened && (crate.transform.position - position).sqrMagnitude < 1f)
                    return true;
            }

            return false;
        }

        // La caisse est tournée vers la tour, comme les coffres : on arrive par l'avant.
        Chest SpawnAt(Vector3 position, Vector3 lookAt)
        {
            var toTarget = lookAt - position;
            toTarget.y = 0f;
            var rotation = toTarget.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toTarget) : Quaternion.identity;

            var crate = Instantiate(m_CratePrefab, position, rotation);
            crate.name = "Caisse de l'ingénieur";
            crate.MakeEngineerCrate(m_CrateColor);
            m_Crates.Add(crate);
            return crate;
        }

        // Raccourci de test au clavier (éditeur) : V pose une caisse de l'ingénieur 3 m devant le joueur (un gisement de
        // cristaux avant le 27 octobre).
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
            HeadMessages.Show(text, m_MessageColor, 4f);
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
