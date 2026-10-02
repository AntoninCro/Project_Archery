using System.Collections.Generic;
using UnityEngine;

namespace Archery.Bows
{
    /// <summary>
    /// Colliders que les flèches traversent sans les toucher : l'arc, le corps du joueur…
    /// </summary>
    public static class ArrowIgnore
    {
        static readonly HashSet<Collider> s_Ignored = new HashSet<Collider>();

        public static void Register(IEnumerable<Collider> colliders)
        {
            foreach (var collider in colliders)
            {
                if (collider != null)
                    s_Ignored.Add(collider);
            }
        }

        public static void Unregister(IEnumerable<Collider> colliders)
        {
            foreach (var collider in colliders)
            {
                if (collider != null)
                    s_Ignored.Remove(collider);
            }
        }

        public static bool IsIgnored(Collider collider) => s_Ignored.Contains(collider);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_Ignored.Clear();
    }
}
