using System;
using System.Collections;
using System.Collections.Generic;
using Archery.Core;
using Archery.Waves;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace Archery.Player
{
    /// <summary>
    /// Téléporte le joueur avec un fondu au noir (téléporteur de la tour…).
    /// Le déplacement passe par le Teleportation Provider d'XRI, qui gère le CharacterController et la gravité ;
    /// sans lui, le XR Origin est déplacé directement.
    /// </summary>
    /// <remarks>
    /// À placer sur le XR Origin, avec le <see cref="PlayerRig"/>. Le fondu utilise le <see cref="ScreenFader"/>.
    /// Au lancement, le joueur apparaissait là où le casque se trouvait dans la pièce, par rapport au centre de la zone de
    /// jeu : parfois dans la tour ou à côté. Depuis le 22 octobre, il est remis au point de départ (là où est le XR
    /// Origin au lancement, tourné comme lui) dès que le casque suit la tête, puis, avant la première vague, chaque fois
    /// qu'on remet le casque, qu'il se recentre ou que son suivi saute.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerRig))]
    public class PlayerTeleport : MonoBehaviour
    {
        [Tooltip("Durée (s) du fondu au noir avant le déplacement.")]
        [SerializeField]
        float m_FadeOutTime = 0.2f;

        [Tooltip("Durée (s) du retour de l'image après le déplacement.")]
        [SerializeField]
        float m_FadeInTime = 0.35f;

        [SerializeField]
        AudioClip m_TeleportClip;

        [Tooltip("Remet le joueur au point de départ quand le casque commence à suivre la tête, puis, avant la première " +
                 "vague, quand on le remet, qu'il se recentre ou que son suivi saute : la place du casque dans la pièce ne " +
                 "compte plus.")]
        [SerializeField]
        bool m_PlaceAtSpawn = true;

        // Un saut de la tête plus grand que ça (m) d'une image à l'autre : le suivi s'est recentré.
        const float k_TrackingJump = 0.5f;

        static readonly List<XRInputSubsystem> s_InputSubsystems = new List<XRInputSubsystem>();

        readonly List<XRInputSubsystem> m_Subscribed = new List<XRInputSubsystem>();
        Vector3 m_SpawnPosition;
        float m_SpawnYaw;
        bool m_Placed;
        bool m_WasPresent;
        bool m_OriginUpdated;
        bool m_HasLastHead;
        Vector3 m_LastHead;

        PlayerRig m_Rig;
        TeleportationProvider m_Provider;
        CharacterController m_Controller;

        public static PlayerTeleport Instance { get; private set; }

        public bool IsTeleporting { get; private set; }

        /// <summary>Le joueur vient d'arriver à destination (pendant que l'image est encore noire).</summary>
        public static event Action Teleported;

        void Awake()
        {
            Instance = this;
            m_Rig = GetComponent<PlayerRig>();
            m_Provider = GetComponentInChildren<TeleportationProvider>(true);
            m_Controller = GetComponentInChildren<CharacterController>(true);

            // Le point de départ : le XR Origin tel qu'il est posé dans la scène (en haut de la tour).
            var origin = m_Rig.Origin != null ? m_Rig.Origin.transform : transform;
            m_SpawnPosition = origin.position;
            m_SpawnYaw = origin.eulerAngles.y;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            foreach (var subsystem in m_Subscribed)
            {
                if (subsystem != null)
                    subsystem.trackingOriginUpdated -= OnTrackingOriginUpdated;
            }

            m_Subscribed.Clear();
        }

        void OnTrackingOriginUpdated(XRInputSubsystem subsystem) => m_OriginUpdated = true;

        void Update()
        {
            if (!m_PlaceAtSpawn)
                return;

            // Sans casque, le jeu au clavier place lui-même la caméra.
            var desktop = DesktopControls.Instance;
            if (desktop != null && desktop.IsActive)
                return;

            var device = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            var head = m_Rig.Head;
            if (!device.isValid || head == null)
                return;

            WatchTrackingOrigin();

            // Sans capteur de présence, le casque compte comme porté.
            var tracked = device.TryGetFeatureValue(CommonUsages.isTracked, out var isTracked) && isTracked;
            var present = !device.TryGetFeatureValue(CommonUsages.userPresence, out var userPresent) || userPresent;
            var local = head.localPosition;
            var jumped = m_HasLastHead && (local - m_LastHead).sqrMagnitude > k_TrackingJump * k_TrackingJump;
            m_LastHead = local;
            m_HasLastHead = tracked;

            var putOn = present && !m_WasPresent;
            m_WasPresent = present;
            var recentered = m_OriginUpdated;
            m_OriginUpdated = false;
            if (!tracked || !present || IsTeleporting)
                return;

            // La première fois dans la scène, toujours ; ensuite, seulement avant la première vague.
            if (m_Placed && !(BeforeFirstWave() && (putOn || recentered || jumped)))
                return;

            if (TryTeleport(m_SpawnPosition, m_SpawnYaw))
                m_Placed = true;
        }

        static bool BeforeFirstWave()
        {
            var waves = WaveManager.Instance;
            return waves == null || waves.Phase == WavePhase.Ready;
        }

        // Le recentrage du casque (bouton Meta maintenu…) : Unity le signale par le sous-système d'entrée XR.
        void WatchTrackingOrigin()
        {
            if (m_Subscribed.Count > 0)
                return;

            SubsystemManager.GetSubsystems(s_InputSubsystems);
            foreach (var subsystem in s_InputSubsystems)
            {
                subsystem.trackingOriginUpdated += OnTrackingOriginUpdated;
                m_Subscribed.Add(subsystem);
            }
        }

        /// <summary>
        /// Envoie les pieds du joueur à <paramref name="feetPosition"/>. Avec <paramref name="yaw"/>, le regard est
        /// tourné dans cette direction (en degrés autour de l'axe vertical). Renvoie faux si une téléportation est déjà en cours.
        /// </summary>
        public bool TryTeleport(Vector3 feetPosition, float? yaw = null)
        {
            if (IsTeleporting || !isActiveAndEnabled)
                return false;

            StartCoroutine(Teleport(feetPosition, yaw));
            return true;
        }

        /// <summary>
        /// Déplace le joueur sans fondu, quand l'image est déjà noire (entrée et sortie de l'arène du boss final) :
        /// c'est l'appelant qui gère le noir. À attendre dans une coroutine.
        /// </summary>
        public IEnumerator TeleportWhileHidden(Vector3 feetPosition, float? yaw = null)
        {
            while (IsTeleporting)
                yield return null;

            IsTeleporting = true;
            Move(feetPosition, yaw);

            // XRI applique le déplacement à l'image suivante.
            yield return null;
            yield return null;

            Teleported?.Invoke();
            IsTeleporting = false;
        }

        IEnumerator Teleport(Vector3 destination, float? yaw)
        {
            IsTeleporting = true;
            var fader = ScreenFader.Instance;
            if (fader != null)
                yield return fader.Fade(1f, m_FadeOutTime);

            Move(destination, yaw);

            // XRI applique le déplacement à l'image suivante.
            yield return null;
            yield return null;

            var head = m_Rig.Head;
            Sfx.Play(m_TeleportClip, head != null ? head.position : destination, 0.8f, 1f, 0f);
            Teleported?.Invoke();

            if (fader != null)
                yield return fader.Fade(0f, m_FadeInTime);
            IsTeleporting = false;
        }

        void Move(Vector3 destination, float? yaw)
        {
            if (m_Provider != null && m_Provider.isActiveAndEnabled)
            {
                var request = new TeleportRequest
                {
                    destinationPosition = destination,
                    destinationRotation = Quaternion.Euler(0f, yaw ?? 0f, 0f),
                    requestTime = Time.time,
                    matchOrientation = yaw.HasValue ? MatchOrientation.TargetUpAndForward : MatchOrientation.None,
                };

                if (m_Provider.QueueTeleportRequest(request))
                    return;
            }

            // Sans XRI : on déplace le XR Origin pour que les pieds arrivent au point voulu.
            var origin = m_Rig.Origin != null ? m_Rig.Origin.transform : transform;
            if (yaw.HasValue && m_Rig.Head != null)
            {
                var turn = Mathf.DeltaAngle(m_Rig.HeadYaw.eulerAngles.y, yaw.Value);
                origin.RotateAround(m_Rig.Head.position, Vector3.up, turn);
            }

            var offset = destination - m_Rig.BodyPosition;
            if (m_Controller != null)
                m_Controller.enabled = false;
            origin.position += offset;
            Physics.SyncTransforms();
            if (m_Controller != null)
                m_Controller.enabled = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Teleported = null;
    }
}
