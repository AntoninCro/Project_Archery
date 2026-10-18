using System.Collections;
using Archery.Defense;
using Archery.Player;
using Archery.UI;
using Archery.Waves;
using Archery.World;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace Archery.FinalBoss
{
    /// <summary>
    /// Enchaîne le boss final (GDD, section 23) côté carte : au début de sa vague, la porte de l'antre s'ouvre ; quand le
    /// joueur passe le seuil, elle se referme, tout devient noir et la scène de l'arène se charge par-dessus la carte.
    /// Le joueur y est déplacé, et l'<see cref="ArenaDirector"/> prend le relais. Après la victoire, le joueur revient
    /// en haut de la tour (mode infini) ou termine la partie sur place.
    /// </summary>
    /// <remarks>
    /// Pendant le combat, la carte est figée : la vague du boss final n'a pas d'ennemis, la tour ne risque rien.
    /// Le soleil, le ciel de la difficulté et les musiques de la carte sont coupés : l'arène a sa propre lumière.
    /// Raccourci de test (éditeur) : <b>N</b> pendant la vague du boss final fait entrer tout de suite dans l'arène.
    /// </remarks>
    [DisallowMultipleComponent]
    public class FinalBossDirector : MonoBehaviour
    {
        [SerializeField]
        LairGate m_Gate;

        [Tooltip("Nom de la scène de l'arène (elle doit être dans File > Build Profiles > Scene List).")]
        [SerializeField]
        string m_ArenaScene = "Arena_Dragon";

        [Tooltip("Point d'arrivée en haut de la tour, pour le retour en mode infini (Tower Top Arrival).")]
        [SerializeField]
        Transform m_ReturnPoint;

        [Tooltip("Durée (s) du fondu au noir quand la porte se referme.")]
        [SerializeField]
        float m_EnterFadeTime = 1.1f;

        [Tooltip("Durée (s) des fondus du retour à la tour.")]
        [SerializeField]
        float m_ReturnFadeTime = 1f;

        [Tooltip("Vague du boss final : la musique et l'ambiance de la carte restent pleines jusqu'à cette distance (m) " +
                 "de la tour, puis baissent à mesure qu'on approche de l'antre, jusqu'au silence à la porte.")]
        [SerializeField]
        float m_FullVolumeRadius = 12f;

        WaveManager m_Waves;
        Scene m_MainScene;
        Scene m_Arena;
        SkyController m_Sky;
        Light m_Sun;
        bool m_Busy;

        public static FinalBossDirector Instance { get; private set; }

        /// <summary>Le joueur est dans l'arène (le bouton « Tour » de la montre est alors caché).</summary>
        public static bool PlayerInArena { get; private set; }

        void Awake()
        {
            Instance = this;

            // Après « Rejouer » (scène rechargée), on repart d'une carte normale.
            PlayerInArena = false;
            AmbienceManager.Silenced = false;
            AmbienceManager.MapVolume = 1f;
            m_MainScene = gameObject.scene;

            if (m_Gate == null)
                m_Gate = FindAnyObjectByType<LairGate>();
            if (m_Gate == null)
                Debug.LogWarning("FinalBossDirector : aucune porte de l'antre (Lair Gate) dans la scène.", this);
        }

        void Start()
        {
            m_Waves = WaveManager.Instance;
            if (m_Waves != null)
                m_Waves.WaveStarted += OnWaveStarted;
            if (m_Gate != null)
                m_Gate.ThresholdCrossed += OnThresholdCrossed;
        }

        void OnDestroy()
        {
            if (m_Waves != null)
                m_Waves.WaveStarted -= OnWaveStarted;
            if (m_Gate != null)
                m_Gate.ThresholdCrossed -= OnThresholdCrossed;
            if (Instance == this)
            {
                Instance = null;
                PlayerInArena = false;
            }
        }

        void Update()
        {
            UpdateMapVolume();
            UpdateDebugKeys();
        }

        // Plus le joueur s'éloigne de la tour vers l'antre, plus la musique et l'ambiance de la carte baissent.
        void UpdateMapVolume()
        {
            var volume = 1f;
            var player = PlayerHealth.Instance;
            if (m_Waves != null && m_Waves.IsFinalBossWave && !PlayerInArena && m_Gate != null && player != null)
            {
                var tower = Tower.Instance != null ? Tower.Instance.transform.position
                          : m_ReturnPoint != null ? m_ReturnPoint.position : transform.position;
                var gate = m_Gate.EntrancePoint;
                var fade = HorizontalDistance(tower, gate) - m_FullVolumeRadius;
                var fromGate = HorizontalDistance(player.BodyPosition, gate);
                volume = fade > 0.1f ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(fromGate / fade)) : 1f;
            }

            AmbienceManager.MapVolume = volume;
        }

        static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        void OnWaveStarted(int wave)
        {
            if (m_Waves.IsFinalBossWave && m_Gate != null)
                m_Gate.Open();
        }

        void OnThresholdCrossed()
        {
            if (!m_Busy && !PlayerInArena && m_Waves != null && m_Waves.IsFinalBossWave)
                StartCoroutine(EnterArena());
        }

        // ---------------------------------------------------------------- entrée dans l'arène

        IEnumerator EnterArena()
        {
            m_Busy = true;
            PlayerInArena = true;
            HeadMessages.Unpin(WaveManager.FinalBossMessageId);
            AmbienceManager.Silenced = true;
            if (m_Gate != null && m_Gate.IsOpen)
                m_Gate.Close();

            var fader = ScreenFader.Instance;
            if (fader != null)
                yield return fader.Fade(1f, m_EnterFadeTime);

            var loading = LoadArenaScene();
            if (loading == null)
            {
                Debug.LogError($"FinalBossDirector : impossible de charger la scène « {m_ArenaScene} ». " +
                               "Ajoute-la dans File > Build Profiles > Scene List.", this);
                PlayerInArena = false;
                AmbienceManager.Silenced = false;
                if (fader != null)
                    fader.FadeTo(0f, 0.5f);
                m_Busy = false;
                yield break;
            }

            while (!loading.isDone)
                yield return null;

            m_Arena = SceneManager.GetSceneByName(m_ArenaScene);
            var arena = ArenaDirector.Instance;
            if (arena == null)
            {
                Debug.LogError("FinalBossDirector : la scène de l'arène n'a pas d'Arena Director.", this);
                m_Busy = false;
                yield break;
            }

            // La lumière de l'arène remplace celle de la carte : soleil et ciel de la difficulté coupés.
            SuspendMapLighting();
            if (m_Arena.IsValid())
                SceneManager.SetActiveScene(m_Arena);

            var spawn = arena.PlayerSpawn;
            var teleport = PlayerTeleport.Instance;
            if (teleport != null && spawn != null)
                yield return teleport.TeleportWhileHidden(spawn.position, spawn.eulerAngles.y);

            arena.BeginIntro();
            m_Busy = false;
        }

        AsyncOperation LoadArenaScene()
        {
#if UNITY_EDITOR
            // Dans l'éditeur, l'arène se charge même si elle manque dans la liste des scènes du build (par son chemin).
            if (!Application.CanStreamedLevelBeLoaded(m_ArenaScene))
            {
                foreach (var guid in UnityEditor.AssetDatabase.FindAssets($"{m_ArenaScene} t:Scene"))
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    if (System.IO.Path.GetFileNameWithoutExtension(path) == m_ArenaScene)
                    {
                        Debug.LogWarning($"FinalBossDirector : « {m_ArenaScene} » n'est pas dans File > Build Profiles > Scene List. " +
                                         "Elle se charge dans l'éditeur, mais il faut l'ajouter pour le build.", this);
                        return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                            path, new LoadSceneParameters(LoadSceneMode.Additive));
                    }
                }
            }
