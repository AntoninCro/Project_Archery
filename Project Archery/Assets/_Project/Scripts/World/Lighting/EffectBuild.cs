using UnityEngine;

namespace Archery.World
{
    /// <summary>
    /// Petits outils pour fabriquer les effets (lumières de la carte et de l'arène) aussi bien en jeu que dans l'éditeur,
    /// où ils sont enregistrés avec la scène sous l'objet « Effet ».
    /// </summary>
    static class EffectBuild
    {
        /// <summary>Détruit un objet : tout de suite dans l'éditeur, en fin d'image en jeu.</summary>
        public static void Kill(Object target)
        {
            if (target == null)
                return;
            if (Application.isPlaying)
                Object.Destroy(target);
            else
                Object.DestroyImmediate(target);
        }

        /// <summary>Le groupe de ce nom sous <paramref name="parent"/> (créé s'il n'existe pas).</summary>
        public static Transform Group(Transform parent, string name)
        {
            var group = parent.Find(name);
            if (group != null)
                return group;

            group = new GameObject(name).transform;
            group.SetParent(parent, false);
            return group;
        }

        /// <summary>Le maillage de la sphère d'Unity (un asset intégré : rien à détruire).</summary>
        public static Mesh SphereMesh()
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var mesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            Kill(sphere);
            return mesh;
        }

        /// <summary>Un cylindre d'Unity sans collider (pour les rayons de lumière).</summary>
        public static GameObject Cylinder(string name, Transform parent)
        {
            var cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            Kill(cylinder.GetComponent<Collider>());
            cylinder.transform.SetParent(parent, false);
            return cylinder;
        }

        /// <summary>
        /// Une copie du matériau Glow, de cette couleur et de cet éclat : c'est l'aspect de l'objet dans l'éditeur, et
        /// la couleur de départ en jeu (on peut la changer dans le matériau).
        /// </summary>
        public static Material GlowMaterial(Material glow, Color color, float intensity, string name)
        {
            var material = new Material(glow) { name = name };
            material.SetColor("_Color", color);
            material.SetFloat("_Intensity", intensity);
            return material;
        }

        /// <summary>Couleur du matériau Glow (blanc s'il n'en a pas).</summary>
        public static Color GlowColor(Renderer renderer)
        {
            var material = renderer != null ? renderer.sharedMaterial : null;
            return material != null && material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
        }

        /// <summary>Un nombre entre 0 et 1 tiré de la position (même valeur à chaque lancement).</summary>
        public static float Hash(Vector3 position)
        {
            var h = Mathf.Sin(position.x * 12.9898f + position.z * 78.233f + position.y * 37.719f) * 43758.5453f;
            return h - Mathf.Floor(h);
        }
    }
}
