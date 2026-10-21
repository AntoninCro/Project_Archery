using System.Collections;
using System.Collections.Generic;
using Archery.BowClasses;
using Archery.Bows;
using Archery.Core;
using Archery.Economy;
using Archery.FinalBoss;
using Archery.Player;
using Archery.UI;
using Archery.Upgrades;
using Archery.Waves;
using Archery.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Archery.Portal
{
    /// <summary>
    /// Easter egg du portail des étoiles (GDD, section 23), côté carte. Sur la terrasse de droite, un anneau de pierre
    /// entouré de 5 obélisques ; chaque obélisque porte une rune un peu cachée (sur sa face extérieure ou à son pied).
    /// Les toucher toutes les 5 de la main pendant une même vague ouvre le portail ; elles s'éteignent en fin de vague.
    /// En traversant le portail, la carte se fige et le joueur passe dans la dimension (<see cref="StarDimension"/>), avec
    /// son seul arc de départ. Il revient devant le portail, réussite ou échec ; une seule tentative par partie.
    /// </summary>
    /// <remarks>
    /// Les runes et l'ouverture sont des objets de la scène, placés dans l'éditeur sur les modèles (menu
    /// <i>Replacer l'ouverture et les runes</i>, Read/Write activé sur <c>statue_obelisk</c> et <c>statue_ring</c>) et
    /// ajustables à la main : en jeu, les statues statiques sont fusionnées (static batching) et leurs maillages ne se
    /// lisent plus. Le voile du portail est fabriqué à l'ouverture, sur l'objet « Ouverture ».
    /// Raccourci de test (éditeur) : <b>P</b> allume les 5 runes, puis fait entrer dans la dimension.
    /// </remarks>
    [DisallowMultipleComponent]
    public class StarPortal : MonoBehaviour
    {
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");

        [Header("Pierres")]
        [Tooltip("L'anneau de pierre (statue_ring) : le portail s'ouvre dedans.")]
        [SerializeField]
        Transform m_Ring;

        [Tooltip("Les 5 obélisques (statue_obelisk) qui portent les runes.")]
        [SerializeField]
        Transform[] m_Obelisks = new Transform[0];

        [Tooltip("Matériau des runes et du voile du portail (couleurs des sommets, transparent : Effects).")]
        [SerializeField]
        Material m_GlowMaterial;

        [SerializeField]
        Color m_RuneColor = new Color(0.62f, 0.45f, 1f);

        [Tooltip("Distance (m) entre la main et une rune pour l'allumer.")]
        [SerializeField]
        float m_TouchDistance = 0.25f;

        [Tooltip("Taille (m) d'une rune.")]
        [SerializeField]
        float m_RuneSize = 0.24f;

        [Header("Placement (ajustable à la main)")]
        [Tooltip("L'ouverture de l'anneau : sa position est le centre du voile, son axe bleu (Z) le sens du passage, son " +
                 "échelle X le rayon. Clic droit sur ce composant > Replacer l'ouverture et les runes pour la remesurer.")]
        [SerializeField]
        Transform m_Opening;

        [Tooltip("Les runes posées sur les obélisques (objets « Rune 1 » à « Rune 5 »), qu'on peut déplacer à la main.")]
        [SerializeField]
        Transform[] m_RuneObjects = new Transform[0];

        [SerializeField]
        [HideInInspector]
        int m_RuneLayout;

        [Header("Dimension")]
        [Tooltip("Nom de la scène de la dimension (elle doit être dans File > Build Profiles > Scene List).")]
        [SerializeField]
        string m_DimensionScene = "Dimension_Etoiles";

        [Tooltip("Durée (s) des fondus au noir à l'entrée et au retour.")]
        [SerializeField]
        float m_FadeTime = 1f;

        [Tooltip("Points gagnés en réussissant l'épreuve (multipliés par la difficulté).")]
        [SerializeField]
        int m_ScoreBonus = 2500;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_RuneClip;

        [Tooltip("Une rune touchée hors d'une vague : elle reste froide.")]
        [SerializeField]
        AudioClip m_ColdClip;

        [SerializeField]
        AudioClip m_OpenClip;

        [SerializeField]
        AudioClip m_EnterClip;

        class Rune
        {
            public Transform Transform;
            public MeshRenderer Renderer;
            public Light Light;
            public bool Lit;
            public float Flash;
        }

        readonly List<Rune> m_Runes = new List<Rune>();
        readonly List<GameObject> m_HiddenClouds = new List<GameObject>();
        MaterialPropertyBlock m_Block;
        WaveManager m_Waves;
        Scene m_MainScene;
        Scene m_Dimension;
        SkyController m_Sky;
        Light m_Sun;
        BowDefinition m_SavedBow;
        Bow m_Bow;

        // L'ouverture de l'anneau (monde) : centre, normale, rayon intérieur, et bas et haut de la pierre.
        Vector3 m_Center;
        Vector3 m_Normal = Vector3.forward;
        float m_Radius = 1.5f;
        float m_Bottom;
        float m_Top;

        Transform m_Veil;
        Transform m_VeilStars;
        Light m_VeilLight;
        MeshRenderer m_GlitchBlock;
        float m_GlitchTimer;
        float m_ColdMessageTime = -10f;
        float m_LastSide;
        int m_EntrySide = 1;
        bool m_Open;
        bool m_Used;
        bool m_Busy;
        float m_Age;

        public static StarPortal Instance { get; private set; }

        /// <summary>Le joueur est dans la dimension (le bouton « Tour » de la montre est alors caché).</summary>
        public static bool PlayerInDimension { get; private set; }

        /// <summary>L'épreuve a été réussie pendant cette partie (une étoile à côté du nom dans le classement).</summary>
        public static bool TrialCompleted { get; private set; }

        public int LitCount
        {
            get
            {
                var count = 0;
                foreach (var rune in m_Runes)
                {
                    if (rune.Lit)
                        count++;
                }

                return count;
            }
        }

        void Awake()
        {
            Instance = this;
            m_MainScene = gameObject.scene;

            // « Rejouer » recharge la scène : nouvelle partie, nouvelle tentative.
            PlayerInDimension = false;
            TrialCompleted = false;
        }

        void Start()
        {
            if (m_Opening != null)
                UseOpening();
            else
                MeasureRing();
            if (!BindRunes())
                BuildRunes();
            m_Waves = WaveManager.Instance;
            if (m_Waves != null)
                m_Waves.WaveEnded += OnWaveEnded;
        }

        void OnDestroy()
        {
            if (m_Waves != null)
                m_Waves.WaveEnded -= OnWaveEnded;
            if (Instance == this)
            {
                Instance = null;
                PlayerInDimension = false;
            }
        }

        bool WaveRunning => m_Waves != null && m_Waves.Phase == WavePhase.Wave && !m_Waves.IsFinalBossWave;

        void Update()
        {
            var deltaTime = Time.deltaTime;
            m_Age += deltaTime;
            UpdateDebugKeys();
            if (m_Busy || PlayerInDimension)
                return;

            UpdateRunes(deltaTime);
            if (m_Open)
            {
                AnimateVeil(deltaTime);
                CheckCrossing();
            }
        }

        // ---------------------------------------------------------------- les runes

        // Une main (ou, sans casque, le regard et la touche E) qui touche une rune l'allume, pendant une vague.
        void UpdateRunes(float deltaTime)
        {
            var hands = HandPositions();
            for (var i = 0; i < m_Runes.Count; i++)
            {
                var rune = m_Runes[i];
                rune.Flash = Mathf.Max(0f, rune.Flash - deltaTime * 2f);
                var nearest = float.MaxValue;
                foreach (var hand in hands)
                    nearest = Mathf.Min(nearest, Vector3.Distance(hand, rune.Transform.position));

                var touched = nearest <= m_TouchDistance || DesktopTouched(rune.Transform.position);
                if (touched && !rune.Lit && !m_Used)
                    Touch(rune, i);

                ShowRune(rune, m_Used ? 0f : Mathf.Clamp01(1f - (nearest - m_TouchDistance) / 1.2f));
            }
        }

        void Touch(Rune rune, int index)
        {
            if (!WaveRunning)
            {
                // Hors d'une vague, la rune frémit et reste froide.
                if (rune.Flash > 0.2f)
                    return;
                rune.Flash = 1f;
                Sfx.Play(m_ColdClip, rune.Transform.position, 0.6f, 0.8f);
                if (Time.time - m_ColdMessageTime > 8f)
                {
                    m_ColdMessageTime = Time.time;
                    HeadMessages.Show("La rune reste froide…\n<size=60%>elle ne s'éveille qu'au cœur d'une vague</size>",
                                      m_RuneColor, 3f, 0.9f);
                }

                return;
            }

            rune.Lit = true;
            rune.Flash = 1f;
            Sfx.Play(m_RuneClip, rune.Transform.position, 0.9f, 0.9f + 0.05f * LitCount);
            var rig = PlayerRig.Instance;
            if (rig != null)
            {
                foreach (var hand in rig.Hands)
                    Haptics.Pulse(hand, 0.5f, 0.12f);
            }

            var lit = LitCount;
            if (lit < m_Runes.Count)
                HeadMessages.Show($"Rune {lit} / {m_Runes.Count}", m_RuneColor, 2f, 0.8f);
            else
                Open();
        }

        void ShowRune(Rune rune, float proximity)
        {
            m_Block ??= new MaterialPropertyBlock();
            var pulse = 0.5f + 0.5f * Mathf.Sin(m_Age * (rune.Lit ? 4f : 1.3f) + rune.Transform.position.x);
            float intensity;
            float alpha;
            if (m_Used)
            {
                intensity = 0.15f;
                alpha = 0.25f;
            }
            else if (rune.Lit)
            {
                intensity = 2.2f + 0.8f * pulse + 2f * rune.Flash;
                alpha = 1f;
            }
            else
            {
                // Éteinte, elle se devine à peine ; elle s'éveille quand la main approche.
                intensity = 0.3f + 0.15f * pulse + 1.1f * proximity + 1.5f * rune.Flash;
                alpha = 0.35f + 0.5f * proximity;
            }

            var color = m_RuneColor * intensity;
            color.a = alpha;
            rune.Renderer.GetPropertyBlock(m_Block);
            m_Block.SetColor(k_BaseColorId, color);
            rune.Renderer.SetPropertyBlock(m_Block);
            if (rune.Light != null)
            {
                rune.Light.enabled = rune.Lit && !m_Used;
                rune.Light.intensity = 1.2f + 0.6f * pulse;
            }
        }

        List<Vector3> HandPositions()
        {
            var positions = new List<Vector3>(2);
            var rig = PlayerRig.Instance;
            if (rig == null)
                return positions;
            foreach (var hand in rig.Hands)
            {
                if (hand != null)
                    positions.Add(hand.transform.position);
            }

            return positions;
        }

        // Sans casque : la touche E, en regardant la rune de près.
        static bool DesktopTouched(Vector3 rune)
        {
            var desktop = DesktopControls.Instance;
            var keyboard = Keyboard.current;
            if (desktop == null || !desktop.HasFocus || keyboard == null || !keyboard.eKey.wasPressedThisFrame || desktop.CameraTransform == null)
                return false;

            var eye = desktop.CameraTransform;
            var offset = rune - eye.position;
            return offset.magnitude < 3f && Vector3.Angle(eye.forward, offset) < 12f;
        }

        void OnWaveEnded(int wave)
        {
            if (m_Used || PlayerInDimension)
                return;

            var any = false;
            foreach (var rune in m_Runes)
            {
                any |= rune.Lit;
                rune.Lit = false;
            }

            if (m_Open)
                Close();
            if (any)
                HeadMessages.Show("Les runes s'éteignent…", m_RuneColor, 2.5f, 0.8f);
        }

        // ---------------------------------------------------------------- le portail

        void Open()
        {
            if (m_Open)
                return;

            m_Open = true;
            BuildVeil();
            Sfx.Play(m_OpenClip, m_Center, 1f, 0.8f, 0f);
            HeadMessages.Show("Le portail des étoiles s'ouvre…\n<size=60%>traverse l'anneau de pierre</size>", m_RuneColor, 5f, 1.2f);
            m_LastSide = Side(HeadPosition());
        }

        void Close()
        {
            m_Open = false;
            if (m_Veil != null)
                DestroyVeil();
        }

        // Le joueur traverse l'anneau : sa tête passe d'un côté à l'autre, dans l'ouverture (ou dans la pierre, sans
        // collision : on ne le refuse pas).
        void CheckCrossing()
        {
            var head = HeadPosition();
            var side = Side(head);
            var crossed = Mathf.Sign(side) != Mathf.Sign(m_LastSide) && Mathf.Abs(side) < 1.5f;
            m_LastSide = side;
            if (!crossed)
                return;

            var offset = head - m_Center;
            var across = offset - m_Normal * Vector3.Dot(offset, m_Normal);
            across.y = 0f;
            if (across.magnitude > m_Radius * 1.1f || head.y < m_Bottom - 0.5f || head.y > m_Top + 0.5f)
                return;

            m_EntrySide = side > 0f ? -1 : 1;
            StartCoroutine(Enter());
        }

        float Side(Vector3 point) => Vector3.Dot(point - m_Center, m_Normal);

        static Vector3 HeadPosition()
        {
            var rig = PlayerRig.Instance;
            return rig != null && rig.Head != null ? rig.Head.position : Vector3.zero;
        }

        // ---------------------------------------------------------------- l'épreuve

        IEnumerator Enter()
        {
            m_Busy = true;
            m_Used = true;
            Close();
            Sfx.Play(m_EnterClip, HeadPosition(), 1f, 1f, 0f);

            var fader = ScreenFader.Instance;
            if (fader != null)
                yield return fader.Fade(1f, m_FadeTime);

            var loading = LoadDimension();
            if (loading == null)
            {
                Debug.LogError($"StarPortal : impossible de charger la scène « {m_DimensionScene} ». " +
                               "Ajoute-la dans File > Build Profiles > Scene List.", this);
                if (fader != null)
                    fader.FadeTo(0f, 0.5f);
                m_Busy = false;
                yield break;
            }

            // La carte se fige, le joueur n'a plus que son arc de départ, sans bonus.
            var rig = PlayerRig.Instance;
            MapFreeze.Freeze(m_MainScene, rig != null && rig.Origin != null ? rig.Origin.transform : null);
            AmbienceManager.Silenced = true;
            SuspendMapLighting();
            SuspendUpgrades(true);
            PlayerInDimension = true;

            while (!loading.isDone)
                yield return null;

            m_Dimension = SceneManager.GetSceneByName(m_DimensionScene);
            if (m_Dimension.IsValid())
                SceneManager.SetActiveScene(m_Dimension);

            var dimension = StarDimension.Instance;
            if (dimension == null)
            {
                Debug.LogError("StarPortal : la scène de la dimension n'a pas de Star Dimension.", this);
                yield return Return(false, false);
                yield break;
            }

            var spawn = dimension.PlayerSpawn;
            var teleport = PlayerTeleport.Instance;
            if (teleport != null && spawn != null)
                yield return teleport.TeleportWhileHidden(spawn.position, spawn.eulerAngles.y);

            dimension.Begin(this);
            if (fader != null)
                fader.FadeTo(0f, m_FadeTime);
            m_Busy = false;
        }

        static readonly Collider[] s_Overlaps = new Collider[16];

        // Le point d'arrivée au retour : 3 m devant l'anneau, ou, si un ennemi (figé) ou un obstacle y est, la première
        // place libre un peu plus loin ou sur le côté (jusqu'à 6 m et 70°). Sans place libre, le point de départ.
        Vector3 FreeArrival(Vector3 outward)
        {
            var flat = new Vector3(outward.x, 0f, outward.z);
            flat = flat.sqrMagnitude > 1e-4f ? flat.normalized : Vector3.forward;
            Vector3? first = null;
            foreach (var distance in new[] { 3f, 4.5f, 6f })
            {
                foreach (var angle in new[] { 0f, 35f, -35f, 70f, -70f })
                {
                    var point = m_Center + Quaternion.Euler(0f, angle, 0f) * flat * distance;
                    point.y = m_Bottom;
                    if (Physics.Raycast(point + Vector3.up * (m_Top - m_Bottom + 2f), Vector3.down, out var ground, m_Top - m_Bottom + 6f,
                                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                        point.y = ground.point.y;
                    if (first == null)
                        first = point;

                    if (IsFree(point))
                        return point;
                }
            }

            return first ?? m_Center + flat * 3f;
        }

        // Rien à la place d'un joueur debout (ennemi, rocher, mur), au-dessus du sol : le terrain et ce que les flèches
        // traversent (barricades…) ne comptent pas.
        static bool IsFree(Vector3 feet)
        {
            var count = Physics.OverlapCapsuleNonAlloc(feet + Vector3.up * 0.7f, feet + Vector3.up * 1.7f, 0.45f, s_Overlaps,
                                                       Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var collider = s_Overlaps[i];
                if (collider != null && !(collider is TerrainCollider) && !ArrowIgnore.IsIgnored(collider))
                    return false;
            }

            return true;
        }

        AsyncOperation LoadDimension()
        {
#if UNITY_EDITOR
            // Dans l'éditeur, la dimension se charge même si elle manque dans la liste des scènes du build (par son chemin).
            if (!Application.CanStreamedLevelBeLoaded(m_DimensionScene))
            {
                foreach (var guid in UnityEditor.AssetDatabase.FindAssets($"{m_DimensionScene} t:Scene"))
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    if (System.IO.Path.GetFileNameWithoutExtension(path) == m_DimensionScene)
                        return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                            path, new LoadSceneParameters(LoadSceneMode.Additive));
                }
            }
#endif
            return SceneManager.LoadSceneAsync(m_DimensionScene, LoadSceneMode.Additive);
        }

        /// <summary>L'épreuve est finie (appelé par la dimension) : retour devant le portail.</summary>
        public void EndTrial(bool success)
        {
            if (!m_Busy && PlayerInDimension)
                StartCoroutine(Return(success, true));
        }

        IEnumerator Return(bool success, bool fade)
        {
            m_Busy = true;
            var fader = ScreenFader.Instance;
            if (fade && fader != null)
                yield return fader.Fade(1f, m_FadeTime);

            if (m_MainScene.IsValid())
                SceneManager.SetActiveScene(m_MainScene);
            RestoreMapLighting();

            // Devant le portail, du côté d'où il était venu, tourné vers l'anneau, sur le sol, à une place libre (pas sur
            // un ennemi figé devant l'anneau, ni dans un rocher).
            var outward = m_Normal * m_EntrySide;
            var arrival = FreeArrival(outward);
            var toRing = m_Center - arrival;
            toRing.y = 0f;
            var yaw = Quaternion.LookRotation(toRing.sqrMagnitude > 0.01f ? toRing : -new Vector3(outward.x, 0f, outward.z)).eulerAngles.y;
            var teleport = PlayerTeleport.Instance;
            if (teleport != null)
                yield return teleport.TeleportWhileHidden(arrival, yaw);

            if (m_Dimension.IsValid() && m_Dimension.isLoaded)
            {
                EditorSelection.ReleaseScene(m_Dimension);
                var unloading = SceneManager.UnloadSceneAsync(m_Dimension);
                while (unloading != null && !unloading.isDone)
                    yield return null;
            }

            SuspendUpgrades(false);
            MapFreeze.Unfreeze();
            AmbienceManager.Silenced = false;
            PlayerInDimension = false;

            var player = PlayerHealth.Instance;
            if (player != null && player.IsAlive)
                player.Health.Heal(player.Health.Max);

            if (success)
                Reward();
            else
                HeadMessages.Show("Expulsé de la dimension…\n<size=60%>le portail se referme jusqu'à la fin de la partie</size>",
                                  new Color(1f, 0.3f, 0.3f), 4f, 1.1f);

            if (fader != null)
                fader.FadeTo(0f, m_FadeTime);
            m_Busy = false;
        }

        void Reward()
        {
            TrialCompleted = true;
            var score = ScoreManager.Instance;
            var points = 0;
            if (score != null)
                points = score.AddPoints(m_ScoreBonus * score.DifficultyMultiplier);

            // L'arc cosmique : débloqué pour toujours (succès « Voyageur des étoiles », dans le menu des arcs), et en main
            // tout de suite, pour le reste de la partie.
            var achievements = AchievementManager.Instance;
            if (achievements != null)
                achievements.Report(AchievementType.StarTrial);
            var cosmic = CosmicBow();
            if (cosmic != null && m_Bow != null)
                m_Bow.SetDefinition(cosmic);

            var bowText = cosmic != null ? " · l'arc cosmique est à toi" : "";
            HeadMessages.Show($"Épreuve des étoiles réussie !\n<size=60%>+{points} points · une étoile au classement{bowText}</size>",
                              new Color(1f, 0.85f, 0.35f), 6f, 1.3f);
        }

        // L'arc à surcharge de la liste des arcs (l'arc cosmique), ou null.
        static BowDefinition CosmicBow()
        {
            var loadout = BowLoadout.Instance;
            if (loadout == null)
                return null;
            foreach (var entry in loadout.Entries)
            {
                if (entry != null && entry.bow != null && entry.bow.overcharge)
                    return entry.bow;
            }

            return null;
        }

        void SuspendUpgrades(bool suspend)
        {
            var upgrades = PlayerUpgrades.Instance;
            if (upgrades != null)
                upgrades.Suspended = suspend;

            if (m_Bow == null)
                m_Bow = FindAnyObjectByType<Bow>();
            if (m_Bow == null)
                return;

            if (suspend)
            {
                // L'arc de sa classe, choisi au début de la partie (sans bonus) ; sans classes d'arc, celui du début.
                m_SavedBow = m_Bow.Definition;
                var loadout = BowLoadout.Instance;
                var start = loadout != null && loadout.Selected != null ? loadout.Selected : m_Bow.StartingDefinition;
                if (start != null)
                    m_Bow.SetDefinition(start);
            }
            else if (m_SavedBow != null)
            {
                m_Bow.SetDefinition(m_SavedBow);
            }
        }

        void SuspendMapLighting()
        {
            m_Sky = FindAnyObjectByType<SkyController>();
            if (m_Sky != null)
                m_Sky.enabled = false;
            m_Sun = RenderSettings.sun;
            if (m_Sun != null)
                m_Sun.enabled = false;

            // Les nuages suivent le joueur au-dessus de lui : pas dans la dimension.
            m_HiddenClouds.Clear();
            foreach (var clouds in FindObjectsByType<CloudLayer>())
            {
                if (!clouds.gameObject.activeSelf)
                    continue;
                clouds.gameObject.SetActive(false);
                m_HiddenClouds.Add(clouds.gameObject);
            }
        }

        void RestoreMapLighting()
        {
            if (m_Sun != null)
                m_Sun.enabled = true;
            if (m_Sky != null)
                m_Sky.enabled = true;
            foreach (var clouds in m_HiddenClouds)
            {
                if (clouds != null)
                    clouds.SetActive(true);
            }

            m_HiddenClouds.Clear();
        }

        // ---------------------------------------------------------------- mesures et fabrication

        // L'ouverture placée dans l'éditeur (objet « Ouverture ») ; le bas et le haut de la pierre pour le passage.
        void UseOpening()
        {
            m_Center = m_Opening.position;
            m_Normal = m_Opening.forward;
            m_Radius = Mathf.Max(0.3f, Mathf.Abs(m_Opening.lossyScale.x));
            var renderer = m_Ring != null ? m_Ring.GetComponentInChildren<Renderer>() : null;
            m_Bottom = renderer != null ? renderer.bounds.min.y : m_Center.y - m_Radius - 1.5f;
            m_Top = renderer != null ? renderer.bounds.max.y : m_Center.y + m_Radius + 0.5f;
        }

        // Les runes posées dans l'éditeur.
        bool BindRunes()
        {
            foreach (var runeObject in m_RuneObjects)
            {
                var renderer = runeObject != null ? runeObject.GetComponent<MeshRenderer>() : null;
                if (renderer == null)
                    continue;
                var light = runeObject.GetComponentInChildren<Light>(true);
                if (light != null)
                    light.enabled = false;
                m_Runes.Add(new Rune { Transform = runeObject, Renderer = renderer, Light = light });
            }

            return m_Runes.Count > 0;
        }

        /// <summary>
        /// Mesure l'ouverture de l'anneau et pose une rune sur chaque obélisque, en objets de la scène (à faire dans
        /// l'éditeur, puis enregistrer la scène). Remplace l'ouverture et les runes déjà posées.
        /// </summary>
        [ContextMenu("Replacer l'ouverture et les runes")]
        public void PlaceOnStones()
        {
            if (m_Opening != null)
                DestroyImmediate(m_Opening.gameObject);

            MeasureRing();
            var opening = new GameObject("Ouverture du portail").transform;
            opening.SetParent(transform, true);
            opening.SetPositionAndRotation(m_Center, Quaternion.LookRotation(m_Normal));
            var parentScale = transform.lossyScale.x;
            opening.localScale = Vector3.one * (m_Radius / Mathf.Max(0.01f, parentScale));
            m_Opening = opening;
            PlaceRunes();
        }

        /// <summary>Version du placement des runes (2 : au milieu des faces des obélisques, plus près des arêtes).</summary>
        public const int RuneLayoutVersion = 2;

        /// <summary>La version du placement des runes posées dans la scène.</summary>
        public int RuneLayout => m_RuneLayout;

        /// <summary>Pose de nouveau les 5 runes sur les obélisques (dans l'éditeur), sans toucher à l'ouverture.</summary>
        [ContextMenu("Replacer les runes")]
        public void PlaceRunes()
        {
            foreach (var runeObject in m_RuneObjects)
            {
                if (runeObject != null)
                    DestroyImmediate(runeObject.gameObject);
            }

            m_Runes.Clear();
            BuildRunes();
            m_RuneObjects = new Transform[m_Runes.Count];
            for (var i = 0; i < m_Runes.Count; i++)
                m_RuneObjects[i] = m_Runes[i].Transform;
            m_Runes.Clear();
            m_RuneLayout = RuneLayoutVersion;
        }

        void OnDrawGizmosSelected()
        {
            if (m_Opening == null)
                return;

            // Le cercle du voile, et le sens du passage.
            Gizmos.color = new Color(0.7f, 0.45f, 1f);
            var radius = Mathf.Abs(m_Opening.lossyScale.x);
            var previous = m_Opening.position + m_Opening.right * radius;
            for (var i = 1; i <= 48; i++)
            {
                var angle = i * Mathf.PI * 2f / 48f;
                var point = m_Opening.position + (m_Opening.right * Mathf.Cos(angle) + m_Opening.up * Mathf.Sin(angle)) * radius;
                Gizmos.DrawLine(previous, point);
                previous = point;
            }

            Gizmos.DrawLine(m_Opening.position - m_Opening.forward, m_Opening.position + m_Opening.forward);
        }

        // L'ouverture de l'anneau, mesurée sur ses sommets dans le monde : sa normale est l'horizontale la plus mince ;
        // son centre et son rayon viennent de rayons lancés dans le plan de l'anneau, depuis une première estimation,
        // jusqu'à la pierre (le centre est la moyenne des points touchés, le rayon leur distance moyenne).
        void MeasureRing()
        {
            if (m_Ring == null)
                return;

            var filter = m_Ring.GetComponentInChildren<MeshFilter>();
            var renderer = m_Ring.GetComponentInChildren<Renderer>();
            if (filter == null || renderer == null)
                return;

            var bounds = renderer.bounds;
            m_Bottom = bounds.min.y;
            m_Top = bounds.max.y;
            m_Center = bounds.center;
            m_Radius = Mathf.Max(0.5f, bounds.extents.y * 0.6f);
            if (!TryGetVertices(filter, out var vertices))
            {
                Debug.LogWarning("StarPortal : le maillage de l'anneau ne se lit pas (Read/Write désactivé, ou statues " +
                                 "fusionnées en jeu) : l'ouverture est estimée grossièrement.", this);
                return;
            }

            var axisA = Flat(m_Ring.right);
            var axisB = Flat(m_Ring.forward);
            Extent(vertices, axisA, out var minA, out var maxA);
            Extent(vertices, axisB, out var minB, out var maxB);
            var aThinner = maxA - minA < maxB - minB;
            m_Normal = aThinner ? axisA : axisB;
            var tangent = aThinner ? axisB : axisA;
            var normalMiddle = aThinner ? (minA + maxA) * 0.5f : (minB + maxB) * 0.5f;
            var tangentMiddle = aThinner ? (minB + maxB) * 0.5f : (minA + maxA) * 0.5f;
            var outer = Mathf.Max(0.5f, (aThinner ? maxB - minB : maxA - minA) * 0.5f);

            var top = float.MinValue;
            var bottom = float.MaxValue;
            foreach (var vertex in vertices)
            {
                top = Mathf.Max(top, vertex.y);
                bottom = Mathf.Min(bottom, vertex.y);
            }

            m_Top = top;
            m_Bottom = bottom;
            m_Center = m_Normal * normalMiddle + tangent * tangentMiddle + Vector3.up * (top - outer);
            m_Radius = outer * 0.6f;

            var triangles = filter.sharedMesh.triangles;
            for (var pass = 0; pass < 6; pass++)
            {
                var sum = Vector3.zero;
                var total = 0f;
                var count = 0;
                const int rays = 32;
                for (var i = 0; i < rays; i++)
                {
                    var angle = i * Mathf.PI * 2f / rays;
                    var direction = tangent * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle);
                    if (!Raycast(vertices, triangles, m_Center, direction, out var distance, out _) || distance > outer * 1.2f)
                        continue;
                    sum += m_Center + direction * distance;
                    total += distance;
                    count++;
                }

                // Le trou doit être fermé tout autour : sinon, on garde l'estimation.
                if (count < rays * 3 / 4)
                    break;
                m_Center = sum / count;
                m_Radius = total / count;
            }
        }

        static void Extent(List<Vector3> vertices, Vector3 axis, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;
            foreach (var vertex in vertices)
            {
                var value = Vector3.Dot(vertex, axis);
                min = Mathf.Min(min, value);
                max = Mathf.Max(max, value);
            }
        }

        static Vector3 Flat(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector3.forward;
        }

        // Une rune par obélisque, posée sur la pierre, sur la face qui ne regarde pas le portail : à hauteur de poitrine
        // ou à son pied, un obélisque sur deux.
        void BuildRunes()
        {
            if (m_GlowMaterial == null || m_Ring == null)
                return;

            var ordered = new List<Transform>();
            foreach (var obelisk in m_Obelisks)
            {
                if (obelisk != null)
                    ordered.Add(obelisk);
            }

            var ringPosition = m_Ring.position;
            ordered.Sort((a, b) => Angle(a.position - ringPosition).CompareTo(Angle(b.position - ringPosition)));
            for (var i = 0; i < ordered.Count; i++)
                m_Runes.Add(BuildRune(ordered[i], i, ringPosition));
        }

        static float Angle(Vector3 direction) => Mathf.Atan2(direction.x, direction.z);

        Rune BuildRune(Transform obelisk, int index, Vector3 ringPosition)
        {
            var renderer = obelisk.GetComponentInChildren<Renderer>();
            var bounds = renderer != null ? renderer.bounds : new Bounds(obelisk.position + Vector3.up, Vector3.one);
            var outward = bounds.center - ringPosition;
            outward.y = 0f;
            outward = outward.sqrMagnitude > 1e-4f ? outward.normalized : Vector3.forward;

            var atFoot = index % 2 == 1;
            var height = bounds.min.y + (atFoot ? 0.3f : 1.35f);
            var axis = new Vector3(bounds.center.x, height, bounds.center.z);
            var depth = Mathf.Abs(bounds.extents.x * outward.x) + Mathf.Abs(bounds.extents.z * outward.z);
            var position = axis + outward * (depth + 0.02f);
            var facing = outward;

            // Sur la pierre elle-même : un rayon lancé de l'extérieur vers l'obélisque, sur ses triangles (le modèle a peu
            // de sommets : à hauteur de poitrine, il n'y en a souvent aucun). La rune épouse la face touchée.
            var filter = obelisk.GetComponentInChildren<MeshFilter>();
            if (filter != null && TryCastMesh(filter, axis + outward * 20f, -outward, out var hit, out var normal))
            {
                // Puis au milieu de cette face, face à l'axe de l'obélisque : vu de l'anneau, le rayon tombe souvent près
                // d'une arête, et la rune débordait dans le vide.
                if (TryCastMesh(filter, axis + normal * 20f, -normal, out var centered, out var centeredNormal))
                {
                    hit = centered;
                    normal = centeredNormal;
                }

                facing = normal;
                position = hit + normal * 0.02f;
            }

            var go = new GameObject($"Rune {index + 1}");
            go.transform.SetParent(transform, true);
            go.transform.SetPositionAndRotation(position, Quaternion.LookRotation(-facing, Vector3.up));
            go.transform.localScale = Vector3.one * m_RuneSize;
            go.AddComponent<MeshFilter>().sharedMesh = BuildGlyph(index);
            var runeRenderer = go.AddComponent<MeshRenderer>();
            runeRenderer.sharedMaterial = m_GlowMaterial;
            runeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var lightObject = new GameObject("Lueur");
            lightObject.transform.SetParent(go.transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 0f, -1.5f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = m_RuneColor;
            light.range = 2.2f;
            light.shadows = LightShadows.None;
            light.enabled = false;

            return new Rune { Transform = go.transform, Renderer = runeRenderer, Light = light };
        }

        // Le point de la pierre touché par un rayon (monde), et la normale de sa face.
        static bool TryCastMesh(MeshFilter filter, Vector3 origin, Vector3 direction, out Vector3 point, out Vector3 normal)
        {
            point = default;
            normal = -direction;
            if (!TryGetVertices(filter, out var vertices) || !Raycast(vertices, filter.sharedMesh.triangles, origin, direction, out var distance, out normal))
                return false;

            point = origin + direction * distance;
            return true;
        }

        // Le premier triangle touché par le rayon (sommets dans le monde), et sa normale tournée vers l'origine du rayon.
        static bool Raycast(List<Vector3> vertices, int[] triangles, Vector3 origin, Vector3 direction, out float best, out Vector3 normal)
        {
            normal = -direction;
            best = float.MaxValue;
            for (var i = 0; i + 2 < triangles.Length; i += 3)
            {
                var a = vertices[triangles[i]];
                var b = vertices[triangles[i + 1]];
                var c = vertices[triangles[i + 2]];
                var edge1 = b - a;
                var edge2 = c - a;
                var p = Vector3.Cross(direction, edge2);
                var determinant = Vector3.Dot(edge1, p);
                if (Mathf.Abs(determinant) < 1e-7f)
                    continue;

                var inverse = 1f / determinant;
                var t = origin - a;
                var u = Vector3.Dot(t, p) * inverse;
                if (u < 0f || u > 1f)
                    continue;
                var q = Vector3.Cross(t, edge1);
                var v = Vector3.Dot(direction, q) * inverse;
                if (v < 0f || u + v > 1f)
                    continue;
                var distance = Vector3.Dot(edge2, q) * inverse;
                if (distance <= 0f || distance >= best)
                    continue;

                best = distance;
                normal = Vector3.Cross(edge1, edge2).normalized;
                if (Vector3.Dot(normal, direction) > 0f)
                    normal = -normal;
            }

            return best < float.MaxValue;
        }

        static bool TryGetVertices(MeshFilter filter, out List<Vector3> world)
        {
            world = null;
            var mesh = filter.sharedMesh;
            if (mesh == null || !mesh.isReadable)
                return false;

            var local = mesh.vertices;
            world = new List<Vector3>(local.Length);
            foreach (var vertex in local)
                world.Add(filter.transform.TransformPoint(vertex));
            return world.Count > 0;
        }

        // Cinq runes différentes (inspirées de l'alphabet runique), en traits, dans un carré de 1 × 1,4 centré.
        static readonly Vector2[][][] k_Glyphs =
        {
            new[] { new[] { new Vector2(0.3f, 0f), new Vector2(0.3f, 1.4f) }, new[] { new Vector2(0.3f, 1f), new Vector2(0.8f, 1.35f) },
                    new[] { new Vector2(0.3f, 0.65f), new Vector2(0.8f, 1f) } },
            new[] { new[] { new Vector2(0.5f, 0f), new Vector2(0.5f, 1.4f) }, new[] { new Vector2(0.5f, 0.8f), new Vector2(0.1f, 1.3f) },
                    new[] { new Vector2(0.5f, 0.8f), new Vector2(0.9f, 1.3f) } },
            new[] { new[] { new Vector2(0.5f, 0.15f), new Vector2(0.9f, 0.7f) }, new[] { new Vector2(0.9f, 0.7f), new Vector2(0.5f, 1.25f) },
                    new[] { new Vector2(0.5f, 1.25f), new Vector2(0.1f, 0.7f) }, new[] { new Vector2(0.1f, 0.7f), new Vector2(0.5f, 0.15f) } },
            new[] { new[] { new Vector2(0.1f, 0.2f), new Vector2(0.1f, 1.2f) }, new[] { new Vector2(0.9f, 0.2f), new Vector2(0.9f, 1.2f) },
                    new[] { new Vector2(0.1f, 0.2f), new Vector2(0.9f, 1.2f) }, new[] { new Vector2(0.1f, 1.2f), new Vector2(0.9f, 0.2f) } },
            new[] { new[] { new Vector2(0.7f, 1.3f), new Vector2(0.3f, 0.85f) }, new[] { new Vector2(0.3f, 0.85f), new Vector2(0.7f, 0.55f) },
                    new[] { new Vector2(0.7f, 0.55f), new Vector2(0.3f, 0.1f) } },
        };

        static Mesh BuildGlyph(int index)
        {
            var builder = new MeshBuilder();
            const float thickness = 0.09f;
            foreach (var stroke in k_Glyphs[index % k_Glyphs.Length])
            {
                var a = new Vector3(stroke[0].x - 0.5f, stroke[0].y - 0.7f, 0f);
                var b = new Vector3(stroke[1].x - 0.5f, stroke[1].y - 0.7f, 0f);
                var side = Vector3.Cross(b - a, Vector3.forward).normalized * (thickness * 0.5f);
                var along = (b - a).normalized * (thickness * 0.5f);
                builder.Quad(a - along - side, b + along - side, b + along + side, a - along + side, Color.white);
            }

            return builder.ToMesh("Rune");
        }

        // Le voile du portail : un disque sombre au bord violet lumineux, des étoiles qui tournent, et de temps en temps
        // un bloc rouge qui bugue.
        void BuildVeil()
        {
            if (m_GlowMaterial == null)
                return;

            var veil = new GameObject("Voile du portail");
            veil.transform.SetParent(transform, true);
            veil.transform.SetPositionAndRotation(m_Center, Quaternion.LookRotation(m_Normal));
            m_Veil = veil.transform;

            var builder = new MeshBuilder();
            const int sides = 48;
            var deep = new Color(0.04f, 0.01f, 0.12f, 0.92f);
            var middle = new Color(0.25f, 0.08f, 0.55f, 0.85f);
            var rim = new Color(1.6f, 0.7f, 2.4f, 0.9f);
            for (var i = 0; i < sides; i++)
            {
                var a0 = i * Mathf.PI * 2f / sides;
                var a1 = (i + 1) * Mathf.PI * 2f / sides;
                var d0 = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f);
                var d1 = new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f);
                builder.Triangle(Vector3.zero, d0 * m_Radius * 0.55f, d1 * m_Radius * 0.55f, deep);
                builder.GradientQuad(d0 * m_Radius * 0.55f, d0 * m_Radius * 0.85f, d1 * m_Radius * 0.85f, d1 * m_Radius * 0.55f, deep, middle);
                builder.GradientQuad(d0 * m_Radius * 0.85f, d0 * m_Radius, d1 * m_Radius, d1 * m_Radius * 0.85f, middle, rim);
            }

            AddPart(veil.transform, "Disque", builder.ToMesh("Voile du portail"));

            var stars = new MeshBuilder();
            var random = new System.Random(7);
            for (var i = 0; i < 70; i++)
            {
                var angle = (float)random.NextDouble() * Mathf.PI * 2f;
                var distance = Mathf.Sqrt((float)random.NextDouble()) * m_Radius * 0.9f;
                var center = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), -0.01f) * distance;
                center.z = -0.01f;
                var size = 0.02f + 0.05f * (float)random.NextDouble();
                var color = random.NextDouble() < 0.7 ? new Color(2f, 2f, 2.2f, 1f) : new Color(1.2f, 0.6f, 2.2f, 1f);
                stars.Quad(center + new Vector3(-size, 0f, 0f), center + new Vector3(0f, size, 0f),
                           center + new Vector3(size, 0f, 0f), center + new Vector3(0f, -size, 0f), color);
            }

            m_VeilStars = AddPart(veil.transform, "Étoiles", stars.ToMesh("Étoiles du portail")).transform;

            var block = new MeshBuilder();
            block.Quad(new Vector3(-0.5f, -0.5f, -0.02f), new Vector3(-0.5f, 0.5f, -0.02f), new Vector3(0.5f, 0.5f, -0.02f),
                       new Vector3(0.5f, -0.5f, -0.02f), new Color(2.5f, 0.15f, 0.2f, 0.85f));
            m_GlitchBlock = AddPart(veil.transform, "Bug", block.ToMesh("Bug du portail"));
            m_GlitchBlock.enabled = false;

            var lightObject = new GameObject("Lueur du portail");
            lightObject.transform.SetParent(veil.transform, false);
            m_VeilLight = lightObject.AddComponent<Light>();
            m_VeilLight.type = LightType.Point;
            m_VeilLight.color = new Color(0.65f, 0.45f, 1f);
            m_VeilLight.range = m_Radius * 4f;
            m_VeilLight.shadows = LightShadows.None;
        }

        MeshRenderer AddPart(Transform parent, string partName, Mesh mesh)
        {
            var go = new GameObject(partName);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_GlowMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return renderer;
        }

        void AnimateVeil(float deltaTime)
        {
            if (m_Veil == null)
                return;

            if (m_VeilStars != null)
                m_VeilStars.localRotation = Quaternion.Euler(0f, 0f, m_Age * 12f);
            if (m_VeilLight != null)
                m_VeilLight.intensity = 2.5f + 0.8f * Mathf.Sin(m_Age * 2.3f);

            // Un bug de temps en temps (au plus deux par seconde) : un bloc rouge qui saute dans le disque.
            m_GlitchTimer -= deltaTime;
            if (m_GlitchTimer <= 0f && m_GlitchBlock != null)
            {
                var show = !m_GlitchBlock.enabled;
                m_GlitchBlock.enabled = show;
                if (show)
                {
                    var spot = Random.insideUnitCircle * m_Radius * 0.7f;
                    m_GlitchBlock.transform.localPosition = new Vector3(spot.x, spot.y, 0f);
                    m_GlitchBlock.transform.localScale = new Vector3(Random.Range(0.3f, 1.2f), Random.Range(0.05f, 0.25f), 1f) * m_Radius * 0.6f;
                    m_GlitchTimer = Random.Range(0.06f, 0.12f);
                }
                else
                {
                    m_GlitchTimer = Random.Range(0.5f, 1.6f);
                }
            }
        }

        void DestroyVeil()
        {
            foreach (var filter in m_Veil.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh != null)
                    Destroy(filter.sharedMesh);
            }

            Destroy(m_Veil.gameObject);
            m_Veil = null;
            m_VeilStars = null;
            m_VeilLight = null;
            m_GlitchBlock = null;
        }

        // ---------------------------------------------------------------- tests

        void UpdateDebugKeys()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // P : allume les 5 runes (le portail s'ouvre) ; P de nouveau : entre dans la dimension.
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.pKey.wasPressedThisFrame || m_Busy || m_Used || PlayerInDimension)
                return;

            if (!m_Open)
            {
                foreach (var rune in m_Runes)
                {
                    rune.Lit = true;
                    rune.Flash = 1f;
                }

                Open();
            }
            else
            {
                m_EntrySide = Side(HeadPosition()) > 0f ? 1 : -1;
                StartCoroutine(Enter());
            }
#endif
        }
    }
}
