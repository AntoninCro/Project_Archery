using System;
using System.Globalization;
using System.Text;
using Archery.Bows;
using Archery.Core;
using Archery.Economy;
using Archery.Enemies;
using Archery.Player;
using Archery.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Archery.Shop
{
    /// <summary>
    /// Le panneau de la boutique (Canvas en World Space). Il affiche les offres du <see cref="ShopManager"/>
    /// pendant les pauses et transmet les clics. Les sons partent du panneau.
    /// Pendant la pause, il suit le joueur, devant lui à gauche, sans jamais rentrer dans un mur.
    /// </summary>
    /// <remarks>
    /// Pour cliquer au rayon de la manette, le Canvas a besoin d'un <c>Tracked Device Graphic Raycaster</c>,
    /// et l'EventSystem d'un <c>XR UI Input Module</c>.
    /// </remarks>
    [DisallowMultipleComponent]
    public class ShopPanel : MonoBehaviour
    {
        [Tooltip("Tout ce qui s'affiche quand la boutique est ouverte (caché pendant les vagues).")]
        [SerializeField]
        GameObject m_Content;

        [Tooltip("Une carte par amélioration proposée (4 par défaut).")]
        [SerializeField]
        ShopCard[] m_UpgradeCards = Array.Empty<ShopCard>();

        [SerializeField]
        ShopCard m_BowCard;

        [SerializeField]
        ShopCard m_TowerCard;

        [Tooltip("Optionnel : carte « Barricades ».")]
        [SerializeField]
        ShopCard m_BarricadeCard;

        [SerializeField]
        Button m_RerollButton;

        [Tooltip("Texte du bouton de relance (son prix).")]
        [SerializeField]
        TMP_Text m_RerollText;

        [SerializeField]
        TMP_Text m_MoneyText;

        [Tooltip("Message après un clic (« Pas assez d'or »…).")]
        [SerializeField]
        TMP_Text m_MessageText;

        [Tooltip("Optionnel : liste des améliorations déjà possédées.")]
        [SerializeField]
        TMP_Text m_OwnedText;

        [SerializeField]
        float m_MessageDuration = 3f;

        [Header("Placement")]
        [Tooltip("Pendant la pause, le panneau suit le joueur, devant lui à gauche, en évitant les murs. " +
                 "Décoché : il reste à sa place dans la scène.")]
        [SerializeField]
        bool m_FollowPlayer = true;

        [Tooltip("Place préférée du panneau par rapport à la tête du joueur (X à droite, Y en haut, Z devant) : devant à gauche.")]
        [SerializeField]
        Vector3 m_OffsetFromHead = new Vector3(-1.4f, -0.4f, 1.6f);

        [Tooltip("Inclinaison (°) du haut du panneau vers l'arrière, pour le lire sans baisser la tête.")]
        [SerializeField]
        float m_Tilt = 15f;

        [Tooltip("Distance (m) dont le joueur doit s'éloigner pour que le panneau le rejoigne : on peut se pencher vers lui sans qu'il recule.")]
        [SerializeField]
        float m_FollowDeadZone = 1.2f;

        [Tooltip("Angle (°) entre le regard et le panneau au-delà duquel il revient devant le joueur : on peut tourner la tête pour le lire.")]
        [SerializeField]
        float m_FollowAngle = 90f;

        [Tooltip("Temps (s) que met le panneau à glisser vers sa nouvelle place.")]
        [SerializeField]
        float m_FollowSmoothing = 0.35f;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_BuyClip;

        [SerializeField]
        AudioClip m_ErrorClip;

        [SerializeField]
        AudioClip m_RerollClip;

        [Tooltip("Optionnel : joué quand la boutique s'ouvre.")]
        [SerializeField]
        AudioClip m_OpenClip;

        // Places essayées autour du joueur, en degrés à partir de la place préférée (négatif : vers sa gauche).
        static readonly float[] k_AngleSteps = { 0f, -15f, 15f, -30f, 30f, -50f, 45f, 65f, 85f, -75f, 110f, -105f, 140f, 180f };
        static readonly float[] k_DistanceScales = { 1f, 0.7f };
        static readonly Collider[] s_Overlaps = new Collider[16];
        static readonly RaycastHit[] s_Hits = new RaycastHit[16];

        readonly StringBuilder m_Builder = new StringBuilder();
        ShopManager m_Shop;
        ScoreManager m_Score;
        PlayerUpgrades m_Upgrades;
        float m_MessageTimer;
        bool m_WasOpen;
        Vector3 m_HomePosition;
        Quaternion m_HomeRotation;
        Vector3 m_AnchorPosition;
        Vector3 m_TargetPosition;
        Quaternion m_TargetRotation;
        float m_NextFollowCheck;

        void Awake()
        {
            m_HomePosition = transform.position;
            m_HomeRotation = transform.rotation;

            foreach (var card in m_UpgradeCards)
            {
                if (card != null)
                    card.Clicked += OnCardClicked;
            }

            if (m_BowCard != null)
                m_BowCard.Clicked += OnCardClicked;
            if (m_TowerCard != null)
                m_TowerCard.Clicked += OnCardClicked;
            if (m_BarricadeCard != null)
                m_BarricadeCard.Clicked += OnCardClicked;
            if (m_RerollButton != null)
                m_RerollButton.onClick.AddListener(OnRerollClicked);
            if (m_Content != null)
                m_Content.SetActive(false);
        }

        void Start()
        {
            m_Shop = ShopManager.Instance;
            m_Score = ScoreManager.Instance;
            m_Upgrades = PlayerUpgrades.Instance;
            if (m_Shop == null)
            {
                Debug.LogWarning("ShopPanel : aucun Shop Manager dans la scène.", this);
                return;
            }

            m_Shop.Changed += Refresh;
            if (m_Score != null)
                m_Score.Changed += Refresh;
            if (m_Upgrades != null)
                m_Upgrades.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (m_Shop != null)
                m_Shop.Changed -= Refresh;
            if (m_Score != null)
                m_Score.Changed -= Refresh;
            if (m_Upgrades != null)
                m_Upgrades.Changed -= Refresh;
        }

        void Update()
        {
            if (m_MessageTimer <= 0f)
                return;

            m_MessageTimer -= Time.deltaTime;
            if (m_MessageTimer <= 0f && m_MessageText != null)
                m_MessageText.text = "";
        }

        void Refresh()
        {
            var open = m_Shop != null && m_Shop.IsOpen;
            if (m_Content != null)
                m_Content.SetActive(open);

            if (open && !m_WasOpen)
            {
                PlaceForPlayer();
                ShowMessage("", Color.white);
                Sfx.Play(m_OpenClip, transform.position, 0.8f);
            }

            m_WasOpen = open;
            if (!open)
                return;

            var money = m_Score != null ? m_Score.Money : 0;
            var offers = m_Shop.UpgradeOffers;
            for (var i = 0; i < m_UpgradeCards.Length; i++)
            {
                if (m_UpgradeCards[i] != null)
                    m_UpgradeCards[i].Show(i < offers.Count ? offers[i] : null, money);
            }

            if (m_BowCard != null)
                m_BowCard.Show(m_Shop.BowOffer, money);
            if (m_TowerCard != null)
                m_TowerCard.Show(m_Shop.TowerOffer, money);
            if (m_BarricadeCard != null)
                m_BarricadeCard.Show(m_Shop.BarricadeOffer, money);
            if (m_RerollText != null)
                m_RerollText.text = $"Relancer\n{m_Shop.RerollCost} or";
            if (m_MoneyText != null)
                m_MoneyText.text = "Or " + money.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', ' ');
            if (m_OwnedText != null)
                m_OwnedText.text = OwnedSummary();
        }

        // À l'ouverture : devant le joueur, à sa gauche. Sans suivi, à sa place dans la scène.
        void PlaceForPlayer()
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (!m_FollowPlayer || head == null)
            {
                transform.SetPositionAndRotation(m_HomePosition, m_HomeRotation);
                return;
            }

            MoveNextTo(head.position, rig.HeadYaw, true);
        }

        // Pendant la pause, le panneau suit le joueur sans le coller : il ne bouge que si le joueur s'éloigne,
        // ou s'il le perd de vue en se tournant. Il glisse alors vers sa nouvelle place.
        void LateUpdate()
        {
            if (!m_WasOpen || !m_FollowPlayer)
                return;

            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (head == null)
                return;

            var headPosition = head.position;
            if (Time.unscaledTime >= m_NextFollowCheck)
            {
                var moved = headPosition - m_AnchorPosition;
                var lookAway = Vector3.Angle(rig.HeadYaw * Vector3.forward, Flat(m_TargetPosition - headPosition)) > m_FollowAngle;
                if (Flat(moved).magnitude > m_FollowDeadZone || Mathf.Abs(moved.y) > 0.6f || lookAway)
                {
                    MoveNextTo(headPosition, rig.HeadYaw, false);
                    m_NextFollowCheck = Time.unscaledTime + 0.4f;
                }
            }

            var t = 1f - Mathf.Exp(-Time.unscaledDeltaTime / Mathf.Max(0.01f, m_FollowSmoothing));
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, m_TargetPosition, t),
                                             Quaternion.Slerp(transform.rotation, m_TargetRotation, t));
        }

        void MoveNextTo(Vector3 head, Quaternion yaw, bool snap)
        {
            m_AnchorPosition = head;
            FindPlace(head, yaw, out m_TargetPosition, out m_TargetRotation);

            // À l'ouverture, ou après une téléportation, il apparaît directement à sa place.
            if (snap || (transform.position - m_TargetPosition).sqrMagnitude > 36f)
                transform.SetPositionAndRotation(m_TargetPosition, m_TargetRotation);
        }

        // La place préférée (devant à gauche), sinon la plus proche autour du joueur où le panneau ne rentre
        // dans rien (mur de la tour, arbre…) et reste visible. Si aucune n'est libre : la place préférée.
        void FindPlace(Vector3 head, Quaternion yaw, out Vector3 position, out Quaternion rotation)
        {
            var flat = Flat(m_OffsetFromHead);
            var distance = Mathf.Max(0.5f, flat.magnitude);
            var baseAngle = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
            var halfExtents = HalfExtents();
            position = default;
            rotation = default;
            var first = true;
            foreach (var step in k_AngleSteps)
            {
                foreach (var scale in k_DistanceScales)
                {
                    var direction = yaw * Quaternion.Euler(0f, baseAngle + step, 0f) * Vector3.forward;
                    var candidate = AboveGround(head + direction * (distance * scale) + Vector3.up * m_OffsetFromHead.y, halfExtents.y);
                    var candidateRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(m_Tilt, 0f, 0f);
                    if (first)
                    {
                        position = candidate;
                        rotation = candidateRotation;
                        first = false;
                    }

                    if (IsFree(head, candidate, candidateRotation, halfExtents))
                    {
                        position = candidate;
                        rotation = candidateRotation;
                        return;
                    }
                }
            }
        }

        // Moitié de la taille du panneau dans le monde (Canvas en World Space).
        Vector3 HalfExtents()
        {
            var size = transform is RectTransform rect ? Vector2.Scale(rect.rect.size, transform.lossyScale) : new Vector2(1.4f, 1.3f);
            return new Vector3(size.x * 0.5f, size.y * 0.5f, 0.05f);
        }

        // Le bas du panneau reste au-dessus du sol, même si le joueur est accroupi.
        static Vector3 AboveGround(Vector3 position, float halfHeight)
        {
            if (Physics.Raycast(position + Vector3.up * halfHeight, Vector3.down, out var hit, halfHeight * 2f + 0.1f,
                                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) && IsObstacle(hit.collider))
                position.y = Mathf.Max(position.y, hit.point.y + halfHeight + 0.05f);
            return position;
        }

        // Le panneau ne rentre dans rien, et rien ne le cache aux yeux du joueur.
        static bool IsFree(Vector3 head, Vector3 position, Quaternion rotation, Vector3 halfExtents)
        {
            var count = Physics.OverlapBoxNonAlloc(position, halfExtents, s_Overlaps, rotation, Physics.DefaultRaycastLayers,
                                                   QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                if (IsObstacle(s_Overlaps[i]))
                    return false;
            }

            var toPanel = position - head;
            var distance = toPanel.magnitude;
            if (distance < 0.01f)
                return true;

            count = Physics.RaycastNonAlloc(head, toPanel / distance, s_Hits, distance, Physics.DefaultRaycastLayers,
                                            QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                if (IsObstacle(s_Hits[i].collider))
                    return false;
            }

            return true;
        }

        // Le joueur, son arc et les ennemis (qui s'en vont à la fin de la vague) ne comptent pas comme obstacles.
        static bool IsObstacle(Collider collider) =>
            collider != null && !ArrowIgnore.IsIgnored(collider) && collider.GetComponentInParent<Enemy>() == null;

        static Vector3 Flat(Vector3 vector) => new Vector3(vector.x, 0f, vector.z);

        void OnCardClicked(ShopCard card)
        {
            if (m_Shop != null)
                Report(m_Shop.TryBuy(card.Offer), m_BuyClip);
        }

        void OnRerollClicked()
        {
            if (m_Shop != null)
                Report(m_Shop.TryReroll(), m_RerollClip);
        }

        void Report(ShopResult result, AudioClip successClip)
        {
            Sfx.Play(result.Success ? successClip : m_ErrorClip, transform.position, 0.9f);
            ShowMessage(result.Message, result.Color);
        }

        void ShowMessage(string message, Color color)
        {
            if (m_MessageText == null)
                return;

            m_MessageText.text = message;
            m_MessageText.color = color;
            m_MessageTimer = string.IsNullOrEmpty(message) ? 0f : m_MessageDuration;
        }

        // « Améliorations : Dégâts ×2 · Multitir »
        string OwnedSummary()
        {
            if (m_Upgrades == null || m_Upgrades.Owned.Count == 0)
                return "";

            m_Builder.Clear();
            m_Builder.Append("Améliorations : ");
            for (var i = 0; i < m_Upgrades.Owned.Count; i++)
            {
                var upgrade = m_Upgrades.Owned[i];
                if (i > 0)
                    m_Builder.Append(" · ");
                m_Builder.Append(upgrade.displayName);
                var stacks = m_Upgrades.Stacks(upgrade.effect);
                if (stacks > 1)
                    m_Builder.Append(" ×").Append(stacks);
            }

            return m_Builder.ToString();
        }
    }
}
