using System.Collections.Generic;
using Archery.Bows;
using Archery.Core;
using Archery.Player;
using Archery.Portal;
using Archery.UI;
using UnityEngine;

namespace Archery.Cyber
{
    /// <summary>
    /// Le toit cyberpunk (2ᵉ easter egg, GDD section 23 ; lore : Docs/Lore.md) : le haut d'un immeuble, la nuit, sous la
    /// pluie, au milieu d'une ville fausse et déchue. Le joueur est un intrus : THUNK essaie de le supprimer. Il tient une
    /// arbalète dans chaque main (<see cref="TwinCrossbows"/>) et reste sur une petite zone, sans se déplacer : il esquive
    /// avec le corps les balles (<see cref="GlitchBullets"/>) des tireurs immobiles qui l'entourent (<see cref="CyberShooter"/>).
    /// </summary>
    /// <remarks>
    /// L'épreuve : la découverte jusqu'à la montée de la musique (12 s), pendant que les panneaux « INTRUS DÉTECTÉ »
    /// montent, sans tireur ; puis « SUPPRESSION » : les tireurs arrivent tous, et il faut tenir 1 minute. Les tirs viennent toujours de devant
    /// le joueur (jamais dans le dos), annoncés (le tireur s'allume), de plus en plus serrés ; quand le joueur tire, un tireur
    /// lui répond (ils copient ses gestes). 3 touches : expulsé. Scène à part (<c>Dimension_Cyberpunk</c>), chargée par le
    /// <see cref="CyberPassage"/> de la carte, construite par <c>Archery > Toit cyberpunk > Reconstruire le décor</c>.
    /// <para>
    /// Le 24 octobre, l'épreuve était impossible : deux fois moins de tirs, plus lents et mieux annoncés, un seul tireur
    /// qui vise à la fois, des balles seules et des rafales au début, et un court répit après chaque touche (une rafale
    /// ne coûte plus qu'une touche). Des indices sonores : le tireur annonce l'attaque (un son par sorte d'attaque) et sa
    /// charge monte jusqu'au tir, en son 3D (<see cref="CyberCues"/>) ; les balles grésillent en approchant ; un tic
    /// pour chacune des 10 dernières secondes ; un cœur qui bat quand il ne reste qu'une touche.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public class CyberDimension : MonoBehaviour, IGlitchDimension
    {
        // Les tirs ne viennent que de ce demi-angle (°) autour du regard du joueur.
        const float k_FrontCone = 70f;

        // La part de son volume que garde la musique pendant la minute à tenir.
        const float k_MusicDuck = 0.7f;

        [Header("Scène (objets modifiables à la main)")]
        [Tooltip("Point d'arrivée du joueur, au milieu de la zone (son axe bleu donne la direction du regard).")]
        [SerializeField]
        Transform m_Spawn;

        [Tooltip("Les postes des tireurs (leurs enfants), sur les toits et les corniches autour.")]
        [SerializeField]
        Transform m_Posts;

        [Tooltip("La rambarde de la zone et les murets du toit (colliders) : les flèches les traversent.")]
        [SerializeField]
        Transform m_Rails;

        [Tooltip("La rambarde de la zone (dans Rails) : fine, elle ne cache pas les tireurs (les murets, si).")]
        [SerializeField]
        Transform m_Zone;

        [Tooltip("La musique : elle part du début à l'arrivée du joueur, et sa montée donne le départ (Intro Time).")]
        [SerializeField]
        AudioSource m_MusicSource;

        [SerializeField]
        AudioClip m_Music;

        [Range(0f, 1f)]
        [SerializeField]
        float m_MusicVolume = 0.8f;

        [Header("Modèles")]
        [Tooltip("Matériau des panneaux et des pixels (couleurs des sommets, transparent : Effects).")]
        [SerializeField]
        Material m_GlowMaterial;

        [Tooltip("Shader des tireurs (Archery/Glitch).")]
        [SerializeField]
        Shader m_GlitchShader;

        [Tooltip("L'apparence des tireurs (le mannequin d'entraînement).")]
        [SerializeField]
        GameObject m_ShooterModel;

        [Tooltip("Le modèle d'une arbalète : la poignée à l'origine, le canon vers +Z.")]
        [SerializeField]
        GameObject m_CrossbowModel;

        [Tooltip("La pointe de l'arbalète (d'où partent les flèches et les balles), dans le repère du modèle.")]
        [SerializeField]
        Vector3 m_CrossbowTip = new Vector3(0f, 0.096f, 0.285f);

        [SerializeField]
        Material m_CrossbowMaterial;

        [Header("Épreuve")]
        [Tooltip("Durée (s) de la découverte : la montée de la musique (12 s dans « Sad Nostalgie »).")]
        [SerializeField]
        float m_IntroTime = 12f;

        [Tooltip("Durée (s) à tenir.")]
        [SerializeField]
        float m_SurviveTime = 60f;

        [Tooltip("Touches avant d'être expulsé.")]
        [Min(1)]
        [SerializeField]
        int m_MaxHits = 3;

        // 24 octobre (l'épreuve était impossible) : 8 à 12 tireurs (12 à 20 avant), une attaque toutes les 2,6 s puis 1,4 s
        // (1,6 puis 0,55), des balles à 5 puis 6,5 m/s (6 puis 8), 1 s de visée (0,7), une réponse sur 7 (une sur 3).
        [Tooltip("Tireurs au départ, puis au plus.")]
        [SerializeField]
        Vector2Int m_ShooterCount = new Vector2Int(8, 12);

        [Tooltip("Temps (s) entre deux attaques, au début puis à la fin de la minute (compté seulement quand aucun tireur ne vise).")]
        [SerializeField]
        Vector2 m_AttackInterval = new Vector2(2.6f, 1.4f);

        [Tooltip("Vitesse (m/s) des balles, au début puis à la fin.")]
        [SerializeField]
        Vector2 m_BulletSpeed = new Vector2(5f, 6.5f);

        [Tooltip("Temps (s) pendant lequel le tireur s'allume (et sa charge monte) avant de tirer.")]
        [SerializeField]
        float m_AimTime = 1f;

        [Tooltip("Chance qu'un tireur réponde quand le joueur tire (ils copient ses gestes).")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_AnswerChance = 0.15f;

        [Tooltip("Délai (s) avant qu'un tireur effacé soit remplacé, au début puis à la fin.")]
        [SerializeField]
        Vector2 m_RespawnDelay = new Vector2(3f, 1.5f);

        [Tooltip("Répit (s) après une touche : les balles le traversent (une rafale ne coûte qu'une touche).")]
        [SerializeField]
        float m_HitGrace = 1.2f;

        [Tooltip("Part de la minute (0 à 1) avec seulement des balles seules et des rafales, avant les lignes et les colonnes.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_EasyStart = 0.2f;

        [Header("Décor")]
        [Tooltip("Les reflets du toit mouillé : rendus une fois, à l'arrivée du joueur.")]
        [SerializeField]
        ReflectionProbe m_Reflections;

        [Header("Panneaux d'erreur")]
        [TextArea(1, 3)]
        [SerializeField]
        string[] m_FakeErrors =
        {
            "INTRUS DÉTECTÉ",
            "Accès non autorisé\nsecteur 7",
            "Processus étranger :\nJOUEUR.exe",
            "Suppression en cours…",
            "Signature inconnue",
            "Quarantaine activée",
            "Tu n'as rien à faire ici",
            "Ce n'est pas ton monde",
            "Retourne jouer",
            "Copie corrompue détectée",
            "Mémoire endommagée :\nville.dat",
            "Ils te regardent",
        };

        [Header("Sons")]
        [SerializeField]
        AudioClip[] m_GlitchClips = new AudioClip[0];

        [SerializeField]
        AudioClip m_PanelOpenClip;

        [SerializeField]
        AudioClip m_PanelCloseClip;

        [Tooltip("Le tir d'un tireur.")]
        [SerializeField]
        AudioClip m_ShotClip;

        [Tooltip("Une balle frôle la tête.")]
        [SerializeField]
        AudioClip m_PassClip;

        [Tooltip("Une balle touche le joueur.")]
        [SerializeField]
        AudioClip m_HitClip;

        [Tooltip("Un tireur effacé.")]
        [SerializeField]
        AudioClip m_KillClip;

        [Tooltip("Le tir des arbalètes du joueur.")]
        [SerializeField]
        AudioClip m_CrossbowClip;

        [SerializeField]
        AudioClip m_SuccessClip;

        [SerializeField]
        AudioClip m_FailClip;

        [Tooltip("Le tic des dernières secondes.")]
        [SerializeField]
        AudioClip m_TickClip;

        [Tooltip("Les dernières secondes annoncées par un tic.")]
        [SerializeField]
        int m_TickSeconds = 10;

        enum Phase
        {
            Waiting,
            Intro,
            Survive,
            Over,
        }

        static readonly RaycastHit[] s_SightHits = new RaycastHit[16];

        readonly List<Transform> m_FreePosts = new List<Transform>();
        readonly List<CyberShooter> m_Shooters = new List<CyberShooter>();
        readonly List<CyberShooter> m_Candidates = new List<CyberShooter>();
        readonly List<float> m_RespawnTimers = new List<float>();
        readonly List<Collider> m_RailColliders = new List<Collider>();
        CyberPassage m_Passage;
        ErrorPanels m_Panels;
        GlitchBullets m_Bullets;
        AudioLowPassFilter m_LowPass;
        AudioSource m_Heartbeat;
        Phase m_Phase;
        float m_Time;
        float m_AttackTimer;
        float m_EndTimer = -1f;
        float m_GraceUntil;
        int m_LastTick;
        int m_Hits;
        bool m_Success;

        public static CyberDimension Instance { get; private set; }

        public Transform PlayerSpawn => m_Spawn != null ? m_Spawn : transform;

        public bool Running => m_Phase == Phase.Intro || m_Phase == Phase.Survive;

        public float TimeLeft => m_Phase == Phase.Survive ? Mathf.Max(0f, m_SurviveTime - (m_Time - m_IntroTime))
                              : m_Phase == Phase.Over ? 0f : m_SurviveTime;

        public float Elapsed => m_Time;

        public Transform Head => PlayerRig.Instance != null ? PlayerRig.Instance.Head : null;

        public float FloorY => transform.position.y;

        public Material GlowMaterial => m_GlowMaterial;

        // Avancement de la minute à tenir (0 au départ, 1 à la fin) : les attaques se resserrent.
        float Progress => m_Phase == Phase.Survive ? Mathf.Clamp01((m_Time - m_IntroTime) / Mathf.Max(1f, m_SurviveTime)) : 0f;

        void Awake()
        {
            Instance = this;
            if (m_Posts != null)
            {
                foreach (Transform post in m_Posts)
                    m_FreePosts.Add(post);
            }

            if (m_Rails != null)
            {
                m_Rails.GetComponentsInChildren(m_RailColliders);
                ArrowIgnore.Register(m_RailColliders);
            }

            SetUpMusic();
            m_Panels = gameObject.AddComponent<ErrorPanels>();
            m_Panels.Configure(this, m_GlowMaterial, m_PanelOpenClip, m_PanelCloseClip, m_FakeErrors);
            m_Panels.SetWords("INTRUS DÉTECTÉ", "ALERTE", "SUPPRESSION", "TEMPS RESTANT");
            m_Bullets = new GameObject("Balles").AddComponent<GlitchBullets>();
            m_Bullets.transform.SetParent(transform, false);
            m_Bullets.Configure(new Color(1f, 0.08f, 0.1f), OnPlayerHit, m_PassClip);

            // Le cœur qui bat quand il ne reste qu'une touche (2D : il est dans la tête du joueur).
            m_Heartbeat = gameObject.AddComponent<AudioSource>();
            m_Heartbeat.clip = CyberCues.Heartbeat;
            m_Heartbeat.loop = true;
            m_Heartbeat.playOnAwake = false;
            m_Heartbeat.spatialBlend = 0f;
            m_Heartbeat.volume = 0.7f;
        }

        void OnEnable() => TwinCrossbows.Fired += OnPlayerFired;

        void OnDisable() => TwinCrossbows.Fired -= OnPlayerFired;

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            ArrowIgnore.Unregister(m_RailColliders);
            TwinCrossbows.Unequip();
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

        /// <summary>Le joueur vient d'arriver : les arbalètes en main, la découverte commence.</summary>
        public void Begin(CyberPassage passage)
        {
            m_Passage = passage;
            DynamicGI.UpdateEnvironment();
            if (m_Reflections != null)
                m_Reflections.RenderProbe();
            m_Phase = Phase.Intro;
            m_Time = 0f;
            m_Hits = 0;
            m_GraceUntil = 0f;
            m_LastTick = -1;
            m_Panels.StartIntro(m_IntroTime);
            TwinCrossbows.Equip(m_CrossbowModel, m_CrossbowMaterial, m_CrossbowClip, m_CrossbowTip);
            if (m_MusicSource != null && m_MusicSource.clip != null)
            {
                m_MusicSource.volume = m_MusicVolume;
                m_MusicSource.timeSamples = 0;
                m_MusicSource.Play();
            }
        }

        void Update()
        {
            if (m_Phase == Phase.Waiting)
                return;

            var deltaTime = Time.deltaTime;
            m_Time += deltaTime;
            // Pendant la découverte, le temps est celui de la musique : « SUPPRESSION » tombe sur sa montée.
            if (m_Phase == Phase.Intro && MusicPlaying)
                m_Time = m_MusicSource.timeSamples / (float)m_MusicSource.clip.frequency;
            UpdateMusic(deltaTime);

            switch (m_Phase)
            {
                case Phase.Intro:
                    // Pas de tireur pendant les erreurs (20 octobre) : ils arrivent tous à « SUPPRESSION », en moins d'une
                    // seconde, pendant que les panneaux se ferment.
                    if (m_Time >= m_IntroTime)
                    {
                        m_Phase = Phase.Survive;
                        m_Panels.Unleash();
                        for (var i = m_Shooters.Count + m_RespawnTimers.Count; i < m_ShooterCount.x; i++)
                            m_RespawnTimers.Add(0.8f * i / Mathf.Max(1, m_ShooterCount.x));
                        m_AttackTimer = 1.2f;
                    }

                    break;

                case Phase.Survive:
                    UpdateRespawns(deltaTime);
                    // Un seul tireur vise à la fois : le temps jusqu'à l'attaque suivante ne court qu'entre deux visées.
                    if (!AnyAiming())
                        m_AttackTimer -= deltaTime;
                    if (m_AttackTimer <= 0f && Attack())
                        m_AttackTimer = AttackInterval() * Random.Range(0.85f, 1.15f);

                    UpdateTicks();
                    if (TimeLeft <= 0f)
                        Finish(true);
                    break;

                case Phase.Over:
                    m_EndTimer -= deltaTime;
                    if (m_EndTimer <= 0f && m_EndTimer > -1f)
                    {
                        m_EndTimer = -10f;
                        if (m_Passage != null)
                            m_Passage.EndTrial(m_Success);
                        else
                            Debug.Log($"CyberDimension : épreuve {(m_Success ? "réussie" : "ratée")} (sans passage : test de la scène seule).");
                    }

                    break;
            }
        }

        bool MusicPlaying => m_MusicSource != null && m_MusicSource.clip != null && m_MusicSource.isPlaying;

        void UpdateMusic(float deltaTime)
        {
            if (m_MusicSource == null)
                return;

            // Pendant la minute, la musique baisse un peu : les annonces des tireurs doivent s'entendre (24 octobre).
            if (m_Phase == Phase.Survive)
                m_MusicSource.volume = Mathf.MoveTowards(m_MusicSource.volume, m_MusicVolume * k_MusicDuck, deltaTime * 0.5f);
            if (m_Phase != Phase.Over)
                return;
            m_MusicSource.volume = Mathf.MoveTowards(m_MusicSource.volume, 0f, deltaTime * 0.6f);
            if (m_LowPass != null)
                m_LowPass.cutoffFrequency = Mathf.MoveTowards(m_LowPass.cutoffFrequency, 600f, deltaTime * 15000f);
        }

        // ---------------------------------------------------------------- les tireurs

        // Un tireur sur un poste libre.
        bool SpawnShooter()
        {
            if (m_FreePosts.Count == 0)
                return false;

            var index = Random.Range(0, m_FreePosts.Count);
            var post = m_FreePosts[index];
            m_FreePosts.RemoveAt(index);
            var shooter = CyberShooter.Spawn(this, post, m_ShooterModel, m_CrossbowModel, m_CrossbowTip, m_GlitchShader, m_CrossbowMaterial);
            m_Shooters.Add(shooter);
            Sfx.Play(RandomGlitchClip(), post.position, 0.5f, Random.Range(0.8f, 1.1f));
            return true;
        }

        /// <summary>Un tireur effacé par une flèche : son poste se libère, un autre viendra.</summary>
        public void ShooterKilled(CyberShooter shooter)
        {
            m_Shooters.Remove(shooter);
            if (shooter.Post != null)
                m_FreePosts.Add(shooter.Post);
            Sfx.Play(m_KillClip, shooter.Point, 0.9f, Random.Range(0.9f, 1.1f));
            if (m_Phase == Phase.Survive)
                m_RespawnTimers.Add(Mathf.Lerp(m_RespawnDelay.x, m_RespawnDelay.y, Progress));
        }

        void UpdateRespawns(float deltaTime)
        {
            var wanted = Mathf.RoundToInt(Mathf.Lerp(m_ShooterCount.x, m_ShooterCount.y, Progress));
            for (var i = m_RespawnTimers.Count - 1; i >= 0; i--)
            {
                m_RespawnTimers[i] -= deltaTime;
                if (m_RespawnTimers[i] > 0f)
                    continue;
                m_RespawnTimers.RemoveAt(i);
                SpawnShooter();
            }

            // De nouveaux tireurs arrivent au fil de la minute.
            if (m_Shooters.Count + m_RespawnTimers.Count < wanted)
                m_RespawnTimers.Add(Random.Range(0.5f, 1.5f));
        }

        // ---------------------------------------------------------------- les attaques

        // Un tireur de devant vise, s'allume et annonce son attaque, puis tire : une balle, une rafale, une ligne
        // (s'accroupir) ou une colonne (se pencher). False : aucun tireur devant le joueur, on réessaie.
        bool Attack()
        {
            var head = Head;
            if (head == null)
                return false;

            var shooter = PickShooter();
            if (shooter == null)
                return false;

            var pattern = PickPattern(Progress);
            var target = head.position;
            shooter.Aim(target, m_AimTime, pattern, () => FirePattern(shooter, pattern, target));
            return true;
        }

        // Au début, des balles seules et des rafales ; ensuite, des lignes et des colonnes aussi, un peu plus avec la fin
        // de la minute.
        int PickPattern(float progress)
        {
            var roll = Random.value;
            if (progress < m_EasyStart)
                return roll < 0.75f ? 0 : 1;
            return roll < 0.45f - 0.15f * progress ? 0 : roll < 0.7f ? 1 : roll < 0.85f ? 2 : 3;
        }

        float AttackInterval() => Mathf.Lerp(m_AttackInterval.x, m_AttackInterval.y, Progress);

        bool AnyAiming()
        {
            foreach (var shooter in m_Shooters)
            {
                if (shooter != null && shooter.Aiming)
                    return true;
            }

            return false;
        }

        // Un tic pour chacune des dernières secondes, plus aigu pour les trois dernières.
        void UpdateTicks()
        {
            var second = Mathf.CeilToInt(TimeLeft);
            if (second == m_LastTick || second <= 0 || second > m_TickSeconds)
                return;
            m_LastTick = second;
            var head = Head;
            if (head != null)
                Sfx.Play(m_TickClip, head.position, 0.6f, second <= 3 ? 1.35f : 1f, 0f);
        }

        // Un tireur prêt, devant le joueur (jamais dans le dos) et qu'il voit, au hasard.
        CyberShooter PickShooter()
        {
            m_Candidates.Clear();
            var head = Head;
            foreach (var shooter in m_Shooters)
            {
                if (shooter != null && shooter.Ready && InFront(shooter.Point, k_FrontCone) && (head == null || Visible(shooter, head.position)))
                    m_Candidates.Add(shooter);
            }

            return m_Candidates.Count > 0 ? m_Candidates[Random.Range(0, m_Candidates.Count)] : null;
        }

        // Le joueur voit ce tireur : rien d'opaque entre ses yeux et la poitrine du tireur (24 octobre : des tireurs tiraient
        // de derrière un muret ou le réservoir, sans qu'on puisse les voir, ni voir leurs balles, ni les toucher). Les murets
        // comptent, même si les flèches les traversent ; pas la rambarde de la zone, fine, ni le joueur, ni le tireur.
        bool Visible(CyberShooter shooter, Vector3 eye)
        {
            var toShooter = shooter.Point - eye;
            var distance = toShooter.magnitude;
            if (distance < 0.5f)
                return true;

            var count = Physics.RaycastNonAlloc(eye, toShooter / distance, s_SightHits, distance, Physics.DefaultRaycastLayers,
                                                QueryTriggerInteraction.Ignore);
            var rig = PlayerRig.Instance;
            var player = rig != null && rig.Origin != null ? rig.Origin.transform : null;
            for (var i = 0; i < count; i++)
            {
                var hit = s_SightHits[i].collider.transform;
                if (hit.IsChildOf(shooter.transform) || (player != null && hit.IsChildOf(player)) || (m_Zone != null && hit.IsChildOf(m_Zone)))
                    continue;
                return false;
            }

            return true;
        }

        void FirePattern(CyberShooter shooter, int pattern, Vector3 target)
        {
            if (shooter == null || !shooter.IsAlive || m_Phase != Phase.Survive)
                return;

            var muzzle = shooter.Muzzle;
            var speed = Mathf.Lerp(m_BulletSpeed.x, m_BulletSpeed.y, Progress);
            var toTarget = target - muzzle;
            var forward = toTarget.normalized;
            var side = Vector3.Cross(Vector3.up, forward).normalized;
            Sfx.Play(m_ShotClip, muzzle, 0.8f, Random.Range(0.9f, 1.1f));

            switch (pattern)
            {
                case 0:
                    // Une balle, droit sur la tête.
                    m_Bullets.Fire(muzzle, forward * speed);
                    break;

                case 1:
                    // Une rafale de trois, à 0,22 s d'écart (0,18 avant le 24 octobre) : on ne l'esquive qu'en bougeant
                    // franchement.
                    m_Bullets.Fire(muzzle, forward * speed);
                    StartCoroutine(Burst(shooter, target, speed, 2));
                    break;

                case 2:
                    // Une ligne à hauteur de tête, sur 2 m : s'accroupir.
                    for (var i = -2; i <= 2; i++)
                        m_Bullets.Fire(muzzle, (target + side * (i * 0.45f) - muzzle).normalized * speed);
                    break;

                default:
                    // Une colonne de la tête aux genoux : se pencher sur le côté.
                    for (var i = 0; i < 4; i++)
                        m_Bullets.Fire(muzzle, (target + Vector3.down * (i * 0.35f) - muzzle).normalized * speed);
                    break;
            }
        }

        System.Collections.IEnumerator Burst(CyberShooter shooter, Vector3 target, float speed, int count)
        {
            for (var i = 0; i < count; i++)
            {
                yield return new WaitForSeconds(0.22f);
                if (shooter == null || !shooter.IsAlive || m_Phase != Phase.Survive)
                    yield break;
                var muzzle = shooter.Muzzle;
                m_Bullets.Fire(muzzle, (target - muzzle).normalized * speed);
            }
        }

        // Le joueur tire : parfois, un tireur de devant lui répond, comme en miroir (ils copient ses gestes). Depuis le
        // 24 octobre, la réponse remplace l'attaque suivante (elle ne s'y ajoute plus), jamais pendant une autre visée,
        // et elle s'annonce comme les autres (0,7 s de visée ; 0,45 avant).
        void OnPlayerFired(Vector3 origin, Vector3 direction)
        {
            if (m_Phase != Phase.Survive || Random.value > m_AnswerChance || Head == null || AnyAiming())
                return;

            var shooter = PickShooter();
            if (shooter != null)
            {
                var target = Head.position;
                shooter.Aim(target, 0.7f, 0, () => FirePattern(shooter, 0, target));
                m_AttackTimer = Mathf.Max(m_AttackTimer, AttackInterval());
            }
        }

        bool InFront(Vector3 point, float halfAngle)
        {
            var head = Head;
            if (head == null)
                return true;
            var forward = head.forward;
            forward.y = 0f;
            var to = point - head.position;
            to.y = 0f;
            return forward.sqrMagnitude < 1e-4f || Vector3.Angle(forward, to) <= halfAngle;
        }

        // ---------------------------------------------------------------- le joueur touché, la fin

        void OnPlayerHit(Vector3 point)
        {
            // Le répit après une touche : les autres balles de la même attaque traversent le joueur.
            if (m_Phase != Phase.Survive || m_Time < m_GraceUntil)
                return;

            m_GraceUntil = m_Time + m_HitGrace;
            m_Hits++;
            var head = Head;
            Sfx.Play(m_HitClip, head != null ? head.position : point, 1f, Random.Range(0.9f, 1.05f), 0f);
            var rig = PlayerRig.Instance;
            if (rig != null)
            {
                foreach (var hand in rig.Hands)
                    Haptics.Pulse(hand, 0.9f, 0.2f);
            }

            var left = m_MaxHits - m_Hits;
            if (left <= 0)
            {
                Finish(false);
                return;
            }

            if (left == 1 && m_Heartbeat != null && !m_Heartbeat.isPlaying)
            {
                m_Heartbeat.outputAudioMixerGroup = Sfx.Output;
                m_Heartbeat.Play();
            }

            HeadMessages.Show(left > 1 ? $"TOUCHÉ\n<size=60%>encore {left} touches</size>" : "TOUCHÉ\n<size=60%>la prochaine te supprime</size>",
                              new Color(1f, 0.25f, 0.25f), 1.6f, 1f);
        }

        void Finish(bool success)
        {
            if (m_Phase == Phase.Over)
                return;

            m_Phase = Phase.Over;
            m_Success = success;
            m_EndTimer = success ? 2.8f : 2f;
            m_Bullets.Clear();
            if (m_Heartbeat != null)
                m_Heartbeat.Stop();
            TwinCrossbows.Unequip();
            foreach (var shooter in m_Shooters)
            {
                if (shooter != null)
                    shooter.Erase();
            }

            m_Shooters.Clear();
            var head = Head;
            if (head != null)
                Sfx.Play(success ? m_SuccessClip : m_FailClip, head.position, 1f, success ? 0.85f : 0.8f, 0f);
            if (success)
                m_Panels.ShowCentered("ERREUR", "ÉCHEC DE LA\nSUPPRESSION", 2.8f, 1.8f, true);
            else
                m_Panels.ShowCentered("SUPPRESSION", "INTRUS\nSUPPRIMÉ", 2f, 1.8f, true);
        }

        // ---------------------------------------------------------------- les panneaux

        public void GetThreats(List<Vector3> points)
        {
            foreach (var shooter in m_Shooters)
            {
                if (shooter != null && shooter.IsAlive)
                    points.Add(shooter.Point);
            }
        }

        public AudioClip RandomGlitchClip() =>
            m_GlitchClips != null && m_GlitchClips.Length > 0 ? m_GlitchClips[Random.Range(0, m_GlitchClips.Length)] : null;
    }
}
