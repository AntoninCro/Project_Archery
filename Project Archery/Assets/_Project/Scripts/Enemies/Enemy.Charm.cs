using UnityEngine;

namespace Archery.Enemies
{
    /// <summary>
    /// Charmé (le duo Chant des sirènes, 31 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, 12.17) : l'ennemi change de camp un
    /// moment. Il n'attaque plus la tour ni le joueur : il court là où le bâtiment qui l'a charmé l'envoie (sur un autre ennemi,
    /// ou vers la forêt) ; c'est ce bâtiment qui fait les coups.
    /// </summary>
    public partial class Enemy
    {
        float m_CharmTimer;
        Vector3 m_CharmPoint;
        float m_CharmRepath;

        /// <summary>Charmé : il court vers le point que lui donne le bâtiment qui l'a charmé.</summary>
        public bool IsCharmed => m_CharmTimer > 0f && m_State != State.Dead;

        /// <summary>
        /// Le charme pour <paramref name="duration"/> s, vers <paramref name="destination"/>. Seulement un ennemi qui marche sur le
        /// NavMesh, pas un boss. Renvoie vrai s'il est charmé.
        /// </summary>
        public bool Charm(float duration, Vector3 destination)
        {
            if (m_State == State.Dead || IsBoss || m_Agent == null || m_Behaviour != null || duration <= 0f)
                return false;

            m_CharmTimer = Mathf.Max(m_CharmTimer, duration);
            m_CharmPoint = destination;
            m_CharmRepath = 0f;
            m_HitDelay = -1f;
            return true;
        }

        /// <summary>Change le point où court un ennemi charmé.</summary>
        public void SetCharmDestination(Vector3 destination) => m_CharmPoint = destination;

        /// <summary>Le charme s'arrête tout de suite.</summary>
        public void Uncharm() => m_CharmTimer = 0f;

        // Charmé : il court vers son point, un peu plus vite que d'habitude. Vrai tant qu'il l'est (le reste de l'Update attend).
        bool UpdateCharm(float deltaTime)
        {
            if (m_CharmTimer <= 0f)
                return false;

            m_CharmTimer -= deltaTime;
            if (m_CharmTimer <= 0f || m_Agent == null || !m_Agent.enabled || !m_Agent.isOnNavMesh)
                return m_CharmTimer > 0f;

            m_State = State.Moving;
            m_Agent.isStopped = false;
            m_Agent.updateRotation = true;
            m_Agent.speed = CurrentSpeed * 1.25f;
            m_CharmRepath -= deltaTime;
            if (m_CharmRepath <= 0f)
            {
                m_Agent.SetDestination(m_CharmPoint);
                m_CharmRepath = 0.25f;
            }

            return true;
        }
    }
}
