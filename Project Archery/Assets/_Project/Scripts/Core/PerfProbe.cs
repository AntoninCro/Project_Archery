using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Archery.Bows;
using Archery.Enemies;
using Archery.Waves;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace Archery.Core
{
    /// <summary>
    /// La mesure de performance des builds de développement (25 octobre : « ça rame un peu » sur le Quest seul, vers la
    /// vague 10, avec les Akimbow et beaucoup d'améliorations, sans rien dans le journal). Toutes les 10 s, une ligne : les
    /// images par seconde, la part d'images trop lentes, la pire, le temps du thread principal, et ce qui charge l'écran à
    /// ce moment (particules et les effets qui en ont le plus, lumières, traînées et éclairs, ennemis, flèches).
    /// </summary>
    /// <remarks>
    /// <para>Complète <see cref="FrameWatchdog"/>, qui ne note que les gels (plus de 80 ms) : dans le casque, une image de
    /// 20 ms fait déjà ramer (à 72 images par seconde, une image a 13,9 ms).</para>
    /// <para>La ligne va dans le journal (sur le Quest : <c>adb logcat -s Unity</c>) et dans un fichier CSV par lancement,
    /// dans le dossier <c>Mesures</c> des sauvegardes (sur le Quest : <c>adb pull
    /// /sdcard/Android/data/com.DefaultCompany.VRTemplate/files/Mesures</c>) ; les 10 derniers sont gardés.</para>
    /// <para>Créée toute seule dans les builds de développement seulement : pas dans l'éditeur, pour ne pas remplir la
    /// console. Les objets sont comptés une fois par mesure ; l'image qui suit n'est pas comptée, pour ne pas mesurer la
    /// mesure.</para>
    /// </remarks>
    public class PerfProbe : MonoBehaviour
    {
        const float k_Interval = 10f;
        // Une image est trop lente au-delà de 1,25 fois la durée d'une image du casque (17 ms à 72 Hz).
        const float k_SlowFactor = 1.25f;
        // Avant que l'affichage du casque démarre, ou sans casque.
        const float k_DefaultRefreshRate = 72f;
        const int k_TopEffects = 3;
        const int k_KeptFiles = 10;
        const string k_Folder = "Mesures";
        const string k_Header = "heure;scènes;vague;images/s;Hz du casque;images lentes (%);pire image (ms);" +
                                "thread principal (ms);particules;effets;lumières;traînées et éclairs;ennemis;" +
                                "flèches en vol;flèches plantées;mémoire (Mo);effets les plus chargés";

        static readonly List<XRDisplaySubsystem> s_Displays = new List<XRDisplaySubsystem>();

        readonly Dictionary<string, int> m_ParticlesByEffect = new Dictionary<string, int>();

        ProfilerRecorder m_MainThread;
        StreamWriter m_File;
        float m_RefreshRate = k_DefaultRefreshRate;
        float m_WindowStart;
        float m_Worst;
        int m_Frames;
        int m_SlowFrames;
        double m_MainThreadSum;
        int m_MainThreadSamples;
        bool m_SkipNext;

#if DEVELOPMENT_BUILD && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            var go = new GameObject("[Mesure de performance]");
            go.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(go);
            go.AddComponent<PerfProbe>();
        }
#endif

        void OnEnable() => m_MainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");

        void OnDisable()
        {
            m_MainThread.Dispose();
            m_File?.Dispose();
            m_File = null;
        }

        void Start()
        {
            m_WindowStart = Time.unscaledTime;
            var path = OpenFile();
            var quality = QualitySettings.names[QualitySettings.GetQualityLevel()];
            Log($"Mesure de performance : une ligne toutes les {k_Interval:0} s. {SystemInfo.deviceModel}, qualité « {quality} »" +
                (path != null ? $", fichier {path}." : ", sans fichier."));
        }

        void Update()
        {
            var frame = Time.unscaledDeltaTime;
            if (m_SkipNext)
            {
                m_SkipNext = false;
            }
            else
            {
                m_Frames++;
                m_Worst = Mathf.Max(m_Worst, frame);
                if (frame > k_SlowFactor / m_RefreshRate)
                    m_SlowFrames++;
                if (m_MainThread.Valid && m_MainThread.Count > 0)
                {
                    m_MainThreadSum += m_MainThread.LastValue;
                    m_MainThreadSamples++;
                }
            }

            var elapsed = Time.unscaledTime - m_WindowStart;
            if (elapsed < k_Interval)
                return;

            Report(elapsed);
            m_RefreshRate = RefreshRate();
            m_WindowStart = Time.unscaledTime;
            m_Worst = 0f;
            m_Frames = 0;
            m_SlowFrames = 0;
            m_MainThreadSum = 0;
            m_MainThreadSamples = 0;
            m_SkipNext = true;
        }

        void Report(float elapsed)
        {
            var fps = m_Frames / elapsed;
            var slowShare = m_Frames > 0 ? 100f * m_SlowFrames / m_Frames : 0f;
            var worst = m_Worst * 1000f;
            // Le compteur du profileur donne des nanosecondes.
            var mainThread = m_MainThreadSamples > 0 ? (float)(m_MainThreadSum / m_MainThreadSamples / 1e6) : 0f;

            var particles = 0;
            var effects = 0;
            m_ParticlesByEffect.Clear();
            foreach (var system in FindObjectsByType<ParticleSystem>())
            {
                var count = system.particleCount;
                if (count == 0)
                    continue;
                particles += count;
                effects++;
                // L'effet : le plus haut des systèmes de particules emboîtés (pas la racine, qui peut être une réserve
                // d'effets ou un ennemi).
                var effect = system.transform;
                while (effect.parent != null && effect.parent.GetComponent<ParticleSystem>() != null)
                    effect = effect.parent;
                var name = effect.name.Replace("(Clone)", "").Trim();
                m_ParticlesByEffect.TryGetValue(name, out var sum);
                m_ParticlesByEffect[name] = sum + count;
            }
            var top = string.Join(", ", m_ParticlesByEffect.OrderByDescending(e => e.Value).Take(k_TopEffects)
                .Select(e => $"{e.Key} {e.Value}"));

            var lights = 0;
            foreach (var light in FindObjectsByType<Light>())
            {
                if (light.enabled)
                    lights++;
            }
            var lines = 0;
            foreach (var trail in FindObjectsByType<TrailRenderer>())
            {
                if (trail.enabled && trail.isVisible)
                    lines++;
            }
            foreach (var line in FindObjectsByType<LineRenderer>())
            {
                if (line.enabled && line.isVisible)
                    lines++;
            }

            var enemies = Enemy.Alive.Count;
            var pool = ArrowPool.Instance;
            var stuck = pool != null ? pool.StuckCount : 0;
            var flying = pool != null ? pool.InUse - stuck : 0;
            var wave = WaveManager.Instance != null ? WaveManager.Instance.WaveNumber : 0;
            var memory = Profiler.GetTotalAllocatedMemoryLong() / 1048576f;
            var scenes = Scenes();

            Log($"Mesure : {fps:0} images/s sur {m_RefreshRate:0} Hz, {slowShare:0} % d'images lentes, pire {worst:0} ms, " +
                $"thread principal {mainThread:0.0} ms ; vague {wave} ({scenes}) ; {particles} particules dans {effects} effets" +
                (top.Length > 0 ? $" (dont {top})" : "") + $" ; {lights} lumières, {lines} traînées et éclairs ; {enemies} ennemis, " +
                $"{flying} flèches en vol, {stuck} plantées ; mémoire {memory:0} Mo.");

            if (m_File == null)
                return;
            try
            {
                m_File.WriteLine(string.Join(";", DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture), scenes,
                    wave, Number(fps), Number(m_RefreshRate), Number(slowShare), Number(worst), Number(mainThread), particles,
                    effects, lights, lines, enemies, flying, stuck, Number(memory), top));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Mesure de performance : écriture impossible ({e.Message}), le fichier est abandonné.");
                m_File.Dispose();
                m_File = null;
            }
        }

        string OpenFile()
        {
            try
            {
                var folder = Path.Combine(Application.persistentDataPath, k_Folder);
                Directory.CreateDirectory(folder);
                var old = Directory.GetFiles(folder, "*.csv");
                Array.Sort(old, StringComparer.Ordinal);
                for (var i = 0; i <= old.Length - k_KeptFiles; i++)
                    File.Delete(old[i]);

                var path = Path.Combine(folder, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture) + ".csv");
                // Point-virgule et virgule décimale, avec l'indicateur UTF-8 : Excel en français l'ouvre tel quel.
                m_File = new StreamWriter(path, false, new UTF8Encoding(true)) { AutoFlush = true };
                m_File.WriteLine(k_Header);
                return path;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Mesure de performance : pas de fichier ({e.Message}).");
                m_File = null;
                return null;
            }
        }

        static float RefreshRate()
        {
            SubsystemManager.GetSubsystems(s_Displays);
            foreach (var display in s_Displays)
            {
                if (display.running && display.TryGetDisplayRefreshRate(out var rate) && rate > 0f)
                    return rate;
            }
            return k_DefaultRefreshRate;
        }

        static string Scenes()
        {
            var names = new string[SceneManager.sceneCount];
            for (var i = 0; i < names.Length; i++)
                names[i] = SceneManager.GetSceneAt(i).name;
            return string.Join(" + ", names);
        }

        static string Number(float value) => value.ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',');

        // Sans la pile d'appels, que le journal du Quest répéterait à chaque ligne.
        static void Log(string message) => Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}", message);
    }
}
