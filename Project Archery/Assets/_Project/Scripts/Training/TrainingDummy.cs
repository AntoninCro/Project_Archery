using Archery.Combat;
using Archery.Difficulty;
using Archery.Enemies;
using Archery.UI;
using Archery.Waves;
using TMPro;
using UnityEngine;

namespace Archery.Training
{
    /// <summary>
    /// Un mannequin du centre d'entraînement qui compte comme un ennemi (26 octobre, demandé par le joueur, pour essayer les
    /// améliorations) : entre les vagues (avant la première et pendant les pauses), les flèches et toutes leurs améliorations le
    /// traitent comme un vrai ennemi (foudre, explosion, aimant, gel, marque, bourreau…), avec les PV d'un ennemi qui apparaîtrait
    /// maintenant (difficulté et vague). Il ne bouge pas (une poussée le déplace, puis il revient à sa place), n'attaque pas, ne
    /// rapporte ni points, ni or, ni succès, et les tourelles l'ignorent (<see cref="Enemy.IsTrainingDummy"/>). Tué, il tombe
    /// en arrière, puis se relève.
    /// </summary>
    /// <remarks>
    /// Pendant une vague, il redevient le mannequin d'avant (invulnérable, il se régénère, hors des ennemis) : l'auto-visée, les
    /// éclairs ou la marque du chasseur ne partent pas vers lui au milieu des vrais ennemis. Posé sur le prefab
    /// <c>Training Dummy</c>, avec son <see cref="Enemy"/>, par <c>Editor/RoguelikeTuningBaker</c>.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Enemy))]
    public class TrainingDummy : EnemyBehaviour
    {
        // Le temps (s) pour se relever, une fois le délai passé.
        const float k_RiseTime = 0.4f;

        [Tooltip("Temps (s) entre sa mort et le moment où il se relève.")]
        [SerializeField]
        float m_ReviveDelay = 2.5f;

        [Tooltip("Vitesse (m/s) à laquelle il revient à sa place après une poussée (aimant, coup de poing).")]
        [SerializeField]
        float m_ReturnSpeed = 1.5f;

        [Tooltip("Ses PV, en multiple de ceux d'un ennemi qui apparaîtrait maintenant (×100 depuis le 27 octobre au soir, demandé " +
                 "par le joueur : il encaisse une longue série de tirs, mesurée par son compteur de dégâts).")]
        [SerializeField]
        float m_HealthMultiplier = 100f;

        [Tooltip("Hauteur (m) du compteur de dégâts au-dessus du haut du mannequin.")]
        [SerializeField]
        float m_MeterHeight = 0.45f;

        // Le compteur de dégâts (27 octobre au soir, demandé par le joueur, pour comparer les builds) : au-dessus du mannequin, les
        // dégâts par seconde de la série en cours et son total, toutes sources comprises (flèches, foudre, explosions, pieux…).
        // Une série finit après k_SeriesGap s sans coup : son résultat reste affiché, plus pâle, jusqu'au coup suivant, et le
        // mannequin reprend tous ses PV. Le temps est celui du jeu (le ralenti du faucon ou de l'acrobate ralentit aussi les ennemis).
        const float k_SeriesGap = 3f;
        static readonly Color k_MeterColor = new Color(1f, 0.86f, 0.42f);
        static readonly Color k_MeterDoneColor = new Color(0.8f, 0.8f, 0.85f, 0.75f);

        TextMeshPro m_Meter;
        float m_MeterTop;
        bool m_SeriesOpen;
        float m_SeriesStart;
        float m_LastHit;
        float m_SeriesTotal;
        string m_ShownText;

        Health m_Health;
        Vector3 m_Home;
        Quaternion m_HomeRotation;
        Quaternion m_FallenRotation;
        bool m_Ready;
        bool m_InWave;
        float m_DeadTime;
        float m_RiseTime = -1f;

        public override Vector3 Velocity => Vector3.zero;

        public override bool IsTrainingDummy => true;

        protected override void Awake()
        {
            base.Awake();
            m_Health = GetComponent<Health>();
        }

        void OnEnable()
        {
            if (m_Health != null)
                m_Health.Damaged += OnDamaged;
        }

        void OnDisable()
        {
            if (m_Health != null)
                m_Health.Damaged -= OnDamaged;
        }

        void Start()
        {
            m_Home = transform.position;
            m_HomeRotation = transform.rotation;
            m_MeterTop = MeasureTop();
            Apply(InWave());
        }

        void Update()
        {
            var wave = InWave();
            if (!m_Ready || wave != m_InWave)
                Apply(wave);

            // La série finit : le mannequin reprend tous ses PV pour la suivante.
            if (m_SeriesOpen && Time.time - m_LastHit > k_SeriesGap)
            {
                m_SeriesOpen = false;
                if (m_Health != null && Enemy != null && Enemy.IsAlive)
                    m_Health.Heal(m_Health.Max);
            }
        }

        void LateUpdate() => UpdateMeter();

        void OnDamaged(Health health, DamageInfo info)
        {
            if (m_InWave || info.Amount <= 0f)
                return;

            var now = Time.time;
            if (!m_SeriesOpen)
            {
                m_SeriesOpen = true;
                m_SeriesStart = now;
                m_SeriesTotal = 0f;
            }

            m_LastHit = now;
            m_SeriesTotal += info.Amount;
        }

