using System.Collections.Generic;
using Archery.BowClasses;
using Archery.Bows;
using Archery.Core;
using Archery.Enemies;
using Archery.Player;
using Archery.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Combat
{
    /// <summary>
    /// Le katana et le gantelet, une arme du joueur (à la place de l'arc ; carte du menu des armes, débloquée par « Rōnin »),
    /// refaite le 22 octobre (avant : un katana et un bouclier). Le katana flotte à côté du joueur et se prend avec la
    /// poignée ; le gantelet est porté sur la main de l'arc. Un symbole style kanji (2 ou 3 traits,
    /// <see cref="KanjiGlyphs"/>) apparaît devant le joueur : il trace chaque trait d'un geste rapide de la lame, qui fend
    /// la matrice le long de son geste (<see cref="MatrixSign"/>). Le signe complet prend la qualité du tracé (précision des
    /// traits : vert, vert clair, orange ou rouge) ; touché avec le gantelet, il part vers l'ennemi le plus proche du centre
    /// de la vue, le suit, et tout le signe touche. Puis un autre symbole apparaît. Le katana frappe aussi au corps à corps
    /// et coupe les projectiles.
    /// </summary>
    /// <remarks>
    /// Créé par l'<see cref="Bow"/> quand sa définition est <see cref="BowDefinition.katana"/> : l'arc est caché, et ses
    /// multiplicateurs (améliorations, coffres) servent ici (dégâts, vitesse, largeur des qualités) ; le signe lancé compte
    /// comme un tir pour le score et les succès. Rangés avec lui (construction, dimensions). Sans casque : le symbole devant
    /// le viseur ; clic gauche maintenu en balayant un trait avec le viseur, puis relâché ; clic droit pour lancer le signe.
    /// Raccourci de test : <b>O</b> (éditeur et builds Development) équipe le katana et le gantelet, même verrouillés, et les rend.
    /// </remarks>
    public class KatanaGauntlet : MonoBehaviour, IMeleeSource
    {
        // Le katana, dans son repère (le milieu de la poignée à l'origine, la lame vers +Y), si le modèle ne donne pas ses
        // repères « Garde » et « Pointe ».
        static readonly Vector3 k_DefaultBladeStart = new Vector3(0f, 0.15f, 0f);
        static readonly Vector3 k_DefaultBladeTip = new Vector3(0f, 0.85f, 0f);

        // En main : la poignée dans la paume, la lame le long du poing (la poignée des manettes Quest est inclinée de 45°).
        static readonly Quaternion k_KatanaAttach = Quaternion.Euler(-45f, 0f, 0f);
        static readonly Vector3 k_KatanaBoxCenter = new Vector3(0f, 0.36f, 0f);
        static readonly Vector3 k_KatanaBoxSize = new Vector3(0.07f, 1f, 0.07f);

        // Rangé : à hauteur de hanche, du côté de la main qui tire, poignée en haut, lame vers le bas et l'arrière.
        static readonly Vector3 k_KatanaHolster = new Vector3(0.32f, -0.42f, 0.2f);

        // Le gantelet : du poignet au bout du majeur (m) dans son modèle ; sa paume, s'il ne la donne pas ; il lance un signe
        // prêt en passant à moins de ce rayon (m) de ses fissures.
        const float k_GauntletLength = 0.19f;
        static readonly Vector3 k_DefaultPalm = new Vector3(0f, -0.02f, 0.07f);
        const float k_LaunchReach = 0.18f;

        // Prêt, le signe vient à portée du gantelet (23 octobre : à 95 cm, on ne pouvait plus le toucher) : à 50 cm devant
        // les yeux, 22 cm plus bas, 12 cm du côté de la main de l'arc, réduit à 60 % pour ne pas boucher la vue.
        static readonly Vector3 k_PresentOffset = new Vector3(0.12f, -0.22f, 0.5f);
        const float k_PresentScale = 0.6f;
        const float k_PresentSpeed = 8f;

        // Le coup : dès que la pointe va à cette vitesse (m/s) ; dégâts ×1 à ×1,6 jusqu'à 7 m/s ; un ennemi n'est touché
        // qu'une fois par délai (s).
        const float k_HitSpeed = 2.5f;
        const float k_HitFastSpeed = 7f;
        const float k_HitCooldown = 0.4f;
        const float k_BladeRadius = 0.035f;
        const float k_ProjectileReach = 0.12f;

        // Le symbole : à cette distance (m) devant les yeux, un peu plus bas (0,7 et 0,18 jusqu'au 23 octobre, il était trop
        // près) ; il rejoint le regard quand il s'en écarte de plus de 20° ou 25 cm (tant qu'aucun trait n'est commencé).
        const float k_GuideDistance = 0.95f;
        const float k_GuideDrop = 0.12f;
        const float k_GuideFollowAngle = 20f;
        const float k_GuideFollowDistance = 0.25f;
        const float k_GuideFollowSpeed = 6f;
        const float k_DesktopGuideDistance = 1f;
        const float k_DesktopFollowAngle = 35f;

        // Un geste : il commence quand la pointe (ramenée dans le plan du symbole) va à 1,4 m/s, la pointe entre 35 cm
        // derrière le plan et 70 cm devant ; il s'arrête quand elle ralentit, qu'elle s'éloigne du plan ou sort du symbole,
        // ou au bout d'une seconde.
        const float k_StartSpeed = 1.4f;
        const float k_EndSpeed = 0.6f;
        const float k_EndHold = 0.05f;
        const float k_TipBehind = 0.35f;
        const float k_TipAhead = 0.7f;
        const float k_MaxGestureTime = 1f;
        const float k_PointSpacing = 0.008f;

        // La précision d'un trait : la distance moyenne (en demi-côtés du symbole, 38 cm) entre les points du trait et le
        // geste, plus l'écart d'angle entre eux là où le geste passe (un coup en travers ne compte pas) : parfait sous 0,13
        // (5 cm), très bon sous 0,21, bon sous 0,32 ; au-delà de 0,45, le geste ne compte pas. (Jusqu'au 23 octobre, le geste
        // était comparé point par point au trait : un coup de sabre qui dépassait des bouts du trait était refusé, et presque
        // aucun ne passait.)
        const int k_CoverSamples = 16;
        const float k_MinGestureLength = 0.3f;
        const float k_AngleWeight = 0.25f;
        const float k_Perfect = 0.13f;
        const float k_Held = 0.21f;
        const float k_Good = 0.32f;
        const float k_Reject = 0.45f;

        // Un signe prêt attend 12 s ; un signe commencé s'abandonne après 7 s sans nouveau trait ; le symbole suivant arrive
        // 0,35 s après un lancer.
        const float k_ReadyLife = 12f;
        const float k_PartialTimeout = 7f;
        const float k_NextGlyphDelay = 0.35f;

        // La cible : l'ennemi le plus proche du centre de la vue (à 70° au plus, sinon 120°), à 90 m au plus.
        const float k_TargetRange = 90f;
        const float k_TargetAngle = 70f;
        const float k_TargetAngleWide = 120f;
        const float k_MinSignSpeed = 18f;

        // Sans casque : le katana devant la caméra.
        static readonly Vector3 k_DesktopKatana = new Vector3(0.16f, -0.26f, 0.32f);

        static readonly int k_GlitchId = Shader.PropertyToID("_Glitch");
        static readonly int k_EmissionId = Shader.PropertyToID("_EmissionColor");
        static readonly Collider[] s_Overlaps = new Collider[32];
        static readonly RaycastHit[] s_Hits = new RaycastHit[16];

        readonly Dictionary<Health, float> m_LastHit = new Dictionary<Health, float>();
        readonly List<EnemyProjectile> m_Projectiles = new List<EnemyProjectile>();
        readonly List<Vector3> m_Gesture = new List<Vector3>();
        readonly List<Vector2> m_Gesture2D = new List<Vector2>();
        readonly List<Vector2> m_SampleB = new List<Vector2>();
        readonly List<Vector3> m_World = new List<Vector3>();
        readonly List<float> m_Errors = new List<float>();
        readonly List<(Renderer Renderer, Color Color)> m_GauntletGlow = new List<(Renderer, Color)>();

        Bow m_Bow;
        BowDefinition m_Definition;
        FloatingGrab m_Katana;
        Transform m_BladeStart;
        Transform m_BladeTip;
        Renderer[] m_KatanaRenderers = System.Array.Empty<Renderer>();
        MaterialPropertyBlock m_Block;
        MaterialPropertyBlock m_GlowBlock;
        GameObject m_Gauntlet;
        Transform m_Palm;
        GlovedHand m_GauntletGlove;
        IXRInteractor m_GauntletHand;
        float m_NextGauntletTry;
        float m_KatanaSide = 1f;
        Material m_FallbackGlyph;
        Material m_FallbackTear;

        KanjiGuide m_Guide;
        MatrixSign m_Sign;
        ShotGrade m_SignGrade;
        int m_Glyph = -1;
        int m_LastGlyph = -1;
        float m_NextGlyphTime;
        float m_LastProgress;
        float m_ReadyTime;
        bool m_GuideFollowing;
        bool m_GuideSnap = true;

        bool m_Gesturing;
        float m_GestureStart;
        float m_SlowTime;
        Vector3 m_PreviousPoint;
        bool m_HasPoint;
        Vector3 m_PreviousTip;
        bool m_HasPrevious;

        bool m_Built;
        bool m_Desktop;
        bool m_Hidden;
        float m_Glitch;

        /// <summary>Le katana et le gantelet de l'arc (sa définition, ses multiplicateurs, ses qualités de tir).</summary>
        public static KatanaGauntlet Create(Bow bow, BowDefinition definition)
        {
            var weapon = new GameObject("Katana et gantelet").AddComponent<KatanaGauntlet>();
            weapon.m_Bow = bow;
            weapon.m_Definition = definition;
            return weapon;
        }

        void OnEnable() => EnemyProjectile.Launched += OnProjectileLaunched;

        void OnDisable() => EnemyProjectile.Launched -= OnProjectileLaunched;

        void OnDestroy()
        {
            if (m_Katana != null)
                Destroy(m_Katana.gameObject);
            if (m_Guide != null)
                Destroy(m_Guide.gameObject);
            RemoveGauntlet();

            // Un signe en vol finit sa course ; celui qui n'est pas parti se brise.
            if (m_Sign != null && !m_Sign.IsFlying)
                m_Sign.Break();
            if (m_FallbackGlyph != null)
                Destroy(m_FallbackGlyph);
            if (m_FallbackTear != null)
                Destroy(m_FallbackTear);
        }

        void OnProjectileLaunched(EnemyProjectile projectile)
        {
            if (projectile != null)
                m_Projectiles.Add(projectile);
        }

        bool KatanaActive => m_Desktop || (m_Katana != null && m_Katana.Held);
        IXRInteractor KatanaHand => m_Desktop || m_Katana == null ? null : m_Katana.Holder;
        float DamageMultiplier => m_Bow != null ? m_Bow.DamageMultiplier : 1f;
        ShotTuning Tuning => m_Bow != null && m_Bow.ShotTuning != null ? m_Bow.ShotTuning : ShotTuning.Fallback;
        Vector3 BladeStart => m_BladeStart != null ? m_BladeStart.position : m_Katana.transform.TransformPoint(k_DefaultBladeStart);
        Vector3 BladeTip => m_BladeTip != null ? m_BladeTip.position : m_Katana.transform.TransformPoint(k_DefaultBladeTip);

        Material GlyphMaterial
        {
            get
            {
                if (m_Definition != null && m_Definition.katanaGlyphMaterial != null)
                    return m_Definition.katanaGlyphMaterial;
                if (m_FallbackGlyph == null)
                    m_FallbackGlyph = new Material(Shader.Find("Archery/KanjiBrush")) { name = "Symbole (secours)" };
                return m_FallbackGlyph;
            }
        }

        Material TearMaterial
        {
            get
            {
                if (m_Definition != null && m_Definition.katanaTearMaterial != null)
                    return m_Definition.katanaTearMaterial;
                if (m_FallbackTear == null)
                    m_FallbackTear = new Material(Shader.Find("Archery/MatrixTear")) { name = "Fissure (secours)" };
                return m_FallbackTear;
            }
        }

        // ---------------------------------------------------------------- construction

        bool TryBuild()
        {
            var rig = PlayerRig.Instance;
            if (rig == null || rig.Head == null || m_Definition == null)
                return false;

            // Le gantelet sur la main de l'arc, le katana de l'autre côté.
            var gauntletSide = rig.BowHand == InteractorHandedness.Right ? 1f : -1f;
            m_KatanaSide = -gauntletSide;

            var katana = new GameObject("Katana").transform;
            katana.SetParent(transform, false);
            AddModel(katana, m_Definition.katanaModel);
            m_BladeStart = FindChild(katana, "Garde");
            m_BladeTip = FindChild(katana, "Pointe");
            m_Katana = FloatingGrab.Create(katana.gameObject, Vector3.zero, k_KatanaAttach, k_KatanaBoxCenter, k_KatanaBoxSize,
                                           Vector3.Scale(k_KatanaHolster, new Vector3(m_KatanaSide, 1f, 1f)),
                                           Quaternion.Euler(-140f, 0f, -12f * m_KatanaSide));
            m_Katana.Grabbed += interactor =>
            {
                m_HasPrevious = false;
                m_HasPoint = false;
            };
            m_Katana.SnapToHolster();
            m_KatanaRenderers = katana.GetComponentsInChildren<Renderer>(true);
            m_Block = new MaterialPropertyBlock();

            m_Guide = KanjiGuide.Create(transform, GlyphMaterial);
            ParentGuide();
            NewGlyph();
            AttachGauntlet();
            return true;
        }

        // Le modèle de la définition (sans ses colliders), ou des formes simples s'il manque.
        static void AddModel(Transform root, GameObject model)
        {
            if (model != null)
            {
                var instance = Instantiate(model, root, false);
                instance.name = model.name;
                foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
                    DestroyImmediate(collider);
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                return;
            }

            Box(root, new Vector3(0f, 0f, 0f), new Vector3(0.032f, 0.24f, 0.032f), new Color(0.08f, 0.06f, 0.06f));
            Box(root, new Vector3(0f, 0.125f, 0f), new Vector3(0.09f, 0.012f, 0.07f), new Color(0.75f, 0.6f, 0.2f));
            Box(root, new Vector3(0f, 0.5f, 0f), new Vector3(0.006f, 0.74f, 0.03f), new Color(0.8f, 0.84f, 0.9f));
        }

        static void Box(Transform parent, Vector3 position, Vector3 size, Color color)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyImmediate(box.GetComponent<Collider>());
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = size;
            var renderer = box.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            renderer.sharedMaterial = material;
        }

        static Transform FindChild(Transform root, string childName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                    return child;
            }

            return null;
        }

        // Au casque, le symbole (et le signe en cours) suivent le XR Origin : marcher ou tourner au joystick ne les fait pas
        // traverser par la lame. Sans casque, ils restent dans le monde : le viseur doit pouvoir les balayer.
        void ParentGuide()
        {
            var rig = PlayerRig.Instance;
            var parent = !m_Desktop && rig != null && rig.Origin != null ? rig.Origin.transform : null;
            if (m_Guide != null)
                m_Guide.transform.SetParent(parent, true);
            if (m_Sign != null && !m_Sign.IsFlying)
                m_Sign.transform.SetParent(parent, true);
        }

        // Le gantelet recouvre la main gantée de l'arc : posé sur son poignet, à sa taille, en miroir pour la main droite
        // (le modèle est une main gauche). Sans mains gantées, il suit la manette.
        void AttachGauntlet()
        {
            m_NextGauntletTry = Time.time + 1f;
            var rig = PlayerRig.Instance;
            if (m_Gauntlet != null || m_Definition == null || m_Definition.gauntletModel == null || rig == null ||
                !rig.TryGetHand(rig.BowHand, out var hand) || hand == null)
                return;

            m_GauntletHand = hand;
            m_Gauntlet = Instantiate(m_Definition.gauntletModel);
            m_Gauntlet.name = m_Definition.gauntletModel.name;
            foreach (var collider in m_Gauntlet.GetComponentsInChildren<Collider>(true))
                DestroyImmediate(collider);

            var glove = GlovedHand.For(hand);
            var left = rig.BowHand != InteractorHandedness.Right;
            if (glove != null)
            {
                m_GauntletGlove = glove;
                left = glove.IsLeft;
                var frame = glove.WristFrame;
                var scale = glove.FingerLength / k_GauntletLength;
                m_Gauntlet.transform.SetParent(glove.transform, false);
                m_Gauntlet.transform.localPosition = frame.position;
                m_Gauntlet.transform.localRotation = frame.rotation;
                m_Gauntlet.transform.localScale = new Vector3(left ? scale : -scale, scale, scale);
                glove.SetGloveHidden(true);
            }
            else
            {
                m_Gauntlet.transform.SetParent(hand.transform, false);
                m_Gauntlet.transform.localPosition = new Vector3(0f, -0.02f, -0.1f);
                m_Gauntlet.transform.localRotation = Quaternion.Euler(-45f, 0f, 0f);
                m_Gauntlet.transform.localScale = new Vector3(left ? 1f : -1f, 1f, 1f);
            }

            m_Palm = FindChild(m_Gauntlet.transform, "Paume");
            if (m_Palm == null)
            {
                m_Palm = new GameObject("Paume").transform;
                m_Palm.SetParent(m_Gauntlet.transform, false);
                m_Palm.localPosition = k_DefaultPalm;
            }

            // Ses lueurs palpitent quand un signe attend d'être lancé.
            m_GauntletGlow.Clear();
            foreach (var renderer in m_Gauntlet.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null || !material.name.ToLowerInvariant().Contains("lueur") || !material.HasProperty(k_EmissionId))
                        continue;
                    m_GauntletGlow.Add((renderer, material.GetColor(k_EmissionId)));
                    break;
                }
            }

            m_Gauntlet.SetActive(!m_Hidden && !m_Desktop);
        }

        void RemoveGauntlet()
        {
            if (m_GauntletGlove != null)
                m_GauntletGlove.SetGloveHidden(false);
            if (m_Gauntlet != null)
                Destroy(m_Gauntlet);
            m_Gauntlet = null;
            m_GauntletGlove = null;
        }

        // ---------------------------------------------------------------- chaque image

        void Update()
        {
            if (!m_Built)
            {
                if (!TryBuild())
                    return;
                m_Built = true;
            }

            if (m_Gauntlet == null && Time.time >= m_NextGauntletTry)
                AttachGauntlet();

            var desktop = DesktopControls.Instance != null && DesktopControls.Instance.IsActive;
            if (desktop != m_Desktop)
                SetDesktop(desktop);

            var hidden = m_Bow != null && m_Bow.IsStowed;
            if (hidden != m_Hidden)
                SetHidden(hidden);

            var deltaTime = Time.deltaTime;
            m_Glitch = Mathf.MoveTowards(m_Glitch, 0f, deltaTime * 4f);
            ApplyBladeGlitch();
            if (m_Hidden)
                return;

            if (!m_Desktop)
                UpdateMelee(deltaTime);
            UpdateSignState();
            UpdateGuide(deltaTime);
            if (!m_Desktop)
                PresentSign(deltaTime);
            if (!SlowMotion.IsPaused)
            {
                if (m_Desktop)
                    UpdateDesktop(deltaTime);
                else
                {
                    UpdateDrawing(deltaTime);
                    UpdateLaunch();
                }
            }

            UpdateGauntletGlow();
        }

        void SetHidden(bool hidden)
        {
            m_Hidden = hidden;
            m_Katana.Release();
            m_Katana.gameObject.SetActive(!hidden);
            if (!hidden && !m_Desktop)
                m_Katana.SnapToHolster();
            if (m_Gauntlet != null)
                m_Gauntlet.SetActive(!hidden && !m_Desktop);
            if (m_GauntletGlove != null)
                m_GauntletGlove.SetGloveHidden(!hidden && !m_Desktop && m_Gauntlet != null);

            // Rangés (construction, dimension) : le signe commencé ou prêt se brise, un nouveau symbole attendra.
            CancelGesture();
            if (m_Sign != null && !m_Sign.IsFlying)
                m_Sign.Break();
            m_Sign = null;
            if (hidden)
                m_Glyph = -1;
            else
                NewGlyph();
            m_HasPrevious = false;
            m_HasPoint = false;
            m_GuideSnap = true;
        }

        void SetDesktop(bool desktop)
        {
            m_Desktop = desktop;
            var camera = DesktopControls.Instance != null ? DesktopControls.Instance.CameraTransform : null;
            m_Katana.Suspend(desktop);
            if (desktop && camera != null)
                m_Katana.transform.SetParent(camera, false);
            else
                m_Katana.transform.SetParent(transform, true);
            if (!desktop)
                m_Katana.SnapToHolster();

            // Sans casque, pas de main : le gantelet n'est pas montré (le clic droit lance le signe).
            if (m_Gauntlet != null)
                m_Gauntlet.SetActive(!desktop && !m_Hidden);
            if (m_GauntletGlove != null)
                m_GauntletGlove.SetGloveHidden(!desktop && !m_Hidden && m_Gauntlet != null);
            CancelGesture();
            ParentGuide();
            m_HasPrevious = false;
            m_HasPoint = false;
            m_GuideSnap = true;
        }

        void ApplyBladeGlitch()
        {
            if (m_Block == null)
                return;
            m_Block.SetFloat(k_GlitchId, m_Glitch);
            foreach (var renderer in m_KatanaRenderers)
            {
                if (renderer != null)
                    renderer.SetPropertyBlock(m_Block);
            }
        }

        // ---------------------------------------------------------------- le coup de katana

        void UpdateMelee(float deltaTime)
        {
            var tip = BladeTip;
            var bladeBase = BladeStart;
            if (!KatanaActive || !m_HasPrevious || deltaTime <= 0f)
            {
                m_PreviousTip = tip;
                m_HasPrevious = KatanaActive;
                return;
            }

            var tipVelocity = (tip - m_PreviousTip) / deltaTime;
            var speed = tipVelocity.magnitude;
            if (speed >= k_HitSpeed)
            {
                Strike(bladeBase, tip, m_PreviousTip, tipVelocity, speed);
                CutProjectiles(bladeBase, tip);
            }

            m_PreviousTip = tip;
        }

        // Le coup : la lame (une capsule) et le chemin de la pointe depuis l'image d'avant ; chaque ennemi touché une fois
        // par délai, par sa zone de touche (la tête compte double, comme pour une flèche).
        void Strike(Vector3 bladeBase, Vector3 tip, Vector3 previousTip, Vector3 velocity, float speed)
        {
            var damage = m_Definition.katanaDamage * DamageMultiplier * Mathf.Lerp(1f, 1.6f, Mathf.InverseLerp(k_HitSpeed, k_HitFastSpeed, speed));
            var direction = velocity / speed;
            var count = Physics.OverlapCapsuleNonAlloc(bladeBase, tip, k_BladeRadius, s_Overlaps, ~0, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
                TryHit(s_Overlaps[i], s_Overlaps[i].ClosestPoint(tip), direction, damage);

            var step = tip - previousTip;
            var distance = step.magnitude;
            if (distance < 1e-4f)
                return;
            var hits = Physics.RaycastNonAlloc(previousTip, step / distance, s_Hits, distance, ~0, QueryTriggerInteraction.Collide);
            for (var i = 0; i < hits; i++)
                TryHit(s_Hits[i].collider, s_Hits[i].point, direction, damage);
        }

        void TryHit(Collider collider, Vector3 point, Vector3 direction, float damage)
        {
            if (collider == null || !collider.TryGetComponent(out Hitbox hitbox))
                return;
            var owner = hitbox.Owner;
            if (owner == null || !owner.IsAlive || (owner.GetComponentInParent<Enemy>() == null && owner.GetComponentInParent<Boss>() == null))
                return;
            if (m_LastHit.TryGetValue(owner, out var last) && Time.time - last < k_HitCooldown)
                return;

            m_LastHit[owner] = Time.time;
            var zone = hitbox.Zone;
            owner.TakeDamage(new DamageInfo
            {
                Amount = damage * hitbox.DamageMultiplier * (zone == HitZone.Head ? Hitbox.HeadDamageMultiplier : 1f),
                Zone = zone,
                Grade = ShotGrade.Good,
                Distance = 0f,
                Point = point,
                Direction = direction,
                Source = this,
            });
            Sfx.Play(m_Definition.katanaHitClip, point, 0.9f, Random.Range(0.9f, 1.1f));
            Haptics.Pulse(KatanaHand, 0.8f, 0.08f);
        }

        // La lame lancée coupe les projectiles qu'elle croise.
        void CutProjectiles(Vector3 bladeBase, Vector3 tip)
        {
            var blade = tip - bladeBase;
            for (var i = m_Projectiles.Count - 1; i >= 0; i--)
            {
                var projectile = m_Projectiles[i];
                if (projectile == null)
                {
                    m_Projectiles.RemoveAt(i);
                    continue;
                }

                var position = projectile.transform.position;
                var t = Mathf.Clamp01(Vector3.Dot(position - bladeBase, blade) / Mathf.Max(1e-4f, blade.sqrMagnitude));
                if ((position - (bladeBase + blade * t)).sqrMagnitude >= k_ProjectileReach * k_ProjectileReach)
                    continue;
                if (projectile.OnArrowHit(new ArrowHit { Collider = null, Point = position, Direction = Vector3.zero, IsShot = true, Grade = ShotGrade.Good }))
                    Haptics.Pulse(KatanaHand, 0.6f, 0.06f);
            }
        }

        // ---------------------------------------------------------------- le symbole

        void NewGlyph()
        {
            m_Glyph = KanjiGlyphs.Random(m_LastGlyph);
            m_LastGlyph = m_Glyph;
            m_Errors.Clear();
            CancelGesture();
            if (m_Guide != null)
                m_Guide.Show(m_Glyph);
            m_GuideSnap = true;
        }

        void UpdateSignState()
        {
            var now = Time.time;
            if (m_Sign != null && m_Sign.IsReady && now - m_ReadyTime > k_ReadyLife)
            {
                m_Sign.Break();
                m_Sign = null;
                m_NextGlyphTime = now;
            }
            else if (m_Sign != null && !m_Sign.IsReady && !m_Gesturing && now - m_LastProgress > k_PartialTimeout)
            {
                // Abandonné en cours de route : le signe commencé se brise, et c'est un autre symbole.
                m_Sign.Break();
                m_Sign = null;
                NewGlyph();
            }

            if (m_Sign == null && m_Glyph < 0 && now >= m_NextGlyphTime)
                NewGlyph();
        }

        // Le symbole devant le joueur : il le suit tant qu'aucun trait n'est commencé, puis reste où il est.
        void UpdateGuide(float deltaTime)
        {
            if (m_Guide == null)
                return;

            var visible = m_Glyph >= 0 && KatanaActive && (m_Sign == null || !m_Sign.IsReady);
            m_Guide.SetVisible(visible);
            var locked = m_Gesturing || (m_Sign != null && m_Sign.StrokeCount > 0);
            if (!visible || locked || !TryGuideTarget(out var target))
            {
                if (!visible)
                    m_GuideSnap = true;
                return;
            }

            var guide = m_Guide.transform;
            if (m_GuideSnap)
            {
                guide.SetPositionAndRotation(target.position, target.rotation);
                m_GuideSnap = false;
                m_GuideFollowing = false;
                return;
            }

            var angle = Quaternion.Angle(guide.rotation, target.rotation);
            var distance = Vector3.Distance(guide.position, target.position);
            // Sans casque, il suit le viseur de plus loin : on doit pouvoir viser le début d'un trait sans qu'il bouge.
            if (angle > (m_Desktop ? k_DesktopFollowAngle : k_GuideFollowAngle) || distance > k_GuideFollowDistance)
                m_GuideFollowing = true;
            if (!m_GuideFollowing)
                return;

            var blend = 1f - Mathf.Exp(-k_GuideFollowSpeed * deltaTime);
            guide.SetPositionAndRotation(Vector3.Lerp(guide.position, target.position, blend), Quaternion.Slerp(guide.rotation, target.rotation, blend));
            if (angle < 2f && distance < 0.02f)
                m_GuideFollowing = false;
        }

        bool TryGuideTarget(out Pose pose)
        {
            if (m_Desktop)
            {
                var camera = DesktopControls.Instance != null ? DesktopControls.Instance.CameraTransform : null;
                if (camera == null)
                {
                    pose = default;
                    return false;
                }

                pose = new Pose(camera.position + camera.forward * k_DesktopGuideDistance - camera.up * 0.02f,
                                Quaternion.LookRotation(camera.forward, camera.up));
                return true;
            }

            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (head == null)
            {
                pose = default;
                return false;
            }

            var yaw = rig.HeadYaw;
            pose = new Pose(head.position + yaw * Vector3.forward * k_GuideDistance + Vector3.down * k_GuideDrop, yaw);
            return true;
        }

        // ---------------------------------------------------------------- tracer au casque

        // La pointe de la lame, ramenée dans le plan du symbole, trace (dans le repère du symbole, qui suit le joueur : marcher
        // ou tourner au joystick ne trace rien).
        void UpdateDrawing(float deltaTime)
        {
            if (!KatanaActive || m_Glyph < 0 || (m_Sign != null && m_Sign.IsReady) || m_Guide == null || deltaTime <= 0f)
            {
                CancelGesture();
                m_HasPoint = false;
                return;
            }

            var guide = m_Guide.transform;
            var local = guide.InverseTransformPoint(BladeTip);
            var depth = local.z;
            local.z = 0f;
            var speed = m_HasPoint ? Vector3.Distance(local, m_PreviousPoint) / deltaTime : 0f;
            var previous = m_PreviousPoint;
            m_PreviousPoint = local;
            var hadPoint = m_HasPoint;
            m_HasPoint = true;
            if (!hadPoint)
                return;

            var nearPlane = depth >= -k_TipBehind && depth <= k_TipAhead;
            var inside = Mathf.Max(Mathf.Abs(local.x), Mathf.Abs(local.y)) / KanjiGuide.HalfSize;
            if (!m_Gesturing)
            {
                if (speed >= k_StartSpeed && nearPlane && inside <= 1.6f && !m_GuideFollowing)
                    BeginGesture(previous, local);
                return;
            }

            AddGesturePoint(local);
            m_SlowTime = speed < k_EndSpeed ? m_SlowTime + deltaTime : 0f;
            if (m_SlowTime >= k_EndHold || !nearPlane || Time.time - m_GestureStart > k_MaxGestureTime || inside > 1.9f)
                EndGesture();
        }

        // Points du geste : dans le repère du symbole, à plat (Z = 0).
        void BeginGesture(Vector3 from, Vector3 to)
        {
            m_Gesture.Clear();
            m_Gesture.Add(from);
            m_Gesture.Add(to);
            m_Gesturing = true;
            m_GestureStart = Time.time;
            m_SlowTime = 0f;
            Sfx.Play(m_Definition.katanaSwooshClip, m_Guide.transform.TransformPoint(to), 0.75f, Random.Range(0.92f, 1.12f));
            Haptics.Pulse(KatanaHand, 0.25f, 0.04f);
        }

        void AddGesturePoint(Vector3 point)
        {
            if (m_Gesture.Count == 0 || (m_Gesture[m_Gesture.Count - 1] - point).sqrMagnitude >= k_PointSpacing * k_PointSpacing)
                m_Gesture.Add(point);
        }

        void CancelGesture()
        {
            m_Gesturing = false;
            m_Gesture.Clear();
        }

        // Le geste fini : il compte pour le trait restant qu'il couvre le mieux. Pour chaque point du trait, la distance au
        // geste : un coup de sabre qui dépasse des deux bouts du trait, ou qui va dans l'autre sens, le couvre aussi bien ; un
        // geste trop court ou à côté laisse des points loin de lui.
        void EndGesture()
        {
            m_Gesturing = false;
            if (m_Gesture.Count < 2 || m_Guide == null)
                return;

            m_Gesture2D.Clear();
            foreach (var point in m_Gesture)
                m_Gesture2D.Add(new Vector2(point.x, point.y) / KanjiGuide.HalfSize);
            if (KanjiGlyphs.Length(m_Gesture2D) < k_MinGestureLength)
                return;

            var best = -1;
            var bestError = float.MaxValue;
            var strokes = m_Guide.Strokes;
            for (var s = 0; s < strokes.Length; s++)
            {
                if (m_Guide.IsDone(s))
                    continue;
                KanjiGlyphs.Resample(strokes[s], k_CoverSamples, m_SampleB);
                var total = 0f;
                var slant = 0f;
                for (var i = 0; i < m_SampleB.Count; i++)
                {
                    var tangent = m_SampleB[Mathf.Min(m_SampleB.Count - 1, i + 1)] - m_SampleB[Mathf.Max(0, i - 1)];
                    total += DistanceToPath(m_SampleB[i], m_Gesture2D, out var direction);
                    if (tangent.sqrMagnitude > 1e-10f)
                    {
                        tangent.Normalize();
                        slant += Mathf.Abs(tangent.x * direction.y - tangent.y * direction.x);
                    }
                }

                var error = (total + k_AngleWeight * slant) / k_CoverSamples;
                if (error < bestError)
                {
                    bestError = error;
                    best = s;
                }
            }

            if (best < 0 || bestError > k_Reject)
            {
                // À côté du symbole : il clignote en rouge, la manette le fait sentir.
                m_Guide.Miss();
                Haptics.Pulse(KatanaHand, 0.15f, 0.03f);
                return;
            }

            AcceptStroke(best, bestError);
        }

        // La distance d'un point au geste, et la direction du geste à l'endroit le plus proche.
        static float DistanceToPath(Vector2 point, List<Vector2> path, out Vector2 direction)
        {
            var best = float.MaxValue;
            direction = Vector2.right;
            for (var i = 1; i < path.Count; i++)
            {
                var a = path[i - 1];
                var segment = path[i] - a;
                var length = segment.sqrMagnitude;
                if (length < 1e-10f)
                    continue;
                var t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / length);
                var distance = Vector2.Distance(point, a + segment * t);
                if (distance < best)
                {
                    best = distance;
                    direction = segment / Mathf.Sqrt(length);
                }
            }

            return best;
        }

        void AcceptStroke(int stroke, float error)
        {
            var guide = m_Guide.transform;
            if (m_Sign == null)
            {
                // Le signe suit le joueur comme le symbole, jusqu'à son lancer.
                m_Sign = MatrixSign.Create(new Pose(guide.position, guide.rotation), TearMaterial, Random.Range(1, 1 << 20));
                m_Sign.transform.SetParent(guide.parent, true);
            }

            m_World.Clear();
            foreach (var point in m_Gesture)
                m_World.Add(guide.TransformPoint(point));
            m_Sign.AddStroke(m_World);
            m_Guide.MarkDone(stroke);
            m_Errors.Add(error);
            m_LastProgress = Time.time;
            m_Glitch = 1f;
            var middle = m_World[m_World.Count / 2];
            Sfx.Play(m_Definition.katanaCrackClip, middle, 0.8f, Random.Range(0.9f, 1.1f));
            Haptics.Pulse(KatanaHand, 0.6f, 0.07f);
            if (m_Guide.Remaining == 0)
                CompleteSign();
        }

        // Le signe complet : sa qualité vient de la précision moyenne de ses traits.
        void CompleteSign()
        {
            var total = 0f;
            foreach (var error in m_Errors)
                total += error;
            m_SignGrade = Grade(m_Errors.Count > 0 ? total / m_Errors.Count : k_Reject);
            var modifiers = Tuning.Get(m_SignGrade);
            m_Sign.SetReady(modifiers.color);
            m_ReadyTime = Time.time;
            m_Glyph = -1;
            m_Guide.SetVisible(false);

            var position = m_Sign.transform.position;
            if (!string.IsNullOrEmpty(modifiers.label))
                FloatingText.Spawn(position + Vector3.up * (KanjiGuide.HalfSize + 0.08f), modifiers.label, modifiers.color, 0.7f, 0.9f);
            Sfx.Play(m_Definition.katanaSignClip, position, 0.8f);
            if (m_SignGrade == ShotGrade.Perfect && m_Bow != null)
                Sfx.Play(m_Bow.PerfectClip, position, 0.7f);
        }

        // Vert (parfait), vert clair (très bon), orange (bon), rouge (raté) ; les améliorations de l'anneau élargissent les
        // qualités, comme celles de l'arc.
        ShotGrade Grade(float error)
        {
            if (m_Bow != null && m_Bow.ForcePerfect)
                return ShotGrade.Perfect;
            var gold = m_Bow != null ? Mathf.Max(0.1f, m_Bow.GoldWidthMultiplier) : 1f;
            var band = m_Bow != null ? Mathf.Max(0.1f, m_Bow.BandWidthMultiplier) : 1f;
            if (error <= k_Perfect * gold)
                return ShotGrade.Perfect;
            if (error <= k_Held * band)
                return ShotGrade.Held;
            return error <= k_Good * band ? ShotGrade.Good : ShotGrade.Miss;
        }

        // ---------------------------------------------------------------- lancer

        // Le signe prêt glisse jusqu'à portée du gantelet, et y reste en suivant le regard.
        void PresentSign(float deltaTime)
        {
            if (m_Sign == null || !m_Sign.IsReady)
                return;
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (head == null)
                return;

            var yaw = rig.HeadYaw;
            var offset = new Vector3(k_PresentOffset.x * -m_KatanaSide, k_PresentOffset.y, k_PresentOffset.z);
            var sign = m_Sign.transform;
            var blend = 1f - Mathf.Exp(-k_PresentSpeed * deltaTime);
            sign.SetPositionAndRotation(Vector3.Lerp(sign.position, head.position + yaw * offset, blend), Quaternion.Slerp(sign.rotation, yaw, blend));
            sign.localScale = Vector3.Lerp(sign.localScale, Vector3.one * k_PresentScale, blend);
        }

        // Le gantelet touche le signe prêt : il part.
        void UpdateLaunch()
        {
            if (m_Sign == null || !m_Sign.IsReady)
                return;

            // La paume du gantelet ; sans gantelet, la manette de la main de l'arc.
            Vector3 palm;
            if (m_Palm != null && m_Gauntlet != null && m_Gauntlet.activeInHierarchy)
                palm = m_Palm.position;
            else if (m_GauntletHand is Component hand && hand != null)
                palm = hand.transform.position;
            else
                return;

            if (m_Sign.DistanceTo(palm) <= k_LaunchReach)
                LaunchSign();
        }

        void LaunchSign()
        {
            if (m_Sign == null || !m_Sign.IsReady)
                return;

            var origin = m_Sign.transform.position;
            var target = FindTarget(out var forward);
            var direction = target != null ? (target.Center - origin).normalized : forward;
            var modifiers = Tuning.Get(m_SignGrade);
            var speed = Mathf.Max(k_MinSignSpeed, m_Definition.arrowSpeed * modifiers.speed * (m_Bow != null ? m_Bow.SpeedMultiplier : 1f));
            var damage = m_Definition.damage * modifiers.damage * DamageMultiplier;
            m_Sign.Launch(direction, speed, target, damage, m_SignGrade, this, m_Definition.katanaHitClip);

            if (m_Bow != null)
            {
                m_Bow.ReportShot(new ShotInfo
                {
                    Arrow = null,
                    Grade = m_SignGrade,
                    DrawRatio = 1f,
                    Speed = speed,
                    Damage = damage,
                    Origin = origin,
                    Direction = direction,
                });
            }

            Sfx.Play(m_Definition.katanaLaunchClip, origin, 1f, Random.Range(0.92f, 1.05f));
            Haptics.Pulse(m_GauntletHand, 0.9f, 0.12f);
            Haptics.Pulse(KatanaHand, 0.3f, 0.05f);
            m_Glitch = 1f;
            m_Sign = null;
            m_NextGlyphTime = Time.time + k_NextGlyphDelay;
        }

        // L'ennemi le plus proche du centre de la vue (à 70° au plus, sinon à 120°), à 90 m au plus. Sans cible, le signe
        // file droit devant le regard.
        Enemy FindTarget(out Vector3 forward)
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            forward = head != null ? head.forward : transform.forward;
            if (head == null)
                return null;

            Enemy best = null;
            Enemy wide = null;
            var bestAngle = k_TargetAngle;
            var wideAngle = k_TargetAngleWide;
            foreach (var enemy in Enemy.Alive)
            {
                if (enemy == null || !enemy.IsAlive)
                    continue;
                var offset = enemy.Center - head.position;
                if (offset.sqrMagnitude > k_TargetRange * k_TargetRange)
                    continue;
                var angle = Vector3.Angle(forward, offset);
                if (angle < bestAngle)
                {
                    bestAngle = angle;
                    best = enemy;
                }

                if (angle < wideAngle)
                {
                    wideAngle = angle;
                    wide = enemy;
                }
            }

            return best != null ? best : wide;
        }

        void UpdateGauntletGlow()
        {
            if (m_GauntletGlow.Count == 0 || m_Block == null)
                return;
            var ready = m_Sign != null && m_Sign.IsReady;
            var boost = ready ? 1.4f + 1.2f * (0.5f + 0.5f * Mathf.Sin(Time.time * 9f)) : 1f;
            if (m_GlowBlock == null)
                m_GlowBlock = new MaterialPropertyBlock();
            foreach (var (renderer, color) in m_GauntletGlow)
            {
                if (renderer == null)
                    continue;
                renderer.GetPropertyBlock(m_GlowBlock);
                m_GlowBlock.SetColor(k_EmissionId, color * boost);
                renderer.SetPropertyBlock(m_GlowBlock);
            }
        }

        // ---------------------------------------------------------------- sans casque

        // Clic gauche maintenu : le symbole se fige, et le viseur trace le trait qu'il balaie jusqu'au relâchement. Clic
        // droit : le signe prêt part.
        void UpdateDesktop(float deltaTime)
        {
            var controls = DesktopControls.Instance;
            var camera = controls != null ? controls.CameraTransform : null;
            var mouse = Mouse.current;
            if (camera == null)
                return;

            var focus = controls.HasFocus && !controls.RefocusedThisFrame && !DesktopHand.IsTypingText && mouse != null;
            var drawing = m_Glyph >= 0 && (m_Sign == null || !m_Sign.IsReady) && m_Guide != null;
            Vector3? point = null;
            if (drawing)
            {
                var plane = new Plane(m_Guide.transform.forward, m_Guide.transform.position);
                var ray = new Ray(camera.position, camera.forward);
                if (plane.Raycast(ray, out var enter))
                    point = ray.GetPoint(enter);
            }

            if (focus && drawing && point.HasValue)
            {
                // Comme au casque, les points du geste sont dans le repère du symbole, à plat.
                var local = m_Guide.transform.InverseTransformPoint(point.Value);
                local.z = 0f;
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    m_Gesture.Clear();
                    m_Gesture.Add(local);
                    m_Gesturing = true;
                    m_GestureStart = Time.time;
                    Sfx.Play(m_Definition.katanaSwooshClip, point.Value, 0.6f, Random.Range(0.92f, 1.12f));
                }
                else if (m_Gesturing && mouse.leftButton.isPressed)
                {
                    AddGesturePoint(local);
                }
                else if (m_Gesturing && mouse.leftButton.wasReleasedThisFrame)
                {
                    AddGesturePoint(local);
                    EndGesture();
                }
            }
            else if (m_Gesturing && (mouse == null || !mouse.leftButton.isPressed))
            {
                EndGesture();
            }

            if (focus && mouse.rightButton.wasPressedThisFrame)
                LaunchSign();

            // Le katana devant la caméra, la pointe vers le point tracé.
            var katana = m_Katana.transform;
            katana.localPosition = k_DesktopKatana;
            var aim = m_Gesturing && point.HasValue ? camera.InverseTransformPoint(point.Value) - k_DesktopKatana : new Vector3(0.12f, 0.55f, 1f);
            katana.localRotation = Quaternion.FromToRotation(Vector3.up, aim.normalized);
        }

        // ---------------------------------------------------------------- raccourci de test

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // O : le katana et le gantelet tout de suite (même verrouillés, même pendant la partie) ; O encore : l'arme choisie.
        sealed class TestKey : MonoBehaviour
        {
            BowDefinition m_Previous;

            void Update()
            {
                var keyboard = Keyboard.current;
                if (keyboard == null || !keyboard.oKey.wasPressedThisFrame)
                    return;
                var bow = FindAnyObjectByType<Bow>();
                var loadout = BowLoadout.Instance;
                if (bow == null || loadout == null)
                    return;

                if (bow.IsKatana)
                {
                    var back = m_Previous != null && !m_Previous.katana ? m_Previous : loadout.Selected;
                    if (back != null && back.katana)
                        back = bow.StartingDefinition;
                    bow.SetDefinition(back);
                    HeadMessages.Show((back != null ? back.displayName : "Arc") + " équipé", new Color(0.55f, 0.9f, 1f), 2f);
                    return;
                }

                foreach (var entry in loadout.Entries)
                {
                    if (entry == null || entry.bow == null || !entry.bow.katana)
                        continue;
                    m_Previous = bow.Definition;
                    bow.SetDefinition(entry.bow);
                    HeadMessages.Show(entry.bow.displayName + " équipés (test)\n<size=60%>O : retour à l'arme d'avant</size>", entry.color, 3f);
                    return;
                }

                HeadMessages.Show("Pas de katana dans la liste des armes (menu Archery > Katana et gantelet)", new Color(1f, 0.6f, 0.55f), 3f);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void InstallTestKey()
        {
            var go = new GameObject("Raccourci katana et gantelet (touche O)");
            DontDestroyOnLoad(go);
            go.AddComponent<TestKey>();
        }
#endif
    }
}
