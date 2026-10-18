using System.Collections.Generic;
using Archery.Bows;
using Archery.Core;
using Archery.Difficulty;
using Archery.Enemies;
using Archery.FinalBoss;
using Archery.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Portal
{
    /// <summary>
    /// La dimension du portail des étoiles (GDD, section 23) : un ciel noir criblé d'étoiles, des nébuleuses et des
    /// galaxies lumineuses, un sol miroir, et des piliers vermillon plantés en désordre, qui ne sont des torii complets
    /// que dans leur reflet ; par-dessus, un monde qui bugue (piliers qui sautent, panneaux d'erreur rouges,
    /// <see cref="ErrorPanels"/>). L'épreuve : 10 s pour découvrir le décor pendant que la musique monte, puis survivre
    /// 30 s au monstre corrompu (<see cref="CorruptedMonster"/>). Le temps restant n'est affiché que sur les panneaux.
    /// </summary>
    /// <remarks>
    /// Scène à part (<c>Dimension_Etoiles</c>), chargée par le <see cref="StarPortal"/> de la carte, placée à 3 km de la
    /// carte et à 600 m de haut (sous la carte, les flèches disparaîtraient ; en dessous de 150 m, les effets de la carte
    /// suivraient le joueur). Tout le décor est fait d'objets de la scène, modifiables à la main (construits une première
    /// fois par <c>Archery > Portail des étoiles > Reconstruire le décor de la dimension</c>) ; le ciel, la lumière
    /// ambiante et le brouillard sont ceux de la scène. Les piliers des torii proches (Capsule Collider) servent de
    /// cachettes au monstre ; le miroir est <see cref="DimensionMirror"/>.
    /// </remarks>
    [DisallowMultipleComponent]
    public class StarDimension : MonoBehaviour
    {
        static readonly int k_EmissionColorId = Shader.PropertyToID("_EmissionColor");

        // Le champ de vision du joueur pour le monstre (demi-angle, en degrés) : d'abord serré, puis élargi.
        const float k_ViewCone = 50f;
        const float k_WideCone = 70f;

        [Header("Scène (objets modifiables à la main)")]
        [Tooltip("Point d'arrivée du joueur (son axe bleu donne la direction du regard).")]
        [SerializeField]
        Transform m_Spawn;

        [Tooltip("Les torii : les piliers (Capsule Collider) proches servent de cachettes au monstre, et les torii bougent " +
                 "quand le monde bugue.")]
        [SerializeField]
        Transform m_Torii;

        [Tooltip("Distance (m) au centre au-delà de laquelle un pilier ne sert plus de cachette.")]
        [SerializeField]
        float m_PostRange = 45f;

        [Tooltip("Les murs invisibles (les flèches les traversent).")]
        [SerializeField]
        Transform m_Walls;

        [Tooltip("La musique (elle monte pendant la découverte).")]
        [SerializeField]
        AudioSource m_MusicSource;

        [Header("Effets")]
        [Tooltip("Matériau des panneaux et des effets (couleurs des sommets, transparent : Effects).")]
        [SerializeField]
        Material m_GlowMaterial;

        [Tooltip("Shader du monstre corrompu (Archery/Glitch).")]
        [SerializeField]
        Shader m_GlitchShader;

        [Header("Épreuve")]
        [Tooltip("Durée (s) de la découverte, avant le compte à rebours.")]
        [SerializeField]
        float m_IntroTime = 10f;

        [Tooltip("Durée (s) à tenir.")]
        [SerializeField]
        float m_SurviveTime = 30f;

        [Tooltip("Les apparences du monstre, dans l'ordre : il change à chaque mort (ennemis du jeu).")]
        [SerializeField]
        Enemy[] m_MonsterModels = new Enemy[0];

        [Tooltip("Délai (s) avant que le monstre suivant apparaisse.")]
        [SerializeField]
        float m_RespawnDelay = 1.2f;

        [Header("Panneaux d'erreur")]
        [Tooltip("Fausses erreurs tirées au hasard (\\n pour un retour à la ligne).")]
        [SerializeField]
        [TextArea(1, 3)]
        string[] m_FakeErrors =
        {
            "THUNK.exe ne répond pas",
            "Mémoire corrompue\n0x00FF3A2C",
            "Fichier introuvable :\netoiles.dat",
            "Exception non gérée :\nJOUEUR_HORS_LIMITES",
            "Accès refusé",
            "Dépassement de pile",
            "Échec de l'intégrité\ndu monde",
            "Segment perdu :\nrealite.dll",
            "Signal inconnu détecté",
            "Tu n'aurais pas dû\nvenir ici",
            "Quelqu'un d'autre\nest là",
            "Ne relance pas\nla partie",
        };

        [Header("Sons")]
        [Tooltip("Musique, si la source n'en a pas.")]
        [SerializeField]
        AudioClip m_Music;

        [SerializeField]
        AudioClip[] m_GlitchClips = new AudioClip[0];

        [SerializeField]
        AudioClip m_PanelOpenClip;

        [SerializeField]
        AudioClip m_PanelCloseClip;

        [SerializeField]
        AudioClip m_KillClip;

        [SerializeField]
        AudioClip m_SuccessClip;

        [SerializeField]
        AudioClip m_FailClip;

        enum Phase
        {
            Waiting,
            Intro,
            Survive,
            Over,
        }

        class Gate
        {
            public Transform Root;
            public Vector3 Home;
            public Renderer[] Renderers;
        }

        readonly List<Transform> m_Posts = new List<Transform>();
        readonly Dictionary<Transform, float> m_PostRadius = new Dictionary<Transform, float>();
        readonly List<Gate> m_Gates = new List<Gate>();
        readonly List<Collider> m_WallColliders = new List<Collider>();
        StarPortal m_Portal;
        ErrorPanels m_Panels;
        CorruptedMonster m_Monster;
        AudioLowPassFilter m_LowPass;
        MaterialPropertyBlock m_Block;
        Phase m_Phase;
        float m_Time;
        float m_RespawnTimer = -1f;
        float m_GateGlitchTimer = 1f;
        float m_EndTimer = -1f;
        bool m_Success;
        int m_ModelIndex;
        Gate m_GlitchedGate;

        public static StarDimension Instance { get; private set; }

        /// <summary>Point d'arrivée du joueur.</summary>
        public Transform PlayerSpawn => m_Spawn != null ? m_Spawn : transform;

        /// <summary>L'épreuve est en cours (découverte ou compte à rebours).</summary>
        public bool Running => m_Phase == Phase.Intro || m_Phase == Phase.Survive;

        /// <summary>Secondes à tenir encore (toute la durée pendant la découverte, 0 une fois l'épreuve finie).</summary>
        public float TimeLeft => m_Phase == Phase.Survive ? Mathf.Max(0f, m_SurviveTime - (m_Time - m_IntroTime))
                              : m_Phase == Phase.Over ? 0f : m_SurviveTime;

        public Transform Head => PlayerRig.Instance != null ? PlayerRig.Instance.Head : null;

        /// <summary>Le monstre, pour ne pas mettre de panneau devant lui.</summary>
        public Vector3? MonsterPoint => m_Monster != null && m_Monster.IsAlive ? m_Monster.Point : (Vector3?)null;

        /// <summary>La hauteur du sol.</summary>
        public float FloorY => transform.position.y;

        void Awake()
        {
            Instance = this;
            FindPosts();
            if (m_Walls != null)
            {
                m_Walls.GetComponentsInChildren(m_WallColliders);
                ArrowIgnore.Register(m_WallColliders);
            }

            SetUpMusic();
            m_Panels = gameObject.AddComponent<ErrorPanels>();
            m_Panels.Configure(this, m_GlowMaterial, m_PanelOpenClip, m_PanelCloseClip, m_FakeErrors);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            ArrowIgnore.Unregister(m_WallColliders);
        }

        // Les cachettes du monstre : les piliers des torii (Capsule Collider) à portée ; les torii qui les portent bugent.
        void FindPosts()
        {
            if (m_Torii == null)
                return;

            var gates = new HashSet<Transform>();
            foreach (var capsule in m_Torii.GetComponentsInChildren<CapsuleCollider>())
            {
                var post = capsule.transform;
                if (FlatDistance(post.position, transform.position) > m_PostRange)
                    continue;

                m_Posts.Add(post);
                var scale = post.lossyScale;
                m_PostRadius[post] = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
                var gate = post.parent != null && post.parent != m_Torii ? post.parent : post;
                if (gates.Add(gate))
                    m_Gates.Add(new Gate { Root = gate, Home = gate.position, Renderers = gate.GetComponentsInChildren<Renderer>(true) });
            }

            if (m_Posts.Count == 0)
                Debug.LogWarning("StarDimension : aucun pilier (Capsule Collider) sous les torii : le monstre n'a nulle part où se cacher.", this);
        }

        void SetUpMusic()
        {
            if (m_MusicSource == null)
            {
                var music = new GameObject("Musique");
                music.transform.SetParent(transform, false);
                m_MusicSource = music.AddComponent<AudioSource>();
                m_MusicSource.loop = true;
                m_MusicSource.spatialBlend = 0f;
                m_MusicSource.pitch = 0.9f;
            }

            if (m_MusicSource.clip == null)
                m_MusicSource.clip = m_Music;
            m_MusicSource.playOnAwake = false;
            m_MusicSource.Stop();
            if (m_MusicSource.outputAudioMixerGroup == null)
                m_MusicSource.outputAudioMixerGroup = Sfx.Output;
            m_LowPass = m_MusicSource.GetComponent<AudioLowPassFilter>();
            if (m_LowPass == null)
                m_LowPass = m_MusicSource.gameObject.AddComponent<AudioLowPassFilter>();
            m_LowPass.cutoffFrequency = 500f;
        }

        /// <summary>Le joueur vient d'arriver : la découverte commence, puis le compte à rebours.</summary>
        public void Begin(StarPortal portal)
        {
            m_Portal = portal;

            // Le ciel, la lumière ambiante et le brouillard sont ceux de cette scène, devenue la scène active.
            DynamicGI.UpdateEnvironment();
            m_Phase = Phase.Intro;
            m_Time = 0f;
            m_Panels.Intro = true;
            m_Panels.ShowCentered("THUNK.exe", "ANOMALIE\nDÉTECTÉE", 3f);
            if (m_MusicSource != null && m_MusicSource.clip != null)
            {
                m_MusicSource.volume = 0f;
                m_MusicSource.Play();
            }
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;
            UpdateGateGlitch(deltaTime);
            if (m_Phase == Phase.Waiting)
                return;

            m_Time += deltaTime;
            UpdateMusic();

            switch (m_Phase)
            {
                case Phase.Intro:
                    if (m_Time >= m_IntroTime)
                    {
                        m_Phase = Phase.Survive;
                        m_Panels.Intro = false;
                        SpawnMonster();
                    }

                    break;

                case Phase.Survive:
                    if (m_RespawnTimer >= 0f)
                    {
                        m_RespawnTimer -= deltaTime;
                        if (m_RespawnTimer < 0f)
                            SpawnMonster();
                    }

                    if (TimeLeft <= 0f)
                        Finish(true);
                    break;

                case Phase.Over:
                    m_EndTimer -= deltaTime;
                    if (m_EndTimer <= 0f && m_EndTimer > -1f)
                    {
                        m_EndTimer = -10f;
                        if (m_Portal != null)
                            m_Portal.EndTrial(m_Success);
                        else
                            Debug.Log($"StarDimension : épreuve {(m_Success ? "réussie" : "ratée")} (sans portail : test de la scène seule).");
                    }

                    break;
            }
        }

        // La musique monte pendant la découverte : plus fort, et de moins en moins étouffée.
        void UpdateMusic()
        {
            if (m_MusicSource == null)
                return;

            var rise = Mathf.Clamp01(m_Time / Mathf.Max(0.1f, m_IntroTime));
            if (m_Phase == Phase.Over)
                m_MusicSource.volume = Mathf.MoveTowards(m_MusicSource.volume, 0f, Time.deltaTime * 0.5f);
            else
                m_MusicSource.volume = Mathf.Lerp(0.15f, 0.6f, rise);
            if (m_LowPass != null)
                m_LowPass.cutoffFrequency = Mathf.Lerp(500f, 22000f, rise * rise);
        }

        // ---------------------------------------------------------------- le monstre

        void SpawnMonster()
        {
            m_RespawnTimer = -1f;
            if (m_MonsterModels == null || m_MonsterModels.Length == 0 || m_Posts.Count == 0)
                return;

            var model = m_MonsterModels[m_ModelIndex % m_MonsterModels.Length];
            m_ModelIndex++;
            var speed = DifficultyManager.Current != null ? DifficultyManager.Current.enemySpeed : 1f;
            m_Monster = CorruptedMonster.Spawn(this, model, SpawnPost(), m_GlitchShader, speed);
        }

        /// <summary>Un monstre vient d'être tué : il éclate en pixels, et le suivant viendra plus loin.</summary>
        public void MonsterKilled(CorruptedMonster monster)
        {
            Pixels(monster.Point, 40, 1.4f);
            Sfx.Play(m_KillClip, monster.Point, 1f, Random.Range(0.85f, 1.1f));
            Destroy(monster.gameObject);
            if (m_Monster == monster)
                m_Monster = null;
            if (m_Phase == Phase.Survive)
                m_RespawnTimer = m_RespawnDelay;
        }

        /// <summary>Le monstre a touché le joueur : expulsé.</summary>
        public void PlayerTouched()
        {
            if (Running)
                Finish(false);
        }

        void Finish(bool success)
        {
            m_Phase = Phase.Over;
            m_Success = success;
            m_EndTimer = success ? 2.5f : 1.8f;
            if (m_Monster != null)
            {
                Pixels(m_Monster.Point, 40, 1.4f);
                Destroy(m_Monster.gameObject);
                m_Monster = null;
            }

            var head = Head;
            Sfx.Play(success ? m_SuccessClip : m_FailClip, head != null ? head.position : transform.position, 1f, 1f, 0f);
            if (success)
                m_Panels.ShowCentered("TRANSFERT TERMINÉ", "ÉPREUVE\nRÉUSSIE", 2.5f);
            else
                m_Panels.ShowCentered("CONNEXION PERDUE", "EXPULSÉ", 1.8f);
        }

        /// <summary>Derrière ce pilier, du côté opposé au joueur, au sol.</summary>
        public Vector3 HidePoint(Transform post)
        {
            var head = Head;
            var away = head != null ? post.position - head.position : post.forward;
            away.y = 0f;
            away = away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.forward;
            var radius = m_PostRadius.TryGetValue(post, out var r) ? r : 0.55f;
            var point = post.position + away * (radius + 0.75f);
            point.y = FloorY;
            return point;
        }

        /// <summary>Ce point est dans le champ de vision du joueur (demi-angle <paramref name="cone"/>, à l'horizontale).</summary>
        public bool InView(Vector3 point, float cone = k_ViewCone)
        {
            var head = Head;
            if (head == null)
                return true;

            var forward = head.forward;
            forward.y = 0f;
            var offset = point - head.position;
            offset.y = 0f;
            return forward.sqrMagnitude < 1e-4f || offset.sqrMagnitude < 1e-4f || Vector3.Angle(forward, offset) <= cone;
        }

        /// <summary>
        /// Le pilier suivant : dans le champ de vision du joueur, et plus près de lui (de 1,5 à 8 m) sans être collé à lui ;
        /// si le monstre est hors de vue (le joueur s'est tourné), un pilier en vue qui n'est pas plus loin.
        /// <paramref name="wide"/> : le champ de vision est élargi jusqu'à ses bords. Null s'il n'y en a pas.
        /// </summary>
        public Transform NextPost(Transform current, float distance, bool wide = false)
        {
            var head = Head;
            if (head == null)
                return null;

            var farthest = current != null && InView(current.position) ? distance - 1.5f : distance + 1f;
            var nearest = Mathf.Max(3.2f, distance - 8f);
            var target = distance - 4f;
            var candidates = new List<Transform>();
            foreach (var cone in wide ? new[] { k_ViewCone, k_WideCone, 95f } : new[] { k_ViewCone, k_WideCone })
            {
                candidates.Clear();
                foreach (var post in m_Posts)
                {
                    if (post == current)
                        continue;
                    var d = FlatDistance(post.position, head.position);
                    if (d <= farthest && d >= nearest && InView(post.position, cone))
                        candidates.Add(post);
                }

                if (candidates.Count == 0)
                    continue;
                candidates.Sort((a, b) => Mathf.Abs(FlatDistance(a.position, head.position) - target)
                    .CompareTo(Mathf.Abs(FlatDistance(b.position, head.position) - target)));
                return candidates[Random.Range(0, Mathf.Min(3, candidates.Count))];
            }

            return null;
        }

        // Loin du joueur, dans son champ de vision : jamais dans son dos.
        Transform SpawnPost()
        {
            var head = Head;
            if (head == null)
                return m_Posts[Random.Range(0, m_Posts.Count)];

            foreach (var (cone, minimum) in new[] { (40f, 16f), (k_ViewCone, 11f), (k_WideCone, 8f) })
            {
                var candidates = new List<Transform>();
                foreach (var post in m_Posts)
                {
                    if (FlatDistance(post.position, head.position) >= minimum && InView(post.position, cone))
                        candidates.Add(post);
                }

                if (candidates.Count > 0)
                    return candidates[Random.Range(0, candidates.Count)];
            }

            var farthest = m_Posts[0];
            foreach (var post in m_Posts)
            {
                if (FlatDistance(post.position, head.position) > FlatDistance(farthest.position, head.position))
                    farthest = post;
            }

            return farthest;
        }

        static float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        public AudioClip RandomGlitchClip() =>
            m_GlitchClips != null && m_GlitchClips.Length > 0 ? m_GlitchClips[Random.Range(0, m_GlitchClips.Length)] : null;

        // ---------------------------------------------------------------- effets

        /// <summary>Des pixels (petits carrés rouges, cyan et blancs) qui jaillissent à cet endroit.</summary>
        public void Pixels(Vector3 position, int count, float spread)
        {
            if (m_GlowMaterial == null)
                return;

            var go = new GameObject("Pixels");
            go.transform.SetParent(transform, true);
            go.transform.position = position;
            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.loop = false;
            main.duration = 0.1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f * spread, 3f * spread);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(2.5f, 0.15f, 0.15f, 1f), new Color(0.2f, 2f, 2.5f, 1f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.gravityModifier = 0f;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f * spread;

            var size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = m_GlowMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            particles.Play();
        }

        /// <summary>Une traînée de blocs entre deux points (le monstre qui saute des images), qui s'efface vite.</summary>
        public void Streak(Vector3 from, Vector3 to)
        {
            if (m_GlowMaterial == null)
                return;

            var builder = new MeshBuilder();
            var steps = Mathf.Clamp(Mathf.RoundToInt(Vector3.Distance(from, to) / 0.3f), 2, 30);
            for (var i = 0; i <= steps; i++)
            {
                var center = Vector3.Lerp(from, to, i / (float)steps) + Random.insideUnitSphere * 0.25f;
                var color = Random.value < 0.5f ? new Color(2.5f, 0.1f, 0.15f, 0.9f) : new Color(0.15f, 2f, 2.5f, 0.9f);
                builder.Box(center, new Vector3(Random.Range(0.05f, 0.25f), Random.Range(0.03f, 0.12f), Random.Range(0.05f, 0.2f)),
                            Quaternion.identity, color);
            }

            var go = new GameObject("Traînée");
            go.transform.SetParent(transform, true);
            var mesh = builder.ToMesh("Traînée");
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_GlowMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            Destroy(mesh, 0.3f);
            Destroy(go, 0.25f);
        }

        // De temps en temps (au plus 2 fois par seconde), un torii saute de côté et vire au cyan, un instant (son reflet
        // suit : DimensionMirror recopie sa place et sa couleur).
        void UpdateGateGlitch(float deltaTime)
        {
            if (m_Gates.Count == 0)
                return;

            m_GateGlitchTimer -= deltaTime;
            if (m_GateGlitchTimer > 0f)
                return;

            m_Block ??= new MaterialPropertyBlock();
            if (m_GlitchedGate != null)
            {
                SetGate(m_GlitchedGate, Vector3.zero, false);
                m_GlitchedGate = null;
                m_GateGlitchTimer = Random.Range(0.5f, 1.4f);
                return;
            }

            m_GlitchedGate = m_Gates[Random.Range(0, m_Gates.Count)];
            SetGate(m_GlitchedGate, m_GlitchedGate.Root.right * Random.Range(-0.4f, 0.4f), true);
            m_GateGlitchTimer = Random.Range(0.06f, 0.1f);
        }

        void SetGate(Gate gate, Vector3 offset, bool glitched)
        {
            if (gate.Root == null)
                return;

            gate.Root.position = gate.Home + offset;
            m_Block.Clear();
            m_Block.SetColor(k_EmissionColorId, new Color(0.2f, 1.6f, 2f));
            foreach (var renderer in gate.Renderers)
            {
                if (renderer != null)
                    renderer.SetPropertyBlock(glitched ? m_Block : null);
            }
        }
    }
}
