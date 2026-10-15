using UnityEngine;

namespace Archery.World
{
    /// <summary>
    /// Réglages des lumières de la carte (<see cref="MapLighting"/>) : les matériaux utilisés et, pour chaque effet, sa
    /// quantité, sa couleur et sa force. Un seul asset, « Resources/MapLighting », chargé au lancement ; on le règle dans
    /// l'Inspector, même pendant le jeu (la plupart des couleurs et des forces changent tout de suite).
    /// </summary>
    /// <remarks>
    /// Tout est plus vif la nuit : la force des lumières suit le réglage « Lamps » du ciel de la difficulté (0 en plein
    /// jour, 1 la nuit).
    /// </remarks>
    [CreateAssetMenu(fileName = "MapLighting", menuName = "Archery/Map Lighting Settings")]
    public class MapLightingSettings : ScriptableObject
    {
        public const string ResourceName = "MapLighting";

        [Header("Matériaux")]
        [Tooltip("Particules additives : flammes, lucioles, pollen, âmes, étincelles (Fire_Particles).")]
        public Material fireMaterial;

        [Tooltip("Cœur des torches et phare de la tour : un feu animé (Dragon_Fireball).")]
        public Material flameCoreMaterial;

        [Tooltip("Manche des torches.")]
        public Material woodMaterial;

        [Tooltip("Corbeille et support des torches.")]
        public Material ironMaterial;

        [Tooltip("Cristaux des falaises (shader Archery/Crystal, comme ceux du plafond de l'arène).")]
        public Material crystalMaterial;

        [Tooltip("Lueurs ajoutées : runes, veines de l'antre, arc-en-ciel (shader Archery/Glow).")]
        public Material glowMaterial;

        [Tooltip("Rayons de lumière : coucher de soleil, phare de la tour (Light_Beam).")]
        public Material beamMaterial;

        [Tooltip("Tonnerre, après un éclair (joué grave).")]
        public AudioClip thunderClip;

        [Header("Torches des voies")]
        [Tooltip("Une torche tous les tant de mètres le long de chaque voie, d'un côté puis de l'autre.")]
        public float laneTorchSpacing = 9f;

        [Tooltip("Une vraie lumière toutes les N torches (les autres n'ont que leur flamme) : en VR, les lumières coûtent cher.")]
        [Min(1)]
        public int lightEvery = 2;

        public Color torchColor = new Color(1f, 0.55f, 0.2f);

        public float torchLightRange = 7f;

        public float torchLightIntensity = 2.5f;

        [Tooltip("Durée (s) de l'allumage en chaîne d'une voie, de l'apparition des ennemis jusqu'à la tour, au début d'une vague.")]
        public float igniteDuration = 4f;

        [Tooltip("Quand un ennemi apparaît, une vague rouge parcourt les torches de sa voie, vers la tour.")]
        public Color alarmColor = new Color(1f, 0.12f, 0.06f);

        [Tooltip("Vitesse (m/s) de la vague rouge.")]
        public float alarmSpeed = 22f;

        [Header("Torches de la forêt")]
        [Min(0)]
        public int forestTorches = 14;

        [Header("Lucioles")]
        [Min(0)]
        public int fireflyGroups = 24;

        [Min(1)]
        public int firefliesPerGroup = 9;

        public Color fireflyColor = new Color(0.75f, 1f, 0.35f);

        [Header("Plantes lumineuses")]
        [Tooltip("Lueur des champignons rouges.")]
        public Color redMushroomGlow = new Color(1f, 0.25f, 0.55f);

        [Tooltip("Lueur des champignons beiges.")]
        public Color tanMushroomGlow = new Color(0.25f, 0.75f, 1f);

        public float mushroomGlow = 1.6f;

        [Tooltip("Part des fleurs dont les pétales luisent la nuit.")]
        [Range(0f, 1f)]
        public float flowerShare = 0.35f;

        public float flowerGlow = 1.2f;

        [Header("Cristaux des falaises")]
        [Min(0)]
        public int cliffCrystals = 16;

        public Color crystalColor = new Color(0.3f, 1f, 0.85f);

        public float crystalIntensity = 2.5f;

