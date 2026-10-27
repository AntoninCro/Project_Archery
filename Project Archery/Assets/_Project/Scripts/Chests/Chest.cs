using System.Collections.Generic;
using Archery.Core;
using Archery.Player;
using Archery.UI;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Chests
{
    /// <summary>
    /// Un coffre de la forêt (GDD, section 13), gratuit. On l'ouvre en le visant (gâchette ou gâchette de prise, à quelques mètres) :
    /// le temps ralentit, trois orbes sortent du coffre, et on en prend une de la même façon. Depuis le 27 octobre au soir
    /// (demandé par le joueur) : un objet, un parchemin, et un bonus temporaire (dégâts doublés, tirs parfaits ou anneau rapide) ;
    /// un fragment de l'arc légendaire peut prendre la place du parchemin. (De la refonte du 25 octobre jusque-là : un objet une
    /// fois sur 4, sinon un parchemin, puis un autre parchemin, et le bonus.)
    /// Le coffre de boss (<see cref="MakeBossChest"/>), plus grand, ne propose que des objets.
    /// </summary>
    /// <remarks>
    /// Un rayon de lumière le signale de loin ; il s'éteint à l'ouverture. Le coffre disparaît à la fin de la vague
    /// (<see cref="ChestSpawner"/>), qu'il ait été ouvert ou non ; le coffre de boss, à la fin de la vague suivante.
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

        [Tooltip("Le coffre ouvert disparaît au bout de ce temps (s), avec ses orbes s'il n'en a pas pris, " +
                 "pour ne pas le confondre avec un coffre neuf. 0 : il reste jusqu'à la fin de la vague.")]
        [Min(0f)]
        [SerializeField]
        float m_DespawnAfterOpen = 15f;

        [Header("Récompenses")]
        [Tooltip("Durée (s) des bonus temporaires.")]
        [SerializeField]
        float m_BuffDuration = 30f;

        [Tooltip("Chance (0 à 1) que la première orbe d'un coffre de vague soit un objet plutôt qu'un parchemin (25 octobre). 1 depuis " +
                 "le 27 octobre au soir (demandé par le joueur : plus de pièces par partie, plus de liberté pour le build) ; 0,25 avant.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_ObjectChance = 1f;

        [Tooltip("Objets proposés par le coffre de boss (un seul à prendre).")]
        [Min(1)]
        [SerializeField]
        int m_BossObjects = 2;

        [Tooltip("Taille du coffre de boss, par rapport à un coffre de vague.")]
        [SerializeField]
        float m_BossScale = 1.3f;

        [Tooltip("Lots de plans proposés par une caisse de l'ingénieur (refonte du tower defense, 27 octobre ; un seul à prendre).")]
        [Min(1)]
        [SerializeField]
        int m_CrateLots = 3;

        [SerializeField]
        ChestColors m_Colors = ChestColors.Default;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_OpenClip;

        [SerializeField]
        AudioClip m_PickClip;

        [SerializeField]
        AudioClip m_VanishClip;

        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");

        readonly List<ChestOrb> m_Orbs = new List<ChestOrb>();
        float m_SlowMotionLeft;
        float m_DespawnLeft = -1f;
        bool m_Vanishing;
        float m_VanishTime;
        Vector3 m_BaseScale;

        public bool IsOpened { get; private set; }

        /// <summary>Une orbe a été prise.</summary>
        public bool IsLooted { get; private set; }

        /// <summary>Le coffre laissé par le boss de la vague : des objets seulement (refonte du 25 octobre).</summary>
        public bool IsBossChest { get; private set; }

        /// <summary>En fait un coffre de boss, plus grand. À appeler juste après sa création, avant son ouverture.</summary>
        public void MakeBossChest()
        {
            IsBossChest = true;
            m_BaseScale *= m_BossScale;
            transform.localScale = m_BaseScale;
        }

        /// <summary>
        /// La caisse de l'ingénieur (refonte du tower defense, 27 octobre) : des lots de plans de construction, à la place des
        /// anciens gisements de cristaux (<c>Building/CrystalSpawner</c>).
        /// </summary>
        public bool IsEngineerCrate { get; private set; }

        /// <summary>
        /// En fait une caisse de l'ingénieur, dans sa couleur. À appeler juste après sa création, avant son ouverture. Le rayon
        /// prend la couleur de la caisse, pour la distinguer de loin d'un coffre doré ; le coffre est teinté en attendant le
        /// modèle de la caisse (étape 1 de la refonte : les modèles).
        /// </summary>
        public void MakeEngineerCrate(Color color)
        {
            IsEngineerCrate = true;
            var block = new MaterialPropertyBlock();
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer || (m_OpenEffect != null && renderer.transform.IsChildOf(m_OpenEffect.transform)))
                    continue;

                var isBeam = m_Beam != null && renderer.transform.IsChildOf(m_Beam.transform);
                renderer.GetPropertyBlock(block);
                block.SetColor(k_BaseColorId, isBeam ? color : Color.Lerp(Color.white, color, 0.55f));
                renderer.SetPropertyBlock(block);
            }
        }

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

            if (m_DespawnLeft > 0f && !m_Vanishing)
            {
                m_DespawnLeft -= Time.unscaledDeltaTime;
                if (m_DespawnLeft <= 0f)
                    Vanish();
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
            if (m_DespawnAfterOpen > 0f)
                m_DespawnLeft = m_DespawnAfterOpen;
        }

        // Les orbes sortent du coffre et s'alignent au-dessus. Vues par le joueur, qui fait face au coffre, de gauche à droite :
        // l'objet ou le parchemin, l'autre parchemin, puis le bonus temporaire ; ou les objets du coffre de boss ; ou les lots de
        // plans de la caisse de l'ingénieur.
        void SpawnOrbs()
        {
            if (m_OrbPrefab == null)
            {
                Debug.LogWarning("Chest : aucun prefab d'orbe.", this);
                return;
            }

            var rewards = IsEngineerCrate ? ChestRewards.RollPlans(m_CrateLots)
                : IsBossChest ? ChestRewards.RollBoss(m_BossObjects)
                : ChestRewards.Roll(m_BuffDuration, m_Colors, m_ObjectChance);
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

        /// <summary>Le coffre disparaît (fin de la vague, ou 15 s après son ouverture) : ses orbes aussi, et le ralenti s'arrête.</summary>
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
            HeadMessages.Show(message, color, 4f);
        }
    }
}
