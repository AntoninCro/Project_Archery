using System;
using System.Collections;
using Archery.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace Archery.Player
{
    /// <summary>
    /// Téléporte le joueur avec un fondu au noir (téléporteur de la tour…).
    /// Le déplacement passe par le Teleportation Provider d'XRI, qui gère le CharacterController et la gravité ;
    /// sans lui, le XR Origin est déplacé directement.
    /// </summary>
    /// <remarks>À placer sur le XR Origin, avec le <see cref="PlayerRig"/>. Le fondu utilise le <see cref="ScreenFader"/>.</remarks>
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
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
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
