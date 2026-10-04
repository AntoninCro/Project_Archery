using Archery.Core;
using Archery.Player;
using UnityEngine;

namespace Archery.Defense
{
    /// <summary>
    /// Téléporteur de la tour (GDD, section 9) : rester un instant debout sur le cercle emmène au point d'arrivée,
    /// avec un fondu au noir. Un cercle au pied de la tour mène au sommet ; un autre, au sommet, ramène en bas.
    /// </summary>
    /// <remarks>
    /// Tant que la tour est détruite, les cercles s'éteignent. Quand elle s'effondre, le cercle du pied de la tour
    /// fait descendre le joueur qui était en haut (« Evacuate On Collapse »).
    /// Après une téléportation, il faut sortir du cercle avant de pouvoir repartir.
    /// </remarks>
    [DisallowMultipleComponent]
    public class TowerTeleporter : MonoBehaviour
    {
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("Point d'arrivée, aux pieds du joueur. Son axe Z (flèche bleue) donne la direction du regard à l'arrivée.")]
        [SerializeField]
        Transform m_Destination;

        [Tooltip("Tourne le regard du joueur dans la direction du point d'arrivée.")]
        [SerializeField]
        bool m_FaceDestinationForward = true;

        [Tooltip("Rayon (m) du cercle sur lequel se tenir.")]
        [SerializeField]
        float m_Radius = 0.75f;

        [Tooltip("Temps (s) à rester sur le cercle avant de partir.")]
        [SerializeField]
        float m_HoldTime = 1f;

        [Tooltip("Le cercle ne marche plus tant que la tour est détruite.")]
        [SerializeField]
        bool m_DisabledWhileTowerDestroyed = true;

        [Tooltip("Pour le cercle du pied de la tour : quand la tour s'effondre, le joueur qui est en haut est envoyé au sol, " +
                 "à côté de ce cercle (ou au point « Evacuation Point »).")]
        [SerializeField]
        bool m_EvacuateOnCollapse = true;

        [Tooltip("Optionnel : point d'arrivée au sol quand la tour s'effondre. Vide : 1,6 m devant ce cercle (son axe Z).")]
        [SerializeField]
        Transform m_EvacuationPoint;

        [Header("Rendu")]
        [Tooltip("Optionnel : rendu du cercle (matériau avec _BaseColor), coloré selon l'état.")]
        [SerializeField]
        Renderer m_Visual;

        [SerializeField]
        Color m_IdleColor = new Color(0.25f, 0.6f, 1f);

        [Tooltip("Couleur atteinte à la fin de la charge.")]
        [SerializeField]
        Color m_ChargedColor = new Color(0.75f, 1f, 1f);

        [SerializeField]
        Color m_DisabledColor = new Color(0.25f, 0.25f, 0.28f);

        [Tooltip("Joué quand le joueur met le pied sur le cercle.")]
        [SerializeField]
        AudioClip m_ChargeClip;

        MaterialPropertyBlock m_PropertyBlock;
        Tower m_Tower;
        float m_Charge;
        bool m_Armed = true;
        bool m_WasCharging;

        void Awake()
        {
            m_PropertyBlock = new MaterialPropertyBlock();
            if (m_Destination == null)
                Debug.LogError("TowerTeleporter : aucun point d'arrivée (Destination).", this);
        }

        void OnEnable() => PlayerTeleport.Teleported += OnPlayerTeleported;

        void OnDisable() => PlayerTeleport.Teleported -= OnPlayerTeleported;

        void Start()
        {
            m_Tower = Tower.Instance;
            if (m_Tower != null)
                m_Tower.Destroyed += OnTowerDestroyed;
        }

        void OnDestroy()
        {
            if (m_Tower != null)
                m_Tower.Destroyed -= OnTowerDestroyed;
        }

        bool IsUsable => m_Destination != null && !(m_DisabledWhileTowerDestroyed && m_Tower != null && !m_Tower.IsStanding);

        void Update()
        {
            var rig = PlayerRig.Instance;
            var teleport = PlayerTeleport.Instance;
            var onPad = rig != null && IsOnPad(rig.BodyPosition);
            if (!onPad)
                m_Armed = true;

            var charging = IsUsable && onPad && m_Armed && teleport != null && !teleport.IsTeleporting;
            if (charging && !m_WasCharging)
                Sfx.Play(m_ChargeClip, transform.position + Vector3.up, 0.7f);
            m_WasCharging = charging;

            m_Charge = charging ? m_Charge + Time.deltaTime / Mathf.Max(0.05f, m_HoldTime) : 0f;
            UpdateVisual();

            if (m_Charge >= 1f)
            {
                m_Charge = 0f;
                Go(teleport);
            }
        }

        bool IsOnPad(Vector3 feet)
        {
            var offset = feet - transform.position;
            if (Mathf.Abs(offset.y) > 1f)
                return false;

            offset.y = 0f;
            return offset.sqrMagnitude <= m_Radius * m_Radius;
        }

        void Go(PlayerTeleport teleport)
        {
            if (teleport == null || m_Destination == null)
                return;

            float? yaw = m_FaceDestinationForward ? m_Destination.eulerAngles.y : (float?)null;
            teleport.TryTeleport(m_Destination.position, yaw);
        }

        // Après n'importe quelle téléportation, il faut sortir du cercle avant de repartir.
        void OnPlayerTeleported()
        {
            m_Armed = false;
            m_Charge = 0f;
        }

        // La tour s'effondre : le joueur qui était en haut descend au sol.
        void OnTowerDestroyed()
        {
            var rig = PlayerRig.Instance;
            var teleport = PlayerTeleport.Instance;
            if (!m_EvacuateOnCollapse || rig == null || teleport == null || m_Tower == null)
                return;
            if (rig.BodyPosition.y < m_Tower.TopHeight - 1.5f)
                return;

            var point = m_EvacuationPoint != null ? m_EvacuationPoint : transform;
            var position = m_EvacuationPoint != null ? point.position : transform.position + transform.forward * 1.6f;
            teleport.TryTeleport(position, point.eulerAngles.y);
        }

        void UpdateVisual()
        {
            if (m_Visual == null)
                return;

            Color color;
            if (!IsUsable)
                color = m_DisabledColor;
            else if (m_Charge > 0f)
                color = Color.Lerp(m_IdleColor, m_ChargedColor, m_Charge) * (0.85f + 0.15f * Mathf.Sin(Time.time * 20f));
            else
                color = m_IdleColor * (0.8f + 0.2f * Mathf.Sin(Time.time * 2f));

            color.a = 1f;
            m_Visual.GetPropertyBlock(m_PropertyBlock);
            m_PropertyBlock.SetColor(k_BaseColorId, color);
            m_Visual.SetPropertyBlock(m_PropertyBlock);
        }
    }
}
