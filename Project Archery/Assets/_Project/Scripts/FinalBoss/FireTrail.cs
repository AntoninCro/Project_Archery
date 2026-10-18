using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Player;
using UnityEngine;

namespace Archery.FinalBoss
{
    /// <summary>Plaque de feu au sol laissée par le dragon en piqué : elle brûle le joueur qui marche dedans, puis s'éteint.</summary>
    public class FireTrail : MonoBehaviour
    {
        float m_Radius;
        float m_Lifetime;
        float m_DamagePerSecond;
        float m_Age;
        float m_TickTimer;
        ParticleSystem m_Flames;
        Object m_Source;

        public static void Spawn(Vector3 groundPoint, float radius, float lifetime, float damagePerSecond, Material fireMaterial, Object source)
        {
            var go = new GameObject("Traînée de feu");
            go.transform.position = groundPoint + Vector3.up * 0.05f;
            var trail = go.AddComponent<FireTrail>();
            trail.m_Radius = radius;
            trail.m_Lifetime = lifetime;
            trail.m_DamagePerSecond = damagePerSecond;
            trail.m_Source = source;
            trail.m_Flames = FireEffects.CreateGroundFlames(go.transform, fireMaterial, radius * 0.8f, 40f);
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;
            m_Age += deltaTime;
            if (m_Age >= m_Lifetime)
            {
                if (m_Flames != null && m_Flames.isEmitting)
                    m_Flames.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (m_Age >= m_Lifetime + 1f)
                    Destroy(gameObject);
                return;
            }

            m_TickTimer -= deltaTime;
            if (m_TickTimer > 0f)
                return;

            const float tick = 0.25f;
            m_TickTimer = tick;
            if (ArenaPlayer.FeetInside(transform.position, m_Radius))
                ArenaPlayer.Damage(m_DamagePerSecond * tick, transform.position, m_Source);
        }
    }
}
