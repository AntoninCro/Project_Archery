using System.Collections;
using System.Collections.Generic;
using Archery.Bows;
using Archery.Core;
using Archery.Enemies;
using Archery.UI;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Le duo <b>Carquois géant</b> (Baliste + Lance-filet ; <c>Docs/Refonte_TowerDefense.md</c>, 12.15 et 12.17) : un mât tend un
    /// grand filet comme une voile devant une arbalète à six arcs. Tes flèches ratées (plantées dans le décor, à portée) sont
    /// rattrapées : elles se rangent dans le filet ; à dix, l'arbalète te les renvoie en une volée de carreaux qui pleut sur le
    /// groupe le plus serré.
    /// <list type="bullet">
    /// <item>1 : une volée de dix carreaux ;</item>
    /// <item>2 : chaque carreau explose à l'impact (et brûle si ta flèche était enflammée) ;</item>
    /// <item>3, la <b>nuée</b> : toutes tes flèches ratées pendant la vague, partout sur la carte, sont gardées ; à trente, elles
    /// repartent d'un coup, trois carreaux chacune : une nuée qui obscurcit le ciel et pleut sur tous les ennemis à portée.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Une flèche ratée : un tir du joueur qui se plante ailleurs que dans un ennemi (<see cref="Arrow.Stuck"/>). Les carreaux
    /// sont ceux de la baliste (<see cref="BallistaBolt"/>), lâchés de haut au-dessus de la cible.
    /// </remarks>
    public class DuoCarquoisGeant : TowerDuo
    {
        [Header("Le filet")]
        [Tooltip("Les flèches rangées dans le filet (pièces du modèle) : une de plus montrée à chaque flèche rattrapée.")]
        [SerializeField]
        Transform[] m_NetArrows = new Transform[0];

        [Tooltip("Le haut de l'arbalète : la volée en part (des étincelles, le son).")]
        [SerializeField]
        Transform m_Crossbow;

        [Tooltip("Rayon (m) où une flèche ratée est rattrapée, par niveau (au niveau 3 : toute la carte).")]
        [SerializeField]
        float[] m_CatchRadiusPerLevel = { 20f, 26f, 1000f };

        [Tooltip("Combien de flèches rattrapées font une volée, par niveau.")]
        [SerializeField]
        int[] m_VolleyAtPerLevel = { 10, 10, 30 };

        [Tooltip("Combien de carreaux par flèche rattrapée, par niveau.")]
        [SerializeField]
        int[] m_BoltsPerArrowPerLevel = { 1, 1, 3 };

        [Header("La volée")]
        [SerializeField]
        BallistaBolt m_BoltPrefab;

        [Tooltip("Portée (m) de la volée.")]
        [SerializeField]
        float m_Range = 40f;

        [Tooltip("Dégâts d'un carreau, par niveau (vague 1, en Normal).")]
        [SerializeField]
        float[] m_BoltDamagePerLevel = { 30f, 32f, 34f };

        [Tooltip("Rayon (m) de la pluie autour de la cible ; hauteur (m) d'où tombent les carreaux ; leur vitesse (m/s).")]
        [SerializeField]
        float m_Spread = 3.5f;

        [SerializeField]
        float m_DropHeight = 22f;

        [SerializeField]
        float m_BoltSpeed = 42f;

        [Tooltip("Niveau 2 : rayon (m) et dégâts de l'explosion d'un carreau.")]
        [SerializeField]
        float m_BlastRadius = 2.2f;

        [SerializeField]
        float m_BlastDamage = 15f;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_CatchClip;

        [SerializeField]
        AudioClip m_VolleyClip;

        readonly List<Enemy> m_Scratch = new List<Enemy>();
        readonly List<Vector3> m_Points = new List<Vector3>();
        int m_Caught;
        int m_Burning;
        bool m_Firing;

        protected override void Start()
        {
            base.Start();
            ShowNet();
        }

        void OnEnable() => Arrow.Stuck += OnArrowStuck;

        void OnDisable() => Arrow.Stuck -= OnArrowStuck;

        protected override void Update()
        {
            base.Update();
            if (!IsBuilt || m_Firing)
                return;

            var level = Mathf.Max(1, Level);
            if (m_Caught >= ValueFor(m_VolleyAtPerLevel, level, 10) && TryTargets(level))
                StartCoroutine(Volley(level));
        }

        void OnArrowStuck(Arrow arrow, Collider collider)
        {
            if (!IsBuilt || arrow == null || (collider != null && collider.GetComponentInParent<Enemy>() != null))
                return;

            var level = Mathf.Max(1, Level);
            var radius = ValueFor(m_CatchRadiusPerLevel, level, 20f);
            var flat = arrow.transform.position - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > radius * radius)
                return;

            m_Caught++;
            if (arrow.IsOnFire)
                m_Burning++;
            ShowNet();
            Burst(arrow.transform.position, new Color(1f, 0.85f, 0.35f, 1f), 12, 3f, 0.15f, 0.5f, 0.3f);
            if (level < 3 || flat.sqrMagnitude < 30f * 30f)
                Sfx.Play(m_CatchClip, arrow.transform.position, 0.6f, Random.Range(1f, 1.2f));
            if (level >= 3 && m_Caught % 10 == 0)
                FloatingText.Spawn(transform.position + Vector3.up * 9f, m_Caught + " / " + ValueFor(m_VolleyAtPerLevel, 3, 30),
                                   new Color(1f, 0.85f, 0.35f, 1f), 1.6f, 1.5f);
        }

        // Les flèches du filet : une par flèche rattrapée (dix au plus).
        void ShowNet()
        {
            for (var i = 0; i < m_NetArrows.Length; i++)
            {
                if (m_NetArrows[i] != null)
                    m_NetArrows[i].gameObject.SetActive(i < m_Caught);
            }
        }

        // Où tomber : au niveau 3, sur tous les ennemis à portée ; sinon sur le groupe le plus serré.
        bool TryTargets(int level)
        {
            m_Points.Clear();
            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(transform.position, m_Range, false));
            if (m_Scratch.Count == 0)
                return false;

            if (level >= 3)
            {
                foreach (var enemy in m_Scratch)
                    m_Points.Add(enemy.transform.position);
                return true;
            }

            var best = 0;
            var point = Vector3.zero;
            foreach (var enemy in m_Scratch)
            {
                var count = 0;
                foreach (var other in m_Scratch)
                {
                    if ((other.transform.position - enemy.transform.position).sqrMagnitude < 16f)
                        count++;
                }

                if (count > best)
                {
                    best = count;
                    point = enemy.transform.position;
                }
            }

            m_Points.Add(point);
            return true;
        }

        // La volée : les carreaux tombent du ciel, un peu en biais depuis la tour, en rafale, sur les points visés.
        IEnumerator Volley(int level)
        {
            m_Firing = true;
            var bolts = m_Caught * ValueFor(m_BoltsPerArrowPerLevel, level, 1);
            var burning = m_Burning > 0;
            m_Caught = 0;
            m_Burning = 0;
            ShowNet();

            var top = m_Crossbow != null ? m_Crossbow.position : transform.position + Vector3.up * 5f;
            Burst(top, new Color(1f, 0.85f, 0.35f, 1f), 60, 12f, 0.25f, 0.8f, 0.4f);
            Sfx.Play(m_VolleyClip, top, 1f, level >= 3 ? 0.7f : 1f);
            if (level >= 3)
                FloatingText.Spawn(top + Vector3.up * 3f, "La nuée !", new Color(1f, 0.85f, 0.35f, 1f), 2.2f, 2.5f);

            yield return new WaitForSeconds(0.5f);
            var damage = ValueFor(m_BoltDamagePerLevel, level, 30f);
            var points = new List<Vector3>(m_Points);
            for (var i = 0; i < bolts; i++)
            {
                if (m_BoltPrefab == null)
                    break;
                var center = points[i % points.Count];
                var offset = Random.insideUnitCircle * m_Spread;
                var target = center + new Vector3(offset.x, 0f, offset.y);
                var from = target + Vector3.up * m_DropHeight + (transform.position - target).normalized * 6f;
                var direction = (target - from).normalized;
                var bolt = Instantiate(m_BoltPrefab, from, Quaternion.LookRotation(direction));
                if (level >= 2)
                    bolt.Launch(direction * m_BoltSpeed, damage * Strength, 0, this, (enemy, dir) => Blast(enemy, burning));
                else
                    bolt.Launch(direction * m_BoltSpeed, damage * Strength, 0, this);
                if (i % 3 == 2)
                    yield return null;
            }

            m_Firing = false;
        }

        // Niveau 2 : le carreau explose ; il brûle si une flèche enflammée a été rattrapée.
        void Blast(Enemy hit, bool burning)
        {
            if (hit == null)
                return;
            var point = hit.Center;
            Burst(point, burning ? new Color(1f, 0.5f, 0.15f, 1f) : new Color(1f, 0.85f, 0.4f, 1f), 18, 5f, 0.3f, 0.5f, 0.4f);
            foreach (var enemy in EnemiesNear(point, m_BlastRadius, false))
                Hurt(enemy, m_BlastDamage * (burning ? 1.5f : 1f), enemy.Center, enemy.transform.position - point);
        }
    }
}
