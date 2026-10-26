using System.Collections.Generic;
using Archery.Bows;
using Archery.Core;
using Archery.Player;
using UnityEngine;

namespace Archery.Upgrades
{
    /// <summary>
    /// L'arc qui raconte la partie (refonte roguelike, étape 4, 26 octobre ; <c>Docs/Refonte_Roguelike.md</c>) : les pièces de
    /// la flèche se voient, dans la couleur de chaque pièce.
    /// <list type="bullet">
    /// <item>sur chaque flèche (en main, encochée, en vol, plantée) : un losange lumineux autour de la pointe (la pièce de la
    /// pointe), trois plumes lumineuses à l'empennage (la pièce de l'empennage), trois runes autour du fût (la pièce du fût) ;
    /// plus grands à chaque niveau ;</item>
    /// <item>sur l'arc : une lueur le long de la corde (la pièce de la corde), qui bat d'or tant qu'un duo est actif
    /// (<see cref="ArrowDuos"/>, étape 5), et une breloque qui pend sous la poignée, au bout d'un fil, et se balance (le
    /// talisman) ;</item>
    /// <item>prendre une pièce joue le motif de son emplacement (<see cref="PieceSounds.Equip"/>), plus aigu à chaque niveau ;</item>
    /// <item>dans l'atelier, à côté de la boutique : les mêmes pièces sur la flèche de vitrine qui tourne (<see cref="Showcase"/>).</item>
    /// </list>
    /// La flèche n'a qu'un maillage et un matériau : les pièces sont de petits maillages ajoutés à chaque flèche, au matériau des
    /// effets (sans lumière, transparent), un matériau par couleur (rien ne casse le regroupement des rendus). Les Akimbow ont
    /// les couleurs sur leurs flèches, pas la corde ni la breloque. Créé tout seul par <see cref="SpecialArrows"/>, sur le même
    /// objet. Les couleurs sont dans <see cref="k_Colors"/> ; une pièce qui n'y est pas prend un or pâle.
    /// </summary>
    [DisallowMultipleComponent]
    public class PieceLook : MonoBehaviour
    {
        // --- Réglages ---

        // Le losange de la pointe : sa longueur (m) et son rayon au moins (sinon un peu plus de deux fois le fût).
        const float k_TipLength = 0.07f;
        const float k_TipRadius = 0.013f;

        // Les plumes de l'empennage : de tant à tant (m) depuis l'encoche, et leur largeur au-delà du fût.
        const float k_FinStart = 0.03f;
        const float k_FinEnd = 0.17f;
        const float k_FinWidth = 0.03f;

        // Les runes du fût : leur largeur (m), ce qui dépasse du fût, et le rayon du fût quand on ne peut pas le mesurer.
        const float k_RuneWidth = 0.012f;
        const float k_RuneMargin = 0.004f;
        const float k_DefaultShaftRadius = 0.006f;

        // Chaque niveau grossit la pièce de 15 % ; l'éclat (au-delà de 1, le halo des lumières vives).
        const float k_LevelScale = 0.15f;
        const float k_Glow = 1.6f;

        // La breloque du talisman : elle pend à tant (m) sous la poignée, au bout d'un fil de tant ; sa taille ; son amorti.
        const float k_CharmDrop = 0.13f;
        const float k_CharmThread = 0.07f;
        const float k_CharmSize = 0.022f;
        const float k_CharmDamping = 2.5f;

