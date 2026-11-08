using System.Collections.Generic;
using Archery.Core;
using Archery.Enemies;
using Archery.UI;
using Archery.Upgrades;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Le duo <b>Boule à neige</b> (Bulle + Tour de givre ; <c>Docs/Refonte_TowerDefense.md</c>, 12.15 et 12.18) : une boule à
    /// neige sur un socle de bois verni. Une boule de verre géante tombe du ciel sur un groupe et l'enferme ; il y neige, et les
    /// ennemis s'y figent peu à peu en figurines. Ta flèche brise la boule : toutes les figurines éclatent. Sinon, au bout de son
    /// temps, elle se fend et les relâche, un peu blessés.
    /// <list type="bullet">
    /// <item>1 : une boule de 5 m, 6 s ;</item>
    /// <item>2 : à mi-temps, la boule se secoue : les figurines tourbillonnent dans la neige et se heurtent ;</item>
    /// <item>3, la <b>boule immense</b> : toutes les 18 s, une boule de 18 m sur toute une voie, secouée elle aussi ; brisée par ta
    /// flèche, chaque figurine éclate et meurt ; fendue, elle leur coûte 40 % de leurs PV.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Figer, c'est le gel des ennemis (<see cref="Enemy.Freeze"/>, la pointe de givre) ; tourbillonner, c'est les tenir
    /// (<see cref="Enemy.Hold"/>). Les boss et les volants ne sont pas enfermés (les boss, seulement englués). La boule est une copie
    /// de celle du modèle (sa sphère de verre et sa collerette), avec un <see cref="DuoShotTarget"/> qui reçoit les flèches ; les
    /// ennemis du dehors ne peuvent pas y entrer.
    /// </remarks>
    public class DuoBouleNeige : TowerDuo
    {
        [Header("La boule")]
        [Tooltip("La boule de verre du modèle, centrée sur sa sphère : copiée à chaque tir, à la bonne taille ; elle tremble au tir.")]
        [SerializeField]
        Transform m_GlobeTemplate;

        [Tooltip("Diamètre (m) de la sphère de la boule du modèle, à l'échelle 1.")]
        [SerializeField]
        float m_TemplateDiameter = 2.4f;

        [SerializeField]
        float m_Range = 32f;

        [Tooltip("Temps (s) entre deux boules, par niveau.")]
        [SerializeField]
        float[] m_IntervalPerLevel = { 9f, 8f, 8f };

        [Tooltip("Boules posées en même temps, au plus (sans la boule immense).")]
        [SerializeField]
        int m_MaxGlobes = 2;

        [Tooltip("Rayon (m) de la boule posée, par niveau.")]
        [SerializeField]
        float[] m_RadiusPerLevel = { 2.6f, 3.2f, 3.2f };

        [Tooltip("Temps (s) de la boule posée, par niveau.")]
        [SerializeField]
        float[] m_LifetimePerLevel = { 6f, 7f, 7f };

        [Tooltip("Temps (s) pour qu'un ennemi enfermé se fige en figurine (il ralentit jusque-là).")]
        [SerializeField]
        float m_FreezeTime = 2f;

        [SerializeField]
        float m_DropHeight = 14f;

        [SerializeField]
        float m_DropTime = 0.6f;

        [Header("Briser la boule")]
        [Tooltip("Dégâts de chaque figurine quand ta flèche brise la boule, par niveau (vague 1, en Normal).")]
        [SerializeField]
        float[] m_ShatterDamagePerLevel = { 90f, 120f, 120f };

        [Tooltip("Dégâts de chaque figurine quand la boule se fend d'elle-même, au bout de son temps, par niveau.")]
        [SerializeField]
        float[] m_CrackDamagePerLevel = { 25f, 35f, 35f };

        [Header("La boule secouée (niveau 2)")]
        [SerializeField]
        float m_ShakeTime = 2f;

        [Tooltip("Dégâts quand deux figurines se heurtent en tourbillonnant.")]
        [SerializeField]
        float m_ShakeHitDamage = 12f;

        [Tooltip("Vitesse (degrés/s) du tourbillon, au plus fort.")]
        [SerializeField]
        float m_ShakeSpin = 320f;

        [Header("La boule immense (niveau 3)")]
        [SerializeField]
        float m_GiantInterval = 18f;

        [SerializeField]
        float m_GiantRadius = 9f;

        [SerializeField]
        float m_GiantLifetime = 10f;

        [Tooltip("Il lui faut au moins tant d'ennemis dessous.")]
        [SerializeField]
        int m_GiantMinimum = 3;

        [Tooltip("Fendue d'elle-même, elle coûte cette part de leurs PV max à ses figurines (brisée par ta flèche, elles meurent).")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_GiantCrackShare = 0.4f;

        [SerializeField]
        Color m_Color = new Color(0.8f, 0.93f, 1f, 1f);

        [Header("Sons")]
        [SerializeField]
        AudioClip m_DropClip;

        [SerializeField]
        AudioClip m_ShakeClip;

        [SerializeField]
        AudioClip m_ShatterClip;

        class Globe
        {
            public Transform Transform;
            public DuoShotTarget Target;
            public ParticleSystem Snow;
            public int Level;
            public bool Giant;
            public float Radius;
            public Vector3 Ground;
            public bool Dropping = true;
            public float Time;
            public float Landed;
            public float Until;
            public bool Shaken;
            public float ShakeUntil;
            public float NextBump;
            public float NextKeepOut;
            public readonly List<Enemy> Inside = new List<Enemy>();

            public Vector3 Rest => Ground + Vector3.up * Radius * 0.82f;
        }

        class Figurine
        {
            public Globe Globe;
            public float Since;
            public bool Frozen;
            public bool Swirling;
            public float Angle;
            public float Distance;
            public float Lift;
        }

        readonly List<Globe> m_Globes = new List<Globe>();
        readonly Dictionary<Enemy, Figurine> m_Inside = new Dictionary<Enemy, Figurine>();
        readonly List<Enemy> m_Scratch = new List<Enemy>();
        float m_NextScan;
        float m_NextShot;
        float m_NextGiant;
        float m_TemplateShake;
        Quaternion m_TemplateRest;
        bool m_HasRest;
        ParticleSystem m_TowerSnow;

        protected override void OnDestroy()
        {
            // Retiré (la vitrine, F4) : les figurines sont relâchées, les boules s'en vont.
            foreach (var pair in m_Inside)
                Free(pair.Key);
            m_Inside.Clear();
            foreach (var globe in m_Globes)
                Remove(globe);
            m_Globes.Clear();
            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();
            for (var i = m_Globes.Count - 1; i >= 0; i--)
            {
                if (!Step(m_Globes[i]))
                    m_Globes.RemoveAt(i);
            }

            UpdateTemplate();
            if (!IsBuilt || m_GlobeTemplate == null || Time.time < m_NextScan)
                return;
            m_NextScan = Time.time + 0.25f;

            var level = Mathf.Max(1, Level);
            if (level >= 3 && Time.time >= m_NextGiant && !HasGiant() && TryDrop(level, true, m_GiantMinimum))
            {
                m_NextGiant = Time.time + m_GiantInterval / OverchargeRate;
                m_NextShot = Mathf.Max(m_NextShot, Time.time + 3f);
                return;
            }

            if (Time.time < m_NextShot || SmallCount() >= m_MaxGlobes)
                return;
            if (TryDrop(level, false, 1))
                m_NextShot = Time.time + ValueFor(m_IntervalPerLevel, level, 9f) / OverchargeRate;
        }

        bool HasGiant()
        {
            foreach (var globe in m_Globes)
            {
                if (globe.Giant)
                    return true;
            }

            return false;
        }

        int SmallCount()
        {
            var count = 0;
            foreach (var globe in m_Globes)
            {
                if (!globe.Giant)
                    count++;
            }

            return count;
        }

        bool TryDrop(int level, bool giant, int minimum)
        {
            var radius = giant ? m_GiantRadius : ValueFor(m_RadiusPerLevel, level, 2.6f);
            if (!DensestGroup(radius, out var center, out var count) || count < minimum)
                return false;

            // La boule immense se pose sur la voie, au milieu.
            if (giant)
            {
                var lane = LaneNearest(center, null, out var along, out var distance);
                if (lane != null && distance < 8f)
                    center = lane.PointAt(along, out _);
            }

            center.y = GroundY(center, 1.5f);
            Drop(center, radius, level, giant);
            return true;
        }

        // Le groupe le plus serré à portée (pas déjà enfermé) : le centre des ennemis autour de celui qui en a le plus près de lui.
        bool DensestGroup(float radius, out Vector3 center, out int count)
        {
            center = transform.position;
            count = 0;
            m_Scratch.Clear();
            foreach (var enemy in EnemiesNear(transform.position, m_Range))
            {
                if (!enemy.IsBoss && enemy.CanBeLaunched && !m_Inside.ContainsKey(enemy) && !InsideAnyGlobe(enemy.transform.position))
                    m_Scratch.Add(enemy);
            }

            var reach = radius * 0.75f;
            foreach (var a in m_Scratch)
            {
                var n = 0;
                var sum = Vector3.zero;
                foreach (var b in m_Scratch)
                {
                    var flat = b.transform.position - a.transform.position;
                    flat.y = 0f;
                    if (flat.sqrMagnitude > reach * reach)
                        continue;
                    n++;
                    sum += b.transform.position;
                }

                if (n <= count)
                    continue;
                count = n;
                center = sum / n;
            }

            return count > 0;
        }

        bool InsideAnyGlobe(Vector3 point)
        {
            foreach (var globe in m_Globes)
            {
                var flat = point - globe.Ground;
                flat.y = 0f;
                if (flat.sqrMagnitude < globe.Radius * globe.Radius)
                    return true;
            }

            return false;
        }

        // Une boule de verre tombe du ciel sur ce point.
        void Drop(Vector3 ground, float radius, int level, bool giant)
        {
            var copy = Instantiate(m_GlobeTemplate.gameObject, ground + Vector3.up * (m_DropHeight + radius), Quaternion.identity);
            copy.name = giant ? "Boule à neige immense" : "Boule à neige";
            copy.SetActive(true);
            foreach (var behaviour in copy.GetComponentsInChildren<MonoBehaviour>())
                Destroy(behaviour);
            foreach (var collider in copy.GetComponentsInChildren<Collider>())
                Destroy(collider);

            copy.transform.localScale = m_GlobeTemplate.lossyScale * (2f * radius / Mathf.Max(0.01f, m_TemplateDiameter));
            var sphere = copy.AddComponent<SphereCollider>();
            sphere.radius = m_TemplateDiameter * 0.5f / Mathf.Max(0.01f, m_GlobeTemplate.lossyScale.x);
            var globe = new Globe { Transform = copy.transform, Level = level, Giant = giant, Radius = radius, Ground = ground };
            globe.Target = copy.AddComponent<DuoShotTarget>();
            globe.Target.Shot = OnGlobeShot;
            copy.transform.position = globe.Rest + Vector3.up * m_DropHeight;
            m_Globes.Add(globe);
            m_TemplateShake = 0.6f;
        }

        bool Step(Globe globe)
        {
            if (globe.Transform == null)
                return false;

            var rest = globe.Rest;
            if (globe.Dropping)
            {
                globe.Time += Time.deltaTime;
                var t = Mathf.Clamp01(globe.Time / m_DropTime);
                globe.Transform.position = Vector3.Lerp(rest + Vector3.up * m_DropHeight, rest, t * t);
                if (t >= 1f)
                    Land(globe);
                return true;
            }

            if (globe.Level >= 2 && !globe.Shaken && Time.time >= Mathf.Lerp(globe.Landed, globe.Until, 0.5f))
                StartShake(globe);

            var shaking = Time.time < globe.ShakeUntil;
            if (shaking)
            {
                var jitter = Mathf.Min(0.25f, 0.05f * globe.Radius);
                globe.Transform.position = rest + new Vector3(Mathf.Sin(Time.time * 47f), 0f, Mathf.Cos(Time.time * 41f)) * jitter;
                globe.Transform.rotation = Quaternion.Euler(Mathf.Sin(Time.time * 38f) * 5f, 0f, Mathf.Cos(Time.time * 33f) * 5f);
            }
            else
            {
                globe.Transform.position = rest;
                globe.Transform.rotation = Quaternion.Slerp(globe.Transform.rotation, Quaternion.identity, Time.deltaTime * 6f);
            }

            UpdateFigurines(globe, shaking);
            if (Time.time >= globe.NextKeepOut)
            {
                globe.NextKeepOut = Time.time + 0.2f;
                KeepOut(globe);
            }

            if (Time.time < globe.Until)
                return true;

            Crack(globe);
            return false;
        }

        // Elle touche le sol : ceux qui sont dessous sont enfermés (un boss est seulement englué), et il se met à neiger dedans.
        void Land(Globe globe)
        {
            globe.Dropping = false;
            globe.Landed = Time.time;
            globe.Until = Time.time + (globe.Giant ? m_GiantLifetime : ValueFor(m_LifetimePerLevel, globe.Level, 6f));
            var rest = globe.Rest;
            globe.Transform.position = rest;
            globe.Snow = Snowfall(rest, globe.Radius);
            Burst(globe.Ground + Vector3.up * 0.3f, Color.white, globe.Giant ? 220 : 60, 5f + globe.Radius, 0.3f + globe.Radius * 0.04f,
                  1.2f, 0.6f, false);
            Sfx.Play(m_DropClip, rest, 1f, globe.Giant ? 0.6f : Random.Range(0.95f, 1.1f));

            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(globe.Ground, globe.Radius - 0.3f));
            foreach (var enemy in m_Scratch)
            {
                if (m_Inside.ContainsKey(enemy))
                    continue;
                if (enemy.IsBoss || !enemy.CanBeLaunched)
                {
                    enemy.Slow(0.3f, globe.Until - Time.time);
                    continue;
                }

                m_Inside[enemy] = new Figurine { Globe = globe, Since = Time.time };
                globe.Inside.Add(enemy);
            }

            if (globe.Giant)
            {
                FloatingText.Spawn(rest + Vector3.up * (globe.Radius + 1f), "La boule immense !", m_Color, 2.4f, 2.5f);
                HeadMessages.Show("La boule immense !\n<size=65%>Brise-la d'une flèche</size>", m_Color, 3f, 0.8f);
            }
            else if (globe.Inside.Count > 0)
            {
                FloatingText.Spawn(rest + Vector3.up * (globe.Radius + 0.5f), "Enfermés !", m_Color, 1.3f, 1.4f);
            }
        }

        // Dedans, ils ralentissent jusqu'à se figer ; un figé qui retombe du tourbillon se refige.
        void UpdateFigurines(Globe globe, bool shaking)
        {
            var announced = 0;
            for (var i = globe.Inside.Count - 1; i >= 0; i--)
            {
                var enemy = globe.Inside[i];
                if (enemy == null || !enemy.IsAlive || !m_Inside.TryGetValue(enemy, out var figurine))
                {
                    if (enemy != null)
                    {
                        Free(enemy);
                        m_Inside.Remove(enemy);
                    }

                    globe.Inside.RemoveAt(i);
                    continue;
                }

                if (figurine.Swirling)
                {
                    if (shaking)
                    {
                        Swirl(globe, enemy, figurine);
                        continue;
                    }

                    figurine.Swirling = false;
                    enemy.Release(Vector3.down * 0.5f);
                    continue;
                }

                if (!figurine.Frozen)
                {
                    var t = (Time.time - figurine.Since) / Mathf.Max(0.1f, m_FreezeTime);
                    if (t < 1f)
                    {
                        enemy.Slow(Mathf.Lerp(0.8f, 0.1f, t), 0.3f);
                        KeepIn(globe, enemy);
                        continue;
                    }

                    figurine.Frozen = true;
                    Freeze(globe, enemy, announced++ < 4);
                    continue;
                }

                if (!enemy.IsAirborne && !enemy.IsFrozen)
                    Freeze(globe, enemy, false);
            }

            if (!shaking || Time.time < globe.NextBump)
                return;

            // Dans le tourbillon, deux figurines qui se heurtent se blessent.
            globe.NextBump = Time.time + 0.3f;
            for (var a = 0; a < globe.Inside.Count; a++)
            {
                var first = globe.Inside[a];
                for (var b = a + 1; b < globe.Inside.Count; b++)
                {
                    var second = globe.Inside[b];
                    if ((first.transform.position - second.transform.position).sqrMagnitude > 1.2f * 1.2f)
                        continue;
                    Hurt(first, m_ShakeHitDamage, first.Center, first.transform.position - second.transform.position);
                    Hurt(second, m_ShakeHitDamage, second.Center, second.transform.position - first.transform.position);
                    Burst((first.Center + second.Center) * 0.5f, Color.white, 8, 3f, 0.15f, 0.4f, 0.3f);
                }
            }
        }

        void Freeze(Globe globe, Enemy enemy, bool announce)
        {
            var remaining = Mathf.Max(0.5f, globe.Until - Time.time + 0.5f);
            enemy.Freeze(remaining);
            enemy.Prepare(Preparation.Frozen, remaining);
            Burst(enemy.Center, Color.white, 14, 2f, 0.2f, 0.6f, 0.1f);
            if (announce)
                FloatingText.Spawn(enemy.Center + Vector3.up * 1.2f, "Figé !", m_Color, 0.9f, 1f);
        }

        // Pas encore figé, il marche encore : la paroi le renvoie au milieu.
        static void KeepIn(Globe globe, Enemy enemy)
        {
            var offset = enemy.transform.position - globe.Ground;
            offset.y = 0f;
            if (offset.sqrMagnitude > (globe.Radius - 0.7f) * (globe.Radius - 0.7f))
                enemy.Push(-offset.normalized * 3f);
        }

        // Ceux du dehors ne peuvent pas y entrer : la paroi les repousse.
        void KeepOut(Globe globe)
        {
            foreach (var enemy in EnemiesNear(globe.Ground, globe.Radius + 0.3f))
            {
                if (enemy.IsBoss || m_Inside.ContainsKey(enemy))
                    continue;
                var offset = enemy.transform.position - globe.Ground;
                offset.y = 0f;
                enemy.Push((offset.sqrMagnitude > 1e-4f ? offset.normalized : Vector3.forward) * 4f);
            }
        }

        // La boule se secoue : les figurines dégèlent, s'envolent et tourbillonnent dans la neige.
        void StartShake(Globe globe)
        {
            globe.Shaken = true;
            globe.ShakeUntil = Time.time + m_ShakeTime;
            var rest = globe.Rest;
            FloatingText.Spawn(rest + Vector3.up * (globe.Radius + 0.5f), "Secouée !", m_Color, globe.Giant ? 2f : 1.2f, 1.2f);
            Sfx.Play(m_ShakeClip, rest, 1f, globe.Giant ? 0.7f : Random.Range(0.95f, 1.1f));
            Burst(rest, Color.white, globe.Giant ? 200 : 60, 2f + globe.Radius * 0.6f, 0.15f, 1.5f, 0.05f);
            foreach (var enemy in globe.Inside)
            {
                if (enemy == null || !enemy.IsAlive || !m_Inside.TryGetValue(enemy, out var figurine) || enemy.IsAirborne)
                    continue;

                enemy.Thaw();
                if (!enemy.Hold(this))
                    continue;
                var offset = enemy.transform.position - globe.Ground;
                offset.y = 0f;
                figurine.Swirling = true;
                figurine.Angle = Mathf.Atan2(offset.z, offset.x);
                figurine.Distance = Mathf.Clamp(offset.magnitude, 0.6f, Mathf.Max(0.7f, globe.Radius - 0.8f));
                figurine.Lift = Random.Range(0.4f, Mathf.Min(2.5f, globe.Radius * 0.45f));
            }
        }

        // Le tourbillon : ils tournent autour du milieu, montent et redescendent, en toupie.
        void Swirl(Globe globe, Enemy enemy, Figurine figurine)
        {
            var progress = Mathf.Clamp01(1f - (globe.ShakeUntil - Time.time) / Mathf.Max(0.1f, m_ShakeTime));
            var bell = Mathf.Sin(Mathf.PI * progress);
            figurine.Angle += m_ShakeSpin * Mathf.Deg2Rad * bell * Time.deltaTime * (globe.Giant ? 0.45f : 1f);
            var around = new Vector3(Mathf.Cos(figurine.Angle), 0f, Mathf.Sin(figurine.Angle)) * figurine.Distance;
            enemy.transform.position = globe.Ground + around + Vector3.up * (figurine.Lift * bell);
            enemy.transform.Rotate(0f, 420f * bell * Time.deltaTime, 0f, Space.World);
            enemy.KeepHeld();
        }

        void OnGlobeShot(DuoShotTarget target)
        {
            for (var i = 0; i < m_Globes.Count; i++)
            {
                var globe = m_Globes[i];
                if (globe.Target != target || globe.Dropping)
                    continue;
                Shatter(globe);
                m_Globes.RemoveAt(i);
                return;
            }
        }

        // Ta flèche brise la boule : toutes les figurines éclatent (la boule immense : elles en meurent).
        void Shatter(Globe globe)
        {
            var rest = globe.Rest;
            FloatingText.Spawn(rest + Vector3.up * (globe.Radius + 0.6f), globe.Giant ? "Toutes éclatées !" : "Éclatées !", m_Color,
                               globe.Giant ? 2.4f : 1.5f, 1.5f);
            var damage = ValueFor(m_ShatterDamagePerLevel, globe.Level, 90f);
            m_Scratch.Clear();
            m_Scratch.AddRange(globe.Inside);
            foreach (var enemy in m_Scratch)
            {
                if (enemy == null)
                    continue;
                Free(enemy);
                m_Inside.Remove(enemy);
                if (!enemy.IsAlive)
                    continue;
                var amount = globe.Giant ? enemy.Health.Max * 3f / Mathf.Max(0.01f, Strength) : damage;
                Burst(enemy.Center, new Color(0.75f, 0.92f, 1f, 1f), 24, 6f, 0.25f, 0.8f, 1f);
                if (Hurt(enemy, amount, enemy.Center, enemy.transform.position - globe.Ground))
                    KnockDown(enemy, globe.Ground, 3f, 5f);
            }

            Break(globe, 1f);
        }

        // Au bout de son temps, elle se fend : les figurines sont relâchées, un peu blessées (la boule immense : 40 % de leurs PV).
        void Crack(Globe globe)
        {
            var damage = ValueFor(m_CrackDamagePerLevel, globe.Level, 25f);
            m_Scratch.Clear();
            m_Scratch.AddRange(globe.Inside);
            foreach (var enemy in m_Scratch)
            {
                if (enemy == null)
                    continue;
                Free(enemy);
                m_Inside.Remove(enemy);
                if (!enemy.IsAlive)
                    continue;
                var amount = damage + (globe.Giant ? enemy.Health.Max * m_GiantCrackShare / Mathf.Max(0.01f, Strength) : 0f);
                Hurt(enemy, amount, enemy.Center, Vector3.up);
            }

            Break(globe, 0.6f);
        }

        // Le verre vole en éclats, la neige retombe.
        void Break(Globe globe, float strength)
        {
            var rest = globe.Rest;
            var count = Mathf.RoundToInt((globe.Giant ? 260 : 90) * strength);
            Burst(rest, new Color(0.85f, 0.95f, 1f, 1f), count, (4f + globe.Radius * 1.5f) * strength, 0.2f + globe.Radius * 0.03f, 1.1f, 1f);
            Burst(rest, Color.white, count / 2, 2f + globe.Radius, 0.25f, 1.6f, 0.2f);
            Sfx.Play(m_ShatterClip, rest, strength, globe.Giant ? 0.65f : Random.Range(0.95f, 1.15f));
            Remove(globe);
        }

        void Remove(Globe globe)
        {
            globe.Inside.Clear();
            if (globe.Snow != null)
                Destroy(globe.Snow.gameObject);
            if (globe.Transform != null)
                Destroy(globe.Transform.gameObject);
            globe.Snow = null;
            globe.Transform = null;
        }

        static void Free(Enemy enemy)
        {
            if (enemy == null)
                return;
            enemy.Unprepare(Preparation.Frozen);
            enemy.Thaw();
            if (enemy.IsHeld)
                enemy.Release(Vector3.zero);
        }

        // La boule du modèle : il y neige toujours ; elle tremble quand elle lance une boule.
        void UpdateTemplate()
        {
            if (m_GlobeTemplate == null)
                return;
            if (!m_HasRest)
            {
                m_TemplateRest = m_GlobeTemplate.localRotation;
                m_HasRest = true;
            }

            if (m_TowerSnow == null && IsBuilt)
            {
                var radius = m_TemplateDiameter * 0.5f * m_GlobeTemplate.lossyScale.x;
                m_TowerSnow = Snowfall(m_GlobeTemplate.position, radius * 0.9f);
                if (m_TowerSnow != null)
                    m_TowerSnow.transform.SetParent(transform, true);
            }

            m_TemplateShake = Mathf.Max(0f, m_TemplateShake - Time.deltaTime);
            var shake = m_TemplateShake * 10f;
            m_GlobeTemplate.localRotation = m_TemplateRest * Quaternion.Euler(Mathf.Sin(Time.time * 40f) * shake, 0f,
                                                                               Mathf.Cos(Time.time * 36f) * shake);
        }

        // La neige qui tombe doucement dans une boule de ce rayon (le matériau d'effet des flèches).
        static ParticleSystem Snowfall(Vector3 center, float radius)
        {
            var arrows = SpecialArrows.Instance;
            var material = arrows != null ? arrows.EffectMaterial : null;
            if (material == null)
                return null;

            var go = new GameObject("Neige de la boule");
            go.transform.position = center;
            var particles = go.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var size = Mathf.Sqrt(Mathf.Max(0.3f, radius) / 2.6f);
            var main = particles.main;
            main.loop = true;
            main.duration = 1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 3.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f * size, 0.14f * size);
            main.startColor = Color.white;
            main.gravityModifier = 0.02f;
            main.maxParticles = Mathf.Clamp(Mathf.RoundToInt(80f * radius), 40, 900);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission;
            emission.rateOverTime = 25f * radius;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius * 0.8f;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            particles.Play();
            return particles;
        }

        /// <summary>La portée dessinée pendant la pose (3 novembre, le placement lisible).</summary>
        public override float ReachAt(int level) => m_Range;
    }
}
