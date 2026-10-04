using System.Collections.Generic;
using Archery.Defense;
using UnityEngine;
using UnityEngine.AI;

namespace Archery.Enemies
{
    /// <summary>
    /// Points d'apparition des ennemis. Le gestionnaire de vagues s'en sert pour faire apparaître
    /// chaque ennemi ; seul, il peut aussi en faire apparaître à intervalle régulier (« Spawn On Start »).
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Tooltip("Ennemi utilisé quand le spawner fonctionne seul (sans gestionnaire de vagues).")]
        [SerializeField]
        Enemy m_EnemyPrefab;

        [Tooltip("Points d'apparition (objets vides au bord de la carte). Si la liste est vide, la position du spawner est utilisée.")]
        [SerializeField]
        Transform[] m_SpawnPoints = new Transform[0];

        [Tooltip("Temps (s) entre deux apparitions.")]
        [SerializeField]
        float m_SpawnInterval = 3f;

        [Tooltip("Nombre maximal d'ennemis vivants en même temps.")]
        [SerializeField]
        int m_MaxAlive = 6;

        [Tooltip("Fait apparaître des ennemis tout seul. À décocher quand un Wave Manager s'en occupe.")]
        [SerializeField]
        bool m_SpawnOnStart = true;

        readonly List<Enemy> m_Spawned = new List<Enemy>();
        float m_Timer = 1f;

        public bool IsSpawning { get; set; }

        void Start()
        {
            IsSpawning = m_SpawnOnStart;
            if (m_EnemyPrefab == null)
                Debug.LogError("EnemySpawner : aucun prefab d'ennemi assigné.", this);
        }

        void Update()
        {
            if (!IsSpawning || m_EnemyPrefab == null)
                return;

            m_Spawned.RemoveAll(enemy => enemy == null || !enemy.IsAlive);
            m_Timer -= Time.deltaTime;
            if (m_Timer > 0f || m_Spawned.Count >= m_MaxAlive)
                return;

            m_Timer = m_SpawnInterval;
            Spawn();
        }

        public Enemy Spawn() => Spawn(m_EnemyPrefab);

        /// <summary>Fait apparaître cet ennemi sur un point d'apparition au hasard.</summary>
        public Enemy Spawn(Enemy prefab)
        {
            if (prefab == null)
                return null;

            var point = m_SpawnPoints.Length > 0 ? m_SpawnPoints[Random.Range(0, m_SpawnPoints.Length)] : transform;
            if (point == null)
                point = transform;

            // L'agent doit apparaître sur le NavMesh, sinon il ne peut pas se déplacer.
            if (!NavMesh.SamplePosition(point.position, out var hit, 4f, NavMesh.AllAreas))
            {
                Debug.LogWarning($"EnemySpawner : pas de NavMesh près de « {point.name} ». As-tu fait le Bake du NavMesh ?", point);
                return null;
            }

            var rotation = point.rotation;
            var tower = Tower.Instance;
            if (tower != null)
            {
                var toTower = tower.transform.position - hit.position;
                toTower.y = 0f;
                if (toTower.sqrMagnitude > 0.01f)
                    rotation = Quaternion.LookRotation(toTower);
            }

            var enemy = Instantiate(prefab, hit.position, rotation);
            m_Spawned.Add(enemy);
            return enemy;
        }

        /// <summary>Point d'apparition le plus proche : c'est par là que les ennemis s'enfuient.</summary>
        public Vector3 NearestSpawnPoint(Vector3 from)
        {
            var best = transform.position;
            var bestSqrDistance = float.MaxValue;
            foreach (var point in m_SpawnPoints)
            {
                if (point == null)
                    continue;

                var sqrDistance = (point.position - from).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    best = point.position;
                }
            }

            return best;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.2f);
            foreach (var point in m_SpawnPoints)
            {
                if (point != null)
                    Gizmos.DrawWireSphere(point.position, 0.6f);
            }
        }
    }
}
