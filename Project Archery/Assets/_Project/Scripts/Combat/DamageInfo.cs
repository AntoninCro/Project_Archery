using Archery.Bows;
using UnityEngine;

namespace Archery.Combat
{
    /// <summary>Zone touchée sur une cible vivante.</summary>
    public enum HitZone
    {
        Body,
        Head,
        WeakPoint,
    }

    /// <summary>Description d'un coup reçu.</summary>
    public struct DamageInfo
    {
        public float Amount;
        public HitZone Zone;
        public ShotGrade Grade;

        /// <summary>Distance du tir (m).</summary>
        public float Distance;

        public Vector3 Point;
        public Vector3 Direction;
        public Object Source;
    }

    /// <summary>Tout ce qui peut subir des dégâts.</summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(in DamageInfo info);
    }
}
