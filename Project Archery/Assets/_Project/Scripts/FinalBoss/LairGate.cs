using System;
using System.Collections.Generic;
using Archery.Core;
using Archery.Player;
using UnityEngine;

namespace Archery.FinalBoss
{
    /// <summary>
    /// La grande porte de l'antre du boss final, au bout de la voie du milieu (GDD, section 10 bis) : un porche de pierre
    /// en arche collé à la face de la montagne, une double porte arrondie (le battant du pack Modular Dungeon de
    /// Quaternius, coupé en deux au milieu), et derrière elle un couloir tout noir. Au-dessus, le nom du jeu est gravé
    /// dans la roche, en lettres de lave (<c>LairGate.Title.cs</c>).
    /// Elle s'ouvre au début de la vague du boss final ; quand le joueur passe le seuil, elle se referme derrière lui
    /// (<see cref="ThresholdCrossed"/>, que le <see cref="FinalBossDirector"/> écoute).
    /// </summary>
    /// <remarks>
    /// Tout est fabriqué par ce script, dans l'éditeur comme en jeu : rien à monter, il suffit de placer l'objet
    /// (axe Z bleu vers la montagne, pieds au sol) et de régler les dimensions ci-dessous. La porte remplit l'ouverture :
    /// posée sur un seuil, presque au ras de la façade, elle va presque d'un pilier à l'autre, et l'arche épouse sa forme,
    /// en laissant juste la place aux battants pour s'ouvrir vers la voie sans toucher la pierre (<c>ComputeArch</c>).
    /// </remarks>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [SelectionBase]
    public partial class LairGate : MonoBehaviour
    {
        const string k_DoorResource = "Lair/Arch_Door";

        [Header("Dimensions (m)")]
        [Tooltip("Largeur du passage entre les piliers. La porte en prend toute la largeur (moins la feuillure), et sa " +
                 "hauteur suit les proportions du modèle.")]
        [SerializeField]
        float m_OpeningWidth = 6f;

        [Tooltip("Profondeur du porche, des battants jusqu'à la face de la montagne (le titre est gravé sur cette face).")]
        [SerializeField]
        float m_Depth = 4f;

        [Tooltip("Largeur des piliers de chaque côté.")]
        [SerializeField]
        float m_PillarWidth = 2.2f;

        [Tooltip("Pierre au-dessus du sommet de l'arche, sous la corniche. L'arche elle-même épouse le haut de la porte.")]
        [SerializeField]
        float m_StoneAboveArch = 0.9f;

        [Header("Porte")]
        [Tooltip("Modèle de la porte (un seul battant, coupé en deux au milieu). Vide : celui du pack Modular Dungeon " +
                 "(Resources/Lair/Arch_Door). Il faut cocher « Read/Write » dans son import.")]
        [SerializeField]
        GameObject m_DoorModel;

        [Tooltip("Étire la porte en hauteur (1 : les proportions du modèle).")]
        [Range(0.8f, 1.5f)]
        [SerializeField]
        float m_DoorStretch = 1f;

        [Tooltip("Hauteur de la porte de secours, en planches, si le modèle manque.")]
        [SerializeField]
        float m_FallbackDoorHeight = 6f;

        [Header("Matériaux")]
        [SerializeField]
        Material m_StoneMaterial;

        [Tooltip("Bois et fer de la porte de secours, et fer des braseros.")]
        [SerializeField]
        Material m_WoodMaterial;

        [SerializeField]
        Material m_IronMaterial;

        [Tooltip("Matériau tout noir de l'intérieur (non éclairé).")]
        [SerializeField]
        Material m_DarknessMaterial;

        [Tooltip("Flammes des deux braseros, allumés quand la porte s'ouvre (et braises du titre).")]
        [SerializeField]
        Material m_FireMaterial;

        [Header("Ouverture")]
        [Tooltip("Angle (°) des battants ouverts, vers l'extérieur : 90 au plus, sinon ils rentreraient dans les piliers.")]
        [Range(30f, 90f)]
        [SerializeField]
        float m_OpenAngle = 90f;

        [SerializeField]
        float m_OpenDuration = 5f;

        [SerializeField]
        float m_CloseDuration = 1.1f;

        [Tooltip("Le seuil est franchi quand la tête du joueur est à cette profondeur (m) dans le porche.")]
        [SerializeField]
        float m_ThresholdDepth = 1.6f;

        [Tooltip("Vue de l'éditeur : montrer la porte ouverte (n'a pas d'effet en jeu).")]
        [SerializeField]
        bool m_PreviewOpen;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_OpenClip;