        // « 1 234 /s », et dessous le total et la durée de la série ; caché pendant les vagues et avant le premier coup.
        void UpdateMeter()
        {
            var shown = !m_InWave && m_SeriesTotal > 0f;
            if (!shown)
            {
                if (m_Meter != null && m_Meter.gameObject.activeSelf)
                    m_Meter.gameObject.SetActive(false);
                return;
            }

            if (m_Meter == null)
                m_Meter = CreateMeter();
            if (!m_Meter.gameObject.activeSelf)
                m_Meter.gameObject.SetActive(true);

            var duration = Mathf.Max(1f, (m_SeriesOpen ? Time.time : m_LastHit) - m_SeriesStart);
            var text = $"{Format(m_SeriesTotal / duration)} /s\n<size=60%>Total {Format(m_SeriesTotal)} en " +
                       $"{duration.ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo("fr-FR"))} s</size>";
            if (text != m_ShownText)
            {
                m_ShownText = text;
                m_Meter.text = text;
            }

            m_Meter.color = m_SeriesOpen ? k_MeterColor : k_MeterDoneColor;
            var position = new Vector3(transform.position.x, m_MeterTop + m_MeterHeight, transform.position.z);
            m_Meter.transform.position = position;
            var camera = Camera.main;
            if (camera != null)
            {
                var toText = position - camera.transform.position;
                if (toText.sqrMagnitude > 1e-6f)
                    m_Meter.transform.rotation = Quaternion.LookRotation(toText, Vector3.up);
                m_Meter.transform.localScale = Vector3.one * Mathf.Clamp(toText.magnitude * 0.08f, 0.4f, 2.5f);
            }
        }

        TextMeshPro CreateMeter()
        {
            // Pas enfant du mannequin : il tombe et se relève, le compteur reste droit au-dessus de sa place.
            var go = new GameObject("Compteur de dégâts (" + name + ")");
            go.transform.SetParent(transform.parent, false);
            var text = go.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.fontSize = 2.2f;
            text.fontStyle = FontStyles.Bold;
            text.rectTransform.sizeDelta = new Vector2(4f, 1f);
            var theme = UiTheme.Load();
            if (theme != null)
                UiThemer.ApplyLabel(text, theme);
            text.enableVertexGradient = false;
            text.outlineWidth = 0.2f;
            text.outlineColor = new Color32(18, 8, 34, 220);
            return text;
        }

        // Le haut du mannequin (ses renderers), pour y poser le compteur.
        float MeasureTop()
        {
            var top = transform.position.y + 1.8f;
            var found = false;
            foreach (var renderer in GetComponentsInChildren<Renderer>())
            {
                if (renderer is ParticleSystemRenderer)
                    continue;
                top = found ? Mathf.Max(top, renderer.bounds.max.y) : renderer.bounds.max.y;
                found = true;
            }

            return top;
        }

        static string Format(float value) =>
            Mathf.RoundToInt(value).ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' ');

        void OnDestroy()
        {
            if (m_Meter != null)
                Destroy(m_Meter.gameObject);
        }

        static bool InWave()
        {
            var waves = WaveManager.Instance;
            return waves != null && waves.Phase == WavePhase.Wave;
        }

        // Ennemi entre les vagues, mannequin invulnérable pendant.
        void Apply(bool wave)
        {
            m_Ready = true;
            m_InWave = wave;
            if (Enemy == null || m_Health == null)
                return;

            if (!Enemy.IsAlive)
                Enemy.Revive();

            transform.SetPositionAndRotation(m_Home, m_HomeRotation);
            m_Health.Invulnerable = wave;
            Enemy.enabled = !wave;
            if (!wave)
                m_Health.ResetHealth(Enemy.Definition.maxHealth * DifficultyManager.Current.enemyHealth * EnemyScaling.Health *
                                     Mathf.Max(0.01f, m_HealthMultiplier));
        }

        // Il ne bouge pas : après une poussée, il revient à sa place.
        public override void Tick(float deltaTime)
        {
            transform.position = Vector3.MoveTowards(transform.position, m_Home, m_ReturnSpeed * deltaTime);
        }

        // Il ne s'enfuit pas à la fin de la vague (il n'est d'ailleurs pas parmi les ennemis pendant une vague).
        public override void BeginFlee(Vector3 exitPoint)
        {
        }

        public override void TickFlee(float deltaTime)
        {
        }

        public override void OnDied()
        {
            m_DeadTime = 0f;
            m_RiseTime = -1f;
        }

        // Il tombe en arrière (fait par Enemy), attend, puis se relève et revit.
        public override bool TickCorpse(float deltaTime)
        {
            m_DeadTime += deltaTime;
            if (m_DeadTime < m_ReviveDelay)
                return false;

            if (m_RiseTime < 0f)
            {
                m_RiseTime = 0f;
                m_FallenRotation = transform.rotation;
            }

            m_RiseTime += deltaTime;
            var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(m_RiseTime / k_RiseTime));
            transform.rotation = Quaternion.Slerp(m_FallenRotation, m_HomeRotation, t);
            if (t >= 1f)
                Enemy.Revive();
            return true;
        }
    }
}
