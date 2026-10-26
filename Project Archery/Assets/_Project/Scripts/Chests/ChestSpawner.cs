using System.Collections.Generic;
using Archery.Bows;
using Archery.Core;
using Archery.Defense;
using Archery.Enemies;
using Archery.Player;
using Archery.UI;
using Archery.Waves;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace Archery.Chests
{
    /// <summary>
    /// Fait apparaître 2 ou 3 coffres par vague (GDD, sections 3 bis et 13), loin de la tour : il faut descendre et courir
    /// pour aller les ouvrir. Le premier arrive pendant l'accalmie, le deuxième au début de la montée, et un troisième
    /// parfois plus tard. Les coffres disparaissent à la fin de la vague, ouverts ou non.
    /// Depuis la refonte du 25 octobre, le boss vaincu laisse aussi un coffre d'objets, qui tombe près du joueur au début
    /// de la pause (pas après la victoire contre le boss final) ; il reste jusqu'à la fin de la vague suivante.
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

        [Tooltip("Moments d'apparition des deux coffres de chaque vague, en part du chrono (0,1 = après 10 % du chrono) : " +
                 "le premier pendant l'accalmie, le deuxième au début de la montée.")]
        [SerializeField]
        Vector2 m_SpawnMoments = new Vector2(0.1f, 0.45f);

        [Tooltip("Chance (0 à 1) d'un troisième coffre dans la vague.")]
        [Range(0f, 1f)]
        [FormerlySerializedAs("m_SecondChestChance")]
        [SerializeField]
        float m_ExtraChestChance = 0.5f;

        [Tooltip("Moment d'apparition du troisième coffre, en part du chrono.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_ExtraChestMoment = 0.7f;

        [Tooltip("Optionnel : son de l'apparition d'un coffre.")]
        [SerializeField]
        AudioClip m_AppearClip;

        [SerializeField]
        Color m_MessageColor = new Color(1f, 0.85f, 0.3f);

        [Header("Coffre de boss")]
        [Tooltip("Délai (s) entre la fin de la vague et la chute du coffre du boss : le message de fin de vague passe d'abord.")]
        [SerializeField]
        float m_BossChestDelay = 1.5f;

        [Tooltip("Distance (m) entre le joueur et le coffre du boss, devant lui à droite (la boutique s'ouvre à sa gauche).")]
        [SerializeField]
        float m_BossChestDistance = 1.8f;

        // Places essayées pour le coffre de boss, en degrés depuis le regard du joueur (positif : vers sa droite).
        static readonly float[] k_BossChestAngles = { 40f, 20f, 60f, 0f, 85f, -20f, 110f, -45f, 145f, -80f, 180f };
        static readonly float[] k_BossChestDistanceScales = { 1f, 0.7f, 1.4f };
        static readonly Vector3 k_BossChestHalfExtents = new Vector3(0.45f, 0.4f, 0.45f);
        static readonly Collider[] s_Overlaps = new Collider[16];

        readonly List<Chest> m_Chests = new List<Chest>();
        readonly List<float> m_Pending = new List<float>();
        readonly List<Transform> m_FreePoints = new List<Transform>();
        WaveManager m_Waves;
        float m_BossChestTimer = -1f;

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

            // Pas de coffre pendant la vague du boss final : la carte est vide.
            if (m_Waves.IsFinalBossWave)
                return;

            m_Pending.Add(m_SpawnMoments.x);
            m_Pending.Add(m_SpawnMoments.y);
            if (Random.value < m_ExtraChestChance)
                m_Pending.Add(m_ExtraChestMoment);
        }

        void OnWaveEnded(int wave)
        {
            ClearAll();

            // Le boss vaincu laisse un coffre d'objets, sauf le boss final de la victoire.
            if (!(m_Waves.HasWon && wave == m_Waves.WavesToWin))
                m_BossChestTimer = Mathf.Max(0.01f, m_BossChestDelay);
        }

        // Fin de vague ou de partie : les coffres restants disparaissent.
        void ClearAll()
        {
            m_Pending.Clear();
            m_BossChestTimer = -1f;
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
            if (m_BossChestTimer > 0f)
            {
                m_BossChestTimer -= Time.deltaTime;
                if (m_BossChestTimer <= 0f)
                    SpawnBossChest();
            }

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
            Announce();
        }

        Chest SpawnAt(Vector3 position, Vector3 lookAt)
        {
            var toTarget = lookAt - position;
            toTarget.y = 0f;
            var rotation = toTarget.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toTarget) : Quaternion.identity;

            var chest = Instantiate(m_ChestPrefab, position, rotation);
            m_Chests.Add(chest);

            Sfx.Play(m_AppearClip, position + Vector3.up, 1f, 1f, 0.5f);
            return chest;
        }

        // Le coffre du boss tombe près du joueur, où qu'il soit, tourné vers lui.
        void SpawnBossChest()
        {
            m_BossChestTimer = -1f;
            var rig = PlayerRig.Instance;
            if (m_ChestPrefab == null || rig == null)
                return;

            var feet = rig.BodyPosition;
            var head = rig.Head != null ? rig.Head.position : feet + Vector3.up * 1.6f;
            SpawnAt(FindBossChestPlace(feet, head, rig.HeadYaw), feet).MakeBossChest();
            HeadMessages.Show("Le boss a laissé un coffre !\n<size=60%>Un objet à choisir</size>", m_MessageColor, 4f);
        }

        // Devant le joueur à droite si possible, sur le même sol que lui (le haut de la tour, ou la carte), à une place libre
        // qu'il voit ; sinon juste devant lui.
        Vector3 FindBossChestPlace(Vector3 feet, Vector3 head, Quaternion yaw)
        {
            foreach (var scale in k_BossChestDistanceScales)
            {
                foreach (var angle in k_BossChestAngles)
                {
                    var direction = yaw * Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                    if (TryGround(feet + direction * (m_BossChestDistance * scale), feet.y, out var ground) && IsFreeAndVisible(ground, head))
                        return ground;
                }
            }

            var front = feet + yaw * Vector3.forward * 1.5f;
            return TryGround(front, feet.y, out var fallback) ? fallback : front;
        }

        static bool TryGround(Vector3 point, float floorHeight, out Vector3 ground)
        {
            ground = point;
            if (!Physics.Raycast(point + Vector3.up * 1.5f, Vector3.down, out var hit, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ||
                ArrowIgnore.IsIgnored(hit.collider) || Mathf.Abs(hit.point.y - floorHeight) > 0.6f)
                return false;

            ground = hit.point;
            return true;
        }

        // Rien à la place du coffre (le joueur, son arc et les ennemis qui s'enfuient ne comptent pas), et le joueur le voit.
        static bool IsFreeAndVisible(Vector3 ground, Vector3 head)
        {
            var center = ground + Vector3.up * (k_BossChestHalfExtents.y + 0.12f);
            var count = Physics.OverlapBoxNonAlloc(center, k_BossChestHalfExtents, s_Overlaps, Quaternion.identity, Physics.DefaultRaycastLayers,
                                                   QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                if (!ArrowIgnore.IsIgnored(s_Overlaps[i]) && s_Overlaps[i].GetComponentInParent<Enemy>() == null)
                    return false;
            }

            return !Physics.Linecast(head, center, out var hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ||
                   ArrowIgnore.IsIgnored(hit.collider) || hit.collider.GetComponentInParent<Enemy>() != null;
        }

        // Raccourcis de test au clavier (éditeur) : C pose un coffre 2,5 m devant le joueur, tourné vers lui ; X, un coffre de boss.
        void UpdateDebugKey()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var keyboard = Keyboard.current;
            var rig = PlayerRig.Instance;
            if (keyboard == null || rig == null || m_ChestPrefab == null)
                return;

            if (keyboard.xKey.wasPressedThisFrame)
                SpawnBossChest();
            if (!keyboard.cKey.wasPressedThisFrame)
                return;

            var feet = rig.BodyPosition;
            var position = feet + rig.HeadYaw * Vector3.forward * 2.5f;
            if (Physics.Raycast(position + Vector3.up * 2f, Vector3.down, out var hit, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                position = hit.point;
            SpawnAt(position, feet);
            Announce();
#endif
        }

        void Announce()
        {
            HeadMessages.Show("Un coffre est apparu !", m_MessageColor, 4f);
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