        [SerializeField]
        AudioClip m_OpenedClip;

        [SerializeField]
        AudioClip m_CloseClip;

        [SerializeField]
        AudioClip m_SlamClip;

        // Mesures fixes du porche (m, dans le repère de la porte : Z vers la montagne).
        const float k_Front = -0.8f;           // la façade du porche
        const float k_LeafThickness = 0.35f;
        // Les battants sont presque au ras de la façade : ouverts vers la voie, ils passent devant elle, pas sous l'arche.
        const float k_HingeZ = k_Front + k_LeafThickness * 0.5f + 0.05f;
        // Entre la porte et chaque pilier : de quoi tourner (le battant ouvert garde 4 cm du pilier).
        const float k_Jamb = k_LeafThickness * 0.5f + 0.04f;
        const float k_Stop = 0.15f;            // les battants couvrent la feuillure de cette largeur tout autour
        const float k_SillTop = 0.16f;         // le seuil de pierre (et la marche devant), sous la porte
        const float k_LeafBottom = k_SillTop + 0.01f;   // la porte est posée sur le seuil, et l'effleure en tournant
        const float k_ArchClearance = 0.12f;   // l'arche passe au moins à cette hauteur au-dessus de la porte
        const float k_VoussoirJut = 0.3f;      // les claveaux avancent jusque-là devant la façade (la clé)
        const float k_Overlap = 0.02f;         // un bloc qui en touche un autre dépasse un peu : pas de faces confondues
        const int k_ArchSteps = 64;

        GeneratedRoot m_Generated;
        Transform m_LeftHinge;
        Transform m_RightHinge;
        ParticleSystem[] m_Braziers = Array.Empty<ParticleSystem>();
        Light[] m_BrazierLights = Array.Empty<Light>();
        float m_Angle;
        float m_TargetAngle;
        float m_Duration = 1f;
        bool m_Dirty;
        bool m_Crossed;
        bool m_WarnedModel;

        // Forme de la porte fermée (repère de la porte) : côtés droits jusqu'à m_DoorSide, puis un arc de cercle
        // jusqu'à m_DoorTop (étiré par m_ArcScale), ou un rectangle pour la porte de secours.
        float m_DoorSide;
        float m_DoorTop;
        bool m_DoorArched;
        float m_ArcCenterY;
        float m_ArcRadius;
        Vector2 m_ArcScale = Vector2.one;
        float m_PorchTop;

        // Dessous de l'arche du porche, de -HalfWidth à +HalfWidth (k_ArchSteps intervalles) : voir ComputeArch.
        readonly float[] m_ArchHeights = new float[k_ArchSteps + 1];

        /// <summary>Le joueur vient de passer le seuil de la porte ouverte.</summary>
        public event Action ThresholdCrossed;

        public bool IsOpen => m_TargetAngle > 0f;
        public bool IsMoving => !Mathf.Approximately(m_Angle, m_TargetAngle);

        float OpenAngle => Mathf.Clamp(m_OpenAngle, 0f, 90f);
        float HalfWidth => m_OpeningWidth * 0.5f;

        // Demi-largeur de la porte (un peu moins que l'ouverture entre les piliers), puis du couloir derrière elle.
        float InnerHalfWidth => Mathf.Max(0.5f, HalfWidth - k_Jamb);
        float CorridorHalfWidth => InnerHalfWidth - k_Stop;

        /// <summary>Point au sol à l'entrée du porche, côté voie (pour les messages, et les tests sans casque).</summary>
        public Vector3 EntrancePoint => transform.TransformPoint(0f, 0f, -2f);

        /// <summary>Demi-largeur du porche (corniche et marche comprises), dans le repère de la porte.</summary>
        public float PorchHalfWidth => HalfWidth + m_PillarWidth + 0.35f;

        /// <summary>Haut du porche (corniche comprise), dans le repère de la porte.</summary>
        public float PorchTop => m_PorchTop + 0.8f;

        /// <summary>Profondeur de la face de la montagne derrière le porche (repère de la porte).</summary>
        public float FaceDepth => m_Depth;

        void OnEnable()
        {
            m_Generated ??= new GeneratedRoot(transform, "Porte (générée)");
            Build();
        }

        void OnDisable() => m_Generated?.Clear();

        void OnValidate() => m_Dirty = true;

        /// <summary>Ouvre la porte en grand (début de la vague du boss final) et allume les braseros.</summary>
        public void Open()
        {
            m_Crossed = false;
            m_TargetAngle = OpenAngle;
            m_Duration = m_OpenDuration;
            SetFireLit(true);
            var center = transform.TransformPoint(0f, m_DoorTop * 0.5f, 0f);
            Sfx.Play(m_OpenClip, center, 1f, 0.55f, 0.4f);
            Invoke(nameof(PlayOpenedClip), m_OpenDuration * 0.85f);
        }

