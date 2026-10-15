using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Player;
using Archery.UI;
using UnityEngine;

namespace Archery.FinalBoss
{
    /// <summary>Le joueur vu par les dangers de l'arène : ses pieds, sa poitrine, et les dégâts qu'il reçoit.</summary>
    public static class ArenaPlayer
    {
        public static bool IsAlive => PlayerHealth.Instance != null && PlayerHealth.Instance.IsAlive;

        public static Vector3 Feet => PlayerHealth.Instance != null ? PlayerHealth.Instance.BodyPosition : Vector3.zero;

        /// <summary>Le haut du corps : 30 cm sous les yeux.</summary>
        public static Vector3 Chest
        {
            get
            {
                var rig = PlayerRig.Instance;
                return rig != null && rig.Head != null ? rig.Head.position + Vector3.down * 0.3f : Feet + Vector3.up * 1.3f;
            }
        }

        public static void Damage(float amount, Vector3 point, Object source)
        {
            var player = PlayerHealth.Instance;
            if (player == null || !player.IsAlive || amount <= 0f)
                return;

            var direction = Chest - point;
            player.Health.TakeDamage(new DamageInfo
            {
                Amount = amount,
                Zone = HitZone.Body,
                Point = point,
                Direction = direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector3.down,
                Source = source,
            });
        }

        /// <summary>Vrai si le joueur a les pieds dans ce disque (au sol, à 1,5 m de hauteur près).</summary>
        public static bool FeetInside(Vector3 center, float radius)
        {
            if (!IsAlive)
                return false;

            var offset = Feet - center;
            if (Mathf.Abs(offset.y) > 1.5f)
                return false;
            offset.y = 0f;
            return offset.sqrMagnitude <= radius * radius;
        }

        /// <summary>Hauteur des pieds au-dessus du sol de l'arène : le joueur saute.</summary>
        public static float HeightAboveGround(ArenaShape arena)
        {
            var feet = Feet;
            return arena != null ? feet.y - arena.GroundHeightAt(feet) : 0f;
        }

        /// <summary>Le joueur a esquivé une attaque en sautant : un petit mot devant ses yeux.</summary>
        public static void ShowDodged()
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            var forward = head != null ? Vector3.ProjectOnPlane(head.forward, Vector3.up) : Vector3.forward;
            var position = head != null ? head.position + (forward.sqrMagnitude > 1e-4f ? forward.normalized : Vector3.forward) * 1.2f + Vector3.down * 0.2f : Chest;
            FloatingText.Spawn(position, "Esquivé !", new Color(0.55f, 1f, 0.5f), 0.8f, 1f);
        }

        /// <summary>Vibration des deux manettes (rugissement, explosion proche).</summary>
        public static void Rumble(float amplitude, float duration)
        {
            var rig = PlayerRig.Instance;
            if (rig == null)
                return;

            foreach (var hand in rig.Hands)
            {
                if (hand != null)
                    Haptics.Pulse(hand, amplitude, duration);
            }
        }
    }
}
