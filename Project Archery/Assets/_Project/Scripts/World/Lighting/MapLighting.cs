using System.Collections;
using Archery.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archery.World
{
    /// <summary>
    /// L'objet « Effet » de la carte : toutes les lumières ajoutées, rangées par groupe, enregistrées avec la scène.
    /// <list type="bullet">
    /// <item>« Torches » (<see cref="MapTorches"/>) : torches des voies et de la forêt ;</item>
    /// <item>« Nature » (<see cref="MapNatureLights"/>) : lucioles, cristaux des parois, pollen, rayons du soleil, brume et
    /// arc-en-ciel des cascades (et, en jeu, les plantes qui luisent) ;</item>
    /// <item>« Jeu » (<see cref="MapGameplayLights"/>) : anneau et rayon de la tour, runes des emplacements, âmes, lueur
    /// du joueur (et, en jeu, les remous de la rivière et la lueur des projectiles) ;</item>
    /// <item>« Antre » (<see cref="MapLairVeins"/>) : les veines de lave autour de la porte de l'antre.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// On peut déplacer, modifier, dupliquer ou supprimer chaque objet : en jeu, ils sont animés tels qu'ils sont (leur
    /// couleur, leur force et leur place servent de départ). Cacher « Effet » (ou un groupe) éteint tout (ou ce groupe).
    /// L'objet est créé par l'éditeur à l'ouverture de la scène s'il n'existe pas (menu Archery > Effets pour le
    /// refaire) ; sans lui, tout est fabriqué au lancement. Réglages : « Resources/MapLighting ».
    /// </remarks>
    [DisallowMultipleComponent]
    public class MapLighting : MonoBehaviour
    {
        public const string RootName = "Effet";

        [Tooltip("Les lumières ont été fabriquées dans l'éditeur et enregistrées avec la scène : en jeu, on les anime telles " +
                 "qu'elles sont, sans en refaire.")]
        [SerializeField]
        bool m_Baked;

        MapLightingSettings m_Settings;

        /// <summary>Fabriqué au lancement (pas enregistré dans la scène) : à détruire avec la scène.</summary>
        public bool RuntimeCreated { get; private set; }

        static MapLightingSettings LoadSettings()
        {
            var settings = Resources.Load<MapLightingSettings>(MapLightingSettings.ResourceName);
            if (settings == null)
                Debug.LogWarning("MapLighting : réglages « Resources/MapLighting » introuvables, pas de lumières sur la carte.");
            return settings;
        }

        /// <summary>L'objet « Effet » de cette scène, même caché ; null s'il n'y en a pas.</summary>
        public static MapLighting FindIn(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.TryGetComponent<MapLighting>(out var lighting))
                    return lighting;
            }

            return null;
        }

        /// <summary>
        /// Au lancement (par le <see cref="SkyController"/>) : l'objet « Effet » de la scène s'il existe (caché : rien ne
        /// s'allume), sinon un neuf, dont les lumières seront fabriquées.
        /// </summary>
        public static MapLighting Attach(Scene scene)
        {
            var existing = FindIn(scene);
            if (existing != null)
                return existing;

            var lighting = new GameObject(RootName).AddComponent<MapLighting>();
            if (scene.IsValid() && scene.isLoaded)
                SceneManager.MoveGameObjectToScene(lighting.gameObject, scene);
            lighting.RuntimeCreated = true;
            return lighting;
        }

        /// <summary>
        /// Éditeur : crée l'objet « Effet » dans cette scène et y fabrique toutes les lumières (elles seront enregistrées
        /// avec la scène). Null sans réglages.
        /// </summary>
        public static MapLighting Bake(Scene scene)
        {
            var settings = LoadSettings();
            if (settings == null)
                return null;

            var lighting = new GameObject(RootName).AddComponent<MapLighting>();
            SceneManager.MoveGameObjectToScene(lighting.gameObject, scene);
            Physics.SyncTransforms();
            lighting.BuildAll(settings, MapScan.Run());
            lighting.m_Baked = true;
            return lighting;
        }

        IEnumerator Start()
        {
            // Une image d'attente : la tour, l'eau et les points d'apparition finissent de se mettre en place.
            yield return null;

            m_Settings = LoadSettings();
            if (m_Settings == null)
                yield break;

            var scan = MapScan.Run();
            if (!m_Baked)
                BuildAll(m_Settings, scan);

            // Seulement les groupes visibles : un groupe caché reste éteint.
            foreach (var part in GetComponentsInChildren<MapTorches>())
                part.Bind(m_Settings, scan);
            foreach (var part in GetComponentsInChildren<MapNatureLights>())
                part.Bind(m_Settings, scan);
            foreach (var part in GetComponentsInChildren<MapGameplayLights>())
                part.Bind(m_Settings, scan);
            foreach (var part in GetComponentsInChildren<MapLairVeins>())
                part.Bind(m_Settings);
        }

        void BuildAll(MapLightingSettings settings, MapScan scan)
        {
            EffectBuild.Group(transform, "Torches").gameObject.AddComponent<MapTorches>().Build(settings, scan);
            EffectBuild.Group(transform, "Nature").gameObject.AddComponent<MapNatureLights>().Build(settings, scan);
            EffectBuild.Group(transform, "Jeu").gameObject.AddComponent<MapGameplayLights>().Build(settings);
            EffectBuild.Group(transform, "Antre").gameObject.AddComponent<MapLairVeins>().Build(settings);
        }

        /// <summary>Le tonnerre, un peu après un éclair (<paramref name="strength"/> : force de l'éclair, 0 à 1).</summary>
        public void PlayThunder(float strength)
        {
            m_Settings ??= Resources.Load<MapLightingSettings>(MapLightingSettings.ResourceName);
            if (m_Settings == null || m_Settings.thunderClip == null)
                return;

            var camera = Camera.main;
            var position = camera != null ? camera.transform.position : transform.position;
            Sfx.Play(m_Settings.thunderClip, position, Mathf.Lerp(0.35f, 0.8f, strength), Random.Range(0.32f, 0.42f), 0f);
        }
    }
}