        /// <summary>Referme la porte, d'un coup (le joueur vient d'entrer).</summary>
        public void Close()
        {
            CancelInvoke(nameof(PlayOpenedClip));
            m_TargetAngle = 0f;
            m_Duration = m_CloseDuration;
            Sfx.Play(m_CloseClip, transform.TransformPoint(0f, 2f, 0f), 1f, 0.5f, 0.3f);
            Invoke(nameof(PlaySlamClip), m_CloseDuration * 0.95f);
        }

        void PlayOpenedClip() => Sfx.Play(m_OpenedClip, transform.TransformPoint(0f, 2f, 0f), 1f, 0.5f, 0.4f);

        void PlaySlamClip() => Sfx.Play(m_SlamClip, transform.TransformPoint(0f, 2f, 0f), 1f, 0.6f, 0.2f);

        void Update()
        {
            if (m_Dirty)
            {
                m_Dirty = false;
                Build();
            }

            if (!Application.isPlaying)
            {
                m_Angle = m_PreviewOpen ? OpenAngle : 0f;
                ApplyAngle();
                return;
            }

            if (!Mathf.Approximately(m_Angle, m_TargetAngle))
            {
                // Lente au départ, puis plus franche (ouverture) ; fermeture brutale.
                var step = OpenAngle / Mathf.Max(0.05f, m_Duration) * Time.deltaTime;
                m_Angle = Mathf.MoveTowards(m_Angle, m_TargetAngle, step);
                ApplyAngle();
            }

            CheckThreshold();
            UpdateGlitch();
            UpdateTitle();
        }

        void CheckThreshold()
        {
            if (m_Crossed || !IsOpen || m_Angle < OpenAngle * 0.5f)
                return;

            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (head == null)
                return;

            var local = transform.InverseTransformPoint(head.position);
            if (local.z < m_ThresholdDepth || local.z > m_Depth + 1f || Mathf.Abs(local.x) > CorridorHalfWidth || local.y > m_DoorTop + 1f)
                return;

            m_Crossed = true;
            ThresholdCrossed?.Invoke();
        }

        void ApplyAngle()
        {
            var eased = Mathf.SmoothStep(0f, 1f, OpenAngle > 0f ? m_Angle / OpenAngle : 0f) * OpenAngle;
            if (m_LeftHinge != null)
                m_LeftHinge.localRotation = Quaternion.Euler(0f, eased, 0f);
            if (m_RightHinge != null)
                m_RightHinge.localRotation = Quaternion.Euler(0f, -eased, 0f);
        }

