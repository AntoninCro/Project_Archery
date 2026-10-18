using System.Collections.Generic;
using System.Linq;
using Archery.Portal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Archery.EditorTools
{
    /// <summary>
    /// Met en place le portail des étoiles (GDD, section 23) sur la carte : à l'ouverture d'une scène qui a l'anneau de
    /// pierre (<c>statue_ring</c>) et ses obélisques (<c>statue_obelisk</c>) mais pas encore de <see cref="StarPortal"/>,
    /// un objet « Portail des étoiles » est créé et relié à eux, avec son matériau et ses sons ; la scène est marquée
    /// comme modifiée, il suffit de l'enregistrer. La scène de la dimension est aussi ajoutée à la liste des scènes du build.
    /// </summary>
    [InitializeOnLoad]
    static class StarPortalBaker
    {
        const string k_DimensionScene = "Assets/_Project/Scenes/Dimension_Etoiles.unity";
        const string k_GlowMaterial = "Assets/_Project/Materials/Effects.mat";
        const string k_Sounds = "Assets/kenney_interfaceSounds/Audio/";

        static StarPortalBaker()
        {
            EditorSceneManager.sceneOpened += (scene, mode) => Schedule();
            Schedule();
        }

        static void Schedule()
        {
            EditorApplication.delayCall -= SetUpOpenScenes;
            EditorApplication.delayCall += SetUpOpenScenes;
        }

        static void SetUpOpenScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            AddDimensionToBuild();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    SetUp(scene);
            }
        }

        static void AddDimensionToBuild()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(k_DimensionScene) == null)
                return;

            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == k_DimensionScene))
                return;

            scenes.Add(new EditorBuildSettingsScene(k_DimensionScene, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("Portail des étoiles : la scène Dimension_Etoiles est ajoutée à la liste des scènes du build.");
        }

        [MenuItem("Archery/Portail des étoiles/Replacer l'ouverture et les runes")]
        static void PlaceAgain()
        {
            foreach (var portal in Object.FindObjectsByType<StarPortal>(FindObjectsInactive.Include))
                PlaceOnStones(portal);
        }

        // L'ouverture et les runes, mesurées sur les pierres dans l'éditeur (en jeu, les statues statiques sont fusionnées
        // et ne se lisent plus) : des objets de la scène, qu'on peut ensuite ajuster à la main.
        static void PlaceOnStones(StarPortal portal)
        {
            portal.PlaceOnStones();
            EditorUtility.SetDirty(portal);
            EditorSceneManager.MarkSceneDirty(portal.gameObject.scene);
            Debug.Log($"Portail des étoiles : ouverture et runes posées sur les pierres, dans la scène « {portal.gameObject.scene.name} ». " +
                      "Enregistre-la (Ctrl+S).", portal);
        }

        static void SetUp(Scene scene)
        {
            Transform ring = null;
            var obelisks = new List<Transform>();
            foreach (var root in scene.GetRootGameObjects())
            {
                var existing = root.GetComponentInChildren<StarPortal>(true);
                if (existing != null)
                {
                    // Portail d'avant le 18 octobre (runes et voile fabriqués en jeu) : on pose l'ouverture et les runes.
                    if (new SerializedObject(existing).FindProperty("m_Opening").objectReferenceValue == null)
                        PlaceOnStones(existing);
                    return;
                }

                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.name == "statue_ring" && ring == null)
                        ring = transform;
                    else if (transform.name.StartsWith("statue_obelisk"))
                        obelisks.Add(transform);
                }
            }

            if (ring == null || obelisks.Count == 0)
                return;

            // Les obélisques autour de l'anneau (les 5 plus proches).
            obelisks = obelisks.OrderBy(o => Vector3.Distance(o.position, ring.position)).Take(5).ToList();

            var go = new GameObject("Portail des étoiles");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = ring.position;
            Undo.RegisterCreatedObjectUndo(go, "Portail des étoiles");
            var portal = go.AddComponent<StarPortal>();

            var settings = new SerializedObject(portal);
            settings.FindProperty("m_Ring").objectReferenceValue = ring;
            var list = settings.FindProperty("m_Obelisks");
            list.arraySize = obelisks.Count;
            for (var i = 0; i < obelisks.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = obelisks[i];
            settings.FindProperty("m_GlowMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(k_GlowMaterial);
            settings.FindProperty("m_RuneClip").objectReferenceValue = Clip("confirmation_002");
            settings.FindProperty("m_ColdClip").objectReferenceValue = Clip("question_001");
            settings.FindProperty("m_OpenClip").objectReferenceValue = Clip("maximize_009");
            settings.FindProperty("m_EnterClip").objectReferenceValue = Clip("glitch_004");
            settings.ApplyModifiedPropertiesWithoutUndo();

            Debug.Log($"Portail des étoiles : mis en place sur « {ring.name} » et {obelisks.Count} obélisques.", go);
            PlaceOnStones(portal);
        }

        static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>(k_Sounds + name + ".ogg");
    }
}
