using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace Archery.Core
{
    /// <summary>
    /// Mouchard des images lentes : chaque image de plus de 80 ms est notée dans le journal (Editor.log, ou Player.log
    /// du jeu construit), avec la mémoire graphique utilisée et les scènes chargées. Sert à comprendre les blocages
    /// du casque (l'image se fige dans le casque alors que le jeu continue) : la carte graphique saturait-elle juste
    /// avant ?
    /// </summary>
    /// <remarks>Créé tout seul au lancement ; au plus un message toutes les 2 s, et un bilan toutes les 5 minutes.</remarks>
    public class FrameWatchdog : MonoBehaviour
    {
        const float k_SlowFrame = 0.08f;
        const float k_MinInterval = 2f;
        const float k_SummaryInterval = 300f;
        // Les premières images (lancement de Play, démarrage du casque) durent souvent plusieurs secondes : normal.
        const int k_WarmupFrames = 30;

        float m_LastLog = float.NegativeInfinity;
        float m_NextSummary;
        float m_Worst;
        int m_SlowFrames;
        int m_Frames;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            var go = new GameObject("[Mouchard des images lentes]");
            go.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(go);
            go.AddComponent<FrameWatchdog>();
        }

        void Start() => m_NextSummary = Time.unscaledTime + k_SummaryInterval;

        void Update()
        {
            if (m_Frames++ < k_WarmupFrames)
                return;

            var frame = Time.unscaledDeltaTime;
            if (frame > k_SlowFrame)
            {
                m_SlowFrames++;
                m_Worst = Mathf.Max(m_Worst, frame);
                if (Time.unscaledTime - m_LastLog >= k_MinInterval)
                {
                    m_LastLog = Time.unscaledTime;
                    Debug.LogWarning($"Image lente : {frame * 1000f:0} ms. {Memory()}, scènes : {Scenes()}.");
                }
            }

            if (Time.unscaledTime >= m_NextSummary)
            {
                m_NextSummary = Time.unscaledTime + k_SummaryInterval;
                Debug.Log($"Bilan des images (5 min) : {m_SlowFrames} images de plus de {k_SlowFrame * 1000f:0} ms, la pire {m_Worst * 1000f:0} ms. " +
                          $"{Memory()}, scènes : {Scenes()}.");
                m_SlowFrames = 0;
                m_Worst = 0f;
            }
        }

        static string Memory()
        {
            var graphics = Profiler.GetAllocatedMemoryForGraphicsDriver();
            var total = Profiler.GetTotalAllocatedMemoryLong();
            return graphics > 0
                ? $"mémoire graphique {graphics / 1048576f:0} Mo, mémoire {total / 1048576f:0} Mo"
                : $"mémoire {total / 1048576f:0} Mo";
        }

        static string Scenes()
        {
            var names = new string[SceneManager.sceneCount];
            for (var i = 0; i < names.Length; i++)
                names[i] = SceneManager.GetSceneAt(i).name;
            return string.Join(", ", names);
        }
    }
}