        void SetFireLit(bool lit)
        {
            foreach (var fire in m_Braziers)
            {
                if (fire == null)
                    continue;
                if (lit)
                    fire.Play();
                else
                    fire.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            foreach (var light in m_BrazierLights)
            {
                if (light != null)
                    light.enabled = lit;
            }
        }

        // ---------------------------------------------------------------- forme de la porte et de l'arche

        // Haut de la porte fermée au-dessus de x (|x| ≤ demi-largeur de la porte).
        float DoorArc(float x)
        {
            if (!m_DoorArched)
                return m_DoorTop;

            var u = x / m_ArcScale.x;
            var y = m_ArcCenterY + Mathf.Sqrt(Mathf.Max(0f, m_ArcRadius * m_ArcRadius - u * u));
            return Mathf.Clamp(k_LeafBottom + y * m_ArcScale.y, m_DoorSide, m_DoorTop);
        }

        // Voûte du couloir, derrière la porte : la forme de la porte, rétrécie de la feuillure ; plate au-delà.
        float CorridorArc(float x)
        {
            var corridor = CorridorHalfWidth;
            if (Mathf.Abs(x) >= corridor)
                return m_DoorSide - k_Stop;
            return DoorArc(x * InnerHalfWidth / corridor) - k_Stop;
        }

        // Dessous de l'arche du porche (calculé par ComputeArch), entre les piliers.
        float FrontArc(float x)
        {
            var half = HalfWidth;
            var t = Mathf.Clamp01((x + half) / (2f * half)) * k_ArchSteps;
            var i = Mathf.Min(Mathf.FloorToInt(t), k_ArchSteps - 1);
            return Mathf.Lerp(m_ArchHeights[i], m_ArchHeights[i + 1], t - i);
        }

        // L'arche épouse le haut de la porte fermée (à k_ArchClearance au-dessus), mais laisse passer les battants qui
        // s'ouvrent vers la voie : près de la charnière, le haut d'un battant monte vite, et en tournant il passe sous
        // l'arche et sous les claveaux qui avancent sur la façade. On relève toutes ces hauteurs, puis on garde leur
        // enveloppe convexe : une courbe d'arche régulière, sans bosse, la plus basse possible.
        void ComputeArch()
        {
            var half = HalfWidth;
            var inner = InnerHalfWidth;
            var heights = m_ArchHeights;
            for (var i = 0; i <= k_ArchSteps; i++)
            {
                var x = -half + 2f * half * i / k_ArchSteps;
                heights[i] = (Mathf.Abs(x) <= inner ? DoorArc(x) : m_DoorSide) + k_ArchClearance;
            }

            // Un point du battant à d de la charnière, ouvert de « angle » : il est sous la pierre tant qu'il n'est pas
            // passé devant les claveaux.
            var reach = k_HingeZ - (k_Front - k_VoussoirJut) + k_LeafThickness * 0.5f;
            var spread = k_LeafThickness * 0.5f + 0.03f;
            for (var angle = 0f; angle <= OpenAngle + 0.01f; angle += 2f)
            {
                var sin = Mathf.Sin(angle * Mathf.Deg2Rad);
                var cos = Mathf.Cos(angle * Mathf.Deg2Rad);
                for (var d = 0f; d <= inner; d += 0.04f)
                {
                    if (d * sin > reach)
                        break;

                    var height = DoorArc(inner - d) + k_ArchClearance;
                    var x = inner - d * cos;
                    RaiseArch(x - spread, x + spread, height);
                    RaiseArch(-x - spread, -x + spread, height);
                }
            }

            // L'enveloppe convexe (par le haut) de ces hauteurs.
            var hull = new List<int>();
            for (var i = 0; i <= k_ArchSteps; i++)
            {
                while (hull.Count >= 2)
                {
                    var a = hull[hull.Count - 2];
                    var b = hull[hull.Count - 1];
                    // b est sous la droite de a à i : il n'est pas sur l'enveloppe.
                    var cross = (b - a) * (heights[i] - heights[a]) - (heights[b] - heights[a]) * (i - a);
                    if (cross < 0f)
                        break;
                    hull.RemoveAt(hull.Count - 1);
                }

                hull.Add(i);
            }

            for (var k = 0; k + 1 < hull.Count; k++)
            {
                var a = hull[k];
                var b = hull[k + 1];
                for (var i = a + 1; i < b; i++)
                    heights[i] = Mathf.Lerp(heights[a], heights[b], (i - a) / (float)(b - a));
            }
        }

        void RaiseArch(float xFrom, float xTo, float height)
        {
            var half = HalfWidth;
            var from = Mathf.Clamp(Mathf.FloorToInt((xFrom + half) / (2f * half) * k_ArchSteps), 0, k_ArchSteps);
            var to = Mathf.Clamp(Mathf.CeilToInt((xTo + half) / (2f * half) * k_ArchSteps), 0, k_ArchSteps);
            for (var i = from; i <= to; i++)
                m_ArchHeights[i] = Mathf.Max(m_ArchHeights[i], height);
        }

        // Les abscisses où l'on découpe les arches : celles de l'arche du porche, et plus serrées près des côtés du couloir.
        List<float> ArchSamples()
        {
            var half = HalfWidth;
            var corridor = CorridorHalfWidth;
            var xs = new List<float> { -(half + k_Overlap), half + k_Overlap, -half, half, -corridor, corridor };
            for (var i = 1; i < k_ArchSteps; i++)
                xs.Add(-half + 2f * half * i / k_ArchSteps);
            const int steps = 32;
            for (var i = 1; i < steps; i++)
                xs.Add(corridor * Mathf.Cos(Mathf.PI * i / steps));

            xs.Sort();
            for (var i = xs.Count - 1; i > 0; i--)
            {
                if (xs[i] - xs[i - 1] < 1e-3f)
                    xs.RemoveAt(i);
            }

            return xs;
        }

        // Le modèle de porte, coupé en deux battants (repère de la porte : x au milieu, pieds à 0, épaisseur centrée).
        bool PrepareDoor(out Mesh left, out Mesh right, out Material[] materials)
        {
            left = null;
            right = null;
            materials = null;
            m_DoorArched = false;
            m_DoorTop = m_DoorSide = Mathf.Max(2f, m_FallbackDoorHeight);

            var model = m_DoorModel != null ? m_DoorModel : Resources.Load<GameObject>(k_DoorResource);
            var filter = model != null ? model.GetComponentInChildren<MeshFilter>(true) : null;
            var mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null || !mesh.isReadable)
            {
                if (!m_WarnedModel)
                {
                    m_WarnedModel = true;
                    Debug.LogWarning(mesh == null
                        ? $"LairGate : modèle de porte introuvable (Resources/{k_DoorResource}) : porte en planches."
                        : $"LairGate : coche « Read/Write » dans l'import du modèle {model.name} pour le couper en deux battants.", this);
                }

                return false;
            }

            // Le repère du modèle (rotation et échelle de l'import), puis ses bornes.
            var asset = filter.transform.localToWorldMatrix;
            var vertices = mesh.vertices;
            var min = Vector3.positiveInfinity;
            var max = Vector3.negativeInfinity;
            foreach (var vertex in vertices)
            {
                var p = asset.MultiplyPoint3x4(vertex);
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }

            var size = max - min;
            if (size.x < 1e-3f || size.y < 1e-3f || size.z < 1e-3f)
                return false;

            // Toute la largeur de la porte, la hauteur dans les proportions du modèle, l'épaisseur des anciens battants
            // (plus fins que le modèle agrandi : ouverts, ils longent les piliers sans y entrer).
            var scale = new Vector3(2f * InnerHalfWidth / size.x, 0f, k_LeafThickness / size.z);
            scale.y = scale.x * m_DoorStretch;
            var toDoor = Matrix4x4.Scale(scale) * Matrix4x4.Translate(new Vector3(-(min.x + max.x) * 0.5f, -min.y, -(min.z + max.z) * 0.5f)) * asset;

            // L'arc du haut : un arc de cercle entre la hauteur des côtés et le sommet (repère du modèle).
            var halfModel = size.x * 0.5f;
            var side = 0f;
            foreach (var vertex in vertices)
            {
                var p = asset.MultiplyPoint3x4(vertex);
                if (Mathf.Abs(p.x - (min.x + max.x) * 0.5f) > halfModel * 0.985f)
                    side = Mathf.Max(side, p.y - min.y);
            }

            var rise = size.y - side;
            m_DoorTop = k_LeafBottom + size.y * scale.y;
            m_DoorSide = k_LeafBottom + side * scale.y;
            m_DoorArched = rise > size.y * 0.02f && side > 0f;
            if (m_DoorArched)
            {
                m_ArcRadius = (halfModel * halfModel + rise * rise) / (2f * rise);
                m_ArcCenterY = size.y - m_ArcRadius;
                m_ArcScale = new Vector2(scale.x, scale.y);
            }
            else
            {
                m_DoorSide = m_DoorTop;
            }

            if (!MeshCut.SplitAtX(mesh, toDoor, out left, out right))
                return false;

            var renderer = filter.GetComponent<MeshRenderer>();
            materials = renderer != null ? renderer.sharedMaterials : new[] { m_WoodMaterial };
            return true;
        }