#endif
            return SceneManager.LoadSceneAsync(m_ArenaScene, LoadSceneMode.Additive);
        }

        void SuspendMapLighting()
        {
            m_Sky = FindAnyObjectByType<SkyController>();
            if (m_Sky != null)
                m_Sky.enabled = false;

            m_Sun = RenderSettings.sun;
            if (m_Sun != null)
                m_Sun.enabled = false;
        }

        void RestoreMapLighting()
        {
            if (m_Sun != null)
                m_Sun.enabled = true;
            if (m_Sky != null)
                m_Sky.enabled = true;
        }

        // ---------------------------------------------------------------- victoire

        /// <summary>Le joueur a choisi de continuer : retour en haut de la tour, boutique, puis mode infini au gong.</summary>
        public void ContinueEndless()
        {
            if (!m_Busy)
                StartCoroutine(ReturnToTower());
        }

        /// <summary>Le joueur a choisi de terminer la partie : écran de victoire, sur place, dans l'arène.</summary>
        public void FinishGame()
        {
            if (m_Waves != null)
                m_Waves.CompleteFinalBoss(finishGame: true);
        }

        IEnumerator ReturnToTower()
        {
            m_Busy = true;
            var fader = ScreenFader.Instance;
            if (fader != null)
                yield return fader.Fade(1f, m_ReturnFadeTime);

            if (m_MainScene.IsValid())
                SceneManager.SetActiveScene(m_MainScene);
            RestoreMapLighting();

            var teleport = PlayerTeleport.Instance;
            if (teleport != null && m_ReturnPoint != null)
                yield return teleport.TeleportWhileHidden(m_ReturnPoint.position, m_ReturnPoint.eulerAngles.y);

            if (m_Arena.IsValid() && m_Arena.isLoaded)
            {
                EditorSelection.ReleaseScene(m_Arena);
                var unloading = SceneManager.UnloadSceneAsync(m_Arena);
                while (unloading != null && !unloading.isDone)
                    yield return null;
            }

            PlayerInArena = false;
            AmbienceManager.Silenced = false;
            if (m_Waves != null)
                m_Waves.CompleteFinalBoss(finishGame: false);

            if (fader != null)
                fader.FadeTo(0f, m_ReturnFadeTime);
            m_Busy = false;
        }

        // ---------------------------------------------------------------- tests

        void UpdateDebugKeys()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // N pendant la vague du boss final : entre tout de suite dans l'arène, sans marcher jusqu'à la porte.
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.nKey.wasPressedThisFrame)
                return;

            if (!m_Busy && !PlayerInArena && m_Waves != null && m_Waves.IsFinalBossWave)
                StartCoroutine(EnterArena());
#endif
        }
    }
}
