using System;
using Archery.Bows;
using Archery.Core;
using Archery.UI;
using UnityEngine;

namespace Archery.Training
{
    /// <summary>
    /// Cible d'entraînement à anneaux : plus l'impact est proche du centre, plus il rapporte (10 au centre).
    /// </summary>
    [DisallowMultipleComponent]
    public class TrainingTarget : MonoBehaviour, IArrowHitHandler
    {
        [Tooltip("Centre de la face ; son axe Z pointe vers le tireur. Ne pas mettre d'échelle sur cet objet.")]
        [SerializeField]
        Transform m_FaceCenter;

        [SerializeField]
        float m_FaceRadius = 0.6f;

        [SerializeField]
        int m_RingCount = 10;

        [SerializeField]
        AudioClip m_BullseyeClip;

        [SerializeField]
        ShotTuning m_ShotTuning;

        /// <summary>Déclenché à chaque flèche tirée qui touche une cible : (cible, impact, points de l'anneau).</summary>
        public static event Action<TrainingTarget, ArrowHit, int> AnyScored;

        public int LastScore { get; private set; }
        public int TotalScore { get; private set; }
        public int ArrowCount { get; private set; }

        public bool OnArrowHit(in ArrowHit hit)
        {
            if (!hit.IsShot)
                return false;

            var ring = RingAt(hit.Point);
            LastScore = ring;
            TotalScore += ring;
            ArrowCount++;
            AnyScored?.Invoke(this, hit, ring);
            ShowScore(hit, ring);
            return false;
        }

        /// <summary>Points de l'anneau touché (0 en dehors de la face).</summary>
        public int RingAt(Vector3 worldPoint)
        {
            var face = m_FaceCenter != null ? m_FaceCenter : transform;
            var local = face.InverseTransformPoint(worldPoint);
            var radius = new Vector2(local.x, local.y).magnitude / Mathf.Max(0.01f, m_FaceRadius);
            if (radius > 1f)
                return 0;
            return Mathf.Clamp(m_RingCount - Mathf.FloorToInt(radius * m_RingCount), 1, m_RingCount);
        }

        public void ResetScore()
        {
            LastScore = 0;
            TotalScore = 0;
            ArrowCount = 0;
        }

        void ShowScore(in ArrowHit hit, int ring)
        {
            var tuning = m_ShotTuning != null ? m_ShotTuning : ShotTuning.Fallback;
            var grade = tuning.Get(hit.Grade);
            var text = ring > 0 ? ring.ToString() : "Raté";
            if (!string.IsNullOrEmpty(grade.label))
                text += "\n" + grade.label;

            var color = ring >= m_RingCount - 1 ? new Color(1f, 0.85f, 0.2f) : Color.white;
            if (hit.Grade != ShotGrade.None)
                color = Color.Lerp(color, grade.color, 0.5f);

            FloatingText.Spawn(hit.Point - hit.Direction * 0.15f + Vector3.up * 0.15f, text, color);
            if (ring == m_RingCount)
                Sfx.Play(m_BullseyeClip, hit.Point, 0.8f);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => AnyScored = null;
    }
}
