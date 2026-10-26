using System.Collections.Generic;
using Archery.BowClasses;
using Archery.Bows;
using Archery.Core;
using Archery.Enemies;
using Archery.Locomotion;
using Archery.Player;
using Archery.UI;
using Archery.Upgrades;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Combat
{
    /// <summary>
    /// Le katana, une arme du joueur (à la place de l'arc ; carte du menu des armes, débloquée par « Rōnin »). Il flotte à
    /// côté du joueur et se prend avec la poignée ; la main de l'arc garde le gant d'archer, qui devient le <b>gant de la
    /// matrice</b> (refait le 24 octobre : avant, un gantelet).
    /// <b>Une taille de katana devant soi</b> (dans le champ de vision, mesurée par rapport au joueur : se déplacer ne coupe
    /// rien) ouvre une faille dans la matrice (<see cref="MatrixRift"/>) <b>au bout de la lame</b>, au fil de son passage ;
    /// elle s'élargit ensuite doucement. Chaque demi-tour de la lame finit une faille et en commence une autre : on peut
    /// enchaîner. Un geste qui ne compte pas (trop court, un aller-retour) laisse une fissure qui grésille et se referme.
    /// 5 failles en tout : une nouvelle referme la plus ancienne, même dans une fusion.
    /// <b>La qualité vient de la taille du coup</b> : plus il est long, meilleur est le tir (les bords en prennent la couleur).
    /// <b>Deux failles qui se croisent fusionnent</b>, quelle que soit leur forme, et ça se voit : une faille plus forte,
    /// cyan, qui explose à l'impact. <b>Un coup de poing de la main lance une onde de choc</b> (<see cref="ShockWave"/>) qui
    /// propulse les failles qu'elle atteint : chacune file vers l'ennemi le plus proche du centre de la vue, le suit, et
    /// toute la faille touche ; les améliorations des flèches lui servent aussi (multitir, écho, déluge, perçage, ricochet,
    /// auto-visée, foudre, explosion, glace, vampirisme). Le katana frappe au corps à corps et coupe les projectiles ; la main
    /// de la matrice frappe aussi les ennemis, et les repousse.
    /// </summary>
    /// <remarks>
    /// Créé par l'<see cref="Bow"/> quand sa définition est <see cref="BowDefinition.katana"/> : l'arc est caché, et ses
    /// multiplicateurs (améliorations, coffres) servent ici (dégâts, vitesse, seuils de qualité) ; chaque faille lancée compte
    /// comme un tir pour le score et les succès. Rangé avec lui (construction, dimensions).
    /// Sans casque : clic gauche, un coup de katana devant soi (qui ouvre une faille ; deux coups de suite se croisent et
    /// fusionnent) ; clic droit, une onde de choc droit devant. Raccourci de test : <b>O</b> (éditeur et builds
    /// Development) équipe le katana, même verrouillé, et le rend.
    /// </remarks>
    public class Katana : MonoBehaviour, IMeleeSource
    {
        // Le katana, dans son repère (le milieu de la poignée à l'origine, la lame vers +Y), si le modèle ne donne pas ses
        // repères « Garde » et « Pointe ».
        static readonly Vector3 k_DefaultBladeStart = new Vector3(0f, 0.15f, 0f);
        static readonly Vector3 k_DefaultBladeTip = new Vector3(0f, 0.85f, 0f);

        // En main : la poignée dans la paume, la lame le long du poing (la poignée des manettes Quest est inclinée de 45°).
        static readonly Quaternion k_KatanaAttach = Quaternion.Euler(-45f, 0f, 0f);
        static readonly Vector3 k_KatanaBoxCenter = new Vector3(0f, 0.36f, 0f);
        static readonly Vector3 k_KatanaBoxSize = new Vector3(0.07f, 1f, 0.07f);

        // Rangé : debout à côté du joueur, comme l'arc rangé (24 octobre : avant, à la hanche, lame vers le bas et l'arrière,
        // comme à la ceinture), du côté de la main qui le prend : 22 cm sur le côté, 45 cm devant, la poignée 62 cm sous les
        // yeux (plus bas que l'arc : la lame monte de 85 cm), penché de 10° vers l'extérieur pour ne pas passer devant le
        // visage.
        static readonly Vector3 k_KatanaHolster = new Vector3(0.22f, -0.62f, 0.45f);
        const float k_KatanaHolsterTilt = 10f;

        // La main de la matrice : le creux du poing, dans le repère du poignet du gant (doigts vers +Z, dos de la main vers
        // +Y), pour un gant de 19 cm du poignet au bout du majeur ; sans gant, devant la manette.
        static readonly Vector3 k_Fist = new Vector3(0f, -0.015f, 0.085f);
        const float k_GloveLength = 0.19f;
        static readonly Vector3 k_ControllerFist = new Vector3(0f, -0.02f, 0.04f);

        // Le coup : dès que la pointe va à cette vitesse (m/s) ; dégâts ×1 à ×1,6 jusqu'à 7 m/s ; un ennemi n'est touché
        // qu'une fois par délai (s).
        const float k_HitSpeed = 2.5f;
        const float k_HitFastSpeed = 7f;
        const float k_HitCooldown = 0.4f;
        const float k_BladeRadius = 0.035f;
        const float k_ProjectileReach = 0.12f;
        const float k_TriggerCooldown = 1f;

        // La coupe se mesure par rapport au joueur (sa poitrine, 12 cm sous les yeux, dans le repère du XR Origin) : avancer
        // au joystick, courir ou marcher dans la pièce ne coupe rien. Elle commence quand la pointe va à 3 m/s, dans le champ
        // de vision (à 55° au plus du regard : pas en balançant les bras pour courir) ; la faille s'ouvre au bout de la lame
        // (24 octobre, après le test : avant, au point de la lame à 62 cm de la poitrine, à portée de la main). La coupe finit
        // quand la lame ralentit sous 1,8 m/s, sort du champ de vision, ou au bout de 0,8 s ou de 2,6 m.
        const float k_CutStartSpeed = 3f;
        const float k_CutEndSpeed = 1.8f;
        const float k_CutEndHold = 0.04f;
        const float k_MaxCutTime = 0.8f;
        const float k_MaxCutLength = 2.6f;
        const float k_CutSpacing = 0.01f;
        const float k_ChestDrop = 0.12f;
        const float k_ViewAngle = 55f;

        // Enchaîner (24 octobre : des coups enchaînés vite fusionnaient en un aller-retour, refusé) : quand la lame fait
        // demi-tour (sa direction tourne de plus de 110° par rapport à ses 5 derniers centimètres), la coupe finit là et une
        // autre commence aussitôt, sans pause.
        const float k_TurnAngle = 110f;
        const float k_TurnWindow = 0.05f;

        // Une vraie taille : au moins 55 cm de geste au bout de la lame, et 40 cm entre son début et sa fin (un va-et-vient ne
        // compte pas). En dessous, la fissure grésille et se referme ; sous 13 cm, elle disparaît sans bruit. (Au bout de la
        // lame, un même geste trace environ deux fois plus long qu'à 62 cm de la poitrine : les seuils d'avant, ×1,6.)
        const float k_MinCutLength = 0.55f;
        const float k_MinCutChord = 0.4f;
        const float k_SilentCut = 0.13f;

        // Les failles : 5 en tout, fusionnées comprises (24 octobre, après le test : une faille de plus à côté d'une fusion de
        // 5 en faisait 6) ; une nouvelle referme d'abord la plus ancienne, même si elle est dans une fusion. Ouverte, une
        // faille vit 3 s, là où la lame l'a ouverte.
        const int k_MaxRifts = 5;
        const float k_RiftLife = 3f;

        // La qualité vient de la longueur du coup, au bout de la lame : moyen sous 90 cm, bon jusqu'à 1,2 m, très bon jusqu'à
        // 1,5 m, parfait au-delà, et toujours pour des failles fusionnées. La précision abaisse le seuil du parfait, la charge
        // rapide ceux du bon et du très bon.
        const float k_GoodLength = 0.9f;
        const float k_HeldLength = 1.2f;
        const float k_PerfectLength = 1.5f;

        // La fusion : 5 failles au plus en une ; +35 % de dégâts par faille fusionnée en plus, et une explosion à l'impact
        // (1,6 m de rayon, 60 cm de plus par faille en plus, 4 m au plus ; 60 % des dégâts au centre).
        const int k_MaxFusion = 5;
        const float k_FusionBonus = 0.35f;
        const float k_BlastRadius = 1.6f;
        const float k_BlastRadiusStep = 0.6f;
        const float k_MaxBlastRadius = 4f;
        const float k_BlastShare = 0.6f;

        // Les 0 et les 1 qui jaillissent d'un croisement : la moitié de la gerbe d'un ennemi tué, soit 20 chiffres
        // (24 octobre, demandé par le joueur ; 40 avant).
        const float k_FusionDigits = 0.5f;

        // La main de la matrice : un coup de poing (le poing à 2 m/s au moins par rapport au joueur, en tendant le bras, 0,4 s
        // après le précédent, pas en courant aux bras) lance une onde de choc devant lui (24 octobre, après le test : avant, il
        // fallait toucher la faille, et la faille s'ouvrait à portée de la main). L'onde file à 9 m/s sur 3 m en s'élargissant
        // (25 cm de rayon au départ, 30 cm de plus par mètre) et propulse chaque faille qu'elle atteint. À 2,2 m/s, le poing
        // (9 cm) frappe aussi les ennemis : la moitié des dégâts d'un coup de katana, et un recul de 4 m/s.
        const float k_WavePunchSpeed = 2f;
        const float k_WaveExtension = 0.3f;
        const float k_WaveCooldown = 0.4f;
        const float k_WaveTravel = 9f;
        const float k_WaveRange = 3f;
        const float k_WaveRadius = 0.25f;
        const float k_WaveGrowth = 0.3f;
        static readonly Color k_WaveColor = new Color(0.4f, 1f, 0.85f);
        const float k_FistSpeed = 2.2f;
        const float k_FistRadius = 0.09f;
        const float k_FistDamage = 0.5f;
        const float k_FistPush = 4f;

        // La cible : l'ennemi le plus proche du centre de la vue (à 70° au plus, sinon 120°), à 90 m au plus. Le multitir
        // ouvre ses failles en plus en éventail (8° entre deux) quand il n'y a pas assez de cibles.
        const float k_TargetRange = 90f;
        const float k_TargetAngle = 70f;
        const float k_TargetAngleWide = 120f;
        const float k_MinRiftSpeed = 18f;
        const float k_FanAngle = 8f;

        // Un lancer parfait (ou de failles fusionnées) marque le coup : le temps passe à 20 % pendant 0,07 s.
        const float k_HitStopScale = 0.2f;
        const float k_HitStopTime = 0.07f;

        // Sans casque : le katana devant la caméra, et un coup (0,22 s) qui balaie la vue en travers ; la faille suit la pointe.
        // Le clic droit lance l'onde de choc droit devant, depuis 35 cm devant la caméra.
        static readonly Vector3 k_DesktopKatana = new Vector3(0.16f, -0.26f, 0.32f);
        const float k_DesktopSwing = 0.22f;
        const float k_DesktopWave = 0.35f;

        static readonly Color k_TrailColor = new Color(0.35f, 1f, 1f);
        // Les failles fusionnées sont cyan (24 octobre : en vert pâle, on ne les distinguait pas d'une faille parfaite).
        static readonly Color k_FusionColor = new Color(0.3f, 0.92f, 1f);
        static readonly int k_GlitchId = Shader.PropertyToID("_Glitch");
        static readonly int k_PulseId = Shader.PropertyToID("_Pulse");
        static readonly Collider[] s_Overlaps = new Collider[32];
        static readonly RaycastHit[] s_Hits = new RaycastHit[16];
        static readonly List<ParticleSystemVertexStream> s_DigitStreams = new List<ParticleSystemVertexStream>
        {
            ParticleSystemVertexStream.Position,
            ParticleSystemVertexStream.Color,
            ParticleSystemVertexStream.UV,
            ParticleSystemVertexStream.StableRandomX,
        };

        // Un écho à relancer : une copie de la faille, cachée là où elle est partie.
        struct PendingEcho
        {
            public float Time;
            public MatrixRift Rift;
            public ShotGrade Grade;
            public float Damage;
            public float Speed;
            public int Volley;
            public int Fusions;
        }

        readonly Dictionary<Health, float> m_LastHit = new Dictionary<Health, float>();
        readonly Dictionary<Object, float> m_LastTrigger = new Dictionary<Object, float>();
        readonly List<EnemyProjectile> m_Projectiles = new List<EnemyProjectile>();
        readonly List<MatrixRift> m_Rifts = new List<MatrixRift>();
        readonly List<MatrixRift> m_Struck = new List<MatrixRift>();
        readonly List<Enemy> m_Targets = new List<Enemy>();
        readonly List<Vector3> m_Cut = new List<Vector3>();
        readonly List<PendingEcho> m_Echoes = new List<PendingEcho>();
        readonly List<ShockWave> m_Waves = new List<ShockWave>();
        float m_NextWave;

        Bow m_Bow;
        BowDefinition m_Definition;
        FloatingGrab m_Grab;
        Transform m_BladeStart;
        Transform m_BladeTip;
        Renderer[] m_KatanaRenderers = System.Array.Empty<Renderer>();
        MaterialPropertyBlock m_Block;
        MaterialPropertyBlock m_GloveBlock;
        BladeTrail m_Trail;
        Material m_FallbackTear;
        float m_KatanaSide = 1f;

        // La main de la matrice : la main de l'arc, son gant.
        IXRInteractor m_GloveHand;
        GlovedHand m_Glove;
        Renderer m_GloveRenderer;
        float m_NextGloveTry;
        float m_GloveGlitch;

        // La coupe en cours : ses points par rapport à la poitrine du joueur, dans le repère du XR Origin (se déplacer
        // pendant le coup ne la déforme pas et ne la fait pas), et la faille qui grandit avec elle.
        bool m_Cutting;
        MatrixRift m_Growing;
        float m_CutStart;
        float m_CutSlow;
        float m_CutLength;
        Vector3 m_PreviousLocalTip;
        bool m_HasLocalTip;
        Vector3 m_PreviousFist;
        bool m_HasFist;
        ArmSwingLocomotion m_ArmSwing;
        float m_NextArmSwingLookup;

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

        /// <summary>Le katana de l'arc (sa définition, ses multiplicateurs, ses qualités de tir).</summary>
        public static Katana Create(Bow bow, BowDefinition definition)
        {
            var weapon = new GameObject("Katana").AddComponent<Katana>();
            weapon.m_Bow = bow;
            weapon.m_Definition = definition;
            return weapon;
        }

        void OnEnable()
        {
            EnemyProjectile.Launched += OnProjectileLaunched;
            Enemy.Killed += OnEnemyKilled;
        }

        void OnDisable()
        {
            EnemyProjectile.Launched -= OnProjectileLaunched;
            Enemy.Killed -= OnEnemyKilled;
        }

        void OnDestroy()
        {
            if (m_Grab != null)
                Destroy(m_Grab.gameObject);
            if (m_Trail != null)
                Destroy(m_Trail.gameObject);
            RestoreGlove();

            // Une faille en vol finit sa course ; celle du coup en cours et celles qui attendaient se referment, les échos ne
            // partent pas.
            AbortCut();
            CloseRifts();
            foreach (var echo in m_Echoes)
            {
                if (echo.Rift != null)
                    Destroy(echo.Rift.gameObject);
            }

            m_Echoes.Clear();
            ClearWaves();
            if (m_FallbackTear != null)
                Destroy(m_FallbackTear);
        }

        void OnProjectileLaunched(EnemyProjectile projectile)
        {
            if (projectile != null)
                m_Projectiles.Add(projectile);
        }

        bool KatanaActive => m_Desktop || (m_Grab != null && m_Grab.Held);
        IXRInteractor KatanaHand => m_Desktop || m_Grab == null ? null : m_Grab.Holder;
        float DamageMultiplier => m_Bow != null ? m_Bow.DamageMultiplier : 1f;
        ShotTuning Tuning => m_Bow != null && m_Bow.ShotTuning != null ? m_Bow.ShotTuning : ShotTuning.Fallback;
        Vector3 BladeStart => m_BladeStart != null ? m_BladeStart.position : m_Grab.transform.TransformPoint(k_DefaultBladeStart);
        Vector3 BladeTip => m_BladeTip != null ? m_BladeTip.position : m_Grab.transform.TransformPoint(k_DefaultBladeTip);

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

            // La main de la matrice est la main de l'arc ; le katana est de l'autre côté.
            m_KatanaSide = rig.BowHand == InteractorHandedness.Right ? -1f : 1f;

            var katana = new GameObject("Katana").transform;
            katana.SetParent(transform, false);
            AddModel(katana, m_Definition.katanaModel);
            m_BladeStart = FindChild(katana, "Garde");
            m_BladeTip = FindChild(katana, "Pointe");
            m_Grab = FloatingGrab.Create(katana.gameObject, Vector3.zero, k_KatanaAttach, k_KatanaBoxCenter, k_KatanaBoxSize,
                                         Vector3.Scale(k_KatanaHolster, new Vector3(m_KatanaSide, 1f, 1f)),
                                         Quaternion.Euler(0f, 0f, -k_KatanaHolsterTilt * m_KatanaSide));
            m_Grab.Grabbed += interactor =>
            {
                m_HasPrevious = false;
                m_HasLocalTip = false;
            };
            m_Grab.SnapToHolster();
            m_KatanaRenderers = katana.GetComponentsInChildren<Renderer>(true);
            m_Block = new MaterialPropertyBlock();
            m_Trail = BladeTrail.Create(m_Definition.katanaTrailMaterial);
            AttachGlove();
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

        // Le gant de la main de l'arc passe dans la matrice. Les mains gantées se construisent parfois après le katana : on
        // réessaie chaque seconde.
        void AttachGlove()
        {
            m_NextGloveTry = Time.time + 1f;
            var rig = PlayerRig.Instance;
            if (rig == null || !rig.TryGetHand(rig.BowHand, out var hand) || hand == null)
                return;

            m_GloveHand = hand;
            if (m_Glove != null || m_Hidden || m_Desktop)
                return;
            var glove = GlovedHand.For(hand);
            var renderer = glove != null ? glove.SetGloveMaterial(m_Definition.katanaGloveMaterial) : null;
            if (renderer == null)
                return;
            m_Glove = glove;
            m_GloveRenderer = renderer;
        }

        void RestoreGlove()
        {
            if (m_Glove != null)
            {
                m_Glove.SetGloveMaterial(null);
                if (m_GloveRenderer != null)
                    m_GloveRenderer.SetPropertyBlock(null);
            }

            m_Glove = null;
            m_GloveRenderer = null;
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

            var desktop = DesktopControls.Instance != null && DesktopControls.Instance.IsActive;
            if (desktop != m_Desktop)
                SetDesktop(desktop);

            var hidden = m_Bow != null && m_Bow.IsStowed;
            if (hidden != m_Hidden)
                SetHidden(hidden);

            if (m_Glove == null && Time.time >= m_NextGloveTry)
                AttachGlove();

            var deltaTime = Time.deltaTime;
            m_Glitch = Mathf.MoveTowards(m_Glitch, 0f, deltaTime * 4f);
            m_GloveGlitch = Mathf.MoveTowards(m_GloveGlitch, 0f, deltaTime * 4f);
            ApplyBladeGlitch();
            if (m_Hidden)
                return;

            // En pause (menu ouvert en cours de partie), on ne coupe rien et la main ne lance rien. Au casque, les vitesses de
            // la lame et de la main se mesurent en temps réel : pendant un ralenti (un lancer parfait, un coffre), un geste
            // lent ne devient pas un coup.
            if (!SlowMotion.IsPaused)
            {
                if (m_Desktop)
                {
                    UpdateDesktop(deltaTime);
                }
                else
                {
                    var realDelta = Time.unscaledDeltaTime;
                    UpdateMelee(realDelta);
                    UpdateCut(realDelta);
                    UpdatePunch(realDelta);
                }
            }

            UpdateRifts();
            UpdateWaves();
            UpdateEchoes();
            UpdateGlove();
        }

        void SetHidden(bool hidden)
        {
            m_Hidden = hidden;
            m_Grab.Release();
            m_Grab.gameObject.SetActive(!hidden);
            if (!hidden && !m_Desktop)
                m_Grab.SnapToHolster();

            // Rangé (construction, dimension) : le gant redevient du cuir, les failles ouvertes se referment.
            if (hidden)
                RestoreGlove();
            else
                AttachGlove();
            AbortCut();
            CloseRifts();
            ClearWaves();
            if (m_Trail != null)
                m_Trail.Clear();
            m_HasPrevious = false;
            m_HasLocalTip = false;
            m_HasFist = false;
        }

        void SetDesktop(bool desktop)
        {
            m_Desktop = desktop;
            var camera = DesktopControls.Instance != null ? DesktopControls.Instance.CameraTransform : null;
            m_Grab.Suspend(desktop);
            if (desktop && camera != null)
                m_Grab.transform.SetParent(camera, false);
            else
                m_Grab.transform.SetParent(transform, true);
            if (!desktop)
                m_Grab.SnapToHolster();

            // Sans casque, pas de main : le gant n'est pas montré (le clic droit frappe les failles).
            if (desktop)
                RestoreGlove();
            else
                AttachGlove();
            AbortCut();
            m_SwingTime = -1f;
            m_HasPrevious = false;
            m_HasLocalTip = false;
            m_HasFist = false;
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
                if (m_Trail != null && !KatanaActive)
                    m_Trail.Clear();
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

        // La traînée : éteinte au repos, elle s'allume à partir de la vitesse d'un coup, pleine quand la lame tranche ; de la
        // couleur de la faille en cours.
        void UpdateTrail(Vector3 bladeBase, Vector3 tip, float speed)
        {
            if (m_Trail == null)
                return;
            var strength = m_Cutting ? 1f : Mathf.InverseLerp(k_HitSpeed * 0.6f, k_CutStartSpeed, speed) * 0.6f;
            var color = m_Cutting && m_Growing != null ? Tuning.Get(GradeOf(m_Growing)).color : k_TrailColor;
            m_Trail.Add(bladeBase, tip, strength, color);
        }

        // Le coup : la lame (une capsule) et le chemin de la pointe depuis l'image d'avant ; chaque ennemi touché une fois
        // par délai, par sa zone de touche (la tête compte double, comme pour une flèche).
        void Strike(Vector3 bladeBase, Vector3 tip, Vector3 previousTip, Vector3 velocity, float speed)
        {
            var damage = m_Definition.katanaDamage * DamageMultiplier * Mathf.Lerp(1f, 1.6f, Mathf.InverseLerp(k_HitSpeed, k_HitFastSpeed, speed));
            var direction = velocity / speed;
            var count = Physics.OverlapCapsuleNonAlloc(bladeBase, tip, k_BladeRadius, s_Overlaps, ~0, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
                TryHit(s_Overlaps[i], s_Overlaps[i].ClosestPoint(tip), direction, damage, 0f, KatanaHand);

            var step = tip - previousTip;
            var distance = step.magnitude;
            if (distance < 1e-4f)
                return;
            var hits = Physics.RaycastNonAlloc(previousTip, step / distance, s_Hits, distance, ~0, QueryTriggerInteraction.Collide);
            for (var i = 0; i < hits; i++)
                TryHit(s_Hits[i].collider, s_Hits[i].point, direction, damage, 0f, KatanaHand);
        }

        // Un coup (de lame ou de poing) qui touche un ennemi par sa zone de touche ; le poing le repousse aussi.
        void TryHit(Collider collider, Vector3 point, Vector3 direction, float damage, float push, IXRInteractor hand)
        {
            if (collider == null)
                return;
            if (!collider.TryGetComponent(out Hitbox hitbox))
            {
                TryTrigger(collider, point, direction, damage, hand);
                return;
            }

            var owner = hitbox.Owner;
            if (owner == null || !owner.IsAlive)
                return;
            var enemy = owner.GetComponentInParent<Enemy>();
            var boss = owner.GetComponentInParent<Boss>();
            if (enemy == null && boss == null)
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
            if (push > 0f && enemy != null && boss == null)
                enemy.Push(direction * push);
            Sfx.Play(m_Definition.katanaHitClip, point, 0.9f, Random.Range(0.9f, 1.1f));
            Haptics.Pulse(hand, 0.8f, 0.08f);
        }

        // Un coup dans ce qui réagit aux flèches sans être un ennemi (23 octobre : au katana, on ne pouvait pas lancer de
        // vague) : le gong, une cible d'entraînement, une cible de la victoire contre MK, une boule de feu. Il compte comme une
        // flèche tirée, une fois par seconde au plus pour chaque objet (on ne fait pas venir le boss par mégarde en frappant le
        // gong deux fois d'un même geste). Pas les boutons des menus ni les panneaux de difficulté : ils se visent au rayon de
        // la main.
        void TryTrigger(Collider collider, Vector3 point, Vector3 direction, float damage, IXRInteractor hand)
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
            Haptics.Pulse(hand, 0.7f, 0.08f);
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

        // Le joueur court en balançant les bras (ArmSwingLocomotion) : ses bras balancent, ils ne tranchent pas.
        bool Running
        {
            get
            {
                if (m_ArmSwing == null && Time.time >= m_NextArmSwingLookup)
                {
                    m_NextArmSwingLookup = Time.time + 2f;
                    m_ArmSwing = FindAnyObjectByType<ArmSwingLocomotion>();
                }

                return m_ArmSwing != null && m_ArmSwing.IsRunning;
            }
        }

        // La pointe va vite par rapport au joueur, dans son champ de vision : la coupe commence, et la faille suit le bout de
        // la lame. Tout se mesure depuis sa poitrine, dans le repère du XR Origin : ses déplacements (joystick, pas dans la
        // pièce) ne comptent pas, et en courant aux bras, aucune coupe ne commence.
        void UpdateCut(float deltaTime)
        {
            var rig = PlayerRig.Instance;
            var origin = rig != null && rig.Origin != null ? rig.Origin.transform : null;
            var head = rig != null ? rig.Head : null;
            if (!KatanaActive || origin == null || head == null || deltaTime <= 0f)
            {
                AbortCut();
                m_HasLocalTip = false;
                return;
            }

            var chest = head.position + Vector3.down * k_ChestDrop;
            var localChest = origin.InverseTransformPoint(chest);
            var tip = BladeTip;
            var bladeBase = BladeStart;
            var localTip = origin.InverseTransformPoint(tip) - localChest;
            var speed = m_HasLocalTip ? Vector3.Distance(localTip, m_PreviousLocalTip) / deltaTime : 0f;
            m_PreviousLocalTip = localTip;
            var hadTip = m_HasLocalTip;
            m_HasLocalTip = true;
            UpdateTrail(bladeBase, tip, speed);
            if (!hadTip)
                return;

            var world = tip;
            var point = localTip;
            var inView = Vector3.Angle(head.forward, world - head.position) <= k_ViewAngle;
            if (!m_Cutting)
            {
                if (speed >= k_CutStartSpeed && inView && !Running)
                    BeginCut(point, world, head.position, origin, tip);
                return;
            }

            // La lame fait demi-tour : cette faille finit là où elle a tourné, une autre commence aussitôt.
            if (Turned(point))
            {
                var turn = m_Cut[m_Cut.Count - 1];
                var turnWorld = origin.TransformPoint(localChest + turn);
                EndCut(head.position);
                if (inView)
                {
                    BeginCut(turn, turnWorld, head.position, origin, tip);
                    AddCutPoint(point, world, head.position);
                }

                return;
            }

            AddCutPoint(point, world, head.position);
            m_CutSlow = speed < k_CutEndSpeed ? m_CutSlow + deltaTime : 0f;
            if (m_CutSlow >= k_CutEndHold || !inView || Time.time - m_CutStart > k_MaxCutTime || m_CutLength >= k_MaxCutLength)
                EndCut(head.position);
        }

        // La faille naît dès que la lame part, au bout de la lame ; elle suit le joueur (XR Origin).
        void BeginCut(Vector3 localPoint, Vector3 worldPoint, Vector3 viewer, Transform parent, Vector3 tip)
        {
            m_Cut.Clear();
            m_Cut.Add(localPoint);
            m_Cutting = true;
            m_CutStart = Time.time;
            m_CutSlow = 0f;
            m_CutLength = 0f;
            m_Growing = MatrixRift.Begin(parent, worldPoint, viewer, TearMaterial, Random.Range(1, 1 << 20));
            m_Growing.SetTint(Tuning.Get(ShotGrade.Ok).color);
            Sfx.Play(m_Definition.katanaSwooshClip, tip, 0.75f, Random.Range(0.92f, 1.12f));
            Haptics.Pulse(KatanaHand, 0.25f, 0.04f);
        }

        void AddCutPoint(Vector3 point, Vector3 world, Vector3 viewer)
        {
            var last = m_Cut[m_Cut.Count - 1];
            var step = Vector3.Distance(last, point);
            if (step < k_CutSpacing)
                return;
            m_Cut.Add(point);
            m_CutLength += step;
            if (m_Growing != null)
            {
                m_Growing.Extend(world, viewer);
                m_Growing.SetTint(Tuning.Get(GradeOf(m_Growing)).color);
            }
        }

        // La lame fait demi-tour : la direction de ce nouveau pas tourne de plus de 110° par rapport aux 5 derniers
        // centimètres de la coupe.
        bool Turned(Vector3 point)
        {
            if (m_Cut.Count < 2)
                return false;
            var last = m_Cut[m_Cut.Count - 1];
            var step = point - last;
            if (step.magnitude < k_CutSpacing)
                return false;

            var travelled = 0f;
            var from = last;
            for (var i = m_Cut.Count - 2; i >= 0; i--)
            {
                travelled += Vector3.Distance(m_Cut[i], m_Cut[i + 1]);
                from = m_Cut[i];
                if (travelled >= k_TurnWindow)
                    break;
            }

            if (travelled < k_TurnWindow * 0.5f)
                return false;
            return Vector3.Angle(last - from, step) > k_TurnAngle;
        }

        // La coupe finie : une vraie taille (assez longue, pas un va-et-vient) ouvre sa faille ; sinon, la fissure grésille
        // et se referme.
        void EndCut(Vector3 viewer)
        {
            m_Cutting = false;
            var rift = m_Growing;
            m_Growing = null;
            if (rift == null)
                return;

            var valid = m_CutLength >= k_MinCutLength && m_Cut.Count >= 3 && Vector3.Distance(m_Cut[0], m_Cut[m_Cut.Count - 1]) >= k_MinCutChord;
            if (!valid)
            {
                if (m_CutLength < k_SilentCut)
                {
                    Destroy(rift.gameObject);
                    return;
                }

                rift.Fizzle();
                Sfx.Play(m_Definition.katanaCrackClip, rift.transform.position, 0.3f, Random.Range(1.8f, 2.1f));
                return;
            }

            rift.Complete(viewer);
            OpenRift(rift, viewer);
        }

        void AbortCut()
        {
            m_Cutting = false;
            if (m_Growing != null)
                m_Growing.Fizzle();
            m_Growing = null;
        }

        // ---------------------------------------------------------------- les failles

        // Une faille ouverte : elle fusionne avec celles qu'elle croise, sinon elle attend avec les autres (5 au plus).
        void OpenRift(MatrixRift rift, Vector3 viewer)
        {
            for (var i = m_Rifts.Count - 1; i >= 0; i--)
            {
                if (m_Rifts[i] == null || !m_Rifts[i].IsOpen)
                    m_Rifts.RemoveAt(i);
            }

            MakeRoom(viewer);
            var forward = (rift.transform.position - viewer).normalized;
            var fused = TryFuse(rift, viewer, forward, out var crossing);
            if (fused == null)
            {
                m_Rifts.Add(rift);
                rift.SetTint(Tuning.Get(GradeOf(rift)).color);
                Sfx.Play(m_Definition.katanaCrackClip, rift.transform.position, 0.7f, Random.Range(0.9f, 1.05f));
                Haptics.Pulse(KatanaHand, 0.5f, 0.06f);
                m_Glitch = Mathf.Max(m_Glitch, 0.5f);
                return;
            }

            // La fusion se voit (24 octobre, après le test : avant, la faille changeait seulement de couleur) : la faille
            // fusionnée passe au cyan et bondit, une onde de lumière part du croisement le long de tous ses brins, un nœud
            // lumineux y reste (MatrixRift.Absorb), et des 0 et des 1 jaillissent du croisement. Un bug, la manette qui le fait
            // sentir.
            fused.SetTint(k_FusionColor);
            m_Glitch = 1f;
            m_GloveGlitch = 1f;
            if (m_Definition.katanaDigitsMaterial != null)
                DigitBurst(new Bounds(crossing, Vector3.one * 0.12f), k_FusionDigits);
            Sfx.Play(m_Definition.katanaSignClip, crossing, 0.8f, Random.Range(0.85f, 0.95f));
            Sfx.Play(m_Definition.katanaCrackClip, crossing, 0.9f, 0.7f);
            FloatingText.Spawn(crossing + Vector3.up * 0.15f, fused.StrandCount > 2 ? $"Fusion ×{fused.StrandCount} !" : "Fusion !",
                               k_FusionColor, 0.7f, 0.9f);
            Haptics.Pulse(KatanaHand, 0.8f, 0.1f);
            Haptics.Pulse(m_GloveHand, 0.4f, 0.06f);
        }

        // Les failles ouvertes que la nouvelle croise (vues depuis les yeux du joueur) fusionnent avec elle, en chaîne : la
        // plus ancienne garde sa place et prend les autres, tant que la fusion ne dépasse pas 5 failles. Renvoie la faille
        // fusionnée (et le dernier croisement), ou null.
        MatrixRift TryFuse(MatrixRift rift, Vector3 viewer, Vector3 forward, out Vector3 crossing)
        {
            MatrixRift result = null;
            var current = rift;
            crossing = rift.transform.position;
            for (var i = 0; i < m_Rifts.Count; i++)
            {
                var other = m_Rifts[i];
                if (other == null || other == current || !other.IsOpen || other.StrandCount + current.StrandCount > k_MaxFusion ||
                    !other.Crosses(current, viewer, forward, out var point))
                    continue;
                crossing = point;
                other.Absorb(current, viewer, point);
                if (result != null && result != other)
                    m_Rifts.Remove(result);
                result = other;
                current = other;
                i = -1;
            }

            return result;
        }

        // De la place pour une faille de plus : 5 en tout, fusionnées comprises. La plus ancienne se referme, même si elle est
        // dans une fusion (elle s'en détache et se referme, la fusion garde les autres).
        void MakeRoom(Vector3 viewer)
        {
            while (true)
            {
                var total = 0;
                MatrixRift oldest = null;
                foreach (var open in m_Rifts)
                {
                    if (open == null || !open.IsOpen)
                        continue;
                    total += open.StrandCount;
                    if (oldest == null || open.OldestStrandTime < oldest.OldestStrandTime)
                        oldest = open;
                }

                if (total < k_MaxRifts || oldest == null)
                    return;
                if (oldest.StrandCount > 1)
                {
                    var detached = oldest.DetachOldest(viewer);
                    if (detached != null)
                        detached.Close();
                    oldest.SetTint(oldest.StrandCount > 1 ? k_FusionColor : Tuning.Get(GradeOf(oldest)).color);
                }
                else
                {
                    oldest.Close();
                    m_Rifts.Remove(oldest);
                }
            }
        }

        // Chaque faille ouverte prend la couleur de la qualité de son tir, et se referme au bout de son temps, là où la lame
        // l'a ouverte (24 octobre, après le test : elle ne glisse plus vers le joueur, l'onde de choc va la chercher).
        void UpdateRifts()
        {
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

                rift.SetTint(rift.StrandCount > 1 ? k_FusionColor : Tuning.Get(GradeOf(rift)).color);
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

        // La qualité selon la taille : plus le coup est long, meilleur est le tir ; des failles fusionnées sont parfaites.
        ShotGrade GradeOf(MatrixRift rift)
        {
            if ((m_Bow != null && m_Bow.ForcePerfect) || rift.StrandCount > 1)
                return ShotGrade.Perfect;
            var gold = m_Bow != null ? Mathf.Max(0.1f, m_Bow.GoldWidthMultiplier) : 1f;
            var band = m_Bow != null ? Mathf.Max(0.1f, m_Bow.BandWidthMultiplier) : 1f;
            var length = rift.Length;
            if (length >= k_PerfectLength / gold)
                return ShotGrade.Perfect;
            if (length >= k_HeldLength / band)
                return ShotGrade.Held;
            return length >= k_GoodLength / band ? ShotGrade.Good : ShotGrade.Ok;
        }

        // ---------------------------------------------------------------- la main de la matrice

        // Le poing de la main de la matrice (le creux du gant, ou devant la manette sans gant).
        bool TryGetFist(out Vector3 fist)
        {
            if (m_Glove != null && m_Glove.isActiveAndEnabled)
            {
                var frame = m_Glove.WristFrame;
                var scale = m_Glove.FingerLength / k_GloveLength;
                fist = m_Glove.transform.TransformPoint(frame.position + frame.rotation * (k_Fist * scale));
                return true;
            }

            if (m_GloveHand is Component hand && hand != null)
            {
                fist = hand.transform.TransformPoint(k_ControllerFist);
                return true;
            }

            fist = Vector3.zero;
            return false;
        }

        // Un coup de poing de la main de la matrice (par rapport à la poitrine du joueur : se déplacer ne compte pas) lance
        // une onde de choc devant lui, qui propulse les failles qu'elle atteint ; assez vite, il frappe aussi les ennemis à
        // portée de poing.
        void UpdatePunch(float deltaTime)
        {
            // Le katana pris de la main de l'arc : cette main tranche, elle ne frappe pas du poing.
            var rig = PlayerRig.Instance;
            var origin = rig != null && rig.Origin != null ? rig.Origin.transform : null;
            var head = rig != null ? rig.Head : null;
            var holdsKatana = KatanaHand != null && KatanaHand == m_GloveHand;
            if (origin == null || head == null || deltaTime <= 0f || holdsKatana || !TryGetFist(out var fist))
            {
                m_HasFist = false;
                return;
            }

            var local = origin.InverseTransformPoint(fist) - origin.InverseTransformPoint(head.position + Vector3.down * k_ChestDrop);
            var velocity = m_HasFist ? (local - m_PreviousFist) / deltaTime : Vector3.zero;
            var speed = velocity.magnitude;
            m_PreviousFist = local;
            m_HasFist = true;

            if (speed >= k_FistSpeed)
            {
                var direction = origin.TransformDirection(velocity / speed);
                var damage = m_Definition.katanaDamage * k_FistDamage * DamageMultiplier;
                var count = Physics.OverlapSphereNonAlloc(fist, k_FistRadius, s_Overlaps, ~0, QueryTriggerInteraction.Collide);
                for (var i = 0; i < count; i++)
                    TryHit(s_Overlaps[i], s_Overlaps[i].ClosestPoint(fist), direction, damage, k_FistPush, m_GloveHand);
            }

            // L'onde de choc : un vrai coup de poing, qui part en tendant le bras (le poing s'éloigne de la poitrine), pas le
            // bras qui balance pour courir.
            if (speed < k_WavePunchSpeed || Time.time < m_NextWave || Running)
                return;
            var punch = origin.TransformDirection(velocity / speed);
            var outward = fist - (head.position + Vector3.down * k_ChestDrop);
            if (outward.sqrMagnitude > 1e-4f && Vector3.Dot(punch, outward.normalized) >= k_WaveExtension)
                SpawnWave(fist, punch);
        }

        // Une onde de choc part de origin vers direction : un anneau de lumière qui s'élargit en filant (ShockWave).
        void SpawnWave(Vector3 origin, Vector3 direction)
        {
            m_NextWave = Time.time + k_WaveCooldown;
            var wave = ShockWave.Spawn(origin, direction, m_Definition.katanaTrailMaterial, k_WaveColor, k_WaveTravel, k_WaveRange,
                                       k_WaveRadius, k_WaveGrowth);
            if (wave != null)
                m_Waves.Add(wave);
            Sfx.Play(m_Definition.katanaSwooshClip, origin, 0.8f, Random.Range(0.55f, 0.65f));
            Haptics.Pulse(m_GloveHand, 0.6f, 0.08f);
            m_GloveGlitch = Mathf.Max(m_GloveGlitch, 0.6f);
        }

        void ClearWaves()
        {
            foreach (var wave in m_Waves)
            {
                if (wave != null)
                    Destroy(wave.gameObject);
            }

            m_Waves.Clear();
        }

        // Chaque onde de choc propulse les failles ouvertes qu'elle atteint en filant (une fois chacune), vers ce que le joueur
        // regarde.
        void UpdateWaves()
        {
            if (m_Waves.Count == 0)
                return;
            var rig = PlayerRig.Instance;
            var view = m_Desktop && DesktopControls.Instance != null ? DesktopControls.Instance.CameraTransform : rig != null ? rig.Head : null;
            for (var i = m_Waves.Count - 1; i >= 0; i--)
            {
                var wave = m_Waves[i];
                if (wave == null)
                {
                    m_Waves.RemoveAt(i);
                    continue;
                }

                m_Struck.Clear();
                foreach (var rift in m_Rifts)
                {
                    if (rift != null && rift.IsOpen && wave.TryHit(rift))
                        m_Struck.Add(rift);
                }

                if (m_Struck.Count > 0)
                    LaunchRifts(m_Struck, view != null ? view.position : wave.transform.position, view != null ? view.forward : wave.Direction);
            }
        }

        // Le gant bat tant qu'une faille attend, et bugue quand il en lance une.
        void UpdateGlove()
        {
            if (m_GloveRenderer == null)
                return;
            var waiting = false;
            foreach (var rift in m_Rifts)
                waiting |= rift != null && rift.IsOpen;
            if (m_GloveBlock == null)
                m_GloveBlock = new MaterialPropertyBlock();
            m_GloveRenderer.GetPropertyBlock(m_GloveBlock);
            m_GloveBlock.SetFloat(k_PulseId, waiting ? 1f : 0f);
            m_GloveBlock.SetFloat(k_GlitchId, m_GloveGlitch);
            m_GloveRenderer.SetPropertyBlock(m_GloveBlock);
        }

        // ---------------------------------------------------------------- le lancer

        // Les failles frappées partent vers les ennemis les plus proches du centre de la vue (une par ennemi, tant qu'il y en
        // a) ; chacune compte comme un tir. Le multitir en lance des copies, l'écho les relance un instant après.
        void LaunchRifts(List<MatrixRift> rifts, Vector3 viewOrigin, Vector3 viewForward)
        {
            FindTargets(viewOrigin, viewForward);
            var upgrades = PlayerUpgrades.Instance;
            var arrows = SpecialArrows.Instance;
            var next = 0;
            var marked = false;
            foreach (var rift in rifts.ToArray())
            {
                if (rift == null || !rift.IsOpen)
                    continue;

                var grade = GradeOf(rift);
                var modifiers = Tuning.Get(grade);
                var fusions = rift.StrandCount - 1;
                var speed = Mathf.Max(k_MinRiftSpeed, m_Definition.arrowSpeed * modifiers.speed * (m_Bow != null ? m_Bow.SpeedMultiplier : 1f));
                var damage = m_Definition.damage * modifiers.damage * DamageMultiplier * (1f + k_FusionBonus * fusions);
                var origin = rift.transform.position;
                // La corde triple (26 octobre) ajoute ses copies comme le multitir : vers d'autres ennemis, en éventail sinon.
                var volley = 1 + (upgrades != null
                    ? PlayerUpgrades.RollCount(upgrades.MultishotAverageFor(grade)) + Mathf.RoundToInt(upgrades.Total(UpgradeEffect.Triple))
                    : 0);
                var echoes = upgrades != null ? PlayerUpgrades.RollCount(upgrades.EchoAverageFor(grade)) : 0;

                // L'écho : des copies cachées là où elle part, relancées un instant après.
                var delay = arrows != null ? arrows.EchoDelay : 0.25f;
                for (var i = 1; i <= echoes; i++)
                {
                    var copy = rift.Clone();
                    copy.gameObject.SetActive(false);
                    m_Echoes.Add(new PendingEcho
                    {
                        Time = Time.time + delay * i,
                        Rift = copy,
                        Grade = grade,
                        Damage = damage,
                        Speed = speed,
                        Volley = volley,
                        Fusions = fusions,
                    });
                }

                Volley(rift, grade, damage, speed, fusions, volley, viewForward, false, ref next);
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
                        Direction = viewForward,
                    });
                }

                var label = fusions > 0 ? "Fusion !" : modifiers.label;
                if (!string.IsNullOrEmpty(label))
                    FloatingText.Spawn(origin + Vector3.up * 0.15f, label, fusions > 0 ? k_FusionColor : modifiers.color, 0.7f, 0.9f);
                Sfx.Play(m_Definition.katanaLaunchClip, origin, 1f, Random.Range(0.92f, 1.05f));
                if (grade == ShotGrade.Perfect)
                {
                    marked = true;
                    Sfx.Play(m_Definition.katanaSignClip, origin, 0.7f);
                    if (m_Bow != null)
                        Sfx.Play(m_Bow.PerfectClip, origin, 0.7f);
                }
            }

            // Un lancer parfait marque le coup : un très bref ralenti.
            if (marked)
                SlowMotion.HitStop(k_HitStopScale, k_HitStopTime);
            Haptics.Pulse(m_GloveHand, 0.9f, 0.12f);
            Haptics.Pulse(KatanaHand, 0.3f, 0.05f);
            m_Glitch = 1f;
            m_GloveGlitch = 1f;
        }

        // Une faille et ses copies du multitir : chacune vers la cible suivante, en éventail s'il n'y en a plus.
        void Volley(MatrixRift rift, ShotGrade grade, float damage, float speed, int fusions, int count, Vector3 viewForward, bool extra,
                    ref int next)
        {
            for (var k = 0; k < count; k++)
            {
                var piece = k == 0 ? rift : rift.Clone();
                var target = next < m_Targets.Count ? m_Targets[next] : null;
                next++;
                var fan = Quaternion.AngleAxis((k % 2 == 0 ? 1f : -1f) * k_FanAngle * ((k + 1) / 2), Vector3.up) * viewForward;
                var origin = piece.transform.position;
                var direction = target != null ? (target.Center - origin).normalized : fan;
                piece.Launch(BuildShot(grade, damage, speed, fusions, target, direction, extra || k > 0));
            }
        }

        // Ce que les améliorations donnent à une faille lancée : effets à l'impact, perçage, ricochet, auto-visée, déluge ;
        // et l'explosion des failles fusionnées.
        RiftShot BuildShot(ShotGrade grade, float damage, float speed, int fusions, Enemy target, Vector3 direction, bool extra)
        {
            var upgrades = PlayerUpgrades.Instance;
            var arrows = SpecialArrows.Instance;
            var modifiers = Tuning.Get(grade);
            var effects = arrows != null ? arrows.RollEffects(grade) : SpecialArrows.Effect.None;
            var color = fusions > 0 ? k_FusionColor : modifiers.color;
            if (effects != SpecialArrows.Effect.None && arrows != null)
                color = Color.Lerp(color, arrows.ColorOf(effects), 0.6f);
            return new RiftShot
            {
                Direction = direction,
                Speed = speed,
                Target = target,
                Damage = damage,
                Grade = grade,
                Color = color,
                HitClip = m_Definition.katanaHitClip,
                IsExtra = extra,
                Pierce = upgrades != null ? PlayerUpgrades.RollCount(upgrades.PierceAverageFor(grade)) : 0,
                Ricochets = upgrades != null ? upgrades.RicochetCount : 0,
                Reacquire = upgrades != null && upgrades.HomingTurnRate > 0f,
                Splits = !extra && upgrades != null ? PlayerUpgrades.RollCount(upgrades.DelugeAverageFor(grade)) : 0,
                Effects = effects,
                BlastRadius = fusions > 0 ? Mathf.Min(k_MaxBlastRadius, k_BlastRadius + k_BlastRadiusStep * (fusions - 1)) : 0f,
                BlastDamage = damage * k_BlastShare,
            };
        }

        // Les échos partent à leur heure, vers ce que le joueur regarde à ce moment-là.
        void UpdateEchoes()
        {
            if (m_Echoes.Count == 0)
                return;
            var rig = PlayerRig.Instance;
            var head = m_Desktop && DesktopControls.Instance != null ? DesktopControls.Instance.CameraTransform : rig != null ? rig.Head : null;
            for (var i = m_Echoes.Count - 1; i >= 0; i--)
            {
                var echo = m_Echoes[i];
                if (Time.time < echo.Time)
                    continue;
                m_Echoes.RemoveAt(i);
                if (echo.Rift == null)
                    continue;

                echo.Rift.gameObject.SetActive(true);
                var forward = head != null ? head.forward : echo.Rift.transform.forward;
                FindTargets(head != null ? head.position : echo.Rift.transform.position, forward);
                var next = 0;
                Volley(echo.Rift, echo.Grade, echo.Damage, echo.Speed, echo.Fusions, echo.Volley, forward, true, ref next);
                Sfx.Play(m_Definition.katanaLaunchClip, echo.Rift.transform.position, 0.7f, 1.12f);
            }
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

        // ---------------------------------------------------------------- l'impact

        // Un ennemi tué par une faille se défait en 0 et en 1 : son corps disparaît dans une gerbe de chiffres verts (un boss
        // garde son corps).
        void OnEnemyKilled(Enemy enemy, DamageInfo info)
        {
            if (enemy == null || !(info.Source is MatrixRift) || m_Definition == null || m_Definition.katanaDigitsMaterial == null)
                return;

            var renderers = enemy.GetComponentsInChildren<Renderer>();
            var bounds = new Bounds(enemy.Center, Vector3.one * 0.8f);
            var first = true;
            foreach (var renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || renderer is ParticleSystemRenderer)
                    continue;
                if (first)
                    bounds = renderer.bounds;
                else
                    bounds.Encapsulate(renderer.bounds);
                first = false;
            }

            DigitBurst(bounds);
            if (enemy.GetComponentInParent<Boss>() == null && enemy.GetComponentInChildren<Boss>() == null)
            {
                foreach (var renderer in renderers)
                {
                    if (renderer != null && !(renderer is ParticleSystemRenderer))
                        renderer.enabled = false;
                }
            }

            Sfx.Play(m_Definition.katanaSignClip, bounds.center, 0.35f, Random.Range(1.2f, 1.5f));
        }

        // Une gerbe de 0 et de 1 dans ces bornes : de 40 à 140 chiffres selon la hauteur, fois share (la moitié à une fusion).
        void DigitBurst(Bounds bounds, float share = 1f)
        {
            var go = new GameObject("Chiffres de la matrice");
            go.transform.position = bounds.center;
            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.duration = 1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.045f, 0.08f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.25f, 1f, 0.45f), new Color(0.7f, 1f, 0.75f));
            main.gravityModifier = -0.12f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 160;

            var size = Vector3.Max(bounds.size * 0.8f, Vector3.one * 0.3f);
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            var count = Mathf.Clamp(Mathf.RoundToInt(40f * size.y), 40, 140) * share;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Max(1, Mathf.RoundToInt(count))) });

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = size;

            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.35f;
            noise.frequency = 1.5f;

            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = fade;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = m_Definition.katanaDigitsMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.SetActiveVertexStreams(s_DigitStreams);

            particles.Play();
            Destroy(go, 2.2f);
        }

        // ---------------------------------------------------------------- sans casque

        // Clic gauche : un coup de katana devant soi (d'un côté puis de l'autre), qui frappe au corps à corps et ouvre une
        // faille au bout de la lame ; deux coups de suite se croisent et fusionnent. Clic droit : la main lance une onde de
        // choc droit devant, qui propulse les failles vers ce que vise le viseur.
        void UpdateDesktop(float deltaTime)
        {
            var controls = DesktopControls.Instance;
            var camera = controls != null ? controls.CameraTransform : null;
            var mouse = Mouse.current;
            if (camera == null)
                return;

            var rig = PlayerRig.Instance;
            var parent = rig != null && rig.Origin != null ? rig.Origin.transform : null;
            var focus = controls.HasFocus && !controls.RefocusedThisFrame && !DesktopHand.IsTypingText && mouse != null;
            if (focus && mouse.leftButton.wasPressedThisFrame && m_SwingTime < 0f)
            {
                m_SwingTime = 0f;
                m_SwingSide = -m_SwingSide;
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

            var katana = m_Grab.transform;
            katana.localPosition = k_DesktopKatana;
            katana.localRotation = Quaternion.FromToRotation(Vector3.up, bladeDirection);

            if (swinging)
            {
                // Le coup frappe au corps à corps, et la lame ouvre la faille à mesure.
                var tip = BladeTip;
                var speed = 0f;
                if (m_HasPrevious && deltaTime > 0f)
                {
                    var velocity = (tip - m_PreviousTip) / deltaTime;
                    speed = velocity.magnitude;
                    if (speed >= k_HitSpeed)
                    {
                        Strike(BladeStart, tip, m_PreviousTip, velocity, speed);
                        CutProjectiles(BladeStart, tip);
                    }
                }

                m_PreviousTip = tip;
                m_HasPrevious = true;
                var local = camera.InverseTransformPoint(tip);
                if (!m_Cutting)
                    BeginCut(local, tip, camera.position, parent, tip);
                else
                    AddCutPoint(local, tip, camera.position);
                UpdateTrail(BladeStart, tip, Mathf.Max(speed, k_CutStartSpeed));

                if (m_SwingTime >= k_DesktopSwing)
                {
                    m_SwingTime = -1f;
                    EndCut(camera.position);
                }
            }

            if (focus && mouse.rightButton.wasPressedThisFrame && Time.time >= m_NextWave)
                SpawnWave(camera.position + camera.forward * k_DesktopWave, camera.forward);
        }

        // ---------------------------------------------------------------- raccourci de test

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // O : le katana tout de suite (même verrouillé, même pendant la partie) ; O encore : l'arme choisie.
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
                    HeadMessages.Show(entry.bow.displayName + " équipé (test)\n<size=60%>O : retour à l'arme d'avant</size>", entry.color, 3f);
                    return;
                }

                HeadMessages.Show("Pas de katana dans la liste des armes (menu Archery > Katana)", new Color(1f, 0.6f, 0.55f), 3f);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void InstallTestKey()
        {
            var go = new GameObject("Raccourci katana (touche O)");
            DontDestroyOnLoad(go);
            go.AddComponent<TestKey>();
        }
#endif
    }
}
