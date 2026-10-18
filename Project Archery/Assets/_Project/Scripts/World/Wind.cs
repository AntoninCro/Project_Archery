using UnityEngine;

namespace Archery.World
{
    /// <summary>
    /// Le vent qui fait bouger les arbres, les herbes, les fleurs et les plantes de la carte (shader Archery/Foliage) :
    /// sa direction, sa force et sa vitesse, envoyées à tous les shaders au lancement du jeu, et dans l'éditeur pour le
    /// voir dans la vue Scene (avec « Always Refresh »). Rien à poser dans les scènes.
    /// </summary>
    /// <remarks>
    /// Les modèles de végétation sont préparés à leur import par FoliageWindImporter (dossier Editor). Pour changer le
    /// vent pendant le jeu (une tempête…), appeler <see cref="Set"/>.
    /// </remarks>
    public static class Wind
    {
        static readonly int k_WindId = Shader.PropertyToID("_ArcheryWind");

        /// <summary>Direction du vent dans le plan horizontal (X, Z du monde).</summary>
        public static Vector2 Direction { get; private set; } = new Vector2(1f, 0.35f);

        /// <summary>
        /// Force : 1 = brise légère (une herbe se couche de quelques centimètres) ; 1,5 = le vent de la carte (relevé le
        /// 15 octobre) ; 0 = aucun vent.
        /// </summary>
        public static float Strength { get; private set; } = 1.5f;

        /// <summary>Vitesse des oscillations : 1 = normale.</summary>
        public static float Speed { get; private set; } = 1f;

        /// <summary>Change le vent, pour toute la végétation.</summary>
        public static void Set(Vector2 direction, float strength, float speed)
        {
            Direction = direction.sqrMagnitude > 1e-4f ? direction.normalized : Vector2.right;
            Strength = Mathf.Max(0f, strength);
            Speed = Mathf.Max(0f, speed);
            Apply();
        }

        static void Apply()
        {
            var direction = Direction.sqrMagnitude > 1e-4f ? Direction.normalized : Vector2.right;
            Shader.SetGlobalVector(k_WindId, new Vector4(direction.x, direction.y, Strength, Speed));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ApplyOnLaunch() => Apply();

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void ApplyInEditor() => Apply();
#endif
    }
}
