using System.Collections.Generic;
using Archery.Bows;
using Archery.Core;
using Archery.Enemies;
using Archery.Player;
using Archery.UI;
using Archery.Waves;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace Archery.Chests
{
    /// <summary>
    /// Le coffre du boss (GDD, section 13) : depuis la refonte du 25 octobre, le boss vaincu laisse un coffre d'objets, là où il
    /// est tombé, au début de la pause (pas après la victoire contre le boss final) ; il reste jusqu'à la fin de la vague suivante.
    /// </summary>
    /// <remarks>
    /// Jusqu'au 2 novembre, il faisait aussi apparaître 2 ou 3 coffres par vague, loin de la tour, dont deux pendant la montée. Le
    /// joueur, après son premier essai (« un simulateur de randonnée ») : tous les coffres de la vague arrivent ensemble, au gong,
    /// d'un seul côté de la forêt (<c>Building/CrystalSpawner</c>, qui prend aussi les emplacements d'ici).
    /// </remarks>
    [DisallowMultipleComponent]
    public class ChestSpawner : MonoBehaviour
    {
        [SerializeField]
        Chest m_ChestPrefab;

        [Tooltip("Emplacements de coffres (objets vides) : ceux des coffres de vague, que le Crystal Spawner reprend avec les siens " +
                 "depuis le 2 novembre.")]
        [SerializeField]
        List<Transform> m_SpawnPoints = new List<Transform>();

        [Tooltip("Optionnel : son de l'apparition d'un coffre.")]
        [SerializeField]
        AudioClip m_AppearClip;

        [SerializeField]
        Color m_MessageColor = new Color(1f, 0.85f, 0.3f);

        [Header("Coffre de boss")]
        [Tooltip("Le boss vaincu laisse un coffre (décoché depuis le 8 novembre : la pause est un choix parmi trois, ShopManager).")]
        [SerializeField]
        bool m_BossChest;

        [Tooltip("Délai (s) entre la fin de la vague et la chute du coffre du boss : le message de fin de vague passe d'abord.")]
        [SerializeField]
        float m_BossChestDelay = 1.5f;

        [Tooltip("Le coffre du boss tombe là où le boss est mort (27 octobre au soir). Sans boss mort pendant la vague (raccourci X) : " +
                 "distance (m) entre le joueur et le coffre, juste devant lui (la boutique s'ouvre à sa gauche, l'atelier à sa droite).")]
        [SerializeField]
        float m_BossChestDistance = 1.8f;

        // Places essayées pour le coffre de boss, en degrés depuis le regard du joueur (positif : vers sa droite). Juste devant
        // d'abord depuis le 27 octobre au soir : l'atelier a pris la droite du joueur (devant à droite, 40°, avant).
        static readonly float[] k_BossChestAngles = { 0f, 15f, -15f, 30f, -30f, 60f, -60f, 90f, -90f, 135f, -135f, 180f };
        static readonly float[] k_BossChestDistanceScales = { 1f, 0.7f, 1.4f };
        static readonly Vector3 k_BossChestHalfExtents = new Vector3(0.45f, 0.4f, 0.45f);
        static readonly Collider[] s_Overlaps = new Collider[16];

        // Autour de l'endroit où le boss est mort, les places essayées pour son coffre (27 octobre au soir) : là même, puis deux
        // cercles (m), 8 places chacun.
        static readonly float[] k_DeathChestRings = { 1.5f, 3f };
        static readonly RaycastHit[] s_GroundHits = new RaycastHit[16];

        readonly List<Chest> m_Chests = new List<Chest>();
        WaveManager m_Waves;
        float m_BossChestTimer = -1f;
        bool m_HasBossDeath;
        Vector3 m_BossDeathPosition;

        /// <summary>Le prefab du coffre, repris par les caisses de l'ingénieur (<c>Building/CrystalSpawner</c>, 27 octobre).</summary>
        public Chest ChestPrefab => m_ChestPrefab;

        /// <summary>Les emplacements des coffres (la grille de construction n'y pose pas d'emplacement, 27 octobre).</summary>
        public IReadOnlyList<Transform> SpawnPoints => m_SpawnPoints;

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
            Boss.Defeated += OnBossDefeated;
        }

        void OnDestroy()
        {
            Boss.Defeated -= OnBossDefeated;
            if (m_Waves == null)
                return;

            m_Waves.WaveStarted -= OnWaveStarted;
            m_Waves.WaveEnded -= OnWaveEnded;
            m_Waves.GameOver -= ClearAll;
        }

        // L'endroit où le boss de la vague meurt (le dernier, s'il y en a plusieurs) : son coffre y tombera.
        void OnBossDefeated(Boss boss)
        {
            if (boss == null)
                return;

            m_HasBossDeath = true;
            m_BossDeathPosition = boss.transform.position;
        }

        void OnWaveStarted(int wave)
        {
            m_HasBossDeath = false;
        }

        void OnWaveEnded(int wave)
        {
            ClearAll();

            // Le boss vaincu laisse un coffre d'objets, sauf le boss final de la victoire. Plus depuis le 8 novembre (le joueur :
            // la pause devient un choix, une tourelle, un objet et un parchemin parmi trois ; ShopManager) : Boss Chest le rallume.
            if (m_BossChest && !(m_Waves.HasWon && wave == m_Waves.WavesToWin))
                m_BossChestTimer = Mathf.Max(0.01f, m_BossChestDelay);
        }

        // Fin de vague ou de partie : les coffres restants disparaissent.
        void ClearAll()
        {
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

        // Le coffre du boss tombe là où le boss est mort (27 octobre au soir, demandé par le joueur : il faut aller le chercher,
        // signalé par son rayon de lumière), tourné vers le joueur. Sans boss mort pendant la vague (raccourci X), ou sans sol
        // sous lui : près du joueur, où qu'il soit, comme avant.
        void SpawnBossChest()
        {
            m_BossChestTimer = -1f;
            var rig = PlayerRig.Instance;
            if (m_ChestPrefab == null || rig == null)
                return;

            var feet = rig.BodyPosition;
            var place = Vector3.zero;
            var atDeath = m_HasBossDeath && TryFindDeathChestPlace(m_BossDeathPosition, out place);
            m_HasBossDeath = false;
            if (atDeath)
            {
                SpawnAt(place, feet).MakeBossChest();
                HeadMessages.Show("Le boss a laissé un coffre là où il est tombé !\n<size=60%>Un objet à choisir</size>", m_MessageColor, 4f);
                return;
            }

            var head = rig.Head != null ? rig.Head.position : feet + Vector3.up * 1.6f;
            SpawnAt(FindBossChestPlace(feet, head, rig.HeadYaw), feet).MakeBossChest();
            HeadMessages.Show("Le boss a laissé un coffre !\n<size=60%>Un objet à choisir</size>", m_MessageColor, 4f);
        }

        // Le sol sous l'endroit où le boss est mort (son corps et les ennemis ne comptent pas), puis la première place libre : là
        // même, sinon sur deux cercles autour.
        bool TryFindDeathChestPlace(Vector3 death, out Vector3 place)
        {
            place = death;
            var count = Physics.RaycastNonAlloc(death + Vector3.up * 3f, Vector3.down, s_GroundHits, 60f, Physics.DefaultRaycastLayers,
                                                QueryTriggerInteraction.Ignore);
            var found = false;
            var best = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                var hit = s_GroundHits[i];
                if (hit.distance >= best || ArrowIgnore.IsIgnored(hit.collider) || hit.collider.GetComponentInParent<Enemy>() != null)
                    continue;

                best = hit.distance;
                place = hit.point;
                found = true;
            }

            if (!found)
                return false;

            var ground = place;
            if (IsFree(ground))
                return true;

            foreach (var radius in k_DeathChestRings)
            {
                for (var i = 0; i < 8; i++)
                {
                    var direction = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
                    if (TryGround(ground + direction * radius, ground.y, out var candidate) && IsFree(candidate))
                    {
                        place = candidate;
                        return true;
                    }
                }
            }

            place = ground;
            return true;
        }

        // Juste devant le joueur si possible, entre la boutique et l'atelier, sur le même sol que lui (le haut de la tour, ou la
        // carte), à une place libre qu'il voit ; sinon juste devant lui quand même.
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

        // Rien à la place du coffre (le joueur, son arc et les ennemis qui s'enfuient ne comptent pas).
        static bool IsFree(Vector3 ground)
        {
            var center = ground + Vector3.up * (k_BossChestHalfExtents.y + 0.12f);
            var count = Physics.OverlapBoxNonAlloc(center, k_BossChestHalfExtents, s_Overlaps, Quaternion.identity, Physics.DefaultRaycastLayers,
                                                   QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                if (!ArrowIgnore.IsIgnored(s_Overlaps[i]) && s_Overlaps[i].GetComponentInParent<Enemy>() == null)
                    return false;
            }

            return true;
        }

        // Rien à la place du coffre, et le joueur le voit.
        static bool IsFreeAndVisible(Vector3 ground, Vector3 head)
        {
            if (!IsFree(ground))
                return false;

            var center = ground + Vector3.up * (k_BossChestHalfExtents.y + 0.12f);
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
    }
}