        // ---------------------------------------------------------------- fabrication

        void Build()
        {
            if (m_Generated == null)
                return;

            var root = m_Generated.Begin();
            var hasModel = PrepareDoor(out var leftLeaf, out var rightLeaf, out var doorMaterials);

            var halfWidth = HalfWidth;
            var inner = InnerHalfWidth;
            var corridor = CorridorHalfWidth;
            var corridorSide = m_DoorSide - k_Stop;
            var pillar = m_PillarWidth;
            ComputeArch();
            var apex = 0f;
            foreach (var height in m_ArchHeights)
                apex = Mathf.Max(apex, height);
            var top = apex + Mathf.Max(0.3f, m_StoneAboveArch);
            var depth = m_Depth;
            var front = k_Front;
            var jambFront = k_HingeZ + k_LeafThickness * 0.5f + 0.05f;
            var xs = ArchSamples();
            m_PorchTop = top;

            // Porche de pierre. Deux faces au même endroit clignotent (z-fighting) : chaque bloc qui en touche un autre
            // le dépasse donc un peu, ou s'arrête un peu avant.
            var stone = new MeshBuilder();
            foreach (var side in new[] { -1f, 1f })
            {
                // Pilier.
                AddBox(stone, side, halfWidth, halfWidth + pillar, 0f, top, front, depth);
                // Socle : déborde devant et vers l'extérieur, à peine dans le passage (les battants ouverts passent à côté).
                AddBox(stone, side, halfWidth - 0.015f, halfWidth + pillar + 0.3f, 0f, 1.2f, front - 0.4f, depth);
                // Feuillure derrière les battants, sur les côtés : le couloir est un peu plus étroit que la porte.
                AddBox(stone, side, corridor - k_Overlap * 0.5f, halfWidth + k_Overlap, 0f, corridorSide, jambFront, depth);
            }

            // L'arche du porche (au-dessus de la porte et de ses battants ouverts), et derrière les battants, la
            // feuillure en arc qui suit la porte : elle remplit tout ce qu'il y a entre la porte et l'arche.
            AddArch(stone, xs, -halfWidth, halfWidth, FrontArc, x => top, front, depth);
            AddArch(stone, xs, -(halfWidth + k_Overlap), halfWidth + k_Overlap, CorridorArc, x => FrontArc(x) + 0.05f, jambFront, depth);

            // Corniche d'un seul bloc sur toute la largeur. Devant, une marche qui continue sous la porte : le seuil, sur
            // lequel elle est posée ; les battants l'effleurent en s'ouvrant.
            AddBox(stone, 1f, -(halfWidth + pillar + 0.25f), halfWidth + pillar + 0.25f, top, top + 0.8f, front - 0.45f, depth);
            AddBox(stone, 1f, -(halfWidth + pillar + 0.35f), halfWidth + pillar + 0.35f, 0f, k_SillTop, front - 1.4f, jambFront + 0.3f);
            AddVoussoirs(stone, front);
            m_Generated.CreateMesh("Pierre", root, stone.ToMesh("Porte de l'antre (pierre)"), m_StoneMaterial, collider: true, castShadows: true);

            // Intérieur tout noir, posé juste devant la pierre (jamais au même endroit) : murs, voûte, fond et sol.
            // Le fond est en retrait de la face de la montagne, dont la roche n'est pas tout à fait plane.
            var dark = new MeshBuilder();
            var darkFront = jambFront + k_Overlap;
            var darkBack = depth - 0.6f;
            var wallInner = corridor - k_Overlap * 1.5f;
            foreach (var side in new[] { -1f, 1f })
                AddBox(dark, side, wallInner, wallInner + 0.05f, 0f, corridorSide - k_Overlap, darkFront, darkBack);
            AddArch(dark, xs, -wallInner, wallInner, x => CorridorArc(x) - k_Overlap * 1.5f - 0.05f, x => CorridorArc(x) - k_Overlap * 1.5f, darkFront, darkBack);
            AddArch(dark, xs, -wallInner, wallInner, x => 0f, x => CorridorArc(x) - k_Overlap, darkBack, darkBack + 0.05f);
            AddBox(dark, 1f, -wallInner, wallInner, 0f, 0.04f, jambFront + 0.25f, darkBack);
            m_Generated.CreateMesh("Obscurité", root, dark.ToMesh("Porte de l'antre (obscurité)"), m_DarknessMaterial, collider: false);

            // Deux braseros devant les piliers, assez loin des battants ouverts.
            var braziers = new MeshBuilder();
            var fires = new ParticleSystem[2];
            var lights = new Light[2];
            for (var i = 0; i < 2; i++)
            {
                var side = i == 0 ? -1f : 1f;
                var basePoint = new Vector3(side * (halfWidth + pillar * 0.5f + 0.2f), 0f, front - 1.9f);
                braziers.Box(basePoint + new Vector3(0f, 0.6f, 0f), new Vector3(0.25f, 0.6f, 0.25f), Quaternion.identity, Color.white);
                braziers.Box(basePoint + new Vector3(0f, 1.35f, 0f), new Vector3(0.55f, 0.15f, 0.55f), Quaternion.Euler(0f, 45f, 0f), Color.white);
                fires[i] = FireEffects.CreateBrazierFire(root, basePoint + new Vector3(0f, 1.5f, 0f), m_FireMaterial, 1.4f);

                var lightObject = m_Generated.Create("Lumière du brasero", root);
                lightObject.transform.localPosition = basePoint + new Vector3(0f, 2.3f, -0.3f);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.5f, 0.2f);
                light.range = 12f;
                light.intensity = 2.5f;
                light.shadows = LightShadows.None;
                lights[i] = light;
            }