        /// <summary>La couleur de chaque pièce sur la flèche et l'arc.</summary>
        static readonly Dictionary<UpgradeEffect, Color> k_Colors = new Dictionary<UpgradeEffect, Color>
        {
            // Pointes
            { UpgradeEffect.Lightning, new Color(0.55f, 0.8f, 1f) },
            { UpgradeEffect.ChainLightning, new Color(0.4f, 0.55f, 1f) },
            { UpgradeEffect.Frost, new Color(0.6f, 0.95f, 1f) },
            { UpgradeEffect.Explosive, new Color(1f, 0.5f, 0.12f) },
            { UpgradeEffect.Piercing, new Color(0.85f, 0.88f, 0.95f) },
            { UpgradeEffect.Vampirism, new Color(0.95f, 0.15f, 0.22f) },
            { UpgradeEffect.HeadHunter, new Color(1f, 0.8f, 0.25f) },
            { UpgradeEffect.Fuse, new Color(1f, 0.38f, 0.05f) },
            { UpgradeEffect.FrostTip, new Color(0.78f, 0.95f, 1f) },
            { UpgradeEffect.Splinter, new Color(1f, 0.62f, 0.45f) },
            { UpgradeEffect.HunterMark, new Color(1f, 0.7f, 0.1f) },

            // Empennages
            { UpgradeEffect.Homing, new Color(0.4f, 1f, 0.5f) },
            { UpgradeEffect.Ricochet, new Color(1f, 0.9f, 0.3f) },
            { UpgradeEffect.Boomerang, new Color(0.6f, 1f, 0.78f) },
            { UpgradeEffect.Telekinesis, new Color(0.75f, 0.45f, 1f) },
            { UpgradeEffect.Bounce, new Color(0.55f, 1f, 0.3f) },
            { UpgradeEffect.Feather, new Color(0.95f, 0.98f, 1f) },
            { UpgradeEffect.Lead, new Color(0.5f, 0.55f, 0.68f) },
            { UpgradeEffect.Spectral, new Color(0.55f, 1f, 0.92f) },

            // Fûts
            { UpgradeEffect.Deluge, new Color(0.35f, 0.65f, 1f) },
            { UpgradeEffect.Magnet, new Color(0.82f, 0.4f, 1f) },
            { UpgradeEffect.Conductor, new Color(0.45f, 0.88f, 1f) },
            { UpgradeEffect.Spike, new Color(0.7f, 0.85f, 0.35f) },
            { UpgradeEffect.Detonator, new Color(1f, 0.3f, 0.2f) },
            { UpgradeEffect.Suspended, new Color(0.6f, 0.7f, 1f) },

            // Cordes
            { UpgradeEffect.Multishot, new Color(1f, 0.92f, 0.6f) },
            { UpgradeEffect.Echo, new Color(0.62f, 0.8f, 1f) },
            { UpgradeEffect.Triple, new Color(1f, 0.72f, 0.28f) },
            { UpgradeEffect.Trance, new Color(1f, 0.45f, 0.75f) },
            { UpgradeEffect.Falcon, new Color(0.85f, 0.65f, 0.4f) },

            // Talismans
            { UpgradeEffect.Acrobat, new Color(1f, 0.5f, 0.85f) },
            { UpgradeEffect.Executioner, new Color(0.75f, 0.1f, 0.15f) },
            { UpgradeEffect.Sniper, new Color(0.4f, 0.9f, 0.55f) },
            { UpgradeEffect.GlassPact, new Color(1f, 0.25f, 0.4f) },
            { UpgradeEffect.Engineer, new Color(1f, 0.78f, 0.3f) },
            { UpgradeEffect.Duelist, new Color(0.9f, 0.9f, 0.95f) },
        };

        // La corde bat d'or tant qu'un duo est actif (étape 5) : sa couleur, la vitesse du battement (battements par seconde).
        static readonly Color k_DuoColor = new Color(1f, 0.8f, 0.25f);
        const float k_DuoPulse = 1.2f;

        static readonly Color k_DefaultColor = new Color(1f, 0.85f, 0.55f);
        static readonly Color k_ThreadColor = new Color(0.9f, 0.85f, 0.7f, 0.7f);
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly ArrowSlot[] k_Slots = { ArrowSlot.Tip, ArrowSlot.Fletching, ArrowSlot.Shaft, ArrowSlot.String, ArrowSlot.Talisman };

        // Les pièces ajoutées à une flèche.
        class Parts
        {
            public MeshRenderer Tip;
            public MeshRenderer Fins;
            public MeshRenderer Runes;
        }

        readonly Dictionary<Arrow, Parts> m_Parts = new Dictionary<Arrow, Parts>();
        readonly Dictionary<Color, Material> m_Materials = new Dictionary<Color, Material>();
        readonly UpgradeEffect[] m_SeenEffects = new UpgradeEffect[k_Slots.Length];
        readonly int[] m_SeenLevels = new int[k_Slots.Length];
        bool m_SeenReady;
        bool m_DuoActive;
        PlayerUpgrades m_Upgrades;
        Mesh m_TipMesh;
        Mesh m_FinMesh;
        Mesh m_RuneMesh;
        Mesh m_CharmMesh;
        float m_MeshLength = -1f;
        Bow m_Bow;
        float m_NextBowSearch;
        Transform m_Charm;
        MeshRenderer m_CharmRenderer;
        LineRenderer m_Thread;
        Vector3 m_CharmPosition;
        Vector3 m_CharmVelocity;
        bool m_CharmPlaced;

