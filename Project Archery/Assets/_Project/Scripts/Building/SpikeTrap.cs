using System.Collections.Generic;
using Archery.Bows;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.Upgrades;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Building
{
    /// <summary>
    /// Pièges à piques sur la voie (GDD, section 9) : les ennemis au sol qui passent dessus perdent des PV à intervalles
    /// réguliers (12 / 18 / 27 toutes les 0,8 / 0,65 / 0,5 s à la vague 1 en Normal) ; au niveau 3, ils sont aussi ralentis.
    /// Les piques jaillissent du plateau à chaque coup. Les volants passent au-dessus. Depuis le 29 octobre (deuxième
    /// brainstorm, <c>Docs/Refonte_TowerDefense.md</c>, partie 12.6), cinq niveaux :
    /// <list type="bullet">
    /// <item>2, <b>barbelées</b> : les blessés saignent encore 3 s après ;</item>
    /// <item>3, le <b>champ de pieux</b> : deux fois plus large ;</item>
    /// <item>4, une <b>mine</b> cachée explose au premier passage et se réarme toutes les 20 s ; ta flèche la fait sauter ;</item>
    /// <item>5, la <b>mâchoire de la terre</b> : toutes les 12 s, la voie s'ouvre et des pieux de 4 m jaillissent sur 20 m : les
    /// petits sont tués net, les autres embrochés en l'air.</item>
    /// </list>
    /// </summary>
    public class SpikeTrap : Structure
    {
        [Tooltip("Dégâts à chaque coup, par niveau, à la vague 1 en Normal (ils suivent ensuite la force des ennemis).")]
        [SerializeField]
        float[] m_DamagePerLevel = { 12f, 18f, 27f };

        [Tooltip("Temps (s) entre deux coups, par niveau : plus lents au début, ils gardent leur cadence au niveau 3.")]
        [SerializeField]
        float[] m_IntervalPerLevel = { 0.8f, 0.65f, 0.5f };

        [Tooltip("Niveau à partir duquel les ennemis sur le piège sont aussi ralentis.")]
        [SerializeField]
        int m_SlowFromLevel = 3;

        [Tooltip("Vitesse des ennemis ralentis (0,8 = 20 % plus lents).")]
        [Range(0.1f, 1f)]
        [SerializeField]
        float m_SlowMultiplier = 0.8f;

        [Tooltip("Taille (m) de la zone piégée, en X et en Z, centrée sur le bâtiment.")]
        [SerializeField]
        Vector2 m_Size = new Vector2(2.6f, 2.6f);

        [Tooltip("Hauteur (m) du dessus du plateau, d'où sortent les piques.")]
        [SerializeField]
        float m_PlateTop = 0.1f;

        [Tooltip("Matériau des piques (générées au lancement).")]
        [SerializeField]
        Material m_SpikeMaterial;

        [Tooltip("Nombre de piques par côté.")]
        [SerializeField]
        int m_SpikesPerSide = 5;

        [Tooltip("Hauteur (m) des piques, par niveau.")]
        [SerializeField]
        float[] m_SpikeHeightPerLevel = { 0.3f, 0.38f, 0.46f };

        [SerializeField]
        AudioClip m_StabClip;

        [Header("Barbelées (niveau 2)")]
        [Tooltip("Dégâts du saignement toutes les 0,5 s, à la vague 1 en Normal.")]
        [SerializeField]
        float m_BleedDamage = 4f;

        [Tooltip("Durée (s) du saignement après le dernier coup.")]
        [SerializeField]
        float m_BleedTime = 3f;

        [Header("Champ de pieux (niveau 3)")]
        [Tooltip("La zone piégée est plus large d'autant (en X, en travers de la voie).")]
        [SerializeField]
        float m_WideScale = 2f;

        [Header("Mine (niveau 4)")]
        [Tooltip("La mine (son modèle), cachée quand elle a explosé.")]
        [SerializeField]
        Transform m_Mine;

        [Tooltip("Dégâts de la mine au centre, à la vague 1 en Normal.")]
        [SerializeField]
        float m_MineDamage = 60f;

        [Tooltip("Rayon (m) de l'explosion.")]
        [SerializeField]
        float m_MineRadius = 3f;

        [Tooltip("Temps (s) avant qu'elle se réarme.")]
        [SerializeField]
        float m_MineRearm = 20f;

        [Tooltip("Distance (m) à la mine d'un ennemi qui la déclenche, ou d'une flèche qui la fait sauter.")]
        [SerializeField]
        float m_MineTrigger = 1.3f;

        [SerializeField]
        AudioClip m_MineClip;

        [Header("Mâchoire de la terre (niveau 5)")]
        [Tooltip("Temps (s) entre deux mâchoires.")]
        [SerializeField]
        float m_JawInterval = 12f;

        [Tooltip("Longueur (m) de voie que couvre la mâchoire, le long de l'avant du piège.")]
        [SerializeField]
        float m_JawLength = 20f;

        [Tooltip("Hauteur (m) des pieux de la mâchoire.")]
        [SerializeField]
        float m_JawHeight = 4f;

        [Tooltip("Dégâts de la mâchoire, à la vague 1 en Normal : les petits n'y survivent pas.")]
        [SerializeField]
        float m_JawDamage = 120f;

        [Tooltip("Vitesse (m/s) vers le haut des survivants, embrochés en l'air.")]
        [SerializeField]
        float m_JawLaunch = 9f;

        [Tooltip("Part de ses PV max qu'y perd un boss.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_JawBossShare = 0.1f;

        [SerializeField]
        AudioClip m_JawClip;

        Transform m_Spikes;
        Transform m_Jaw;
        Vector2 m_SpikeArea;
        float m_Timer;
        float m_Pop;
        float m_JawTimer = 4f;
        float m_JawPop;
        float m_MineTimer;
        float m_BleedTimer;
        readonly Dictionary<Enemy, float> m_Bleeding = new Dictionary<Enemy, float>();
        readonly List<Enemy> m_Healed = new List<Enemy>();

        float Interval => ValueFor(m_IntervalPerLevel, Mathf.Max(1, Level), 0.5f);

        Vector2 Size => Level >= 3 ? new Vector2(m_Size.x * m_WideScale, m_Size.y) : m_Size;

        bool MineArmed => Level >= 4 && m_MineTimer <= 0f;

        protected override void Awake()
        {
            BuildSpikes(m_Size);
            base.Awake();
        }

        void OnEnable() => Arrow.AnyHit += OnArrowHit;

        void OnDisable() => Arrow.AnyHit -= OnArrowHit;

        protected override void Update()
        {
            base.Update();
            if (!IsBuilt)
                return;

            var deltaTime = Time.deltaTime;
            m_Timer -= deltaTime * OverchargeRate;
            if (m_Timer <= 0f)
            {
                m_Timer = Interval;
                Strike();
            }

            if (Level >= 2)
                Bleed(deltaTime);
            if (Level >= 4)
                UpdateMine(deltaTime);
            if (Level >= 5)
                UpdateJaw(deltaTime);

            // Les piques jaillissent d'un coup, puis rentrent doucement.
            m_Pop = Mathf.MoveTowards(m_Pop, 0f, deltaTime * 2.5f);
            PlaceSpikes();
        }

        protected override void OnLevelChanged(int level)
        {
            base.OnLevelChanged(level);
            if (Size != m_SpikeArea)
                BuildSpikes(Size);
            PlaceSpikes();
        }

        // La mâchoire n'est pas une partie du bâtiment : ni en plan bleu, ni dans le volume frappé au marteau.
        protected override bool IsBlueprintPart(Renderer renderer) => m_Jaw == null || !renderer.transform.IsChildOf(m_Jaw);

        void Strike()
        {
            var damage = ValueFor(m_DamagePerLevel, Level, 12f) * Strength;
            var slow = Level >= m_SlowFromLevel;
            var any = false;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive || !Contains(enemy.transform.position, Size, 0f))
                    continue;

                if (slow)
                    enemy.Slow(m_SlowMultiplier, Interval * 1.5f);
                Hit(enemy, damage, Vector3.up);
                if (Level >= 2 && enemy.IsAlive)
                    m_Bleeding[enemy] = Time.time + m_BleedTime;
                any = true;
            }

            if (!any)
                return;

            m_Pop = 1f;
            Sfx.Play(m_StabClip, transform.position + Vector3.up * 0.3f, 0.8f, Random.Range(0.9f, 1.1f));
        }

        // ---------------------------------------------------------------- niveau 2 : le saignement

        void Bleed(float deltaTime)
        {
            if (m_Bleeding.Count == 0)
                return;

            m_BleedTimer -= deltaTime;
            if (m_BleedTimer > 0f)
                return;

            m_BleedTimer = 0.5f;
            m_Healed.Clear();
            var damage = m_BleedDamage * Strength;
            foreach (var pair in m_Bleeding)
            {
                if (pair.Key == null || !pair.Key.IsAlive || Time.time > pair.Value)
                {
                    m_Healed.Add(pair.Key);
                    continue;
                }

                Hit(pair.Key, damage, Vector3.down);
            }

            foreach (var enemy in m_Healed)
                m_Bleeding.Remove(enemy);
        }

        // ---------------------------------------------------------------- niveau 4 : la mine

        void UpdateMine(float deltaTime)
        {
            if (m_MineTimer > 0f)
            {
                m_MineTimer -= deltaTime * OverchargeRate;
                if (m_MineTimer <= 0f && m_Mine != null)
                    m_Mine.gameObject.SetActive(true);
                return;
            }

            var center = MinePoint;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive || enemy.IsFlyer || enemy.IsAirborne)
                    continue;

                var offset = enemy.transform.position - center;
                offset.y = 0f;
                if (offset.sqrMagnitude <= m_MineTrigger * m_MineTrigger)
                {
                    ExplodeMine();
                    return;
                }
            }
        }

        Vector3 MinePoint => m_Mine != null ? m_Mine.position : transform.position;

        // Ta flèche qui touche la mine (ou tout près) la fait sauter.
        void OnArrowHit(ArrowHit hit)
        {
            if (!IsBuilt || !MineArmed || !hit.IsShot)
                return;
            if ((hit.Point - MinePoint).sqrMagnitude <= m_MineTrigger * m_MineTrigger)
                ExplodeMine();
        }

        // Comme la mine (StructureMine) : l'explosion de la flèche explosive ; ceux qu'elle touche volent.
        void ExplodeMine()
        {
            m_MineTimer = m_MineRearm;
            if (m_Mine != null)
                m_Mine.gameObject.SetActive(false);

            var center = MinePoint;
            var damage = m_MineDamage * Strength;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive)
                    continue;

                var offset = enemy.transform.position - center;
                var distance = offset.magnitude;
                if (distance > m_MineRadius)
                    continue;

                offset.y = 0f;
                var direction = offset.sqrMagnitude > 1e-4f ? offset.normalized : Vector3.forward;
                Hit(enemy, damage * Mathf.Lerp(1f, 0.5f, distance / m_MineRadius), direction);
                if (enemy.IsAlive)
                    enemy.Launch(direction * 3f + Vector3.up * 6f, this);
            }

            var arrows = SpecialArrows.Instance;
            if (arrows != null && arrows.ExplosionFireMaterial != null && arrows.ExplosionSmokeMaterial != null)
                ExplosionFx.Play(center + Vector3.up * 0.3f, m_MineRadius, arrows.ExplosionFireMaterial, arrows.ExplosionSmokeMaterial, null);
            Sfx.Play(m_MineClip != null ? m_MineClip : arrows != null ? arrows.ExplosionClip : null, center + Vector3.up * 0.5f);
        }

        // ---------------------------------------------------------------- niveau 5 : la mâchoire de la terre

        void UpdateJaw(float deltaTime)
        {
            if (m_Jaw == null)
                BuildJaw();

            // Les pieux jaillissent d'un coup, restent un instant, puis rentrent.
            m_JawPop = Mathf.MoveTowards(m_JawPop, 0f, deltaTime * 0.6f);
            var raised = Mathf.Clamp01(m_JawPop * 3f);
            m_Jaw.gameObject.SetActive(raised > 0.01f);
            m_Jaw.localScale = new Vector3(1f, m_JawHeight * raised, 1f);

            m_JawTimer -= deltaTime * OverchargeRate;
            if (m_JawTimer > 0f)
                return;

            var area = new Vector2(Size.x, m_JawLength);
            var caught = 0;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive || enemy.IsFlyer || enemy.IsAirborne || !Contains(enemy.transform.position, area, 0.5f))
                    continue;

                caught++;
                if (enemy.IsBoss)
                {
                    Hit(enemy, enemy.Health.Max * m_JawBossShare, Vector3.up);
                    continue;
                }

                Hit(enemy, m_JawDamage * Strength, Vector3.up);
                if (enemy.IsAlive)
                {
                    enemy.Launch(Vector3.up * m_JawLaunch + Random.insideUnitSphere * 1.5f, this);
                    enemy.Stun(1.5f);
                }
            }

            // Personne sur la voie : elle attend.
            if (caught == 0)
            {
                m_JawTimer = 0.4f;
                return;
            }

            m_JawTimer = m_JawInterval;
            m_JawPop = 1f;
            m_Pop = 1f;
            Sfx.Play(m_JawClip != null ? m_JawClip : m_StabClip, transform.position + Vector3.up, 1f, 0.6f);
        }

        // Une rangée de grands pieux sur toute la longueur de la mâchoire (rentrés : leur échelle Y est nulle).
        void BuildJaw()
        {
            var go = new GameObject("Mâchoire (effet)");
            m_Jaw = go.transform;
            m_Jaw.SetParent(transform, false);
            m_Jaw.localPosition = Vector3.up * m_PlateTop;
            var area = new Vector2(Size.x, m_JawLength);
            // Des pieux fins et serrés (29 octobre au soir, le joueur : trop épais), sur 20 m de voie (10 m avant).
            var mesh = SpikeMesh(area, Mathf.Max(2, Mathf.RoundToInt(area.x / 0.75f)), Mathf.Max(2, Mathf.RoundToInt(area.y / 0.75f)), 0.14f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_SpikeMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            go.SetActive(false);
        }

        // ---------------------------------------------------------------- outils

        // Les ennemis au sol dans cette zone, centrée sur le bâtiment (les volants sont bien plus haut).
        bool Contains(Vector3 position, Vector2 size, float margin)
        {
            var local = Quaternion.Inverse(transform.rotation) * (position - transform.position);
            return Mathf.Abs(local.x) <= size.x * 0.5f + margin && Mathf.Abs(local.z) <= size.y * 0.5f + margin &&
                   local.y > -0.6f && local.y < 1.2f;
        }

        void Hit(Enemy enemy, float damage, Vector3 direction) =>
            enemy.Health.TakeDamage(new DamageInfo
            {
                Amount = damage,
                Zone = HitZone.Body,
                Point = enemy.transform.position + Vector3.up * 0.3f,
                Direction = direction,
                Source = this,
            });

        // Au repos, seules les pointes dépassent du plateau ; à chaque coup, les piques sortent entièrement.
        void PlaceSpikes()
        {
            if (m_Spikes == null)
                return;

            var height = ValueFor(m_SpikeHeightPerLevel, Mathf.Max(1, Level), 0.3f);
            var raised = Mathf.Lerp(0.3f, 1f, Mathf.SmoothStep(0f, 1f, m_Pop));
            m_Spikes.localScale = new Vector3(1f, height, 1f);
            m_Spikes.localPosition = new Vector3(0f, m_PlateTop - height * (1f - raised), 0f);
        }

        // Une grille de piques pyramidales, à facettes plates comme les modèles du kit (refaite plus large au niveau 3).
        void BuildSpikes(Vector2 size)
        {
            if (m_Spikes == null)
            {
                var go = new GameObject("Spikes");
                m_Spikes = go.transform;
                m_Spikes.SetParent(transform, false);
                go.AddComponent<MeshFilter>();
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = m_SpikeMaterial;
                renderer.shadowCastingMode = ShadowCastingMode.On;
            }

            var count = Mathf.Max(1, m_SpikesPerSide);
            var countX = Mathf.Max(1, Mathf.RoundToInt(count * size.x / Mathf.Max(0.1f, m_Size.x)));
            var filter = m_Spikes.GetComponent<MeshFilter>();
            filter.sharedMesh = SpikeMesh(size, countX, count, 0.3f);
            m_SpikeArea = size;
            PlaceSpikes();
        }

        static Mesh SpikeMesh(Vector2 size, int countX, int countZ, float thickness)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var step = new Vector2(size.x / countX, size.y / countZ);
            var half = Mathf.Min(step.x, step.y) * thickness;
            for (var i = 0; i < countX; i++)
            {
                for (var j = 0; j < countZ; j++)
                {
                    var center = new Vector3(-size.x * 0.5f + step.x * (i + 0.5f), 0f, -size.y * 0.5f + step.y * (j + 0.5f));
                    AddSpike(vertices, triangles, center, half);
                }
            }

            var mesh = new Mesh { name = "Spikes" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // Pyramide à base carrée, de hauteur 1 (mise à l'échelle par niveau).
        static void AddSpike(List<Vector3> vertices, List<int> triangles, Vector3 center, float half)
        {
            var tip = center + Vector3.up;
            var corners = new[]
            {
                center + new Vector3(-half, 0f, -half), center + new Vector3(-half, 0f, half),
                center + new Vector3(half, 0f, half), center + new Vector3(half, 0f, -half),
            };
            for (var k = 0; k < 4; k++)
            {
                var start = vertices.Count;
                // Ordre des sommets : la face regarde vers l'extérieur (sens horaire vu de dehors, comme Unity l'attend).
                vertices.Add(corners[(k + 1) % 4]);
                vertices.Add(tip);
                vertices.Add(corners[k]);
                triangles.Add(start);
                triangles.Add(start + 1);
                triangles.Add(start + 2);
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.3f, 0.6f);
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(new Vector3(0f, 0.3f, 0f), new Vector3(m_Size.x, 0.6f, m_Size.y));
            Gizmos.DrawWireCube(new Vector3(0f, 0.3f, 0f), new Vector3(m_Size.x * m_WideScale, 0.6f, m_JawLength));
        }
    }
}
