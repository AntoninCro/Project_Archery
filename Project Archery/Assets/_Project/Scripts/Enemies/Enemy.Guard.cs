using System.Collections.Generic;
using Archery.Player;
using Archery.World;
using UnityEngine;
using UnityEngine.AI;

namespace Archery.Enemies
{
    /// <summary>
    /// Gardien d'un coffre de la vague (4 novembre ; <c>Docs/Refonte_TowerDefense.md</c>, 12.61 ; l'idée du joueur : « des ennemis
    /// vadrouillent dans la forêt et nous attaquent si on s'approche ; on doit les tuer à l'arc pour récupérer les coffres ») : il ne va
    /// jamais à la tour. Il vadrouille au pas autour de son coffre, d'un point au hasard à l'autre, en faisant des pauses ; il fonce sur
    /// le joueur qui s'approche à moins de <c>aggro</c> m de lui (ou
    /// qui le blesse), le poursuit tant que le joueur reste à moins de <c>leash</c> m du coffre, puis y retourne. Le coffre n'était pas
    /// verrouillé (le joueur, 4 novembre : pas de règle de plus) ; depuis le 12 novembre, il est scellé tant que ses gardiens vivent
    /// (<c>Chests/ChestSeal</c> : courir prendre tous les coffres sans un coup était trop simple). Le même soir, après une partie du
    /// joueur (« les ennemis spawn parfois en dehors de la forêt, sur la voie, et parfois en dessous de la map ») : il n'apparaît et ne
    /// rôde qu'au sol à l'air libre, relié au reste de la carte, hors des voies si possible (<see cref="TryGuardPoint"/>) ; enfoui ou
    /// égaré, il revient près de son coffre.
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
        float m_GuardCheckTimer;

        // Où un gardien peut se tenir (12 novembre, le soir). Dans la partie du joueur, un gardien de la vague 1 est resté
        // introuvable (son coffre, scellé, n'a jamais été ouvert) et le blessait sans personne autour : le NavMesh a des morceaux
        // sous la carte et dans les rochers, que NavMesh.SamplePosition trouvait parfois avant le sol ; et l'attaque ne regarde que la
        // distance à l'horizontale. Un point est bon s'il n'a pas de dessus solide juste au-dessus de lui (k_GuardBuriedDepth), s'il
        // est relié aux voies par le NavMesh (pas un morceau isolé) et, si possible, s'il est loin d'elles (k_GuardLaneClearance,
        // comme les points de la forêt de MapScan).
        // Le premier jet, le même soir, ne faisait plus apparaître aucun gardien (le journal : « gardiens : 0 ») : il voulait un
        // chemin jusqu'au point du NavMesh le plus proche de la tour, or le NavMesh est construit d'après tous les colliders de la
        // scène (NavMeshSurface, Physics Colliders), jusque sur le toit de la tour, un morceau isolé ; et il regardait 30 m au-dessus
        // de chaque point, tout dessus compris. Maintenant : relié au point d'apparition d'une voie (là où les ennemis apparaissent
        // et partent à pied), et sans aucun point relié, un point au sol suffit ; seul compte un dessus tourné vers le haut, à moins de
        // 8 m au-dessus du point. Un coffre qui a moins de gardiens que prévu le dit (un avertissement, compté au journal), avec
        // les raisons des refus.
        const float k_GuardLaneClearance = 5f;
        const float k_GuardBuriedDepth = 1f;
        const float k_GuardColumn = 8f;
        const float k_GuardMaxRise = 2.5f;
        const float k_GuardCheckInterval = 1f;
        const float k_GuardLostHeight = 6f;

        static List<MapScan.Lane> s_GuardLanes;
        static NavMeshPath s_GuardPath;
        static readonly RaycastHit[] s_GuardColumnHits = new RaycastHit[24];

