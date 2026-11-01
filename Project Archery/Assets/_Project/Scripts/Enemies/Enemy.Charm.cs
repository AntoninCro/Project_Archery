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
        float m_CharmStop = -1f;

        /// <summary>Charmé : il court vers le point que lui donne le bâtiment qui l'a charmé.</summary>
        public bool IsCharmed => m_CharmTimer > 0f && m_State != State.Dead;

        /// <summary>Un tireur (il lance des projectiles au lieu de frapper) : charmé, il peut tirer où le bâtiment lui dit (<see cref="ShootAt"/>).</summary>
        public bool IsRanged => m_RangedAttack != null;

        /// <summary>
        /// Le charme pour <paramref name="duration"/> s, vers <paramref name="destination"/>. Seulement un ennemi qui marche sur le
        /// NavMesh, pas un boss. Renvoie vrai s'il est charmé. <paramref name="stopAt"/> : il s'arrête à tant de mètres de son
        /// point (un tireur qui tire de loin) ; sinon tout près.
        /// </summary>
        /// <remarks>
        /// 1er novembre : un tireur charmé gardait son arrêt à 80 % de sa portée de tir (17 m) : il s'arrêtait loin de la baudruche,
        /// de l'arène ou de sa proie, et restait là à regarder (essai du joueur). Charmé, il s'arrête maintenant où le bâtiment veut.
        /// </remarks>
        public bool Charm(float duration, Vector3 destination, float stopAt = -1f)
        {
            if (m_State == State.Dead || IsBoss || m_Agent == null || m_Behaviour != null || duration <= 0f)
                return false;

            m_CharmTimer = Mathf.Max(m_CharmTimer, duration);
            m_CharmPoint = destination;
            m_CharmStop = stopAt;
            m_CharmRepath = 0f;
            m_HitDelay = -1f;
            return true;
        }

        /// <summary>Change le point où court un ennemi charmé.</summary>
        public void SetCharmDestination(Vector3 destination) => m_CharmPoint = destination;

        /// <summary>Le charme s'arrête tout de suite (un volant attiré repart aussi).</summary>
        public void Uncharm()
        {
            if (m_CharmTimer > 0f)
                RestoreStoppingDistance();
            m_CharmTimer = 0f;
            if (m_Behaviour is FlyingEnemy flyer)
                flyer.EndLure();
        }

        /// <summary>
        /// Un volant attiré vers ce point (la baudruche, 1er novembre) : il y tourne en rond <paramref name="duration"/> s au lieu
        /// de piquer sur le joueur. Faux pour un ennemi qui n'est pas un volant, ou pris au filet. <see cref="Uncharm"/> le libère.
        /// </summary>
        public bool LureFlyer(Vector3 point, float duration)
        {
            if (m_State == State.Dead || m_State == State.Fleeing || IsBoss || IsNetted || !(m_Behaviour is FlyingEnemy flyer))
                return false;

            flyer.Lure(point, duration);
            return true;
        }

        /// <summary>
        /// Charmé, un tireur tire sur ce point (tourné vers lui, son animation d'attaque) : un projectile sans dégâts, et
        /// <paramref name="arrived"/> quand il arrive (c'est le bâtiment qui porte le coup). Faux s'il ne tire pas.
        /// </summary>
        public bool ShootAt(Vector3 point, System.Action arrived)
        {
            if (m_RangedAttack == null || m_State == State.Dead || IsAirborne)
                return false;

            var flat = point - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 1e-4f)
                transform.rotation = Quaternion.LookRotation(flat);
            PlayAttackAnimation();
            return m_RangedAttack.FireAt(point, arrived);
        }

        // Charmé, il s'arrêtait à sa distance d'attaque ; il la retrouve à la fin du charme (en fuite : à la sortie même).
        void RestoreStoppingDistance()
        {
            if (m_Agent != null)
                m_Agent.stoppingDistance = m_State == State.Fleeing ? 0.5f : Def.attackRange * 0.8f;
        }

        // Charmé : il court vers son point, un peu plus vite que d'habitude. Vrai tant qu'il l'est (le reste de l'Update attend).
        bool UpdateCharm(float deltaTime)
        {
            if (m_CharmTimer <= 0f)
                return false;

            m_CharmTimer -= deltaTime;
            if (m_CharmTimer <= 0f)
                RestoreStoppingDistance();
            if (m_CharmTimer <= 0f || m_Agent == null || !m_Agent.enabled || !m_Agent.isOnNavMesh)
                return m_CharmTimer > 0f;

            m_State = State.Moving;
            m_Agent.isStopped = false;
            m_Agent.updateRotation = true;
            m_Agent.stoppingDistance = m_CharmStop >= 0f ? m_CharmStop : Mathf.Min(Def.attackRange * 0.8f, 1.2f);
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
