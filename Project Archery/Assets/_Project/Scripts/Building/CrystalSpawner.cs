using System.Collections.Generic;
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
    /// Fait apparaître les gisements de cristaux de chaque vague (GDD, section 9) sur des emplacements choisis,
    /// loin de la tour : 3 par vague, un de plus toutes les 3 vagues. Ce qui n'est pas récolté disparaît à la fin
    /// de la vague.
    /// </summary>
    [DisallowMultipleComponent]
    public class CrystalSpawner : MonoBehaviour
    {
        [SerializeField]
        CrystalDeposit m_DepositPrefab;

        [Tooltip("Emplacements possibles (objets vides, dans la jungle et sur les terrasses).")]
        [SerializeField]
        List<Transform> m_Spots = new List<Transform>();

        [Header("Nombre")]
        [Tooltip("Gisements par vague, au départ.")]
        [SerializeField]
        int m_BaseCount = 3;

        [Tooltip("Un gisement de plus toutes les N vagues.")]
        [Min(1)]
        [SerializeField]
        int m_WavesPerExtra = 3;

        [SerializeField]
        int m_MaxCount = 6;

        [Tooltip("Distance minimale (m) entre la tour et un gisement.")]
        [SerializeField]
        float m_MinTowerDistance = 15f;

        [Tooltip("Délai (s) après le début de la vague avant l'apparition des gisements.")]
        [SerializeField]
        float m_SpawnDelay = 3f;

        [Header("Matériaux")]
        [Tooltip("Matériaux d'un gisement à la vague 1.")]
        [SerializeField]
        int m_BaseAmount = 20;

        [Tooltip("Matériaux en plus par vague.")]
        [SerializeField]
        int m_AmountPerWave = 2;

        [Header("Retours")]
        [Tooltip("Optionnel : son de l'apparition des gisements.")]
        [SerializeField]
        AudioClip m_AppearClip;

        [SerializeField]
        Color m_MessageColor = new Color(0.72f, 0.55f, 1f);

        readonly List<CrystalDeposit> m_Deposits = new List<CrystalDeposit>();
        readonly List<Transform> m_FreeSpots = new List<Transform>();
        WaveManager m_Waves;
        float m_SpawnTimer = -1f;

        public static CrystalSpawner Instance { get; private set; }

        /// <summary>Gisements encore à récolter sur la carte.</summary>
        public int Remaining
        {
            get
            {
                var count = 0;
                foreach (var deposit in m_Deposits)
                {
                    if (deposit != null && !deposit.IsHarvested)
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
            m_Waves = WaveManager.Instance;
            if (m_Waves == null)
            {
                Debug.LogWarning("CrystalSpawner : aucun Wave Manager, les cristaux n'apparaîtront jamais.", this);
                return;
            }

            if (m_DepositPrefab == null)
                Debug.LogWarning("CrystalSpawner : aucun prefab de gisement.", this);

            m_Waves.WaveStarted += OnWaveStarted;
            m_Waves.WaveEnded += OnWaveEnded;
            m_Waves.GameOver += ClearAll;
            CrystalDeposit.Harvested += OnHarvested;
        }

        void OnDestroy()
        {
            CrystalDeposit.Harvested -= OnHarvested;
            if (Instance == this)
                Instance = null;
            if (m_Waves == null)
                return;

            m_Waves.WaveStarted -= OnWaveStarted;
            m_Waves.WaveEnded -= OnWaveEnded;
            m_Waves.GameOver -= ClearAll;
        }

        void OnWaveStarted(int wave) => m_SpawnTimer = m_SpawnDelay;

        void OnWaveEnded(int wave) => ClearAll();

        void ClearAll()
        {
            m_SpawnTimer = -1f;
            foreach (var deposit in m_Deposits)
            {
                if (deposit != null && !deposit.IsHarvested)
                    deposit.Vanish();
            }

            m_Deposits.Clear();
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
            if (m_DepositPrefab == null)
                return;

            var count = Mathf.Min(m_MaxCount, m_BaseCount + (wave - 1) / m_WavesPerExtra);
            var tower = TowerPosition();
            m_FreeSpots.Clear();
            foreach (var spot in m_Spots)
            {
                var flat = spot != null ? spot.position - tower : Vector3.zero;
                flat.y = 0f;
                if (spot != null && flat.magnitude >= m_MinTowerDistance)
                    m_FreeSpots.Add(spot);
            }

            var spawned = 0;
            while (spawned < count && m_FreeSpots.Count > 0)
            {
                var index = Random.Range(0, m_FreeSpots.Count);
                var spot = m_FreeSpots[index];
                m_FreeSpots.RemoveAt(index);
                SpawnAt(spot.position, AmountFor(wave));
                spawned++;
            }

            if (spawned == 0)
            {
                Debug.LogWarning("CrystalSpawner : aucun emplacement à plus de " + m_MinTowerDistance + " m de la tour.", this);
                return;
            }

            if (m_AppearClip != null)
                Sfx.Play(m_AppearClip, PlayerPosition() + Vector3.up, 0.8f, 1f, 0f);
            Announce(spawned == 1 ? "Un gisement de cristaux est apparu !" : $"{spawned} gisements de cristaux sont apparus !");
        }

        int AmountFor(int wave) => m_BaseAmount + m_AmountPerWave * Mathf.Max(0, wave - 1);

        CrystalDeposit SpawnAt(Vector3 position, int amount)
        {
            var deposit = Instantiate(m_DepositPrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            deposit.Amount = amount;
            m_Deposits.Add(deposit);
            return deposit;
        }

        void OnHarvested(CrystalDeposit deposit)
        {
            var remaining = Remaining;
            var text = $"+{deposit.Amount} matériaux";
            if (m_Waves != null && m_Waves.Phase == WavePhase.Wave && m_Deposits.Contains(deposit))
                text += remaining == 0 ? "\n<size=70%>Tous les gisements sont récoltés</size>" : $"\n<size=70%>Encore {remaining} gisement{(remaining > 1 ? "s" : "")}</size>";
            Announce(text);
        }

        // Raccourci de test au clavier (éditeur) : V pose un gisement 3 m devant le joueur.
        void UpdateDebugKey()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var keyboard = Keyboard.current;
            var rig = PlayerRig.Instance;
            if (keyboard == null || !keyboard.vKey.wasPressedThisFrame || rig == null || m_DepositPrefab == null)
                return;

            var position = rig.BodyPosition + rig.HeadYaw * Vector3.forward * 3f;
            if (Physics.Raycast(position + Vector3.up * 2f, Vector3.down, out var hit, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                position = hit.point;
            SpawnAt(position, AmountFor(m_Waves != null ? Mathf.Max(1, m_Waves.WaveNumber) : 1));
#endif
        }

        void Announce(string text)
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (head != null)
                FloatingText.Spawn(head.position + rig.HeadYaw * new Vector3(0f, 0.15f, 2.4f), text, m_MessageColor, 1.1f, 2f);
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