        // Les refus de points, pour l'avertissement : pas de NavMesh, trop haut ou trop bas, sous un dessus, coupé des voies.
        static readonly int[] s_GuardRefusals = new int[4];

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
            m_GuardCheckTimer = k_GuardCheckInterval;
            // Pas tous en même temps : certains commencent par une pause.
            m_GuardPauseUntil = Time.time + Random.Range(0f, k_GuardPause.y);
            return true;
        }

        /// <summary>
        /// Un point où un gardien peut se tenir, autour de <paramref name="center"/> (le coffre), entre <paramref name="minRadius"/>
        /// et <paramref name="maxRadius"/> m : sur le NavMesh, au sol sans dessus solide juste au-dessus (ni sous la carte, ni dans un
        /// rocher), pas plus de 2,5 m plus haut ou plus bas que le centre, relié aux voies si <paramref name="connected"/> (sinon, si
        /// aucun essai ne l'est, un point au sol suffit) ; hors des voies si l'un des essais l'est, sinon le premier bon point. Faux si
        /// aucun essai ne convient.
        /// </summary>
        public static bool TryGuardPoint(Vector3 center, float minRadius, float maxRadius, bool connected, out Vector3 point,
                                         int attempts = 16)
        {
            if (FindGuardPoint(center, minRadius, maxRadius, connected, attempts, out point))
                return true;
            return connected && FindGuardPoint(center, minRadius, maxRadius, false, attempts, out point);
        }

        /// <summary>Les raisons des refus de points depuis le dernier appel (pour un avertissement), puis remet les comptes à zéro.</summary>
        public static string TakeGuardRefusals()
        {
            var text = $"refus : sans NavMesh {s_GuardRefusals[0]}, trop haut ou trop bas {s_GuardRefusals[1]}, " +
                       $"sous un dessus {s_GuardRefusals[2]}, coupé des voies {s_GuardRefusals[3]}";
            System.Array.Clear(s_GuardRefusals, 0, s_GuardRefusals.Length);
            return text;
        }

        static bool FindGuardPoint(Vector3 center, float minRadius, float maxRadius, bool connected, int attempts, out Vector3 point)
        {
            point = center;
            var found = false;
            for (var i = 0; i < attempts; i++)
            {
                var offset = Random.insideUnitCircle.normalized * Random.Range(minRadius, Mathf.Max(minRadius, maxRadius));
                var candidate = center + new Vector3(offset.x, 0f, offset.y);
                if (!NavMesh.SamplePosition(candidate, out var hit, 2f, NavMesh.AllAreas))
                {
                    s_GuardRefusals[0]++;
                    continue;
                }

                var position = hit.position;
                if (Mathf.Abs(position.y - center.y) > k_GuardMaxRise)
                {
                    s_GuardRefusals[1]++;
                    continue;
                }

                if (!IsOpenGround(position))
                {
                    s_GuardRefusals[2]++;
                    continue;
                }

                if (connected && !IsConnected(position))
                {
                    s_GuardRefusals[3]++;
                    continue;
                }

                if (GuardLaneDistance(position) >= k_GuardLaneClearance)
                {
                    point = position;
                    return true;
                }

                if (!found)
                {
                    found = true;
                    point = position;
                }
            }

            return found;
        }

        // Pas de dessus solide juste au-dessus de ce point : à la verticale, aucun dessus tourné vers le haut (ni une plante, ni un objet
        // qui bouge, ni le joueur) entre k_GuardBuriedDepth et k_GuardColumn m au-dessus de lui. Un point sous la carte a le sol
        // au-dessus de lui ; un point dans un rocher, le dessus du rocher.
        static bool IsOpenGround(Vector3 point)
        {
            var count = Physics.RaycastNonAlloc(point + Vector3.up * k_GuardColumn, Vector3.down, s_GuardColumnHits, k_GuardColumn,
                                                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var hit = s_GuardColumnHits[i];
                var collider = hit.collider;
                if (collider == null || collider.attachedRigidbody != null || collider is CharacterController || hit.normal.y < 0.5f ||
                    MapScan.IsPlant(collider.transform))
                    continue;
                if (hit.point.y > point.y + k_GuardBuriedDepth)
                    return false;
            }

            return true;
        }

        // Ce point mène, par le NavMesh, au point d'apparition d'une voie : là où les ennemis apparaissent et partent à pied vers la
        // tour (un morceau isolé, sous la carte ou sur un rocher, n'y mène pas). Sans voie connue, tout point est relié.
        static bool IsConnected(Vector3 point)
        {
            s_GuardLanes ??= MapScan.RunLanes().Lanes;
            if (s_GuardLanes.Count == 0)
                return true;

            s_GuardPath ??= new NavMeshPath();
            foreach (var lane in s_GuardLanes)
            {
                if (lane.Points.Count > 0 && NavMesh.CalculatePath(point, lane.Points[0], NavMesh.AllAreas, s_GuardPath) &&
                    s_GuardPath.status == NavMeshPathStatus.PathComplete)
                    return true;
            }

            return false;
        }

        // Distance (à l'horizontale) à la voie la plus proche : le chemin des ennemis, de leur point d'apparition à la tour.
        static float GuardLaneDistance(Vector3 point)
        {
            s_GuardLanes ??= MapScan.RunLanes().Lanes;
            var best = float.MaxValue;
            foreach (var lane in s_GuardLanes)
                best = Mathf.Min(best, lane.DistanceTo(point, out _));
            return best;
        }

        // Enfoui (sous le sol, dans un rocher) ou loin de son coffre en hauteur (tombé d'une terrasse, parti sous la carte) : il revient
        // près de son coffre, en rôdant.
        void CheckGuardFooting(float deltaTime)
        {
            m_GuardCheckTimer -= deltaTime;
            if (m_GuardCheckTimer > 0f)
                return;

            m_GuardCheckTimer = k_GuardCheckInterval;
            if (m_Agent == null || !m_Agent.enabled)
                return;
            var position = transform.position;
            if (Mathf.Abs(position.y - m_GuardPost.y) <= k_GuardLostHeight && IsOpenGround(position))
                return;
            if (!TryGuardPoint(m_GuardPost, 1.5f, m_GuardRadius, true, out var point))
                return;

            m_Agent.Warp(point);
            m_GuardChasing = false;
            m_GuardWalking = false;
            m_GuardPauseUntil = 0f;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetGuardStatics()
        {
            s_GuardLanes = null;
            System.Array.Clear(s_GuardRefusals, 0, s_GuardRefusals.Length);
        }

        // Le gardien qui ne poursuit pas le joueur rôde autour de son coffre, au pas. Vrai tant qu'il rôde (le reste de l'Update
        // attend) ; faux quand il poursuit le joueur (ChooseTarget le vise alors, comme un ennemi ordinaire).
        bool UpdateGuard(float deltaTime)
        {
            if (!m_Guarding)
                return false;

            CheckGuardFooting(deltaTime);
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

        // Un point au hasard autour du coffre, à quelques mètres de lui au moins (qu'on le voie marcher), au sol à l'air libre, hors
        // des voies si possible (12 novembre : il rôdait parfois sur la voie, ou sous la carte). Pas de chemin calculé jusqu'à la tour à
        // chaque point : le NavMeshAgent ne va de toute façon que là où il peut aller.
        Vector3 WanderPoint()
        {
            for (var attempt = 0; attempt < 4; attempt++)
            {
                if (!TryGuardPoint(m_GuardPost, 0f, m_GuardRadius, false, out var point, 6))
                    break;
                if (attempt < 3 && HorizontalDistance(transform.position, point) < 2f)
                    continue;
                return point;
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
