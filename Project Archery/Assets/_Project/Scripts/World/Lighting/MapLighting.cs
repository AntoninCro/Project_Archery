using System.Collections;
using Archery.Core;
using UnityEngine;

namespace Archery.World
{
    /// <summary>
    /// Toutes les lumières ajoutées à la carte, créées au lancement par le <see cref="SkyController"/> sans rien poser
    /// dans la scène : torches des voies et de la forêt (<see cref="MapTorches"/>), lucioles, plantes et cristaux
    /// lumineux, pollen, rayons du soleil, brume des cascades (<see cref="MapNatureLights"/>), phare de la tour, runes
    /// des emplacements, âmes des ennemis, lueur du joueur, remous de la rivière (<see cref="MapGameplayLights"/>) et
    /// veines de l'antre (<see cref="MapLairVeins"/>).
    /// </summary>
    /// <remarks>
    /// Les réglages sont dans l'asset « Resources/MapLighting » (<see cref="MapLightingSettings"/>). La force de tout
    /// suit l'heure du ciel de la difficulté (<see cref="SkyController.Lamps"/>) : discret le jour, vif la nuit.
    /// </remarks>
    public class MapLighting : MonoBehaviour
    {
        MapLightingSettings m_Settings;

        public static MapLighting Create()
        {
            var settings = Resources.Load<MapLightingSettings>(MapLightingSettings.ResourceName);
            if (settings == null)
            {
                Debug.LogWarning("MapLighting : réglages « Resources/MapLighting » introuvables, pas de lumières sur la carte.");
                return null;
            }

            var lighting = new GameObject("Lumières de la carte").AddComponent<MapLighting>();
            lighting.m_Settings = settings;
            return lighting;
        }

        IEnumerator Start()
        {
            // Une image d'attente : la tour, l'eau et les points d'apparition finissent de se mettre en place.
            yield return null;

            var scan = MapScan.Run();
            gameObject.AddComponent<MapTorches>().Init(m_Settings, scan);
            gameObject.AddComponent<MapNatureLights>().Init(m_Settings, scan);
            gameObject.AddComponent<MapGameplayLights>().Init(m_Settings, scan);
            gameObject.AddComponent<MapLairVeins>().Init(m_Settings);
        }

        /// <summary>Le tonnerre, un peu après un éclair (<paramref name="strength"/> : force de l'éclair, 0 à 1).</summary>
        public void PlayThunder(float strength)
        {
            if (m_Settings == null || m_Settings.thunderClip == null)
                return;

            var camera = Camera.main;
            var position = camera != null ? camera.transform.position : transform.position;
            Sfx.Play(m_Settings.thunderClip, position, Mathf.Lerp(0.35f, 0.8f, strength), Random.Range(0.32f, 0.42f), 0f);
        }
    }
}
