using System.Collections.Generic;
using Archery.Core;
using Archery.Defense;
using Archery.Player;
using Archery.UI;
using Archery.Waves;
using UnityEngine;
using UnityEngine.AI;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace Archery.Chests
{
    /// <summary>
    /// Fait apparaître 1 ou 2 coffres par vague (GDD, section 13), loin de la tour : il faut descendre et courir
    /// pour aller les ouvrir. Les coffres disparaissent à la fin de la vague, ouverts ou non.
    /// </summary>
    /// <remarks>
    /// Sans points d'apparition, les coffres se posent au hasard sur le sol des ennemis (NavMesh), dans un anneau
    /// autour de la tour.
    /// </remarks>
    [DisallowMultipleComponent]
    public class ChestSpawner : MonoBehaviour
    {
        [SerializeField]
        Chest m_ChestPrefab;

        [Tooltip("Emplacements possibles (objets vides). Vide : au hasard sur le NavMesh, autour de la tour.")]
        [SerializeField]
        List<Transform> m_SpawnPoints = new List<Transform>();

        [Tooltip("Distance minimale (m) entre la tour et un coffre tiré au hasard.")]
        [SerializeField]
        float m_MinDistance = 15f;

        [Tooltip("Distance maximale (m) entre la tour et un coffre tiré au hasard.")]
        [SerializeField]
        float m_MaxDistance = 30f;

        [Tooltip("Distance minimale (m) entre deux coffres.")]
        [SerializeField]
        float m_MinSpacing = 8f;

        [Tooltip("Chance (0 à 1) d'un deuxième coffre dans la vague.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_SecondChestChance = 0.5f;

        [Tooltip("Moments d'apparition du premier et du deuxième coffre, en part du chrono (0,15 = après 15 % de la vague).")]
        [SerializeField]
        Vector2 m_SpawnMoments = new Vector2(0.15f, 0.55f);

        [Tooltip("Optionnel : son de l'apparition d'un coffre.")]
        [SerializeField]
        AudioClip m_AppearClip;

        [SerializeField]
        Color m_MessageColor = new Color(1f, 0.85f, 0.3f);

        readonly List<Chest> m_Chests = new List<Chest>();
        readonly List<float> m_Pending = new List<float>();
        readonly List<Transform> m_FreePoints = new List<Transform>();
        WaveManager m_Waves;

        void Start()
        {
            m_Waves = WaveManager.Instance;
            if (m_Waves == null)
            {
                Debug.LogWarning("ChestSpawner : aucun Wave Manager, les coffres n'apparaîtront jamais.", this);
                return;
            }

            if (m_ChestPrefab == null)
                Debug.LogWarning("ChestSpawner : aucun prefab de coffre.", this);

            m_Waves.WaveStarted += OnWaveStarted;
            m_Waves.WaveEnded += OnWaveEnded;
            m_Waves.GameOver += ClearAll;
        }

        void OnDestroy()
        {
            if (m_Waves == null)
                return;

            m_Waves.WaveStarted -= OnWaveStarted;
            m_Waves.WaveEnded -= OnWaveEnded;
            m_Waves.GameOver -= ClearAll;
        }

        void OnWaveStarted(int wave)
        {
            m_Pending.Clear();
            m_Pending.Add(m_SpawnMoments.x);
            if (Random.value < m_SecondChestChance)
                m_Pending.Add(m_SpawnMoments.y);
        }

        void OnWaveEnded(int wave) => ClearAll();

        // Fin de vague ou de partie : les coffres restants disparaissent.
        void ClearAll()
        {
            m_Pending.Clear();
            foreach (var chest in m_Chests)
            {
                if (chest != null)
                    chest.Vanish();
            }

            m_Chests.Clear();
        }

        void Update()
        {
            UpdateDebugKey();
            if (m_Waves == null || m_Waves.Phase != WavePhase.Wave || m_Pending.Count == 0 || m_Waves.WaveDuration <= 0f)
                return;

            var elapsed = 1f - m_Waves.TimeRemaining / m_Waves.WaveDuration;
            for (var i = m_Pending.Count - 1; i >= 0; i--)
            {
                if (elapsed < m_Pending[i])
                    continue;

                m_Pending.RemoveAt(i);
                Spawn();
            }
        }

        void Spawn()
        {
            if (m_ChestPrefab == null || !TryFindPosition(out var position))
                return;

            // Le coffre est tourné vers la tour : on arrive par l'avant.
            SpawnAt(position, TowerPosition());
        }

        void SpawnAt(Vector3 position, Vector3 lookAt)
        {
            var toTarget = lookAt - position;
            toTarget.y = 0f;
            var rotation = toTarget.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toTarget) : Quaternion.identity;

            var chest = Instantiate(m_ChestPrefab, position, rotation);
            m_Chests.Add(chest);

            Sfx.Play(m_AppearClip, position + Vector3.up, 1f, 1f, 0.5f);
            Announce();
        }

        // Raccourci de test au clavier (éditeur) : C pose un coffre 2,5 m devant le joueur, tourné vers lui.
        void UpdateDebugKey()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var keyboard = Keyboard.current;
            var rig = PlayerRig.Instance;
            if (keyboard == null || !keyboard.cKey.wasPressedThisFrame || rig == null || m_ChestPrefab == null)
                return;

            var feet = rig.BodyPosition;
            var position = feet + rig.HeadYaw * Vector3.forward * 2.5f;
            if (Physics.Raycast(position + Vector3.up * 2f, Vector3.down, out var hit, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                position = hit.point;
            SpawnAt(position, feet);
#endif
        }

        void Announce()
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (head != null)
                FloatingText.Spawn(head.position + rig.HeadYaw * new Vector3(0f, 0.25f, 2.5f), "Un coffre est apparu !", m_MessageColor, 1.2f, 2f);
        }

        bool TryFindPosition(out Vector3 position)
        {
            m_Chests.RemoveAll(chest => chest == null);
            return m_SpawnPoints.Count > 0 ? TryPickSpawnPoint(out position) : TryPickRandomPosition(out position);
        }

        bool TryPickSpawnPoint(out Vector3 position)
        {
            m_FreePoints.Clear();
            foreach (var point in m_SpawnPoints)
            {
                if (point != null && IsFarFromOtherChests(point.position))
                    m_FreePoints.Add(point);
            }

            position = default;
            if (m_FreePoints.Count == 0)
                return false;

            position = m_FreePoints[Random.Range(0, m_FreePoints.Count)].position;
            return true;
        }

        // Un point au hasard dans l'anneau autour de la tour, posé sur le NavMesh.
        bool TryPickRandomPosition(out Vector3 position)
        {
            var center = TowerPosition();
            for (var attempt = 0; attempt < 30; attempt++)
            {
                var angle = Random.Range(0f, Mathf.PI * 2f);
                var distance = Random.Range(m_MinDistance, Mathf.Max(m_MinDistance, m_MaxDistance));
                var candidate = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
                if (!NavMesh.SamplePosition(candidate, out var hit, 3f, NavMesh.AllAreas))
                    continue;

                var flat = hit.position - center;
                flat.y = 0f;
                if (flat.magnitude < m_MinDistance * 0.8f || !IsFarFromOtherChests(hit.position))
                    continue;

                position = hit.position;
                return true;
            }

            position = default;
            Debug.LogWarning("ChestSpawner : aucune place trouvée pour un coffre (NavMesh autour de la tour ?).", this);
            return false;
        }

        bool IsFarFromOtherChests(Vector3 position)
        {
            foreach (var chest in m_Chests)
            {
                if (chest != null && Vector3.Distance(chest.transform.position, position) < m_MinSpacing)
                    return false;
            }

            return true;
        }

        Vector3 TowerPosition()
        {
            var tower = Tower.Instance;
            return tower != null ? tower.transform.position : transform.position;
        }
    }
}
