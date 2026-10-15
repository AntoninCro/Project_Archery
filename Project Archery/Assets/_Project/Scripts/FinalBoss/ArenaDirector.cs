using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.Player;
using Archery.UI;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace Archery.FinalBoss
{
    /// <summary>
    /// La mise en scène de l'arène du dragon (GDD, section 23), au rythme de la musique :
    /// <list type="number">
    /// <item>le joueur arrive dans le noir complet, la musique commence ;</item>
    /// <item>après les 4 premiers temps, les cristaux du plafond s'allument, un quart sur chaque temps ;</item>
    /// <item>la barre de PV du dragon apparaît et se remplit, avec son nom ;</item>
    /// <item>le dragon se réveille et rugit : le combat commence. Les cristaux pulsent ensuite sur chaque temps,
    /// et virent au rouge quand il devient enragé ;</item>
    /// <item>à sa mort, deux cibles apparaissent : continuer en mode infini, ou terminer la partie.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Tout se règle en temps de la musique : il suffit de donner le BPM et l'instant du premier temps du morceau.
    /// Raccourcis de test (éditeur) : <b>N</b> passe l'intro, puis tue le dragon ; après la victoire, <b>I</b> choisit
    /// le mode infini et <b>T</b> termine la partie.
    /// </remarks>
    [DisallowMultipleComponent]
    public class ArenaDirector : MonoBehaviour
    {
        [Header("Références")]
        [Tooltip("Point d'arrivée du joueur (pieds au sol, axe Z bleu vers le dragon).")]
        [SerializeField]
        Transform m_PlayerSpawn;

        [SerializeField]
        DragonBoss m_Dragon;

        [SerializeField]
        CeilingCrystals m_Crystals;

        [SerializeField]
        ArenaShape m_Arena;

        [SerializeField]
        ArenaLava m_Lava;

        ArenaTurrets m_Turrets;

        [Header("Musique")]
        [SerializeField]
        AudioClip m_Music;

        [Tooltip("Tempo du morceau (temps par minute).")]
        [SerializeField]
        float m_Bpm = 140.26f;

        [Tooltip("Instant (s) du premier temps dans le fichier.")]
        [SerializeField]
        float m_FirstBeat = 0.04f;

        [Tooltip("À la fin du morceau, il reprend à partir de ce temps (16 : après l'intro). -1 : pas de boucle.")]
        [SerializeField]
        int m_LoopFromBeat = 16;

        [Range(0f, 1f)]
        [SerializeField]
        float m_MusicVolume = 0.9f;

        [Tooltip("Groupe « Music » du mixer (curseur de volume des paramètres).")]
        [SerializeField]
        AudioMixerGroup m_MusicGroup;

        [Tooltip("Gain (dB) ajouté au groupe Music du mixer pendant le combat, au-dessus du réglage des paramètres. La musique " +
                 "de la carte est coupée dans l'arène : seule celle du boss en profite. Remis comme avant en quittant l'arène.")]
        [Range(0f, 12f)]
        [SerializeField]
        float m_MusicBoostDb = 6f;

        [Tooltip("Paramètre exposé du mixer pour le volume de la musique (celui des paramètres).")]
        [SerializeField]
        string m_MusicVolumeParameter = "MusicVolume";

        float m_MusicBaseDb;
        bool m_MusicBoosted;
        bool m_DragonDashed;
        bool m_DragonBreathing;

        [Header("Intro (en temps de l'intro, le premier temps est le 0)")]
        [Tooltip("Nombre de temps de la musique dans un temps de l'intro : 4 = l'intro suit le quart du tempo (un temps de l'intro par mesure).")]
        [Min(1)]
        [SerializeField]
        int m_MusicBeatsPerIntroBeat = 4;

        [Tooltip("Volume de la musique dans le noir, avant que les cristaux s'allument (part du volume normal). " +
                 "Elle remonte à mesure que les quarts de cristaux s'allument.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_DarkMusicVolume = 0.6f;

        [Tooltip("Temps où s'allume le premier quart des cristaux (4 : après les 4 premiers temps).")]
        [SerializeField]
        int m_CrystalsFromBeat = 4;

        [Tooltip("Temps entre deux quarts de cristaux (1 : un quart sur chaque temps).")]
        [SerializeField]
        int m_BeatsPerCrystalQuarter = 1;

        [Tooltip("Temps où le dragon s'envole du haut de la coulée de lave (7 : avec le dernier quart de cristaux) ; il pique sur " +
                 "le centre de l'arène et s'y pose juste avant la barre. Pendant que la barre se remplit, il crache son feu vers la voûte.")]
        [SerializeField]
        float m_DragonDashBeat = 7f;

        [Tooltip("Temps où la barre de PV apparaît (et où les coulées de lave commencent à descendre les parois).")]
        [SerializeField]
        int m_BarFromBeat = 8;

        [Tooltip("Durée (en temps) du remplissage de la barre.")]
        [SerializeField]
        int m_BarFillBeats = 2;

        [Tooltip("Durée (en temps) de la descente des coulées de lave, qui commence avec la barre (2 : en même temps que la barre).")]
        [SerializeField]
        int m_LavaFallBeats = 2;

        [Tooltip("Temps où le dragon se réveille (10 : 17,1 s, dès que la barre est pleine).")]
        [SerializeField]
        int m_WakeBeat = 10;

        [Header("Lumière")]
        [Tooltip("Lumière ambiante une fois tous les cristaux allumés : grotte teintée de rouge, assez sombre.")]
        [SerializeField]
        Color m_AmbientLit = new Color(0.2f, 0.07f, 0.06f);

        [Tooltip("Lumière ambiante pendant la rage du dragon.")]
        [SerializeField]
        Color m_AmbientRage = new Color(0.28f, 0.04f, 0.03f);

        [SerializeField]
        Color m_FogColor = new Color(0.09f, 0.02f, 0.015f);

        [SerializeField]
        float m_FogDensity = 0.018f;

        [Tooltip("Lueur de la lave une fois la grotte éclairée (multiplie sa couleur).")]
        [SerializeField]
        float m_LavaGlow = 1.6f;

        [Tooltip("Surcroît d'éclat des cristaux sur chaque temps, pendant le combat.")]
        [SerializeField]
        float m_BeatPulse = 0.35f;

        [SerializeField]
        Color m_RageTint = new Color(1f, 0.22f, 0.16f);

        [SerializeField]
        Color m_VictoryTint = new Color(1f, 0.85f, 0.6f);

        [Header("Victoire")]
        [Tooltip("Délai (s) entre la mort du dragon et l'apparition des deux cibles.")]
        [SerializeField]
        float m_ChoiceDelay = 4.5f;

        [Tooltip("Matériau des cibles (couleurs des sommets).")]
        [SerializeField]
        Material m_ChoiceMaterial;

        [SerializeField]
        AudioClip m_ChoiceClip;


        enum Stage
        {
            Waiting,
            Intro,
            Fight,
            Victory,
            Chosen,
            Defeat,
        }

        static readonly int k_ArenaLavaGlowId = Shader.PropertyToID("_ArenaLavaGlow");
        static readonly int k_LavaFallCutoffId = Shader.PropertyToID("_LavaFallCutoffY");

        AudioSource m_SourceA;
        AudioSource m_SourceB;
        AudioSource m_Current;
        AudioSource m_Pending;
        double m_SegmentDspStart;
        double m_SegmentSongStart;
        double m_SegmentDspEnd;
        double m_PendingDspStart;
        bool m_MusicStarted;
        float m_MusicFade = 1f;
        float m_MusicLevel = 1f;
        bool m_FadingOut;

        readonly float[] m_LitTime = { -1f, -1f, -1f, -1f };
        Light[] m_LavaLights = System.Array.Empty<Light>();
        BossHealthBar m_Bar;
        Stage m_Stage = Stage.Waiting;
        float m_Light;
        float m_StageTime;
        Color m_Ambient;
        bool m_Raging;
        ArenaChoiceTarget m_EndlessTarget;
        ArenaChoiceTarget m_FinishTarget;

        public static ArenaDirector Instance { get; private set; }

        /// <summary>
        /// Là où arrive le joueur. Dans l'arène en cubes, le repère suit la case d'arrivée du plan (S) : il reste juste si
        /// le plan ou la taille des cubes change.
        /// </summary>
        public Transform PlayerSpawn
        {
            get
            {
                if (m_PlayerSpawn != null && m_Arena is ArenaGridBuilder grid && grid.isActiveAndEnabled)
                {
                    var spawn = grid.SpawnPoint;
                    m_PlayerSpawn.SetPositionAndRotation(spawn.position, spawn.rotation);
                }

                return m_PlayerSpawn;
            }
        }

        double SecondsPerBeat => 60.0 / Mathf.Max(1f, m_Bpm);

        // Pour les lumières de l'arène (ArenaLights).

        /// <summary>Temps de la musique en cours (0 au premier temps, avec sa fraction) ; -1 avant la musique.</summary>
        public double Beat => CurrentBeat;

        /// <summary>Tempo de la musique (temps par minute).</summary>
        public float Bpm => m_Bpm;

        /// <summary>Allumage de l'arène : 0 dans le noir du début de l'intro, 1 une fois les cristaux allumés.</summary>
        public float LightLevel => m_Light;

        /// <summary>Le combat est en cours (après l'intro, avant la mort du dragon).</summary>
        public bool IsFighting => m_Stage == Stage.Fight;

        /// <summary>Le dragon est mort (en attendant le choix du joueur, et après).</summary>
        public bool IsVictory => m_Stage == Stage.Victory || m_Stage == Stage.Chosen;

        /// <summary>Surcroît d'éclat de la lave (0 : normal ; 1 : deux fois plus vive ; -1 : éteinte).</summary>
        public float LavaBoost { get; set; }

        void ApplyLavaGlow() => Shader.SetGlobalFloat(k_ArenaLavaGlowId, m_Light * m_LavaGlow * Mathf.Max(0f, 1f + LavaBoost));

        void Awake()
        {
            Instance = this;
            if (m_Arena == null)
                m_Arena = FindAnyObjectByType<ArenaShape>();
            if (m_Dragon == null)
                m_Dragon = FindAnyObjectByType<DragonBoss>();
            if (m_Crystals == null)
                m_Crystals = FindAnyObjectByType<CeilingCrystals>();
            if (m_Lava == null)
                m_Lava = FindAnyObjectByType<ArenaLava>();
            m_Turrets = GetComponent<ArenaTurrets>();

            m_SourceA = CreateMusicSource("Musique A");
            m_SourceB = CreateMusicSource("Musique B");
            m_Ambient = m_AmbientLit;
        }

        void Start()
        {
            CreateLavaLights();
            SetLight(0f);
            ArenaLights.Create(this, m_Arena, m_Dragon, m_Crystals, m_Turrets);

            if (m_Dragon != null)
            {
                m_Dragon.PhaseChanged += OnDragonPhaseChanged;
                var health = m_Dragon.GetComponent<Health>();
                if (health != null)
                    health.Died += OnDragonDied;
            }

            var player = PlayerHealth.Instance;
            if (player != null)
                player.Health.Died += OnPlayerDied;

            // Scène ouverte seule dans l'éditeur : on éclaire tout pour qu'on puisse la regarder.
            if (FinalBossDirector.Instance == null)
            {
                Debug.LogWarning("ArenaDirector : l'arène se lance depuis la partie (scène Prototype_Tir, vague du boss final). " +
                                 "Ouverte seule, elle reste éclairée et le dragon attend en haut de la coulée de lave.", this);
                m_Crystals?.SetAllLevels(1f);
                SetLight(1f);
                SetFallProgress(1f);
            }
            else if (m_Dragon != null)
            {
                // Dans le noir de l'intro, le dragon n'existe pas encore : il apparaît avec les premiers cristaux.
                m_Dragon.SetVisible(false);
            }
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            RestoreMusicVolume();
            if (m_Dragon != null)
            {
                m_Dragon.PhaseChanged -= OnDragonPhaseChanged;
                var health = m_Dragon.GetComponent<Health>();
                if (health != null)
                    health.Died -= OnDragonDied;
            }

            var player = PlayerHealth.Instance;
            if (player != null)
                player.Health.Died -= OnPlayerDied;
            if (m_Bar != null)
                Destroy(m_Bar.gameObject);
        }

        AudioSource CreateMusicSource(string sourceName)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.priority = 0;
            source.volume = m_MusicVolume;
            source.outputAudioMixerGroup = m_MusicGroup;
            return source;
        }

        void CreateLavaLights()
        {
            // Une arène qui éclaire déjà sa lave (la fourche) n'en a pas besoin.
            if (m_Arena == null || m_Arena.HasOwnLavaLights)
                return;

            const int count = 4;
            m_LavaLights = new Light[count];
            for (var i = 0; i < count; i++)
            {
                var angle = (i + 0.5f) * Mathf.PI * 2f / count;
                var radius = m_Arena.RoamRadius + 1.5f;
                var go = new GameObject($"Lueur de la lave {i + 1}");
                go.transform.SetParent(transform, false);
                go.transform.position = m_Arena.transform.TransformPoint(new Vector3(Mathf.Sin(angle) * radius, 1.2f, Mathf.Cos(angle) * radius));
                var light = go.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(1f, 0.38f, 0.1f);
                light.range = 16f;
                light.shadows = LightShadows.None;
                m_LavaLights[i] = light;
            }
        }

        // ---------------------------------------------------------------- intro

        /// <summary>Le joueur vient d'arriver, dans le noir : la musique commence, et l'intro avec elle.</summary>
        public void BeginIntro()
        {
            if (m_Stage != Stage.Waiting)
                return;

            m_Stage = Stage.Intro;
            m_StageTime = 0f;
            ApplyRenderSettings(0f);
            SetLight(0f);
            SetFallProgress(0f);
            if (m_Lava != null && m_Dragon != null && m_Dragon.TryGetComponent<Enemy>(out var enemy))
                m_Lava.DamageScale = enemy.DamageMultiplier;

            StartMusic();

            // La grotte est noire : on peut lever le voile tout de suite.
            var fader = ScreenFader.Instance;
            if (fader != null)
                fader.FadeTo(0f, 0.3f);

            var bossName = m_Dragon != null && m_Dragon.TryGetComponent<Boss>(out var boss) ? boss.DisplayName : "Le dragon";
            m_Bar = BossHealthBar.Create(bossName, new[] { 0.66f, 0.33f });

            // Les tourelles du joueur viendront, une (ou deux) par quart de cristaux.
            if (m_Turrets != null)
                m_Turrets.Prepare(m_Arena);
        }

        void Update()
        {
            m_StageTime += Time.deltaTime;
            UpdateMusic();
            UpdateDebugKeys();

            switch (m_Stage)
            {
                case Stage.Intro:
                    UpdateIntro();
                    break;
                case Stage.Fight:
                    UpdateFight();
                    break;
                case Stage.Victory:
                    UpdateVictory();
                    break;
            }

            UpdateCrystals();
            ApplyLavaGlow();
        }

        void UpdateIntro()
        {
            var beat = CurrentBeat / m_MusicBeatsPerIntroBeat;

            // Un quart des cristaux sur chaque temps.
            for (var i = 0; i < CeilingCrystals.GroupCount; i++)
            {
                if (m_LitTime[i] < 0f && beat >= m_CrystalsFromBeat + i * m_BeatsPerCrystalQuarter)
                {
                    m_LitTime[i] = Time.time;
                    ArenaPlayer.Rumble(0.15f, 0.05f);
                    if (m_Dragon != null)
                        m_Dragon.SetVisible(true);
                    if (m_Turrets != null)
                        m_Turrets.Arrive(i);
                }
            }

            var lit = 0;
            foreach (var time in m_LitTime)
            {
                if (time >= 0f)
                    lit++;
            }

            m_Light = Mathf.MoveTowards(m_Light, lit / (float)CeilingCrystals.GroupCount, Time.deltaTime * 3f);
            SetLight(m_Light);

            // Le dragon, debout en haut de la coulée pendant l'allumage, s'envole et pique sur le centre avant la barre ;
            // pendant qu'elle se remplit, il crache son feu vers la voûte.
            if (m_Dragon != null && !m_DragonDashed && beat >= m_DragonDashBeat)
            {
                m_DragonDashed = true;
                var seconds = (m_BarFromBeat - m_DragonDashBeat) * SecondsPerBeat * m_MusicBeatsPerIntroBeat;
                m_Dragon.IntroDash((float)seconds * 0.95f);
            }

            if (m_Dragon != null && !m_DragonBreathing && beat >= m_BarFromBeat)
            {
                m_DragonBreathing = true;
                m_Dragon.IntroBreath();
            }

            // La barre apparaît et se remplit ; en même temps, la lave commence à descendre le long des parois.
            if (beat >= m_BarFromBeat)
            {
                var since = (float)(beat - m_BarFromBeat);
                if (m_Bar != null)
                {
                    m_Bar.SetVisible(true);
                    m_Bar.SetValue(Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(since / Mathf.Max(1, m_BarFillBeats))), instant: true);
                }

                SetFallProgress(Mathf.Clamp01(since / Mathf.Max(1, m_LavaFallBeats)));
            }

            if (beat >= m_WakeBeat)
                StartFight();
        }

        void SkipIntro()
        {
            if (m_Dragon != null)
                m_Dragon.SetVisible(true);
            for (var i = 0; i < m_LitTime.Length; i++)
            {
                if (m_LitTime[i] < 0f)
                    m_LitTime[i] = Time.time;
            }

            m_Light = 1f;
            SetLight(1f);
            if (m_Bar != null)
            {
                m_Bar.SetVisible(true);
                m_Bar.SetValue(1f, instant: true);
            }

            if (m_Turrets != null)
                m_Turrets.ArriveAll();
            StartFight();
        }

        void StartFight()
        {
            if (m_Dragon != null)
                m_Dragon.SetVisible(true);
            m_Stage = Stage.Fight;
            m_StageTime = 0f;
            m_Light = 1f;
            SetLight(1f);
            SetFallProgress(1f);
            if (m_Dragon != null)
                m_Dragon.WakeUp();
            if (m_Turrets != null)
                m_Turrets.StartFighting();
        }

        // ---------------------------------------------------------------- combat

        void UpdateFight()
        {
            if (m_Bar != null && m_Dragon != null && m_Dragon.TryGetComponent<Health>(out var health))
                m_Bar.SetValue(health.Normalized);

            // L'ambiance rougit doucement pendant la rage.
            var target = m_Raging ? m_AmbientRage : m_AmbientLit;
            m_Ambient = Color.Lerp(m_Ambient, target, 1f - Mathf.Exp(-Time.deltaTime * 1.5f));
            ApplyRenderSettings(m_Light);
        }

        void OnDragonPhaseChanged(DragonPhase phase)
        {
            if (phase == DragonPhase.Enraged)
            {
                m_Raging = true;
                m_Crystals?.SetTint(m_RageTint);
                m_Lava?.BeginOverflow();
                m_Bar?.SetNameColor(new Color(1f, 0.45f, 0.35f));
                HeadMessages.Show("Le dragon est enragé !", new Color(1f, 0.35f, 0.25f), 3f, 1.2f);
            }
            else if (phase == DragonPhase.Air)
            {
                HeadMessages.Show("Il s'envole !\n<size=60%>bouge pour esquiver, sors des bandes rouges</size>", new Color(1f, 0.6f, 0.3f), 3.5f, 1.1f);
            }
        }

        void OnDragonDied(Health health, DamageInfo info)
        {
            if (m_Stage != Stage.Fight && m_Stage != Stage.Intro)
                return;

            m_Stage = Stage.Victory;
            m_StageTime = 0f;
            m_Raging = false;
            m_Crystals?.SetTint(m_VictoryTint);
            m_Lava?.Calm();
            if (m_Bar != null)
            {
                m_Bar.SetValue(0f);
                m_Bar.SetVisible(false);
            }

            HeadMessages.Show("Le dragon est vaincu !", new Color(1f, 0.85f, 0.3f), 4.5f, 1.4f);
        }

        void UpdateVictory()
        {
            m_Ambient = Color.Lerp(m_Ambient, m_AmbientLit * 1.3f, 1f - Mathf.Exp(-Time.deltaTime));
            ApplyRenderSettings(m_Light);
            if (m_StageTime < m_ChoiceDelay || m_EndlessTarget != null)
                return;

            ShowChoices();
        }

        // Deux cibles devant le joueur : à gauche le mode infini, à droite la fin de partie.
        void ShowChoices()
        {
            var rig = PlayerRig.Instance;
            var head = rig != null && rig.Head != null ? rig.Head : null;
            if (head == null)
                return;

            var forward = head.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward;
            var right = Vector3.Cross(Vector3.up, forward);
            var center = head.position + forward * 3.5f + Vector3.down * 0.15f;
            var rotation = Quaternion.LookRotation(forward, Vector3.up);

            m_EndlessTarget = ArenaChoiceTarget.Create(center - right * 1.4f, rotation, "Continuer\n<size=70%>mode infini</size>",
                                                       new Color(0.35f, 0.65f, 1f), m_ChoiceMaterial, m_ChoiceClip, ChooseEndless);
            m_FinishTarget = ArenaChoiceTarget.Create(center + right * 1.4f, rotation, "Terminer\n<size=70%>la partie</size>",
                                                      new Color(1f, 0.8f, 0.3f), m_ChoiceMaterial, m_ChoiceClip, ChooseFinish);
            HeadMessages.Show("Tire dans une cible :\ncontinuer en mode infini, ou terminer la partie", new Color(1f, 0.9f, 0.6f), 6f, 1f);
        }

        void ChooseEndless()
        {
            if (m_Stage != Stage.Victory)
                return;

            m_Stage = Stage.Chosen;
            ClearChoices();
            var director = FinalBossDirector.Instance;
            if (director != null)
                director.ContinueEndless();
        }

        void ChooseFinish()
        {
            if (m_Stage != Stage.Victory)
                return;

            m_Stage = Stage.Chosen;
            ClearChoices();
            var director = FinalBossDirector.Instance;
            if (director != null)
                director.FinishGame();
        }

        void ClearChoices()
        {
            foreach (var target in new[] { m_EndlessTarget, m_FinishTarget })
            {
                if (target == null)
                    continue;
                EditorSelection.ReleaseInside(target.transform, null);
                Destroy(target.gameObject);
            }
        }

        void OnPlayerDied(Health health, DamageInfo info)
        {
            if (m_Stage == Stage.Waiting || m_Stage == Stage.Chosen)
                return;

            m_Stage = Stage.Defeat;
            m_FadingOut = true;
            if (m_Bar != null)
                m_Bar.SetVisible(false);
        }

        // ---------------------------------------------------------------- lumière

        void UpdateCrystals()
        {
            if (m_Crystals == null || m_Stage == Stage.Waiting)
                return;

            // Chaque quart s'allume d'un éclat, puis se stabilise.
            for (var i = 0; i < CeilingCrystals.GroupCount; i++)
            {
                var since = m_LitTime[i] < 0f ? -1f : Time.time - m_LitTime[i];
                var level = since < 0f ? 0f : 1f + 1.5f * Mathf.Exp(-since * 5f);
                m_Crystals.SetLevel(i, level);
            }

            // Pendant le combat, un battement sur chaque temps.
            if (m_Stage == Stage.Fight || m_Stage == Stage.Victory)
            {
                var phase = (float)(CurrentBeat - System.Math.Floor(CurrentBeat));
                m_Crystals.Pulse(m_BeatPulse * Mathf.Exp(-phase * 5f));
            }
            else
            {
                m_Crystals.Pulse(0f);
            }
        }

        void SetLight(float level)
        {
            m_Light = level;
            ApplyLavaGlow();
            if (m_Arena != null)
                m_Arena.SetLightLevel(level);
            foreach (var light in m_LavaLights)
            {
                if (light == null)
                    continue;
                light.intensity = level * 2.2f;
                light.enabled = level > 0.001f;
            }

            if (m_Stage != Stage.Waiting)
                ApplyRenderSettings(level);
        }

        // Coulées de lave le long des parois : 0 = rien, 1 = jusqu'en bas. Elles se dévoilent du haut vers le bas.
        void SetFallProgress(float progress)
        {
            var cutoff = progress >= 1f || m_Arena == null
                ? -100000f
                : Mathf.Lerp(m_Arena.LavaFallTopY + 0.2f, m_Arena.FloorY - 0.5f, Mathf.Clamp01(progress));
            Shader.SetGlobalFloat(k_LavaFallCutoffId, cutoff);
        }

        // Lumière de la scène active (l'arène) : ambiance et brouillard suivent l'allumage des cristaux.
        void ApplyRenderSettings(float level)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = m_Ambient * level;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = m_FogColor * level;
            RenderSettings.fogDensity = m_FogDensity;
            RenderSettings.skybox = null;
        }

        // ---------------------------------------------------------------- musique

        void StartMusic()
        {
            if (m_Music == null)
            {
                Debug.LogWarning("ArenaDirector : aucune musique. L'intro suit quand même le tempo réglé.", this);
            }

            m_MusicStarted = true;
            BoostMusicVolume();
            m_MusicLevel = m_DarkMusicVolume;
            ApplyMusicVolume();
            var start = AudioSettings.dspTime + 0.2;
            m_SegmentDspStart = start;
            m_SegmentSongStart = 0.0;
            m_SegmentDspEnd = m_Music != null ? start + m_Music.samples / (double)m_Music.frequency : double.MaxValue;
            m_Current = m_SourceA;
            m_Pending = null;
            if (m_Music != null)
            {
                m_SourceA.clip = m_Music;
                m_SourceA.timeSamples = 0;
                m_SourceA.PlayScheduled(start);
            }
        }

        // La musique du boss plus forte que celle de la carte : le groupe Music du mixer monte pendant le combat.
        void BoostMusicVolume()
        {
            var mixer = m_MusicGroup != null ? m_MusicGroup.audioMixer : null;
            if (m_MusicBoosted || m_MusicBoostDb <= 0f || mixer == null || !mixer.GetFloat(m_MusicVolumeParameter, out var decibels))
                return;

            m_MusicBaseDb = decibels;
            m_MusicBoosted = true;
            mixer.SetFloat(m_MusicVolumeParameter, Mathf.Min(20f, decibels + m_MusicBoostDb));
        }

        void RestoreMusicVolume()
        {
            if (!m_MusicBoosted)
                return;

            m_MusicBoosted = false;
            var mixer = m_MusicGroup != null ? m_MusicGroup.audioMixer : null;
            if (mixer != null)
                mixer.SetFloat(m_MusicVolumeParameter, m_MusicBaseDb);
        }

        void ApplyMusicVolume() => m_SourceA.volume = m_SourceB.volume = m_MusicVolume * m_MusicLevel * m_MusicFade;

        // Fin du morceau : il reprend à partir du temps choisi, sans coupure (deux sources qui se relaient).
        void UpdateMusic()
        {
            if (!m_MusicStarted || m_Music == null)
                return;

            // Plus douce dans le noir ; elle remonte à mesure que les quarts de cristaux s'allument.
            var lit = 0;
            foreach (var time in m_LitTime)
            {
                if (time >= 0f)
                    lit++;
            }

            var wanted = m_Stage == Stage.Intro ? Mathf.Lerp(m_DarkMusicVolume, 1f, lit / (float)CeilingCrystals.GroupCount) : 1f;
            m_MusicLevel = Mathf.MoveTowards(m_MusicLevel, wanted, Time.unscaledDeltaTime * 0.6f);
            ApplyMusicVolume();

            if (m_FadingOut)
            {
                m_MusicFade = Mathf.MoveTowards(m_MusicFade, 0f, Time.unscaledDeltaTime / 3f);
                ApplyMusicVolume();
                if (m_MusicFade <= 0f)
                {
                    m_SourceA.Stop();
                    m_SourceB.Stop();
                    m_MusicStarted = false;
                }

                return;
            }

            if (m_LoopFromBeat < 0)
                return;

            var now = AudioSettings.dspTime;
            var loopStart = m_FirstBeat + m_LoopFromBeat * SecondsPerBeat;
            if (m_Pending == null && now > m_SegmentDspEnd - 2.0)
            {
                m_Pending = m_Current == m_SourceA ? m_SourceB : m_SourceA;
                m_Pending.clip = m_Music;
                m_Pending.timeSamples = Mathf.Clamp((int)(loopStart * m_Music.frequency), 0, m_Music.samples - 1);
                m_Pending.PlayScheduled(m_SegmentDspEnd);
                m_PendingDspStart = m_SegmentDspEnd;
            }

            if (m_Pending != null && now >= m_PendingDspStart)
            {
                m_Current = m_Pending;
                m_Pending = null;
                m_SegmentDspStart = m_PendingDspStart;
                m_SegmentSongStart = loopStart;
                m_SegmentDspEnd = m_SegmentDspStart + (m_Music.length - loopStart);
            }
        }

        /// <summary>Temps de la musique en cours (0 au premier temps), avec sa fraction.</summary>
        double CurrentBeat
        {
            get
            {
                if (!m_MusicStarted)
                    return -1.0;
                var songTime = AudioSettings.dspTime - m_SegmentDspStart + m_SegmentSongStart;
                return (songTime - m_FirstBeat) / SecondsPerBeat;
            }
        }

        // ---------------------------------------------------------------- tests

        void UpdateDebugKeys()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.nKey.wasPressedThisFrame)
            {
                if (m_Stage == Stage.Intro)
                    SkipIntro();
                else if (m_Stage == Stage.Fight && m_Dragon != null && m_Dragon.TryGetComponent<Health>(out var health) && health.IsAlive)
                    health.TakeDamage(new DamageInfo { Amount = health.Max * 10f, Point = m_Dragon.transform.position, Source = this });
            }

            if (m_Stage == Stage.Victory && m_EndlessTarget != null)
            {
                if (keyboard.iKey.wasPressedThisFrame)
                    ChooseEndless();
                else if (keyboard.tKey.wasPressedThisFrame)
                    ChooseFinish();
            }
#endif
        }
    }
}
