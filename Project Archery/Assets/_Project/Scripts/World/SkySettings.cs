using System;
using UnityEngine;

namespace Archery.World
{
    /// <summary>
    /// Un ciel (GDD, section 11) : dégradé de couleurs, soleil ou lune, lumière principale, lumière ambiante
    /// et brouillard. Chaque difficulté a le sien ; <see cref="SkyController"/> l'applique à la scène.
    /// </summary>
    [Serializable]
    public class SkySettings
    {
        [Header("Soleil ou lune")]
        [Tooltip("Hauteur de l'astre au-dessus de l'horizon (°). 90 = au zénith.")]
        [Range(0f, 90f)]
        public float elevation = 7f;

        [Tooltip("Direction de l'astre (°) : 0 = vers +Z, côté d'où arrivent les ennemis ; 90 = vers +X.")]
        [Range(-180f, 180f)]
        public float azimuth = -65f;

        [Tooltip("Rayon apparent du disque (°).")]
        [Range(0.5f, 10f)]
        public float discSize = 2.8f;

        public Color discColor = new Color(1f, 0.8f, 0.55f);

        [Tooltip("Halo autour du disque, ajouté au ciel.")]
        public Color glowColor = new Color(0.7f, 0.35f, 0.15f);

        [Tooltip("Étendue du halo (°).")]
        [Range(1f, 60f)]
        public float glowSize = 25f;

        [Tooltip("0 = soleil, 1 = lune avec ses taches sombres.")]
        [Range(0f, 1f)]
        public float moonSurface;

        [Header("Ciel")]
        public Color zenithColor = new Color(0.2f, 0.27f, 0.5f);

        [Tooltip("Aussi la couleur du brouillard : le décor lointain se fond dans l'horizon.")]
        public Color horizonColor = new Color(0.98f, 0.6f, 0.38f);

        [Tooltip("Sous l'horizon (en général caché par le sol).")]
        public Color groundColor = new Color(0.25f, 0.2f, 0.2f);

        [Tooltip("Plus la valeur est grande, plus la bande claire de l'horizon est fine.")]
        [Range(0.5f, 8f)]
        public float horizonSharpness = 2.2f;

        [Range(0f, 1f)]
        public float stars;

        [Header("Lumière principale (vient de l'astre)")]
        public Color lightColor = new Color(1f, 0.62f, 0.36f);

        [Range(0f, 2f)]
        public float lightIntensity = 1f;

        [Range(0f, 1f)]
        public float shadowStrength = 0.7f;

        [Header("Ambiance")]
        [Tooltip("Lumière ambiante venant du haut.")]
        public Color ambientSky = new Color(0.42f, 0.42f, 0.56f);

        [Tooltip("Lumière ambiante venant de l'horizon.")]
        public Color ambientEquator = new Color(0.62f, 0.45f, 0.38f);

        [Tooltip("Lumière ambiante venant du sol.")]
        public Color ambientGround = new Color(0.24f, 0.2f, 0.18f);

        [Tooltip("Densité du brouillard (0 = aucun).")]
        [Range(0f, 0.05f)]
        public float fogDensity = 0.006f;

        [Tooltip("Intensité des reflets du ciel sur les objets brillants (à baisser la nuit).")]
        [Range(0f, 1f)]
        public float reflections = 0.7f;

        [Tooltip("Ciel de nuit : allume les objets « Night Only » (lanterne de la tour, yeux des ennemis).")]
        public bool night;

        /// <summary>Direction de l'astre vue depuis le sol (vecteur unitaire).</summary>
        public Vector3 DiscDirection => Quaternion.Euler(-elevation, azimuth, 0f) * Vector3.forward;

        /// <summary>Rotation de la lumière directionnelle : elle part de l'astre vers le sol.</summary>
        public Quaternion LightRotation => Quaternion.Euler(elevation, azimuth + 180f, 0f);

        public void CopyFrom(SkySettings other) => Lerp(other, other, 0f);

