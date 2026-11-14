using System.Collections.Generic;
using Archery.BowClasses;
using Archery.Bows;
using Archery.Core;
using Archery.Enemies;
using Archery.Grenades;
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
    /// La batte de baseball, une arme du joueur (à la place de l'arc ; 13 novembre, l'idée du joueur, débloquée dès le départ en
    /// attendant d'en faire un secret plus tard dans l'aventure). Elle flotte à côté du joueur, comme le katana, et se prend avec
    /// la poignée ; l'autre main sort les grenades de la ceinture, qui rechargent en 3 s au lieu de 15 (l'Adresse les accélère,
    /// <see cref="GrenadeBelt.Cooldown"/>).
    /// <b>Une grenade lancée puis frappée par la batte</b> file sur l'ennemi le plus proche du centre de la vue, le suit, et
    /// éclate sur lui (<see cref="ArrowGrenade.Bat"/>). <b>La qualité vient du chemin parcouru par la grenade</b>, du lâcher au
    /// coup : lancée haut, elle monte puis redescend, et son chemin compte double ; lancée devant soi, on peut aussi courir après
    /// elle. Plus la qualité est haute, plus la grenade part vite, plus elle a de flammes, et ses flèches ont la qualité du coup
    /// (dégâts, effets des améliorations). La Force augmente les dégâts des flèches, comme pour l'arc.
    /// <b>Au corps à corps</b>, la batte frappe les ennemis (et les repousse), renvoie les projectiles, et sonne le gong comme
    /// le katana. <b>Une batte steampunk</b> (le joueur, le même jour ; le modèle est fait par <c>Editor/BatteBaker</c>) : à chaque
    /// coup, de la vapeur sort de ses cheminées (les repères « Vapeur » du modèle), d'autant plus que le coup est bon.
    /// </summary>
    /// <remarks>
    /// Créée par l'<see cref="Bow"/> quand sa définition est <see cref="BowDefinition.bat"/> : l'arc est caché, et ses
    /// multiplicateurs (améliorations, coffres) servent ici (dégâts, seuils de qualité). Rangée avec lui (construction,
    /// dimensions). Sans casque : clic gauche, un coup de batte devant soi. Raccourci de test : <b>F6</b> (éditeur et builds
    /// Development) équipe la batte, même pendant une vague, et la rend.
    /// </remarks>
    public partial class BaseballBat : MonoBehaviour, IMeleeSource
    {
        // La batte, dans son repère : le milieu de la poignée à l'origine, la batte vers +Y, le bout à 72 cm (84 cm en tout avec
        // le pommeau), si le modèle ne donne pas son repère « Bout ».
        static readonly Vector3 k_DefaultTip = new Vector3(0f, 0.72f, 0f);

        // En main : la poignée dans la paume, la batte le long du poing (la poignée des manettes Quest est inclinée de 45°), comme
        // le katana ; rangée, debout à côté du joueur, du côté de la main qui la prend, comme lui.
        static readonly Quaternion k_Attach = Quaternion.Euler(-45f, 0f, 0f);
        static readonly Vector3 k_BoxCenter = new Vector3(0f, 0.3f, 0f);
        static readonly Vector3 k_BoxSize = new Vector3(0.08f, 0.9f, 0.08f);
        static readonly Vector3 k_Holster = new Vector3(0.22f, -0.62f, 0.45f);
        const float k_HolsterTilt = 10f;

        // La partie qui frappe : de 20 cm au-dessus de la main au bout. On ne sent pas le choc dans la manette, et la grenade est
        // petite : une grenade à 35 cm de l'axe de la batte (16 cm jusqu'à l'essai du 13 novembre au soir, le joueur : « la
        // grenade est très difficile à frapper précisément, on agrandit grandement sa zone »), sur tout le trajet de la batte
        // depuis l'image d'avant (un coup rapide ne passe pas à travers), est frappée, si ce point de la batte va à 2 m/s au
        // moins (un geste mou ne compte pas).
        const float k_SweetStart = 0.2f;
        const float k_GrenadeReach = 0.35f;
        const float k_GrenadeHitSpeed = 2f;
        const int k_SweepSteps = 6;

        // La qualité vient du chemin parcouru par la grenade, du lâcher au coup (13 novembre, le joueur : « plus la grenade va
        // voler longtemps, surtout à une grande distance, plus elle s'approche d'un parfait ») : raté sous 80 cm, moyen jusqu'à
        // 1,6 m, bon jusqu'à 2,6 m, très bon jusqu'à 4 m, parfait au-delà (lancée à 2 m au-dessus de la main, elle monte et
        // redescend : 4 m). Comme pour le katana, la Précision et l'Adresse abaissent le seuil du parfait, la Charge rapide ceux
        // des autres qualités.
        const float k_OkPath = 0.8f;
        const float k_GoodPath = 1.6f;
        const float k_HeldPath = 2.6f;
        const float k_PerfectPath = 4f;

        // Plus le coup est bon, plus la grenade part vite (le joueur : « elle partirait extrêmement rapidement si elle est tirée
        // en parfait ») et plus elle a de flammes.
        const float k_MissSpeed = 14f;
        const float k_OkSpeed = 20f;
        const float k_GoodSpeed = 28f;
        const float k_HeldSpeed = 38f;
        const float k_PerfectSpeed = 55f;

        // Un coup parfait marque le coup : le temps passe à 15 % pendant 0,1 s.
        const float k_HitStopScale = 0.15f;
        const float k_HitStopTime = 0.1f;

        // Le coup au corps à corps, comme le katana : dès que le bout va à 2,5 m/s ; dégâts ×1 à ×1,6 jusqu'à 7 m/s ; un ennemi
        // n'est touché qu'une fois par délai (s) ; le coup le repousse de 6 m/s (pas un boss).
        const float k_HitSpeed = 2.5f;
        const float k_HitFastSpeed = 7f;
        const float k_HitCooldown = 0.4f;
        const float k_BatRadius = 0.04f;
        const float k_Push = 6f;
        const float k_ProjectileReach = 0.14f;
        const float k_TriggerCooldown = 1f;

        // La cible : l'ennemi le plus proche du centre de la vue (à 70° au plus, sinon 120°), à 90 m au plus, comme le katana.
        // Sans cible, la grenade file droit devant le regard, un peu vers le haut.
        // Le regard qui compte (13 novembre au soir, après l'essai du joueur : « on a souvent tendance à regarder la grenade à la
        // place des ennemis, le ciblage est un peu aléatoire ») : la direction horizontale du dernier regard à peu près droit
        // devant (moins de 35° vers le haut). Lever la tête vers la grenade ne la change pas ; on vise en regardant les ennemis
        // avant de lancer, et les angles se comptent à l'horizontale.
        const float k_TargetRange = 90f;
        const float k_TargetAngle = 70f;
        const float k_TargetAngleWide = 120f;
        const float k_AimMaxPitch = 35f;
        const float k_NoTargetLift = 5f;

        // Revu le 14 novembre (le joueur : « la visée visera toujours une cible plus à droite que celle que je veux », et « elle vise
        // parfois des ennemis derrière les décors ») : la grenade qui redescend sur le côté de la batte (et la grenade fantôme) se
        // regarde presque à l'horizontale, et ce regard devenait la visée ; un regard à 25° au plus d'une grenade en l'air ne compte
        // plus. Et seuls les ennemis que la grenade peut atteindre en ligne droite (ni relief, ni décor, ni bâtiment entre eux) sont
        // des cibles.
        const float k_AimIgnoreGrenade = 25f;

        // La vapeur (13 novembre, le joueur : une batte steampunk, « qui dégage de la fumée dès qu'elle frappe la balle ») : des
        // bouffées qui sortent du bout des tuyaux (les repères « Vapeur » du modèle) à chaque coup, de 5 à 28 par tuyau selon la
        // qualité, plus grosses et plus rapides pour un bon coup.
        const int k_SteamMin = 5;
        const int k_SteamMax = 28;
        const float k_MeleeSteam = 0.2f;

        // Les améliorations de la corde, avec la batte (13 novembre, le joueur : « adapte-les toutes ») :
        // - multitir et corde triple : des copies de la grenade frappée, chacune sur l'ennemi suivant (en éventail de 8° s'il n'y
        //   en a plus), comme les failles du katana ;
        // - écho : la grenade éclate encore au même endroit, un instant après (ArrowGrenade) ;
        // - corde de siège : une grenade restée longtemps en l'air a plus de flèches, jusqu'à +50, +75 ou +100 % (niveaux 1, 2,
        //   3) après 2 s de vol ;
        // - corde fantôme (le joueur : « une balle qui flotte devant la batte, qu'on peut refrapper ») : après un vrai coup, une
        //   grenade fantôme flotte devant le joueur ; frappée, elle part comme le dernier vrai coup (sa qualité, sa charge du
        //   siège), +20 % de dégâts ; 1, 2 ou sans fin par vrai coup (niveaux 1, 2, 3), la suivante 1 s après (sans ce délai, le
        //   niveau 3 ferait une grenade parfaite à chaque coup de batte).
        const float k_FanAngle = 8f;
        const float k_SiegeChargeTime = 2f;
        static readonly Vector3 k_SiegeArrows = new Vector3(0.5f, 0.75f, 1f);
        static readonly int[] k_GhostShots = { 1, 2, int.MaxValue };
        const float k_GhostBonus = 1.2f;
        const float k_GhostRespawn = 1f;

        // La grenade fantôme flotte du côté de la main qui tient la batte (le joueur, après le deuxième essai : « pour éviter qu'il
        // ait à frapper en face de lui »). Le 14 novembre (« beaucoup trop à droite et pas assez devant, on la voyait à peine dans la
        // vision périphérique ») : 35 cm sur le côté, 80 cm devant les yeux, 30 cm plus bas (70 et 50 cm avant ; 95 cm droit devant
        // au début). Elle n'apparaît qu'une fois la grenade lancée frappée, 0,4 s après le coup, et disparaît (sans compter) quand
        // une vraie grenade est en l'air (même jour : « elle apparaissait parfois avant qu'on frappe la grenade, et on envoyait les
        // deux en même temps »).
        static readonly Vector3 k_GhostOffset = new Vector3(0.35f, -0.3f, 0.8f);
        const float k_GhostAfterHit = 0.4f;
        static readonly Color k_GhostGlow = new Color(0.35f, 0.95f, 1.4f, 1f);
        static readonly Color k_SiegeColor = new Color(1f, 0.88f, 0.45f);

        // Sans casque : la batte devant la caméra, et un coup (0,25 s) qui balaie la vue en travers.
        static readonly Vector3 k_DesktopBat = new Vector3(0.18f, -0.3f, 0.3f);
        const float k_DesktopSwing = 0.25f;

        static readonly Collider[] s_Overlaps = new Collider[32];
        static readonly RaycastHit[] s_Hits = new RaycastHit[16];
        static readonly List<Enemy> s_Targets = new List<Enemy>();
        static readonly List<ArrowGrenade> s_Volley = new List<ArrowGrenade>();
        static readonly RaycastHit[] s_Sight = new RaycastHit[16];
        static BaseballBat s_Active;

        readonly Dictionary<Health, float> m_LastHit = new Dictionary<Health, float>();
        readonly Dictionary<Object, float> m_LastTrigger = new Dictionary<Object, float>();
        readonly List<EnemyProjectile> m_Projectiles = new List<EnemyProjectile>();
        readonly HashSet<ArrowGrenade> m_Ignored = new HashSet<ArrowGrenade>();
        readonly List<ParticleSystem> m_Steam = new List<ParticleSystem>();

        Bow m_Bow;
        BowDefinition m_Definition;
        FloatingGrab m_Grab;
        Collider m_GrabCollider;
        Transform m_Tip;
        float m_Side = 1f;
        bool m_Built;
        bool m_Desktop;
        bool m_Hidden;

        // La direction de la visée (le dernier regard à peu près droit devant, à l'horizontale).
        Vector3 m_Aim;
        bool m_HasAim;

        // Le trajet de la batte depuis l'image d'avant (repère du monde).
        Vector3 m_PreviousStart;
        Vector3 m_PreviousTip;
        bool m_HasPrevious;

        // Sans casque.
        float m_SwingTime = -1f;
        float m_SwingSide = 1f;

        // Le dernier vrai coup (la grenade fantôme le refait) et la grenade fantôme.
        bool m_HasLastShot;
        ShotGrade m_LastGrade;
        float m_LastSiege = 1f;
        int m_GhostLeft;
        float m_NextGhost;
        ArrowGrenade m_Ghost;
        Material m_GhostMaterial;

        /// <summary>La batte de l'arc (sa définition, ses multiplicateurs, ses qualités de tir).</summary>
        public static BaseballBat Create(Bow bow, BowDefinition definition)
        {
            var weapon = new GameObject("Batte de baseball").AddComponent<BaseballBat>();
            weapon.m_Bow = bow;
            weapon.m_Definition = definition;
            s_Active = weapon;
            return weapon;
        }

        /// <summary>
        /// Le côté de la main qui tient la batte, dans le repère du regard : +1 à droite, -1 à gauche (sans batte en main, le côté
        /// où elle est rangée ; sans batte, à droite). La grenade lancée en l'air retombe de ce côté.
        /// </summary>
        public static float HeldSide
        {
            get
            {
                if (s_Active == null)
                    return 1f;
                if (s_Active.m_Grab != null && s_Active.m_Grab.Holder is XRBaseInputInteractor hand)
                    return hand.handedness == InteractorHandedness.Left ? -1f : 1f;
                return s_Active.m_Side;
            }
        }

        void OnEnable() => EnemyProjectile.Launched += OnProjectileLaunched;

        void OnDisable() => EnemyProjectile.Launched -= OnProjectileLaunched;

        void OnDestroy()
        {
            if (s_Active == this)
                s_Active = null;
            if (m_Grab != null)
                Destroy(m_Grab.gameObject);
            ClearGhost();
            if (m_GhostMaterial != null)
                Destroy(m_GhostMaterial);
            ClearLock();
        }

        void OnProjectileLaunched(EnemyProjectile projectile)
        {
            if (projectile != null)
                m_Projectiles.Add(projectile);
        }

        bool BatActive => m_Desktop || (m_Grab != null && m_Grab.Held);
        IXRSelectInteractor BatHand => m_Desktop || m_Grab == null ? null : m_Grab.Holder;
        float DamageMultiplier => m_Bow != null ? m_Bow.DamageMultiplier : 1f;
        ShotTuning Tuning => m_Bow != null && m_Bow.ShotTuning != null ? m_Bow.ShotTuning : ShotTuning.Fallback;
        Vector3 SweetStart => m_Grab.transform.TransformPoint(0f, k_SweetStart, 0f);
        Vector3 Tip => m_Tip != null ? m_Tip.position : m_Grab.transform.TransformPoint(k_DefaultTip);

        // ---------------------------------------------------------------- construction

        bool TryBuild()
        {
            var rig = PlayerRig.Instance;
            if (rig == null || rig.Head == null || m_Definition == null)
                return false;

            // Comme le katana : la batte dans la main qui tirait la corde, l'autre main (celle de l'arc) pour les grenades.
            m_Side = rig.BowHand == InteractorHandedness.Right ? -1f : 1f;

            var bat = new GameObject("Batte").transform;
            bat.SetParent(transform, false);
            AddModel(bat, m_Definition.batModel);
            m_Tip = FindChild(bat, "Bout");
            m_Grab = FloatingGrab.Create(bat.gameObject, Vector3.zero, k_Attach, k_BoxCenter, k_BoxSize,
                                         Vector3.Scale(k_Holster, new Vector3(m_Side, 1f, 1f)),
                                         Quaternion.Euler(0f, 0f, -k_HolsterTilt * m_Side));
            m_Grab.Grabbed += _ => m_HasPrevious = false;
            m_Grab.SnapToHolster();
            m_GrabCollider = bat.GetComponent<BoxCollider>();
            foreach (var child in bat.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != "Vapeur")
                    continue;
                var steam = CreateSteam(child, m_Definition.batSteamMaterial);
                if (steam != null)
                    m_Steam.Add(steam);
            }

            BuildLook(bat);
            return true;
        }

        // Une bouche de vapeur (au bout d'un tuyau, ou une fuite) : des particules qui partent vers son +Z, grossissent, montent un
        // peu et s'effacent ; elles restent dans le monde (la batte qui bouge ne les entraîne pas). Émises à la demande.
        ParticleSystem CreateSteam(Transform vent, Material material)
        {
            if (material == null)
                return null;

            var go = new GameObject("Bouffées de vapeur");
            go.transform.SetParent(vent, false);
            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(0.95f, 0.95f, 0.95f, 0.55f);
            main.gravityModifier = -0.06f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            var emission = particles.emission;
            emission.rateOverTime = 0f;

            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 18f;
            shape.radius = 0.004f;

            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.8f, 0.8f, 0.82f), 1f) },
                         new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.6f, 0.4f), new GradientAlphaKey(0f, 1f) });
            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = fade;

            var sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 4f));

            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.25f;
            noise.frequency = 1.2f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return particles;
        }

        // Une bouffée de chaque tuyau, de 0 (un petit souffle) à 1 (un parfait : un gros jet).
        void Puff(float strength)
        {
            strength = Mathf.Clamp01(strength);
            var count = Mathf.RoundToInt(Mathf.Lerp(k_SteamMin, k_SteamMax, strength));
            var speed = Mathf.Lerp(0.7f, 1.8f, strength);
            var size = Mathf.Lerp(0.8f, 1.6f, strength);
            foreach (var steam in m_Steam)
            {
                if (steam == null)
                    continue;
                var main = steam.main;
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f * speed, 1.4f * speed);
                main.startSize = new ParticleSystem.MinMaxCurve(0.03f * size, 0.08f * size);
                steam.Emit(count);
            }
        }

        // Le modèle de la définition (sans ses colliders), ou une batte simple s'il manque : la poignée, le corps, le pommeau.
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

            var wood = new Color(0.72f, 0.5f, 0.28f);
            Cylinder(root, 0.05f, 0.32f, 0.014f, new Color(0.1f, 0.08f, 0.08f));
            Cylinder(root, 0.45f, 0.56f, 0.032f, wood);
            Cylinder(root, -0.115f, 0.02f, 0.022f, wood);
        }

        static void Cylinder(Transform parent, float center, float height, float radius, Color color)
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            DestroyImmediate(cylinder.GetComponent<Collider>());
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = new Vector3(0f, center, 0f);
            cylinder.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            var renderer = cylinder.GetComponent<MeshRenderer>();
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
            UpdateGhost();
            UpdateAim();
            UpdateLock();
            if (m_Hidden)
                return;

            UpdateLook(Time.deltaTime);
            IgnoreGrenades();

            // En pause (menu ouvert en cours de partie), la batte ne frappe rien. Au casque, les vitesses se mesurent en temps
            // réel : pendant un ralenti (un coup parfait, un coffre), un geste lent ne devient pas un coup.
            if (SlowMotion.IsPaused)
            {
                m_HasPrevious = false;
                return;
            }

            if (m_Desktop)
                UpdateDesktop(Time.unscaledDeltaTime);
            else
                UpdateSwing(Time.unscaledDeltaTime);
        }

        void SetHidden(bool hidden)
        {
            m_Hidden = hidden;
            m_Grab.Release();
            m_Grab.gameObject.SetActive(!hidden);
            if (!hidden && !m_Desktop)
                m_Grab.SnapToHolster();
            m_HasPrevious = false;
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
            m_SwingTime = -1f;
            m_HasPrevious = false;
        }

        // La batte ne bouscule pas les grenades par la physique (un rebond avant le coup) : elle les frappe par son propre calcul.
        void IgnoreGrenades()
        {
            if (m_GrabCollider == null)
                return;
            m_Ignored.RemoveWhere(grenade => grenade == null);
            foreach (var grenade in ArrowGrenade.InFlight)
            {
                if (grenade == null || !m_Ignored.Add(grenade))
                    continue;
                foreach (var collider in grenade.Colliders)
                {
                    if (collider != null)
                        Physics.IgnoreCollision(m_GrabCollider, collider, true);
                }
            }
        }

        // ---------------------------------------------------------------- le coup

        void UpdateSwing(float deltaTime)
        {
            var start = SweetStart;
            var tip = Tip;
            if (!BatActive || !m_HasPrevious || deltaTime <= 0f)
            {
                m_PreviousStart = start;
                m_PreviousTip = tip;
                m_HasPrevious = BatActive;
                return;
            }

            Swing(start, tip, deltaTime);
        }

        // Le trajet de la batte depuis l'image d'avant : les grenades qu'il croise, puis les ennemis, les projectiles et ce qui
        // réagit aux flèches.
        void Swing(Vector3 start, Vector3 tip, float deltaTime)
        {
            HitGrenades(start, tip, deltaTime);

            var velocity = (tip - m_PreviousTip) / deltaTime;
            var speed = velocity.magnitude;
            if (speed >= k_HitSpeed)
            {
                Strike(start, tip, velocity, speed);
                DeflectProjectiles(start, tip);
            }

            m_PreviousStart = start;
            m_PreviousTip = tip;
        }

        void HitGrenades(Vector3 start, Vector3 tip, float deltaTime)
        {
            var grenades = ArrowGrenade.InFlight;
            for (var i = grenades.Count - 1; i >= 0; i--)
            {
                var grenade = grenades[i];
                if (grenade == null || !grenade.CanBeBatted)
                    continue;
                if (!Sweep(grenade.Position, start, tip, out var along, out var when))
                    continue;

                // La vitesse du point de la batte qui la touche.
                var before = Vector3.Lerp(m_PreviousStart, m_PreviousTip, along);
                var after = Vector3.Lerp(start, tip, along);
                if ((after - before).magnitude / deltaTime < k_GrenadeHitSpeed)
                    continue;

                HitGrenade(grenade, Vector3.Lerp(before, after, when));
            }
        }

        // La grenade est-elle à portée de la batte à un moment de son trajet depuis l'image d'avant ? (Quelques positions
        // intermédiaires de la batte ; along : où sur la batte, de 0 au début de la partie qui frappe à 1 au bout ; when : quand.)
        bool Sweep(Vector3 point, Vector3 start, Vector3 tip, out float along, out float when)
        {
            along = 0f;
            when = 0f;
            var best = float.MaxValue;
            for (var i = 0; i <= k_SweepSteps; i++)
            {
                var t = i / (float)k_SweepSteps;
                var a = Vector3.Lerp(m_PreviousStart, start, t);
                var b = Vector3.Lerp(m_PreviousTip, tip, t);
                var segment = b - a;
                var u = Mathf.Clamp01(Vector3.Dot(point - a, segment) / Mathf.Max(1e-6f, segment.sqrMagnitude));
                var distance = (point - (a + segment * u)).sqrMagnitude;
                if (distance >= best)
                    continue;
                best = distance;
                along = u;
                when = t;
            }

            return best <= k_GrenadeReach * k_GrenadeReach;
        }

        void HitGrenade(ArrowGrenade grenade, Vector3 point)
        {
            // La grenade fantôme part comme le dernier vrai coup ; un vrai coup redonne des grenades fantômes.
            var ghost = grenade.IsFloatingGhost;
            var grade = ghost ? m_LastGrade : GradeOf(grenade.PathLength);
            var siege = ghost ? m_LastSiege : SiegeBonus(grenade.AirTime);
            if (ghost)
            {
                if (m_GhostLeft != int.MaxValue)
                    m_GhostLeft--;
                m_Ghost = null;
                m_NextGhost = Time.time + k_GhostRespawn;
            }
            else
            {
                m_HasLastShot = true;
                m_LastGrade = grade;
                m_LastSiege = siege;
                m_GhostLeft = GhostShots;
                m_NextGhost = Time.time + k_GhostAfterHit;
            }

            var modifiers = Tuning.Get(grade);
            var view = ViewPose();
            var aim = Aim(view);
            var origin = grenade.Position;
            FindTargets(view.position, aim, origin);
            PreferLocked();
            var lifted = Quaternion.AngleAxis(-k_NoTargetLift, Vector3.Cross(Vector3.up, aim)) * aim;
            var direction = s_Targets.Count > 0 ? (s_Targets[0].Center - origin).normalized : lifted;
            var speed = SpeedOf(grade);
            var flames = FlamesOf(grade);

            // Le multitir et la corde triple : les copies d'abord (copiée après le coup, une grenade emporterait ses flammes),
            // chacune sur l'ennemi suivant, en éventail s'il n'y en a plus ; puis la grenade frappée, sur le premier.
            var upgrades = PlayerUpgrades.Instance;
            var volley = 1 + (upgrades != null
                ? PlayerUpgrades.RollCount(upgrades.MultishotAverageFor(grade)) + Mathf.RoundToInt(upgrades.Total(UpgradeEffect.Triple))
                : 0);
            var echoes = upgrades != null ? PlayerUpgrades.RollCount(upgrades.EchoAverageFor(grade)) : 0;
            s_Volley.Clear();
            s_Volley.Add(grenade);
            for (var k = volley - 1; k >= 1; k--)
                s_Volley.Add(grenade.CopyForVolley());

            // Nées au même endroit, les copies ne doivent pas se heurter (avec la corde triple, la grenade éclatait sur la batte).
            if (s_Volley.Count > 1)
                ArrowGrenade.IgnoreTogether(s_Volley);
            for (var k = volley - 1; k >= 0; k--)
            {
                var target = k < s_Targets.Count ? s_Targets[k] : null;
                var fan = Quaternion.AngleAxis((k % 2 == 0 ? 1f : -1f) * k_FanAngle * ((k + 1) / 2), Vector3.up) * direction;
                var piece = s_Volley[k];
                piece.Bat(new BatShot
                {
                    Grade = grade,
                    Target = target,
                    Direction = target != null ? (target.Center - origin).normalized : fan,
                    Speed = speed,
                    Flames = flames,
                    DamageBonus = ghost ? k_GhostBonus : 1f,
                    ArrowBonus = siege,
                    Echoes = echoes,
                });
            }

            if (m_Bow != null)
            {
                var definition = m_Bow.StartingDefinition != null ? m_Bow.StartingDefinition : m_Definition;
                m_Bow.ReportShot(new ShotInfo
                {
                    Arrow = null,
                    Grade = grade,
                    DrawRatio = 1f,
                    Speed = speed,
                    Damage = (definition != null ? definition.damage : 10f) * m_Bow.GradeDamage(grade) * DamageMultiplier,
                    Origin = origin,
                    Direction = direction,
                    Hand = BatHand,
                });
            }

            // Le son monte avec la qualité. Un parfait a son propre son, bien plus puissant (le joueur, le même jour : « trouve un
            // son bien plus puissant pour le parfait » ; il contient son coup de batte), et un très bref ralenti ; sans lui, le
            // coup de batte et le son du tir parfait de l'arc.
            var homeRun = grade == ShotGrade.Perfect ? m_Definition.batHomeRunClip : null;
            if (homeRun != null)
                Sfx.Play(homeRun, point, 1f, Random.Range(0.98f, 1.02f));
            else
                Sfx.Play(m_Definition.batHitClip, point, Mathf.Lerp(0.75f, 1f, flames), Mathf.Lerp(0.9f, 1.08f, flames) * Random.Range(0.97f, 1.03f));
            if (grade == ShotGrade.Perfect)
            {
                if (homeRun == null && m_Bow != null)
                    Sfx.Play(m_Bow.PerfectClip, point, 1f);
                SlowMotion.HitStop(k_HitStopScale, k_HitStopTime);
            }

            if (!string.IsNullOrEmpty(modifiers.label))
                FloatingText.Spawn(origin + Vector3.up * 0.15f, modifiers.label, modifiers.color, Mathf.Lerp(0.6f, 1.1f, flames), 0.9f);
            if (siege > 1.05f)
                FloatingText.Spawn(origin - Vector3.up * 0.05f, "Siège ×" + siege.ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo("fr-FR")),
                                   k_SiegeColor, 0.55f, 0.9f);
            Haptics.Pulse(BatHand, Mathf.Lerp(0.45f, 1f, flames), Mathf.Lerp(0.06f, 0.16f, flames));
            Puff(0.25f + 0.75f * flames);
            OnLookHit(flames);
        }

        // La corde de siège : le nombre de flèches de la grenade, selon son temps de vol avant le coup.
        static float SiegeBonus(float airTime)
        {
            var upgrades = PlayerUpgrades.Instance;
            var level = upgrades != null ? upgrades.Stacks(UpgradeEffect.Siege) : 0;
            return level <= 0 ? 1f : 1f + SpecialArrows.ByLevel(k_SiegeArrows, level) * Mathf.Clamp01(airTime / k_SiegeChargeTime);
        }

        // Les grenades fantômes par vrai coup, selon le niveau de la corde fantôme.
        static int GhostShots
        {
            get
            {
                var upgrades = PlayerUpgrades.Instance;
                var level = upgrades != null ? upgrades.Stacks(UpgradeEffect.Ghost) : 0;
                return level <= 0 ? 0 : k_GhostShots[Mathf.Min(level, k_GhostShots.Length) - 1];
            }
        }

        // ---------------------------------------------------------------- la grenade fantôme

        // Elle flotte devant le joueur (95 cm devant les yeux, 30 cm plus bas : à portée du bout de la batte), monte et descend
        // doucement et tourne sur elle-même ; elle suit ses déplacements. Rangée avec la batte, elle disparaît.
        void UpdateGhost()
        {
            var upgrades = PlayerUpgrades.Instance;
            var level = upgrades != null ? upgrades.Stacks(UpgradeEffect.Ghost) : 0;
            if (level <= 0)
                m_GhostLeft = 0;
            if (level <= 0 || m_Hidden || !m_HasLastShot)
            {
                ClearGhost();
                return;
            }

            if (m_Ghost != null && !m_Ghost.IsFloatingGhost)
                m_Ghost = null;

            // Une vraie grenade en l'air : la fantôme s'efface (sans compter) et revient après le coup.
            if (RealGrenadeInAir())
            {
                ClearGhost();
                m_NextGhost = Mathf.Max(m_NextGhost, Time.time + k_GhostAfterHit);
                return;
            }

            if (m_Ghost == null)
            {
                if (m_GhostLeft <= 0 || Time.time < m_NextGhost || !TryGhostPose(out var spawn))
                    return;
                var belt = GrenadeBelt.Instance;
                m_Ghost = ArrowGrenade.SpawnGhost(belt != null ? belt.GrenadePrefab : null, GhostMaterial(), spawn.position);
                if (m_Ghost == null)
                    return;
            }

            if (TryGhostPose(out var pose))
                m_Ghost.HoldAt(pose.position, pose.rotation);
        }

        bool TryGhostPose(out Pose pose)
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (head == null)
            {
                pose = default;
                return false;
            }

            var bob = Vector3.up * (Mathf.Sin(Time.time * 2.2f) * 0.03f);
            var offset = new Vector3(k_GhostOffset.x * HeldSide, k_GhostOffset.y, k_GhostOffset.z);
            pose = new Pose(head.position + rig.HeadYaw * offset + bob, Quaternion.Euler(0f, Time.time * 60f, 0f));
            return true;
        }

        static bool RealGrenadeInAir()
        {
            foreach (var grenade in ArrowGrenade.InFlight)
            {
                if (grenade != null && grenade.CanBeBatted && !grenade.IsFloatingGhost)
                    return true;
            }

            return false;
        }

        void ClearGhost()
        {
            if (m_Ghost != null && m_Ghost.IsFloatingGhost)
                m_Ghost.Discard();
            m_Ghost = null;
        }

        // L'allure des flèches fantômes : le shader des hologrammes (Archery/Hologram), bleu pâle, des lignes qui défilent.
        Material GhostMaterial()
        {
            if (m_GhostMaterial != null)
                return m_GhostMaterial;
            var shader = Shader.Find("Archery/Hologram");
            if (shader == null)
                return null;

            m_GhostMaterial = new Material(shader) { name = "Grenade fantôme" };
            m_GhostMaterial.SetColor("_Color", k_GhostGlow);
            m_GhostMaterial.SetFloat("_Intensity", 1.1f);
            m_GhostMaterial.SetFloat("_ScanDensity", 14f);
            m_GhostMaterial.SetFloat("_ScanSpeed", 0.8f);
            m_GhostMaterial.SetFloat("_ScanDepth", 0.35f);
            m_GhostMaterial.SetFloat("_Flicker", 0.12f);
            m_GhostMaterial.SetFloat("_Fresnel", 0.35f);
            return m_GhostMaterial;
        }

        ShotGrade GradeOf(float path)
        {
            if (m_Bow != null && m_Bow.ForcePerfect)
                return ShotGrade.Perfect;
            var gold = m_Bow != null ? Mathf.Max(0.1f, m_Bow.GoldWidthMultiplier) : 1f;
            var band = m_Bow != null ? Mathf.Max(0.1f, m_Bow.BandWidthMultiplier) : 1f;
            if (path >= k_PerfectPath / gold)
                return ShotGrade.Perfect;
            if (path >= k_HeldPath / band)
                return ShotGrade.Held;
            if (path >= k_GoodPath / band)
                return ShotGrade.Good;
            return path >= k_OkPath / band ? ShotGrade.Ok : ShotGrade.Miss;
        }

        static float SpeedOf(ShotGrade grade) => grade switch
        {
            ShotGrade.Perfect => k_PerfectSpeed,
            ShotGrade.Held => k_HeldSpeed,
            ShotGrade.Good => k_GoodSpeed,
            ShotGrade.Ok => k_OkSpeed,
            _ => k_MissSpeed,
        };

        static float FlamesOf(ShotGrade grade) => grade switch
        {
            ShotGrade.Perfect => 1f,
            ShotGrade.Held => 0.65f,
            ShotGrade.Good => 0.4f,
            ShotGrade.Ok => 0.15f,
            _ => 0f,
        };

        // Le regard : la tête au casque, la caméra sans casque.
        Pose ViewPose()
        {
            var controls = DesktopControls.Instance;
            var rig = PlayerRig.Instance;
            var head = m_Desktop && controls != null ? controls.CameraTransform : rig != null ? rig.Head : null;
            return head != null ? new Pose(head.position, head.rotation) : new Pose(transform.position, transform.rotation);
        }

        // Retient la direction horizontale du regard tant qu'il est à peu près droit devant (pas levé vers la grenade).
        void UpdateAim()
        {
            var view = ViewPose();
            var forward = view.forward;
            if (Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f)) * Mathf.Rad2Deg > k_AimMaxPitch)
                return;

            // Le joueur regarde une grenade (celle qu'il va frapper) : ce n'est pas là qu'il vise.
            foreach (var grenade in ArrowGrenade.InFlight)
            {
                if (grenade != null && grenade.CanBeBatted && Vector3.Angle(forward, grenade.Position - view.position) < k_AimIgnoreGrenade)
                    return;
            }

            var flat = Vector3.ProjectOnPlane(forward, Vector3.up);
            if (flat.sqrMagnitude < 0.01f)
                return;
            m_Aim = flat.normalized;
            m_HasAim = true;
        }

        Vector3 Aim(Pose view)
        {
            if (m_HasAim)
                return m_Aim;
            var flat = Vector3.ProjectOnPlane(view.forward, Vector3.up);
            return flat.sqrMagnitude > 0.01f ? flat.normalized : Vector3.forward;
        }

        // Les ennemis autour de la visée (à l'horizontale : un ennemi en contrebas ou sur une hauteur compte comme devant) que la
        // grenade, partie de shotOrigin, peut atteindre en ligne droite ; du plus proche de la visée au plus éloigné.
        static void FindTargets(Vector3 viewOrigin, Vector3 aim, Vector3 shotOrigin)
        {
            s_Targets.Clear();
            var limit = k_TargetAngle;
            for (var pass = 0; pass < 2 && s_Targets.Count == 0; pass++)
            {
                foreach (var enemy in Enemy.Alive)
                {
                    if (enemy == null || !enemy.IsAlive)
                        continue;
                    var offset = enemy.Center - viewOrigin;
                    if (offset.sqrMagnitude <= k_TargetRange * k_TargetRange && FlatAngle(aim, offset) < limit && InSight(shotOrigin, enemy))
                        s_Targets.Add(enemy);
                }

                limit = k_TargetAngleWide;
            }

            s_Targets.Sort((a, b) => FlatAngle(aim, a.Center - viewOrigin).CompareTo(FlatAngle(aim, b.Center - viewOrigin)));
        }

        // Rien entre la grenade et l'ennemi : ni relief, ni rocher, ni arbre, ni bâtiment. Les ennemis (lui ou un autre devant), les
        // grenades, le joueur et ses armes, ce que les flèches traversent et les zones (triggers) ne cachent rien.
        static bool InSight(Vector3 from, Enemy enemy)
        {
            var offset = enemy.Center - from;
            var distance = offset.magnitude;
            if (distance < 0.5f)
                return true;

            var count = Physics.RaycastNonAlloc(from, offset / distance, s_Sight, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var rig = PlayerRig.Instance;
            var player = rig != null && rig.Origin != null ? rig.Origin.transform : null;
            for (var i = 0; i < count; i++)
            {
                var collider = s_Sight[i].collider;
                if (collider == null || ArrowIgnore.IsIgnored(collider) || collider.GetComponentInParent<Enemy>() != null ||
                    collider.GetComponentInParent<ArrowGrenade>() != null || (player != null && collider.transform.IsChildOf(player)))
                    continue;
                return false;
            }

            return true;
        }

        static float FlatAngle(Vector3 aim, Vector3 offset) => Vector3.Angle(aim, Vector3.ProjectOnPlane(offset, Vector3.up));

        // ---------------------------------------------------------------- le corps à corps

        // Le coup : la batte (une capsule) et le chemin du bout depuis l'image d'avant ; chaque ennemi touché une fois par délai,
        // par sa zone de touche (la tête compte double, comme pour une flèche), et repoussé.
        void Strike(Vector3 start, Vector3 tip, Vector3 velocity, float speed)
        {
            var damage = m_Definition.batDamage * DamageMultiplier * Mathf.Lerp(1f, 1.6f, Mathf.InverseLerp(k_HitSpeed, k_HitFastSpeed, speed));
            var direction = velocity / speed;
            var count = Physics.OverlapCapsuleNonAlloc(start, tip, k_BatRadius, s_Overlaps, ~0, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
                TryHit(s_Overlaps[i], PointOn(s_Overlaps[i], tip), direction, damage);

            var step = tip - m_PreviousTip;
            var distance = step.magnitude;
            if (distance < 1e-4f)
                return;
            var hits = Physics.RaycastNonAlloc(m_PreviousTip, step / distance, s_Hits, distance, ~0, QueryTriggerInteraction.Collide);
            for (var i = 0; i < hits; i++)
                TryHit(s_Hits[i].collider, s_Hits[i].point, direction, damage);
        }

        // Le point du collider le plus proche du bout de la batte. Unity ne sait le calculer que pour les boîtes, les sphères, les
        // capsules et les maillages convexes (le premier essai du 13 novembre : « Physics.ClosestPoint can only be used with... »
        // ×8 dans le journal, la batte qui frôlait un arbre ou le terrain) : pour les autres, le bout de la batte.
        static Vector3 PointOn(Collider collider, Vector3 tip) =>
            collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider || (collider is MeshCollider mesh && mesh.convex)
                ? collider.ClosestPoint(tip)
                : tip;

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
            if (enemy != null && boss == null)
                enemy.Push(direction * k_Push);
            Sfx.Play(m_Definition.batHitClip, point, 0.8f, Random.Range(0.85f, 0.95f));
            Haptics.Pulse(BatHand, 0.8f, 0.08f);
            Puff(k_MeleeSteam);
            OnLookHit(k_MeleeSteam);
        }

        // Un coup dans ce qui réagit aux flèches sans être un ennemi (le gong, une cible d'entraînement, une cible de la victoire
        // contre MK, une boule de feu), comme le katana : il compte comme une flèche tirée, une fois par seconde au plus pour
        // chaque objet. Pas les boutons des menus ni les panneaux de difficulté : ils se visent au rayon de la main.
        void TryTrigger(Collider collider, Vector3 point, Vector3 direction, float damage)
        {
            var handler = collider.GetComponentInParent<IArrowHitHandler>();
            if (handler == null || handler is Hitbox || handler is ILaserTransparent || !(handler is Object key) || key == null)
                return;
            if (m_LastTrigger.TryGetValue(key, out var last) && Time.time - last < k_TriggerCooldown)
                return;

            m_LastTrigger[key] = Time.time;
            handler.OnArrowHit(new ArrowHit
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
            });
            Haptics.Pulse(BatHand, 0.7f, 0.08f);
        }

        // La batte renvoie (détruit) les projectiles qu'elle croise, comme la lame du katana.
        void DeflectProjectiles(Vector3 start, Vector3 tip)
        {
            var bat = tip - start;
            for (var i = m_Projectiles.Count - 1; i >= 0; i--)
            {
                var projectile = m_Projectiles[i];
                if (projectile == null)
                {
                    m_Projectiles.RemoveAt(i);
                    continue;
                }

                var position = projectile.transform.position;
                var t = Mathf.Clamp01(Vector3.Dot(position - start, bat) / Mathf.Max(1e-4f, bat.sqrMagnitude));
                if ((position - (start + bat * t)).sqrMagnitude >= k_ProjectileReach * k_ProjectileReach)
                    continue;
                if (projectile.OnArrowHit(new ArrowHit { Collider = null, Point = position, Direction = Vector3.zero, IsShot = true, Grade = ShotGrade.Good }))
                    Haptics.Pulse(BatHand, 0.6f, 0.06f);
            }
        }

        // ---------------------------------------------------------------- sans casque

        // Clic gauche : un coup de batte devant soi (d'un côté puis de l'autre), qui frappe au corps à corps et les grenades qu'il
        // croise.
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
                m_HasPrevious = false;
            }

            // Au repos, la batte dressée vers l'avant ; pendant le coup, elle balaie la vue en travers.
            var direction = new Vector3(0.2f, 0.6f, 1f).normalized;
            var swinging = m_SwingTime >= 0f;
            if (swinging)
            {
                m_SwingTime += deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(m_SwingTime / k_DesktopSwing));
                var from = new Vector3(0.95f * m_SwingSide, 0.25f, 0.7f).normalized;
                var to = new Vector3(-0.95f * m_SwingSide, 0.05f, 0.7f).normalized;
                direction = Vector3.Slerp(from, to, t);
            }

            var bat = m_Grab.transform;
            bat.localPosition = k_DesktopBat;
            bat.localRotation = Quaternion.FromToRotation(Vector3.up, direction);
            if (!swinging)
                return;

            var start = SweetStart;
            var tip = Tip;
            if (m_HasPrevious && deltaTime > 0f)
                Swing(start, tip, deltaTime);
            m_PreviousStart = start;
            m_PreviousTip = tip;
            m_HasPrevious = true;
            if (m_SwingTime >= k_DesktopSwing)
                m_SwingTime = -1f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_Active = null;

        // ---------------------------------------------------------------- raccourci de test

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // F6 : la batte tout de suite (même pendant la partie) ; F6 encore : l'arme choisie.
        sealed class TestKey : MonoBehaviour
        {
            BowDefinition m_Previous;

            void Update()
            {
                var keyboard = Keyboard.current;
                if (keyboard == null || !keyboard.f6Key.wasPressedThisFrame)
                    return;
                var bow = FindAnyObjectByType<Bow>();
                var loadout = BowLoadout.Instance;
                if (bow == null || loadout == null)
                    return;

                if (bow.IsBat)
                {
                    var back = m_Previous != null && !m_Previous.bat ? m_Previous : loadout.Selected;
                    if (back != null && back.bat)
                        back = bow.StartingDefinition;
                    bow.SetDefinition(back);
                    HeadMessages.Show((back != null ? back.displayName : "Arc") + " équipé", new Color(0.55f, 0.9f, 1f), 2f);
                    return;
                }

                foreach (var entry in loadout.Entries)
                {
                    if (entry == null || entry.bow == null || !entry.bow.bat)
                        continue;
                    m_Previous = bow.Definition;
                    bow.SetDefinition(entry.bow);
                    if (GrenadeBelt.Instance != null)
                        GrenadeBelt.Instance.ResetCooldown();
                    HeadMessages.Show(entry.bow.displayName + " équipée (test)\n<size=60%>F6 : retour à l'arme d'avant</size>", entry.color, 3f);
                    return;
                }

                HeadMessages.Show("Pas de batte dans la liste des armes (menu Archery > Batte)", new Color(1f, 0.6f, 0.55f), 3f);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void InstallTestKey()
        {
            var go = new GameObject("Raccourci batte (touche F6)");
            DontDestroyOnLoad(go);
            go.AddComponent<TestKey>();
        }
#endif
    }
}
