using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Archery.Core
{
    /// <summary>
    /// Le rendu fovéal dans le casque seul (build Android ; audit de performance du 25 octobre,
    /// <c>Docs/Audit_Performance_Quest.md</c>) : les bords de l'image, que l'œil voit flous à travers les lentilles, sont
    /// dessinés en moins de détails. Rien sur PC : avec Quest Link, c'est le PC qui dessine.
    /// </summary>
    /// <remarks>
    /// Il se crée tout seul au lancement : rien à poser dans la scène. Il lui faut la fonction « Foveated Rendering » de
    /// l'OpenXR pour Android, avec l'API de l'URP (<c>Editor/QuestQualityBaker</c>).
    /// </remarks>
    [DisallowMultipleComponent]
    public class QuestFoveation : MonoBehaviour
    {
        // De 0 (aucun) à 1 (le plus fort). À moitié : le gain est déjà net, et les bords de l'image restent propres.
        const float k_Level = 0.5f;

        // L'affichage du casque peut démarrer un peu après la première scène : on le cherche pendant 10 s au plus.
        const float k_Patience = 10f;

        static readonly List<XRDisplaySubsystem> s_Displays = new List<XRDisplaySubsystem>();

        float m_GiveUpAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (Application.platform != RuntimePlatform.Android)
                return;

            var go = new GameObject("[Quest Foveation]");
            DontDestroyOnLoad(go);
            go.AddComponent<QuestFoveation>();
        }

        void Start() => m_GiveUpAt = Time.realtimeSinceStartup + k_Patience;

        void Update()
        {
            SubsystemManager.GetSubsystems(s_Displays);
            foreach (var display in s_Displays)
            {
                if (!display.running)
                    continue;

                display.foveatedRenderingFlags = XRDisplaySubsystem.FoveatedRenderingFlags.None;
                display.foveatedRenderingLevel = k_Level;
                Debug.Log($"QuestFoveation : rendu fovéal au niveau {k_Level}.");
                Destroy(gameObject);
                return;
            }

            if (Time.realtimeSinceStartup > m_GiveUpAt)
            {
                Debug.LogWarning("QuestFoveation : pas d'affichage de casque, pas de rendu fovéal.");
                Destroy(gameObject);
            }
        }
    }
}
