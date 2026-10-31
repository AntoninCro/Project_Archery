using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Building
{
    /// <summary>
    /// Un ennemi gelé (tour de givre, deuxième brainstorm du tower defense, 29 octobre) : pris dans un bloc de glace, figé
    /// (étourdi, son animation arrêtée). Le premier coup qui ne vient pas du givre brise la glace : au niveau 3 (le pic de glace),
    /// il lui coûte une part de ses PV max en plus ; au niveau 5 (le cœur de l'hiver), il vole en éclats et meurt. Sinon, la
    /// glace fond au bout de sa durée. Les volants et les ennemis en l'air ne gèlent pas ; les boss, seulement dans la tempête du
    /// cœur de l'hiver (29 octobre au soir, le joueur), et le coup qui brise leur glace ne leur coûte qu'une part de leurs PV.
    /// </summary>
    public class IceStatue : MonoBehaviour
    {
        static readonly Dictionary<Enemy, IceStatue> s_Frozen = new Dictionary<Enemy, IceStatue>();

        Enemy m_Enemy;
        Animator m_Animator;
        float m_AnimatorSpeed = 1f;
        float m_Left;
        float m_BreakShare;
        Object m_Source;
        AudioClip m_BreakClip;
        Transform m_Block;
        float m_Burst;
        bool m_Broken;

        /// <summary>Cet ennemi est gelé.</summary>
        public static bool IsFrozen(Enemy enemy) => enemy != null && s_Frozen.ContainsKey(enemy);

        /// <summary>
        /// Gèle l'ennemi <paramref name="duration"/> s (prolonge s'il l'est déjà). <paramref name="breakShare"/> : part de ses PV
        /// max que lui coûte le coup qui brise la glace ; 1 ou plus, il vole en éclats et meurt. Renvoie vrai s'il est gelé.
        /// </summary>
        public static bool Freeze(Enemy enemy, float duration, float breakShare, Material ice, AudioClip breakClip, Object source,
                                  bool allowBoss = false, float bossBreakShare = 0.1f)
        {
            if (enemy == null || !enemy.IsAlive || (enemy.IsBoss && !allowBoss) || enemy.IsFlyer || enemy.IsAirborne || duration <= 0f)
                return false;

            if (enemy.IsBoss)
                breakShare = Mathf.Min(breakShare, bossBreakShare);

            if (s_Frozen.TryGetValue(enemy, out var statue) && statue != null)
            {
                statue.m_Left = Mathf.Max(statue.m_Left, duration);
                statue.m_BreakShare = Mathf.Max(statue.m_BreakShare, breakShare);
                enemy.Stun(statue.m_Left);
                enemy.Prepare(Preparation.Frozen, statue.m_Left);
                return true;
            }

            var go = new GameObject("Glace");
            statue = go.AddComponent<IceStatue>();
            statue.m_Enemy = enemy;
            statue.m_Left = duration;
            statue.m_BreakShare = breakShare;
            statue.m_Source = source;
            statue.m_BreakClip = breakClip;
            statue.m_Block = CreateBlock(enemy, ice, go.transform);
            statue.m_Animator = enemy.GetComponentInChildren<Animator>();
            if (statue.m_Animator != null)
            {
                statue.m_AnimatorSpeed = statue.m_Animator.speed;
                statue.m_Animator.speed = 0f;
            }

            s_Frozen[enemy] = statue;
            enemy.Stun(duration);
            // Gelé, il est préparé pour ta flèche (« Brisé ! », 30 octobre), jusqu'à ce que la glace se brise ou fonde (Thaw).
            enemy.Prepare(Preparation.Frozen, duration);
            enemy.Health.Damaged += statue.OnDamaged;
            return true;
        }

        // Un bloc de glace autour de son corps (ses rendus), un peu plus grand.
        static Transform CreateBlock(Enemy enemy, Material ice, Transform parent)
        {
            var bounds = new Bounds(enemy.Center, Vector3.one);
            var any = false;
            foreach (var renderer in enemy.GetComponentsInChildren<Renderer>())
            {
                if (!(renderer is MeshRenderer || renderer is SkinnedMeshRenderer) || !renderer.enabled)
                    continue;
                if (any)
                    bounds.Encapsulate(renderer.bounds);
                else
                    bounds = renderer.bounds;
                any = true;
            }

            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "Bloc de glace";
            Destroy(block.GetComponent<Collider>());
            block.transform.SetParent(parent, false);
            parent.position = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            block.transform.localPosition = Vector3.up * bounds.extents.y;
            block.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 90f), 0f);
            block.transform.localScale = new Vector3(Mathf.Max(0.8f, bounds.size.x), bounds.size.y, Mathf.Max(0.8f, bounds.size.z)) * 1.15f;
            var blockRenderer = block.GetComponent<Renderer>();
            if (ice != null)
                blockRenderer.sharedMaterial = ice;
            blockRenderer.shadowCastingMode = ShadowCastingMode.Off;
            return block.transform;
        }

        // Le premier coup qui ne vient pas du givre brise la glace.
        void OnDamaged(Health health, DamageInfo info)
        {
            if (m_Broken || info.Source == m_Source || m_Enemy == null)
                return;

            m_Broken = true;
            m_Enemy.Health.Damaged -= OnDamaged;
            Sfx.Play(m_BreakClip, m_Enemy.Center, 1f, Random.Range(0.9f, 1.15f));
            if (m_Enemy.IsAlive)
            {
                var amount = m_BreakShare >= 1f ? m_Enemy.Health.Current + 1f : m_Enemy.Health.Max * m_BreakShare;
                if (amount > 0f)
                {
                    m_Enemy.Health.TakeDamage(new DamageInfo
                    {
                        Amount = amount,
                        Zone = HitZone.Body,
                        Point = m_Enemy.Center,
                        Direction = info.Direction,
                        Source = m_Source,
                    });
                }
            }

            Thaw();
        }

        void Update()
        {
            if (m_Enemy == null)
            {
                Destroy(gameObject);
                return;
            }

            // Brisée : le bloc éclate (il gonfle d'un coup et disparaît).
            if (m_Broken)
            {
                m_Burst += Time.deltaTime;
                if (m_Block != null)
                    m_Block.localScale *= 1f + Time.deltaTime * 8f;
                if (m_Burst >= 0.12f)
                    Destroy(gameObject);
                return;
            }

            m_Left -= Time.deltaTime;
            if (m_Left <= 0f || !m_Enemy.IsAlive || m_Enemy.IsAirborne)
            {
                m_Enemy.Health.Damaged -= OnDamaged;
                Thaw();
                Destroy(gameObject);
            }
        }

        void Thaw()
        {
            if (m_Enemy != null && s_Frozen.TryGetValue(m_Enemy, out var statue) && statue == this)
            {
                s_Frozen.Remove(m_Enemy);
                m_Enemy.Unprepare(Preparation.Frozen);
            }
            if (m_Animator != null)
                m_Animator.speed = m_AnimatorSpeed;
        }

        void OnDestroy()
        {
            if (m_Enemy != null && !m_Broken)
                m_Enemy.Health.Damaged -= OnDamaged;
            Thaw();
        }
    }
}
