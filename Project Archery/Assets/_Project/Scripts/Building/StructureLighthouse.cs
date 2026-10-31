using System.Collections.Generic;
using Archery.Bows;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.Upgrades;
using Archery.World;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Phare (refonte du tower defense, étape 5, lot 5 ; 28 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, partie 6.1) : en
    /// hauteur. Dans son rayon, les points faibles des ennemis brillent : la tête, et le point faible des boss. Une flèche qui y
    /// touche fait 20 / 35 / 50 % de dégâts en plus. Son faisceau tourne. Rare. Depuis le 29 octobre (deuxième brainstorm,
    /// <c>Docs/Refonte_TowerDefense.md</c>, partie 12.6), c'est la tour de soutien (l'atelier y est fondu), à cinq niveaux :
    /// <list type="bullet">
    /// <item>1 : en plus, les bâtiments de son rayon font 20 % de dégâts en plus ;</item>
    /// <item>2, la <b>cadence</b> : ils tirent 15 % plus vite (l'ancien atelier) ;</item>
    /// <item>3, le <b>grand phare</b> : rayon, renforts et faisceau plus grands ;</item>
    /// <item>4 : un anneau de feu flotte sur la voie la plus proche, près de ta tour (29 octobre au soir, le joueur ; avant, le
    /// faisceau lui-même, trop dur à viser) : ta flèche qui le traverse s'enflamme (l'ancien anneau de feu), sa cible brûle ;</item>
    /// <item>5, le <b>phare du jugement</b> : toutes les 10 s, un rayon géant tombe du ciel sur l'ennemi le plus fort, et tous
    /// les bâtiments du rayon tirent deux fois plus vite pendant 5 s.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Les points faibles (zone <see cref="HitZone.WeakPoint"/>) n'existent que sur les boss : pour les autres ennemis, c'est
    /// la tête qui brille. La lueur est une petite sphère sans collider (<c>Marker Material</c>) posée sur la zone de touche ;
    /// le bonus passe par <see cref="Arrow.AnyHit"/>, en dégâts du phare.
    /// </remarks>
    public class StructureLighthouse : Structure
    {
        [Tooltip("Rayon (m) du phare, par niveau.")]
        [SerializeField]
        float[] m_RadiusPerLevel = { 12f, 14f, 16f };

        [Tooltip("Dégâts en plus d'une flèche sur un point faible qui brille (0,2 = 20 %), par niveau.")]
        [SerializeField]
        float[] m_BonusPerLevel = { 0.2f, 0.35f, 0.5f };

        [Tooltip("Diamètre (m) de la lueur sur un point faible.")]
        [SerializeField]
        float m_MarkerSize = 0.3f;

        [SerializeField]
        Material m_MarkerMaterial;

        [Tooltip("Faisceau : tourne autour de la verticale.")]
        [SerializeField]
        Transform m_Beam;

        [Tooltip("Vitesse (°/s) du faisceau.")]
        [SerializeField]
        float m_BeamSpeed = 60f;

        [Header("Soutien et évolutions (29 octobre)")]
        [Tooltip("Dégâts des bâtiments de son rayon (1,2 = 20 % de plus), par niveau.")]
        [SerializeField]
        float[] m_SupportDamagePerLevel = { 1.2f, 1.2f, 1.3f, 1.3f, 1.4f };

        [Tooltip("Cadence des bâtiments de son rayon (1,15 = 15 % plus vite), par niveau.")]
        [SerializeField]
        float[] m_SupportRatePerLevel = { 1f, 1.15f, 1.25f, 1.25f, 1.35f };

        [Tooltip("Taille du faisceau, par niveau (le grand phare).")]
        [SerializeField]
        float[] m_BeamScalePerLevel = { 1f, 1f, 1.6f, 1.6f, 2f };

        [Tooltip("Rayon (m) de l'anneau de feu (niveau 4).")]
        [SerializeField]
        float m_RingRadius = 1.3f;

        [Tooltip("Distance (m) de l'anneau à ta tour, le long de la voie la plus proche du phare.")]
        [SerializeField]
        float m_RingFromTower = 14f;

        [Tooltip("Hauteur (m) de l'anneau au-dessus de la voie.")]
        [SerializeField]
        float m_RingHeight = 2.6f;

        [SerializeField]
        Material m_RingMaterial;

        [Tooltip("Dégâts de la brûlure (en tout), en part de ceux de la flèche enflammée.")]
        [SerializeField]
        float m_BurnShare = 0.4f;

        [Tooltip("Durée (s) de la brûlure.")]
        [SerializeField]
        float m_BurnDuration = 3f;

        [Tooltip("Temps (s) entre deux jugements (niveau 5).")]
        [SerializeField]
        float m_JudgmentInterval = 10f;

        [Tooltip("Dégâts du jugement, à la vague 1 en Normal (la moitié de ses PV max au moins, sauf un boss).")]
        [SerializeField]
        float m_JudgmentDamage = 150f;

        [Tooltip("Part de ses PV max que perd au moins un ennemi frappé par le jugement.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_JudgmentShare = 0.5f;

        [Tooltip("Part de ses PV max que perd un boss frappé par le jugement.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_JudgmentBossShare = 0.1f;

        [Tooltip("Cadence des bâtiments du rayon après un jugement, et pendant combien de temps (s).")]
        [SerializeField]
        Vector2 m_JudgmentBoost = new Vector2(2f, 5f);

        [SerializeField]
        Material m_JudgmentMaterial;

        [SerializeField]
        AudioClip m_JudgmentClip;

        // Une lueur sur un point faible : elle n'est pas enfant de l'ennemi (30 octobre : la rattacher au phare pendant que l'ennemi
        // s'activait ou se désactivait, à sa destruction, faisait une erreur de Unity) ; elle le suit à chaque image.
        struct Glow
        {
            public GameObject Marker;
            public Transform Hitbox;
            public Vector3 Center;
        }

        readonly Dictionary<Enemy, List<Glow>> m_Marked = new Dictionary<Enemy, List<Glow>>();
        Transform m_Glows;
        readonly List<Enemy> m_Leaving = new List<Enemy>();
        readonly HashSet<Enemy> m_Inside = new HashSet<Enemy>();
        readonly Stack<GameObject> m_FreeMarkers = new Stack<GameObject>();
        float m_ScanTimer;
        readonly ArrowWatch m_Arrows = new ArrowWatch();
        readonly HashSet<Arrow> m_Ignited = new HashSet<Arrow>();
        float m_JudgmentTimer = 4f;
        Transform m_Ring;
        Camera m_Head;
        Transform m_Judgment;
        float m_JudgmentAge = -1f;

        void OnEnable()
        {
            Arrow.AnyHit += OnArrowHit;
            Arrow.ShotEnded += OnShotEnded;
            m_Arrows.Begin();
        }

        void OnDisable()
        {
            Arrow.AnyHit -= OnArrowHit;
            Arrow.ShotEnded -= OnShotEnded;
            m_Arrows.End();
            ClearMarks();
        }

        void OnShotEnded(Arrow arrow) => m_Ignited.Remove(arrow);

        float BeamScale => ValueFor(m_BeamScalePerLevel, Mathf.Max(1, Level), 1f);

        protected override void OnLevelChanged(int level)
        {
            base.OnLevelChanged(level);
            if (m_Beam != null)
                m_Beam.localScale = Vector3.one * BeamScale;
        }

        // Les lueurs ne sont pas des pièces du bâtiment, ni le faisceau (29 octobre, retour du joueur) : ses deux rayons de 6 m
        // comptaient dans la taille du phare (12 m de large, 6 × 3 cases de la grille au lieu de 3 × 3), dans le volume frappé
        // au marteau et dans l'aperçu du menu des plans, où la tour devenait minuscule.
        protected override bool IsBlueprintPart(Renderer renderer) =>
            !renderer.name.StartsWith("Lueur du phare") && (m_Beam == null || !renderer.transform.IsChildOf(m_Beam));

        protected override void Update()
        {
            base.Update();

            // Le faisceau ne passe pas en plan bleu : il s'allume une fois le phare construit.
            if (m_Beam != null && m_Beam.gameObject.activeSelf != IsBuilt)
                m_Beam.gameObject.SetActive(IsBuilt);

            if (!IsBuilt)
                return;

            if (m_Beam != null)
                m_Beam.Rotate(0f, m_BeamSpeed * Time.deltaTime * OverchargeRate, 0f, Space.World);

            var level = Level;
            if (level >= 4)
                IgniteArrows();
            else if (m_Ring != null)
                m_Ring.gameObject.SetActive(false);
            if (level >= 4 && m_Ring != null && !m_Ring.gameObject.activeSelf)
                m_Ring.gameObject.SetActive(true);
            UpdateJudgmentEffect(Time.deltaTime);
            if (level >= 5)
            {
                m_JudgmentTimer -= Time.deltaTime * OverchargeRate;
                if (m_JudgmentTimer <= 0f)
                    m_JudgmentTimer = Judge() ? m_JudgmentInterval : 0.5f;
            }

            m_ScanTimer -= Time.deltaTime;
            if (m_ScanTimer > 0f)
                return;

            m_ScanTimer = 0.25f;
            Scan();
            SupportBuildings();
        }

        // Le soutien : les bâtiments construits de son rayon (pas lui-même) font plus de dégâts, et tirent plus vite au niveau 2.
        void SupportBuildings()
        {
            var level = Mathf.Max(1, Level);
            var damage = ValueFor(m_SupportDamagePerLevel, level, 1.2f);
            var rate = ValueFor(m_SupportRatePerLevel, level, 1f);
            var radius = Radius;
            foreach (var structure in All)
            {
                if (structure != null && structure != this && structure.IsBuilt && IsInside(structure.transform.position, radius))
                    structure.Support(damage, rate, 0.6f);
            }
        }

        // ---------------------------------------------------------------- niveau 4 : la flèche enflammée

        // Une flèche qui traverse l'anneau de feu s'enflamme, une fois par tir. L'anneau se tourne vers la tête du joueur.
        void IgniteArrows()
        {
            if (m_Ring == null && !CreateRing())
                return;

            if (m_Head == null)
                m_Head = Camera.main;
            if (m_Head != null)
            {
                var toPlayer = m_Head.transform.position - m_Ring.position;
                if (toPlayer.sqrMagnitude > 1e-3f)
                    m_Ring.rotation = Quaternion.RotateTowards(m_Ring.rotation, Quaternion.LookRotation(toPlayer), 120f * Time.deltaTime);
            }

            var center = m_Ring.position;
            var normal = m_Ring.forward;
            m_Arrows.Reset();
            while (m_Arrows.Next(out var arrow, out var from, out var to))
            {
                var before = Vector3.Dot(from - center, normal);
                var after = Vector3.Dot(to - center, normal);
                if (before * after > 0f || Mathf.Approximately(before, after))
                    continue;

                var point = Vector3.Lerp(from, to, before / (before - after));
                if ((point - center).sqrMagnitude > m_RingRadius * m_RingRadius || arrow.IsOnFire || !m_Arrows.Once(arrow))
                    continue;

                arrow.Ignite(BurnFlames.Arrow);
                m_Ignited.Add(arrow);
            }
        }

        // L'anneau : sur la voie la plus proche du phare, à m_RingFromTower de la tour du joueur (là où il tire le plus), un peu en
        // l'air. Un anneau de maillons lumineux et quatre foyers.
        bool CreateRing()
        {
            var grid = BuildGrid.Instance;
            if (grid == null || grid.Lanes == null || grid.Lanes.Count == 0)
                return false;

            MapScan.Lane nearest = null;
            var best = float.MaxValue;
            foreach (var lane in grid.Lanes)
            {
                var distance = lane.DistanceTo(transform.position, out _);
                if (lane.Points.Count >= 2 && distance < best)
                {
                    best = distance;
                    nearest = lane;
                }
            }

            if (nearest == null)
                return false;

            var point = nearest.PointAt(Mathf.Max(0f, nearest.Length - m_RingFromTower), out _);
            var ring = new GameObject("Anneau de feu du phare (effet)").transform;
            ring.position = point + Vector3.up * m_RingHeight;
            const int links = 18;
            for (var i = 0; i < links; i++)
            {
                var angle = i * 360f / links;
                var link = GameObject.CreatePrimitive(PrimitiveType.Cube);
                link.name = "Maillon";
                Destroy(link.GetComponent<Collider>());
                link.transform.SetParent(ring, false);
                link.transform.localPosition = Quaternion.Euler(0f, 0f, angle) * Vector3.up * m_RingRadius;
                link.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                link.transform.localScale = new Vector3(2f * Mathf.PI * m_RingRadius / links * 1.15f, 0.12f, 0.12f);
                var renderer = link.GetComponent<Renderer>();
                if (m_RingMaterial != null)
                    renderer.sharedMaterial = m_RingMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            var flames = BurnFlames.Enemy;
            if (flames != null)
            {
                for (var i = 0; i < 4; i++)
                {
                    var fire = Instantiate(flames, ring);
                    fire.transform.localPosition = Quaternion.Euler(0f, 0f, i * 90f + 45f) * Vector3.up * m_RingRadius;
                    fire.SetActive(true);
                }
            }

            m_Ring = ring;
            return true;
        }

        // ---------------------------------------------------------------- niveau 5 : le jugement

        // Un rayon géant tombe du ciel sur l'ennemi le plus fort du rayon (le plus de PV) ; les bâtiments du rayon s'emballent.
        bool Judge()
        {
            var radius = Radius;
            Enemy target = null;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive || !IsInside(enemy.transform.position, radius))
                    continue;
                if (target == null || enemy.Health.Current > target.Health.Current)
                    target = enemy;
            }

            if (target == null)
                return false;

            var amount = target.IsBoss ? target.Health.Max * m_JudgmentBossShare
                : Mathf.Max(m_JudgmentDamage * Strength, target.Health.Max * m_JudgmentShare);
            var point = target.transform.position;
            target.Health.TakeDamage(new DamageInfo
            {
                Amount = amount,
                Zone = HitZone.Body,
                Point = target.Center,
                Direction = Vector3.down,
                Source = this,
            });

            foreach (var structure in All)
            {
                if (structure != null && structure.IsBuilt && IsInside(structure.transform.position, radius))
                    structure.Boost(m_JudgmentBoost.x, m_JudgmentBoost.y);
            }

            ShowJudgment(point);
            var arrows = SpecialArrows.Instance;
            if (arrows != null && arrows.ExplosionFireMaterial != null && arrows.ExplosionSmokeMaterial != null)
                ExplosionFx.Play(point + Vector3.up * 0.3f, 2.5f, arrows.ExplosionFireMaterial, arrows.ExplosionSmokeMaterial, null);
            Sfx.Play(m_JudgmentClip, point + Vector3.up * 2f, 1f, 0.8f);
            return true;
        }

        // Une colonne de lumière de 80 m, qui s'affine et s'efface en 0,8 s.
        void ShowJudgment(Vector3 point)
        {
            if (m_Judgment == null)
            {
                var column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                column.name = "Jugement (effet)";
                Destroy(column.GetComponent<Collider>());
                var renderer = column.GetComponent<Renderer>();
                renderer.sharedMaterial = m_JudgmentMaterial != null ? m_JudgmentMaterial : m_MarkerMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                m_Judgment = column.transform;
            }

            m_Judgment.position = point + Vector3.up * 40f;
            m_Judgment.gameObject.SetActive(true);
            m_JudgmentAge = 0f;
        }

        void UpdateJudgmentEffect(float deltaTime)
        {
            if (m_JudgmentAge < 0f || m_Judgment == null)
                return;

            m_JudgmentAge += deltaTime;
            var width = 2.2f * Mathf.Clamp01(1f - m_JudgmentAge / 0.8f);
            m_Judgment.localScale = new Vector3(width, 40f, width);
            if (m_JudgmentAge < 0.8f)
                return;

            m_Judgment.gameObject.SetActive(false);
            m_JudgmentAge = -1f;
        }

        protected override void OnDestroy()
        {
            if (m_Glows != null)
                Destroy(m_Glows.gameObject);
            if (m_Judgment != null)
                Destroy(m_Judgment.gameObject);
            if (m_Ring != null)
                Destroy(m_Ring.gameObject);
            base.OnDestroy();
        }

        // Marque les ennemis qui entrent dans le rayon, démarque ceux qui en sortent (ou meurent).
        void Scan()
        {
            var radius = Radius;
            m_Inside.Clear();
            foreach (var enemy in AliveEnemies())
            {
                if (enemy != null && enemy.IsAlive && IsInside(enemy.transform.position, radius))
                    m_Inside.Add(enemy);
            }

            m_Leaving.Clear();
            foreach (var pair in m_Marked)
            {
                if (pair.Key == null || !m_Inside.Contains(pair.Key))
                    m_Leaving.Add(pair.Key);
            }

            foreach (var enemy in m_Leaving)
                Unmark(enemy);

            // Marqué, il est préparé pour ta flèche (« Marqué ! », 30 octobre) : jusqu'au prochain balayage, un peu plus.
            foreach (var enemy in m_Inside)
            {
                if (!m_Marked.ContainsKey(enemy))
                    Mark(enemy);
                enemy.Prepare(Preparation.Marked, 0.4f);
            }
        }

        float Radius => ValueFor(m_RadiusPerLevel, Mathf.Max(1, Level), 12f) * RangeScale * OverchargeRange;

        // À plat : un phare en hauteur éclaire la voie en contrebas.
        bool IsInside(Vector3 position, float radius)
        {
            var offset = position - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude <= radius * radius;
        }

        // Les lueurs suivent leur point faible ; elles s'éteignent quand il disparaît (l'ennemi détruit, un point faible rentré).
        void LateUpdate()
        {
            foreach (var pair in m_Marked)
            {
                foreach (var glow in pair.Value)
                {
                    if (glow.Marker == null)
                        continue;
                    var shown = glow.Hitbox != null && glow.Hitbox.gameObject.activeInHierarchy;
                    if (glow.Marker.activeSelf != shown)
                        glow.Marker.SetActive(shown);
                    if (shown)
                        glow.Marker.transform.position = glow.Hitbox.TransformPoint(glow.Center);
                }
            }
        }

        void Mark(Enemy enemy)
        {
            var markers = new List<Glow>();
            var hasWeakPoint = false;
            foreach (var hitbox in enemy.GetComponentsInChildren<Hitbox>())
                hasWeakPoint |= hitbox.Zone == HitZone.WeakPoint;

            foreach (var hitbox in enemy.GetComponentsInChildren<Hitbox>())
            {
                if (hitbox.Zone == (hasWeakPoint ? HitZone.WeakPoint : HitZone.Head) && hitbox.TryGetComponent<Collider>(out var collider))
                    markers.Add(TakeMarker(hitbox.transform, collider.bounds.center));
            }

            m_Marked[enemy] = markers;
        }

        void Unmark(Enemy enemy)
        {
            if (!m_Marked.TryGetValue(enemy, out var markers))
                return;

            m_Marked.Remove(enemy);
            foreach (var glow in markers)
            {
                if (glow.Marker == null)
                    continue;
                glow.Marker.SetActive(false);
                m_FreeMarkers.Push(glow.Marker);
            }
        }

        void ClearMarks()
        {
            m_Leaving.Clear();
            m_Leaving.AddRange(m_Marked.Keys);
            foreach (var enemy in m_Leaving)
                Unmark(enemy);
        }

        // Une lueur (prise dans la réserve, sinon créée) sur ce point du point faible ; toutes sont rangées sous un même objet, hors
        // du phare et des ennemis.
        Glow TakeMarker(Transform hitbox, Vector3 position)
        {
            if (m_Glows == null)
                m_Glows = new GameObject("Lueurs du phare").transform;

            var marker = null as GameObject;
            while (marker == null && m_FreeMarkers.Count > 0)
                marker = m_FreeMarkers.Pop();
            if (marker == null)
            {
                marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "Lueur du phare";
                Destroy(marker.GetComponent<Collider>());
                var renderer = marker.GetComponent<Renderer>();
                if (m_MarkerMaterial != null)
                    renderer.sharedMaterial = m_MarkerMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                marker.transform.SetParent(m_Glows, false);
                marker.transform.localScale = Vector3.one * m_MarkerSize;
            }

            marker.transform.position = position;
            marker.SetActive(true);
            return new Glow { Marker = marker, Hitbox = hitbox, Center = hitbox.InverseTransformPoint(position) };
        }

        // Une flèche tirée sur un point faible qui brille : les dégâts en plus du phare.
        void OnArrowHit(ArrowHit hit)
        {
            // La flèche enflammée par le faisceau (niveau 4) : sa cible brûle, comme avec l'anneau de feu.
            if (hit.Arrow != null && m_Ignited.Remove(hit.Arrow) && hit.Collider != null)
            {
                var burning = hit.Collider.GetComponentInParent<Enemy>();
                if (burning != null && burning.IsAlive)
                    Burning.Apply(burning, hit.Damage * m_BurnShare, m_BurnDuration, hit.Grade, hit.TravelDistance, BurnFlames.Enemy);
            }

            if (!IsBuilt || !hit.IsShot || hit.Collider == null || !hit.Collider.TryGetComponent<Hitbox>(out var hitbox))
                return;

            var enemy = hit.Collider.GetComponentInParent<Enemy>();
            if (enemy == null || !enemy.IsAlive || !m_Marked.ContainsKey(enemy))
                return;
            if (hitbox.Zone != HitZone.Head && hitbox.Zone != HitZone.WeakPoint)
                return;

            enemy.Health.TakeDamage(new DamageInfo
            {
                Amount = hit.Damage * ValueFor(m_BonusPerLevel, Mathf.Max(1, Level), 0.2f),
                Zone = HitZone.Body,
                Point = hit.Point,
                Direction = hit.Direction,
                Source = this,
            });
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.95f, 0.6f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, ValueFor(m_RadiusPerLevel, Mathf.Max(1, Level), 12f));
        }
    }
}
