using UnityEngine;

namespace Archery.Enemies
{
    /// <summary>
    /// Calme un point faible porté par un os qui s'agite (la tête d'un boss qui court) : il suit le corps du boss
    /// (ses déplacements et ses rotations) tels quels, mais ne garde qu'une partie des secousses de l'animation, lissées.
    /// </summary>
    /// <remarks>
    /// Ajouté en jeu par le <see cref="Boss"/> sur chaque zone de touche « WeakPoint ». Il passe avant l'aspect du point
    /// faible (<see cref="Effects.WeakPointVisual"/>), dont le sceau se tourne vers le joueur depuis la bonne place.
    /// </remarks>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public class SteadyWeakPoint : MonoBehaviour
    {
        // Temps de réponse du lissage (s) : les pas de course (une à deux secousses par seconde) sont très atténués,
        // les mouvements lents (se pencher, frapper la tour) sont suivis.
        const float k_SmoothTime = 0.35f;

        Transform m_Body;
        Transform m_Bone;
        Vector3 m_LocalPosition;
        Quaternion m_LocalRotation;
        Vector3 m_Smoothed;
        Quaternion m_SmoothedRotation;
        float m_Steadiness;
        bool m_Started;

        /// <summary>
        /// Calme <paramref name="point"/> (enfant d'un os) par rapport à <paramref name="body"/> : 0 = il suit l'os, 1 = il ne
        /// garde que les mouvements lents.
        /// </summary>
        public static void Attach(Transform point, Transform body, float steadiness)
        {
            if (point == null || body == null || point.parent == null || steadiness <= 0f)
                return;

            if (!point.TryGetComponent<SteadyWeakPoint>(out var steady))
                steady = point.gameObject.AddComponent<SteadyWeakPoint>();
            steady.m_Body = body;
            steady.m_Bone = point.parent;
            steady.m_LocalPosition = point.localPosition;
            steady.m_LocalRotation = point.localRotation;
            steady.m_Steadiness = Mathf.Clamp01(steadiness);
            steady.m_Started = false;
        }

        // Après l'animation de l'image.
        void LateUpdate()
        {
            if (m_Body == null || m_Bone == null)
                return;

            // Là où l'animation le mettrait, dans le repère du corps.
            var target = m_Body.InverseTransformPoint(m_Bone.TransformPoint(m_LocalPosition));
            var targetRotation = Quaternion.Inverse(m_Body.rotation) * (m_Bone.rotation * m_LocalRotation);
            if (!m_Started)
            {
                m_Smoothed = target;
                m_SmoothedRotation = targetRotation;
                m_Started = true;
            }
            else
            {
                var follow = 1f - Mathf.Exp(-Time.deltaTime / k_SmoothTime);
                m_Smoothed = Vector3.Lerp(m_Smoothed, target, follow);
                m_SmoothedRotation = Quaternion.Slerp(m_SmoothedRotation, targetRotation, follow);
            }

            transform.SetPositionAndRotation(m_Body.TransformPoint(Vector3.Lerp(target, m_Smoothed, m_Steadiness)),
                                             m_Body.rotation * Quaternion.Slerp(targetRotation, m_SmoothedRotation, m_Steadiness));
        }
    }
}
