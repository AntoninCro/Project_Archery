using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.Upgrades;
using Archery.World;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Le duo <b>Boulet fou</b> (Barricade + Tremplin ; l'idée du joueur, 31 octobre, <c>Docs/Refonte_TowerDefense.md</c>, 12.15) :
    /// le boulet hérissé du pendule de la barricade, au bout d'un bras, fait des tours complets à toute vitesse, en travers de la
    /// voie, comme une grande roue ; rebondissant, il envoie chaque ennemi qu'il frappe loin sur le côté, jusque sur la voie
    /// voisine, et loin en arrière sur elle. Les ennemis repartent de là vers la tour, par cette voie.
    /// <list type="bullet">
    /// <item>1 : un boulet ; la roue tourne du côté de la voie voisine la plus proche ;</item>
    /// <item>2 : plus vite, plus loin ; l'ennemi lancé renverse ceux sur qui il retombe ;</item>
    /// <item>3, le <b>boulet de canon</b> (après le premier essai, le joueur : « du même niveau que le tremplin ») : deux boulets ;
    /// l'ennemi frappé part à plat, à toute vitesse, jusqu'au mur d'en face, où il s'écrase (une grosse part de ses PV) et
    /// renverse et blesse ceux qui sont autour.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Le lancer passe par <c>Enemy.Launch</c> (une cloche calculée pour retomber sur la voie visée) : les boss ne sont que
    /// poussés, les volants et les mannequins ne bougent pas. Sans autre voie à portée, l'ennemi part sur le côté.
    /// </remarks>
    public class DuoBouletFou : TowerDuo
    {
        [Header("La roue")]
        [Tooltip("Le bras qui tourne (pièce du modèle) : il tourne autour de son axe Z, en travers de la voie.")]
        [SerializeField]
        Transform m_Arm;

        [Tooltip("Les boulets (le centre de chacun) : le second ne sert qu'une fois montré, au niveau 3.")]
        [SerializeField]
        Transform[] m_Balls = new Transform[0];

        [Tooltip("Rayon (m) d'un boulet, pour ce qu'il touche.")]
        [SerializeField]
        float m_BallRadius = 0.8f;

        [Tooltip("Hauteur (m) au-dessus du sol sous laquelle le centre d'un boulet frappe : le bas de la roue.")]
        [SerializeField]
        float m_StrikeHeight = 2.8f;

        [Tooltip("Temps (s) d'un tour complet, par niveau.")]
        [SerializeField]
        float[] m_TurnTimePerLevel = { 1.6f, 1.25f, 1.1f };

        [Tooltip("Dégâts d'un coup de boulet, par niveau (vague 1, en Normal).")]
        [SerializeField]
        float[] m_HitDamagePerLevel = { 25f, 35f, 45f };

        [Tooltip("Temps (s) avant qu'un même ennemi soit frappé de nouveau.")]
        [SerializeField]
        float m_Cooldown = 1.2f;

        [Header("Le lancer (niveaux 1 et 2)")]
        [Tooltip("Distance (m) au plus d'une autre voie où envoyer l'ennemi, par niveau.")]
        [SerializeField]
        float[] m_MaxThrowPerLevel = { 50f, 65f };

        [Tooltip("Distance (m) dont il retombe en arrière sur la voie voisine (vers la forêt), par niveau : il y perd du chemin.")]
        [SerializeField]
        float[] m_BackAlongPerLevel = { 10f, 18f };

        [Tooltip("Sans autre voie à portée : distance (m) du lancer sur le côté, par niveau.")]
        [SerializeField]
        float[] m_SideThrowPerLevel = { 28f, 38f };

        [Tooltip("Vitesse (m/s) à plat du lancer : la durée du vol en découle (entre 1,4 et 3,2 s).")]
        [SerializeField]
        float m_ThrowSpeed = 16f;

        [Tooltip("Rayon (m) autour de l'ennemi retombé : ceux qui y sont tombent (niveau 2).")]
        [SerializeField]
        float m_LandingRadius = 3f;

        [Header("Le boulet de canon (niveau 3)")]
        [Tooltip("Vitesse (m/s) du lancer à plat, jusqu'au mur d'en face.")]
        [SerializeField]
        float m_WallSpeed = 38f;

        [Tooltip("Vitesse (m/s) vers le haut au départ : il passe par-dessus ce qui est bas.")]
        [SerializeField]
        float m_WallLift = 3f;

        [Tooltip("Pesanteur du vol, en part de la vraie : presque rien, il file droit.")]
        [SerializeField]
        float m_WallGravity = 0.15f;

        [Tooltip("Part de ses PV max que lui coûte le choc contre le mur (ou le sol, s'il n'en heurte pas).")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_WallSmashShare = 0.6f;

        [Tooltip("Rayon (m) et dégâts (au centre) du choc pour ceux qui sont autour.")]
        [SerializeField]
        float m_SmashRadius = 3f;

        [SerializeField]
        float m_SmashDamage = 40f;

        [Header("Sons")]
        [Tooltip("Le boulet qui passe au plus bas, à chaque tour.")]
        [SerializeField]
        AudioClip m_SwingClip;

        [SerializeField]
        AudioClip m_HitClip;

        static readonly List<Enemy> s_Near = new List<Enemy>();
        readonly Dictionary<Enemy, float> m_Next = new Dictionary<Enemy, float>();
        readonly HashSet<Enemy> m_Thrown = new HashSet<Enemy>();
        readonly List<Enemy> m_Forget = new List<Enemy>();
        Vector3[] m_BallScales = new Vector3[0];
        float m_Angle;
        float m_Spin = 1f;
        float m_Squash;
        float m_NextSide;
        bool m_WasLow;

        protected override void Awake()
        {
            base.Awake();
            m_BallScales = new Vector3[m_Balls.Length];
            for (var i = 0; i < m_Balls.Length; i++)
                m_BallScales[i] = m_Balls[i] != null ? m_Balls[i].localScale : Vector3.one;
        }

        void OnEnable()
        {
            Enemy.Landed += OnLanded;
            Enemy.Smashed += OnSmashed;
        }

        void OnDisable()
        {
            Enemy.Landed -= OnLanded;
            Enemy.Smashed -= OnSmashed;
        }

        protected override void Update()
        {
            base.Update();
            if (!IsBuilt || m_Arm == null)
                return;

            // La roue tourne du côté de la voie voisine (revu de temps en temps : la tour a pu être déplacée).
            if (Time.time >= m_NextSide)
            {
                m_NextSide = Time.time + 2f;
                m_Spin = SideTowardNeighbour();
            }

            var level = Mathf.Max(1, Level);
            var turn = Mathf.Max(0.3f, ValueFor(m_TurnTimePerLevel, level, 1.6f));
            m_Angle = Mathf.Repeat(m_Angle + m_Spin * 360f * Time.deltaTime * OverchargeRate / turn, 360f);
            m_Arm.localRotation = Quaternion.Euler(0f, 0f, m_Angle);

            var low = false;
            for (var i = 0; i < m_Balls.Length; i++)
            {
                var ball = m_Balls[i];
                if (ball == null || !ball.gameObject.activeInHierarchy)
                    continue;
                if (ball.position.y - transform.position.y < m_StrikeHeight)
                {
                    low = true;
                    Strike(ball.position, level);
                }
            }

            // Le souffle du boulet qui passe au plus bas.
            if (low && !m_WasLow)
                Sfx.Play(m_SwingClip, transform.position + Vector3.up * 1.5f, 0.5f, Random.Range(0.55f, 0.65f));
            m_WasLow = low;
            UpdateSquash();
            if (m_Next.Count > 32)
                ForgetGone();
        }

        // Les ennemis que touche ce boulet partent vers la voie voisine.
        void Strike(Vector3 ball, int level)
        {
            var reach = m_BallRadius + 0.6f;
            var side = transform.right * m_Spin;
            side.y = 0f;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive || enemy.IsFlyer || enemy.IsAirborne ||
                    (m_Next.TryGetValue(enemy, out var next) && Time.time < next) || (enemy.Center - ball).sqrMagnitude > reach * reach)
                    continue;

                m_Next[enemy] = Time.time + m_Cooldown;
                enemy.Health.TakeDamage(new DamageInfo
                {
                    Amount = ValueFor(m_HitDamagePerLevel, level, 25f) * Strength,
                    Zone = HitZone.Body,
                    Point = ball,
                    Direction = side.normalized,
                    Source = this,
                });
                if (!enemy.IsAlive)
                    continue;

                if (Throw(enemy, side.normalized, level))
                    m_Thrown.Add(enemy);
                m_Squash = 1f;
                Sfx.Play(m_HitClip, ball, 0.9f, Random.Range(0.9f, 1.1f));
            }
        }

        // Niveaux 1 et 2 : une cloche qui retombe sur la voie visée. Niveau 3 : à plat, à toute vitesse, jusqu'au mur d'en face.
        bool Throw(Enemy enemy, Vector3 side, int level)
        {
            if (level >= 3)
            {
                if (!enemy.Launch(side * m_WallSpeed + Vector3.up * m_WallLift, this, false, m_WallGravity))
                    return false;
                enemy.SetSmash(enemy.Health.Max * m_WallSmashShare);
                return true;
            }

            var start = enemy.transform.position;
            var to = Landing(enemy, side, level) - start;
            var flat = new Vector3(to.x, 0f, to.z);
            var time = Mathf.Clamp(flat.magnitude / Mathf.Max(1f, m_ThrowSpeed), 1.4f, 3.2f);
            var gravity = -Physics.gravity.y;
            var velocity = flat / time;
            velocity.y = (to.y + 0.5f * gravity * time * time) / time;
            return enemy.Launch(velocity, this);
        }

        // Un point de la voie voisine, en arrière (vers la forêt) de celui qui est le plus proche de l'ennemi, du côté où tourne la
        // roue s'il y en a une ; sinon sur le côté.
        Vector3 Landing(Enemy enemy, Vector3 side, int level)
        {
            var maxThrow = ValueFor(m_MaxThrowPerLevel, level, 50f);
            var back = ValueFor(m_BackAlongPerLevel, level, 10f);
            var from = enemy.transform.position;
            var lanes = BuildGrid.Instance != null ? BuildGrid.Instance.Lanes : null;
            if (lanes != null && lanes.Count > 1)
            {
                var own = Nearest(lanes, from, null, out _);
                var bestDistance = float.MaxValue;
                var best = Vector3.zero;
                var bestOnSide = false;
                foreach (var lane in lanes)
                {
                    if (lane == own || lane.Points.Count < 2)
                        continue;

                    var distance = lane.DistanceTo(from, out var along);
                    if (distance > maxThrow || distance < 4f)
                        continue;

                    var point = lane.PointAt(Mathf.Max(0f, along - back), out _);
                    var onSide = Vector3.Dot(point - from, side) > 0f;
                    if ((onSide && !bestOnSide) || (onSide == bestOnSide && distance < bestDistance))
                    {
                        best = point;
                        bestDistance = distance;
                        bestOnSide = onSide;
                    }
                }

                if (bestDistance < float.MaxValue)
                    return best;
            }

            return from + side * ValueFor(m_SideThrowPerLevel, level, 28f);
        }

        // Le sens de la roue (1 ou -1) : au plus bas, le boulet va vers la voie voisine la plus proche de la tour.
        float SideTowardNeighbour()
        {
            var lanes = BuildGrid.Instance != null ? BuildGrid.Instance.Lanes : null;
            if (lanes == null || lanes.Count < 2)
                return m_Spin;

            var origin = transform.position;
            var own = Nearest(lanes, origin, null, out _);
            var other = Nearest(lanes, origin, own, out var along);
            if (other == null)
                return m_Spin;

            var point = other.PointAt(along, out _);
            return Vector3.Dot(point - origin, transform.right) >= 0f ? 1f : -1f;
        }

        static MapScan.Lane Nearest(IReadOnlyList<MapScan.Lane> lanes, Vector3 point, MapScan.Lane except, out float along)
        {
            along = 0f;
            MapScan.Lane best = null;
            var bestDistance = float.MaxValue;
            foreach (var lane in lanes)
            {
                if (lane == except || lane.Points.Count < 2)
                    continue;

                var distance = lane.DistanceTo(point, out var at);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = lane;
                    along = at;
                }
            }

            return best;
        }

        // Un ennemi lancé par le boulet retombe : au niveau 2, ceux qui sont autour tombent.
        void OnLanded(Enemy enemy)
        {
            if (enemy == null || !m_Thrown.Remove(enemy) || Level != 2)
                return;

            foreach (var other in Around(enemy, enemy.transform.position, m_LandingRadius))
                Knock(other, enemy.transform.position);
        }

        // Le boulet de canon (niveau 3) s'écrase contre le mur d'en face (ou le sol) : ceux qui sont autour sont blessés et renversés.
        void OnSmashed(Enemy enemy, Vector3 point)
        {
            if (enemy == null || !m_Thrown.Remove(enemy))
                return;

            foreach (var other in Around(enemy, point, m_SmashRadius))
            {
                var away = other.transform.position - point;
                away.y = 0f;
                var distance = away.magnitude;
                other.Health.TakeDamage(new DamageInfo
                {
                    Amount = m_SmashDamage * Strength * Mathf.Lerp(1f, 0.5f, distance / m_SmashRadius),
                    Zone = HitZone.Body,
                    Point = other.Center,
                    Direction = distance > 1e-3f ? away / distance : Vector3.up,
                    Source = this,
                });
                if (other.IsAlive)
                    Knock(other, point);
            }

            var arrows = SpecialArrows.Instance;
            if (arrows != null && arrows.ExplosionFireMaterial != null && arrows.ExplosionSmokeMaterial != null)
                ExplosionFx.Play(point, 1.5f, arrows.ExplosionFireMaterial, arrows.ExplosionSmokeMaterial, null);
            Sfx.Play(m_HitClip, point, 1f, Random.Range(0.55f, 0.65f));
        }

        // Les ennemis au sol autour de ce point, sauf celui-ci.
        static List<Enemy> Around(Enemy except, Vector3 point, float radius)
        {
            s_Near.Clear();
            foreach (var other in AliveEnemies())
            {
                if (other != null && other != except && other.IsAlive && !other.IsAirborne &&
                    (other.transform.position - point).sqrMagnitude <= radius * radius)
                    s_Near.Add(other);
            }

            return s_Near;
        }

        // Renversé : un petit saut loin du point.
        void Knock(Enemy enemy, Vector3 point)
        {
            var away = enemy.transform.position - point;
            away.y = 0f;
            enemy.Launch((away.sqrMagnitude > 1e-4f ? away.normalized : Random.insideUnitSphere) * 2.5f + Vector3.up * 4f, this);
        }

        // Rebondissant : le boulet s'écrase un instant à chaque coup, puis reprend sa forme.
        void UpdateSquash()
        {
            if (m_Squash <= 0f)
                return;

            m_Squash = Mathf.MoveTowards(m_Squash, 0f, Time.deltaTime * 6f);
            var squash = Mathf.Sin(m_Squash * Mathf.PI) * 0.25f;
            for (var i = 0; i < m_Balls.Length; i++)
            {
                if (m_Balls[i] != null)
                    m_Balls[i].localScale = Vector3.Scale(m_BallScales[i], new Vector3(1f + squash, 1f - squash, 1f + squash));
            }
        }

        void ForgetGone()
        {
            m_Forget.Clear();
            foreach (var pair in m_Next)
            {
                if (pair.Key == null || !pair.Key.IsAlive || Time.time > pair.Value + 5f)
                    m_Forget.Add(pair.Key);
            }

            foreach (var enemy in m_Forget)
            {
                m_Next.Remove(enemy);
                m_Thrown.Remove(enemy);
            }
        }
    }
}
