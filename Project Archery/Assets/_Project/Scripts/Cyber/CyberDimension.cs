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
    /// </remarks>
    [DisallowMultipleComponent]
    public class CyberDimension : MonoBehaviour, IGlitchDimension
    {
        // Les tirs ne viennent que de ce demi-angle (°) autour du regard du joueur.
        const float k_FrontCone = 70f;

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

        [Tooltip("Tireurs au départ, puis au plus.")]
        [SerializeField]
        Vector2Int m_ShooterCount = new Vector2Int(12, 20);

        [Tooltip("Temps (s) entre deux attaques, au début puis à la fin de la minute.")]
        [SerializeField]
        Vector2 m_AttackInterval = new Vector2(1.6f, 0.55f);

        [Tooltip("Vitesse (m/s) des balles, au début puis à la fin.")]
        [SerializeField]
        Vector2 m_BulletSpeed = new Vector2(6f, 8f);

        [Tooltip("Temps (s) pendant lequel le tireur s'allume avant de tirer.")]
        [SerializeField]
        float m_AimTime = 0.7f;

        [Tooltip("Chance qu'un tireur réponde quand le joueur tire (ils copient ses gestes).")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_AnswerChance = 0.35f;

        [Tooltip("Délai (s) avant qu'un tireur effacé soit remplacé, au début puis à la fin.")]
        [SerializeField]
        Vector2 m_RespawnDelay = new Vector2(3f, 1.2f);

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

        enum Phase
        {
            Waiting,
            Intro,
            Survive,
            Over,
        }

        readonly List<Transform> m_FreePosts = new List<Transform>();
        readonly List<CyberShooter> m_Shooters = new List<CyberShooter>();
        readonly List<CyberShooter> m_Candidates = new List<CyberShooter>();
        readonly List<float> m_RespawnTimers = new List<float>();
        readonly List<Collider> m_RailColliders = new List<Collider>();
        CyberPassage m_Passage;
        ErrorPanels m_Panels;
        GlitchBullets m_Bullets;
        AudioLowPassFilter m_LowPass;
        Phase m_Phase;
        float m_Time;
        float m_AttackTimer;
        float m_EndTimer = -1f;
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
            m_Phase = Phase.Intro;
            m_Time = 0f;
            m_Hits = 0;
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
                    m_AttackTimer -= deltaTime;
                    if (m_AttackTimer <= 0f)
                    {
                        m_AttackTimer = Mathf.Lerp(m_AttackInterval.x, m_AttackInterval.y, Progress) * Random.Range(0.8f, 1.2f);
                        Attack();
                    }

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
            if (m_MusicSource == null || m_Phase != Phase.Over)
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

        // Un tireur de devant vise, s'allume, puis tire : une balle, une rafale, une ligne (s'accroupir) ou une colonne
        // (se pencher). Les plus serrées viennent avec la fin de la minute.
        void Attack()
        {
            var head = Head;
            if (head == null)
                return;

            var shooter = PickShooter();
            if (shooter == null)
                return;

            var progress = Progress;
            var roll = Random.value;
            var pattern = roll < 0.4f - 0.15f * progress ? 0 : roll < 0.7f ? 1 : roll < 0.85f ? 2 : 3;
            var target = head.position;
            shooter.Aim(target, m_AimTime, () => FirePattern(shooter, pattern, target));
        }

        // Un tireur prêt, devant le joueur (jamais dans le dos), au hasard.
        CyberShooter PickShooter()
        {
            m_Candidates.Clear();
            foreach (var shooter in m_Shooters)
            {
                if (shooter != null && shooter.Ready && InFront(shooter.Point, k_FrontCone))
                    m_Candidates.Add(shooter);
            }

            return m_Candidates.Count > 0 ? m_Candidates[Random.Range(0, m_Candidates.Count)] : null;
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
                    // Une rafale de trois, à 0,18 s d'écart : on ne l'esquive qu'en bougeant franchement.
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
                yield return new WaitForSeconds(0.18f);
                if (shooter == null || !shooter.IsAlive || m_Phase != Phase.Survive)
                    yield break;
                var muzzle = shooter.Muzzle;
                m_Bullets.Fire(muzzle, (target - muzzle).normalized * speed);
            }
        }

        // Le joueur tire : parfois, un tireur de devant lui répond, comme en miroir (ils copient ses gestes).
        void OnPlayerFired(Vector3 origin, Vector3 direction)
        {
            if (m_Phase != Phase.Survive || Random.value > m_AnswerChance || Head == null)
                return;

            var shooter = PickShooter();
            if (shooter != null)
            {
                var target = Head.position;
                shooter.Aim(target, 0.45f, () => FirePattern(shooter, 0, target));
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
            if (m_Phase != Phase.Survive)
                return;

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
