using System;
using System.Collections.Generic;
using Archery.Core;
using Archery.Player;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Archery.FinalBoss
{
    /// <summary>
    /// La porte glitchée (le passage vers le toit cyberpunk, <c>Archery.Cyber.CyberPassage</c>) : fermée, elle garde son
    /// aspect, mais par salves, quelques morceaux de ses battants se corrompent (des rectangles sombres, cyan, rouges ou
    /// presque noirs, comme des pixels morts), elle tressaute à peine et grésille… et on la traverse comme si elle n'était
    /// pas là. <see cref="GlitchCrossed"/> part quand la tête du joueur passe derrière les battants fermés.
    /// </summary>
    public partial class LairGate
    {
        static readonly int k_GlitchColorId = Shader.PropertyToID("_BaseColor");
        static readonly Color[] k_FragmentColors =
        {
            new Color(0.02f, 0.16f, 0.2f, 0.92f),
            new Color(0.2f, 0.02f, 0.03f, 0.92f),
            new Color(0.01f, 0.01f, 0.015f, 0.95f),
            new Color(0.12f, 0.02f, 0.11f, 0.9f),
        };

        // La tête est passée derrière les battants fermés, dans le couloir (repère de la porte : z vers la montagne).
        const float k_GlitchCrossDepth = 0.35f;

        // Morceaux corrompus par battant, et décalage (m) devant la face du battant.
        const int k_FragmentsPerLeaf = 6;
        const float k_FragmentLift = 0.012f;

        sealed class Fragment
        {
            public Transform Root;
            public MeshRenderer Renderer;
            public BoxCollider Face;
            public int Axis;
            public float Sign;
        }

        readonly List<Collider> m_GlitchColliders = new List<Collider>();
        readonly List<Fragment> m_Fragments = new List<Fragment>();
        MaterialPropertyBlock m_GlitchBlock;
        Material m_FragmentMaterial;
        Mesh m_FragmentMesh;
        Vector3 m_LeftHome;
        Vector3 m_RightHome;
        bool m_Glitched;
        bool m_GlitchCrossed;
        float m_GlitchTimer;
        float m_GlitchBurst;
        AudioClip m_GlitchClip;

        /// <summary>Le joueur vient de traverser la porte glitchée (fermée).</summary>
        public event Action GlitchCrossed;

        /// <summary>La porte est glitchée : elle tressaute et se traverse.</summary>
        public bool Glitched => m_Glitched;

        /// <summary>
        /// Glitche la porte fermée, ou la rend normale. <paramref name="crackle"/> : le grésillement qu'elle fait de temps en temps.
        /// </summary>
        public void SetGlitched(bool glitched, AudioClip crackle = null)
        {
            if (crackle != null)
                m_GlitchClip = crackle;
            if (glitched == m_Glitched)
                return;

            m_Glitched = glitched;
            m_GlitchCrossed = false;
            m_GlitchColliders.Clear();
            foreach (var hinge in new[] { m_LeftHinge, m_RightHinge })
            {
                if (hinge != null)
                    hinge.GetComponentsInChildren(true, m_GlitchColliders);
            }

            foreach (var collider in m_GlitchColliders)
            {
                if (collider != null)
                    collider.enabled = !glitched;
            }

            if (glitched)
            {
                m_LeftHome = m_LeftHinge != null ? m_LeftHinge.localPosition : Vector3.zero;
                m_RightHome = m_RightHinge != null ? m_RightHinge.localPosition : Vector3.zero;
                m_GlitchTimer = 0.4f;
                BuildFragments();
            }
            else
            {
                if (m_LeftHinge != null)
                    m_LeftHinge.localPosition = m_LeftHome;
                if (m_RightHinge != null)
                    m_RightHinge.localPosition = m_RightHome;
                ClearFragments();
            }
        }

        // Des salves espacées : la porte est normale la plupart du temps, puis quelques morceaux se corrompent.
        void UpdateGlitch()
        {
            if (!m_Glitched || !Application.isPlaying)
                return;

            var deltaTime = Time.deltaTime;
            m_GlitchBurst -= deltaTime;
            m_GlitchTimer -= deltaTime;
            if (m_GlitchTimer <= 0f)
            {
                if (m_GlitchBurst > 0f)
                {
                    m_GlitchTimer = Random.Range(0.05f, 0.1f);
                    ShowGlitchFrame(true);
                }
                else if (AnyFragmentShown())
                {
                    // Fin de la salve : tout redevient normal, jusqu'à la suivante.
                    m_GlitchTimer = Random.Range(0.8f, 2.5f);
                    ShowGlitchFrame(false);
                }
                else
                {
                    m_GlitchBurst = Random.Range(0.12f, 0.35f);
                    m_GlitchTimer = 0f;
                    Sfx.Play(m_GlitchClip, transform.TransformPoint(0f, m_DoorTop * 0.5f, 0f), 0.6f, Random.Range(0.8f, 1.15f), 0.9f);
                }
            }

            CheckGlitchCrossing();
        }

        void ShowGlitchFrame(bool glitching)
        {
            // Un petit sursaut des battants, de temps en temps seulement.
            var jump = glitching && Random.value < 0.3f;
            var offset = jump ? new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(-0.02f, 0.03f), 0f) : Vector3.zero;
            if (m_LeftHinge != null)
                m_LeftHinge.localPosition = m_LeftHome + offset;
            if (m_RightHinge != null)
                m_RightHinge.localPosition = m_RightHome + offset;

            // Quelques morceaux sombres, ailleurs à chaque image de la salve.
            var shown = glitching ? Random.Range(1, 5) : 0;
            for (var i = 0; i < m_Fragments.Count; i++)
                m_Fragments[i].Root.gameObject.SetActive(false);
            for (var i = 0; i < shown && m_Fragments.Count > 0; i++)
                PlaceFragment(m_Fragments[Random.Range(0, m_Fragments.Count)]);
        }

        bool AnyFragmentShown()
        {
            foreach (var fragment in m_Fragments)
            {
                if (fragment.Root != null && fragment.Root.gameObject.activeSelf)
                    return true;
            }

            return false;
        }

        // Un rectangle à un endroit au hasard de la face du battant côté voie : une bande fine ou un bloc.
        void PlaceFragment(Fragment fragment)
        {
            if (fragment.Root == null || fragment.Face == null)
                return;

            var box = fragment.Face;
            var u = (fragment.Axis + 1) % 3;
            var v = (fragment.Axis + 2) % 3;
            var size = box.size;
            var width = size[u] * Random.Range(0.15f, 0.6f);
            var height = size[v] * (Random.value < 0.6f ? Random.Range(0.02f, 0.08f) : Random.Range(0.08f, 0.25f));
            var local = box.center;
            local[u] += Random.Range(-0.5f, 0.5f) * (size[u] - width);
            local[v] += Random.Range(-0.5f, 0.5f) * (size[v] - height);
            var scale = fragment.Root.parent.lossyScale;
            local[fragment.Axis] += fragment.Sign * (size[fragment.Axis] * 0.5f + k_FragmentLift / Mathf.Max(0.001f, Mathf.Abs(scale[fragment.Axis])));

            var normal = Vector3.zero;
            normal[fragment.Axis] = fragment.Sign;
            var up = Vector3.zero;
            up[v] = 1f;
            fragment.Root.localPosition = local;
            fragment.Root.localRotation = Quaternion.LookRotation(normal, up);
            fragment.Root.localScale = new Vector3(width, height, 1f);

            m_GlitchBlock ??= new MaterialPropertyBlock();
            fragment.Renderer.GetPropertyBlock(m_GlitchBlock);
            m_GlitchBlock.SetColor(k_GlitchColorId, k_FragmentColors[Random.Range(0, k_FragmentColors.Length)]);
            fragment.Renderer.SetPropertyBlock(m_GlitchBlock);
            fragment.Root.gameObject.SetActive(true);
        }

        // Les morceaux vivent sur chaque battant (ils suivent ses sursauts), sur sa face côté voie.
        void BuildFragments()
        {
            ClearFragments();
            if (m_FragmentMaterial == null)
            {
                var shader = Shader.Find("Archery/UnlitVertexColor");
                if (shader == null)
                    return;
                m_FragmentMaterial = new Material(shader) { name = "Porte glitchée (morceaux)" };
            }

            m_FragmentMesh ??= BuildFragmentMesh();
            foreach (var hinge in new[] { m_LeftHinge, m_RightHinge })
            {
                var face = hinge != null ? hinge.GetComponentInChildren<BoxCollider>(true) : null;
                if (face == null)
                    continue;

                // La face du battant qui regarde la voie (le repère de la porte : z vers la montagne).
                var toRoad = face.transform.InverseTransformDirection(-transform.forward);
                var axis = Mathf.Abs(toRoad.x) > Mathf.Abs(toRoad.y) ? (Mathf.Abs(toRoad.x) > Mathf.Abs(toRoad.z) ? 0 : 2)
                         : (Mathf.Abs(toRoad.y) > Mathf.Abs(toRoad.z) ? 1 : 2);
                for (var i = 0; i < k_FragmentsPerLeaf; i++)
                {
                    var go = new GameObject("Morceau glitché");
                    go.hideFlags = HideFlags.DontSave;
                    go.transform.SetParent(face.transform, false);
                    go.AddComponent<MeshFilter>().sharedMesh = m_FragmentMesh;
                    var renderer = go.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = m_FragmentMaterial;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    go.SetActive(false);
                    m_Fragments.Add(new Fragment { Root = go.transform, Renderer = renderer, Face = face, Axis = axis, Sign = Mathf.Sign(toRoad[axis]) });
                }
            }
        }

        void ClearFragments()
        {
            foreach (var fragment in m_Fragments)
            {
                if (fragment.Root != null)
                    Destroy(fragment.Root.gameObject);
            }

            m_Fragments.Clear();
        }

        // Un carré de 1 × 1, visible des deux côtés, aux couleurs des sommets blanches (la couleur vient du matériau).
        static Mesh BuildFragmentMesh()
        {
            var vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f),
            };
            var mesh = new Mesh
            {
                name = "Morceau glitché",
                vertices = vertices,
                colors = new[] { Color.white, Color.white, Color.white, Color.white },
                triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 1, 2, 0, 2, 3 },
            };
            mesh.RecalculateBounds();
            return mesh;
        }

        void CheckGlitchCrossing()
        {
            if (m_GlitchCrossed || IsOpen)
                return;

            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (head == null)
                return;

            var local = transform.InverseTransformPoint(head.position);
            if (local.z < k_GlitchCrossDepth || local.z > m_Depth + 1f || Mathf.Abs(local.x) > CorridorHalfWidth || local.y > m_DoorTop + 1f)
                return;

            m_GlitchCrossed = true;
            GlitchCrossed?.Invoke();
        }

        /// <summary>Le joueur peut de nouveau traverser la porte glitchée (après un retour du toit, par exemple).</summary>
        public void ResetGlitchCrossing() => m_GlitchCrossed = false;
    }
}