        /// <summary>Celui de la scène (créé par <see cref="SpecialArrows"/>), ou null.</summary>
        public static PieceLook Instance { get; private set; }

        void OnEnable()
        {
            Instance = this;
            ArrowPool.Taken += Apply;
        }

        void OnDisable()
        {
            if (Instance == this)
                Instance = null;
            ArrowPool.Taken -= Apply;
            if (m_Upgrades != null)
                m_Upgrades.Changed -= Refresh;
            m_Upgrades = null;
        }

        void OnDestroy()
        {
            foreach (var material in m_Materials.Values)
            {
                if (material != null)
                    Destroy(material);
            }
        }

        void Update()
        {
            // Le joueur et ses améliorations peuvent arriver après : on s'abonne dès qu'ils sont là.
            if (m_Upgrades == null && PlayerUpgrades.Instance != null)
            {
                m_Upgrades = PlayerUpgrades.Instance;
                m_Upgrades.Changed += Refresh;
                Refresh();
            }
        }

        static Upgrade Piece(ArrowSlot slot)
        {
            var upgrades = PlayerUpgrades.Instance;
            return upgrades == null || upgrades.Suspended ? null : upgrades.InSlot(slot);
        }

        static int LevelOf(Upgrade piece)
        {
            var upgrades = PlayerUpgrades.Instance;
            return piece != null && upgrades != null ? Mathf.Max(1, upgrades.Stacks(piece.effect)) : 1;
        }

        /// <summary>La couleur d'une pièce sur la flèche et l'arc.</summary>
        public static Color ColorOf(Upgrade piece) =>
            piece != null && k_Colors.TryGetValue(piece.effect, out var color) ? color : k_DefaultColor;

        // Une pièce prise ou perdue : toutes les flèches, la corde et la breloque changent ; le motif de l'emplacement sonne.
        void Refresh()
        {
            var pool = ArrowPool.Instance;
            if (pool != null)
            {
                foreach (var arrow in pool.AllArrows())
                    Apply(arrow);
            }

            m_DuoActive = ArrowDuos.AnyActive();
            UpdateStringGlow();
            ApplyShowcase();
            PlayEquipSounds();
        }

        // La lueur de la corde : la couleur de la pièce de la corde ; pendant un duo, elle bat d'or (à chaque image, LateUpdate).
        void UpdateStringGlow()
        {
            var bow = FindBow();
            if (bow == null)
                return;

            var corde = Piece(ArrowSlot.String);
            var color = corde != null && EffectMaterial != null ? Glow(ColorOf(corde), 0.85f) : Color.clear;
            if (m_DuoActive && EffectMaterial != null)
            {
                var pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * k_DuoPulse * 2f * Mathf.PI);
                var gold = Glow(k_DuoColor, 0.9f);
                color = corde != null ? Color.Lerp(color, gold, pulse) : new Color(gold.r, gold.g, gold.b, Mathf.Lerp(0.3f, 0.9f, pulse));
            }

            bow.SetStringGlow(color, EffectMaterial);
        }

        void PlayEquipSounds()
        {
            var upgrades = PlayerUpgrades.Instance;
            if (upgrades == null || upgrades.Suspended)
                return;

            for (var i = 0; i < k_Slots.Length; i++)
            {
                var piece = upgrades.InSlot(k_Slots[i]);
                var effect = piece != null ? piece.effect : default;
                var level = piece != null ? upgrades.Stacks(piece.effect) : 0;
                if (m_SeenReady && piece != null && (effect != m_SeenEffects[i] || level > m_SeenLevels[i]))
                {
                    var head = PlayerRig.Instance != null ? PlayerRig.Instance.Head : null;
                    var position = head != null ? head.position : transform.position;
                    Sfx.Play(PieceSounds.Equip(k_Slots[i]), position, 0.7f, 1f + 0.12f * (level - 1), 0f);
                }

                m_SeenEffects[i] = effect;
                m_SeenLevels[i] = level;
            }

            m_SeenReady = true;
        }

        // ---------------------------------------------------------------- sur les flèches

        void Apply(Arrow arrow)
        {
            if (arrow == null)
                return;

            var tip = Piece(ArrowSlot.Tip);
            var fins = Piece(ArrowSlot.Fletching);
            var runes = Piece(ArrowSlot.Shaft);
            if (tip == null && fins == null && runes == null && !m_Parts.ContainsKey(arrow))
                return;

            var parts = PartsOf(arrow);
            SetPart(parts.Tip, tip, true);
            SetPart(parts.Fins, fins, false);
            SetPart(parts.Runes, runes, false);
        }