            m_Generated.CreateMesh("Braseros", root, braziers.ToMesh("Porte de l'antre (braseros)"), m_IronMaterial, collider: true);
            m_Braziers = fires;
            m_BrazierLights = lights;

            // Les deux battants, chacun sur sa charnière, contre la feuillure : ouverts, ils longent les piliers sans y entrer.
            if (hasModel)
            {
                m_LeftHinge = BuildModelLeaf(root, -1f, inner, leftLeaf, doorMaterials);
                m_RightHinge = BuildModelLeaf(root, 1f, inner, rightLeaf, doorMaterials);
            }
            else
            {
                m_LeftHinge = BuildLeaf(root, -1f, inner, m_DoorTop);
                m_RightHinge = BuildLeaf(root, 1f, inner, m_DoorTop);
            }

            BuildTitle(root);
            SetFireLit(Application.isPlaying ? IsOpen : m_PreviewOpen);
            ApplyAngle();
        }

        // Une boîte de x (de part et d'autre selon le côté), y et z donnés par leurs bornes.
        static void AddBox(MeshBuilder builder, float side, float xFrom, float xTo, float yFrom, float yTo, float zFrom, float zTo)
        {
            var center = new Vector3(side * (xFrom + xTo) * 0.5f, (yFrom + yTo) * 0.5f, (zFrom + zTo) * 0.5f);
            var half = new Vector3(Mathf.Abs(xTo - xFrom) * 0.5f, Mathf.Abs(yTo - yFrom) * 0.5f, Mathf.Abs(zTo - zFrom) * 0.5f);
            builder.Box(center, half, Quaternion.identity, Color.white);
        }

