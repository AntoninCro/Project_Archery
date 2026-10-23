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
    /// Le katana et le gantelet, une arme du joueur (à la place de l'arc ; carte du menu des armes, débloquée par « Rōnin »).
    /// Le katana flotte à côté du joueur et se prend avec la poignée ; le gantelet est porté sur la main de l'arc.
    /// Refaits le 23 octobre : plus de symbole à tracer. <b>Une vraie taille de katana</b> (rapide, d'au moins 40 cm,
    /// mesurée par rapport au joueur : se déplacer ne coupe rien) <b>ouvre une faille</b> dans la matrice
    /// (<see cref="MatrixRift"/>) le long du geste de la lame, à portée de bras : une fine fissure, puis elle s'ouvre d'un
    /// coup comme un œil, et vit (bords qui ondulent, tracé qui frémit) ; <b>frappée vite avec le gantelet</b>, elle file
    /// vers l'ennemi le plus proche du centre de la vue, le suit, et toute la faille touche. Sa fraîcheur donne la qualité
    /// du tir (moins de 0,4 s après son ouverture : parfait ; moins de 1 s : bon ; après : raté), et ses bords en prennent
    /// la couleur ; elle se referme au bout de 2,2 s. Jusqu'à 3 failles ouvertes à la fois. Le katana frappe aussi au corps
    /// à corps et coupe les projectiles.
    /// </summary>
    /// <remarks>
    /// Créé par l'<see cref="Bow"/> quand sa définition est <see cref="BowDefinition.katana"/> : l'arc est caché, et ses
    /// multiplicateurs (améliorations, coffres) servent ici (dégâts, vitesse, largeur des fenêtres de fraîcheur) ; chaque
    /// faille lancée compte comme un tir pour le score et les succès. Rangés avec lui (construction, dimensions).
    /// Sans casque : clic gauche, un coup de katana devant soi (qui ouvre une faille) ; clic droit, le gantelet frappe les
    /// failles ouvertes. Raccourci de test : <b>O</b> (éditeur et builds Development) équipe le katana et le gantelet,
    /// même verrouillés, et les rend.
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

        // Le gantelet : du poignet au bout du majeur (m) dans son modèle ; sa paume, s'il ne la donne pas.
        const float k_GauntletLength = 0.19f;
        static readonly Vector3 k_DefaultPalm = new Vector3(0f, -0.02f, 0.07f);

        // Le coup : dès que la pointe va à cette vitesse (m/s) ; dégâts ×1 à ×1,6 jusqu'à 7 m/s ; un ennemi n'est touché
        // qu'une fois par délai (s).
        const float k_HitSpeed = 2.5f;
        const float k_HitFastSpeed = 7f;
        const float k_HitCooldown = 0.4f;
        const float k_BladeRadius = 0.035f;
        const float k_ProjectileReach = 0.12f;
        const float k_TriggerCooldown = 1f;

        // La coupe se mesure par rapport au joueur (sa poitrine, 12 cm sous les yeux, dans le repère du XR Origin) : avancer
        // au joystick, courir ou marcher dans la pièce ne coupe rien (demandé le 23 octobre : en se déplaçant, on coupait
        // dans le vide). Elle commence quand la pointe va à 3,5 m/s, s'arrête quand elle ralentit sous 2 m/s, au bout de
        // 0,7 s ou de 1,4 m. Elle suit le point de la lame qui passe à 62 cm de la poitrine : la faille s'ouvre à portée du
        // gantelet, là où la lame a tranché.
        const float k_CutStartSpeed = 3.5f;
        const float k_CutEndSpeed = 2f;
        const float k_CutEndHold = 0.05f;
        const float k_MaxCutTime = 0.7f;
        const float k_MaxCutLength = 1.4f;
        const float k_CutSpacing = 0.01f;
        const float k_ChestDrop = 0.12f;
        const float k_CutReach = 0.62f;

        // Une vraie taille (23 octobre : agiter l'épée sur place devant le gantelet ouvrait des failles à la chaîne) : au
        // moins 40 cm de geste, et 30 cm entre son début et sa fin (un va-et-vient ne compte pas) ; devant le joueur (à 75°
        // au plus de son regard : pas en balançant les bras pour courir) ; et 0,25 s au moins après la faille précédente.
        const float k_MinCutLength = 0.4f;
        const float k_MinCutChord = 0.3f;
        const float k_CutFrontAngle = 75f;
        const float k_CutCooldown = 0.25f;

        // Les failles : 3 ouvertes au plus (une 4e referme la plus ancienne) ; ouverte, une faille vit 2,2 s, et glisse
        // doucement à 60 cm de la poitrine si elle s'est ouverte plus loin (lame tendue).
        const int k_MaxRifts = 3;
        const float k_RiftLife = 2.2f;
        const float k_DriftReach = 0.6f;
        const float k_DriftRate = 2.5f;

        // Sa fraîcheur donne la qualité du tir : frappée moins de 0,4 s après s'être ouverte, parfaite (vert) ; moins de
        // 1 s, bonne (orange) ; après, ratée (rouge). Les améliorations de l'anneau élargissent ces fenêtres.
        const float k_PerfectAge = 0.4f;
        const float k_GoodAge = 1f;

        // Le gantelet frappe les failles à moins de 40 cm de sa paume, en allant au moins à 1,3 m/s (par rapport au joueur).
        const float k_PunchSpeed = 1.3f;
        const float k_PunchReach = 0.4f;

        // La cible : l'ennemi le plus proche du centre de la vue (à 70° au plus, sinon 120°), à 90 m au plus.
        const float k_TargetRange = 90f;
        const float k_TargetAngle = 70f;
        const float k_TargetAngleWide = 120f;
        const float k_MinRiftSpeed = 18f;

        // Sans casque : le katana devant la caméra, et un coup (0,22 s) qui balaie la vue en travers ; la faille suit le
        // point de la lame à 50 cm de la poignée.
        static readonly Vector3 k_DesktopKatana = new Vector3(0.16f, -0.26f, 0.32f);
        const float k_DesktopSwing = 0.22f;
        const float k_DesktopCutAlong = 0.5f;

        static readonly int k_GlitchId = Shader.PropertyToID("_Glitch");
        static readonly int k_EmissionId = Shader.PropertyToID("_EmissionColor");
        static readonly Collider[] s_Overlaps = new Collider[32];
        static readonly RaycastHit[] s_Hits = new RaycastHit[16];

        readonly Dictionary<Health, float> m_LastHit = new Dictionary<Health, float>();
        readonly Dictionary<Object, float> m_LastTrigger = new Dictionary<Object, float>();
        readonly List<EnemyProjectile> m_Projectiles = new List<EnemyProjectile>();
        readonly List<MatrixRift> m_Rifts = new List<MatrixRift>();
        readonly List<MatrixRift> m_Struck = new List<MatrixRift>();
        readonly List<Enemy> m_Targets = new List<Enemy>();
        readonly List<Vector3> m_Cut = new List<Vector3>();
        readonly List<Vector3> m_World = new List<Vector3>();
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
        Material m_FallbackTear;

        // La coupe en cours : ses points par rapport à la poitrine du joueur, dans le repère du XR Origin (se déplacer
        // pendant le coup ne la déforme pas et ne la fait pas).
        bool m_Cutting;
        float m_CutStart;
        float m_CutSlow;
        float m_CutLength;
        float m_NextCutTime;
        Vector3 m_PreviousLocalTip;
        bool m_HasLocalTip;
        Vector3 m_PreviousPalm;
        bool m_HasPalm;

        // Le coup de katana au corps à corps (repère du monde).
        Vector3 m_PreviousTip;
        bool m_HasPrevious;

        // Sans casque.
        float m_SwingTime = -1f;
        float m_SwingSide = 1f;

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
            RemoveGauntlet();

            // Une faille en vol finit sa course ; celles qui attendaient se referment.
            CloseRifts();
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

        Material TearMaterial
        {
            get
            {
                if (m_Definition != null && m_Definition.katanaTearMaterial != null)
                    return m_Definition.katanaTearMaterial;
                if (m_FallbackTear == null)
                    m_FallbackTear = new Material(Shader.Find("Archery/MatrixTear")) { name = "Faille (secours)" };
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
                m_HasLocalTip = false;
            };
            m_Katana.SnapToHolster();
            m_KatanaRenderers = katana.GetComponentsInChildren<Renderer>(true);
            m_Block = new MaterialPropertyBlock();
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

            // Ses lueurs palpitent tant qu'une faille attend d'être frappée.
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

            // En pause (menu ouvert en cours de partie), on ne coupe rien et le gantelet ne lance rien.
            if (!SlowMotion.IsPaused)
            {
                if (m_Desktop)
                {
                    UpdateDesktop(deltaTime);
                }
                else
                {
                    UpdateMelee(deltaTime);
                    UpdateCut(deltaTime);
                    UpdatePunch(deltaTime);
                }
            }

            UpdateRifts(deltaTime);
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

            // Rangés (construction, dimension) : les failles ouvertes se referment.
            m_Cutting = false;
            CloseRifts();
            m_HasPrevious = false;
            m_HasLocalTip = false;
            m_HasPalm = false;
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

            // Sans casque, pas de main : le gantelet n'est pas montré (le clic droit frappe les failles).
            if (m_Gauntlet != null)
                m_Gauntlet.SetActive(!desktop && !m_Hidden);
            if (m_GauntletGlove != null)
                m_GauntletGlove.SetGloveHidden(!desktop && !m_Hidden && m_Gauntlet != null);
            m_Cutting = false;
            m_SwingTime = -1f;
            m_HasPrevious = false;
            m_HasLocalTip = false;
            m_HasPalm = false;
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
            if (collider == null)
                return;
            if (!collider.TryGetComponent(out Hitbox hitbox))
            {
                TryTrigger(collider, point, direction, damage);
                return;
            }

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

        // Un coup de lame dans ce qui réagit aux flèches sans être un ennemi (23 octobre : au katana, on ne pouvait pas
        // lancer de vague) : le gong, une cible d'entraînement, une cible de la victoire contre MK, une boule de feu. Il compte
        // comme une flèche tirée, une fois par seconde au plus pour chaque objet (on ne fait pas venir le boss par mégarde en
        // frappant le gong deux fois d'un même geste). Pas les boutons des menus ni les panneaux de difficulté : ils se
        // visent au rayon de la main.
        void TryTrigger(Collider collider, Vector3 point, Vector3 direction, float damage)
        {
            var handler = collider.GetComponentInParent<IArrowHitHandler>();
            if (handler == null || handler is Hitbox || handler is ILaserTransparent || !(handler is Object key) || key == null)
                return;
            if (m_LastTrigger.TryGetValue(key, out var last) && Time.time - last < k_TriggerCooldown)
                return;

            m_LastTrigger[key] = Time.time;
            var hit = new ArrowHit
            {
                Arrow = null,
                Collider = collider,
                Point = point,
                Normal = -direction,
                Direction = direction,
                Speed = 0f,
                Damage = damage,
                Grade = ShotGrade.Good,
                TravelDistance = 0f,
                IsShot = true,
            };
            handler.OnArrowHit(hit);
            Haptics.Pulse(KatanaHand, 0.7f, 0.08f);
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

        // ---------------------------------------------------------------- la coupe (au casque)

        // La pointe va vite par rapport au joueur : la coupe commence, et suit le point de la lame à portée de bras. Tout se
        // mesure depuis sa poitrine, dans le repère du XR Origin : ses déplacements (joystick, course aux bras, pas dans la
        // pièce) ne comptent pas.
        void UpdateCut(float deltaTime)
        {
            var rig = PlayerRig.Instance;
            var origin = rig != null && rig.Origin != null ? rig.Origin.transform : null;
            var head = rig != null ? rig.Head : null;
            if (!KatanaActive || origin == null || head == null || deltaTime <= 0f)
            {
                m_Cutting = false;
                m_HasLocalTip = false;
                return;
            }

            var chest = head.position + Vector3.down * k_ChestDrop;
            var localChest = origin.InverseTransformPoint(chest);
            var tip = BladeTip;
            var localTip = origin.InverseTransformPoint(tip) - localChest;
            var speed = m_HasLocalTip ? Vector3.Distance(localTip, m_PreviousLocalTip) / deltaTime : 0f;
            m_PreviousLocalTip = localTip;
            var hadTip = m_HasLocalTip;
            m_HasLocalTip = true;
            if (!hadTip)
                return;

            var point = origin.InverseTransformPoint(ReachPoint(BladeStart, tip, chest, k_CutReach)) - localChest;
            if (!m_Cutting)
            {
                if (speed >= k_CutStartSpeed && Time.time >= m_NextCutTime)
                    BeginCut(point, tip);
                return;
            }

            AddCutPoint(point);
            m_CutSlow = speed < k_CutEndSpeed ? m_CutSlow + deltaTime : 0f;
            if (m_CutSlow >= k_CutEndHold || Time.time - m_CutStart > k_MaxCutTime || m_CutLength >= k_MaxCutLength)
                EndCut(origin, localChest, head, rig.HeadYaw * Vector3.forward);
        }

        // Le point de la lame qui passe à <paramref name="reach"/> de <paramref name="center"/> ; la pointe si toute la lame
        // est plus près, le point le plus proche si toute la lame est plus loin.
        static Vector3 ReachPoint(Vector3 bladeBase, Vector3 tip, Vector3 center, float reach)
        {
            var blade = tip - bladeBase;
            var a = blade.sqrMagnitude;
            if (a < 1e-6f)
                return tip;

            var fromCenter = bladeBase - center;
            var c = fromCenter.sqrMagnitude - reach * reach;
            var baseInside = c <= 0f;
            var tipInside = (tip - center).sqrMagnitude <= reach * reach;
            if (baseInside && tipInside)
                return tip;

            var b = 2f * Vector3.Dot(fromCenter, blade);
            var discriminant = b * b - 4f * a * c;
            if (baseInside == tipInside || discriminant < 0f)
                return bladeBase + blade * Mathf.Clamp01(-Vector3.Dot(fromCenter, blade) / a);

            var root = Mathf.Sqrt(discriminant);
            var t = baseInside ? (-b + root) / (2f * a) : (-b - root) / (2f * a);
            return bladeBase + blade * Mathf.Clamp01(t);
        }

        void BeginCut(Vector3 localPoint, Vector3 tip)
        {
            m_Cut.Clear();
            m_Cut.Add(localPoint);
            m_Cutting = true;
            m_CutStart = Time.time;
            m_CutSlow = 0f;
            m_CutLength = 0f;
            Sfx.Play(m_Definition.katanaSwooshClip, tip, 0.75f, Random.Range(0.92f, 1.12f));
            Haptics.Pulse(KatanaHand, 0.25f, 0.04f);
        }

        void AddCutPoint(Vector3 point)
        {
            var last = m_Cut[m_Cut.Count - 1];
            var step = Vector3.Distance(last, point);
            if (step < k_CutSpacing)
                return;
            m_Cut.Add(point);
            m_CutLength += step;
        }

        // La coupe finie : une vraie taille (assez longue, pas un va-et-vient), devant le joueur, ouvre une faille. Ses points,
        // relatifs à la poitrine, sont reposés autour de la poitrine d'aujourd'hui.
        void EndCut(Transform origin, Vector3 localChest, Transform head, Vector3 forward)
        {
            m_Cutting = false;
            if (m_CutLength < k_MinCutLength || m_Cut.Count < 3 || Vector3.Distance(m_Cut[0], m_Cut[m_Cut.Count - 1]) < k_MinCutChord)
                return;

            m_World.Clear();
            foreach (var point in m_Cut)
                m_World.Add(origin.TransformPoint(localChest + point));
            var middle = Vector3.ProjectOnPlane(m_World[m_World.Count / 2] - head.position, Vector3.up);
            var ahead = Vector3.ProjectOnPlane(forward, Vector3.up);
            if (middle.sqrMagnitude > 1e-4f && ahead.sqrMagnitude > 1e-4f && Vector3.Angle(middle, ahead) > k_CutFrontAngle)
                return;

            OpenRift(m_World, head.position, origin);
        }

        // ---------------------------------------------------------------- les failles

        void OpenRift(List<Vector3> worldPoints, Vector3 viewer, Transform parent)
        {
            // Une 4e faille referme la plus ancienne.
            for (var i = m_Rifts.Count - 1; i >= 0; i--)
            {
                if (m_Rifts[i] == null || !m_Rifts[i].IsOpen)
                    m_Rifts.RemoveAt(i);
            }

            while (m_Rifts.Count >= k_MaxRifts)
            {
                m_Rifts[0].Close();
                m_Rifts.RemoveAt(0);
            }

            var rift = MatrixRift.Create(worldPoints, viewer, TearMaterial, Random.Range(1, 1 << 20));
            if (parent != null)
                rift.transform.SetParent(parent, true);
            rift.SetTint(Tuning.Get(Freshness(0f)).color);
            rift.Snapped += OnRiftSnapped;
            m_Rifts.Add(rift);
            m_NextCutTime = Time.time + k_CutCooldown;

            // D'abord la fine fissure : un petit craquement aigu ; la faille s'ouvrira d'un coup juste après.
            Sfx.Play(m_Definition.katanaCrackClip, rift.transform.position, 0.35f, Random.Range(1.4f, 1.6f));
            Haptics.Pulse(KatanaHand, 0.25f, 0.03f);
        }

        // La fissure s'ouvre d'un coup : le craquement franc, la lame qui bugue, la manette qui le fait sentir.
        void OnRiftSnapped(MatrixRift rift)
        {
            if (rift == null)
                return;
            m_Glitch = 1f;
            Sfx.Play(m_Definition.katanaCrackClip, rift.transform.position, 0.85f, Random.Range(0.85f, 1.0f));
            Haptics.Pulse(KatanaHand, 0.6f, 0.07f);
            Haptics.Pulse(m_GauntletHand, 0.2f, 0.04f);
        }

        // Chaque faille ouverte prend la couleur de la qualité qu'aurait le tir maintenant, se referme au bout de son temps,
        // et (au casque) glisse à portée du gantelet si elle s'est ouverte trop loin.
        void UpdateRifts(float deltaTime)
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            for (var i = m_Rifts.Count - 1; i >= 0; i--)
            {
                var rift = m_Rifts[i];
                if (rift == null || !rift.IsOpen)
                {
                    m_Rifts.RemoveAt(i);
                    continue;
                }

                if (rift.Age > k_RiftLife)
                {
                    rift.Close();
                    m_Rifts.RemoveAt(i);
                    continue;
                }

                rift.SetTint(Tuning.Get(Freshness(rift.Age)).color);
                if (m_Desktop || head == null)
                    continue;

                var chest = head.position + Vector3.down * k_ChestDrop;
                var offset = rift.transform.position - chest;
                if (offset.magnitude > k_DriftReach + 0.05f)
                    rift.DriftTowards(chest + offset.normalized * k_DriftReach, k_DriftRate, deltaTime);
            }
        }

        void CloseRifts()
        {
            foreach (var rift in m_Rifts)
            {
                if (rift != null)
                    rift.Close();
            }

            m_Rifts.Clear();
        }

        // Frappée tout de suite : parfaite (vert) ; un peu après : bonne (orange) ; trop tard : ratée (rouge).
        ShotGrade Freshness(float age)
        {
            if (m_Bow != null && m_Bow.ForcePerfect)
                return ShotGrade.Perfect;
            var gold = m_Bow != null ? Mathf.Max(0.1f, m_Bow.GoldWidthMultiplier) : 1f;
            var band = m_Bow != null ? Mathf.Max(0.1f, m_Bow.BandWidthMultiplier) : 1f;
            if (age <= k_PerfectAge * gold)
                return ShotGrade.Perfect;
            return age <= k_GoodAge * band ? ShotGrade.Good : ShotGrade.Miss;
        }

        // ---------------------------------------------------------------- le gantelet

        // Un coup de poing du gantelet (par rapport à la poitrine du joueur : se déplacer ne compte pas) lance les failles
        // qu'il frappe.
        void UpdatePunch(float deltaTime)
        {
            var rig = PlayerRig.Instance;
            var origin = rig != null && rig.Origin != null ? rig.Origin.transform : null;
            var head = rig != null ? rig.Head : null;

            // La paume du gantelet ; sans gantelet, la manette de la main de l'arc.
            var palm = Vector3.zero;
            if (m_Palm != null && m_Gauntlet != null && m_Gauntlet.activeInHierarchy)
                palm = m_Palm.position;
            else if (m_GauntletHand is Component hand && hand != null)
                palm = hand.transform.position;
            else
                origin = null;

            if (origin == null || head == null || deltaTime <= 0f)
            {
                m_HasPalm = false;
                return;
            }

            var local = origin.InverseTransformPoint(palm) - origin.InverseTransformPoint(head.position + Vector3.down * k_ChestDrop);
            var speed = m_HasPalm ? Vector3.Distance(local, m_PreviousPalm) / deltaTime : 0f;
            m_PreviousPalm = local;
            m_HasPalm = true;
            if (speed < k_PunchSpeed || m_Rifts.Count == 0)
                return;

            m_Struck.Clear();
            foreach (var rift in m_Rifts)
            {
                if (rift != null && rift.IsOpen && rift.DistanceTo(palm) <= k_PunchReach)
                    m_Struck.Add(rift);
            }

            if (m_Struck.Count > 0)
                LaunchRifts(m_Struck, head.position, head.forward);
        }

        // Les failles frappées partent vers les ennemis les plus proches du centre de la vue (une par ennemi, tant qu'il y
        // en a) ; chacune compte comme un tir.
        void LaunchRifts(List<MatrixRift> rifts, Vector3 viewOrigin, Vector3 viewForward)
        {
            FindTargets(viewOrigin, viewForward);
            for (var i = 0; i < rifts.Count; i++)
            {
                var rift = rifts[i];
                if (rift == null || !rift.IsOpen)
                    continue;

                var grade = Freshness(rift.Age);
                var modifiers = Tuning.Get(grade);
                var target = m_Targets.Count > 0 ? m_Targets[i % m_Targets.Count] : null;
                var origin = rift.transform.position;
                var direction = target != null ? (target.Center - origin).normalized : viewForward;
                var speed = Mathf.Max(k_MinRiftSpeed, m_Definition.arrowSpeed * modifiers.speed * (m_Bow != null ? m_Bow.SpeedMultiplier : 1f));
                var damage = m_Definition.damage * modifiers.damage * DamageMultiplier;
                rift.Launch(direction, speed, target, damage, grade, modifiers.color, this, m_Definition.katanaHitClip);
                m_Rifts.Remove(rift);

                if (m_Bow != null)
                {
                    m_Bow.ReportShot(new ShotInfo
                    {
                        Arrow = null,
                        Grade = grade,
                        DrawRatio = 1f,
                        Speed = speed,
                        Damage = damage,
                        Origin = origin,
                        Direction = direction,
                    });
                }

                if (!string.IsNullOrEmpty(modifiers.label))
                    FloatingText.Spawn(origin + Vector3.up * 0.15f, modifiers.label, modifiers.color, 0.7f, 0.9f);
                Sfx.Play(m_Definition.katanaLaunchClip, origin, 1f, Random.Range(0.92f, 1.05f));
                if (grade == ShotGrade.Perfect)
                {
                    Sfx.Play(m_Definition.katanaSignClip, origin, 0.7f);
                    if (m_Bow != null)
                        Sfx.Play(m_Bow.PerfectClip, origin, 0.7f);
                }
            }

            Haptics.Pulse(m_GauntletHand, 0.9f, 0.12f);
            Haptics.Pulse(KatanaHand, 0.3f, 0.05f);
            m_Glitch = 1f;
        }

        // Les ennemis à 70° au plus du centre de la vue (sinon à 120°), à 90 m au plus, du plus proche du centre au plus
        // éloigné. Sans cible, la faille file droit devant le regard.
        void FindTargets(Vector3 viewOrigin, Vector3 forward)
        {
            m_Targets.Clear();
            var limit = k_TargetAngle;
            for (var pass = 0; pass < 2 && m_Targets.Count == 0; pass++)
            {
                foreach (var enemy in Enemy.Alive)
                {
                    if (enemy == null || !enemy.IsAlive)
                        continue;
                    var offset = enemy.Center - viewOrigin;
                    if (offset.sqrMagnitude <= k_TargetRange * k_TargetRange && Vector3.Angle(forward, offset) < limit)
                        m_Targets.Add(enemy);
                }

                limit = k_TargetAngleWide;
            }

            m_Targets.Sort((a, b) => Vector3.Angle(forward, a.Center - viewOrigin).CompareTo(Vector3.Angle(forward, b.Center - viewOrigin)));
        }

        // Ses lueurs palpitent tant qu'une faille attend, vite pendant qu'un tir serait parfait.
        void UpdateGauntletGlow()
        {
            if (m_GauntletGlow.Count == 0 || m_Block == null)
                return;

            var waiting = false;
            var perfect = false;
            foreach (var rift in m_Rifts)
            {
                if (rift == null || !rift.IsOpen)
                    continue;
                waiting = true;
                perfect |= Freshness(rift.Age) == ShotGrade.Perfect;
            }

            var boost = !waiting ? 1f : perfect ? 1.6f + 1.4f * (0.5f + 0.5f * Mathf.Sin(Time.time * 16f)) : 1.3f + 0.6f * (0.5f + 0.5f * Mathf.Sin(Time.time * 7f));
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

        // Clic gauche : un coup de katana devant soi (d'un côté puis de l'autre), qui frappe au corps à corps et ouvre une
        // faille. Clic droit : le gantelet frappe les failles ouvertes, qui partent vers ce que vise le viseur.
        void UpdateDesktop(float deltaTime)
        {
            var controls = DesktopControls.Instance;
            var camera = controls != null ? controls.CameraTransform : null;
            var mouse = Mouse.current;
            if (camera == null)
                return;

            var focus = controls.HasFocus && !controls.RefocusedThisFrame && !DesktopHand.IsTypingText && mouse != null;
            if (focus && mouse.leftButton.wasPressedThisFrame && m_SwingTime < 0f)
            {
                m_SwingTime = 0f;
                m_SwingSide = -m_SwingSide;
                m_World.Clear();
                m_HasPrevious = false;
                Sfx.Play(m_Definition.katanaSwooshClip, camera.position + camera.forward * 0.6f, 0.6f, Random.Range(0.92f, 1.12f));
            }

            // Au repos, la lame dressée vers l'avant ; pendant le coup, elle balaie la vue en travers.
            var bladeDirection = new Vector3(0.15f, 0.55f, 1f).normalized;
            var swinging = m_SwingTime >= 0f;
            if (swinging)
            {
                m_SwingTime += deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(m_SwingTime / k_DesktopSwing));
                var from = new Vector3(0.9f * m_SwingSide, 0.35f, 0.8f).normalized;
                var to = new Vector3(-0.9f * m_SwingSide, -0.25f, 0.8f).normalized;
                bladeDirection = Vector3.Slerp(from, to, t);
            }

            var katana = m_Katana.transform;
            katana.localPosition = k_DesktopKatana;
            katana.localRotation = Quaternion.FromToRotation(Vector3.up, bladeDirection);

            if (swinging)
            {
                // Le coup frappe au corps à corps, et la lame trace la faille.
                var tip = BladeTip;
                if (m_HasPrevious && deltaTime > 0f)
                {
                    var velocity = (tip - m_PreviousTip) / deltaTime;
                    var speed = velocity.magnitude;
                    if (speed >= k_HitSpeed)
                    {
                        Strike(BladeStart, tip, m_PreviousTip, velocity, speed);
                        CutProjectiles(BladeStart, tip);
                    }
                }

                m_PreviousTip = tip;
                m_HasPrevious = true;
                m_World.Add(camera.TransformPoint(k_DesktopKatana + bladeDirection * k_DesktopCutAlong));
                if (m_SwingTime >= k_DesktopSwing)
                {
                    m_SwingTime = -1f;
                    var rig = PlayerRig.Instance;
                    if (m_World.Count >= 3)
                        OpenRift(m_World, camera.position, rig != null && rig.Origin != null ? rig.Origin.transform : null);
                }
            }

            if (focus && mouse.rightButton.wasPressedThisFrame)
            {
                m_Struck.Clear();
                foreach (var rift in m_Rifts)
                {
                    if (rift != null && rift.IsOpen)
                        m_Struck.Add(rift);
                }

                if (m_Struck.Count > 0)
                    LaunchRifts(m_Struck, camera.position, camera.forward);
            }
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
