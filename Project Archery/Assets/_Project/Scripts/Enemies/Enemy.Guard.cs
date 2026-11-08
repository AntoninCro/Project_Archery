using Archery.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Archery.Enemies
{
    /// <summary>
    /// Gardien d'un coffre de la vague (4 novembre ; <c>Docs/Refonte_TowerDefense.md</c>, 12.61 ; l'idée du joueur : « des ennemis
    /// vadrouillent dans la forêt et nous attaquent si on s'approche ; on doit les tuer à l'arc pour récupérer les coffres ») : il ne va
    /// jamais à la tour. Il vadrouille au pas autour de son coffre, d'un point au hasard à l'autre, en faisant des pauses ; il fonce sur
    /// le joueur qui s'approche à moins de <c>aggro</c> m de lui (ou
    /// qui le blesse), le poursuit tant que le joueur reste à moins de <c>leash</c> m du coffre, puis y retourne. Le coffre n'est pas
    /// verrouillé (le joueur : pas de règle de plus) : on peut foncer dessus, au prix de ses PV.
    /// </summary>
    public partial class Enemy
    {
        bool m_Guarding;
        Vector3 m_GuardPost;
        float m_GuardRadius;
        float m_GuardAggro;
        float m_GuardLeash;
        bool m_GuardChasing;
        bool m_GuardWalking;
        Vector3 m_GuardWander;
        float m_GuardWanderTimer;
        float m_GuardPauseUntil;
        float m_GuardHurtTime = float.NegativeInfinity;

        // La ronde (4 novembre, le joueur : « ils vadrouillent un peu au hasard en prenant des pauses de temps en temps ») : arrivé à
        // son point, il s'arrête 6 fois sur 10, de 1,5 à 4,5 s.
        const float k_GuardPauseChance = 0.6f;
        static readonly Vector2 k_GuardPause = new Vector2(1.5f, 4.5f);

        /// <summary>Il garde (ou gardait, mort) un coffre : il ne compte pas parmi les ennemis de la vague qui vont à la tour.</summary>
        public bool IsGuard => m_Guarding;

        /// <summary>
        /// En fait le gardien d'un coffre, à ce point : il rôde à <paramref name="radius"/> m autour, attaque le joueur à
        /// <paramref name="aggro"/> m, ne le poursuit pas au-delà de <paramref name="leash"/> m du coffre. <paramref name="awake"/> : il
        /// fonce tout de suite sur le joueur (il vient de sortir parce que le joueur approche). Seulement un ennemi qui marche.
        /// </summary>
        public bool Guard(Vector3 post, float radius, float aggro, float leash, bool awake)
        {
            if (m_State == State.Dead || IsBoss || m_Behaviour != null)
                return false;

            m_Guarding = true;
            m_GuardPost = post;
            m_GuardRadius = Mathf.Max(0.5f, radius);
            m_GuardAggro = aggro;
            m_GuardLeash = Mathf.Max(aggro, leash);
            m_GuardChasing = awake;
            m_GuardWalking = false;
            m_GuardWanderTimer = 0f;
            // Pas tous en même temps : certains commencent par une pause.
            m_GuardPauseUntil = Time.time + Random.Range(0f, k_GuardPause.y);
            return true;
        }

        // Le gardien qui ne poursuit pas le joueur rôde autour de son coffre, au pas. Vrai tant qu'il rôde (le reste de l'Update
        // attend) ; faux quand il poursuit le joueur (ChooseTarget le vise alors, comme un ennemi ordinaire).
        bool UpdateGuard(float deltaTime)
        {
            if (!m_Guarding)
                return false;

            var player = PlayerHealth.Instance;
            if (player != null && player.IsAlive)
            {
                var body = player.BodyPosition;
                var seen = HorizontalDistance(transform.position, body) <= m_GuardAggro || Time.time - m_GuardHurtTime < 3f;
                if (HorizontalDistance(m_GuardPost, body) <= m_GuardLeash && (m_GuardChasing || seen))
                {
                    if (!m_GuardChasing)
                    {
                        // Il passe du pas à la course : la distance d'arrêt de l'attaque, et un chemin tout de suite.
                        RestoreStoppingDistance();
                        m_RepathTimer = 0f;
                    }

                    m_GuardChasing = true;
                    return false;
                }
            }

            // Il lâche le joueur : il repart vers un point autour du coffre, sans pause.
            if (m_GuardChasing)
            {
                m_GuardWalking = false;
                m_GuardPauseUntil = 0f;
            }

            m_GuardChasing = false;
            m_State = State.Moving;
            m_Target = null;
            m_HitDelay = -1f;
            if (m_Agent == null || !m_Agent.enabled || !m_Agent.isOnNavMesh)
                return true;

            // Une pause : il reste planté là un moment.
            if (Time.time < m_GuardPauseUntil)
            {
                Stop();
                return true;
            }

            // Arrivé (ou en route depuis trop longtemps) : souvent une pause, puis un autre point au hasard autour du coffre.
            m_GuardWanderTimer -= deltaTime;
            var arrived = m_GuardWalking && (HorizontalDistance(transform.position, m_GuardWander) < 0.8f || m_GuardWanderTimer <= 0f);
            if (arrived && Random.value < k_GuardPauseChance)
            {
                m_GuardWalking = false;
                m_GuardPauseUntil = Time.time + Random.Range(k_GuardPause.x, k_GuardPause.y);
                Stop();
                return true;
            }

            if (!m_GuardWalking || arrived)
            {
                m_GuardWalking = true;
                m_GuardWanderTimer = 8f;
                m_GuardWander = WanderPoint();
                m_Agent.SetDestination(m_GuardWander);
            }

            m_Agent.isStopped = false;
            m_Agent.updateRotation = true;
            m_Agent.stoppingDistance = 0.5f;
            m_Agent.speed = CurrentSpeed * 0.5f;
            return true;
        }

        // Un point au hasard autour du coffre, à quelques mètres de lui au moins (qu'on le voie marcher), sur le NavMesh.
        Vector3 WanderPoint()
        {
            for (var attempt = 0; attempt < 4; attempt++)
            {
                var offset = Random.insideUnitCircle * m_GuardRadius;
                var point = m_GuardPost + new Vector3(offset.x, 0f, offset.y);
                if (!NavMesh.SamplePosition(point, out var hit, 2f, NavMesh.AllAreas))
                    continue;
                if (attempt < 3 && HorizontalDistance(transform.position, hit.position) < 2f)
                    continue;
                return hit.position;
            }

            return m_GuardPost;
        }

        // Le gardien qui poursuit vise le joueur, quoi qu'il arrive (ni barricade, ni tour). Vrai s'il a sa cible.
        bool ChooseGuardTarget()
        {
            if (!m_Guarding)
                return false;

            var player = PlayerHealth.Instance;
            m_Target = player != null && player.IsAlive ? player.Health : null;
            m_TargetPoint = player != null ? player.BodyPosition : m_GuardPost;
            return true;
        }

        // Blessé (une flèche de loin) : il cherche le joueur, même au-delà de sa distance d'attaque.
        void NoteGuardHurt()
        {
            if (m_Guarding)
                m_GuardHurtTime = Time.time;
        }
    }
}