        // Un bloc plein entre deux courbes (dessous et dessus, fonctions de x), de z0 à z1, découpé aux abscisses xs.
        static void AddArch(MeshBuilder builder, List<float> xs, float xFrom, float xTo, Func<float, float> bottom, Func<float, float> topOf, float z0, float z1)
        {
            var samples = new List<float> { xFrom };
            foreach (var x in xs)
            {
                if (x > xFrom + 1e-3f && x < xTo - 1e-3f)
                    samples.Add(x);
            }

            samples.Add(xTo);

            if (samples.Count < 2)
                return;

            for (var i = 0; i + 1 < samples.Count; i++)
            {
                var xa = samples[i];
                var xb = samples[i + 1];
                var ba = bottom(xa);
                var bb = bottom(xb);
                var ta = Mathf.Max(ba, topOf(xa));
                var tb = Mathf.Max(bb, topOf(xb));
                if (ta - ba < 1e-4f && tb - bb < 1e-4f)
                    continue;

                Face(builder, new Vector3(xa, ba, z0), new Vector3(xa, ta, z0), new Vector3(xb, tb, z0), new Vector3(xb, bb, z0), Vector3.back);
                Face(builder, new Vector3(xa, ba, z1), new Vector3(xa, ta, z1), new Vector3(xb, tb, z1), new Vector3(xb, bb, z1), Vector3.forward);
                Face(builder, new Vector3(xa, ba, z0), new Vector3(xb, bb, z0), new Vector3(xb, bb, z1), new Vector3(xa, ba, z1), new Vector3(bb - ba, -(xb - xa), 0f));
                Face(builder, new Vector3(xa, ta, z0), new Vector3(xb, tb, z0), new Vector3(xb, tb, z1), new Vector3(xa, ta, z1), new Vector3(-(tb - ta), xb - xa, 0f));
            }

            // Les deux bouts.
            var first = samples[0];
            var last = samples[samples.Count - 1];
            Face(builder, new Vector3(first, bottom(first), z0), new Vector3(first, topOf(first), z0), new Vector3(first, topOf(first), z1),
                 new Vector3(first, bottom(first), z1), Vector3.left);
            Face(builder, new Vector3(last, bottom(last), z0), new Vector3(last, topOf(last), z0), new Vector3(last, topOf(last), z1),
                 new Vector3(last, bottom(last), z1), Vector3.right);
        }

