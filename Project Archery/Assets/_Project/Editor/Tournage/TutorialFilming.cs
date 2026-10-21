#if ARCHERY_RECORDER
using System.Collections.Generic;
using System.IO;
using Archery.Menus;
using Archery.UI;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le tournage des vidéos du tutoriel (menu <c>Archery > Tutoriel > Tournage des vidéos</c>) : en mode Play, dans le
    /// casque, les deux joysticks enfoncés en même temps (ou F8) démarrent l'enregistrement de la vidéo choisie, et
    /// l'arrêtent. Unity Recorder filme la <see cref="TutorialFilmCamera"/> (la tête lissée, en 16:9) et écrit
    /// <c>Resources/Tutoriel/&lt;nom&gt;.mp4</c> (H.264, 1280 × 720, sans le son) : la fenêtre du tutoriel la trouve toute
    /// seule. Puis la vidéo suivante de la liste est choisie.
    /// </summary>
    /// <remarks>
    /// Recorder en fréquence « variable » : chaque image garde son heure réelle (la vidéo a la bonne vitesse), et sa
    /// limite (240 images par seconde) est bien au-dessus de celle du casque : il ne freine jamais le jeu. En fréquence
    /// « constante », il ralentirait tout le jeu, et la VR avec. Il ne garde qu'une image sur N (environ 30 par seconde),
    /// celles que la caméra vient de filmer, à 12 Mbit/s : la première vidéo gardait les 72 images par seconde du casque,
    /// dont deux tiers de copies, et devenait très pixelisée dès qu'on tournait la tête.
    /// </remarks>
    public class TutorialFilming : EditorWindow
    {
        const string k_Folder = "Assets/_Project/Resources/Tutoriel/";
        const string k_ContentPath = "Assets/_Project/Resources/TutorialContent.asset";
        const string k_ActivePref = "Archery.Tournage.Actif";
        const string k_ClipPref = "Archery.Tournage.Video";
        const string k_FlipPref = "Archery.Tournage.Retourner";

        // Images par seconde visées dans la vidéo, et son débit (Mbit/s) : de quoi garder une image nette quand tout bouge.
        const float k_VideoFrameRate = 30f;
        const float k_BitRate = 12f;

        const string k_Shortcuts =
            "Raccourcis de test (clavier, en mode Play) :\n" +
            "N : lance la vague ; pendant le chrono, fait venir le boss ; pendant le boss, le tue\n" +
            "V : un gisement de cristaux 3 m devant toi   ·   B : +50 matériaux   ·   H : finit la construction\n" +
            "C : un coffre   ·   1 à 4 : la difficulté   ·   K : le joueur meurt\n" +
            "5 à 9 : équipe un arc   ·   U : débloque tous les arcs   ·   J : reverrouille les arcs";

        struct Clip
        {
            public string Name;
            public string Title;
        }

        static RecorderController s_Controller;
        static string s_RecordingClip;
        static double s_StartTime;
        static readonly List<Object> s_Leftovers = new List<Object>();

        Vector2 m_Scroll;

        [MenuItem("Archery/Tutoriel/Tournage des vidéos")]
        static void Open()
        {
            Active = true;
            GetWindow<TutorialFilming>("Tournage du tutoriel");
        }

        [InitializeOnLoadMethod]
        static void Hook()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        /// <summary>
        /// Le tournage est prêt à chaque passage en mode Play : seulement tant que la fenêtre est ouverte (et la case cochée).
        /// Fenêtre fermée, les deux joysticks enfoncés en jouant n'enregistrent rien : pas de vidéo écrasée par mégarde.
        /// </summary>
        static bool Active
        {
            get => EditorPrefs.GetBool(k_ActivePref, false) && HasOpenInstances<TutorialFilming>();
            set => EditorPrefs.SetBool(k_ActivePref, value);
        }

        static bool Checked
        {
            get => EditorPrefs.GetBool(k_ActivePref, false);
            set => EditorPrefs.SetBool(k_ActivePref, value);
        }

        static int ClipIndex
        {
            get => EditorPrefs.GetInt(k_ClipPref, 0);
            set => EditorPrefs.SetInt(k_ClipPref, value);
        }

        static bool Flip
        {
            get => EditorPrefs.GetBool(k_FlipPref, false);
            set => EditorPrefs.SetBool(k_FlipPref, value);
        }

        static bool Recording => s_Controller != null;

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    if (!Active)
                        return;
                    TutorialFilmCamera.Create();
                    TutorialFilmCamera.ToggleRequested -= Toggle;
                    TutorialFilmCamera.ToggleRequested += Toggle;
                    break;

                case PlayModeStateChange.ExitingPlayMode:
                    Stop();
                    TutorialFilmCamera.ToggleRequested -= Toggle;
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    foreach (var leftover in s_Leftovers)
                    {
                        if (leftover != null)
                            DestroyImmediate(leftover);
                    }

                    s_Leftovers.Clear();
                    AssetDatabase.Refresh();
                    break;
            }
        }

        // Les vidéos attendues, dans l'ordre du tutoriel : Tir_1, Tir_2, Deplacement_1…
        static List<Clip> Clips()
        {
            var clips = new List<Clip>();
            var content = AssetDatabase.LoadAssetAtPath<TutorialContent>(k_ContentPath);
            if (content == null)
                return clips;

            foreach (var section in content.sections)
            {
                if (string.IsNullOrEmpty(section.videoPrefix))
                    continue;
                for (var i = 0; i < section.windows.Count; i++)
                    clips.Add(new Clip { Name = section.videoPrefix + "_" + (i + 1), Title = section.windows[i].title });
            }

            return clips;
        }

        static void Toggle()
        {
            if (Recording)
                Stop();
            else
                Begin();
        }

        static void Begin()
        {
            var camera = TutorialFilmCamera.Instance;
            var clips = Clips();
            if (Recording || camera == null || clips.Count == 0)
                return;

            var clip = clips[Mathf.Clamp(ClipIndex, 0, clips.Count - 1)];
            Directory.CreateDirectory(k_Folder);

            // Une image sur N du casque (72 à 120 par seconde) : environ 30 par seconde dans la vidéo.
            var step = Mathf.Max(1, Mathf.RoundToInt(1f / Mathf.Max(0.001f, Time.smoothDeltaTime) / k_VideoFrameRate));

            var settings = CreateInstance<RecorderControllerSettings>();
            var movie = CreateInstance<MovieRecorderSettings>();
            movie.name = "Tutoriel " + clip.Name;
            movie.Enabled = true;
            movie.EncoderSettings = new CoreEncoderSettings
            {
                Codec = CoreEncoderSettings.OutputCodec.MP4,
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.Custom,
                TargetBitRate = k_BitRate,
                EncodingProfile = CoreEncoderSettings.H264EncodingProfile.High,
            };
            movie.CaptureAudio = false;
            movie.ImageInputSettings = new RenderTextureInputSettings { RenderTexture = camera.Texture, FlipFinalOutput = Flip };
            movie.OutputFile = Application.dataPath + "/_Project/Resources/Tutoriel/" + clip.Name;

            // « Capturer une image sur N » n'a pas d'accès public dans Recorder : réglé par son champ enregistré.
            var serialized = new SerializedObject(movie);
            var everyNth = serialized.FindProperty("captureEveryNthFrame");
            if (everyNth != null)
            {
                everyNth.intValue = step;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                step = 1;
            }

            settings.AddRecorderSettings(movie);
            settings.SetRecordModeToManual();
            settings.FrameRatePlayback = FrameRatePlayback.Variable;
            settings.FrameRate = 240f;
            settings.CapFrameRate = false;
            s_Leftovers.Add(settings);
            s_Leftovers.Add(movie);

            var controller = new RecorderController(settings);
            controller.PrepareRecording();
            camera.FrameStep = step;
            camera.Filming = true;
            if (!controller.StartRecording())
            {
                camera.Filming = false;
                Debug.LogError($"Tournage : Unity Recorder n'a pas pu démarrer l'enregistrement de {clip.Name} (voir la console).");
                return;
            }

            s_Controller = controller;
            s_RecordingClip = clip.Name;
            s_StartTime = EditorApplication.timeSinceStartup;
            camera.Signal(true);
            Debug.Log($"Tournage : enregistrement de {clip.Name} ({clip.Title}), une image sur {step} du casque " +
                      $"(environ {1f / Mathf.Max(0.001f, Time.smoothDeltaTime) / step:0} par seconde), {k_BitRate:0} Mbit/s.");
        }

        static void Stop()
        {
            if (!Recording)
                return;

            s_Controller.StopRecording();
            s_Controller = null;
            var seconds = EditorApplication.timeSinceStartup - s_StartTime;

            var camera = TutorialFilmCamera.Instance;
            if (camera != null)
            {
                camera.Filming = false;
                camera.Signal(false);
            }

            // La suivante de la liste.
            var clips = Clips();
            var index = clips.FindIndex(c => c.Name == s_RecordingClip);
            if (index >= 0)
                ClipIndex = Mathf.Min(index + 1, clips.Count - 1);
            var next = index >= 0 && index + 1 < clips.Count ? clips[index + 1].Name : "aucune (toutes faites)";

            var recorded = s_RecordingClip;
            HeadMessages.Show($"Vidéo enregistrée : {recorded} ({seconds:0.0} s)\nSuivante : {next}", Color.white, 3f);
            Debug.Log($"Tournage : {recorded}.mp4 enregistrée ({seconds:0.0} s) dans {k_Folder}. Suivante : {next}.");

            // Importée tout de suite : on peut la voir dans le tutoriel du jeu sans quitter le mode Play.
            EditorApplication.delayCall += () => AssetDatabase.ImportAsset(k_Folder + recorded + ".mp4");
        }

        void OnInspectorUpdate()
        {
            if (EditorApplication.isPlaying)
                Repaint();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Tournage des vidéos du tutoriel", EditorStyles.boldLabel);
            Checked = EditorGUILayout.ToggleLeft("Tournage prêt à chaque passage en mode Play (tant que cette fenêtre est ouverte)", Checked);
            EditorGUILayout.HelpBox(
                "En mode Play, dans le casque : enfonce les deux joysticks en même temps (ou F8) pour démarrer " +
                "l'enregistrement de la vidéo choisie ci-dessous, pareil pour l'arrêter (une vibration au début, une " +
                "longue à la fin). La suivante est choisie toute seule. Refaire une vidéo : choisis-la et recommence, " +
                "l'ancienne est remplacée.\nConseils : 5 à 10 s, un seul geste, la tête lente, finir dans la pose du début.",
                MessageType.Info);

            if (EditorApplication.isPlaying && Checked && TutorialFilmCamera.Instance == null)
                EditorGUILayout.HelpBox("Le tournage a été activé pendant le mode Play : relance le mode Play.", MessageType.Warning);

            // L'état, et les boutons (pour filmer sans casque).
            using (new EditorGUILayout.HorizontalScope())
            {
                var status = Recording
                    ? $"● Enregistrement : {s_RecordingClip}   {EditorApplication.timeSinceStartup - s_StartTime:0.0} s"
                    : "Prêt";
                EditorGUILayout.LabelField(status, Recording ? EditorStyles.boldLabel : EditorStyles.label);
                using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || TutorialFilmCamera.Instance == null))
                {
                    if (GUILayout.Button(Recording ? "Arrêter" : "Démarrer", GUILayout.Width(100f)))
                        Toggle();
                }
            }

            Flip = EditorGUILayout.ToggleLeft("Retourner l'image (si les vidéos sortent à l'envers)", Flip);

            // Les vidéos : celle qui sera filmée, et celles déjà faites.
            EditorGUILayout.Space();
            var clips = Clips();
            if (clips.Count == 0)
                EditorGUILayout.HelpBox($"Pas de contenu du tutoriel ({k_ContentPath}).", MessageType.Warning);

            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll, GUILayout.MaxHeight(300f));
            var selected = Mathf.Clamp(ClipIndex, 0, Mathf.Max(0, clips.Count - 1));
            for (var i = 0; i < clips.Count; i++)
            {
                var clip = clips[i];
                var done = File.Exists(k_Folder + clip.Name + ".mp4");
                using (new EditorGUILayout.HorizontalScope())
                {
                    var label = $"{(done ? "✔" : "  ")}  {clip.Name}   {clip.Title}";
                    if (GUILayout.Toggle(i == selected, label, EditorStyles.radioButton) && i != selected && !Recording)
                        ClipIndex = i;
                }
            }

            EditorGUILayout.EndScrollView();

            // Ce que la caméra filme, pendant l'enregistrement.
            var camera = TutorialFilmCamera.Instance;
            if (Recording && camera != null && camera.Texture != null)
            {
                var width = position.width - 20f;
                var rect = GUILayoutUtility.GetRect(width, width * TutorialFilmCamera.Height / TutorialFilmCamera.Width);
                EditorGUI.DrawPreviewTexture(rect, camera.Texture);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(k_Shortcuts, MessageType.None);
        }
    }
}
#endif
