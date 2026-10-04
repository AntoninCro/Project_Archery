using System.Collections.Generic;
using UnityEngine;

namespace Archery.Core
{
    /// <summary>
    /// Ralenti du temps (GDD, section 13 : ouverture d'un coffre). Plusieurs demandes peuvent se superposer :
    /// la plus lente gagne. Le temps passe en douceur d'une vitesse à l'autre, et revient à la normale quand plus
    /// personne ne le demande, ou quand la scène se décharge.
    /// </summary>
    /// <remarks>
    /// Le pas de la physique suit le ralenti, pour que les flèches et les ennemis restent fluides.
    /// L'objet qui pilote le ralenti est créé tout seul : il n'y a rien à placer dans la scène.
    /// </remarks>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public sealed class SlowMotion : MonoBehaviour
    {
        // Vitesse de la transition : de 1 à 0,3 en un quart de seconde environ (temps réel).
        const float k_TransitionSpeed = 3f;

        static readonly Dictionary<object, float> s_Requests = new Dictionary<object, float>();
        static SlowMotion s_Instance;
        static float s_NormalFixedDeltaTime = -1f;

        /// <summary>Le temps est ralenti, ou en train de revenir à la normale.</summary>
        public static bool IsActive => s_Instance != null;

        /// <summary>Demande un ralenti (0,3 = trois fois plus lent). Un nouvel appel du même demandeur remplace le précédent.</summary>
        public static void Begin(object owner, float timeScale)
        {
            if (owner == null)
                return;

            s_Requests[owner] = Mathf.Clamp(timeScale, 0.05f, 1f);
            if (s_Instance == null)
                new GameObject("[Slow Motion]").AddComponent<SlowMotion>();
        }

        /// <summary>Retire la demande de ce demandeur.</summary>
        public static void End(object owner)
        {
            if (owner != null)
                s_Requests.Remove(owner);
        }

        static float TargetScale
        {
            get
            {
                var target = 1f;
                foreach (var scale in s_Requests.Values)
                    target = Mathf.Min(target, scale);
                return target;
            }
        }

        void Awake()
        {
            s_Instance = this;
            if (s_NormalFixedDeltaTime < 0f)
                s_NormalFixedDeltaTime = Time.fixedDeltaTime;
        }

        void Update()
        {
            var target = TargetScale;
            Time.timeScale = Mathf.MoveTowards(Time.timeScale, target, k_TransitionSpeed * Time.unscaledDeltaTime);
            Time.fixedDeltaTime = s_NormalFixedDeltaTime * Time.timeScale;

            if (s_Requests.Count == 0 && Time.timeScale >= 1f)
                Destroy(gameObject);
        }

        // Le temps revient toujours à la normale, même si la scène est rechargée pendant un ralenti.
        void OnDestroy()
        {
            if (s_Instance != this)
                return;

            s_Instance = null;
            s_Requests.Clear();
            Time.timeScale = 1f;
            if (s_NormalFixedDeltaTime > 0f)
                Time.fixedDeltaTime = s_NormalFixedDeltaTime;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Requests.Clear();
            s_Instance = null;
            s_NormalFixedDeltaTime = -1f;
        }
    }
}
