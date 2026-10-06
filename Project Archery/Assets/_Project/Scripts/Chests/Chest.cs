using System.Collections.Generic;
using Archery.Core;
using Archery.Player;
using Archery.UI;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Chests
{
    /// <summary>
    /// Un coffre de la forêt (GDD, section 13), gratuit. On soulève le couvercle à la main : le temps ralentit,
    /// trois orbes sortent du coffre, et on en attrape une : deux améliorations permanentes (tirées comme en boutique)
    /// et un bonus temporaire (dégâts doublés, tirs parfaits ou anneau rapide).
    /// </summary>
    /// <remarks>
    /// Un rayon de lumière le signale de loin ; il s'éteint à l'ouverture. Le coffre disparaît à la fin de la vague
    /// (<see cref="ChestSpawner"/>), qu'il ait été ouvert ou non.
    /// </remarks>
    [DisallowMultipleComponent]
    public class Chest : MonoBehaviour
    {
        [Tooltip("Couvercle à soulever.")]
        [SerializeField]
        ChestLid m_Lid;

        [Tooltip("Prefab d'une orbe.")]
        [SerializeField]
        ChestOrb m_OrbPrefab;

        [Tooltip("Point au-dessus du coffre où flottent les orbes. Vide : 1,1 m au-dessus du coffre.")]
        [SerializeField]
        Transform m_OrbAnchor;

        [Tooltip("Écart (m) entre deux orbes.")]
        [SerializeField]
        float m_OrbSpacing = 0.4f;

        [Tooltip("Rayon de lumière qui signale le coffre, éteint à l'ouverture.")]
        [SerializeField]
        GameObject m_Beam;

        [Tooltip("Optionnel : effet allumé à l'ouverture, par exemple la lueur du pack de coffre. Il est éteint au départ.")]
        [SerializeField]
        GameObject m_OpenEffect;

        [Header("Ralenti")]
        [Tooltip("Vitesse du temps pendant le choix d'une orbe (0,3 = trois fois plus lent).")]
        [Range(0.05f, 1f)]
        [SerializeField]
        float m_SlowMotionScale = 0.3f;

        [Tooltip("Durée maximale (s, en temps réel) du ralenti si aucune orbe n'est prise. Les orbes restent ensuite.")]
        [SerializeField]
        float m_SlowMotionMaxDuration = 6f;

        [Header("Récompenses")]
        [Tooltip("Durée (s) des bonus temporaires.")]
        [SerializeField]
        float m_BuffDuration = 30f;

        [SerializeField]
        ChestColors m_Colors = ChestColors.Default;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_OpenClip;

        [SerializeField]
        AudioClip m_PickClip;

        [SerializeField]
        AudioClip m_VanishClip;

        readonly List<ChestOrb> m_Orbs = new List<ChestOrb>();
        float m_SlowMotionLeft;
        bool m_Vanishing;
        float m_VanishTime;
        Vector3 m_BaseScale;

        public bool IsOpened { get; private set; }

        /// <summary>Une orbe a été prise.</summary>
        public bool IsLooted { get; private set; }

        void Awake()
        {
            m_BaseScale = transform.localScale;
            if (m_OpenEffect != null)
                m_OpenEffect.SetActive(false);
            if (m_Lid == null)
                m_Lid = GetComponentInChildren<ChestLid>(true);
            if (m_Lid != null)
                m_Lid.Opened += Open;
            else
                Debug.LogWarning("Chest : aucun Chest Lid dans le coffre.", this);
        }

        void OnDestroy()
        {
            if (m_Lid != null)
                m_Lid.Opened -= Open;
            SlowMotion.End(this);
            foreach (var orb in m_Orbs)
            {
                if (orb != null)
                    Destroy(orb.gameObject);
            }
        }

        void Update()
        {
            if (m_SlowMotionLeft > 0f)
            {
                m_SlowMotionLeft -= Time.unscaledDeltaTime;
                if (m_SlowMotionLeft <= 0f)
                    SlowMotion.End(this);
            }

            if (!m_Vanishing)
                return;

            // Le coffre rétrécit puis disparaît.
            m_VanishTime += Time.unscaledDeltaTime;
            transform.localScale = m_BaseScale * Mathf.Max(0f, 1f - m_VanishTime / 0.4f);
            if (m_VanishTime >= 0.4f)
                Destroy(gameObject);
        }

        void Open()
        {
            if (IsOpened || m_Vanishing)
                return;

            IsOpened = true;
            if (m_Beam != null)
                m_Beam.SetActive(false);
            if (m_OpenEffect != null)
                m_OpenEffect.SetActive(true);

            Sfx.Play(m_OpenClip, transform.position + Vector3.up * 0.5f);
            SpawnOrbs();

            SlowMotion.Begin(this, m_SlowMotionScale);
            m_SlowMotionLeft = m_SlowMotionMaxDuration;
        }

        // Les orbes sortent du coffre et s'alignent au-dessus. Vues par le joueur, qui fait face au coffre, de gauche à droite :
        // les deux améliorations permanentes, puis le bonus temporaire.
        void SpawnOrbs()
        {
            if (m_OrbPrefab == null)
            {
                Debug.LogWarning("Chest : aucun prefab d'orbe.", this);
                return;
            }

            var rewards = ChestRewards.Roll(m_BuffDuration, m_Colors);
            var anchor = m_OrbAnchor != null ? m_OrbAnchor.position : transform.position + transform.up * 1.1f;
            var from = transform.position + transform.up * 0.4f;
            for (var i = 0; i < rewards.Count; i++)
            {
                var offset = (i - (rewards.Count - 1) * 0.5f) * m_OrbSpacing;
                var orb = Instantiate(m_OrbPrefab, from, Quaternion.identity);
                orb.Setup(this, rewards[i], from, anchor - transform.right * offset);
                m_Orbs.Add(orb);
            }
        }

        /// <summary>Appelé par une orbe attrapée : sa récompense est donnée, les autres disparaissent.</summary>
        public void Pick(ChestOrb picked, IXRSelectInteractor hand)
        {
            if (IsLooted || m_Vanishing)
                return;

            IsLooted = true;
            var reward = picked.Reward;
            var message = reward.Apply(m_BuffDuration);

            foreach (var orb in m_Orbs)
            {
                if (orb != null)
                    orb.Leave();
            }

            Haptics.Pulse(hand, 0.7f, 0.15f);
            Sfx.Play(m_PickClip, picked.transform.position, 0.9f, 1f, 0.3f);
            ShowMessage(message, reward.Color);

            SlowMotion.End(this);
            m_SlowMotionLeft = 0f;
        }

        /// <summary>Le coffre disparaît (fin de la vague) : ses orbes aussi, et le ralenti s'arrête.</summary>
        public void Vanish()
        {
            if (m_Vanishing)
                return;

            m_Vanishing = true;
            SlowMotion.End(this);
            m_SlowMotionLeft = 0f;

            foreach (var orb in m_Orbs)
            {
                if (orb != null)
                    orb.Leave();
            }

            Sfx.Play(m_VanishClip, transform.position + Vector3.up * 0.5f, 0.7f);
        }

        static void ShowMessage(string message, Color color)
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (head != null)
                FloatingText.Spawn(head.position + rig.HeadYaw * new Vector3(0f, 0.1f, 1.6f), message, color, 0.9f, 2f);
        }
    }
}
