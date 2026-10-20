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
    /// <see cref="ErrorPanels"/>). L'épreuve : le temps de découvrir le décor jusqu'à la montée de la musique (12 s), puis
    /// survivre 1 minute aux monstres corrompus (<see cref="CorruptedMonster"/>), plusieurs à la fois : ils ne bougent que quand on ne
    /// les regarde pas. Le temps restant n'est affiché que sur les panneaux.
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
    public class StarDimension : MonoBehaviour, IGlitchDimension
    {
        static readonly int k_EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int k_EmissionMapId = Shader.PropertyToID("_EmissionMap");

        // Le champ de vision du joueur (demi-angle, en degrés) où les monstres apparaissent : d'abord serré, puis élargi.
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

        [Tooltip("La musique : elle part du début à l'arrivée du joueur, et sa montée donne le départ de l'épreuve " +
                 "(Intro Time). Son groupe de mixer : Music, réglé par le volume de la musique.")]
        [SerializeField]
        AudioSource m_MusicSource;

        [Tooltip("Volume de la musique.")]
        [SerializeField]
        [Range(0f, 1f)]
        float m_MusicVolume = 0.8f;

        [Header("Effets")]
        [Tooltip("Matériau des panneaux et des effets (couleurs des sommets, transparent : Effects).")]
        [SerializeField]
        Material m_GlowMaterial;

        [Tooltip("Shader du monstre corrompu (Archery/Glitch).")]
        [SerializeField]
        Shader m_GlitchShader;

        [Header("Épreuve")]
        [Tooltip("Durée (s) de la découverte, avant le compte à rebours : le moment de la montée dans la musique (12 s " +
                 "dans « Sad Nostalgie »). La découverte suit la musique elle-même : les monstres et « SURVIVEZ » tombent " +
                 "pile sur la montée.")]
        [SerializeField]
        float m_IntroTime = 12f;

        [Tooltip("Durée (s) à tenir.")]
        [SerializeField]
        float m_SurviveTime = 60f;

        [Tooltip("Les apparences des monstres, dans l'ordre : chaque nouveau monstre prend la suivante (ennemis du jeu).")]
        [SerializeField]
        Enemy[] m_MonsterModels = new Enemy[0];

        [Tooltip("Nombre de monstres en même temps, en Normal (selon le nombre d'ennemis de la difficulté : moins en Facile, " +
                 "plus en Difficile et Impossible). Seul, on le figerait en le regardant.")]
        [SerializeField]
        int m_MonsterCount = 3;

        [Tooltip("Délai (s) avant qu'un monstre tué soit remplacé.")]
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

        [Tooltip("Pas des monstres : un à chaque saut, plus grave que nature (Kenney, footstep_concrete).")]
        [SerializeField]
        AudioClip[] m_StepClips = new AudioClip[0];

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
        readonly List<CorruptedMonster> m_Monsters = new List<CorruptedMonster>();
        readonly List<float> m_RespawnTimers = new List<float>();
        readonly HashSet<Transform> m_Claimed = new HashSet<Transform>();
        AudioLowPassFilter m_LowPass;
        MaterialPropertyBlock m_Block;
        Phase m_Phase;
        float m_Time;
        float m_GateGlitchTimer = 1f;
        float m_EndTimer = -1f;
        bool m_Success;
        int m_ModelIndex;
        Gate m_GlitchedGate;
        AudioClip m_StaticClip;

        public static StarDimension Instance { get; private set; }

        /// <summary>Point d'arrivée du joueur.</summary>
        public Transform PlayerSpawn => m_Spawn != null ? m_Spawn : transform;

        /// <summary>L'épreuve est en cours (découverte ou compte à rebours).</summary>
        public bool Running => m_Phase == Phase.Intro || m_Phase == Phase.Survive;

        /// <summary>Secondes à tenir encore (toute la durée pendant la découverte, 0 une fois l'épreuve finie).</summary>
        public float TimeLeft => m_Phase == Phase.Survive ? Mathf.Max(0f, m_SurviveTime - (m_Time - m_IntroTime))
                              : m_Phase == Phase.Over ? 0f : m_SurviveTime;

        /// <summary>Secondes depuis l'arrivée du joueur (pendant la découverte : la position dans la musique).</summary>
        public float Elapsed => m_Time;

        public Transform Head => PlayerRig.Instance != null ? PlayerRig.Instance.Head : null;

        /// <summary>Les monstres en vie, pour ne pas mettre de panneau devant eux.</summary>
        public IReadOnlyList<CorruptedMonster> Monsters => m_Monsters;

        /// <summary>Matériau des effets (couleurs des sommets, transparent) : panneaux, pixels, yeux des monstres.</summary>
        public Material GlowMaterial => m_GlowMaterial;

        /// <summary>Le grésillement continu des monstres (parasites numériques en boucle, fabriqués au lancement).</summary>
        public AudioClip StaticClip => m_StaticClip;

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
            m_StaticClip = BuildStaticClip();
            m_Panels = gameObject.AddComponent<ErrorPanels>();
            m_Panels.Configure(this, m_GlowMaterial, m_PanelOpenClip, m_PanelCloseClip, m_FakeErrors);
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            ArrowIgnore.Unregister(m_WallColliders);
            if (m_StaticClip != null)
                Destroy(m_StaticClip);
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
            m_LowPass.cutoffFrequency = 22000f;
        }

        /// <summary>Le joueur vient d'arriver : la découverte commence, puis le compte à rebours.</summary>
        public void Begin(StarPortal portal)
        {
            m_Portal = portal;

            // Le ciel, la lumière ambiante et le brouillard sont ceux de cette scène, devenue la scène active.
            DynamicGI.UpdateEnvironment();
            m_Phase = Phase.Intro;
            m_Time = 0f;
            // Les panneaux d'erreur montent pendant la découverte : le premier à 5 s, puis de plus en plus vite.
            m_Panels.StartIntro(m_IntroTime);
            if (m_MusicSource != null && m_MusicSource.clip != null)
            {
                m_MusicSource.volume = m_MusicVolume;
                m_MusicSource.timeSamples = 0;
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
            // Pendant la découverte, le temps est celui de la musique : l'épreuve part sur sa montée, même après une
            // saccade (le chargement de la scène) ou si le son a démarré un peu en retard.
            if (m_Phase == Phase.Intro && MusicPlaying)
                m_Time = m_MusicSource.timeSamples / (float)m_MusicSource.clip.frequency;
            UpdateMusic();

            switch (m_Phase)
            {
                case Phase.Intro:
                    if (m_Time >= m_IntroTime)
                    {
                        m_Phase = Phase.Survive;
                        // « SURVIVEZ », et les dizaines de panneaux se ferment presque d'un coup.
                        m_Panels.Unleash();
                        var count = MonsterCount();
                        for (var i = 0; i < count; i++)
                            SpawnMonster();
                    }

                    break;

                case Phase.Survive:
                    for (var i = m_RespawnTimers.Count - 1; i >= 0; i--)
                    {
                        m_RespawnTimers[i] -= deltaTime;
                        if (m_RespawnTimers[i] > 0f)
                            continue;
                        m_RespawnTimers.RemoveAt(i);
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

        bool MusicPlaying => m_MusicSource != null && m_MusicSource.clip != null && m_MusicSource.isPlaying;

        // La musique joue telle quelle (sa montée est dans le morceau) ; à la fin de l'épreuve, elle s'étouffe et s'éteint.
        void UpdateMusic()
        {
            if (m_MusicSource == null)
                return;

            if (m_Phase == Phase.Over)
            {
                var deltaTime = Time.deltaTime;
                m_MusicSource.volume = Mathf.MoveTowards(m_MusicSource.volume, 0f, deltaTime * 0.6f);
                if (m_LowPass != null)
                    m_LowPass.cutoffFrequency = Mathf.MoveTowards(m_LowPass.cutoffFrequency, 600f, deltaTime * 15000f);
            }
            else
            {
                // Un monstre tout près : la musique baisse (jusqu'à moitié moins), pour bien l'entendre arriver.
                var duck = Mathf.Lerp(0.45f, 1f, Mathf.InverseLerp(3f, 15f, NearestMonsterDistance()));
                m_MusicSource.volume = Mathf.MoveTowards(m_MusicSource.volume, m_MusicVolume * duck, Time.deltaTime * 0.8f);
            }
        }

        float NearestMonsterDistance()
        {
            var head = Head;
            var nearest = float.MaxValue;
            if (head == null)
                return nearest;

            foreach (var monster in m_Monsters)
            {
                if (monster != null && monster.IsAlive)
                    nearest = Mathf.Min(nearest, FlatDistance(monster.transform.position, head.position));
            }

            return nearest;
        }

        // Le grésillement des monstres : 2 s de parasites numériques en boucle. Des paquets de 10 à 80 ms, chacun avec son
        // volume, sa « résolution » (échantillons répétés, comme un son écrasé) et son grain, parfois un silence, et quelques
        // craquements secs ; la fin se fond dans le début, pour boucler sans couture.
        static AudioClip BuildStaticClip()
        {
            const int rate = 44100;
            const int length = rate * 2;
            const int fade = rate / 20;
            var samples = new float[length + fade];
            var random = new System.Random(23);
            var index = 0;
            var smoothed = 0f;
            var peak = 0.001f;
            while (index < samples.Length)
            {
                var count = random.Next(rate / 100, rate * 8 / 100);
                var amplitude = random.NextDouble() < 0.15 ? 0f : (float)(0.2 + 0.8 * random.NextDouble());
                var hold = random.Next(1, 12);
                var grain = (float)(0.08 + 0.6 * random.NextDouble());
                var value = 0f;
                for (var k = 0; k < count && index < samples.Length; k++, index++)
                {
                    if (k % hold == 0)
                        value = (float)(random.NextDouble() * 2.0 - 1.0);
                    smoothed += (value - smoothed) * grain;
                    samples[index] = smoothed * amplitude;
                    peak = Mathf.Max(peak, Mathf.Abs(samples[index]));
                }
            }

            var data = new float[length];
            for (var k = 0; k < length; k++)
            {
                var sample = samples[k];
                if (k < fade)
                {
                    var t = k / (float)fade;
                    sample = sample * t + samples[length + k] * (1f - t);
                }

                data[k] = sample * (0.8f / peak);
            }

            for (var k = 0; k < 40; k++)
                data[random.Next(length)] = random.NextDouble() < 0.5 ? 0.9f : -0.9f;

            var clip = AudioClip.Create("Grésillement des monstres", length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ---------------------------------------------------------------- le monstre

        // Plusieurs monstres : un seul se figerait dès qu'on le regarde. Selon la difficulté (2 en Facile, 3 en Normal…).
        int MonsterCount()
        {
            var factor = DifficultyManager.Current != null ? DifficultyManager.Current.enemyCount : 1f;
            return Mathf.Clamp(Mathf.RoundToInt(m_MonsterCount * factor), 1, 6);
        }

        void SpawnMonster()
        {
            if (m_MonsterModels == null || m_MonsterModels.Length == 0 || m_Posts.Count == 0)
                return;

            var model = m_MonsterModels[m_ModelIndex % m_MonsterModels.Length];
            m_ModelIndex++;
            var speed = DifficultyManager.Current != null ? DifficultyManager.Current.enemySpeed : 1f;
            m_Monsters.Add(CorruptedMonster.Spawn(this, model, SpawnPost(), m_GlitchShader, speed));
        }

        /// <summary>Un monstre vient d'être tué : il éclate en pixels, et un autre le remplace, plus loin.</summary>
        public void MonsterKilled(CorruptedMonster monster)
        {
            Pixels(monster.Point, 40, 1.4f);
            Sfx.Play(m_KillClip, monster.Point, 1f, Random.Range(0.85f, 1.1f));
            m_Monsters.Remove(monster);
            Destroy(monster.gameObject);
            if (m_Phase == Phase.Survive)
                m_RespawnTimers.Add(m_RespawnDelay);
        }

        /// <summary>Ce pilier est pris par un monstre (caché derrière, ou en route) : les autres ne le choisissent pas.</summary>
        public void Claim(Transform post)
        {
            if (post != null)
                m_Claimed.Add(post);
        }

        public void Release(Transform post)
        {
            if (post != null)
                m_Claimed.Remove(post);
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
            foreach (var monster in m_Monsters)
            {
                if (monster == null)
                    continue;
                Pixels(monster.Point, 40, 1.4f);
                Destroy(monster.gameObject);
            }

            m_Monsters.Clear();
            m_RespawnTimers.Clear();

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
        /// Le pilier suivant : plus près du joueur (de 1,5 à 8 m) sans être collé à lui, libre, et <b>hors de son champ de
        /// vision</b> (à plus de 70° de son regard : sur les côtés, dans son dos). Les monstres le contournent au lieu de
        /// venir se figer sous ses yeux : il doit se retourner pour les surveiller. <paramref name="wide"/> : faute de mieux,
        /// un pilier qu'il voit du coin de l'œil (à plus de 30° de son regard). Null s'il n'y en a pas.
        /// </summary>
        public Transform NextPost(Transform current, float distance, bool wide = false)
        {
            var head = Head;
            if (head == null)
                return null;

            var farthest = distance - 1.5f;
            var nearest = Mathf.Max(3.2f, distance - 8f);
            var target = distance - 4f;
            var candidates = new List<Transform>();
            foreach (var minimumAngle in wide ? new[] { 70f, 30f } : new[] { 70f })
            {
                candidates.Clear();
                foreach (var post in m_Posts)
                {
                    if (post == current || m_Claimed.Contains(post))
                        continue;
                    var d = FlatDistance(post.position, head.position);
                    if (d <= farthest && d >= nearest && !InView(post.position, minimumAngle))
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

        // Loin du joueur, dans son champ de vision (jamais dans son dos), sur un pilier libre, et si possible écarté des
        // autres monstres (au moins 20° vus du joueur) : il ne peut pas tous les regarder à la fois.
        Transform SpawnPost()
        {
            var head = Head;
            if (head == null)
                return m_Posts[Random.Range(0, m_Posts.Count)];

            foreach (var spread in new[] { 20f, 0f })
            {
                foreach (var (cone, minimum) in new[] { (40f, 16f), (k_ViewCone, 11f), (k_WideCone, 8f) })
                {
                    var candidates = new List<Transform>();
                    foreach (var post in m_Posts)
                    {
                        if (!m_Claimed.Contains(post) && FlatDistance(post.position, head.position) >= minimum &&
                            InView(post.position, cone) && ApartFromMonsters(head.position, post.position, spread))
                            candidates.Add(post);
                    }

                    if (candidates.Count > 0)
                        return candidates[Random.Range(0, candidates.Count)];
                }
            }

            var farthest = m_Posts[0];
            foreach (var post in m_Posts)
            {
                if (FlatDistance(post.position, head.position) > FlatDistance(farthest.position, head.position))
                    farthest = post;
            }

            return farthest;
        }

        bool ApartFromMonsters(Vector3 viewer, Vector3 point, float angle)
        {
            var direction = point - viewer;
            direction.y = 0f;
            foreach (var monster in m_Monsters)
            {
                if (monster == null)
                    continue;
                var other = monster.transform.position - viewer;
                other.y = 0f;
                if (Vector3.Angle(direction, other) < angle)
                    return false;
            }

            return true;
        }

        static float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        /// <summary>Les monstres en vie : les panneaux ne les cachent pas.</summary>
        public void GetThreats(List<Vector3> points)
        {
            foreach (var monster in m_Monsters)
            {
                if (monster != null && monster.IsAlive)
                    points.Add(monster.Point);
            }
        }

        public AudioClip RandomGlitchClip() =>
            m_GlitchClips != null && m_GlitchClips.Length > 0 ? m_GlitchClips[Random.Range(0, m_GlitchClips.Length)] : null;

        public AudioClip RandomStepClip() =>
            m_StepClips != null && m_StepClips.Length > 0 ? m_StepClips[Random.Range(0, m_StepClips.Length)] : null;

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
            // Le flash cyan remplace la lueur de la laque : la texture de lueur est remplacée par du blanc.
            m_Block.Clear();
            m_Block.SetColor(k_EmissionColorId, new Color(0.2f, 1.6f, 2f));
            m_Block.SetTexture(k_EmissionMapId, Texture2D.whiteTexture);
            foreach (var renderer in gate.Renderers)
            {
                if (renderer != null)
                    renderer.SetPropertyBlock(glitched ? m_Block : null);
            }
        }
    }
}
