using UnityEngine;

namespace Archery.FinalBoss
{
    /// <summary>
    /// Anneau de feu du dragon (il se dresse sur ses pattes arrière, puis frappe le sol) : un mur de flammes bas qui part
    /// du point d'impact et s'élargit à travers toute l'arène, en suivant le sol. On l'esquive en sautant par-dessus au
    /// moment où il passe sous ses pieds.
    /// </summary>
    public class FireRing : MonoBehaviour
    {
        const int k_Segments = 128;
        const float k_FlameDensity = 6f;   // flammes par mètre de bord et par seconde

        ArenaShape m_Arena;
        Mesh m_Mesh;
        Vector3[] m_Vertices;
        ParticleSystem m_Flames;
        Object m_Source;
        float m_Radius = 0.5f;
        float m_MaxRadius;
        float m_Speed;
        float m_Height;
        float m_Shown;
        float m_Damage;
        float m_Clearance;
        float m_EndTime = -1f;
        bool m_Passed;

        /// <summary>
        /// Lance un anneau depuis ce point du sol. <paramref name="clearance"/> : hauteur des pieds au-dessus du sol pour
        /// passer par-dessus.
        /// </summary>
        public static FireRing Spawn(Vector3 center, float speed, float maxRadius, float height, float damage, float clearance,
                                     Material wallMaterial, Material fireMaterial, ArenaShape arena, Object source)
        {
            var go = new GameObject("Anneau de feu");
            go.transform.position = center;
            var ring = go.AddComponent<FireRing>();
            ring.m_Arena = arena;
            ring.m_Speed = speed;
            ring.m_MaxRadius = maxRadius;
            ring.m_Height = height;
            ring.m_Shown = height;
            ring.m_Damage = damage;
            ring.m_Clearance = clearance;
            ring.m_Source = source;
            ring.BuildMesh(wallMaterial);
            if (fireMaterial != null)
                ring.m_Flames = FireEffects.CreateRingFlames(go.transform, fireMaterial);
            ring.Refresh();
            return ring;
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;
            if (m_EndTime < 0f)
            {
                var from = m_Radius;
                m_Radius = Mathf.Min(m_MaxRadius, m_Radius + m_Speed * deltaTime);
                CheckPlayer(from, m_Radius);
                if (m_Radius >= m_MaxRadius)
                {
                    m_EndTime = Time.time;
                    if (m_Flames != null)
                        m_Flames.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
            else
            {
                // Arrivé au bout de l'arène, le mur retombe et s'éteint.
                m_Shown = Mathf.MoveTowards(m_Shown, 0f, deltaTime * m_Height / 0.4f);
                if (Time.time - m_EndTime > 1f)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            Refresh();
        }

        // Le bord de l'anneau vient de passer là où est le joueur : touché, sauf s'il saute par-dessus.
        void CheckPlayer(float from, float to)
        {
            if (m_Passed || !ArenaPlayer.IsAlive)
                return;

            var offset = ArenaPlayer.Feet - transform.position;
            offset.y = 0f;
            if (offset.magnitude > to + 0.3f)
                return;

            m_Passed = true;
            if (ArenaPlayer.HeightAboveGround(m_Arena) >= m_Clearance)
            {
                ArenaPlayer.ShowDodged();
                return;
            }

            ArenaPlayer.Damage(m_Damage, ArenaPlayer.Feet + Vector3.up * 0.3f, m_Source);
            ArenaPlayer.Rumble(0.7f, 0.25f);
        }

        // Le mur : une bande verticale vue des deux côtés (on la voit arriver de l'intérieur comme de l'extérieur).
        void BuildMesh(Material material)
        {
            var side = (k_Segments + 1) * 2;
            m_Vertices = new Vector3[side * 2];
            var normals = new Vector3[side * 2];
            for (var i = 0; i <= k_Segments; i++)
            {
                var angle = i * Mathf.PI * 2f / k_Segments;
                var outward = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                normals[i * 2] = normals[i * 2 + 1] = outward;
                normals[side + i * 2] = normals[side + i * 2 + 1] = -outward;
            }

            var triangles = new int[k_Segments * 12];
            var t = 0;
            for (var i = 0; i < k_Segments; i++)
            {
                int bottom0 = i * 2, top0 = i * 2 + 1, bottom1 = i * 2 + 2, top1 = i * 2 + 3;

                // Face extérieure.
                triangles[t++] = bottom0;
                triangles[t++] = bottom1;
                triangles[t++] = top0;
                triangles[t++] = top0;
                triangles[t++] = bottom1;
                triangles[t++] = top1;

                // Face intérieure (ordre inversé).
                triangles[t++] = side + bottom0;
                triangles[t++] = side + top0;
                triangles[t++] = side + bottom1;
                triangles[t++] = side + top0;
                triangles[t++] = side + top1;
                triangles[t++] = side + bottom1;
            }

            m_Mesh = new Mesh { name = "Anneau de feu" };
            m_Mesh.MarkDynamic();
            m_Mesh.vertices = m_Vertices;
            m_Mesh.normals = normals;
            m_Mesh.triangles = triangles;

            gameObject.AddComponent<MeshFilter>().sharedMesh = m_Mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // Le mur au rayon actuel, posé sur le sol (il monte sur l'estrade, descend dans la lave).
        void Refresh()
        {
            var center = transform.position;
            var side = (k_Segments + 1) * 2;
            for (var i = 0; i <= k_Segments; i++)
            {
                var angle = i * Mathf.PI * 2f / k_Segments;
                var offset = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * m_Radius;
                var ground = m_Arena != null ? m_Arena.GroundHeightAt(center + offset) - center.y : 0f;
                var bottom = offset + Vector3.up * (ground - 0.1f);
                var top = bottom + Vector3.up * (m_Shown + 0.1f);
                m_Vertices[i * 2] = m_Vertices[side + i * 2] = bottom;
                m_Vertices[i * 2 + 1] = m_Vertices[side + i * 2 + 1] = top;
            }

            m_Mesh.vertices = m_Vertices;
            m_Mesh.RecalculateBounds();
            if (m_Flames != null)
                FireEffects.SetRingRadius(m_Flames, m_Radius, m_EndTime < 0f ? k_FlameDensity : 0f);
        }

        void OnDestroy()
        {
            if (m_Mesh != null)
                Destroy(m_Mesh);
        }
    }
}
