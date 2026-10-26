using Archery.Player;
using Archery.Save;
using Archery.UI;
using Archery.Waves;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace Archery.Lore
{
    /// <summary>
    /// « Je t'attendais » (Lore.md, canon du 25 octobre 2026 : une copie de toi joue pendant ton absence) : quand le joueur
    /// remet le casque après l'avoir retiré un moment, la montre l'affiche à la place de la ligne de la vague.
    /// </summary>
    /// <remarks>
    /// Il se crée tout seul au lancement : rien à poser dans la scène. Le casque compte comme retiré quand son capteur de
    /// présence ne voit plus personne, ou, hors de l'éditeur, quand le jeu perd la main (avec Quest Link, retirer le casque
    /// met le jeu en arrière-plan ; dans l'éditeur, cliquer dans une autre fenêtre le ferait aussi). Une fois par partie au
    /// plus, après quelques parties finies, et jamais sans casque (jeu au clavier). Raccourci de test : <b>Y</b>.
    /// </remarks>
    [DisallowMultipleComponent]
    public class HeadsetAbsence : MonoBehaviour
    {
        // Une absence plus courte ne compte pas : le casque remonté sur le front, un réglage de sangle.
        const float k_MinAbsence = 8f;

        // Parties finies avant que la montre parle (25 octobre, provisoire).
        const int k_After = 2;

        const string k_Message = "<i>Je t'attendais.</i>";
        const float k_Duration = 6f;

        // Hors de la montre (jeu au clavier, pour le raccourci de test) : un message devant les yeux.
        static readonly Color k_Color = new Color(0.85f, 0.85f, 0.9f);

        // Moment (temps réel) où le casque a été retiré ; négatif s'il est porté.
        float m_LeftAt = -1f;
        bool m_SaidThisGame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            var go = new GameObject("[Headset Absence]");
            DontDestroyOnLoad(go);
            go.AddComponent<HeadsetAbsence>();
        }

        void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;

        void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        // Une nouvelle partie commence : la montre pourra reparler.
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single)
                m_SaidThisGame = false;
        }

        // Avec Quest Link, le jeu peut ne plus tourner du tout pendant que le casque est posé : on note le départ ici aussi.
        void OnApplicationFocus(bool focused)
        {
            if (!focused && !Application.isEditor)
                Leave();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
                Leave();
        }

        void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.yKey.wasPressedThisFrame && !DesktopHand.IsTypingText)
                Say();
#endif

            // Jeu au clavier : pas de casque à retirer (quitter la fenêtre ne compte pas).
            var desktop = DesktopControls.Instance;
            if (desktop != null && desktop.IsActive)
            {
                m_LeftAt = -1f;
                return;
            }

            if (!IsWorn())
            {
                Leave();
                return;
            }

            if (m_LeftAt < 0f)
                return;

            var away = Time.realtimeSinceStartup - m_LeftAt;
            m_LeftAt = -1f;
            if (away >= k_MinAbsence && !m_SaidThisGame && GameMemory.GamesFinished >= k_After && InGame())
                Say();
        }

        void Leave()
        {
            if (m_LeftAt < 0f)
                m_LeftAt = Time.realtimeSinceStartup;
        }

        void Say()
        {
            m_SaidThisGame = true;
            HudDisplay.Whisper(k_Message, k_Duration);

            var desktop = DesktopControls.Instance;
            if (desktop != null && desktop.IsActive)
                HeadMessages.Show(k_Message, k_Color, k_Duration);
        }

        // Sans casque branché, ou sans capteur de présence, le casque compte comme porté (comme PlayerTeleport).
        static bool IsWorn()
        {
            var device = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (!device.isValid)
                return true;

            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.userPresence, out var present) && !present)
                return false;

            return Application.isEditor || Application.isFocused;
        }

        // Pendant une partie, menu compris, mais pas sur l'écran de fin.
        static bool InGame()
        {
            var waves = WaveManager.Instance;
            return waves != null && waves.Phase != WavePhase.GameOver;
        }
    }
}
