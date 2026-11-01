using System.Collections;
using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.Player;
using Archery.UI;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Le trio <b>Grande bombarde</b> (Feu d'artifice + Tremplin, 1er novembre au soir ; <c>Docs/Refonte_TowerDefense.md</c>, 12.28) :
    /// un mortier de feu d'artifice géant, en bronze, au bord de la voie, posé sur quatre gros ressorts. Il se <b>remplit de poudre</b>
    /// (ses cinq cercles rougeoient un à un) : chaque ennemi tué autour de lui, et chaque flèche tirée dans sa gueule. <b>C'est toi qui
    /// choisis le moment</b> : ta flèche dans la cible de la mèche l'allume, et plus il est plein, plus la salve est grosse ; ses bombes
    /// de feu d'artifice tombent sur les groupes, explosent en gerbes de couleur et projettent les ennemis (le tremplin).
    /// <list type="bullet">
    /// <item>1 : jusqu'à 12 bombes, selon la poudre ;</item>
    /// <item>2 : il se remplit deux fois plus vite ; au tir, il rebondit si fort sur ses ressorts que les ennemis proches tombent ;</item>
    /// <item>3, <b>le bouquet final</b> : plein à ras bord, il n'attend plus ; il déborde 3 s, puis tire 40 bombes sur toutes les
    /// voies à portée.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Le joueur, le 1er novembre au soir : le Volcan était à revoir en entier ; de trois propositions, il garde la mécanique du
    /// « Réveil » (une jauge qui se remplit, ta flèche qui déclenche) et oublie le volcan. La gueule et la cible de la mèche reçoivent
    /// les flèches (<see cref="DuoShotTarget"/> : la flèche disparaît dedans) ; leurs colliders sont ajoutés une fois le trio construit
    /// (les bâtiments n'en ont pas). Les ennemis tués par ses propres bombes ne le remplissent pas (il se rechargerait tout seul). Les
    /// boss ne sont pas projetés (ils prennent les dégâts) ; les volants ne sont pas visés.
    /// </remarks>
    public class TrioBombarde : TowerTrio
    {
        // La hauteur des ressorts du modèle (BO_SPRING de Tools/allure_tours/trios.py) : le corps qui rebondit les étire d'autant.
        const float k_SpringLength = 0.75f;

        [Header("La bombarde")]
        [Tooltip("Le corps qui rebondit sur les ressorts (l'affût, le fût, les bombes).")]
        [SerializeField]
        Transform m_Body;

        [Tooltip("Les ressorts (repère à leur pied) : étirés avec le corps.")]
        [SerializeField]
        Transform m_Springs;

        [Tooltip("La gueule : elle avale tes flèches, les bombes en partent.")]
        [SerializeField]
        Transform m_Muzzle;

        [Tooltip("La lumière de la culasse : l'étincelle de la mèche y arrive.")]
        [SerializeField]
        Transform m_Touchhole;

        [Tooltip("La cible de la mèche (le disque face à son -Z) : elle se tourne vers toi ; ta flèche dedans allume la mèche.")]
        [SerializeField]
        Transform m_FuseTarget;

        [Tooltip("Les cercles de la jauge, du bas vers le haut : ils rougeoient à mesure que la poudre monte.")]
        [SerializeField]
        Transform[] m_GaugeRings = new Transform[0];

        [Tooltip("Les bombes du modèle (repère en bas, au milieu) : chaque tir en copie une.")]
        [SerializeField]
        Transform[] m_Bombs = new Transform[0];

        [Tooltip("Le matériau d'un cercle allumé (la braise).")]
        [SerializeField]
        Material m_GlowMaterial;

        [Header("La poudre")]
        [Tooltip("Rayon (m, à plat) où un ennemi tué la remplit.")]
        [SerializeField]
        float m_FeedRadius = 18f;

        [Tooltip("La poudre (sur 1) d'un ennemi tué autour d'elle, et d'une flèche dans sa gueule.")]
        [SerializeField]
        float m_KillCharge = 0.08f;

        [SerializeField]
        float m_ArrowCharge = 0.12f;

        [Tooltip("Elle se remplit tant de fois plus vite, par niveau.")]
        [SerializeField]
        float[] m_ChargeRatePerLevel = { 1f, 2f, 2f };

        [Tooltip("La poudre qu'il faut au moins pour que la mèche prenne.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_MinCharge = 0.15f;

        [Header("La salve")]
        [Tooltip("Portée (m, à plat) des bombes.")]
        [SerializeField]
        float m_Range = 30f;

        [Tooltip("Bombes d'une salve pleine, par niveau (moins si elle n'est pas pleine).")]
        [SerializeField]
        int[] m_MaxBombsPerLevel = { 12, 14, 16 };

        [Tooltip("Temps (s) que l'étincelle met à courir le long de la mèche, et entre deux bombes.")]
        [SerializeField]
        float m_FuseTime = 0.9f;

        [SerializeField]
        float m_SalvoGap = 0.09f;

        [Tooltip("Durée (s) du vol d'une bombe, et hauteur (m) de sa cloche au-dessus du milieu du trajet.")]
        [SerializeField]
        float m_FlightTime = 1.5f;

        [SerializeField]
        float m_ArcHeight = 12f;

        [Tooltip("Rayon (m) de l'explosion d'une bombe ; ses dégâts, par niveau (vague 1, en Normal).")]
        [SerializeField]
        float m_BombRadius = 3f;

        [SerializeField]
        float[] m_BombDamagePerLevel = { 45f, 55f, 65f };

        [Tooltip("Les ennemis touchés sont projetés : loin du point (m/s) et vers le haut.")]
        [SerializeField]
        float m_Push = 3f;

        [SerializeField]
        float m_Lift = 6f;

        [Header("Le rebond (niveau 2)")]
        [Tooltip("Rayon (m) où les ennemis tombent quand elle rebondit au tir.")]
        [SerializeField]
        float m_ShakeRadius = 10f;

        [Header("Le bouquet final (niveau 3)")]
        [Tooltip("Pleine, elle déborde tant de secondes, puis tire tant de bombes en tant de secondes, sur tout ce qui est à portée.")]
        [SerializeField]
        float m_FinaleDelay = 3f;

        [SerializeField]
        int m_FinaleBombs = 40;

        [SerializeField]
        float m_FinaleTime = 2.5f;

        [SerializeField]
        float m_FinaleRange = 60f;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_FireClip;

        [SerializeField]
        AudioClip m_LaunchClip;

        [SerializeField]
        AudioClip m_BombClip;

        [SerializeField]
        AudioClip m_FeedClip;

        [SerializeField]
        AudioClip m_RumbleClip;

        [SerializeField]
        AudioClip m_FuseClip;

        // Les couleurs des bombes du modèle, dans l'ordre de m_Bombs (bleue, rouge, verte), puis l'or.
        static readonly Color[] k_Colors =
        {
            new Color(0.35f, 0.6f, 1f, 1f), new Color(1f, 0.3f, 0.25f, 1f), new Color(0.35f, 1f, 0.45f, 1f), new Color(1f, 0.82f, 0.35f, 1f),
        };

        static readonly Color k_Gold = new Color(1f, 0.82f, 0.35f, 1f);
        static readonly Color k_Ember = new Color(1f, 0.55f, 0.2f, 1f);
        static readonly Color k_Smoke = new Color(0.55f, 0.53f, 0.5f, 1f);

        class Bomb
        {
            public Transform Body;
            public Vector3 From;
            public Vector3 Control;
            public Vector3 To;
            public float Time;
            public float Duration;
            public int Level;
            public Color Color;
            public bool Finale;
            public bool Loud;
            public Vector3 Spin;
        }

        readonly List<Bomb> m_Flying = new List<Bomb>();
        readonly List<Vector3> m_Targets = new List<Vector3>();
        readonly List<Enemy> m_Pool = new List<Enemy>();
        readonly List<Enemy> m_Scratch = new List<Enemy>();
        Renderer[] m_RingRenderers = new Renderer[0];
        Material[] m_ColdMaterials = new Material[0];
        Material m_Glow;
        DuoShotTarget m_MouthShot;
        DuoShotTarget m_FuseShot;
        float m_Charge;
        int m_Lit = -1;
        bool m_Lighting;
        bool m_FullTold;
        float m_FinaleAt = -1f;
        float m_NextSmoke;
        float m_BounceTime = 10f;
        float m_BounceAmplitude;
        Vector3 m_BodyBase;
        Vector3 m_SpringsBase;

        /// <summary>La poudre, de 0 à 1.</summary>
        public float Charge => m_Charge;

        protected override void Awake()
        {
            base.Awake();
            if (m_Body != null)
                m_BodyBase = m_Body.localPosition;
            if (m_Springs != null)
                m_SpringsBase = m_Springs.localScale;

            m_RingRenderers = new Renderer[m_GaugeRings.Length];
            m_ColdMaterials = new Material[m_GaugeRings.Length];
            for (var i = 0; i < m_GaugeRings.Length; i++)
            {
                m_RingRenderers[i] = m_GaugeRings[i] != null ? m_GaugeRings[i].GetComponentInChildren<Renderer>() : null;
                m_ColdMaterials[i] = m_RingRenderers[i] != null ? m_RingRenderers[i].sharedMaterial : null;
            }

            // La braise de la palette n'est qu'une couleur : sa copie brille (une lueur, sans lumière en plus).
            if (m_GlowMaterial != null)
            {
                m_Glow = new Material(m_GlowMaterial) { name = m_GlowMaterial.name + " (lueur)" };
                if (m_Glow.HasProperty("_EmissionColor"))
                {
                    m_Glow.EnableKeyword("_EMISSION");
                    m_Glow.SetColor("_EmissionColor", k_Ember * 1.6f);
                }
            }
        }

        void OnEnable() => Enemy.Killed += OnKilled;

        void OnDisable() => Enemy.Killed -= OnKilled;

        protected override void OnDestroy()
        {
            foreach (var bomb in m_Flying)
            {
                if (bomb.Body != null)
                    Destroy(bomb.Body.gameObject);
            }

            m_Flying.Clear();
            if (m_Glow != null)
                Destroy(m_Glow);
            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();
            UpdateBombs();
            UpdateBounce();
            if (!IsBuilt || IsUnderConstruction)
                return;

            Arm();
            FaceFuse();
            UpdateGauge();
            Smoke();

            if (m_FinaleAt > 0f && Time.time >= m_FinaleAt)
            {
                m_FinaleAt = -1f;
                Fire(Mathf.Max(1, Level), true);
            }
        }

        // ---------------------------------------------------------------- la poudre

        // Un ennemi tué autour d'elle la remplit (pas ceux de ses propres bombes).
        void OnKilled(Enemy enemy, DamageInfo info)
        {
            if (!IsBuilt || IsUnderConstruction || enemy == null || ReferenceEquals(info.Source, this))
                return;

            var flat = enemy.transform.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude <= m_FeedRadius * m_FeedRadius)
                AddCharge(m_KillCharge);
        }

        void AddCharge(float amount)
        {
            var level = Mathf.Max(1, Level);
            m_Charge = Mathf.Min(1f, m_Charge + amount * ValueFor(m_ChargeRatePerLevel, level, 1f) * OverchargeRate);
            if (m_Charge < 1f)
                return;

            // Pleine : au niveau 3, elle déborde et n'attend plus ; sinon elle attend ta flèche dans la mèche.
            if (level >= 3)
            {
                if (m_FinaleAt < 0f && !m_Lighting)
                {
                    m_FinaleAt = Time.time + m_FinaleDelay;
                    HeadMessages.Show("La bombarde déborde !", k_Ember, 2.5f);
                    FloatingText.Spawn(MuzzlePoint + Vector3.up * 1.5f, "Elle déborde !", k_Ember, 2f, m_FinaleDelay);
                    Sfx.Play(m_RumbleClip, MuzzlePoint, 1f, 0.5f);
                }

                return;
            }

            if (m_FullTold)
                return;
            m_FullTold = true;
            FloatingText.Spawn(MuzzlePoint + Vector3.up * 1.5f, "Pleine ! Tire dans la mèche", k_Gold, 1.8f, 2.5f);
        }

        // Les cercles s'allument à mesure que la poudre monte ; pleine (en attendant ta flèche), le dernier clignote.
        void UpdateGauge()
        {
            var count = m_RingRenderers.Length;
            if (count == 0 || m_Glow == null)
                return;

            var lit = Mathf.Clamp(Mathf.FloorToInt(m_Charge * count + 1e-4f), 0, count);
            var blink = m_Charge >= 1f && Mathf.Repeat(Time.time, 0.5f) < 0.25f;
            if (lit == m_Lit && m_Charge < 1f)
                return;

            if (lit > m_Lit && m_Lit >= 0)
            {
                Sfx.Play(m_RumbleClip, MuzzlePoint, 0.7f, 0.55f + 0.12f * lit);
                Burst(MuzzlePoint, k_Smoke, 10 + lit * 4, 2f, 0.6f, 1.6f, -0.15f, false);
            }

            m_Lit = lit;
            for (var i = 0; i < count; i++)
            {
                var renderer = m_RingRenderers[i];
                if (renderer == null)
                    continue;
                var on = i < lit && !(blink && i == count - 1);
                var material = on ? m_Glow : m_ColdMaterials[i];
                if (renderer.sharedMaterial != material)
                    renderer.sharedMaterial = material;
            }
        }

        // De la fumée qui monte de la gueule, d'autant plus qu'elle est pleine.
        void Smoke()
        {
            if (m_Charge <= 0.01f || Time.time < m_NextSmoke)
                return;
            m_NextSmoke = Time.time + Mathf.Lerp(1.2f, 0.35f, m_Charge);
            Burst(MuzzlePoint, k_Smoke, 3 + Mathf.RoundToInt(m_Charge * 8f), 1.2f, 0.5f + m_Charge * 0.5f, 2f, -0.12f, false);
            if (m_Charge >= 0.6f)
                Burst(MuzzlePoint, k_Ember, 4, 2f, 0.12f, 0.8f, 0.3f, false);
        }

        Vector3 MuzzlePoint => m_Muzzle != null ? m_Muzzle.position : transform.position + Vector3.up * 7f;

        // ---------------------------------------------------------------- la gueule et la mèche

        // Les colliders de la gueule et de la cible, une fois construite : tes flèches y disparaissent.
        void Arm()
        {
            if (m_MouthShot != null)
                return;

            if (m_Muzzle != null)
            {
                var mouth = new GameObject("Gueule (flèches)");
                mouth.transform.SetParent(m_Muzzle, false);
                var sphere = mouth.AddComponent<SphereCollider>();
                sphere.radius = 1.2f;
                sphere.center = new Vector3(0f, -0.2f, 0f);
                m_MouthShot = mouth.AddComponent<DuoShotTarget>();
                m_MouthShot.Shot = OnMouthShot;
            }

            if (m_FuseTarget != null)
            {
                var box = m_FuseTarget.gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(1.6f, 1.6f, 0.3f);
                m_FuseShot = m_FuseTarget.gameObject.AddComponent<DuoShotTarget>();
                m_FuseShot.Shot = OnFuseShot;
            }
        }

        // La cible de la mèche se tourne vers toi (autour de la verticale).
        void FaceFuse()
        {
            var rig = PlayerRig.Instance;
            if (m_FuseTarget == null || rig == null || rig.Head == null)
                return;

            var away = m_FuseTarget.position - rig.Head.position;
            away.y = 0f;
            if (away.sqrMagnitude > 0.01f)
                m_FuseTarget.rotation = Quaternion.RotateTowards(m_FuseTarget.rotation, Quaternion.LookRotation(away), 120f * Time.deltaTime);
        }

        void OnMouthShot(DuoShotTarget target)
        {
            if (m_Lighting || m_FinaleAt > 0f)
                return;

            AddCharge(m_ArrowCharge);
            FloatingText.Spawn(MuzzlePoint + Vector3.up * 1f, $"Poudre {Mathf.RoundToInt(m_Charge * 100f)} %", k_Gold, 1.2f, 1.2f);
            Sfx.Play(m_FeedClip, MuzzlePoint, 0.8f, Random.Range(0.8f, 0.95f));
            Burst(MuzzlePoint, k_Smoke, 12, 2.5f, 0.5f, 1.2f, -0.1f, false);
        }

        void OnFuseShot(DuoShotTarget target)
        {
            var point = m_FuseTarget != null ? m_FuseTarget.position : transform.position;
            if (m_Lighting || m_FinaleAt > 0f)
                return;

            if (m_Charge < m_MinCharge)
            {
                FloatingText.Spawn(point + Vector3.up, "Pas assez de poudre", new Color(0.8f, 0.8f, 0.8f, 1f), 1.2f, 1.5f);
                return;
            }

            // Personne à portée : la poudre reste (la salve ne partirait sur rien).
            if (EnemiesNear(transform.position, m_Range).Count == 0)
            {
                FloatingText.Spawn(point + Vector3.up, "Personne à portée", new Color(0.8f, 0.8f, 0.8f, 1f), 1.2f, 1.5f);
                return;
            }

            StartCoroutine(Light(Mathf.Max(1, Level)));
        }

        // L'étincelle court le long de la mèche, de la cible à la lumière de la culasse ; puis la salve.
        IEnumerator Light(int level)
        {
            m_Lighting = true;
            var from = m_FuseTarget != null ? m_FuseTarget.position : transform.position;
            Sfx.Play(m_FuseClip, from, 0.8f, 1.4f);
            FloatingText.Spawn(from + Vector3.up, "La mèche !", k_Ember, 1.3f, 1f);
            var next = 0f;
            for (var t = 0f; t < m_FuseTime; t += Time.deltaTime)
            {
                if (t >= next)
                {
                    next = t + 0.04f;
                    var to = m_Touchhole != null ? m_Touchhole.position : MuzzlePoint;
                    Burst(Vector3.Lerp(from, to, t / m_FuseTime), k_Ember, 5, 1.5f, 0.1f, 0.4f, 0.4f);
                }

                yield return null;
            }

            m_Lighting = false;
            Fire(level, false);
        }

        // ---------------------------------------------------------------- la salve

        // La salve : d'autant plus de bombes qu'elle est pleine (le bouquet final : toutes), sur les groupes à portée.
        void Fire(int level, bool finale)
        {
            var share = finale ? 1f : m_Charge;
            var count = finale ? m_FinaleBombs : Mathf.Max(1, Mathf.RoundToInt(share * ValueFor(m_MaxBombsPerLevel, level, 12)));
            PickTargets(count, finale ? m_FinaleRange : m_Range, finale);
            if (m_Targets.Count == 0)
                return;

            m_Charge = 0f;
            m_FullTold = false;
            m_BounceTime = 0f;
            m_BounceAmplitude = finale ? 0.6f : 0.2f + 0.25f * share;
            Sfx.Play(m_FireClip, MuzzlePoint, 1f, finale ? 0.55f : Mathf.Lerp(1.1f, 0.75f, share));
            Burst(MuzzlePoint, k_Smoke, finale ? 80 : 30, finale ? 9f : 5f, 1f, 2.2f, -0.1f, false);
            Burst(MuzzlePoint, k_Gold, finale ? 120 : 40, finale ? 12f : 7f, 0.3f, 1f, 0.3f, false);
            FloatingText.Spawn(MuzzlePoint + Vector3.up * 2f, finale ? "Le bouquet final !" : $"Le bouquet ! ×{m_Targets.Count}", k_Gold,
                               finale ? 2.6f : 1.8f, 2f);
            if (finale)
                HeadMessages.Show("Le bouquet final !", k_Gold, 2.5f);

            // Niveau 2 : elle rebondit si fort sur ses ressorts que les ennemis proches tombent.
            if (level >= 2)
            {
                m_Scratch.Clear();
                m_Scratch.AddRange(EnemiesNear(transform.position, m_ShakeRadius));
                foreach (var enemy in m_Scratch)
                    KnockDown(enemy, transform.position, 3f, 5f);
                if (m_Scratch.Count > 0)
                    FloatingText.Spawn(transform.position + Vector3.up * 2f, "Secousse !", k_Ember, 1.4f, 1.2f);
            }

            StartCoroutine(Salvo(level, finale));
        }

        // Les points visés : le groupe le plus serré, puis le suivant (sans ceux du premier), et ainsi de suite ; les bombes en trop
        // vont aux plus gros groupes, à côté. Le bouquet final en met aussi sur les voies, au hasard, pour remplir le ciel.
        void PickTargets(int count, float range, bool finale)
        {
            m_Targets.Clear();
            m_Pool.Clear();
            m_Pool.AddRange(EnemiesNear(transform.position, range));
            var groups = finale ? Mathf.Max(1, count / 2) : count;
            while (m_Pool.Count > 0 && m_Targets.Count < groups)
            {
                Enemy best = null;
                var most = -1;
                foreach (var enemy in m_Pool)
                {
                    var near = 0;
                    foreach (var other in m_Pool)
                    {
                        if ((other.transform.position - enemy.transform.position).sqrMagnitude < 16f)
                            near++;
                    }

                    if (near > most)
                    {
                        most = near;
                        best = enemy;
                    }
                }

                var center = best.transform.position;
                m_Targets.Add(center);
                m_Pool.RemoveAll(enemy => (enemy.transform.position - center).sqrMagnitude < 3.5f * 3.5f);
            }

            var found = m_Targets.Count;
            var lanes = BuildGrid.Instance != null ? BuildGrid.Instance.Lanes : null;
            for (var i = found; i < count; i++)
            {
                if (finale && lanes != null && lanes.Count > 0 && (found == 0 || i % 2 == 0) && TryLanePoint(lanes, range, out var point))
                {
                    m_Targets.Add(point);
                    continue;
                }

                if (found == 0)
                    break;
                var circle = Random.insideUnitCircle * 2.5f;
                m_Targets.Add(m_Targets[i % found] + new Vector3(circle.x, 0f, circle.y));
            }
        }

        // Un point au hasard sur une voie, à portée.
        bool TryLanePoint(IReadOnlyList<Archery.World.MapScan.Lane> lanes, float range, out Vector3 point)
        {
            for (var attempt = 0; attempt < 6; attempt++)
            {
                var lane = lanes[Random.Range(0, lanes.Count)];
                if (lane == null || lane.Points.Count < 2)
                    continue;
                point = lane.PointAt(Random.Range(0f, lane.Length), out _);
                var flat = point - transform.position;
                flat.y = 0f;
                if (flat.sqrMagnitude <= range * range)
                    return true;
            }

            point = default;
            return false;
        }

        IEnumerator Salvo(int level, bool finale)
        {
            var targets = new List<Vector3>(m_Targets);
            var gap = finale ? m_FinaleTime / Mathf.Max(1, targets.Count) : m_SalvoGap;
            for (var i = 0; i < targets.Count; i++)
            {
                Launch(targets[i], level, finale, i);
                yield return new WaitForSeconds(gap);
            }
        }

        // Une bombe : la copie d'une bombe du modèle, qui part de la gueule en cloche vers le sol du point visé, en tournoyant.
        void Launch(Vector3 target, int level, bool finale, int index)
        {
            var from = MuzzlePoint;
            var to = target;
            to.y = GroundY(target, 1.5f);

            var pick = m_Bombs.Length > 0 ? Random.Range(0, m_Bombs.Length) : -1;
            var model = pick >= 0 ? m_Bombs[pick] : null;
            GameObject body;
            if (model != null)
            {
                body = Instantiate(model.gameObject, from, Random.rotation);
                body.transform.localScale = model.lossyScale * (finale ? 1.3f : 1f);
            }
            else
            {
                body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                body.transform.position = from;
                body.transform.localScale = Vector3.one * 0.7f;
            }

            foreach (var collider in body.GetComponentsInChildren<Collider>())
                Destroy(collider);
            body.name = "Bombe de la bombarde";
            body.SetActive(true);
            var color = k_Colors[pick >= 0 ? pick % (k_Colors.Length - 1) : k_Colors.Length - 1];
            if (finale && index % 4 == 3)
                color = k_Gold;
            DuoFeuArtifice.AddTrail(body.transform, color);

            var flat = to - from;
            flat.y = 0f;
            m_Flying.Add(new Bomb
            {
                Body = body.transform,
                From = from,
                Control = (from + to) * 0.5f + Vector3.up * (m_ArcHeight + flat.magnitude * 0.2f),
                To = to,
                Duration = m_FlightTime * Random.Range(0.85f, 1.15f) * (finale ? 1.2f : 1f),
                Level = level,
                Color = color,
                Finale = finale,
                Loud = !finale || index % 3 == 0,
                Spin = Random.onUnitSphere * Random.Range(200f, 500f),
            });
            if (index % 3 == 0)
                Sfx.Play(m_LaunchClip, from, 0.7f, Random.Range(0.8f, 1.1f));
        }

        void UpdateBombs()
        {
            for (var i = m_Flying.Count - 1; i >= 0; i--)
            {
                var bomb = m_Flying[i];
                if (bomb.Body == null)
                {
                    m_Flying.RemoveAt(i);
                    continue;
                }

                bomb.Time += Time.deltaTime;
                var t = Mathf.Clamp01(bomb.Time / bomb.Duration);
                var u = 1f - t;
                bomb.Body.position = u * u * bomb.From + 2f * u * t * bomb.Control + t * t * bomb.To;
                bomb.Body.Rotate(bomb.Spin * Time.deltaTime, Space.Self);
                if (t < 1f)
                    continue;

                m_Flying.RemoveAt(i);
                Destroy(bomb.Body.gameObject);
                Impact(bomb);
            }
        }

        // L'impact : une gerbe de feu d'artifice ; ceux qui sont autour sont blessés et projetés (pas les boss).
        void Impact(Bomb bomb)
        {
            var point = bomb.To;
            var big = bomb.Finale ? 1.25f : 1f;
            if (bomb.Loud)
            {
                Explosion(point, m_BombRadius * 0.8f * big);
                Sfx.Play(m_BombClip, point, 0.9f, Random.Range(0.75f, 1.05f));
            }

            Burst(point + Vector3.up * 0.6f, bomb.Color, (int)(60 * big), 8f * big, 0.35f, 1.1f, 0.4f);
            Burst(point + Vector3.up * 0.6f, Color.Lerp(bomb.Color, Color.white, 0.7f), 20, 4f, 0.25f, 1.4f, 0.6f);

            var damage = ValueFor(m_BombDamagePerLevel, bomb.Level, 45f);
            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(point, m_BombRadius * big));
            foreach (var enemy in m_Scratch)
            {
                if (Hurt(enemy, damage, enemy.Center, enemy.transform.position - point) && !enemy.IsBoss)
                    KnockDown(enemy, point, m_Push, m_Lift);
            }
        }

        // ---------------------------------------------------------------- le rebond

        // Au tir, le corps s'enfonce sur ses ressorts puis rebondit, de moins en moins (le tremplin) ; les ressorts suivent.
        void UpdateBounce()
        {
            if (m_BounceTime > 1.6f || m_Body == null)
                return;

            m_BounceTime += Time.deltaTime;
            var dy = m_BounceTime > 1.6f ? 0f : -m_BounceAmplitude * Mathf.Exp(-3.5f * m_BounceTime) * Mathf.Cos(14f * m_BounceTime);
            m_Body.localPosition = m_BodyBase + Vector3.up * dy;
            if (m_Springs != null)
                m_Springs.localScale = new Vector3(m_SpringsBase.x, m_SpringsBase.y * Mathf.Max(0.2f, (k_SpringLength + dy) / k_SpringLength),
                                                   m_SpringsBase.z);
        }
    }
}
