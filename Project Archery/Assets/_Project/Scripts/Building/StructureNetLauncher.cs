using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.Upgrades;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Lance-filet (refonte du tower defense, étape 5, lot 4 ; 28 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, parties 6.1 et
    /// 6.2) : au bord de la voie. Il vise le volant le plus proche et lui lance un filet : le volant tombe au sol (sa chute le
    /// blesse) et y reste étourdi, à portée de flèche et de katana, puis il repart. Depuis le 29 octobre (deuxième brainstorm,
    /// partie 12.6), cinq niveaux :
    /// <list type="bullet">
    /// <item>2 : le volant tombé reste plus longtemps au sol ;</item>
    /// <item>3, le <b>barillet</b> : deux filets à la fois ;</item>
    /// <item>4, le <b>filet électrique</b> : il étourdit et blesse, et vise aussi les ennemis au sol quand il n'y a pas de
    /// volant ;</item>
    /// <item>5, la <b>toile géante</b> : toutes les 15 s, un filet immense couvre le ciel au-dessus de la voie pendant 6 s ; tous
    /// les volants qui le touchent s'écrasent au sol.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Il hérite de la <see cref="Ballista"/> pour la visée (la tourelle suit sa cible) et la cadence ; le filet (une forme
    /// aplatie au matériau <c>Net Material</c>) vole jusqu'à elle, puis l'enveloppe tant qu'elle est prise (<c>Enemy.Net</c>).
    /// </remarks>
    public class StructureNetLauncher : Ballista
    {
        [Header("Filet")]
        [Tooltip("Durée (s) pendant laquelle un volant pris reste au sol, par niveau.")]
        [SerializeField]
        float[] m_HoldPerLevel = { 3f, 4f, 5f };

        [Tooltip("Filets par tir, par niveau (chacun sur une cible différente).")]
        [SerializeField]
        int[] m_NetsPerLevel = { 1, 1, 2 };

        [Tooltip("Vitesse (m/s) du filet.")]
        [SerializeField]
        float m_NetSpeed = 30f;

        [Tooltip("Diamètre (m) du filet déployé.")]
        [SerializeField]
        float m_NetSize = 1.4f;

        [SerializeField]
        Material m_NetMaterial;

        [SerializeField]
        AudioClip m_CatchClip;

        [Header("Filet électrique (niveau 4)")]
        [SerializeField]
        Material m_ShockNetMaterial;

        [Tooltip("Dégâts du filet électrique, à la vague 1 en Normal.")]
        [SerializeField]
        float m_ShockDamage = 15f;

        [Tooltip("Étourdissement (s) d'un ennemi au sol pris dans le filet électrique.")]
        [SerializeField]
        float m_ShockStun = 2f;

        [SerializeField]
        Color m_ShockColor = new Color(0.5f, 0.85f, 1f);

        [Header("Toile géante (niveau 5)")]
        [Tooltip("Temps (s) entre deux toiles géantes.")]
        [SerializeField]
        float m_WebInterval = 15f;

        [Tooltip("Durée (s) de la toile géante.")]
        [SerializeField]
        float m_WebDuration = 6f;

        [Tooltip("Rayon (m) de la toile géante.")]
        [SerializeField]
        float m_WebRadius = 14f;

        [SerializeField]
        AudioClip m_WebClip;

        class Net
        {
            public Transform Body;
            public Enemy Target;
            public bool Caught;
            public float Until;
        }

        readonly List<Net> m_Nets = new List<Net>();
        readonly List<Enemy> m_Aimed = new List<Enemy>();
        Transform m_Web;
        float m_WebTimer = 4f;
        float m_WebLeft;

        protected override bool FiresBolts => false;

        bool Electric => Level >= 4;

        // Le volant le plus proche à portée, visible, qui n'est pas déjà pris (ni visé par un filet en vol) ; au niveau 4, à
        // défaut, l'ennemi au sol le plus proche.
        protected override Enemy FindTarget()
        {
            var muzzle = Muzzle;
            Enemy best = null;
            Enemy ground = null;
            var range = Range;
            var bestDistance = range * range;
            var groundDistance = range * range;
            foreach (var enemy in AliveEnemies())
            {
                if (!IsFree(enemy))
                    continue;

                var distance = (enemy.Center - muzzle).sqrMagnitude;
                if (enemy.IsFlyer && distance < bestDistance && IsVisible(muzzle, enemy))
                {
                    best = enemy;
                    bestDistance = distance;
                }
                else if (!enemy.IsFlyer && distance < groundDistance && IsVisible(muzzle, enemy))
                {
                    ground = enemy;
                    groundDistance = distance;
                }
            }

            return best != null ? best : ground;
        }

        bool IsFree(Enemy enemy)
        {
            if (enemy == null || !enemy.IsAlive || enemy.IsNetted)
                return false;
            if (!enemy.IsFlyer && (!Electric || enemy.IsAirborne || enemy.IsStunned || enemy.IsBoss))
                return false;

            foreach (var net in m_Nets)
            {
                if (net.Target == enemy)
                    return false;
            }

            return true;
        }

        // À la place d'un carreau : un filet sur la cible, et au niveau 3 un deuxième sur la cible libre la plus proche d'elle.
        protected override void Shoot(Vector3 direction)
        {
            var first = Target;
            if (first == null || !IsFree(first))
                return;

            m_Aimed.Clear();
            m_Aimed.Add(first);
            var count = ValueFor(m_NetsPerLevel, Level, 1);
            foreach (var enemy in AliveEnemies())
            {
                if (m_Aimed.Count >= count)
                    break;
                if (IsFree(enemy) && !m_Aimed.Contains(enemy) && (enemy.Center - first.Center).sqrMagnitude <= Range * Range)
                    m_Aimed.Add(enemy);
            }

            foreach (var enemy in m_Aimed)
                m_Nets.Add(new Net { Body = SpawnNet(Muzzle), Target = enemy });
        }

        Transform SpawnNet(Vector3 position)
        {
            var body = NetBody("Filet", Electric && m_ShockNetMaterial != null ? m_ShockNetMaterial : m_NetMaterial);
            body.position = position;
            body.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            return body;
        }

        static Mesh s_NetMesh;

        // Un vrai filet (29 octobre au soir, le joueur : la boule aplatie n'était pas belle) : un dôme de cordes, trois cercles et
        // douze rayons, de 1 m de diamètre, qui pend sous son centre.
        static Transform NetBody(string name, Material material)
        {
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = NetMesh;
            var renderer = go.AddComponent<MeshRenderer>();
            if (material != null)
                renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        static Mesh NetMesh
        {
            get
            {
                if (s_NetMesh != null)
                    return s_NetMesh;

                var vertices = new List<Vector3>();
                var triangles = new List<int>();
                const int segments = 16;
                const int spokes = 12;
                const float width = 0.018f;
                foreach (var radius in new[] { 0.17f, 0.34f, 0.5f })
                {
                    for (var i = 0; i < segments; i++)
                        AddCord(vertices, triangles, NetPoint(radius, i * 360f / segments), NetPoint(radius, (i + 1) * 360f / segments), width);
                }

                for (var i = 0; i < spokes; i++)
                {
                    var angle = i * 360f / spokes;
                    for (var step = 0; step < 4; step++)
                        AddCord(vertices, triangles, NetPoint(step * 0.125f, angle), NetPoint((step + 1) * 0.125f, angle), width);
                }

                s_NetMesh = new Mesh { name = "Filet" };
                s_NetMesh.SetVertices(vertices);
                s_NetMesh.SetTriangles(triangles, 0);
                s_NetMesh.RecalculateNormals();
                s_NetMesh.RecalculateBounds();
                return s_NetMesh;
            }
        }

        // Le dôme : plus bas vers le bord (le centre en haut, comme un filet qui retombe).
        static Vector3 NetPoint(float radius, float angle)
        {
            var radians = angle * Mathf.Deg2Rad;
            var fall = radius / 0.5f;
            return new Vector3(Mathf.Cos(radians) * radius, -0.3f * fall * fall, Mathf.Sin(radians) * radius);
        }

        // Une corde : une barre carrée de a à b (quatre faces).
        static void AddCord(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, float width)
        {
            var along = b - a;
            if (along.sqrMagnitude < 1e-8f)
                return;

            var side = Vector3.Cross(along, Vector3.up);
            if (side.sqrMagnitude < 1e-8f)
                side = Vector3.Cross(along, Vector3.forward);
            side = side.normalized * width;
            var up = Vector3.Cross(side.normalized, along.normalized) * width;
            var corners = new[] { side + up, -side + up, -side - up, side - up };
            for (var k = 0; k < 4; k++)
            {
                var start = vertices.Count;
                var c0 = corners[k];
                var c1 = corners[(k + 1) % 4];
                vertices.Add(a + c0);
                vertices.Add(a + c1);
                vertices.Add(b + c1);
                vertices.Add(b + c0);
                triangles.Add(start);
                triangles.Add(start + 2);
                triangles.Add(start + 1);
                triangles.Add(start);
                triangles.Add(start + 3);
                triangles.Add(start + 2);
            }
        }

        protected override void Update()
        {
            base.Update();
            UpdateNets(Time.deltaTime);
            UpdateWeb(Time.deltaTime);
        }

        // Un filet vole vers sa cible en s'ouvrant ; arrivé, il la prend et l'enveloppe jusqu'à ce qu'elle se libère.
        void UpdateNets(float deltaTime)
        {
            for (var i = m_Nets.Count - 1; i >= 0; i--)
            {
                var net = m_Nets[i];
                var alive = net.Target != null && net.Target.IsAlive;
                var held = net.Target != null && (net.Target.IsFlyer ? net.Target.IsNetted : Time.time < net.Until);
                if (net.Body == null || !alive || (net.Caught && !held))
                {
                    if (net.Body != null)
                        Destroy(net.Body.gameObject);
                    m_Nets.RemoveAt(i);
                    continue;
                }

                var center = net.Target.Center;
                if (net.Caught)
                {
                    net.Body.position = center;
                    continue;
                }

                net.Body.position = Vector3.MoveTowards(net.Body.position, center, m_NetSpeed * deltaTime);
                var open = Mathf.MoveTowards(net.Body.localScale.x, m_NetSize, m_NetSize * 3f * deltaTime);
                net.Body.localScale = new Vector3(open, open * 0.6f, open);
                if ((net.Body.position - center).sqrMagnitude > 0.25f)
                    continue;

                net.Caught = Catch(net);
                if (net.Caught)
                {
                    net.Body.localScale = new Vector3(m_NetSize, m_NetSize * 1.2f, m_NetSize);
                    Sfx.Play(m_CatchClip, center, 0.9f, Random.Range(0.9f, 1.1f));
                }
                else
                {
                    Destroy(net.Body.gameObject);
                    m_Nets.RemoveAt(i);
                }
            }
        }

        // Un volant tombe au sol ; au niveau 4, le filet électrique blesse, et cloue aussi un ennemi au sol (étourdi).
        bool Catch(Net net)
        {
            var target = net.Target;
            var hold = ValueFor(m_HoldPerLevel, Mathf.Max(1, Level), 3f);
            bool caught;
            if (target.IsFlyer)
            {
                caught = target.Net(hold, this);
            }
            else
            {
                target.Stun(m_ShockStun);
                net.Until = Time.time + m_ShockStun;
                caught = true;
            }

            if (caught && Electric)
            {
                target.Health.TakeDamage(new DamageInfo
                {
                    Amount = m_ShockDamage * Strength,
                    Zone = HitZone.Body,
                    Point = target.Center,
                    Direction = Vector3.down,
                    Source = this,
                });
                LightningBolt.Spawn(target.Center + Vector3.up * 0.8f, target.Center - Vector3.up * 0.4f, m_ShockColor, null, 0.06f, 0.25f);
            }

            return caught;
        }

        // ---------------------------------------------------------------- niveau 5 : la toile géante

        // Elle se tend au-dessus du volant visé, à sa hauteur ; tout volant qui passe sous son rayon (elle couvre le ciel) s'y
        // prend et s'écrase au sol.
        void UpdateWeb(float deltaTime)
        {
            if (m_WebLeft > 0f)
            {
                m_WebLeft -= deltaTime;
                var open = Mathf.Clamp01((m_WebDuration - m_WebLeft) / 0.4f) * Mathf.Clamp01(m_WebLeft / 0.4f);
                if (m_Web != null)
                    m_Web.localScale = new Vector3(m_WebRadius * 2f * open, 4f * open, m_WebRadius * 2f * open);

                var hold = ValueFor(m_HoldPerLevel, Mathf.Max(1, Level), 3f);
                foreach (var enemy in AliveEnemies())
                {
                    if (enemy == null || !enemy.IsAlive || !enemy.IsFlyer || enemy.IsNetted || m_Web == null)
                        continue;

                    var offset = enemy.transform.position - m_Web.position;
                    offset.y = 0f;
                    if (offset.sqrMagnitude <= m_WebRadius * m_WebRadius * open * open && enemy.Net(hold, this))
                        LightningBolt.Spawn(m_Web.position + offset, enemy.Center, m_ShockColor, null, 0.05f, 0.2f);
                }

                if (m_WebLeft <= 0f && m_Web != null)
                    m_Web.gameObject.SetActive(false);
                return;
            }

            if (!IsBuilt || Level < 5)
                return;

            m_WebTimer -= deltaTime * OverchargeRate;
            if (m_WebTimer > 0f)
                return;

            var target = Target;
            if (target == null || !target.IsFlyer)
            {
                m_WebTimer = 0.5f;
                return;
            }

            // Le même filet que les tirs, en immense : un dôme de cordes de 28 m au-dessus des volants.
            if (m_Web == null)
                m_Web = NetBody("Toile géante (effet)", Electric && m_ShockNetMaterial != null ? m_ShockNetMaterial : m_NetMaterial);

            m_Web.gameObject.SetActive(true);
            m_Web.position = target.Center + Vector3.up * 2.5f;
            m_Web.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            m_WebLeft = m_WebDuration;
            m_WebTimer = m_WebInterval;
            Sfx.Play(m_WebClip != null ? m_WebClip : m_CatchClip, m_Web.position, 1f, 0.7f);
        }

        protected override void OnDestroy()
        {
            foreach (var net in m_Nets)
            {
                if (net.Body != null)
                    Destroy(net.Body.gameObject);
            }

            m_Nets.Clear();
            if (m_Web != null)
                Destroy(m_Web.gameObject);
            base.OnDestroy();
        }
    }
}
