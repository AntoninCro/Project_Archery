using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.Upgrades;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Tour-aimant (refonte du tower defense, étape 5, lot 3 ; 28 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, parties 6.1 et
    /// 6.2) : au bord de la voie. Par impulsions, elle attire vers elle les ennemis à portée (ils glissent de quelques mètres) :
    /// le groupe se resserre, à portée du mortier, des mines ou d'une flèche explosive. Depuis le 29 octobre (deuxième
    /// brainstorm, partie 12.6), cinq niveaux :
    /// <list type="bullet">
    /// <item>2 : elle attire aussi les volants vers le sol (ils tombent, comme pris dans un filet) ;</item>
    /// <item>3, l'<b>électro-aimant</b> : elle attire en continu dans sa zone ;</item>
    /// <item>4 : tes flèches qui passent dans sa zone sont courbées vers les ennemis attirés ;</item>
    /// <item>5, la <b>singularité</b> : toutes les 15 s, un trou noir aspire tous les ennemis à 15 m en une boule qui tournoie
    /// 3 s, puis les recrache en l'air.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// L'attraction passe par <c>Enemy.Push</c> (comme celle du fût aimant) ; un ennemi ne dépasse pas la tour. Les boss ne
    /// glissent que moitié moins, et la singularité ne les prend pas. Un éclair (<see cref="LightningBolt"/>) relie la tour aux
    /// ennemis attirés.
    /// </remarks>
    public class StructureMagnet : Structure
    {
        // Enemy.Push s'amortit en exp(-6 t) : une poussée de v m/s fait glisser de v / 6 m en tout.
        const float k_PushDecay = 6f;

        [Tooltip("Portée (m) d'une impulsion, par niveau.")]
        [SerializeField]
        float[] m_RangePerLevel = { 8f, 9f, 10f };

        [Tooltip("Distance (m) dont un ennemi est attiré à chaque impulsion, par niveau.")]
        [SerializeField]
        float[] m_PullPerLevel = { 3f, 3.5f, 4f };

        [Tooltip("Temps (s) entre deux impulsions, par niveau.")]
        [SerializeField]
        float[] m_IntervalPerLevel = { 3.5f, 3f, 2.5f };

        [Tooltip("Distance (m) de la tour sous laquelle on n'attire plus.")]
        [SerializeField]
        float m_KeepAway = 1.5f;

        [Tooltip("Éclairs montrés au plus à chaque impulsion.")]
        [SerializeField]
        int m_MaxBolts = 6;

        [SerializeField]
        Color m_BoltColor = new Color(0.85f, 0.4f, 1f);

        [SerializeField]
        AudioClip m_PulseClip;

        [Tooltip("Temps (s) pendant lequel un ennemi attiré reste préparé pour tes flèches (« Attiré ! », 30 octobre) : " +
                 "50 % de dégâts en plus.")]
        [SerializeField]
        float m_PulledPrepared = 1.5f;

        [Header("Évolutions (29 octobre)")]
        [Tooltip("Temps (s) au sol d'un volant attiré (niveau 2).")]
        [SerializeField]
        float m_FlyerHold = 1.5f;

        [Tooltip("Vitesse (m/s) de l'attraction continue de l'électro-aimant (niveau 3).")]
        [SerializeField]
        float m_ContinuousPull = 1.6f;

        [Tooltip("Vitesse (°/s) à laquelle tes flèches se courbent vers un ennemi attiré (niveau 4).")]
        [SerializeField]
        float m_CurveRate = 240f;

        [Tooltip("Temps (s) entre deux singularités (niveau 5).")]
        [SerializeField]
        float m_SingularityInterval = 15f;

        [Tooltip("Portée (m) de la singularité.")]
        [SerializeField]
        float m_SingularityRange = 15f;

        [Tooltip("Durée (s) pendant laquelle la boule tournoie.")]
        [SerializeField]
        float m_SingularitySpin = 3f;

        [Tooltip("Ennemis aspirés au plus.")]
        [SerializeField]
        int m_SingularityMax = 14;

        [Tooltip("Vitesse (m/s) vers le haut des ennemis recrachés.")]
        [SerializeField]
        float m_SingularityThrow = 11f;

        [SerializeField]
        Material m_SingularityMaterial;

        [SerializeField]
        AudioClip m_SingularityClip;

        class Seized
        {
            public Enemy Enemy;
            public Vector3 Start;
            public float Angle;
            public float Radius;
            public float Height;
        }

        readonly ArrowWatch m_Arrows = new ArrowWatch();
        readonly List<Seized> m_Seized = new List<Seized>();
        float m_Timer = 1f;
        float m_BoltTimer;
        float m_SingularityTimer = 6f;
        float m_SingularityAge = -1f;
        Vector3 m_SingularityCenter;
        Transform m_Hole;

        Vector3 Tip => transform.position + Vector3.up * Mathf.Max(1.2f, ConstructionBounds.size.y * 0.9f);

        float Range => ValueFor(m_RangePerLevel, Mathf.Max(1, Level), 8f) * RangeScale * OverchargeRange;

        void OnEnable() => m_Arrows.Begin();

        void OnDisable() => m_Arrows.End();

        protected override void Update()
        {
            base.Update();
            UpdateSingularity(Time.deltaTime);
            if (!IsBuilt)
                return;

            var level = Mathf.Max(1, Level);
            if (level >= 4)
                CurveArrows(Time.deltaTime);
            if (level >= 5 && m_SingularityAge < 0f)
            {
                m_SingularityTimer -= Time.deltaTime * OverchargeRate;
                if (m_SingularityTimer <= 0f)
                    m_SingularityTimer = BeginSingularity() ? m_SingularityInterval : 0.5f;
            }

            m_Timer -= Time.deltaTime * OverchargeRate;
            if (m_Timer > 0f)
                return;

            // L'électro-aimant attire en continu : de petites impulsions rapprochées.
            if (level >= 3)
            {
                m_Timer = 0.25f;
                Pulse(level, m_ContinuousPull * 0.25f);
                return;
            }

            m_Timer = Pulse(level, ValueFor(m_PullPerLevel, level, 3f)) ? ValueFor(m_IntervalPerLevel, level, 3.5f) : 0.4f;
        }

        // Renvoie vrai si au moins un ennemi a été attiré.
        bool Pulse(int level, float pull)
        {
            var range = Range;
            var origin = transform.position;
            var bolts = 0;
            m_BoltTimer -= level >= 3 ? 0.25f : 1f;
            var showBolts = m_BoltTimer <= 0f;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive || enemy.IsAirborne || enemy.IsHeld)
                    continue;

                var toward = origin - enemy.transform.position;
                toward.y = 0f;
                var distance = toward.magnitude;
                if (distance > range || distance <= m_KeepAway)
                    continue;

                // Niveau 2 : un volant est tiré vers le sol.
                if (enemy.IsFlyer)
                {
                    if (level < 2 || enemy.IsNetted || !enemy.Net(m_FlyerHold, this))
                        continue;
                }
                else
                {
                    var slide = Mathf.Min(pull, distance - m_KeepAway) * (enemy.IsBoss ? 0.5f : 1f);
                    enemy.Push(toward / distance * slide * k_PushDecay);
                    enemy.Prepare(Preparation.Pulled, m_PulledPrepared);
                }

                if (showBolts && bolts < m_MaxBolts)
                    LightningBolt.Spawn(Tip, enemy.Center, m_BoltColor);
                bolts++;
            }

            if (bolts > 0 && showBolts)
            {
                m_BoltTimer = 1f;
                Sfx.Play(m_PulseClip, Tip, level >= 3 ? 0.5f : 0.8f, Random.Range(0.9f, 1.1f));
            }

            return bolts > 0;
        }

        // ---------------------------------------------------------------- niveau 4 : les flèches courbées

        // Une flèche dans la zone se tourne vers l'ennemi attiré qu'elle allait le mieux toucher (devant elle).
        void CurveArrows(float deltaTime)
        {
            var range = Range;
            m_Arrows.Reset();
            while (m_Arrows.Next(out var arrow, out var from, out var to))
            {
                var flat = to - transform.position;
                flat.y = 0f;
                if (flat.sqrMagnitude > range * range || to.y - transform.position.y > 12f)
                    continue;

                var velocity = arrow.Velocity;
                var speed = velocity.magnitude;
                if (speed < 1f)
                    continue;

                var heading = velocity / speed;
                Enemy best = null;
                var bestDot = 0.6f;
                foreach (var enemy in AliveEnemies())
                {
                    if (enemy == null || !enemy.IsAlive)
                        continue;
                    var toEnemy = enemy.Center - to;
                    var offset = enemy.transform.position - transform.position;
                    offset.y = 0f;
                    if (offset.sqrMagnitude > range * range || toEnemy.sqrMagnitude < 0.25f)
                        continue;

                    var dot = Vector3.Dot(heading, toEnemy.normalized);
                    if (dot > bestDot)
                    {
                        best = enemy;
                        bestDot = dot;
                    }
                }

                if (best == null)
                    continue;

                var turned = Vector3.RotateTowards(heading, (best.Center - to).normalized, m_CurveRate * Mathf.Deg2Rad * deltaTime, 0f);
                arrow.Velocity = turned * speed;
                if (m_Arrows.Once(arrow))
                    arrow.SetTrailColor(m_BoltColor);
            }
        }

        // ---------------------------------------------------------------- niveau 5 : la singularité

        bool BeginSingularity()
        {
            var range = m_SingularityRange * RangeScale * OverchargeRange;
            var origin = transform.position;
            var sum = Vector3.zero;
            m_Seized.Clear();
            foreach (var enemy in AliveEnemies())
            {
                if (m_Seized.Count >= m_SingularityMax)
                    break;
                if (enemy == null || !enemy.CanBeLaunched || enemy.IsAirborne)
                    continue;

                var offset = enemy.transform.position - origin;
                offset.y = 0f;
                if (offset.sqrMagnitude > range * range || !enemy.Hold(this))
                    continue;

                m_Seized.Add(new Seized { Enemy = enemy, Start = enemy.transform.position });
                sum += enemy.transform.position;
            }

            if (m_Seized.Count == 0)
                return false;

            // Le trou noir s'ouvre au-dessus du groupe, à 3 m de haut.
            m_SingularityCenter = sum / m_Seized.Count + Vector3.up * 3f;
            for (var i = 0; i < m_Seized.Count; i++)
            {
                m_Seized[i].Angle = i * 360f / m_Seized.Count;
                m_Seized[i].Radius = Random.Range(0.8f, 1.6f);
                m_Seized[i].Height = Random.Range(-0.8f, 0.8f);
            }

            m_SingularityAge = 0f;
            Sfx.Play(m_SingularityClip != null ? m_SingularityClip : m_PulseClip, m_SingularityCenter, 1f, 0.5f);
            return true;
        }

        // Aspirés (0,8 s), ils tournoient en boule (m_SingularitySpin s), puis sont recrachés en l'air dans tous les sens.
        void UpdateSingularity(float deltaTime)
        {
            if (m_SingularityAge < 0f)
                return;

            m_SingularityAge += deltaTime;
            const float suck = 0.8f;
            var end = suck + m_SingularitySpin;
            var hole = Mathf.Clamp01(m_SingularityAge / suck) * Mathf.Clamp01((end + 0.3f - m_SingularityAge) / 0.3f);
            ShowHole(hole);

            foreach (var seized in m_Seized)
            {
                if (seized.Enemy == null || !seized.Enemy.IsHeld)
                    continue;

                seized.Angle += deltaTime * Mathf.Lerp(120f, 540f, Mathf.Clamp01(m_SingularityAge / end));
                var orbit = m_SingularityCenter + Quaternion.Euler(0f, seized.Angle, 0f) * Vector3.forward * seized.Radius +
                            Vector3.up * (seized.Height + 0.3f * Mathf.Sin(seized.Angle * Mathf.Deg2Rad * 2f));
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(m_SingularityAge / suck));
                seized.Enemy.transform.position = Vector3.Lerp(seized.Start, orbit, t);
            }

            if (m_SingularityAge < end)
                return;

            foreach (var seized in m_Seized)
            {
                if (seized.Enemy == null || !seized.Enemy.IsHeld)
                    continue;

                var away = seized.Enemy.transform.position - m_SingularityCenter;
                away.y = 0f;
                away = away.sqrMagnitude > 1e-3f ? away.normalized : Random.insideUnitSphere;
                away.y = 0f;
                seized.Enemy.Release(away.normalized * Random.Range(4f, 8f) + Vector3.up * m_SingularityThrow);
            }

            m_Seized.Clear();
            m_SingularityAge = -1f;
            ShowHole(0f);
            Sfx.Play(m_PulseClip, m_SingularityCenter, 1f, 0.6f);
        }

        // Le trou noir : une sphère sombre qui grossit, tourne, et des éclairs qui y plongent.
        void ShowHole(float size)
        {
            if (m_Hole == null)
            {
                if (size <= 0f)
                    return;

                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "Singularité (effet)";
                Destroy(sphere.GetComponent<Collider>());
                var renderer = sphere.GetComponent<Renderer>();
                if (m_SingularityMaterial != null)
                    renderer.sharedMaterial = m_SingularityMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                m_Hole = sphere.transform;
            }

            m_Hole.gameObject.SetActive(size > 0.01f);
            m_Hole.position = m_SingularityCenter;
            m_Hole.localScale = Vector3.one * (1.6f * size);
            m_Hole.Rotate(0f, 400f * Time.deltaTime, 0f);
            if (size > 0.2f && Random.value < 0.25f)
                LightningBolt.Spawn(m_SingularityCenter + Random.onUnitSphere * 3f, m_SingularityCenter, m_BoltColor, null, 0.05f, 0.2f);
        }

        protected override void OnDestroy()
        {
            foreach (var seized in m_Seized)
            {
                if (seized.Enemy != null)
                    seized.Enemy.Release(Vector3.zero);
            }

            m_Seized.Clear();
            if (m_Hole != null)
                Destroy(m_Hole.gameObject);
            base.OnDestroy();
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.85f, 0.4f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, ValueFor(m_RangePerLevel, Mathf.Max(1, Level), 8f));
            Gizmos.DrawWireSphere(transform.position, m_SingularityRange);
        }
    }
}