        /// <summary>Mélange deux ciels dans celui-ci (t = 0 : <paramref name="from"/>, t = 1 : <paramref name="to"/>).</summary>
        public void Lerp(SkySettings from, SkySettings to, float t)
        {
            elevation = Mathf.Lerp(from.elevation, to.elevation, t);
            azimuth = Mathf.LerpAngle(from.azimuth, to.azimuth, t);
            discSize = Mathf.Lerp(from.discSize, to.discSize, t);
            discColor = Color.Lerp(from.discColor, to.discColor, t);
            glowColor = Color.Lerp(from.glowColor, to.glowColor, t);
            glowSize = Mathf.Lerp(from.glowSize, to.glowSize, t);
            moonSurface = Mathf.Lerp(from.moonSurface, to.moonSurface, t);
            zenithColor = Color.Lerp(from.zenithColor, to.zenithColor, t);
            horizonColor = Color.Lerp(from.horizonColor, to.horizonColor, t);
            groundColor = Color.Lerp(from.groundColor, to.groundColor, t);
            horizonSharpness = Mathf.Lerp(from.horizonSharpness, to.horizonSharpness, t);
            stars = Mathf.Lerp(from.stars, to.stars, t);
            lightColor = Color.Lerp(from.lightColor, to.lightColor, t);
            lightIntensity = Mathf.Lerp(from.lightIntensity, to.lightIntensity, t);
            shadowStrength = Mathf.Lerp(from.shadowStrength, to.shadowStrength, t);
            ambientSky = Color.Lerp(from.ambientSky, to.ambientSky, t);
            ambientEquator = Color.Lerp(from.ambientEquator, to.ambientEquator, t);
            ambientGround = Color.Lerp(from.ambientGround, to.ambientGround, t);
            fogDensity = Mathf.Lerp(from.fogDensity, to.fogDensity, t);
            reflections = Mathf.Lerp(from.reflections, to.reflections, t);
            night = t < 0.5f ? from.night : to.night;
        }

        // Les quatre ciels du GDD (section 11). Valeurs de départ, à ajuster à l'œil dans les assets.

        /// <summary>Facile : soleil presque au zénith.</summary>
        public static SkySettings Noon() => new SkySettings
        {
            elevation = 75f,
            azimuth = 30f,
            discSize = 2.2f,
            discColor = new Color(1f, 0.97f, 0.88f),
            glowColor = new Color(0.45f, 0.42f, 0.32f),
            glowSize = 9f,
            moonSurface = 0f,
            zenithColor = new Color(0.24f, 0.52f, 0.92f),
            horizonColor = new Color(0.7f, 0.85f, 0.97f),
            groundColor = new Color(0.36f, 0.4f, 0.32f),
            horizonSharpness = 3f,
            stars = 0f,
            lightColor = new Color(1f, 0.96f, 0.88f),
            lightIntensity = 1.25f,
            shadowStrength = 0.75f,
            ambientSky = new Color(0.55f, 0.65f, 0.8f),
            ambientEquator = new Color(0.5f, 0.55f, 0.55f),
            ambientGround = new Color(0.3f, 0.3f, 0.25f),
            fogDensity = 0.004f,
            reflections = 1f,
            night = false,
        };

        /// <summary>Normal : coucher de soleil, à gauche du chemin des ennemis.</summary>
        public static SkySettings Sunset() => new SkySettings();

        /// <summary>Difficile : nuit, lune claire.</summary>
        public static SkySettings Night() => new SkySettings
        {
            elevation = 35f,
            azimuth = 40f,
            discSize = 3.5f,
            discColor = new Color(0.92f, 0.95f, 1f),
            glowColor = new Color(0.12f, 0.16f, 0.28f),
            glowSize = 12f,
            moonSurface = 1f,
            zenithColor = new Color(0.01f, 0.02f, 0.06f),
            horizonColor = new Color(0.06f, 0.1f, 0.2f),
            groundColor = new Color(0.02f, 0.03f, 0.04f),
            horizonSharpness = 2.5f,
            stars = 1f,
            lightColor = new Color(0.62f, 0.72f, 1f),
            lightIntensity = 0.6f,
            shadowStrength = 0.6f,
            ambientSky = new Color(0.2f, 0.25f, 0.42f),
            ambientEquator = new Color(0.14f, 0.17f, 0.28f),
            ambientGround = new Color(0.07f, 0.07f, 0.1f),
            fogDensity = 0.012f,
            reflections = 0.2f,
            night = true,
        };

        /// <summary>Impossible : ciel et lune rouges, la lune juste au-dessus du chemin des ennemis.</summary>
        public static SkySettings BloodMoon() => new SkySettings
        {
            elevation = 22f,
            azimuth = 0f,
            discSize = 6f,
            discColor = new Color(1f, 0.28f, 0.18f),
            glowColor = new Color(0.45f, 0.06f, 0.03f),
            glowSize = 18f,
            moonSurface = 1f,
            zenithColor = new Color(0.06f, 0f, 0.01f),
            horizonColor = new Color(0.3f, 0.04f, 0.03f),
            groundColor = new Color(0.05f, 0.01f, 0.01f),
            horizonSharpness = 2f,
            stars = 0.4f,
            lightColor = new Color(1f, 0.35f, 0.28f),
            lightIntensity = 0.55f,
            shadowStrength = 0.6f,
            ambientSky = new Color(0.3f, 0.1f, 0.1f),
            ambientEquator = new Color(0.22f, 0.07f, 0.07f),
            ambientGround = new Color(0.08f, 0.02f, 0.02f),
            fogDensity = 0.015f,
            reflections = 0.25f,
            night = true,
        };
    }
}
