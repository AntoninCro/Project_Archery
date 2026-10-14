using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Player;
using UnityEngine;

namespace Archery.FinalBoss
{
    /// <summary>
    /// Rocher qui tombe du plafond quand le dragon enragé rugit : un cercle rouge prévient au sol, puis le rocher
    /// s'écrase et blesse le joueur qui est dessous.
    /// </summary>
    public class FallingRock : MonoBehaviour
    {
        Vector3 m_Target;
        Vector3 m_Start;
        float m_Warning;
        float m_FallTime;
        float m_Age;
        float m_Damage;
        float m_Radius;
        bool m_Landed;
        Transform m_Rock;
        Transform m_Circle;
        Material m_CircleMaterial;
        Material m_FireMaterial;
        AudioClip m_ImpactClip;
        Object m_Source;

        /// <summary>Lance la chute d'un rocher sur ce point du sol.</summary>
        public static void Spawn(Vector3 groundPoint, Vector3 ceilingPoint, float warning, float damage, float radius,
                                 Material rockMaterial, Material circleMaterial, Material fireMaterial, AudioClip impactClip, Object source)
        {
            var go = new GameObject("Rocher qui tombe");
            go.transform.position = groundPoint;
            var rock = go.AddComponent<FallingRock>();
            rock.m_Target = groundPoint;
            rock.m_Start = ceilingPoint;
            rock.m_Warning = warning;
            rock.m_FallTime = 0.55f;
            rock.m_Damage = damage;
            rock.m_Radius = radius;
            rock.m_CircleMaterial = circleMaterial;
            rock.m_FireMaterial = fireMaterial;
            rock.m_ImpactClip = impactClip;
            rock.m_Source = source;
            rock.Build(rockMaterial);
        }

        void Build(Material rockMaterial)
        {
            var random = new System.Random(Random.Range(0, int.MaxValue));
            var rockMesh = new MeshBuilder();
            rockMesh.Rock(Vector3.zero, Vector3.one * (m_Radius * 0.45f), Quaternion.identity, Color.white, random, 0.35f);
            m_Rock = CreateMeshObject("Rocher", rockMesh.ToMesh("Rocher"), rockMaterial).transform;
            m_Rock.position = m_Start;
            m_Rock.gameObject.SetActive(false);

            var circle = new MeshBuilder();
            const int sides = 32;
            for (var i = 0; i < sides; i++)
            {
                var a0 = i * Mathf.PI * 2f / sides;
                var a1 = (i + 1) * Mathf.PI * 2f / sides;
                var inner = 0.82f;
                var red = new Color(1f, 0.15f, 0.05f, 0.85f);
                var p0 = new Vector3(Mathf.Sin(a0), 0f, Mathf.Cos(a0));
                var p1 = new Vector3(Mathf.Sin(a1), 0f, Mathf.Cos(a1));
                circle.Quad(p0 * inner, p0, p1, p1 * inner, red);
                circle.Triangle(Vector3.zero, p0 * inner, p1 * inner, new Color(1f, 0.1f, 0.05f, 0.25f));
            }

            m_Circle = CreateMeshObject("Cercle", circle.ToMesh("Cercle d'alerte"), m_CircleMaterial).transform;
            m_Circle.position = m_Target + Vector3.up * 0.06f;
            m_Circle.localScale = Vector3.one * m_Radius;
        }

        GameObject CreateMeshObject(string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        void Update()
        {
            m_Age += Time.deltaTime;
            if (m_Circle != null)
            {
                // Le cercle bat de plus en plus vite avant l'impact.
                var urgency = Mathf.Clamp01(m_Age / Mathf.Max(0.1f, m_Warning));
                var beat = 0.85f + 0.15f * Mathf.Sin(m_Age * Mathf.Lerp(8f, 30f, urgency));
                m_Circle.localScale = Vector3.one * (m_Radius * beat);
            }

            if (m_Landed)
            {
                // Le rocher reste un moment, puis s'enfonce.
                if (m_Age > m_Warning + m_FallTime + 4f)
                    m_Rock.position += Vector3.down * (Time.deltaTime * 0.6f);
                if (m_Age > m_Warning + m_FallTime + 6f)
                    Destroy(gameObject);
                return;
            }

            if (m_Age < m_Warning)
                return;

            m_Rock.gameObject.SetActive(true);
            var t = Mathf.Clamp01((m_Age - m_Warning) / m_FallTime);
            m_Rock.position = Vector3.Lerp(m_Start, m_Target + Vector3.up * (m_Radius * 0.3f), t * t);
            m_Rock.rotation = Quaternion.Euler(t * 200f, t * 90f, 0f);
            if (t >= 1f)
                Land();
        }

        void Land()
        {
            m_Landed = true;
            if (m_Circle != null)
                Destroy(m_Circle.gameObject);

            FireEffects.Burst(m_Target + Vector3.up * 0.3f, m_FireMaterial, m_Radius * 0.6f, 30);
            Sfx.Play(m_ImpactClip, m_Target, 1f, Random.Range(0.6f, 0.75f));
            if (ArenaPlayer.FeetInside(m_Target, m_Radius))
            {
                ArenaPlayer.Damage(m_Damage, m_Target, m_Source);
                ArenaPlayer.Rumble(0.8f, 0.25f);
            }
            else if (ArenaPlayer.FeetInside(m_Target, m_Radius * 3f))
            {
                ArenaPlayer.Rumble(0.3f, 0.15f);
            }
        }

        void OnDestroy()
        {
            foreach (var filter in GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh != null)
                    Destroy(filter.sharedMesh);
            }
        }
    }
}
