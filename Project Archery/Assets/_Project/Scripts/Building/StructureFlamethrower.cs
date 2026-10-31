using System.Collections.Generic;
using Archery.Bows;
using Archery.Core;
using Archery.Enemies;
using Archery.FinalBoss;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Lance-flammes (refonte du tower defense, étape 5, lot 5 ; 28 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, partie 6.1) :
    /// au bord de la voie, tourné vers elle. Quand des ennemis sont dans son cône (volants compris), il crache un jet de feu :
    /// chacun prend une brûlure (celle des flèches enflammées du brasero, <see cref="Burning"/>), puis il se recharge. Plus loin,
    /// plus fort et plus souvent à chaque niveau. Rare.
    /// </summary>
    /// <remarks>
    /// Le jet est le souffle de feu du dragon (<see cref="FireEffects.CreateBreath"/>), qui part de la buse (<c>Nozzle</c>,
    /// axe Z) pendant moins d'une seconde ; les flammes des ennemis qui brûlent viennent de <see cref="BurnFlames"/>.
    /// </remarks>
    public class StructureFlamethrower : Structure
    {
        [Tooltip("Portée (m) du cône de feu, par niveau.")]
        [SerializeField]
        float[] m_RangePerLevel = { 6f, 7f, 8f };

        [Tooltip("Demi-angle (°) du cône.")]
        [SerializeField]
        float m_HalfAngle = 25f;

        [Tooltip("Dégâts de la brûlure (en tout), par niveau, à la vague 1 en Normal (ils suivent ensuite la force des ennemis).")]
        [SerializeField]
        float[] m_BurnDamagePerLevel = { 12f, 18f, 27f };

        [Tooltip("Durée (s) de la brûlure.")]
        [SerializeField]
        float m_BurnDuration = 3f;

        [Tooltip("Temps (s) entre deux jets, par niveau.")]
        [SerializeField]
        float[] m_IntervalPerLevel = { 3f, 2.6f, 2.2f };

        [Tooltip("Buse : le jet part de là, le long de son axe Z.")]
        [SerializeField]
        Transform m_Nozzle;

        [Tooltip("Durée (s) d'un jet.")]
        [SerializeField]
        float m_JetTime = 0.8f;

        [SerializeField]
        AudioClip m_BlastClip;

        readonly List<Enemy> m_InCone = new List<Enemy>();
        ParticleSystem m_Jet;
        float m_Cooldown = 1f;
        float m_JetTimer;

        Transform Nozzle => m_Nozzle != null ? m_Nozzle : transform;

        protected override void Update()
        {
            base.Update();
            if (!IsBuilt)
                return;

            if (m_JetTimer > 0f)
            {
                m_JetTimer -= Time.deltaTime;
                if (m_JetTimer <= 0f && m_Jet != null)
                    m_Jet.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            m_Cooldown -= Time.deltaTime * OverchargeRate;
            if (m_Cooldown > 0f)
                return;

            var level = Mathf.Max(1, Level);
            var range = ValueFor(m_RangePerLevel, level, 6f) * RangeScale * OverchargeRange;
            FindInCone(range);
            if (m_InCone.Count == 0)
            {
                m_Cooldown = 0.25f;
                return;
            }

            m_Cooldown = ValueFor(m_IntervalPerLevel, level, 3f);
            var damage = ValueFor(m_BurnDamagePerLevel, level, 12f) * Strength;
            foreach (var enemy in m_InCone)
            {
                if (enemy != null && enemy.IsAlive)
                    Burning.Apply(enemy, damage, m_BurnDuration, ShotGrade.None, 0f, BurnFlames.Enemy);
            }

            ShowJet(range);
            Sfx.Play(m_BlastClip, Nozzle.position, 0.9f, Random.Range(0.9f, 1.1f));
        }

        protected override void OnDestroy()
        {
            if (m_Jet != null)
                Destroy(m_Jet.gameObject);
            base.OnDestroy();
        }

        // Le jet n'est pas une pièce du bâtiment (il est fait au premier tir, sous la buse).
        protected override bool IsBlueprintPart(Renderer renderer) => m_Jet == null || !renderer.transform.IsChildOf(m_Jet.transform);

        void FindInCone(float range)
        {
            m_InCone.Clear();
            var origin = transform.position;
            var forward = transform.forward;
            forward.y = 0f;
            forward.Normalize();
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.IsAlive)
                    continue;

                var offset = enemy.Center - origin;
                if (offset.y < -3f || offset.y > 6f)
                    continue;
                offset.y = 0f;
                var distance = offset.magnitude;
                if (distance <= range && (distance < 0.5f || Vector3.Angle(forward, offset) <= m_HalfAngle))
                    m_InCone.Add(enemy);
            }
        }

        // Le souffle de feu du dragon, à la portée du niveau, pendant un instant. Fait au premier jet.
        void ShowJet(float range)
        {
            if (m_Jet == null)
            {
                var material = BurnFlames.FireMaterial;
                if (material == null)
                    return;
                m_Jet = FireEffects.CreateBreath(Nozzle, material, range, m_HalfAngle);
            }

            FireEffects.SetBreathRange(m_Jet, range);
            m_Jet.Play(true);
            m_JetTimer = m_JetTime;
        }

        void OnDrawGizmosSelected()
        {
            var range = ValueFor(m_RangePerLevel, Mathf.Max(1, Level), 6f);
            Gizmos.color = new Color(1f, 0.5f, 0.15f, 0.6f);
            var origin = Nozzle.position;
            Gizmos.DrawLine(origin, origin + Quaternion.Euler(0f, m_HalfAngle, 0f) * transform.forward * range);
            Gizmos.DrawLine(origin, origin + Quaternion.Euler(0f, -m_HalfAngle, 0f) * transform.forward * range);
        }
    }
}
