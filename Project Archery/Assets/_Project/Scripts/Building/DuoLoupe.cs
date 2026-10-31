using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.UI;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Le duo <b>Loupe</b> (Phare + Baliste ; <c>Docs/Refonte_TowerDefense.md</c>, 12.15 et 12.17) : une loupe de laiton grande
    /// comme une porte, sur une tour de bois, se tourne vers les ennemis : dans son champ, ils ont la tête trois fois plus grosse
    /// (sa zone de tir aussi ; le point faible des boss deux fois) : les tirs à la tête deviennent faciles.
    /// <list type="bullet">
    /// <item>1 : les grosses têtes ;</item>
    /// <item>2 : une baliste sur la tour plante ses carreaux dans les têtes géantes ;</item>
    /// <item>3, la <b>fourmi</b> : la loupe concentre le soleil en un rayon sur l'ennemi le plus fort du champ ; il fume, puis
    /// prend feu (un tiers de ses PV ; un boss, 15 %) ; le rayon passe au suivant.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// La tête grossit par son os (le squelette humanoïde de l'Animator, ou l'os qui porte la zone de tête), sinon par la zone de
    /// tête seule ; l'échelle est rendue en sortant du champ, et à la mort. Les volants comptent.
    /// </remarks>
    public class DuoLoupe : TowerDuo
    {
        [Header("La loupe")]
        [Tooltip("Ce qui tourne vers les ennemis (autour de Y) : la fourche et la loupe.")]
        [SerializeField]
        Transform m_Yoke;

        [Tooltip("La loupe elle-même : elle s'incline vers les ennemis ; son axe Z est son regard.")]
        [SerializeField]
        Transform m_Lens;

        [Tooltip("Portée (m) et demi-angle (°) de son champ.")]
        [SerializeField]
        float m_Range = 30f;

        [SerializeField]
        float m_HalfAngle = 38f;

        [Tooltip("Combien de fois plus grosse la tête, par niveau ; le point faible des boss.")]
        [SerializeField]
        float[] m_HeadScalePerLevel = { 3f, 3f, 3.3f };

        [SerializeField]
        float m_WeakPointScale = 2f;

        [Tooltip("Temps (s) pour que la tête grossisse ou dégonfle.")]
        [SerializeField]
        float m_GrowTime = 0.35f;

        [Header("La baliste (niveau 2)")]
        [SerializeField]
        BallistaBolt m_BoltPrefab;

        [Tooltip("D'où part le carreau.")]
        [SerializeField]
        Transform m_Bow;

        [SerializeField]
        float m_BoltInterval = 1.6f;

        [SerializeField]
        float m_BoltDamage = 45f;

        [SerializeField]
        float m_BoltSpeed = 55f;

        [Header("La fourmi (niveau 3)")]
        [Tooltip("Le rayon (enfant de la loupe, le long de son axe Z, 1 m à l'échelle 1) : allongé jusqu'à l'ennemi.")]
        [SerializeField]
        Transform m_Ray;

        [Tooltip("Temps (s) où l'ennemi fume avant de prendre feu, et durée (s) de son feu.")]
        [SerializeField]
        float m_SmokeTime = 1.2f;

        [SerializeField]
        float m_BurnTime = 2f;

        [Tooltip("Ce que coûte le feu : une part de ses PV max (un boss, moins), et des dégâts en plus.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_BurnShare = 0.33f;

        [Range(0f, 1f)]
        [SerializeField]
        float m_BossBurnShare = 0.15f;

        [SerializeField]
        float m_BurnDamage = 40f;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_BoltClip;

        [SerializeField]
        AudioClip m_IgniteClip;

        class Part
        {
            public Transform Transform;
            public Vector3 Scale;
            public bool WeakPoint;
        }

        class Big
        {
            public readonly List<Part> Parts = new List<Part>();
            public float Grow;
            public bool InField;
            public Vector3 Head;
        }

        readonly Dictionary<Enemy, Big> m_Big = new Dictionary<Enemy, Big>();
        readonly List<Enemy> m_Gone = new List<Enemy>();
        Enemy m_RayTarget;
        float m_RayTime;
        bool m_Burning;
        float m_NextBurnTick;
        float m_BurnLeft;
        float m_NextBolt;
        float m_NextScan;

        void OnDisable() => RestoreAll();

        protected override void OnDestroy()
        {
            RestoreAll();
            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsBuilt)
            {
                SetRay(null);
                return;
            }

            var level = Mathf.Max(1, Level);
            if (Time.time >= m_NextScan)
            {
                m_NextScan = Time.time + 0.2f;
                Scan(level);
            }

            Aim();
            Grow();
            if (level >= 2)
                UpdateBolts();
            if (level >= 3)
                UpdateRay();
            else
                SetRay(null);
        }

        // Les têtes grossissent en entrant dans le champ, dégonflent en en sortant.
        void Scan(int level)
        {
            foreach (var pair in m_Big)
                pair.Value.InField = false;

            var forward = Forward();
            foreach (var enemy in EnemiesNear(transform.position, m_Range, false))
            {
                var to = enemy.transform.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 1f && Vector3.Angle(forward, to) > m_HalfAngle)
                    continue;
                if (!m_Big.TryGetValue(enemy, out var big))
                {
                    big = Collect(enemy);
                    m_Big[enemy] = big;
                }

                big.InField = true;
            }
        }

        Vector3 Forward()
        {
            var forward = m_Yoke != null ? m_Yoke.forward : transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward;
        }

        // La loupe se tourne vers l'ennemi le plus proche à portée (ou celui du rayon).
        void Aim()
        {
            Enemy target = m_RayTarget;
            if (target == null)
            {
                var best = float.MaxValue;
                foreach (var pair in m_Big)
                {
                    if (pair.Key == null || !pair.Key.IsAlive)
                        continue;
                    var d = (pair.Key.transform.position - transform.position).sqrMagnitude;
                    if (d < best)
                    {
                        best = d;
                        target = pair.Key;
                    }
                }

                if (target == null)
                {
                    foreach (var enemy in EnemiesNear(transform.position, m_Range, false))
                    {
                        target = enemy;
                        break;
                    }
                }
            }

            if (target == null)
                return;
            var look = target.Center - (m_Lens != null ? m_Lens.position : transform.position);
            var flat = new Vector3(look.x, 0f, look.z);
            if (m_Yoke != null && flat.sqrMagnitude > 0.01f)
                m_Yoke.rotation = Quaternion.RotateTowards(m_Yoke.rotation, Quaternion.LookRotation(flat), 90f * Time.deltaTime);
            if (m_Lens != null && flat.sqrMagnitude > 0.01f)
            {
                var pitch = -Mathf.Atan2(look.y, flat.magnitude) * Mathf.Rad2Deg;
                m_Lens.localRotation = Quaternion.RotateTowards(m_Lens.localRotation, Quaternion.Euler(Mathf.Clamp(pitch, -10f, 45f), 0f, 0f),
                                                                60f * Time.deltaTime);
            }
        }

        void Grow()
        {
            if (m_Big.Count == 0)
                return;
            m_Gone.Clear();
            foreach (var pair in m_Big)
            {
                var enemy = pair.Key;
                var big = pair.Value;
                if (enemy == null || !enemy.IsAlive)
                {
                    m_Gone.Add(enemy);
                    continue;
                }

                big.Grow = Mathf.MoveTowards(big.Grow, big.InField ? 1f : 0f, Time.deltaTime / Mathf.Max(0.05f, m_GrowTime));
                if (!big.InField && big.Grow <= 0f)
                    m_Gone.Add(enemy);
            }

            foreach (var enemy in m_Gone)
            {
                if (m_Big.TryGetValue(enemy, out var big))
                    Restore(big);
                m_Big.Remove(enemy);
            }
        }

        // L'échelle des têtes, après l'animation (elle ne touche pas à l'échelle, mais on la remet à chaque image).
        void LateUpdate()
        {
            if (m_Big.Count == 0)
                return;
            var head = ValueFor(m_HeadScalePerLevel, Mathf.Max(1, Level), 3f);
            foreach (var pair in m_Big)
            {
                var big = pair.Value;
                foreach (var part in big.Parts)
                {
                    if (part.Transform == null)
                        continue;
                    var factor = Mathf.Lerp(1f, part.WeakPoint ? m_WeakPointScale : head, Mathf.SmoothStep(0f, 1f, big.Grow));
                    part.Transform.localScale = part.Scale * factor;
                }
            }
        }

        // Ce qui grossit : l'os de la tête (humanoïde, ou celui qui porte la zone de tête), sinon la zone de tête ; le point faible.
        static Big Collect(Enemy enemy)
        {
            var big = new Big();
            Transform bone = null;
            var animator = enemy.GetComponentInChildren<Animator>();
            if (animator != null && animator.isHuman)
                bone = animator.GetBoneTransform(HumanBodyBones.Head);

            foreach (var hitbox in enemy.GetComponentsInChildren<Hitbox>(true))
            {
                if (hitbox.Zone == HitZone.WeakPoint)
                {
                    big.Parts.Add(new Part { Transform = hitbox.transform, Scale = hitbox.transform.localScale, WeakPoint = true });
                    continue;
                }

                if (hitbox.Zone != HitZone.Head)
                    continue;
                if (bone == null && IsBone(enemy, hitbox.transform.parent))
                    bone = hitbox.transform.parent;
                if (bone == null || !hitbox.transform.IsChildOf(bone))
                    big.Parts.Add(new Part { Transform = hitbox.transform, Scale = hitbox.transform.localScale });
            }

            if (bone != null && bone != enemy.transform)
                big.Parts.Add(new Part { Transform = bone, Scale = bone.localScale });
            return big;
        }

        static bool IsBone(Enemy enemy, Transform candidate)
        {
            if (candidate == null || candidate == enemy.transform)
                return false;
            foreach (var skinned in enemy.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (System.Array.IndexOf(skinned.bones, candidate) >= 0)
                    return true;
            }

            return false;
        }

        static void Restore(Big big)
        {
            foreach (var part in big.Parts)
            {
                if (part.Transform != null)
                    part.Transform.localScale = part.Scale;
            }
        }

        void RestoreAll()
        {
            foreach (var pair in m_Big)
                Restore(pair.Value);
            m_Big.Clear();
            SetRay(null);
        }

        // La baliste (niveau 2) : un carreau dans la plus grosse tête du champ, le plus proche d'abord.
        void UpdateBolts()
        {
            if (m_BoltPrefab == null || Time.time < m_NextBolt)
                return;

            Enemy target = null;
            var best = float.MaxValue;
            foreach (var pair in m_Big)
            {
                if (pair.Key == null || !pair.Key.IsAlive || pair.Value.Grow < 0.9f || pair.Key == m_RayTarget)
                    continue;
                var d = (pair.Key.transform.position - transform.position).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    target = pair.Key;
                }
            }

            if (target == null)
                return;
            m_NextBolt = Time.time + m_BoltInterval / OverchargeRate;
            var from = m_Bow != null ? m_Bow.position : transform.position + Vector3.up * 5f;
            var head = HeadPoint(target);
            var time = Vector3.Distance(from, head) / m_BoltSpeed;
            var aim = head + target.Velocity * time;
            var direction = (aim - from).normalized;
            var bolt = Instantiate(m_BoltPrefab, from, Quaternion.LookRotation(direction));
            bolt.Launch(direction * m_BoltSpeed, m_BoltDamage * Strength, 0, this);
            Sfx.Play(m_BoltClip, from, 0.8f, Random.Range(0.95f, 1.1f));
        }

        static Vector3 HeadPoint(Enemy enemy)
        {
            foreach (var hitbox in enemy.GetComponentsInChildren<Hitbox>())
            {
                if (hitbox.Zone != HitZone.Head)
                    continue;
                var collider = hitbox.GetComponent<Collider>();
                return collider != null ? collider.bounds.center : hitbox.transform.position;
            }

            return enemy.Center + Vector3.up * 0.6f;
        }

        // La fourmi (niveau 3) : le rayon sur le plus fort du champ ; il fume, prend feu, brûle ; puis le suivant.
        void UpdateRay()
        {
            if (m_RayTarget != null && (!m_RayTarget.IsAlive || !m_Big.ContainsKey(m_RayTarget) || (m_Burning && m_BurnLeft <= 0f)))
            {
                m_RayTarget = null;
                m_Burning = false;
                m_RayTime = -0.8f;
            }

            if (m_RayTarget == null)
            {
                m_RayTime += Time.deltaTime;
                SetRay(null);
                if (m_RayTime < 0f)
                    return;
                m_RayTarget = Strongest();
                m_RayTime = 0f;
                if (m_RayTarget == null)
                    return;
            }

            var target = m_RayTarget;
            m_RayTime += Time.deltaTime * OverchargeRate;
            SetRay(target);
            var head = HeadPoint(target);
            if (!m_Burning)
            {
                if (Random.value < Time.deltaTime * 6f)
                    Burst(head, new Color(0.5f, 0.5f, 0.5f, 0.8f), 6, 1.5f, 0.5f, 1f, -0.3f);
                if (m_RayTime < m_SmokeTime)
                    return;

                m_Burning = true;
                var share = target.IsBoss ? m_BossBurnShare : m_BurnShare;
                m_BurnLeft = (m_BurnDamage + target.Health.Max * share / Mathf.Max(0.01f, Strength));
                m_NextBurnTick = Time.time;
                Explosion(head, 1.2f);
                FloatingText.Spawn(head + Vector3.up * 1.2f, "En feu !", new Color(1f, 0.55f, 0.15f, 1f), 1.4f, 1.3f);
                Sfx.Play(m_IgniteClip, head, 1f, Random.Range(0.9f, 1.05f));
            }

            if (Time.time < m_NextBurnTick)
                return;
            m_NextBurnTick = Time.time + 0.25f;
            var tick = Mathf.Min(m_BurnLeft, (m_BurnDamage + target.Health.Max * (target.IsBoss ? m_BossBurnShare : m_BurnShare) /
                                                Mathf.Max(0.01f, Strength)) * 0.25f / Mathf.Max(0.25f, m_BurnTime));
            m_BurnLeft -= tick;
            Burst(target.Center, new Color(1f, 0.5f, 0.1f, 1f), 14, 3f, 0.35f, 0.6f, -0.5f);
            Hurt(target, tick, target.Center, Vector3.down);
        }

        Enemy Strongest()
        {
            Enemy best = null;
            var bestHealth = 0f;
            foreach (var pair in m_Big)
            {
                if (pair.Key == null || !pair.Key.IsAlive || !pair.Value.InField)
                    continue;
                if (pair.Key.Health.Max > bestHealth)
                {
                    bestHealth = pair.Key.Health.Max;
                    best = pair.Key;
                }
            }

            return best;
        }

        void SetRay(Enemy target)
        {
            if (m_Ray == null)
                return;
            var on = target != null;
            if (m_Ray.gameObject.activeSelf != on)
                m_Ray.gameObject.SetActive(on);
            if (!on)
                return;
            var head = HeadPoint(target);
            m_Ray.rotation = Quaternion.LookRotation(head - m_Ray.position);
            var scale = m_Ray.localScale;
            scale.z = Mathf.Max(0.1f, Vector3.Distance(m_Ray.position, head) / Mathf.Max(0.01f, m_Ray.parent != null ? m_Ray.parent.lossyScale.z : 1f));
            m_Ray.localScale = scale;
        }
    }
}
