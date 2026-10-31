using System.Collections.Generic;
using Archery.Core;
using Archery.Enemies;
using Archery.UI;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Le duo <b>Chant des sirènes</b> (Phare + Bulle ; <c>Docs/Refonte_TowerDefense.md</c>, 12.15 et 12.17) : un phare de nacre
    /// coiffé d'une sirène. Son faisceau hypnotise un ennemi : enfermé dans une bulle rose, il change de camp, se jette sur les
    /// autres ennemis, les renverse et les blesse ; sans personne à frapper, il repart vers la forêt.
    /// <list type="bullet">
    /// <item>1 : un ennemi charmé 6 s ;</item>
    /// <item>2 : deux à la fois ; à la fin, sa bulle éclate en deux bulles qui charment ses voisins ;</item>
    /// <item>3, la <b>mêlée</b> : tout ce qui est à 20 m change de camp 5 s : chacun se jette sur son voisin, une mêlée géante.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Le charme est <see cref="Enemy.Charm"/> : l'ennemi court où le duo l'envoie ; les coups, c'est le duo qui les porte. Les
    /// boss et les volants ne sont pas charmés. La bulle est une sphère au matériau des hologrammes, teinté de rose.
    /// </remarks>
    public class DuoSirenes : TowerDuo
    {
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_FilledColorId = Shader.PropertyToID("_FilledColor");
        static readonly int k_EdgeColorId = Shader.PropertyToID("_EdgeColor");

        [Header("Le phare")]
        [Tooltip("La lanterne : elle se tourne vers l'ennemi qu'elle charme.")]
        [SerializeField]
        Transform m_Lantern;

        [Tooltip("Le faisceau (enfant de la lanterne, le long de son axe Z, 1 m à l'échelle 1) : allongé jusqu'à l'ennemi.")]
        [SerializeField]
        Transform m_Beam;

        [SerializeField]
        float m_Range = 26f;

        [Header("Le charme")]
        [Tooltip("Temps (s) entre deux chants, par niveau (au niveau 3 : un chant simple entre deux mêlées).")]
        [SerializeField]
        float[] m_IntervalPerLevel = { 5f, 4.5f, 4f };

        [Tooltip("Combien d'ennemis charmés par chant, par niveau.")]
        [SerializeField]
        int[] m_TargetsPerLevel = { 1, 2, 2 };

        [Tooltip("Temps (s) du charme, par niveau.")]
        [SerializeField]
        float[] m_CharmTimePerLevel = { 6f, 6f, 6f };

        [Tooltip("Dégâts d'un coup d'un ennemi charmé, par niveau (vague 1, en Normal), et le temps (s) entre deux de ses coups.")]
        [SerializeField]
        float[] m_HitDamagePerLevel = { 20f, 26f, 24f };

        [SerializeField]
        float m_HitCooldown = 0.8f;

        [Tooltip("Distance (m) à laquelle un ennemi charmé frappe.")]
        [SerializeField]
        float m_HitRange = 1.6f;

        [Header("Les bulles qui éclatent (niveau 2)")]
        [SerializeField]
        float m_SpreadRadius = 7f;

        [SerializeField]
        float m_SpreadTime = 3.5f;

        [Header("La mêlée (niveau 3)")]
        [SerializeField]
        float m_MeleeInterval = 15f;

        [SerializeField]
        float m_MeleeRadius = 20f;

        [SerializeField]
        float m_MeleeTime = 5f;

        [SerializeField]
        Color m_BubbleColor = new Color(1f, 0.45f, 0.75f, 0.28f);

        [SerializeField]
        Color m_BubbleEdge = new Color(1f, 0.7f, 0.9f, 1f);

        [Header("Sons")]
        [SerializeField]
        AudioClip m_SongClip;

        [SerializeField]
        AudioClip m_PopClip;

        class Charmed
        {
            public float Until;
            public float NextHit;
            public float NextPath;
            public bool Spread;
            public float Phase = Random.value * 6f;
            public GameObject Bubble;
            public Vector3 BubbleScale;
        }

        readonly Dictionary<Enemy, Charmed> m_Charmed = new Dictionary<Enemy, Charmed>();
        readonly List<Enemy> m_Ended = new List<Enemy>();
        readonly List<Enemy> m_Scratch = new List<Enemy>();
        Material m_BubbleMaterial;
        Mesh m_Sphere;
        float m_NextSong;
        float m_NextMelee;
        float m_BeamUntil;
        Vector3 m_BeamTarget;

        protected override void OnDestroy()
        {
            foreach (var pair in m_Charmed)
            {
                if (pair.Key != null)
                    pair.Key.Uncharm();
                if (pair.Value.Bubble != null)
                    Destroy(pair.Value.Bubble);
            }

            m_Charmed.Clear();
            if (m_BubbleMaterial != null)
                Destroy(m_BubbleMaterial);
            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();
            UpdateCharmed();
            UpdateBeam();
            if (!IsBuilt)
                return;

            var level = Mathf.Max(1, Level);
            if (level >= 3 && Time.time >= m_NextMelee && Melee())
            {
                m_NextMelee = Time.time + m_MeleeInterval / OverchargeRate;
                m_NextSong = Time.time + 3f;
                return;
            }

            if (Time.time < m_NextSong)
                return;
            m_NextSong = Time.time + 0.5f;
            var sung = 0;
            var wanted = ValueFor(m_TargetsPerLevel, level, 1);
            for (var i = 0; i < wanted; i++)
            {
                var target = FrontTarget();
                if (target == null || !Enchant(target, ValueFor(m_CharmTimePerLevel, level, 6f), level >= 2))
                    break;
                sung++;
                m_BeamTarget = target.Center;
            }

            if (sung == 0)
                return;
            m_NextSong = Time.time + ValueFor(m_IntervalPerLevel, level, 5f) / OverchargeRate;
            m_BeamUntil = Time.time + 0.8f;
            Sfx.Play(m_SongClip, transform.position + Vector3.up * 8f, 0.8f, Random.Range(0.95f, 1.1f));
        }

        // L'ennemi le plus avancé vers la tour, à portée, pas encore charmé.
        Enemy FrontTarget()
        {
            Enemy best = null;
            var bestScore = float.MaxValue;
            var tower = Archery.Defense.Tower.Instance;
            var goal = tower != null ? tower.transform.position : transform.position;
            foreach (var enemy in EnemiesNear(transform.position, m_Range))
            {
                if (m_Charmed.ContainsKey(enemy) || enemy.IsBoss || !enemy.CanBeLaunched)
                    continue;
                var score = (enemy.transform.position - goal).sqrMagnitude;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = enemy;
                }
            }

            return best;
        }

        // La mêlée : tout ce qui est à portée change de camp.
        bool Melee()
        {
            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(transform.position, m_MeleeRadius));
            var count = 0;
            foreach (var enemy in m_Scratch)
            {
                if (Enchant(enemy, m_MeleeTime, false))
                    count++;
            }

            if (count < 2)
                return count > 0;
            FloatingText.Spawn(transform.position + Vector3.up * 10f, "La mêlée !", m_BubbleEdge, 2.2f, 2.5f);
            Sfx.Play(m_SongClip, transform.position + Vector3.up * 8f, 1f, 0.75f);
            m_BeamUntil = Time.time + 1.5f;
            m_BeamTarget = transform.position + transform.forward * m_MeleeRadius * 0.5f;
            return true;
        }

        bool Enchant(Enemy enemy, float duration, bool spread)
        {
            if (enemy == null || !enemy.IsAlive || enemy.IsAirborne || m_Charmed.ContainsKey(enemy))
                return false;
            if (!enemy.Charm(duration, enemy.transform.position))
                return false;

            var charmed = new Charmed { Until = Time.time + duration, NextHit = Time.time + 0.3f, Spread = spread };
            charmed.Bubble = MakeBubble(enemy, out charmed.BubbleScale);
            m_Charmed[enemy] = charmed;
            enemy.Prepare(Preparation.Stunned, duration);
            FloatingText.Spawn(enemy.Center + Vector3.up * 1.4f, "Charmé !", m_BubbleEdge, 1.1f, 1.2f);
            return true;
        }

        // Chaque ennemi charmé court sur l'ennemi le plus proche (pas charmé, sauf dans la mêlée) et le frappe ; sinon vers la forêt.
        void UpdateCharmed()
        {
            if (m_Charmed.Count == 0)
                return;

            var level = Mathf.Max(1, Level);
            m_Ended.Clear();
            foreach (var pair in m_Charmed)
            {
                var enemy = pair.Key;
                var charmed = pair.Value;
                if (enemy == null || !enemy.IsAlive || !enemy.IsCharmed || Time.time >= charmed.Until)
                {
                    m_Ended.Add(enemy);
                    continue;
                }

                if (charmed.Bubble != null)
                {
                    var pulse = 1f + 0.06f * Mathf.Sin(Time.time * 6f + charmed.Phase);
                    charmed.Bubble.transform.position = enemy.Center;
                    charmed.Bubble.transform.localScale = charmed.BubbleScale * pulse;
                }

                var prey = Prey(enemy, out var distance);
                if (Time.time >= charmed.NextPath)
                {
                    charmed.NextPath = Time.time + 0.3f;
                    enemy.SetCharmDestination(prey != null ? prey.transform.position
                        : enemy.transform.position + ForestwardAt(enemy.transform.position) * 10f);
                }

                if (prey == null || distance > m_HitRange || Time.time < charmed.NextHit)
                    continue;
                charmed.NextHit = Time.time + m_HitCooldown;
                var from = enemy.transform.position;
                if (Hurt(prey, ValueFor(m_HitDamagePerLevel, level, 20f), prey.Center, prey.transform.position - from) &&
                    !m_Charmed.ContainsKey(prey))
                    KnockDown(prey, from, 2.5f, 3.5f);
                Burst(prey.Center, m_BubbleEdge, 10, 3f, 0.2f, 0.4f, 0.5f);
            }

            foreach (var enemy in m_Ended)
                End(enemy, level);
        }

        // La proie : l'ennemi au sol le plus proche ; un charmé s'en prend d'abord à ceux qui ne le sont pas (sauf dans la mêlée,
        // où chacun frappe son voisin).
        Enemy Prey(Enemy hunter, out float distance)
        {
            Enemy best = null;
            distance = float.MaxValue;
            var bestFree = false;
            foreach (var other in GroundEnemies())
            {
                if (other == hunter)
                    continue;
                var flat = other.transform.position - hunter.transform.position;
                flat.y = 0f;
                var d = flat.magnitude;
                if (d > 25f)
                    continue;
                var free = !m_Charmed.ContainsKey(other);
                if ((free && !bestFree) || (free == bestFree && d < distance))
                {
                    best = other;
                    distance = d;
                    bestFree = free;
                }
            }

            return best;
        }

        // Le charme finit : la bulle éclate ; au niveau 2, en deux bulles qui charment deux voisins.
        void End(Enemy enemy, int level)
        {
            if (!m_Charmed.TryGetValue(enemy, out var charmed))
                return;
            m_Charmed.Remove(enemy);
            if (charmed.Bubble != null)
            {
                Burst(charmed.Bubble.transform.position, m_BubbleEdge, 30, 4f, 0.3f, 0.6f, 0.2f);
                Destroy(charmed.Bubble);
            }

            if (enemy == null)
                return;
            enemy.Uncharm();
            enemy.Unprepare(Preparation.Stunned);
            Sfx.Play(m_PopClip, enemy.Center, 0.7f, Random.Range(1f, 1.2f));
            if (!charmed.Spread || level < 2 || !enemy.IsAlive)
                return;

            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(enemy.transform.position, m_SpreadRadius, true, enemy));
            m_Scratch.Sort((a, b) => (a.transform.position - enemy.transform.position).sqrMagnitude
                .CompareTo((b.transform.position - enemy.transform.position).sqrMagnitude));
            var spread = 0;
            foreach (var other in m_Scratch)
            {
                if (spread >= 2)
                    break;
                if (!other.IsBoss && Enchant(other, m_SpreadTime, false))
                    spread++;
            }
        }

        GameObject MakeBubble(Enemy enemy, out Vector3 scale)
        {
            scale = Vector3.one;
            if (m_BubbleMaterial == null)
            {
                var builds = BuildManager.Instance;
                if (builds == null || builds.BlueprintMaterial == null)
                    return null;
                m_BubbleMaterial = new Material(builds.BlueprintMaterial) { name = "Bulle des sirènes" };
                m_BubbleMaterial.SetColor(k_BaseColorId, m_BubbleColor);
                m_BubbleMaterial.SetColor(k_FilledColorId, m_BubbleColor);
                m_BubbleMaterial.SetColor(k_EdgeColorId, m_BubbleEdge);
            }

            if (m_Sphere == null)
            {
                var primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                m_Sphere = primitive.GetComponent<MeshFilter>().sharedMesh;
                Destroy(primitive);
            }

            var bubble = new GameObject("Bulle des sirènes");
            bubble.AddComponent<MeshFilter>().sharedMesh = m_Sphere;
            var renderer = bubble.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_BubbleMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var body = enemy.GetComponentInChildren<Renderer>();
            var size = body != null ? Mathf.Clamp(body.bounds.size.y * 1.15f, 1.2f, 4.5f) : 2f;
            scale = Vector3.one * size;
            bubble.transform.position = enemy.Center;
            bubble.transform.localScale = scale;
            return bubble;
        }

        // La lanterne se tourne vers ce qu'elle charme ; le faisceau s'allonge jusque-là le temps du chant.
        void UpdateBeam()
        {
            var singing = Time.time < m_BeamUntil;
            if (m_Lantern != null)
            {
                if (singing)
                {
                    var look = m_BeamTarget - m_Lantern.position;
                    if (look.sqrMagnitude > 0.01f)
                        m_Lantern.rotation = Quaternion.Slerp(m_Lantern.rotation, Quaternion.LookRotation(look), Time.deltaTime * 12f);
                }
                else
                {
                    m_Lantern.Rotate(0f, 30f * Time.deltaTime, 0f, Space.World);
                }
            }

            if (m_Beam == null)
                return;
            m_Beam.gameObject.SetActive(singing);
            if (!singing)
                return;
            var scale = m_Beam.localScale;
            scale.z = Mathf.Max(0.1f, Vector3.Distance(m_Beam.position, m_BeamTarget));
            m_Beam.localScale = scale;
        }
    }
}
