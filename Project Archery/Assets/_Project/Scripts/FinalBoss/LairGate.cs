using System;
using Archery.Core;
using Archery.Player;
using TMPro;
using UnityEngine;

namespace Archery.FinalBoss
{
    /// <summary>
    /// La grande porte de l'antre du boss final, au bout de la voie du milieu (GDD, section 23) : un porche de pierre
    /// collé à la face de la montagne, deux battants de bois cerclés de fer, et derrière eux un couloir tout noir.
    /// Elle s'ouvre au début de la vague du boss final ; quand le joueur passe le seuil, elle se referme derrière lui
    /// (<see cref="ThresholdCrossed"/>, que le <see cref="FinalBossDirector"/> écoute).
    /// </summary>
    /// <remarks>
    /// Tout est fabriqué par ce script, dans l'éditeur comme en jeu : rien à monter, il suffit de placer l'objet
    /// (axe Z bleu vers la montagne, pieds au sol) et de régler les dimensions ci-dessous.
    /// </remarks>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [SelectionBase]
    public class LairGate : MonoBehaviour
    {
        [Header("Dimensions (m)")]
        [Tooltip("Largeur du passage (les deux battants ensemble).")]
        [SerializeField]
        float m_OpeningWidth = 6f;

        [SerializeField]
        float m_OpeningHeight = 8f;

        [Tooltip("Profondeur du porche, des battants jusqu'à la face de la montagne.")]
        [SerializeField]
        float m_Depth = 4f;

        [Tooltip("Largeur des piliers de chaque côté.")]
        [SerializeField]
        float m_PillarWidth = 2.2f;

        [Tooltip("Hauteur du linteau au-dessus du passage.")]
        [SerializeField]
        float m_LintelHeight = 3f;

        [Header("Matériaux")]
        [SerializeField]
        Material m_StoneMaterial;

        [SerializeField]
        Material m_WoodMaterial;

        [SerializeField]
        Material m_IronMaterial;

        [Tooltip("Matériau tout noir de l'intérieur (non éclairé).")]
        [SerializeField]
        Material m_DarknessMaterial;

        [Tooltip("Flammes des deux braseros, allumés quand la porte s'ouvre.")]
        [SerializeField]
        Material m_FireMaterial;

        [Header("Inscription")]
        [SerializeField]
        string m_EngravedText = "VR ARCHERY CHALLENGE";

        [SerializeField]
        Color m_EngravedColor = new Color(0.35f, 0.3f, 0.28f);

        [Tooltip("Couleur de l'inscription quand la porte s'ouvre.")]
        [SerializeField]
        Color m_EngravedGlowColor = new Color(1f, 0.3f, 0.15f);

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
        const float k_Front = -0.8f;           // les piliers dépassent un peu devant les battants
        const float k_HingeZ = 0.25f;          // charnières, un peu en retrait
        const float k_Jamb = 0.3f;             // feuillure de pierre derrière les battants, de chaque côté
        const float k_LeafThickness = 0.35f;
        const float k_LeafBottom = 0.18f;      // jour sous les battants : ils ne frottent pas la marche en s'ouvrant
        const float k_Overlap = 0.02f;         // un bloc qui en touche un autre dépasse un peu : pas de faces confondues

        GeneratedRoot m_Generated;
        Transform m_LeftHinge;
        Transform m_RightHinge;
        TMP_Text m_Engraving;
        ParticleSystem[] m_Braziers = Array.Empty<ParticleSystem>();
        Light[] m_BrazierLights = Array.Empty<Light>();
        float m_Angle;
        float m_TargetAngle;
        float m_Duration = 1f;
        bool m_Dirty;
        bool m_Crossed;

        /// <summary>Le joueur vient de passer le seuil de la porte ouverte.</summary>
        public event Action ThresholdCrossed;

        public bool IsOpen => m_TargetAngle > 0f;

        float OpenAngle => Mathf.Clamp(m_OpenAngle, 0f, 90f);

        // Demi-largeur du couloir derrière les battants (un peu moins que l'ouverture entre les piliers).
        float InnerHalfWidth => Mathf.Max(0.5f, m_OpeningWidth * 0.5f - k_Jamb);
        public bool IsMoving => !Mathf.Approximately(m_Angle, m_TargetAngle);

