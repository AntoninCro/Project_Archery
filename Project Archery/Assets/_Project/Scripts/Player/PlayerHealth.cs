using Archery.Combat;
using Archery.Core;
using Archery.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archery.Player
{
    /// <summary>
    /// PV du joueur (GDD, section 3) : vibrations et voile rouge quand il est touché.
    /// À placer sur le XR Origin, avec un <see cref="Health"/> et le <see cref="PlayerRig"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health), typeof(PlayerRig))]
    public class PlayerHealth : MonoBehaviour
    {
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("Optionnel : un quad devant la caméra (matériau URP/Unlit transparent) qui rougit quand on est touché.")]
        [SerializeField]
        Renderer m_DamageOverlay;

        [SerializeField]
        Color m_OverlayColor = new Color(0.75f, 0f, 0f, 0.45f);

        [SerializeField]
        float m_OverlayFadeSpeed = 2.5f;

        [SerializeField]
        AudioClip m_HurtClip;

        [Tooltip("En attendant le vrai écran de fin de partie : recharge la scène après la mort.")]
        [SerializeField]
        bool m_ReloadSceneOnDeath = true;

        [SerializeField]
        float m_ReloadDelay = 3f;

        Health m_Health;
        PlayerRig m_Rig;
        MaterialPropertyBlock m_PropertyBlock;
        float m_OverlayAmount;

        public static PlayerHealth Instance { get; private set; }

        public Health Health => m_Health;
        public bool IsAlive => m_Health != null && m_Health.IsAlive;

        /// <summary>Point visé par les ennemis : les pieds du joueur.</summary>
        public Vector3 BodyPosition => m_Rig.BodyPosition;

        void Awake()
        {
            Instance = this;
            m_Health = GetComponent<Health>();
            m_Rig = GetComponent<PlayerRig>();
            m_PropertyBlock = new MaterialPropertyBlock();
            if (m_DamageOverlay != null)
                m_DamageOverlay.enabled = false;
        }

        void OnEnable()
        {
            m_Health.Damaged += OnDamaged;
            m_Health.Died += OnDied;
        }

        void OnDisable()
        {
            m_Health.Damaged -= OnDamaged;
            m_Health.Died -= OnDied;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Update()
        {
            if (m_DamageOverlay == null || m_OverlayAmount <= 0f)
                return;

            m_OverlayAmount = Mathf.MoveTowards(m_OverlayAmount, 0f, m_OverlayFadeSpeed * Time.deltaTime);
            var color = m_OverlayColor;
            color.a *= m_OverlayAmount;
            m_DamageOverlay.GetPropertyBlock(m_PropertyBlock);
            m_PropertyBlock.SetColor(k_BaseColorId, color);
            m_DamageOverlay.SetPropertyBlock(m_PropertyBlock);
            m_DamageOverlay.enabled = m_OverlayAmount > 0f;
        }

        void OnDamaged(Health health, DamageInfo info)
        {
            var strength = Mathf.Clamp01(info.Amount / 20f);
            foreach (var hand in m_Rig.Hands)
                Haptics.Pulse(hand, 0.3f + 0.5f * strength, 0.15f);

            m_OverlayAmount = Mathf.Max(m_OverlayAmount, 0.5f + 0.5f * strength);
            if (m_DamageOverlay != null)
                m_DamageOverlay.enabled = true;

            var head = m_Rig.Head;
            Sfx.Play(m_HurtClip, head != null ? head.position : transform.position, 0.9f, 1f, 0f);
        }

        void OnDied(Health health, DamageInfo info)
        {
            var head = m_Rig.Head;
            if (head != null)
                FloatingText.Spawn(head.position + head.forward * 2f, "Tu es mort !", new Color(1f, 0.3f, 0.25f), 0.6f, m_ReloadDelay);

            if (m_ReloadSceneOnDeath)
                Invoke(nameof(ReloadScene), m_ReloadDelay);
        }

        void ReloadScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