        // Allumée de la couleur de la pièce, plus grande à chaque niveau (la pointe partout, les plumes et les runes en largeur).
        void SetPart(MeshRenderer part, Upgrade piece, bool uniform)
        {
            if (part == null)
                return;

            var material = piece != null ? MaterialFor(Glow(ColorOf(piece), 0.9f)) : null;
            part.enabled = material != null;
            if (material == null)
                return;

            part.sharedMaterial = material;
            var scale = 1f + k_LevelScale * (LevelOf(piece) - 1);
            part.transform.localScale = uniform ? Vector3.one * scale : new Vector3(scale, scale, 1f);
        }

        Parts PartsOf(Arrow arrow)
        {
            if (m_Parts.TryGetValue(arrow, out var parts) && parts.Tip != null)
                return parts;

            parts = CreateParts(arrow.transform, arrow.Length);
            m_Parts[arrow] = parts;
            return parts;
        }

        // La pointe, les plumes et les runes, éteintes, sur une flèche (l'encoche à l'origine, la pointe vers +Z).
        Parts CreateParts(Transform root, float length)
        {
            EnsureMeshes(root, length);
            return new Parts
            {
                Tip = CreatePart(root, "Pièce : pointe", m_TipMesh, new Vector3(0f, 0f, length - k_TipLength * 0.5f)),
                Fins = CreatePart(root, "Pièce : empennage", m_FinMesh, Vector3.zero),
                Runes = CreatePart(root, "Pièce : fût", m_RuneMesh, Vector3.zero),
            };
        }

        // ---------------------------------------------------------------- la flèche de l'atelier

        // La flèche de vitrine de l'atelier (26 octobre) : une copie du modèle, sans script, qui porte les mêmes pièces.
        Parts m_Showcase;

        /// <summary>
        /// Pose les pièces sur la flèche de vitrine de l'atelier (<see cref="Shop.ArrowWorkshop"/>) : <paramref name="root"/> a
        /// l'encoche à l'origine et la pointe vers +Z, à <paramref name="length"/> m. Elles suivent ensuite celles de la flèche, à
        /// chaque pièce prise ou équipée.
        /// </summary>
        public void Showcase(Transform root, float length)
        {
            if (root == null)
                return;

            m_Showcase = CreateParts(root, length);
            foreach (var part in new[] { m_Showcase.Tip, m_Showcase.Fins, m_Showcase.Runes })
                part.gameObject.layer = root.gameObject.layer;
            ApplyShowcase();
        }

        void ApplyShowcase()
        {
            if (m_Showcase == null || m_Showcase.Tip == null)
                return;

            SetPart(m_Showcase.Tip, Piece(ArrowSlot.Tip), true);
            SetPart(m_Showcase.Fins, Piece(ArrowSlot.Fletching), false);
            SetPart(m_Showcase.Runes, Piece(ArrowSlot.Shaft), false);
        }