        /// <summary>Point au sol à l'entrée du porche, côté voie (pour les messages, et les tests sans casque).</summary>
        public Vector3 EntrancePoint => transform.TransformPoint(0f, 0f, -2f);

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
            var center = transform.TransformPoint(0f, m_OpeningHeight * 0.5f, 0f);
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
            if (local.z < m_ThresholdDepth || local.z > m_Depth + 1f || Mathf.Abs(local.x) > InnerHalfWidth || local.y > m_OpeningHeight + 1f)
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

            if (m_Engraving != null)
                m_Engraving.color = lit ? m_EngravedGlowColor : m_EngravedColor;
        }

        // ---------------------------------------------------------------- fabrication

        void Build()
        {
            if (m_Generated == null)
                return;

            var root = m_Generated.Begin();
            var halfWidth = m_OpeningWidth * 0.5f;
            var inner = InnerHalfWidth;
            var height = m_OpeningHeight;
            var pillar = m_PillarWidth;
            var top = height + m_LintelHeight;
            var depth = m_Depth;
            var front = k_Front;
            var white = Color.white;

            // Porche de pierre. Deux faces au même endroit clignotent (z-fighting) : chaque bloc qui en touche un autre
            // le dépasse donc un peu, ou s'arrête un peu avant.
            var stone = new MeshBuilder();
            foreach (var side in new[] { -1f, 1f })
            {
                // Pilier.
                AddBox(stone, side, halfWidth, halfWidth + pillar, 0f, top, front, depth);
                // Socle : déborde devant, vers l'extérieur, et un peu dans le passage (les battants ouverts passent à côté).
                AddBox(stone, side, halfWidth - 0.1f, halfWidth + pillar + 0.3f, 0f, 1.2f, front - 0.4f, depth);
                // Feuillure derrière les battants : le couloir est un peu plus étroit que l'ouverture.
                var jambFront = k_HingeZ + k_LeafThickness * 0.5f + 0.05f;
                AddBox(stone, side, inner - k_Overlap * 0.5f, halfWidth + k_Overlap, 0f, height, jambFront, depth);
            }

            // Linteau, corniche d'un seul bloc sur toute la largeur, clé de voûte sous l'inscription, marche devant.
            AddBox(stone, 1f, -halfWidth, halfWidth, height, top, front, depth);
            AddBox(stone, 1f, -(halfWidth + pillar + 0.25f), halfWidth + pillar + 0.25f, top, top + 0.8f, front - 0.45f, depth);
            AddBox(stone, 1f, -0.6f, 0.6f, height - 0.15f, height + 0.65f, front - 0.45f, front + 0.05f);
            AddBox(stone, 1f, -(halfWidth + pillar + 0.35f), halfWidth + pillar + 0.35f, 0f, 0.16f, front - 1.4f, front - 0.4f + k_Overlap * 2f);
            m_Generated.CreateMesh("Pierre", root, stone.ToMesh("Porte de l'antre (pierre)"), m_StoneMaterial, collider: true, castShadows: true);

            // Intérieur tout noir, posé juste devant la pierre (jamais au même endroit) : murs, plafond, fond et sol.
            // Le fond est en retrait de la face de la montagne, dont la roche n'est pas tout à fait plane.
            var dark = new MeshBuilder();
            var darkFront = k_HingeZ + k_LeafThickness * 0.5f + 0.05f + k_Overlap;
            var darkBack = depth - 0.6f;
            var wallInner = inner - k_Overlap * 1.5f;
            foreach (var side in new[] { -1f, 1f })
                AddBox(dark, side, wallInner, wallInner + 0.05f, 0f, height - k_Overlap, darkFront, darkBack);
            AddBox(dark, 1f, -wallInner, wallInner, height - k_Overlap * 1.5f, height - k_Overlap * 0.5f, darkFront, darkBack);
            AddBox(dark, 1f, -wallInner, wallInner, 0f, height - k_Overlap, darkBack, darkBack + 0.05f);
            AddBox(dark, 1f, -wallInner, wallInner, 0f, 0.04f, 0f, darkBack);
            m_Generated.CreateMesh("Obscurité", root, dark.ToMesh("Porte de l'antre (obscurité)"), m_DarknessMaterial, collider: false);

            // Deux braseros devant les piliers, assez loin des battants ouverts.
            var braziers = new MeshBuilder();
            var fires = new ParticleSystem[2];
            var lights = new Light[2];
            var grey = white;
            for (var i = 0; i < 2; i++)
            {
                var side = i == 0 ? -1f : 1f;
                var basePoint = new Vector3(side * (halfWidth + pillar * 0.5f + 0.2f), 0f, front - 1.9f);
                braziers.Box(basePoint + new Vector3(0f, 0.6f, 0f), new Vector3(0.25f, 0.6f, 0.25f), Quaternion.identity, grey);
                braziers.Box(basePoint + new Vector3(0f, 1.35f, 0f), new Vector3(0.55f, 0.15f, 0.55f), Quaternion.Euler(0f, 45f, 0f), grey);
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
            m_LeftHinge = BuildLeaf(root, -1f, inner, height);
            m_RightHinge = BuildLeaf(root, 1f, inner, height);

            m_Engraving = BuildEngraving(root, height, front);
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

        Transform BuildLeaf(Transform root, float side, float length, float height)
        {
            var hinge = m_Generated.Create(side < 0f ? "Battant gauche" : "Battant droit", root).transform;
            hinge.localPosition = new Vector3(side * length, k_LeafBottom, k_HingeZ);

            // Le battant s'étend de la charnière vers le centre du passage, sans toucher le linteau.
            const float thickness = k_LeafThickness;
            var leafHeight = height - k_LeafBottom - 0.05f;
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
            // Gros anneau de fer, près du centre.
            var ringCenter = new Vector3(-side * (length - 0.6f), leafHeight * 0.42f, -thickness * 0.5f - 0.12f);
            for (var i = 0; i < 8; i++)
            {
                var angle = i * Mathf.PI / 4f;
                var offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.32f;
                iron.Box(ringCenter + offset, new Vector3(0.07f, 0.07f, 0.05f), Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg), Color.white);
            }

            var leaf = m_Generated.CreateMesh("Bois", hinge, wood.ToMesh("Battant (bois)"), m_WoodMaterial, collider: false, castShadows: true);
            var box = leaf.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(-side * length * 0.5f, leafHeight * 0.5f, 0f);
            box.size = new Vector3(length, leafHeight, thickness);
            m_Generated.CreateMesh("Fer", hinge, iron.ToMesh("Battant (fer)"), m_IronMaterial, collider: false);
            return hinge;
        }

        TMP_Text BuildEngraving(Transform root, float height, float front)
        {
            if (string.IsNullOrEmpty(m_EngravedText))
                return null;

            var go = m_Generated.Create("Inscription", root);
            // Au-dessus de la clé de voûte, à peine devant la pierre du linteau.
            go.transform.localPosition = new Vector3(0f, height + 0.65f + (m_LintelHeight - 0.65f) * 0.5f, front - 0.03f);
            go.transform.localRotation = Quaternion.identity;
            var text = go.AddComponent<TextMeshPro>();
            text.text = m_EngravedText;
            text.fontSize = 12f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.characterSpacing = 8f;
            text.color = m_EngravedColor;
            text.rectTransform.sizeDelta = new Vector2(m_OpeningWidth + m_PillarWidth * 2f, Mathf.Max(0.5f, (m_LintelHeight - 0.65f) * 0.85f));
            text.enableAutoSizing = true;
            text.fontSizeMin = 4f;
            text.fontSizeMax = 14f;
            // TextMeshPro regarde vers -Z : vers la voie, comme il faut.
            return text;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.8f);
            Gizmos.DrawWireCube(new Vector3(0f, m_OpeningHeight * 0.5f, (m_ThresholdDepth + m_Depth) * 0.5f),
                                new Vector3(m_OpeningWidth, m_OpeningHeight, m_Depth - m_ThresholdDepth));
        }
    }
}
