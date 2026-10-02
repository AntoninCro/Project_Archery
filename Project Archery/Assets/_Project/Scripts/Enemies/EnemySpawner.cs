using System.Collections.Generic;
using Archery.Defense;
using UnityEngine;
using UnityEngine.AI;

namespace Archery.Enemies
{
    /// <summary>
    /// Fait apparaître des ennemis à intervalle régulier sur des points d'apparition.
    /// Version simple pour tester : la gestion des vagues la remplacera.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
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

        public Enemy Spawn()
        {
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

            var enemy = Instantiate(m_EnemyPrefab, hit.position, rotation);
            m_Spawned.Add(enemy);
            return enemy;
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