        static MeshRenderer CreatePart(Transform parent, string name, Mesh mesh, Vector3 localPosition)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.enabled = false;
            return renderer;
        }

        // Les maillages des pièces, faits une fois, à la longueur et à l'épaisseur de la flèche (mesurée sur son maillage).
        void EnsureMeshes(Transform arrow, float length)
        {
            if (m_TipMesh != null && Mathf.Approximately(length, m_MeshLength))
                return;

            m_MeshLength = length;
            var shaft = MeasureShaftRadius(arrow, length);
            m_TipMesh = Diamond("Pièce : pointe", k_TipLength * 0.5f, Mathf.Max(k_TipRadius, shaft * 2.2f), 6);
            m_FinMesh = Fins(k_FinStart, k_FinEnd, shaft, shaft + k_FinWidth);
            m_RuneMesh = Runes(new[] { 0.42f * length, 0.5f * length, 0.58f * length }, shaft + k_RuneMargin, k_RuneWidth);
        }

        // Le rayon du fût : les sommets du maillage de la flèche au milieu de sa longueur (si on peut les lire).
        static float MeasureShaftRadius(Transform arrow, float length)
        {
            var filter = arrow.GetComponentInChildren<MeshFilter>(true);
            var mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null || !mesh.isReadable)
                return k_DefaultShaftRadius;

            var toArrow = arrow.worldToLocalMatrix * filter.transform.localToWorldMatrix;
            var best = 0f;
            var found = false;
            foreach (var vertex in mesh.vertices)
            {
                var point = toArrow.MultiplyPoint3x4(vertex);
                if (point.z < 0.4f * length || point.z > 0.6f * length)
                    continue;

                best = Mathf.Max(best, new Vector2(point.x, point.y).magnitude);
                found = true;
            }

            return found ? Mathf.Clamp(best, 0.002f, 0.03f) : k_DefaultShaftRadius;
        }

        // Un losange allongé le long de Z, centré : la pointe vive, l'arrière plus pâle (prêté aux marques de l'œil de faucon).
        internal static Mesh Diamond(string name, float halfLength, float radius, int sides)
        {
            var vertices = new List<Vector3> { new Vector3(0f, 0f, halfLength), new Vector3(0f, 0f, -halfLength) };
            var colors = new List<Color> { Color.white, new Color(1f, 1f, 1f, 0.4f) };
            for (var i = 0; i < sides; i++)
            {
                var angle = i * Mathf.PI * 2f / sides;
                vertices.Add(new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, -halfLength * 0.25f));
                colors.Add(new Color(1f, 1f, 1f, 0.85f));
            }

            var triangles = new List<int>();
            for (var i = 0; i < sides; i++)
            {
                var a = 2 + i;
                var b = 2 + (i + 1) % sides;
                triangles.AddRange(new[] { 0, a, b, 1, b, a });
            }

            return Build(name, vertices, colors, triangles);
        }

        // Trois plumes à 120° autour du fût, de la largeur du fût à « outer », plus pâles au bord.
        static Mesh Fins(float start, float end, float inner, float outer)
        {
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            for (var fin = 0; fin < 3; fin++)
            {
                var angle = fin * Mathf.PI * 2f / 3f + Mathf.PI * 0.5f;
                var radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                var first = vertices.Count;
                vertices.Add(radial * inner + Vector3.forward * start);
                vertices.Add(radial * outer + Vector3.forward * (start + 0.02f));
                vertices.Add(radial * (outer * 0.75f) + Vector3.forward * end);
                vertices.Add(radial * inner + Vector3.forward * (end + 0.015f));
                colors.Add(new Color(1f, 1f, 1f, 0.9f));
                colors.Add(new Color(1f, 1f, 1f, 0.45f));
                colors.Add(new Color(1f, 1f, 1f, 0.45f));
                colors.Add(new Color(1f, 1f, 1f, 0.9f));
                triangles.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
            }

            return Build("Pièce : empennage", vertices, colors, triangles);
        }

        // Des anneaux autour du fût (octogones), aux distances « positions » de l'encoche.
        static Mesh Runes(float[] positions, float radius, float width)
        {
            const int sides = 8;
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            foreach (var z in positions)
            {
                var first = vertices.Count;
                for (var i = 0; i < sides; i++)
                {
                    var angle = i * Mathf.PI * 2f / sides;
                    var radial = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                    vertices.Add(radial + Vector3.forward * (z - width * 0.5f));
                    vertices.Add(radial + Vector3.forward * (z + width * 0.5f));
                    colors.Add(new Color(1f, 1f, 1f, 0.95f));
                    colors.Add(new Color(1f, 1f, 1f, 0.95f));
                }

                for (var i = 0; i < sides; i++)
                {
                    var a = first + 2 * i;
                    var b = first + 2 * ((i + 1) % sides);
                    triangles.AddRange(new[] { a, b, a + 1, b, b + 1, a + 1 });
                }
            }

            return Build("Pièce : fût", vertices, colors, triangles);
        }

        static Mesh Build(string name, List<Vector3> vertices, List<Color> colors, List<int> triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // ---------------------------------------------------------------- sur l'arc : la breloque du talisman

        // Après les déplacements de l'arc : la breloque pend sous la poignée et se balance (un pendule amorti).
        void LateUpdate()
        {
            if (m_DuoActive)
                UpdateStringGlow();

            var talisman = Piece(ArrowSlot.Talisman);
            var bow = talisman != null ? FindBow() : null;
            var grip = bow != null ? (bow.Grip != null ? bow.Grip : bow.transform) : null;
            var show = grip != null && !bow.IsReplaced && bow.gameObject.activeInHierarchy;
            if (!show)
            {
                if (m_Charm != null && m_Charm.gameObject.activeSelf)
                {
                    m_Charm.gameObject.SetActive(false);
                    m_Thread.enabled = false;
                }

                m_CharmPlaced = false;
                return;
            }

            EnsureCharm();
            var up = bow.Model != null ? bow.Model.up : bow.transform.up;
            var anchor = grip.position - up * k_CharmDrop;
            var deltaTime = Mathf.Min(Time.deltaTime, 0.05f);
            if (!m_CharmPlaced || (m_CharmPosition - anchor).sqrMagnitude > 1f)
            {
                m_CharmPosition = anchor + Vector3.down * k_CharmThread;
                m_CharmVelocity = Vector3.zero;
                m_CharmPlaced = true;
            }
            else if (deltaTime > 0f)
            {
                var previous = m_CharmPosition;
                m_CharmVelocity += Physics.gravity * deltaTime;
                m_CharmVelocity *= Mathf.Exp(-k_CharmDamping * deltaTime);
                m_CharmPosition += m_CharmVelocity * deltaTime;
                var offset = m_CharmPosition - anchor;
                m_CharmPosition = anchor + (offset.sqrMagnitude > 1e-6f ? offset.normalized : Vector3.down) * k_CharmThread;
                m_CharmVelocity = (m_CharmPosition - previous) / deltaTime;
            }

            var hang = m_CharmPosition - anchor;
            var level = LevelOf(talisman);
            m_Charm.gameObject.SetActive(true);
            m_Charm.position = m_CharmPosition + hang.normalized * (k_CharmSize * 0.6f);
            m_Charm.rotation = Quaternion.LookRotation(hang.sqrMagnitude > 1e-6f ? hang : Vector3.down) * Quaternion.Euler(0f, 0f, Time.time * 90f);
            m_Charm.localScale = Vector3.one * (1f + k_LevelScale * (level - 1));
            var material = MaterialFor(Glow(ColorOf(talisman), 1f));
            if (m_CharmRenderer.sharedMaterial != material)
                m_CharmRenderer.sharedMaterial = material;

            m_Thread.enabled = true;
            m_Thread.SetPosition(0, anchor);
            m_Thread.SetPosition(1, m_CharmPosition);
        }

        void EnsureCharm()
        {
            if (m_Charm != null)
                return;

            m_CharmMesh = Diamond("Breloque", k_CharmSize, k_CharmSize * 0.7f, 4);
            var charm = new GameObject("Breloque du talisman");
            charm.transform.SetParent(transform, false);
            charm.AddComponent<MeshFilter>().sharedMesh = m_CharmMesh;
            m_CharmRenderer = charm.AddComponent<MeshRenderer>();
            m_CharmRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_CharmRenderer.receiveShadows = false;
            m_Charm = charm.transform;

            m_Thread = new GameObject("Fil de la breloque").AddComponent<LineRenderer>();
            m_Thread.transform.SetParent(transform, false);
            m_Thread.useWorldSpace = true;
            m_Thread.positionCount = 2;
            m_Thread.widthMultiplier = 0.0025f;
            m_Thread.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_Thread.receiveShadows = false;
            m_Thread.sharedMaterial = EffectMaterial;
            m_Thread.startColor = m_Thread.endColor = k_ThreadColor;
        }

        // ---------------------------------------------------------------- outils

        static Material EffectMaterial => SpecialArrows.Instance != null ? SpecialArrows.Instance.EffectMaterial : null;

        // Une couleur vive (au-delà de 1 : le halo des lumières vives), un peu transparente.
        static Color Glow(Color color, float alpha) => new Color(color.r * k_Glow, color.g * k_Glow, color.b * k_Glow, alpha);

        // Un matériau par couleur, copié du matériau des effets.
        Material MaterialFor(Color color)
        {
            if (m_Materials.TryGetValue(color, out var material) && material != null)
                return material;

            var source = EffectMaterial;
            if (source == null)
                return null;

            material = new Material(source) { name = "Pièce " + ColorUtility.ToHtmlStringRGB(color) };
            material.SetColor(k_BaseColorId, color);
            m_Materials[color] = material;
            return material;
        }

        Bow FindBow()
        {
            if (m_Bow == null && Time.unscaledTime >= m_NextBowSearch)
            {
                m_NextBowSearch = Time.unscaledTime + 1f;
                m_Bow = FindAnyObjectByType<Bow>();
            }

            return m_Bow;
        }
    }
}
