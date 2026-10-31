using Archery.FinalBoss;
using Archery.Upgrades;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Les flammes du feu des bâtiments (refonte du tower defense, étape 5, lot 5 ; 28 octobre) : sur un ennemi qui brûle et sur
    /// la pointe d'une flèche enflammée. Le brasero n'est plus dans la scène, son effet non plus : elles sont faites par le code
    /// (<see cref="FireEffects"/>, celles du dragon), avec le matériau de feu des flèches explosives.
    /// </summary>
    /// <remarks>
    /// Les modèles sont rangés sous un objet caché : une copie (<c>Instantiate</c>) est active et s'allume toute seule.
    /// </remarks>
    static class BurnFlames
    {
        static GameObject s_Enemy;
        static GameObject s_Arrow;

        /// <summary>Matériau du feu (<c>Fire_Particles</c>), null sans <see cref="SpecialArrows"/>.</summary>
        public static Material FireMaterial => SpecialArrows.Instance != null ? SpecialArrows.Instance.ExplosionFireMaterial : null;

        /// <summary>Modèle des flammes d'un ennemi qui brûle (pour <c>Burning.Apply</c>) ; null sans matériau de feu.</summary>
        public static GameObject Enemy => s_Enemy != null ? s_Enemy : s_Enemy = Create("Flammes de brûlure (modèle)", 0.7f, 45f);

        /// <summary>Modèle des petites flammes d'une pointe de flèche enflammée (pour <c>Arrow.Ignite</c>).</summary>
        public static GameObject Arrow => s_Arrow != null ? s_Arrow : s_Arrow = Create("Flammes de flèche (modèle)", 0.15f, 30f);

        static GameObject Create(string name, float size, float rate)
        {
            var material = FireMaterial;
            if (material == null)
                return null;

            var holder = new GameObject(name);
            holder.SetActive(false);
            var flames = FireEffects.CreateFireballFlames(holder.transform, material, size);
            var main = flames.main;
            main.playOnAwake = true;
            main.maxParticles = 80;
            var emission = flames.emission;
            emission.rateOverTime = rate;
            return flames.gameObject;
        }
    }
}
