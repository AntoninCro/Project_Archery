using System;
using Archery.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Random = UnityEngine.Random;

namespace Archery.Cyber
{
    /// <summary>
    /// Un éclat de miroir glitché, caché derrière une cascade (le passage vers le toit cyberpunk, <see cref="CyberPassage"/>) :
    /// il flotte, tourne, tressaute par à-coups et grésille de temps en temps. On le prend à la main, ou en le visant avec
    /// l'une ou l'autre gâchette, à quelques mètres au plus (comme un coffre) ; sans casque, avec E.
    /// </summary>
    /// <remarks>Le modèle est le premier enfant (<see cref="m_Visual"/>) ; le collider est sur cet objet.</remarks>
    [DisallowMultipleComponent]
    public class GlitchShard : XRBaseInteractable
    {
        [Tooltip("Distance (m) maximale entre la main (la manette) et l'éclat pour le prendre.")]
        [SerializeField]
        float m_MaxPickDistance = 4f;

        [Tooltip("Le modèle de l'éclat : il flotte et tourne. Vide : le premier enfant.")]
        [SerializeField]
        Transform m_Visual;

        [Tooltip("Matériau des pixels quand on le prend (couleurs des sommets : Materials/Effects).")]
        [SerializeField]
        Material m_PixelMaterial;

        [Tooltip("Grésillements joués de temps en temps (on l'entend avant de le voir).")]
        [SerializeField]
        AudioClip[] m_CrackleClips = new AudioClip[0];

        [SerializeField]
        AudioClip m_PickClip;

        Vector3 m_Home;
        Vector3 m_Jitter;
        float m_Phase;
        float m_CrackleTimer;
        float m_JitterTimer;

        /// <summary>L'éclat a été pris.</summary>
        public bool Taken { get; private set; }

        /// <summary>L'éclat vient d'être pris.</summary>
        public event Action<GlitchShard> Picked;

        protected override void Awake()
        {
            base.Awake();
            if (m_Visual == null && transform.childCount > 0)
                m_Visual = transform.GetChild(0);
            m_Home = m_Visual != null ? m_Visual.localPosition : Vector3.zero;
            m_Phase = Random.value * 10f;
            m_CrackleTimer = Random.Range(1f, 4f);
        }

        public override bool IsSelectableBy(IXRSelectInteractor interactor) =>
            base.IsSelectableBy(interactor) && !Taken && IsInReach(interactor);

        // La main (la manette, pas le point visé) doit être à moins de quelques mètres de l'éclat.
        bool IsInReach(IXRInteractor interactor)
        {
            var hand = interactor.transform.position;
            foreach (var collider in colliders)
            {
                if (collider != null && collider.enabled &&
                    (collider.ClosestPoint(hand) - hand).sqrMagnitude <= m_MaxPickDistance * m_MaxPickDistance)
                    return true;
            }

            return false;
        }

        // La grosse gâchette le prend aussi, comme la gâchette de prise.
        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
                TriggerPick.Process(this);
        }

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            if (Taken)
                return;

            Taken = true;
            Haptics.Pulse(args.interactorObject, 0.7f, 0.15f);
            Sfx.Play(m_PickClip, transform.position, 1f, 1.15f);
            GlitchPixels.Burst(m_Visual != null ? m_Visual.position : transform.position, 60, 0.7f, m_PixelMaterial);
            foreach (var collider in colliders)
            {
                if (collider != null)
                    collider.enabled = false;
            }

            if (m_Visual != null)
                m_Visual.gameObject.SetActive(false);
            Picked?.Invoke(this);
        }

        void Update()
        {
            if (Taken || m_Visual == null)
                return;

            var deltaTime = Time.deltaTime;
            var time = Time.time + m_Phase;

            // Il flotte et tourne ; de temps en temps, une salve de petits sauts.
            m_JitterTimer -= deltaTime;
            if (m_JitterTimer <= 0f)
            {
                var jump = Random.value < 0.35f;
                m_Jitter = jump ? Random.insideUnitSphere * 0.06f : Vector3.zero;
                m_JitterTimer = jump ? Random.Range(0.04f, 0.1f) : Random.Range(0.2f, 0.8f);
            }

            m_Visual.localPosition = m_Home + Vector3.up * (Mathf.Sin(time * 1.3f) * 0.06f) + m_Jitter;
            m_Visual.localRotation = Quaternion.Euler(Mathf.Sin(time * 0.7f) * 12f, time * 40f, 0f);

            m_CrackleTimer -= deltaTime;
            if (m_CrackleTimer <= 0f)
            {
                m_CrackleTimer = Random.Range(2.5f, 5f);
                if (m_CrackleClips != null && m_CrackleClips.Length > 0)
                    Sfx.Play(m_CrackleClips[Random.Range(0, m_CrackleClips.Length)], transform.position, 0.5f, Random.Range(0.9f, 1.2f));
            }
        }
    }
}
