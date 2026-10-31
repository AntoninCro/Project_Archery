using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.UI;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Le duo <b>Marteau-pilon</b> (Barricade + Piques ; <c>Docs/Refonte_TowerDefense.md</c>, 12.15 et 12.17) : un portique de forge
    /// en travers de la voie ; son marteau de pierre glisse au-dessus de l'ennemi visé et l'enfonce dans le sol jusqu'au cou. Planté,
    /// seule sa tête dépasse (« Planté ! » : un tir à la tête facile, l'ennemi préparé) ; à la fin, il en sort comme un bouchon.
    /// <list type="bullet">
    /// <item>1 : un marteau, un coup toutes les 3 s ;</item>
    /// <item>2 : l'onde du coup renverse et blesse tout autour ;</item>
    /// <item>3, <b>la forge</b> : trois marteaux en cadence sur toute la largeur de la voie (gauche, milieu, droite), puis le
    /// <b>grand coup</b>, les trois ensemble : l'onde envoie en l'air tout ce qui est à 11 m.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Planter un ennemi, c'est le tenir (<see cref="Enemy.Hold"/>) sous le sol, puis le lâcher vers le haut. Les boss ne sont
    /// qu'étourdis ; les volants pas touchés. Le modèle : x en travers de la voie (les marteaux y glissent), z le long.
    /// </remarks>
    public class DuoMarteauPilon : TowerDuo
    {
        [Header("Les marteaux")]
        [Tooltip("Les têtes des marteaux : le milieu (niveau 1), puis la gauche et la droite (niveau 3).")]
        [SerializeField]
        Transform[] m_Hammers = new Transform[0];

        [Tooltip("Les chariots sous la poutre (un par marteau) : ils suivent le marteau en travers.")]
        [SerializeField]
        Transform[] m_Chariots = new Transform[0];

        [Tooltip("Les câbles (un par marteau ; repère en haut, le câble descend de 1 m à l'échelle 1) : étirés jusqu'au marteau.")]
        [SerializeField]
        Transform[] m_Cables = new Transform[0];

        [Tooltip("Demi-hauteur (m) d'une tête de marteau : au plus bas, elle touche le sol.")]
        [SerializeField]
        float m_HeadHalf = 0.7f;

        [Tooltip("Demi-largeur (m) de la zone frappée, en travers de la voie, et sa demi-profondeur le long d'elle.")]
        [SerializeField]
        float m_HalfWidth = 3.4f;

        [SerializeField]
        float m_HalfDepth = 2.4f;

        [Tooltip("Jusqu'où (m, en travers) le marteau du milieu glisse au plus : au-delà, il heurterait les piliers.")]
        [SerializeField]
        float m_TravelHalf = 2f;

        [Header("Les coups")]
        [Tooltip("Temps (s) entre deux coups, par niveau (au niveau 3 : entre deux temps de la cadence).")]
        [SerializeField]
        float[] m_IntervalPerLevel = { 3f, 2.4f, 0.8f };

        [Tooltip("Dégâts du marteau sur l'ennemi frappé, par niveau (vague 1, en Normal).")]
        [SerializeField]
        float[] m_DamagePerLevel = { 45f, 60f, 70f };

        [Tooltip("Temps (s) qu'un ennemi reste planté, par niveau.")]
        [SerializeField]
        float[] m_PlantTimePerLevel = { 4f, 4.5f, 5f };

        [Tooltip("Vitesse (m/s) vers le haut quand il sort du sol, comme un bouchon.")]
        [SerializeField]
        float m_PopSpeed = 7f;

        [Header("L'onde (niveaux 2 et 3)")]
        [SerializeField]
        float m_WaveRadius = 5f;

        [SerializeField]
        float m_WaveDamage = 20f;

        [Header("Le grand coup (niveau 3)")]
        [Tooltip("Rayon (m) de l'onde du grand coup, les trois marteaux ensemble.")]
        [SerializeField]
        float m_GreatRadius = 11f;

        [SerializeField]
        float m_GreatDamage = 45f;

        [Tooltip("Vitesse (m/s) vers le haut de ceux que l'onde envoie en l'air.")]
        [SerializeField]
        float m_GreatLift = 11f;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_SlamClip;

        [SerializeField]
        AudioClip m_PopClip;

        class Hammer
        {
            public Transform Head;
            public Transform Chariot;
            public Transform Cable;
            public Vector3 Rest;
            public float HomeX;
            public float From;
            public float To;
            public float Time = -1f;
            public bool Struck;
            public bool Great;
            public Enemy Target;
        }

        class Planted
        {
            public Enemy Enemy;
            public float Until;
            public float GroundY;
        }

        // Le temps d'un coup : viser (le marteau glisse et se lève), tomber, rester au sol, remonter.
        static readonly float[] k_Slow = { 0.45f, 0.12f, 0.3f, 0.8f };
        static readonly float[] k_Fast = { 0.2f, 0.08f, 0.12f, 0.3f };

        readonly List<Hammer> m_State = new List<Hammer>();
        readonly List<Planted> m_Planted = new List<Planted>();
        readonly List<Enemy> m_Hit = new List<Enemy>();
        float m_NextBlow;
        int m_Beat;

        protected override void Awake()
        {
            base.Awake();
            for (var i = 0; i < m_Hammers.Length; i++)
            {
                if (m_Hammers[i] == null)
                    continue;
                m_State.Add(new Hammer
                {
                    Head = m_Hammers[i],
                    Chariot = i < m_Chariots.Length ? m_Chariots[i] : null,
                    Cable = i < m_Cables.Length ? m_Cables[i] : null,
                    Rest = m_Hammers[i].localPosition,
                    HomeX = m_Hammers[i].localPosition.x,
                });
            }
        }

        protected override void OnDestroy()
        {
            foreach (var planted in m_Planted)
                Unplant(planted, false);
            m_Planted.Clear();
            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();
            UpdatePlanted();
            if (!IsBuilt || m_State.Count == 0)
                return;

            var level = Mathf.Max(1, Level);
            foreach (var hammer in m_State)
                Animate(hammer, level);

            if (Time.time < m_NextBlow)
                return;

            if (level >= 3)
                ForgeBeat();
            else if (TryAim(m_State[0], -m_HalfWidth, m_HalfWidth, false))
                m_NextBlow = Time.time + ValueFor(m_IntervalPerLevel, level, 3f) / OverchargeRate;
        }

        // La forge (niveau 3) : gauche, milieu, droite, puis le grand coup ; elle bat tant qu'il y a des ennemis à portée.
        void ForgeBeat()
        {
            if (EnemiesNear(transform.position, m_GreatRadius).Count == 0)
                return;

            m_NextBlow = Time.time + ValueFor(m_IntervalPerLevel, 3, 0.8f) / OverchargeRate;
            var beat = m_Beat++ % 4;
            var third = m_HalfWidth / 3f;
            if (beat < 3)
            {
                // Les marteaux : 0 au milieu, 1 à gauche, 2 à droite ; les temps : gauche, milieu, droite.
                var index = beat == 0 ? 1 : beat == 1 ? 0 : 2;
                if (index >= m_State.Count)
                    index = 0;
                var center = index == 1 ? -2f * third : index == 2 ? 2f * third : 0f;
                Aim(m_State[index], center - third, center + third, false);
                return;
            }

            for (var i = 0; i < m_State.Count; i++)
            {
                var center = i == 1 ? -2f * third : i == 2 ? 2f * third : 0f;
                Aim(m_State[i], center - third, center + third, true);
            }
        }

        // Vise l'ennemi au sol le plus proche du milieu de cette bande (en travers) ; sans ennemi, ne frappe pas.
        bool TryAim(Hammer hammer, float minX, float maxX, bool great)
        {
            if (hammer.Time >= 0f || FindTarget(minX, maxX) == null)
                return false;
            Aim(hammer, minX, maxX, great);
            return true;
        }

        // Le marteau part : vers l'ennemi de sa bande s'il y en a un, sinon au milieu de sa bande (la cadence de la forge).
        void Aim(Hammer hammer, float minX, float maxX, bool great)
        {
            if (hammer.Time >= 0f && !great)
                return;

            // Au niveau 3, chaque marteau frappe sa bande sans glisser (ils se heurteraient) : l'ennemi est tiré dessous.
            var target = FindTarget(minX, maxX);
            hammer.Target = target;
            hammer.From = hammer.Head.localPosition.x;
            hammer.To = Level >= 3 ? hammer.HomeX
                : target != null ? Mathf.Clamp(transform.InverseTransformPoint(target.transform.position).x, minX, maxX)
                : (minX + maxX) * 0.5f;
            hammer.To = Mathf.Clamp(hammer.To, -m_TravelHalf, m_TravelHalf);
            hammer.Time = 0f;
            hammer.Struck = false;
            hammer.Great = great;
        }

        Enemy FindTarget(float minX, float maxX)
        {
            Enemy best = null;
            var bestScore = float.MaxValue;
            var middle = (minX + maxX) * 0.5f;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive || enemy.IsFlyer || enemy.IsAirborne || IsPlanted(enemy))
                    continue;

                var local = transform.InverseTransformPoint(enemy.transform.position);
                if (local.x < minX - 0.6f || local.x > maxX + 0.6f || Mathf.Abs(local.z) > m_HalfDepth || Mathf.Abs(local.y) > 3f)
                    continue;

                var score = Mathf.Abs(local.x - middle) + Mathf.Abs(local.z) * 0.5f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = enemy;
                }
            }

            return best;
        }

        void Animate(Hammer hammer, int level)
        {
            var times = level >= 3 ? k_Fast : k_Slow;
            var bottom = m_HeadHalf;
            var raised = hammer.Rest.y + 0.8f;
            var position = hammer.Rest;
            if (hammer.Time >= 0f)
            {
                hammer.Time += Time.deltaTime * OverchargeRate;
                var t = hammer.Time;
                position.x = Mathf.Lerp(hammer.From, hammer.To, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / times[0])));
                if (t < times[0])
                {
                    position.y = Mathf.Lerp(hammer.Rest.y, raised, Mathf.SmoothStep(0f, 1f, t / times[0]));
                }
                else if (t < times[0] + times[1])
                {
                    var fall = (t - times[0]) / times[1];
                    position.y = Mathf.Lerp(raised, bottom, fall * fall);
                }
                else
                {
                    if (!hammer.Struck)
                    {
                        hammer.Struck = true;
                        Strike(hammer, level);
                    }

                    var after = t - times[0] - times[1];
                    position.y = after < times[2] ? bottom
                        : Mathf.Lerp(bottom, hammer.Rest.y, Mathf.SmoothStep(0f, 1f, (after - times[2]) / times[3]));
                    if (after >= times[2] + times[3])
                    {
                        hammer.Time = -1f;
                        hammer.Rest.x = hammer.To;
                        position = hammer.Rest;
                    }
                }
            }
            else
            {
                // Au repos, il revient doucement à sa place (au-dessus du milieu de sa bande).
                hammer.Rest.x = Mathf.MoveTowards(hammer.Rest.x, hammer.HomeX, Time.deltaTime * 1.5f);
                position = hammer.Rest;
            }

            hammer.Head.localPosition = position;
            if (hammer.Chariot != null)
            {
                var chariot = hammer.Chariot.localPosition;
                chariot.x = position.x;
                hammer.Chariot.localPosition = chariot;
            }

            if (hammer.Cable != null)
            {
                var top = hammer.Cable.position.y;
                var head = hammer.Head.position.y + m_HeadHalf * hammer.Head.lossyScale.y;
                var scale = hammer.Cable.localScale;
                scale.y = Mathf.Max(0.05f, (top - head) / Mathf.Max(0.01f, hammer.Cable.parent != null ? hammer.Cable.parent.lossyScale.y : 1f));
                hammer.Cable.localScale = scale;
            }
        }

        // Le marteau touche le sol : il plante sa cible ; l'onde (niveau 2) ; le grand coup (niveau 3, les trois ensemble).
        void Strike(Hammer hammer, int level)
        {
            var point = hammer.Head.position - Vector3.up * m_HeadHalf * hammer.Head.lossyScale.y;
            var damage = ValueFor(m_DamagePerLevel, level, 45f);
            m_Hit.Clear();
            var target = hammer.Target;
            if (target != null && target.IsAlive && !target.IsAirborne)
            {
                var flat = target.transform.position - point;
                flat.y = 0f;
                if (flat.magnitude < 2.2f)
                {
                    m_Hit.Add(target);
                    if (Hurt(target, damage, point, Vector3.down))
                        Plant(target, point, ValueFor(m_PlantTimePerLevel, level, 4f));
                }
            }

            // Ceux qui sont juste sous la tête prennent le coup aussi.
            foreach (var enemy in EnemiesNear(point, 1.2f, true, target))
            {
                if (IsPlanted(enemy))
                    continue;
                m_Hit.Add(enemy);
                Hurt(enemy, damage * 0.5f, point, Vector3.down);
            }

            if (level >= 2)
            {
                foreach (var enemy in EnemiesNear(point, m_WaveRadius))
                {
                    if (m_Hit.Contains(enemy) || IsPlanted(enemy))
                        continue;
                    if (Hurt(enemy, m_WaveDamage, point, enemy.transform.position - point))
                        KnockDown(enemy, point, 3f, 5f);
                }
            }

            Burst(point + Vector3.up * 0.2f, new Color(0.78f, 0.7f, 0.56f, 1f), 40, 7f, 0.5f, 0.9f, 1.2f, false);
            Sfx.Play(m_SlamClip, point, 1f, Random.Range(0.55f, 0.7f));

            // Le grand coup : le premier marteau des trois qui touche fait l'onde géante.
            if (hammer.Great && hammer == FirstGreat())
                GreatBlow();
        }

        Hammer FirstGreat()
        {
            foreach (var hammer in m_State)
            {
                if (hammer.Great && hammer.Struck)
                    return hammer;
            }

            return null;
        }

        void GreatBlow()
        {
            var center = transform.position;
            foreach (var enemy in EnemiesNear(center, m_GreatRadius))
            {
                if (IsPlanted(enemy) || m_Hit.Contains(enemy))
                    continue;
                var away = enemy.transform.position - center;
                away.y = 0f;
                if (!Hurt(enemy, m_GreatDamage, enemy.Center, away))
                    continue;
                var push = away.sqrMagnitude > 1e-4f ? away.normalized * 2.5f : Vector3.zero;
                enemy.Launch(push + Vector3.up * m_GreatLift * Random.Range(0.85f, 1.1f), this);
            }

            for (var i = 0; i < 8; i++)
            {
                var angle = i * Mathf.PI / 4f;
                var at = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * m_GreatRadius * 0.6f;
                Burst(at + Vector3.up * 0.2f, new Color(0.78f, 0.7f, 0.56f, 1f), 30, 9f, 0.7f, 1.1f, 1.2f, false);
            }

            Burst(center + Vector3.up * 1f, new Color(1f, 0.75f, 0.3f, 1f), 80, 14f, 0.35f, 0.9f, 0.6f);
            Sfx.Play(m_SlamClip, center, 1f, 0.4f);
        }

        // Planté jusqu'au cou : tenu sous le sol, seule la tête dépasse.
        void Plant(Enemy enemy, Vector3 point, float duration)
        {
            if (enemy.IsBoss || !enemy.CanBeLaunched)
            {
                enemy.Stun(1.2f);
                return;
            }

            var ground = enemy.transform.position.y;
            var sink = SinkDepth(enemy);
            if (!enemy.Hold(this))
                return;

            var position = enemy.transform.position;
            position.x = point.x;
            position.z = point.z;
            position.y = ground - sink;
            enemy.transform.position = position;
            enemy.Prepare(Preparation.Pinned, duration);
            m_Planted.Add(new Planted { Enemy = enemy, Until = Time.time + duration, GroundY = ground });
            FloatingText.Spawn(position + Vector3.up * (sink + 1.2f), "Planté !", new Color(1f, 0.8f, 0.35f, 1f), 1.4f, 1.4f);
        }

        // Jusqu'où l'enfoncer : sous le bas de sa tête (sa zone de tête), sinon aux trois quarts de sa hauteur.
        static float SinkDepth(Enemy enemy)
        {
            var root = enemy.transform.position.y;
            foreach (var hitbox in enemy.GetComponentsInChildren<Hitbox>())
            {
                if (hitbox.Zone != HitZone.Head)
                    continue;
                var collider = hitbox.GetComponent<Collider>();
                if (collider != null)
                    return Mathf.Clamp(collider.bounds.min.y - root - 0.05f, 0.3f, 4f);
            }

            var height = 1.8f;
            var renderer = enemy.GetComponentInChildren<Renderer>();
            if (renderer != null)
                height = renderer.bounds.max.y - root;
            return Mathf.Clamp(height * 0.75f, 0.3f, 4f);
        }

        bool IsPlanted(Enemy enemy)
        {
            foreach (var planted in m_Planted)
            {
                if (planted.Enemy == enemy)
                    return true;
            }

            return false;
        }

        void UpdatePlanted()
        {
            for (var i = m_Planted.Count - 1; i >= 0; i--)
            {
                var planted = m_Planted[i];
                if (planted.Enemy != null && planted.Enemy.IsAlive && planted.Enemy.IsHeld && Time.time < planted.Until)
                    continue;
                Unplant(planted, true);
                m_Planted.RemoveAt(i);
            }
        }

        // Il sort du sol comme un bouchon (vivant), ou y remonte mort.
        void Unplant(Planted planted, bool pop)
        {
            var enemy = planted.Enemy;
            if (enemy == null || !enemy.IsHeld)
                return;

            var position = enemy.transform.position;
            position.y = planted.GroundY;
            enemy.transform.position = position;
            enemy.Unprepare(Preparation.Pinned);
            var alive = enemy.IsAlive && pop;
            enemy.Release(alive ? Vector3.up * m_PopSpeed : Vector3.zero);
            if (alive)
                Sfx.Play(m_PopClip, position, 0.8f, Random.Range(0.9f, 1.15f));
        }
    }
}
