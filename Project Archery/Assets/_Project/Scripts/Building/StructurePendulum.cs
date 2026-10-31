using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Pendule (refonte du tower defense, étape 5, lot 4 ; 28 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, parties 6.1 et
    /// 6.2) : sur la voie, un portique d'où pend un boulet qui se balance en travers de la voie. Il fauche les ennemis au sol
    /// qu'il croise : il les blesse et les projette sur le côté (leur chute les blesse aussi). Plus fort et plus rapide à chaque
    /// niveau. Rare (proposée).
    /// </summary>
    /// <remarks>
    /// Le catalogue le voulait sur 2 × 2 emplacements ; il tient sur un seul (3 m), le boulet balayant au-delà. Le balancier
    /// (<c>Arm</c>) tourne autour de son axe Z ; <c>Ball</c> est le centre du boulet.
    /// </remarks>
    public class StructurePendulum : Structure
    {
        [Tooltip("Dégâts d'un coup de boulet, par niveau, à la vague 1 en Normal (ils suivent ensuite la force des ennemis).")]
        [SerializeField]
        float[] m_DamagePerLevel = { 20f, 30f, 45f };

        [Tooltip("Durée (s) d'un aller-retour du boulet, par niveau.")]
        [SerializeField]
        float[] m_PeriodPerLevel = { 3f, 2.6f, 2.2f };

        [Tooltip("Vitesse (m/s) sur le côté des ennemis fauchés, par niveau.")]
        [SerializeField]
        float[] m_KnockPerLevel = { 5f, 6f, 7f };

        [Tooltip("Angle (°) du balancement de chaque côté.")]
        [SerializeField]
        float m_Amplitude = 65f;

        [Tooltip("Rayon (m) du boulet, pour toucher.")]
        [SerializeField]
        float m_HitRadius = 0.9f;

        [Tooltip("Temps (s) avant que le boulet puisse retoucher le même ennemi.")]
        [SerializeField]
        float m_RehitDelay = 0.8f;

        [Tooltip("Balancier : tourne autour de son axe Z (le portique).")]
        [SerializeField]
        Transform m_Arm;

        [Tooltip("Centre du boulet.")]
        [SerializeField]
        Transform m_Ball;

        [SerializeField]
        AudioClip m_HitClip;

        readonly Dictionary<Enemy, float> m_LastHit = new Dictionary<Enemy, float>();
        readonly List<Enemy> m_Forget = new List<Enemy>();
        Quaternion m_ArmRest = Quaternion.identity;
        Vector3 m_PreviousBall;
        float m_Phase;

        protected override void Awake()
        {
            if (m_Arm != null)
                m_ArmRest = m_Arm.localRotation;
            base.Awake();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsBuilt || m_Arm == null || m_Ball == null)
                return;

            var deltaTime = Time.deltaTime;
            var level = Mathf.Max(1, Level);
            m_Phase += deltaTime * OverchargeRate / Mathf.Max(0.5f, ValueFor(m_PeriodPerLevel, level, 3f));
            m_Phase %= 1f;
            m_Arm.localRotation = m_ArmRest * Quaternion.Euler(0f, 0f, m_Amplitude * Mathf.Sin(m_Phase * 2f * Mathf.PI));

            var ball = m_Ball.position;
            var motion = deltaTime > 0f ? (ball - m_PreviousBall) / deltaTime : Vector3.zero;
            m_PreviousBall = ball;
            motion.y = 0f;
            // Trop lent (au bout de sa course) : il ne fauche pas. Trop rapide : la première image, ou il vient d'être déplacé.
            if (motion.sqrMagnitude < 1f || motion.sqrMagnitude > 900f)
                return;

            var damage = ValueFor(m_DamagePerLevel, level, 20f) * Strength;
            var knock = ValueFor(m_KnockPerLevel, level, 5f);
            var side = motion.normalized;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive || enemy.IsFlyer || enemy.IsAirborne)
                    continue;
                if ((enemy.Center - ball).sqrMagnitude > (m_HitRadius + 0.4f) * (m_HitRadius + 0.4f))
                    continue;
                if (m_LastHit.TryGetValue(enemy, out var last) && Time.time - last < m_RehitDelay)
                    continue;

                m_LastHit[enemy] = Time.time;
                enemy.Health.TakeDamage(new DamageInfo
                {
                    Amount = damage,
                    Zone = HitZone.Body,
                    Point = enemy.Center,
                    Direction = side,
                    Source = this,
                });
                if (enemy.IsAlive)
                    enemy.Launch(side * knock + Vector3.up * 4f, this);
                Sfx.Play(m_HitClip, enemy.Center, 0.9f, Random.Range(0.9f, 1.1f));
            }

            ForgetOldHits();
        }

        void ForgetOldHits()
        {
            if (m_LastHit.Count < 16)
                return;

            m_Forget.Clear();
            foreach (var pair in m_LastHit)
            {
                if (pair.Key == null || Time.time - pair.Value > m_RehitDelay)
                    m_Forget.Add(pair.Key);
            }

            foreach (var enemy in m_Forget)
                m_LastHit.Remove(enemy);
        }
    }
}
