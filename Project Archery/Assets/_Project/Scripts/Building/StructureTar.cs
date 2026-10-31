using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Building
{
    /// <summary>
    /// Goudron (refonte du tower defense, étape 5, lot 1 ; 28 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, partie 6.1) : sur la
    /// voie, une flaque noire et brillante qui ralentit les ennemis au sol qui la traversent ; elle grandit avec les niveaux.
    /// Commune, par lots de 2. (Sa réaction au talisman de l'ingénieur, s'enflammer, vient à l'étape 8 ; ses fusions, le Brasier
    /// et le Verglas, à l'étape 6.)
    /// </summary>
    /// <remarks>La flaque est faite par le code au lancement (un disque irrégulier) ; les tonneaux renversés sont dans le prefab.</remarks>
    public class StructureTar : Structure
    {
        [Tooltip("Vitesse gardée par les ennemis dans la flaque, par niveau (0,6 = 40 % plus lents).")]
        [SerializeField]
        float[] m_SpeedPerLevel = { 0.6f, 0.5f, 0.4f };

        [Tooltip("Rayon (m) de la flaque, par niveau.")]
        [SerializeField]
        float[] m_RadiusPerLevel = { 1.4f, 1.7f, 2f };

        [Tooltip("Matériau de la flaque (noir, très lisse).")]
        [SerializeField]
        Material m_PuddleMaterial;

        [Tooltip("Temps (s) entre deux vérifications des ennemis dans la flaque ; le ralentissement dure un peu plus.")]
        [SerializeField]
        float m_TickInterval = 0.25f;

        Transform m_Puddle;
        float m_Timer;

        float Radius => ValueFor(m_RadiusPerLevel, Mathf.Max(1, Level), 1.4f);

        protected override void Awake()
        {
            BuildPuddle();
            base.Awake();
        }

        protected override void OnLevelChanged(int level)
        {
            base.OnLevelChanged(level);
            ScalePuddle();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsBuilt)
                return;

            m_Timer -= Time.deltaTime;
            if (m_Timer > 0f)
                return;

            m_Timer = m_TickInterval;
            var speed = ValueFor(m_SpeedPerLevel, Level, 0.6f);
            var radius = Radius;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive || enemy.IsAirborne)
                    continue;

                var offset = enemy.transform.position - transform.position;
                if (offset.y < -1f || offset.y > 1.2f)
                    continue;
                offset.y = 0f;
                if (offset.sqrMagnitude <= radius * radius)
                    enemy.Slow(speed, m_TickInterval * 2f);
            }
        }

        void ScalePuddle()
        {
            if (m_Puddle != null)
                m_Puddle.localScale = new Vector3(Radius, 1f, Radius);
        }

        // Un disque de rayon 1 au bord irrégulier, à peine au-dessus du sol.
        void BuildPuddle()
        {
            var go = new GameObject("Flaque");
            m_Puddle = go.transform;
            m_Puddle.SetParent(transform, false);
            m_Puddle.localPosition = Vector3.up * 0.03f;

            const int segments = 28;
            var vertices = new List<Vector3> { Vector3.zero };
            var triangles = new List<int>();
            var random = new System.Random(7);
            for (var i = 0; i < segments; i++)
            {
                var angle = i * Mathf.PI * 2f / segments;
                var radius = 0.85f + 0.15f * (float)random.NextDouble();
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }

            for (var i = 0; i < segments; i++)
            {
                triangles.Add(0);
                triangles.Add(1 + (i + 1) % segments);
                triangles.Add(1 + i);
            }

            var mesh = new Mesh { name = "Flaque de goudron" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_PuddleMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            ScalePuddle();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.3f, 0.2f, 0.7f);
            Gizmos.DrawWireSphere(transform.position, ValueFor(m_RadiusPerLevel, Mathf.Max(1, Level), 1.4f));
        }
    }
}