        // Un quadrilatère tourné vers l'extérieur indiqué, quel que soit l'ordre de ses coins.
        static void Face(MeshBuilder builder, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
        {
            var normal = Vector3.Cross(b - a, c - a);
            if (normal.sqrMagnitude < 1e-10f)
                normal = Vector3.Cross(c - a, d - a);
            if (Vector3.Dot(normal, outward) < 0f)
                builder.Quad(a, d, c, b, Color.white);
            else
                builder.Quad(a, b, c, d, Color.white);
        }

        // Les claveaux de l'arche, en saillie sur la façade, avec une grosse clé de voûte au sommet : treize pierres de même
        // longueur le long de la courbe de l'arche, d'un pilier à l'autre, chacune tournée vers l'extérieur de la courbe.
        void AddVoussoirs(MeshBuilder builder, float front)
        {
            const int stones = 13;
            var half = HalfWidth;
            var points = new List<Vector3>();
            var lengths = new List<float> { 0f };
            for (var i = 0; i <= k_ArchSteps; i++)
            {
                var x = -half + 2f * half * i / k_ArchSteps;
                points.Add(new Vector3(x, m_ArchHeights[i], 0f));
                if (i > 0)
                    lengths.Add(lengths[i - 1] + Vector3.Distance(points[i - 1], points[i]));
            }

            var total = lengths[lengths.Count - 1];
            Vector3 At(float s)
            {
                var i = 1;
                while (i < lengths.Count - 1 && lengths[i] < s)
                    i++;
                var span = Mathf.Max(1e-5f, lengths[i] - lengths[i - 1]);
                return Vector3.Lerp(points[i - 1], points[i], (s - lengths[i - 1]) / span);
            }

            for (var i = 0; i < stones; i++)
            {
                var p0 = At(total * i / stones);
                var p1 = At(total * (i + 1) / stones);
                var key = i == stones / 2;
                var tangent = (p1 - p0).normalized;
                var normal = new Vector3(-tangent.y, tangent.x, 0f);
                var length = Vector3.Distance(p0, p1) - 0.05f + (key ? 0.15f : 0f);
                var radial = key ? 1.05f : 0.75f;
                var jut = key ? k_VoussoirJut : 0.18f;
                var center = At(total * (i + 0.5f) / stones) + normal * (radial * 0.5f);
                center.z = front + (0.25f - jut) * 0.5f;
                builder.Box(center, new Vector3(length * 0.5f, radial * 0.5f, (0.25f + jut) * 0.5f), Quaternion.LookRotation(Vector3.forward, normal), Color.white);
            }
        }

        // Un battant du modèle : la moitié du maillage, sur sa charnière (le côté de la porte contre le pilier).
        Transform BuildModelLeaf(Transform root, float side, float length, Mesh half, Material[] materials)
        {
            var hinge = m_Generated.Create(side < 0f ? "Battant gauche" : "Battant droit", root).transform;
            hinge.localPosition = new Vector3(side * length, k_LeafBottom, k_HingeZ);

            var leaf = m_Generated.CreateMesh("Porte", hinge, half, materials.Length > 0 ? materials[0] : m_WoodMaterial, collider: false, castShadows: true);
            leaf.sharedMaterials = materials;
            leaf.receiveShadows = true;
            leaf.transform.localPosition = new Vector3(-side * length, 0f, 0f);
            var box = leaf.gameObject.AddComponent<BoxCollider>();
            box.center = half.bounds.center;
            box.size = half.bounds.size;
            return hinge;
        }

        // Porte de secours (sans le modèle) : un battant de planches cerclé de fer.
        Transform BuildLeaf(Transform root, float side, float length, float height)
        {
            var hinge = m_Generated.Create(side < 0f ? "Battant gauche" : "Battant droit", root).transform;
            hinge.localPosition = new Vector3(side * length, k_LeafBottom, k_HingeZ);

            // Le battant s'étend de la charnière vers le centre du passage.
            const float thickness = k_LeafThickness;
            var leafHeight = height - k_LeafBottom;
            var wood = new MeshBuilder();
            var planks = 5;
            var plankWidth = length / planks;
            for (var i = 0; i < planks; i++)
            {
                var x = -side * (i + 0.5f) * plankWidth;
                var plankHeight = leafHeight - (i % 2) * 0.15f;
                wood.Box(new Vector3(x, plankHeight * 0.5f, 0f), new Vector3(plankWidth * 0.48f, plankHeight * 0.5f, thickness * 0.5f), Quaternion.identity, Color.white);
            }

            var iron = new MeshBuilder();
            foreach (var y in new[] { 1.3f, leafHeight * 0.5f, leafHeight - 1.2f })
                iron.Box(new Vector3(-side * length * 0.5f, y, -thickness * 0.5f - 0.04f), new Vector3(length * 0.5f - 0.05f, 0.12f, 0.05f), Quaternion.identity, Color.white);

            var leaf = m_Generated.CreateMesh("Bois", hinge, wood.ToMesh("Battant (bois)"), m_WoodMaterial, collider: false, castShadows: true);
            var box = leaf.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(-side * length * 0.5f, leafHeight * 0.5f, 0f);
            box.size = new Vector3(length, leafHeight, thickness);
            m_Generated.CreateMesh("Fer", hinge, iron.ToMesh("Battant (fer)"), m_IronMaterial, collider: false);
            return hinge;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.8f);
            Gizmos.DrawWireCube(new Vector3(0f, m_DoorTop * 0.5f, (m_ThresholdDepth + m_Depth) * 0.5f),
                                new Vector3(CorridorHalfWidth * 2f, m_DoorTop, m_Depth - m_ThresholdDepth));
            DrawTitleGizmo();
        }
    }
}
