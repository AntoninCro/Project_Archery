using Archery.Core;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Soufflerie (refonte du tower defense, étape 5, lot 3 ; 28 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, parties 6.1 et
    /// 6.2) : au bord de la voie, tournée vers elle. Par bouffées, un cône de vent repousse vers la forêt les ennemis qui s'y
    /// trouvent (au sol, ils glissent sur la voie ; les volants sont déviés), puis elle reprend son souffle. Plus loin, plus fort
    /// et plus longtemps à chaque niveau. Commune (proposée).
    /// </summary>
    /// <remarks>
    /// Le vent passe par <c>Enemy.Push</c> (le coup de poing de la main de la matrice) : l'ennemi garde le NavMesh. Les boss ne
    /// sont poussés que moitié moins. Les pales (<c>Rotor</c>) tournent vite pendant une bouffée, lentement sinon.
    /// </remarks>
    public class StructureBlower : Structure
    {
        [Tooltip("Portée (m) du cône de vent, par niveau.")]
        [SerializeField]
        float[] m_RangePerLevel = { 9f, 10.5f, 12f };

        [Tooltip("Demi-angle (°) du cône de vent.")]
        [SerializeField]
        float m_HalfAngle = 30f;

        [Tooltip("Vitesse (m/s) à laquelle le vent repousse un ennemi tout près, par niveau (moitié au bout du cône).")]
        [SerializeField]
        float[] m_WindSpeedPerLevel = { 3f, 3.5f, 4f };

        [Tooltip("Durée (s) d'une bouffée, par niveau.")]
        [SerializeField]
        float[] m_BlowTimePerLevel = { 2.5f, 3f, 3.5f };

        [Tooltip("Pause (s) entre deux bouffées.")]
        [SerializeField]
        float m_RestTime = 2.5f;

        [Tooltip("Pales : tournent autour de leur axe Z.")]
        [SerializeField]
        Transform m_Rotor;

        [SerializeField]
        AudioClip m_GustClip;

        float m_Timer = 1f;
        bool m_Blowing;
        float m_Spin;

        protected override void Update()
        {
            base.Update();
            if (!IsBuilt)
                return;

            var deltaTime = Time.deltaTime;
            var level = Mathf.Max(1, Level);
            m_Timer -= deltaTime * OverchargeRate;
            if (m_Timer <= 0f)
            {
                m_Blowing = !m_Blowing;
                m_Timer = m_Blowing ? ValueFor(m_BlowTimePerLevel, level, 2.5f) : m_RestTime;
                if (m_Blowing)
                    Sfx.Play(m_GustClip, transform.position + Vector3.up * 1.5f, 0.8f, Random.Range(0.9f, 1.1f));
            }

            m_Spin = Mathf.MoveTowards(m_Spin, m_Blowing ? 900f : 60f, deltaTime * 900f);
            if (m_Rotor != null)
                m_Rotor.Rotate(0f, 0f, m_Spin * deltaTime, Space.Self);

            if (m_Blowing)
                Blow(level);
        }

        void Blow(int level)
        {
            var range = ValueFor(m_RangePerLevel, level, 9f) * RangeScale * OverchargeRange;
            var wind = ValueFor(m_WindSpeedPerLevel, level, 3f);
            var origin = transform.position;
            var forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive || enemy.IsAirborne)
                    continue;

                var offset = enemy.transform.position - origin;
                if (offset.y < -4f || offset.y > 10f)
                    continue;
                offset.y = 0f;
                var distance = offset.magnitude;
                if (distance > range || (distance > 0.5f && Vector3.Angle(forward, offset) > m_HalfAngle))
                    continue;

                var strength = wind * Mathf.Lerp(1f, 0.5f, distance / range) * (enemy.IsBoss ? 0.5f : 1f);
                enemy.Push(TowardForest(enemy) * strength);
            }
        }

        void OnDrawGizmosSelected()
        {
            var range = ValueFor(m_RangePerLevel, Mathf.Max(1, Level), 9f);
            Gizmos.color = new Color(0.8f, 0.95f, 1f, 0.6f);
            var origin = transform.position + Vector3.up * 0.5f;
            Gizmos.DrawLine(origin, origin + Quaternion.Euler(0f, m_HalfAngle, 0f) * transform.forward * range);
            Gizmos.DrawLine(origin, origin + Quaternion.Euler(0f, -m_HalfAngle, 0f) * transform.forward * range);
            Gizmos.DrawLine(origin, origin + transform.forward * range);
        }
    }
}