        [Header("Jour")]
        [Tooltip("Rayons de lumière au coucher du soleil, dans la forêt (0 : aucun).")]
        [Min(0)]
        public int lightShafts = 8;

        [Tooltip("Pollen et poussières dorées qui flottent autour du joueur en plein jour.")]
        public Color moteColor = new Color(1f, 0.88f, 0.55f);

        [Header("Cascades")]
        [Tooltip("Brume au pied des cascades.")]
        public bool waterfallMist = true;

        [Tooltip("Arc-en-ciel dans la brume de la plus haute cascade, le jour.")]
        public bool rainbow = true;

        [Header("Rivière")]
        [Tooltip("Couleur des remous lumineux quand un ennemi traverse le gué (la lueur de l'eau se règle dans le matériau de l'eau).")]
        public Color rippleColor = new Color(0.3f, 1f, 0.9f);

        [Header("Phare de la tour")]
        public Color beaconFull = new Color(0.35f, 1f, 0.5f);

        public Color beaconHalf = new Color(1f, 0.7f, 0.2f);

        public Color beaconLow = new Color(1f, 0.15f, 0.1f);

        [Header("Emplacements de construction")]
        public Color runeColor = new Color(0.45f, 0.85f, 1f);

        [Header("Ennemis")]
        [Tooltip("Les âmes qui s'envolent d'un ennemi vaincu.")]
        public Color soulColor = new Color(0.7f, 0.9f, 1f);

        [Tooltip("Portée (m) de la lueur des projectiles ennemis, la nuit.")]
        public float projectileLightRange = 5f;

        [Header("Joueur")]
        [Tooltip("Petite lumière douce autour du joueur, la nuit.")]
        public float playerLightRange = 7f;

        public float playerLightIntensity = 0.7f;

        public Color playerLightColor = new Color(1f, 0.85f, 0.65f);

        [Header("Antre du boss")]
        [Tooltip("Veines rouges dans la face de pierre, plus vives à chaque vague.")]
        public Color veinColor = new Color(1f, 0.15f, 0.05f);

        [Min(0)]
        public int veins = 10;

        [Header("Arène du boss final")]
        [Tooltip("Veines de lave dans les parois de l'arène, qui battent sur la musique.")]
        [Min(0)]
        public int arenaWallVeins = 18;

        [Tooltip("Veines de lave dans la voûte.")]
        [Min(0)]
        public int arenaCeilingVeins = 10;

        public Color arenaVeinColor = new Color(1f, 0.28f, 0.05f);

        [Tooltip("Braises qui montent dans l'air de la grotte.")]
        public Color emberColor = new Color(1f, 0.5f, 0.15f);

        [Tooltip("Sol brûlant là où frappent les boules de feu et le souffle (il refroidit en quelques secondes).")]
        public Color scorchColor = new Color(1f, 0.45f, 0.1f);

        [Tooltip("Sol qui chauffe là où le souffle va frapper.")]
        public Color warningColor = new Color(1f, 0.18f, 0.05f);

        [Tooltip("Rayons de soleil qui percent la voûte quand le dragon est vaincu.")]
        public Color sunColor = new Color(1f, 0.93f, 0.78f);

        [Tooltip("Petite lumière douce autour du joueur dans l'arène.")]
        public float arenaPlayerLightIntensity = 0.5f;

        [Header("Piliers de l'arène")]
        [Tooltip("Contour lumineux des piliers (shader Archery/FrostOverlay) : on les voit de partout, même dans le brouillard.")]
        public Material pillarRimMaterial;

        [Tooltip("Lumière froide qui éclaire chaque pilier par en dessous.")]
        public Color pillarLightColor = new Color(0.55f, 0.72f, 1f);

        [Tooltip("Force de cette lumière. Elle s'affaiblit vite avec la distance et l'obsidienne est sombre : il en faut beaucoup.")]
        public float pillarLightIntensity = 25f;

        [Tooltip("Couronne de cristaux au sommet des piliers (et petits cristaux au pied de la lumière).")]
        public Color pillarCrystalColor = new Color(0.35f, 0.88f, 1f);

        [Tooltip("Abri derrière les piliers, éclairé au sol pendant que le dragon prépare son souffle.")]
        public Color coverColor = new Color(0.3f, 0.75f, 1f);
    }
}
