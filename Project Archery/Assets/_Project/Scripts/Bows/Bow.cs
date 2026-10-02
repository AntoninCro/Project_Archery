using System;
using Archery.Core;
using Archery.Player;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Bows
{
    /// <summary>
    /// L'arc. Il se tient comme un objet XRI classique, encoche la flèche tenue par l'autre main,
    /// gère la tension de la corde, l'anneau de timing et le tir (GDD, section 4).
    /// </summary>
    /// <remarks>
    /// Le calcul de la visée se fait dans <see cref="ProcessInteractable"/>, juste après que XRI a
    /// déplacé l'arc, y compris dans la phase OnBeforeRender pour éviter tout décalage visuel.
    /// </remarks>
    [DisallowMultipleComponent]
    public class Bow : XRGrabInteractable
    {
        public enum AimMode
        {
            /// <summary>La flèche va de la main qui tend la corde vers le repose-flèche, comme un vrai arc.</summary>
            HandToRest,

            /// <summary>La flèche suit l'orientation de la main qui tient l'arc.</summary>
            BowForward,
        }

        /// <summary>Déclenché à chaque tir (score, statistiques, interface…).</summary>
        public static event Action<Bow, ShotInfo> ShotFired;

        [Header("Arc")]
        [SerializeField]
        BowDefinition m_Definition;

        [SerializeField]
        ShotTuning m_ShotTuning;

        [Header("Pièces")]
        [Tooltip("Partie visuelle qui pivote pour s'aligner sur la flèche pendant la visée.")]
        [SerializeField]
        Transform m_Model;

        [SerializeField]
        Transform m_ArrowRest;

        [SerializeField]
        Transform m_StringTop;

        [SerializeField]
        Transform m_StringBottom;

        [Tooltip("Corde (3 points), dans le repère local du modèle.")]
        [SerializeField]
        LineRenderer m_String;

        [SerializeField]
        Transform m_UpperLimb;

        [SerializeField]
        Transform m_LowerLimb;

        [SerializeField]
        TimingRing m_TimingRing;

        [Tooltip("Position de l'anneau dans le repère du modèle (à côté de la poignée). X est placé côté extérieur de l'arc.")]
        [SerializeField]
        Vector3 m_RingOffset = new Vector3(0.085f, 0f, 0f);

        [Header("Tir")]
        [SerializeField]
        AimMode m_AimMode = AimMode.HandToRest;

        [Tooltip("Lissage de la visée (0 = aucun). À augmenter si la visée tremble.")]
        [Range(0f, 40f)]
        [SerializeField]
        float m_AimSmoothing;

        [Tooltip("Distance (m) entre l'encoche de la flèche et la corde pour encocher.")]
        [SerializeField]
        float m_NockRadius = 0.12f;

        [Tooltip("Marge (m) au-delà de la tension maximale avant que la flèche se décroche.")]
        [SerializeField]
        float m_UnnockSlack = 0.3f;

        [Tooltip("Angle maximal (°) entre l'arc et la corde tirée avant que la flèche se décroche.")]
        [SerializeField]
        float m_MaxPullAngle = 75f;

        [Range(0.5f, 1f)]
        [SerializeField]
        float m_FullDrawThreshold = 0.95f;

        [Tooltip("Si la tension redescend sous ce seuil, l'anneau de timing s'annule.")]
        [Range(0.3f, 1f)]
        [SerializeField]
        float m_RingCancelThreshold = 0.85f;

        [SerializeField]
        float m_LimbFlexAngle = 7f;

        [Header("Retour de l'arc")]
        [Tooltip("Délai (s) avant que l'arc lâché revienne près du joueur.")]
        [SerializeField]
        float m_ReturnDelay = 1.5f;

        [SerializeField]
        float m_ReturnSpeed = 6f;

        [Header("Sons")]
        [Tooltip("Grincement en boucle pendant la tension.")]
        [SerializeField]
        AudioSource m_CreakSource;

        [SerializeField]
        AudioClip m_ReleaseClip;

        [SerializeField]
        AudioClip m_NockClip;

        [SerializeField]
        AudioClip m_GoldClip;

        [SerializeField]
        AudioClip m_RingLoopClip;

        [SerializeField]
        AudioClip m_PerfectClip;

        BowDefinition m_FallbackDefinition;
        Rigidbody m_Body;
        Arrow m_Nocked;
        IXRSelectInteractor m_BowHand;
        float m_DrawRatio;
        float m_DrawDistance;
        float m_LastTickRatio;
        Vector3 m_AimDirection = Vector3.forward;
        Quaternion m_ModelRotation = Quaternion.identity;
        Vector3 m_RestLocal;
        float m_BraceHeight = 0.15f;
        Quaternion m_UpperLimbRest = Quaternion.identity;
        Quaternion m_LowerLimbRest = Quaternion.identity;
        float m_StringVibration;
        float m_TimeSinceRelease;
        bool m_Holstered;
        Collider[] m_OwnColliders = Array.Empty<Collider>();

        public BowDefinition Definition
        {
            get => m_Definition;
            set => m_Definition = value;
        }

        public float DrawRatio => m_DrawRatio;
        public bool HasArrow => m_Nocked != null;

        // Multiplicateurs donnés par les améliorations, les coffres et la difficulté.
        public float SpeedMultiplier { get; set; } = 1f;
        public float DamageMultiplier { get; set; } = 1f;
        public float RingSpeedMultiplier { get; set; } = 1f;
        public float BandWidthMultiplier { get; set; } = 1f;
        public float GoldWidthMultiplier { get; set; } = 1f;
        public int BonusPierce { get; set; }

        BowDefinition Def
        {
            get
            {
                if (m_Definition != null)
                    return m_Definition;
                if (m_FallbackDefinition == null)
                    m_FallbackDefinition = ScriptableObject.CreateInstance<BowDefinition>();
                return m_FallbackDefinition;
            }
        }

        ShotTuning Tuning => m_ShotTuning != null ? m_ShotTuning : ShotTuning.Fallback;
        float MaxDraw => Mathf.Max(0.05f, Def.maxDrawDistance);

        protected override void Awake()
        {
            base.Awake();
            m_Body = GetComponent<Rigidbody>();
            m_OwnColliders = GetComponentsInChildren<Collider>(true);
            ArrowIgnore.Register(m_OwnColliders);

            if (m_UpperLimb != null)
                m_UpperLimbRest = m_UpperLimb.localRotation;
            if (m_LowerLimb != null)
                m_LowerLimbRest = m_LowerLimb.localRotation;

            CacheGeometry();

            if (m_String != null)
            {
                m_String.useWorldSpace = false;
                m_String.positionCount = 3;
            }

            if (m_TimingRing != null)
            {
                m_TimingRing.GoldEntered += OnGoldEntered;
                m_TimingRing.Looped += OnRingLooped;
            }

            // Au lancement, l'arc vient directement se placer près du joueur.
            m_TimeSinceRelease = m_ReturnDelay;
        }

        protected override void OnDestroy()
        {
            ArrowIgnore.Unregister(m_OwnColliders);
            if (m_TimingRing != null)
            {
                m_TimingRing.GoldEntered -= OnGoldEntered;
                m_TimingRing.Looped -= OnRingLooped;
            }

            base.OnDestroy();
        }

        // Une seule main tient l'arc : l'autre ne peut pas le lui « voler » en approchant pour encocher.
        public override bool IsSelectableBy(IXRSelectInteractor interactor) =>
            base.IsSelectableBy(interactor) && (!isSelected || IsSelected(interactor));

        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            base.OnSelectEntered(args);
            m_BowHand = args.interactorObject;
            m_Holstered = false;
            if (PlayerRig.Instance != null && m_BowHand is XRBaseInteractor interactor)
                PlayerRig.Instance.BowHand = interactor.handedness;
        }

        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            base.OnSelectExited(args);
            if (isSelected)
                return;

            Unnock();
            m_BowHand = null;
            m_TimeSinceRelease = 0f;
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);

            switch (updatePhase)
            {
                case XRInteractionUpdateOrder.UpdatePhase.Dynamic:
                    if (isSelected)
                    {
                        TryNock();
                        UpdateDraw(Time.deltaTime, true);
                    }
                    else
                    {
                        UpdateReturn(Time.deltaTime);
                    }

                    UpdateVisuals(Time.deltaTime);
                    break;

                case XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender:
                    // Recalcule la pose avec le suivi le plus récent, sans effet de jeu.
                    if (isSelected)
                        UpdateDraw(0f, false);
                    UpdateVisuals(0f);
                    break;
            }
        }

        /// <summary>
        /// Appelé par la flèche encochée quand la main la lâche : c'est le tir.
        /// </summary>
        internal void OnNockedArrowReleased(Arrow arrow, IXRSelectInteractor drawHand, bool canFire)
        {
            if (arrow != m_Nocked)
                return;

            m_Nocked = null;
            var ratio = m_DrawRatio;
            m_DrawRatio = 0f;
            m_DrawDistance = 0f;
            StopCreak();

            var grade = m_TimingRing != null && m_TimingRing.IsRunning ? m_TimingRing.Release() : ShotGrade.None;
            if (!canFire || ratio < Def.minDrawToFire)
            {
                if (m_TimingRing != null)
                    m_TimingRing.Cancel();
                arrow.Drop(Vector3.zero);
                return;
            }

            var modifiers = Tuning.Get(grade);
            var power = Def.drawToPower.Evaluate(ratio);
            var speed = Def.arrowSpeed * power * modifiers.speed * SpeedMultiplier;
            var damage = Def.damage * power * modifiers.damage * DamageMultiplier;
            var origin = arrow.transform.position;
            var trailColor = grade == ShotGrade.None ? new Color(1f, 1f, 1f, 0.5f) : modifiers.color;

            arrow.Launch(new ArrowLaunch
            {
                Velocity = m_AimDirection * speed,
                Damage = damage,
                Grade = grade,
                Pierce = Def.pierceCount + BonusPierce,
                TrailColor = trailColor,
                IsShot = true,
            });

            if (m_TimingRing != null && grade != ShotGrade.None)
                m_TimingRing.ShowResult(modifiers.color);

            m_StringVibration = 1f;
            Haptics.Pulse(drawHand, 0.7f, 0.06f);
            Haptics.Pulse(m_BowHand, 0.45f, 0.08f);

            var soundPosition = m_ArrowRest != null ? m_ArrowRest.position : transform.position;
            Sfx.Play(m_ReleaseClip, soundPosition, 1f, UnityEngine.Random.Range(0.95f, 1.05f));
            if (grade == ShotGrade.Perfect)
                Sfx.Play(m_PerfectClip, soundPosition, 0.8f);

            ShotFired?.Invoke(this, new ShotInfo
            {
                Grade = grade,
                DrawRatio = ratio,
                Speed = speed,
                Damage = damage,
                Origin = origin,
                Direction = m_AimDirection,
            });
        }

        void CacheGeometry()
        {
            if (m_Model == null || m_ArrowRest == null || m_StringTop == null || m_StringBottom == null)
            {
                Debug.LogError("Bow : il manque des pièces (modèle, repose-flèche ou extrémités de la corde).", this);
                return;
            }

            m_RestLocal = m_Model.InverseTransformPoint(m_ArrowRest.position);
            var stringAtRest = StringPointAtRestHeight();
            m_BraceHeight = Mathf.Max(0.05f, m_RestLocal.z - stringAtRest.z);
        }

        // Point de la corde au repos, à la hauteur du repose-flèche (repère du modèle).
        Vector3 StringPointAtRestHeight()
        {
            var top = m_Model.InverseTransformPoint(m_StringTop.position);
            var bottom = m_Model.InverseTransformPoint(m_StringBottom.position);
            var t = Mathf.InverseLerp(bottom.y, top.y, m_RestLocal.y);
            var point = Vector3.Lerp(bottom, top, t);
            point.x = m_RestLocal.x;
            point.y = m_RestLocal.y;
            return point;
        }

        Vector3 NockLocal(float draw) => new Vector3(m_RestLocal.x, m_RestLocal.y, m_RestLocal.z - m_BraceHeight - draw);

        void TryNock()
        {
            if (m_Nocked != null || m_Model == null)
                return;

            var nockPoint = m_Model.TransformPoint(NockLocal(0f));
            var bestSqrDistance = m_NockRadius * m_NockRadius;
            Arrow best = null;
            var heldArrows = Arrow.HeldArrows;
            for (var i = 0; i < heldArrows.Count; i++)
            {
                var arrow = heldArrows[i];
                if (arrow == null || arrow.CurrentState != Arrow.State.Held || arrow.Hand == null || arrow.Hand == m_BowHand)
                    continue;

                var sqrDistance = (arrow.NockPosition - nockPoint).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    best = arrow;
                }
            }

            if (best == null)
                return;

            m_Nocked = best;
            best.OnNocked(this);
            m_DrawRatio = 0f;
            m_DrawDistance = 0f;
            m_LastTickRatio = 0f;
            Haptics.Pulse(best.Hand, 0.35f, 0.05f);
            Haptics.Pulse(m_BowHand, 0.2f, 0.04f);
            Sfx.Play(m_NockClip, nockPoint, 0.8f);
        }

        void Unnock()
        {
            if (m_Nocked == null)
                return;

            var arrow = m_Nocked;
            m_Nocked = null;
            m_DrawRatio = 0f;
            m_DrawDistance = 0f;
            StopCreak();
            if (m_TimingRing != null)
                m_TimingRing.Cancel();
            arrow.OnUnnocked();
        }

        void UpdateDraw(float deltaTime, bool gameplay)
        {
            if (m_Nocked == null || m_Model == null)
                return;

            var hand = m_Nocked.Hand;
            if (hand == null)
            {
                if (gameplay)
                    Unnock();
                return;
            }

            var root = transform;
            var handPosition = hand.GetAttachTransform(m_Nocked).position;

            // Le modèle pivote autour de la poignée : on estime le repose-flèche avec l'orientation de la main.
            var restPosition = root.TransformPoint(m_Model.localPosition + m_RestLocal);
            var toRest = restPosition - handPosition;
            var distance = toRest.magnitude;

            if (gameplay && ShouldUnnock(root, toRest, distance))
            {
                Unnock();
                return;
            }

            Vector3 direction;
            float draw;
            if (m_AimMode == AimMode.HandToRest && distance > 0.05f)
            {
                direction = toRest / distance;
                draw = distance - m_BraceHeight;
            }
            else
            {
                direction = root.forward;
                draw = Vector3.Dot(direction, toRest) - m_BraceHeight;
            }

            draw = Mathf.Clamp(draw, 0f, MaxDraw);

            // Le modèle s'aligne sur la flèche ; le haut de l'arc reste du côté du haut de la main.
            var targetRotation = Quaternion.Inverse(root.rotation) * Quaternion.LookRotation(direction, root.up);
            if (m_AimSmoothing <= 0f)
                m_ModelRotation = targetRotation;
            else if (deltaTime > 0f)
                m_ModelRotation = Quaternion.Slerp(m_ModelRotation, targetRotation, 1f - Mathf.Exp(-m_AimSmoothing * deltaTime));

            m_Model.localRotation = m_ModelRotation;
            m_DrawDistance = draw;
            m_Nocked.SetNockedPose(m_Model.TransformPoint(NockLocal(draw)), m_Model.rotation);
            m_AimDirection = m_Model.forward;

            if (!gameplay)
                return;

            var ratio = draw / MaxDraw;
            var drawSpeed = deltaTime > 0f ? Mathf.Abs(ratio - m_DrawRatio) / deltaTime : 0f;
            m_DrawRatio = ratio;

            // Petits crans de vibration pendant la tension.
            if (Mathf.Abs(ratio - m_LastTickRatio) >= 0.08f)
            {
                Haptics.Pulse(hand, 0.06f + 0.3f * ratio, 0.02f);
                m_LastTickRatio = ratio;
            }

            UpdateCreak(ratio, drawSpeed, deltaTime);

            if (m_TimingRing != null)
            {
                if (!m_TimingRing.IsRunning && ratio >= m_FullDrawThreshold)
                    StartRing();
                else if (m_TimingRing.IsRunning && ratio < m_RingCancelThreshold)
                    m_TimingRing.Cancel();
            }
        }

        bool ShouldUnnock(Transform root, Vector3 toRest, float distance)
        {
            if (distance > m_BraceHeight + MaxDraw + m_UnnockSlack)
                return true;

            // Main passée devant l'arc.
            if (Vector3.Dot(root.forward, -toRest) > 0.08f)
                return true;

            // Corde tirée trop sur le côté.
            return distance > m_BraceHeight && Vector3.Angle(root.forward, toRest) > m_MaxPullAngle;
        }

        void StartRing()
        {
            var def = Def;
            var widthScale = Mathf.Max(0.1f, BandWidthMultiplier);
            m_TimingRing.Begin(
                def.ringDuration / Mathf.Max(0.1f, RingSpeedMultiplier),
                def.goldHalfWidth * widthScale * Mathf.Max(0.1f, GoldWidthMultiplier),
                def.greenWidth * widthScale,
                def.orangeWidth * widthScale);
        }

        void UpdateReturn(float deltaTime)
        {
            var rig = PlayerRig.Instance;
            if (rig == null)
                return;

            m_TimeSinceRelease += deltaTime;
            if (!m_Holstered && m_TimeSinceRelease < m_ReturnDelay)
                return;

            if (!rig.TryGetBowHolsterPose(out var target))
                return;

            if (m_Body != null && !m_Body.isKinematic)
                m_Body.isKinematic = true;

            if (m_Holstered)
            {
                transform.SetPositionAndRotation(target.position, target.rotation);
                return;
            }

            var blend = 1f - Mathf.Exp(-m_ReturnSpeed * deltaTime);
            transform.SetPositionAndRotation(
                Vector3.Lerp(transform.position, target.position, blend),
                Quaternion.Slerp(transform.rotation, target.rotation, blend));

            if ((transform.position - target.position).sqrMagnitude < 0.0004f)
                m_Holstered = true;
        }

        void UpdateVisuals(float deltaTime)
        {
            if (m_Model == null)
                return;

            if (deltaTime > 0f)
            {
                if (m_Nocked == null)
                {
                    m_ModelRotation = Quaternion.Slerp(m_ModelRotation, Quaternion.identity, 1f - Mathf.Exp(-12f * deltaTime));
                    m_Model.localRotation = m_ModelRotation;
                }

                m_StringVibration = Mathf.MoveTowards(m_StringVibration, 0f, deltaTime * 4f);
            }

            // Les branches plient avec la tension.
            var flex = m_Nocked != null ? m_LimbFlexAngle * m_DrawDistance / MaxDraw : 0f;
            if (m_UpperLimb != null)
                m_UpperLimb.localRotation = m_UpperLimbRest * Quaternion.Euler(-flex, 0f, 0f);
            if (m_LowerLimb != null)
                m_LowerLimb.localRotation = m_LowerLimbRest * Quaternion.Euler(flex, 0f, 0f);

            if (m_String != null && m_StringTop != null && m_StringBottom != null)
            {
                Vector3 middle;
                if (m_Nocked != null)
                {
                    middle = NockLocal(m_DrawDistance);
                }
                else
                {
                    middle = StringPointAtRestHeight();
                    middle.z += Mathf.Sin(Time.time * 95f) * 0.015f * m_StringVibration;
                }

                m_String.SetPosition(0, m_Model.InverseTransformPoint(m_StringTop.position));
                m_String.SetPosition(1, middle);
                m_String.SetPosition(2, m_Model.InverseTransformPoint(m_StringBottom.position));
            }

            if (m_TimingRing != null)
            {
                var offset = m_RingOffset;
                offset.x = Mathf.Abs(offset.x) * OutsideSign();
                m_TimingRing.transform.position = m_Model.TransformPoint(offset);
            }
        }

        // Côté extérieur de l'arc : à gauche pour un arc tenu de la main gauche, à droite sinon.
        float OutsideSign() =>
            m_BowHand is XRBaseInteractor interactor && interactor.handedness == InteractorHandedness.Right ? 1f : -1f;

        void UpdateCreak(float ratio, float drawSpeed, float deltaTime)
        {
            if (m_CreakSource == null || m_CreakSource.clip == null)
                return;

            if (!m_CreakSource.isPlaying)
            {
                m_CreakSource.volume = 0f;
                m_CreakSource.Play();
            }

            var targetVolume = Mathf.Clamp01(drawSpeed * 1.2f) * 0.8f;
            m_CreakSource.volume = Mathf.MoveTowards(m_CreakSource.volume, targetVolume, deltaTime * 6f);
            m_CreakSource.pitch = Mathf.Lerp(0.85f, 1.2f, ratio);
        }

        void StopCreak()
        {
            if (m_CreakSource != null)
                m_CreakSource.Stop();
        }

        void OnGoldEntered()
        {
            if (m_Nocked != null)
                Haptics.Pulse(m_Nocked.Hand, 0.6f, 0.05f);
            Haptics.Pulse(m_BowHand, 0.35f, 0.05f);
            Sfx.Play(m_GoldClip, m_TimingRing.transform.position, 0.5f);
        }

        void OnRingLooped()
        {
            if (m_Nocked != null)
                Haptics.Pulse(m_Nocked.Hand, 0.12f, 0.02f);
            Sfx.Play(m_RingLoopClip, m_TimingRing.transform.position, 0.35f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => ShotFired = null;
    }
}
