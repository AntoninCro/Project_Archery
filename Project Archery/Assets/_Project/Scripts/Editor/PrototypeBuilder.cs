using System.Collections.Generic;
using System.IO;
using System.Linq;
using Archery.Bows;
using Archery.Combat;
using Archery.Player;
using Archery.Training;
using Archery.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Attachment;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Archery.EditorTools
{
    /// <summary>
    /// Construit le prototype de tir : matériaux, données, prefabs (arc, flèche, cibles) et scène de test.
    /// Menu : Archery &gt; Construire le prototype de tir. Relançable sans risque : tout est mis à jour.
    /// </summary>
    public static class PrototypeBuilder
    {
        const string k_Root = "Assets/_Project";
        const string k_MaterialsFolder = k_Root + "/Materials";
        const string k_PrefabsFolder = k_Root + "/Prefabs";
        const string k_DataFolder = k_Root + "/Data";
        const string k_BowsFolder = k_DataFolder + "/Bows";
        const string k_ScenesFolder = k_Root + "/Scenes";
        const string k_TexturesFolder = k_Root + "/Art/Textures";
        const string k_AudioFolder = k_Root + "/Audio/Placeholder";
        const string k_ScenePath = k_ScenesFolder + "/Prototype_Tir.unity";

        class MaterialSet
        {
            public Material Wood;
            public Material DarkWood;
            public Material Leather;
            public Material String;
            public Material Shaft;
            public Material Steel;
            public Material Fletching;
            public Material Nock;
            public Material Trail;
            public Material Ring;
            public Material TargetFace;
            public Material Straw;
            public Material Ground;
            public Material Line;
            public Material Burlap;
        }

        class ClipSet
        {
            public AudioClip Release;
            public AudioClip Creak;
            public AudioClip Flight;
            public AudioClip ImpactWood;
            public AudioClip ImpactTarget;
            public AudioClip ImpactFlesh;
            public AudioClip Headshot;
            public AudioClip Perfect;
            public AudioClip Gold;
            public AudioClip Tick;
            public AudioClip QuiverDraw;
            public AudioClip Nock;
        }

        [MenuItem("Archery/Construire le prototype de tir", priority = 0)]
        public static void Build()
        {
            if (File.Exists(k_ScenePath) && !EditorUtility.DisplayDialog("Prototype de tir",
                    "La scène Prototype_Tir existe déjà. La reconstruire ?\n(Les prefabs et les matériaux sont aussi mis à jour.)",
                    "Reconstruire", "Annuler"))
                return;

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // La scène est créée en premier : changer de scène décharge les assets inutilisés,
            // ce qui invaliderait les prefabs et matériaux créés juste avant.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            CreateFolders();
            var clips = LoadClips();
            var materials = CreateMaterials();
            var tuning = LoadOrCreate<ShotTuning>(k_DataFolder + "/ShotTuning.asset", null);
            var bows = CreateBowDefinitions();
            AssetDatabase.SaveAssets();

            BuildArrowPrefab(materials, clips);
            BuildBowPrefab(materials, clips, bows[0], tuning);
            BuildTargetPrefab(materials, clips, tuning);
            BuildDummyPrefab(materials, clips, tuning);
            PopulateScene(scene);

            AssetDatabase.SaveAssets();
            Debug.Log($"Prototype de tir construit : {k_ScenePath}. Lance la scène avec le casque branché (Link).");
        }

        // ----------------------------------------------------------------- Données

        static List<BowDefinition> CreateBowDefinitions()
        {
            // Valeurs du GDD, section 5.
            return new List<BowDefinition>
            {
                LoadOrCreate<BowDefinition>(k_BowsFolder + "/Bow_Chasse.asset", d =>
                {
                    d.displayName = "Arc de chasse";
                    d.description = "Équilibré. L'arc de départ.";
                    d.arrowSpeed = 35f;
                    d.damage = 10f;
                    d.ringDuration = 1.2f;
                }),
                LoadOrCreate<BowDefinition>(k_BowsFolder + "/Bow_Composite.asset", d =>
                {
                    d.displayName = "Arc composite";
                    d.description = "Anneau rapide, bandes larges.";
                    d.price = 120;
                    d.availableFromWave = 2;
                    d.arrowSpeed = 40f;
                    d.damage = 13f;
                    d.ringDuration = 1f;
                    d.goldHalfWidth = 0.075f;
                    d.greenWidth = 0.095f;
                    d.orangeWidth = 0.11f;
                }),
                LoadOrCreate<BowDefinition>(k_BowsFolder + "/Bow_Long.asset", d =>
                {
                    d.displayName = "Arc long";
                    d.description = "Puissant mais lent à charger.";
                    d.price = 200;
                    d.availableFromWave = 4;
                    d.arrowSpeed = 50f;
                    d.damage = 20f;
                    d.maxDrawDistance = 0.5f;
                    d.ringDuration = 1.5f;
                }),
                LoadOrCreate<BowDefinition>(k_BowsFolder + "/Bow_Runique.asset", d =>
                {
                    d.displayName = "Arc runique";
                    d.description = "Ses flèches traversent un ennemi.";
                    d.price = 350;
                    d.availableFromWave = 7;
                    d.arrowSpeed = 55f;
                    d.damage = 24f;
                    d.pierceCount = 1;
                }),
            };
        }

        static ClipSet LoadClips() => new ClipSet
        {
            Release = Clip("bow_release"),
            Creak = Clip("bow_creak_loop"),
            Flight = Clip("arrow_flight_loop"),
            ImpactWood = Clip("impact_wood"),
            ImpactTarget = Clip("impact_target"),
            ImpactFlesh = Clip("impact_flesh"),
            Headshot = Clip("headshot_ding"),
            Perfect = Clip("perfect_chime"),
            Gold = Clip("ring_gold"),
            Tick = Clip("ring_tick"),
            QuiverDraw = Clip("quiver_draw"),
            Nock = Clip("nock_click"),
        };

        static AudioClip Clip(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{k_AudioFolder}/{name}.wav");
            if (clip == null)
                Debug.LogWarning($"Son introuvable : {k_AudioFolder}/{name}.wav");
            return clip;
        }

        static MaterialSet CreateMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            var ringShader = Shader.Find("Archery/TimingRing");
            var trailShader = Shader.Find("Archery/UnlitVertexColor");
            if (lit == null || unlit == null || ringShader == null || trailShader == null)
                Debug.LogError("Shaders introuvables : vérifie que l'URP est actif et que les shaders Archery sont importés.");

            var targetFace = Mat("Target_Face", lit, Color.white, 0.1f);
            var faceTexture = CreateTargetFaceTexture();
            targetFace.SetTexture("_BaseMap", faceTexture);
            targetFace.mainTexture = faceTexture;

            return new MaterialSet
            {
                Wood = Mat("Bow_Wood", lit, new Color(0.55f, 0.33f, 0.16f), 0.35f),
                DarkWood = Mat("Bow_DarkWood", lit, new Color(0.3f, 0.18f, 0.09f), 0.3f),
                Leather = Mat("Bow_Leather", lit, new Color(0.17f, 0.1f, 0.06f), 0.15f),
                String = Mat("Bow_String", unlit, new Color(0.93f, 0.9f, 0.82f)),
                Shaft = Mat("Arrow_Shaft", lit, new Color(0.76f, 0.6f, 0.38f), 0.25f),
                Steel = Mat("Arrow_Head", lit, new Color(0.62f, 0.64f, 0.68f), 0.65f, 0.8f),
                Fletching = Mat("Arrow_Fletching", lit, new Color(0.85f, 0.18f, 0.14f), 0.1f),
                Nock = Mat("Arrow_Nock", lit, new Color(0.95f, 0.85f, 0.3f), 0.3f),
                Trail = Mat("Arrow_Trail", trailShader, Color.white),
                Ring = Mat("Timing_Ring", ringShader, Color.white),
                TargetFace = targetFace,
                Straw = Mat("Target_Straw", lit, new Color(0.86f, 0.76f, 0.47f), 0.05f),
                Ground = Mat("Ground_Grass", lit, new Color(0.36f, 0.55f, 0.27f), 0.05f),
                Line = Mat("Shooting_Line", unlit, new Color(0.95f, 0.95f, 0.95f)),
                Burlap = Mat("Dummy_Burlap", lit, new Color(0.72f, 0.6f, 0.42f), 0.05f),
            };
        }

        static Material Mat(string name, Shader shader, Color color, float smoothness = 0.2f, float metallic = 0f)
        {
            var path = $"{k_MaterialsFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else if (shader != null && material.shader != shader)
            {
                material.shader = shader;
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);

            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Face de cible aux couleurs officielles : or, rouge, bleu, noir, blanc.</summary>
        static Texture2D CreateTargetFaceTexture()
        {
            const int size = 512;
            var path = $"{k_TexturesFolder}/TargetFace.png";
            var ringColors = new[]
            {
                new Color(1f, 0.85f, 0.15f), new Color(1f, 0.85f, 0.15f),
                new Color(0.88f, 0.15f, 0.12f), new Color(0.88f, 0.15f, 0.12f),
                new Color(0.12f, 0.55f, 0.88f), new Color(0.12f, 0.55f, 0.88f),
                new Color(0.08f, 0.08f, 0.08f), new Color(0.08f, 0.08f, 0.08f),
                new Color(0.97f, 0.97f, 0.97f), new Color(0.97f, 0.97f, 0.97f),
            };
            var outside = new Color(0.86f, 0.76f, 0.47f);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = (x + 0.5f) / size * 2f - 1f;
                    var dy = (y + 0.5f) / size * 2f - 1f;
                    var radius = Mathf.Sqrt(dx * dx + dy * dy);
                    Color color;
                    if (radius > 1f)
                    {
                        color = outside;
                    }
                    else
                    {
                        var scaled = radius * 10f;
                        var ring = Mathf.Min(9, Mathf.FloorToInt(scaled));
                        color = ringColors[ring];
                        // Fines lignes de séparation entre les anneaux.
                        var edge = Mathf.Min(scaled - Mathf.Floor(scaled), Mathf.Ceil(scaled) - scaled);
                        if (edge < 0.025f && scaled > 0.05f)
                            color = ring == 6 || ring == 7 ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.1f, 0.1f, 0.1f);
                    }

                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = true;
                importer.anisoLevel = 4;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ----------------------------------------------------------------- Prefabs

        static void BuildArrowPrefab(MaterialSet m, ClipSet c)
        {
            const float length = 0.8f;
            var root = new GameObject("Arrow");

            var body = root.AddComponent<Rigidbody>();
            body.mass = 0.05f;
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.None;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.linearDamping = 0f;
            body.angularDamping = 0f;

            // L'origine de la flèche est l'encoche ; elle pointe vers +Z.
            var visual = Empty("Visual", root.transform, Vector3.zero);
            Part(PrimitiveType.Cylinder, "Shaft", visual, new Vector3(0f, 0f, length * 0.5f), Quaternion.Euler(90f, 0f, 0f), new Vector3(0.009f, length * 0.5f, 0.009f), m.Shaft);
            Part(PrimitiveType.Cube, "Head", visual, new Vector3(0f, 0f, length - 0.02f), Quaternion.Euler(0f, 0f, 45f), new Vector3(0.016f, 0.016f, 0.05f), m.Steel);
            for (var i = 0; i < 3; i++)
            {
                var rotation = Quaternion.Euler(0f, 0f, i * 120f);
                Part(PrimitiveType.Cube, "Fletching", visual, rotation * new Vector3(0f, 0.014f, 0.08f), rotation, new Vector3(0.0015f, 0.024f, 0.09f), m.Fletching);
            }

            Part(PrimitiveType.Cube, "Nock", visual, new Vector3(0f, 0f, 0.008f), Quaternion.identity, new Vector3(0.012f, 0.012f, 0.016f), m.Nock);

            var tip = Empty("Tip", root.transform, new Vector3(0f, 0f, length));

            var trailObject = Empty("Trail", root.transform, new Vector3(0f, 0f, 0.1f));
            var trail = trailObject.gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.25f;
            trail.minVertexDistance = 0.05f;
            trail.widthMultiplier = 0.03f;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.numCapVertices = 2;
            trail.emitting = false;
            trail.sharedMaterial = m.Trail;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;

            var flightAudio = root.AddComponent<AudioSource>();
            flightAudio.clip = c.Flight;
            flightAudio.loop = true;
            flightAudio.playOnAwake = false;
            flightAudio.spatialBlend = 1f;
            flightAudio.volume = 0.45f;
            flightAudio.rolloffMode = AudioRolloffMode.Logarithmic;
            flightAudio.minDistance = 1.5f;
            flightAudio.maxDistance = 40f;

            var arrow = root.AddComponent<Arrow>();
            SetReference(arrow, "m_Tip", tip);
            SetReference(arrow, "m_Trail", trail);
            SetReference(arrow, "m_FlightAudio", flightAudio);
            SetReference(arrow, "m_ImpactDefault", c.ImpactWood);
            SetReference(arrow, "m_ImpactLiving", c.ImpactFlesh);
            SetVector3(arrow, "m_HoldOffset", Vector3.zero);

            SavePrefab(root, "Arrow");
        }

        static void BuildBowPrefab(MaterialSet m, ClipSet c, BowDefinition definition, ShotTuning tuning)
        {
            var root = new GameObject("Bow");

            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            // Collider de prise autour de la poignée (sur le layer Default, le seul que la main détecte).
            var grabCollider = root.AddComponent<BoxCollider>();
            grabCollider.center = new Vector3(0f, 0f, -0.01f);
            grabCollider.size = new Vector3(0.1f, 0.5f, 0.14f);

            // La pose de la manette (pose « aim ») est à l'avant du contrôleur :
            // on recule la poignée de quelques centimètres pour qu'elle tombe dans la paume.
            var grip = Empty("Grip Attach", root.transform, new Vector3(0f, 0.015f, 0.055f));

            var model = Empty("Model", root.transform, Vector3.zero);
            Part(PrimitiveType.Cube, "Riser", model, Vector3.zero, Quaternion.identity, new Vector3(0.03f, 0.34f, 0.045f), m.Wood);
            Part(PrimitiveType.Cube, "Grip", model, new Vector3(0f, -0.02f, 0f), Quaternion.identity, new Vector3(0.037f, 0.11f, 0.052f), m.Leather);
            Part(PrimitiveType.Cube, "Shelf", model, new Vector3(0f, 0.045f, 0f), Quaternion.identity, new Vector3(0.036f, 0.01f, 0.03f), m.DarkWood);
            var rest = Empty("Arrow Rest", model, new Vector3(0f, 0.058f, 0f));

            var upperLimb = Empty("Upper Limb", model, new Vector3(0f, 0.16f, 0f));
            var lowerLimb = Empty("Lower Limb", model, new Vector3(0f, -0.16f, 0f));
            var stringTop = BuildLimb(upperLimb, 1f, m);
            var stringBottom = BuildLimb(lowerLimb, -1f, m);

            var stringObject = Empty("String", model, Vector3.zero);
            var line = stringObject.gameObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 3;
            line.widthMultiplier = 0.004f;
            line.numCapVertices = 0;
            line.sharedMaterial = m.String;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            var top = model.InverseTransformPoint(stringTop.position);
            var bottom = model.InverseTransformPoint(stringBottom.position);
            line.SetPositions(new[] { top, new Vector3(0f, rest.localPosition.y, (top.z + bottom.z) * 0.5f), bottom });

            var ringObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ringObject.name = "Timing Ring";
            Object.DestroyImmediate(ringObject.GetComponent<Collider>());
            ringObject.transform.SetParent(model, false);
            ringObject.transform.localPosition = new Vector3(-0.08f, 0.07f, 0f);
            ringObject.transform.localScale = Vector3.one * 0.1f;
            var ringRenderer = ringObject.GetComponent<MeshRenderer>();
            ringRenderer.sharedMaterial = m.Ring;
            ringRenderer.shadowCastingMode = ShadowCastingMode.Off;
            ringRenderer.receiveShadows = false;
            var timingRing = ringObject.AddComponent<TimingRing>();
            SetReference(timingRing, "m_Renderer", ringRenderer);

            var creak = model.gameObject.AddComponent<AudioSource>();
            creak.clip = c.Creak;
            creak.loop = true;
            creak.playOnAwake = false;
            creak.volume = 0f;
            creak.spatialBlend = 1f;
            creak.minDistance = 0.5f;
            creak.maxDistance = 10f;
            creak.dopplerLevel = 0f;

            var bow = root.AddComponent<Bow>();
            bow.movementType = XRBaseInteractable.MovementType.Instantaneous;
            bow.throwOnDetach = false;
            bow.useDynamicAttach = false;
            bow.retainTransformParent = false;
            bow.farAttachMode = InteractableFarAttachMode.Near;
            bow.attachTransform = grip;

            SetReference(bow, "m_Definition", definition);
            SetReference(bow, "m_ShotTuning", tuning);
            SetReference(bow, "m_Model", model);
            SetReference(bow, "m_ArrowRest", rest);
            SetReference(bow, "m_StringTop", stringTop);
            SetReference(bow, "m_StringBottom", stringBottom);
            SetReference(bow, "m_String", line);
            SetReference(bow, "m_UpperLimb", upperLimb);
            SetReference(bow, "m_LowerLimb", lowerLimb);
            SetReference(bow, "m_TimingRing", timingRing);
            SetReference(bow, "m_CreakSource", creak);
            SetReference(bow, "m_ReleaseClip", c.Release);
            SetReference(bow, "m_NockClip", c.Nock);
            SetReference(bow, "m_GoldClip", c.Gold);
            SetReference(bow, "m_RingLoopClip", c.Tick);
            SetReference(bow, "m_PerfectClip", c.Perfect);

            SavePrefab(root, "Bow");
        }

        /// <summary>
        /// Branche faite de 3 segments qui se courbent vers l'archer (-Z). Renvoie l'extrémité, où s'accroche la corde.
        /// </summary>
        static Transform BuildLimb(Transform pivot, float sign, MaterialSet m)
        {
            float[] angles = { 8f, 20f, 35f };
            float[] widths = { 0.028f, 0.024f, 0.019f };
            float[] depths = { 0.018f, 0.015f, 0.012f };
            const float segmentLength = 0.15f;

            var start = Vector3.zero;
            for (var i = 0; i < angles.Length; i++)
            {
                var radians = angles[i] * Mathf.Deg2Rad;
                var direction = new Vector3(0f, sign * Mathf.Cos(radians), -Mathf.Sin(radians));
                var center = start + direction * (segmentLength * 0.5f);
                var rotation = Quaternion.FromToRotation(Vector3.up, direction);
                Part(PrimitiveType.Cube, $"Segment {i + 1}", pivot, center, rotation, new Vector3(widths[i], segmentLength, depths[i]), m.Wood);
                start += direction * segmentLength;
            }

            Part(PrimitiveType.Cube, "Tip", pivot, start, Quaternion.identity, new Vector3(0.022f, 0.02f, 0.02f), m.DarkWood);
            return Empty("String Anchor", pivot, start);
        }

        static void BuildTargetPrefab(MaterialSet m, ClipSet c, ShotTuning tuning)
        {
            const float faceHeight = 1.3f;
            const float faceRadius = 0.6f;
            var root = new GameObject("Training Target");

            // Le tireur est du côté -Z de la cible.
            var hitCollider = root.AddComponent<BoxCollider>();
            hitCollider.center = new Vector3(0f, faceHeight, 0f);
            hitCollider.size = new Vector3(faceRadius * 2.05f, faceRadius * 2.05f, 0.12f);

            Part(PrimitiveType.Cylinder, "Boss", root.transform, new Vector3(0f, faceHeight, 0f), Quaternion.Euler(90f, 0f, 0f), new Vector3(faceRadius * 2.1f, 0.06f, faceRadius * 2.1f), m.Straw);
            Part(PrimitiveType.Quad, "Face", root.transform, new Vector3(0f, faceHeight, -0.062f), Quaternion.identity, new Vector3(faceRadius * 2f, faceRadius * 2f, 1f), m.TargetFace);
            var faceCenter = Empty("Face Center", root.transform, new Vector3(0f, faceHeight, -0.062f));
            faceCenter.localRotation = Quaternion.LookRotation(Vector3.back);

            // Chevalet.
            Part(PrimitiveType.Cube, "Leg Left", root.transform, new Vector3(-0.45f, 0.75f, 0.25f), Quaternion.Euler(-15f, 0f, 0f), new Vector3(0.07f, 1.6f, 0.07f), m.DarkWood);
            Part(PrimitiveType.Cube, "Leg Right", root.transform, new Vector3(0.45f, 0.75f, 0.25f), Quaternion.Euler(-15f, 0f, 0f), new Vector3(0.07f, 1.6f, 0.07f), m.DarkWood);
            Part(PrimitiveType.Cube, "Leg Back", root.transform, new Vector3(0f, 0.7f, 0.55f), Quaternion.Euler(25f, 0f, 0f), new Vector3(0.07f, 1.5f, 0.07f), m.DarkWood);

            var target = root.AddComponent<TrainingTarget>();
            SetReference(target, "m_FaceCenter", faceCenter);
            SetFloat(target, "m_FaceRadius", faceRadius);
            SetReference(target, "m_BullseyeClip", c.Perfect);
            SetReference(target, "m_ShotTuning", tuning);

            SavePrefab(root, "Training Target");
        }

        static void BuildDummyPrefab(MaterialSet m, ClipSet c, ShotTuning tuning)
        {
            var root = new GameObject("Training Dummy");

            // Rigidbody cinématique : un seul « corps » pour les flèches perçantes.
            var body = root.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            var health = root.AddComponent<Health>();
            SetFloat(health, "m_MaxHealth", 100f);
            SetBool(health, "m_Invulnerable", true);
            SetFloat(health, "m_RegenPerSecond", 60f);
            SetFloat(health, "m_RegenDelay", 1.5f);
            var popups = root.AddComponent<DamagePopups>();
            SetReference(popups, "m_ShotTuning", tuning);

            Part(PrimitiveType.Cylinder, "Post", root.transform, new Vector3(0f, 0.45f, 0f), Quaternion.identity, new Vector3(0.08f, 0.45f, 0.08f), m.DarkWood, true);
            Part(PrimitiveType.Cube, "Base", root.transform, new Vector3(0f, 0.03f, 0f), Quaternion.identity, new Vector3(0.6f, 0.06f, 0.6f), m.DarkWood, true);

            var torso = Part(PrimitiveType.Capsule, "Body", root.transform, new Vector3(0f, 1.2f, 0f), Quaternion.identity, new Vector3(0.5f, 0.45f, 0.32f), m.Burlap, true);
            var torsoHitbox = torso.AddComponent<Hitbox>();
            SetEnum(torsoHitbox, "m_Zone", (int)HitZone.Body);
            SetFloat(torsoHitbox, "m_DamageMultiplier", 1f);

            var head = Part(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0f, 1.82f, 0f), Quaternion.identity, Vector3.one * 0.3f, m.Burlap, true);
            var headHitbox = head.AddComponent<Hitbox>();
            SetEnum(headHitbox, "m_Zone", (int)HitZone.Head);
            SetFloat(headHitbox, "m_DamageMultiplier", 2f);
            SetReference(headHitbox, "m_HitClip", c.Headshot);

            SavePrefab(root, "Training Dummy");
        }

        // ----------------------------------------------------------------- Scène

        static void PopulateScene(Scene scene)
        {
            // Prefabs et matériaux rechargés depuis le disque : on ne garde jamais une référence vers un asset déchargé.
            var arrowPrefab = LoadAsset<GameObject>($"{k_PrefabsFolder}/Arrow.prefab");
            var bowPrefab = LoadAsset<GameObject>($"{k_PrefabsFolder}/Bow.prefab");
            var targetPrefab = LoadAsset<GameObject>($"{k_PrefabsFolder}/Training Target.prefab");
            var dummyPrefab = LoadAsset<GameObject>($"{k_PrefabsFolder}/Training Dummy.prefab");
            var groundMaterial = LoadAsset<Material>($"{k_MaterialsFolder}/Ground_Grass.mat");
            var lineMaterial = LoadAsset<Material>($"{k_MaterialsFolder}/Shooting_Line.mat");
            if (arrowPrefab == null || bowPrefab == null || targetPrefab == null || dummyPrefab == null)
                return;

            // La caméra vient du XR Origin.
            foreach (var camera in Object.FindObjectsByType<Camera>())
                Object.DestroyImmediate(camera.gameObject);

            var light = Object.FindAnyObjectByType<Light>();
            if (light != null)
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();

            var originPrefab = FindXROriginPrefab();
            if (originPrefab == null)
            {
                Debug.LogError("Prefab « XR Origin (XR Rig) » des Starter Assets de XRI introuvable.");
                return;
            }

            var origin = (GameObject)PrefabUtility.InstantiatePrefab(originPrefab, scene);
            origin.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            origin.AddComponent<PlayerRig>();
            var quiver = origin.AddComponent<Quiver>();
            SetReference(quiver, "m_DrawClip", LoadClipQuiet("quiver_draw"));

            var pool = new GameObject("Arrow Pool").AddComponent<ArrowPool>();
            SetReference(pool, "m_ArrowPrefab", arrowPrefab.GetComponent<Arrow>());

            var bow = (GameObject)PrefabUtility.InstantiatePrefab(bowPrefab, scene);
            bow.transform.position = new Vector3(-0.25f, 1f, 0.4f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(20f, 1f, 20f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMaterial;

            Part(PrimitiveType.Cube, "Shooting Line", null, new Vector3(0f, 0.005f, 0.6f), Quaternion.identity, new Vector3(6f, 0.01f, 0.05f), lineMaterial);

            // Cibles fixes à 10, 20 et 30 m, une cible mobile et deux mannequins (dont un mobile).
            PlaceTarget(targetPrefab, scene, new Vector3(-3f, 0f, 10f), "10 m", null);
            PlaceTarget(targetPrefab, scene, new Vector3(0f, 0f, 20f), "20 m", null);
            PlaceTarget(targetPrefab, scene, new Vector3(3f, 0f, 30f), "30 m", null);
            PlaceTarget(targetPrefab, scene, new Vector3(-6f, 0f, 25f), "Mobile", new Vector3(8f, 0f, 0f));
            PlaceDummy(dummyPrefab, scene, new Vector3(5f, 0f, 12f), null);
            PlaceDummy(dummyPrefab, scene, new Vector3(-4f, 0f, 16f), new Vector3(6f, 0f, 0f));

            EditorSceneManager.SaveScene(scene, k_ScenePath);

            var buildScenes = EditorBuildSettings.scenes.Where(s => s.path != k_ScenePath).ToList();
            buildScenes.Insert(0, new EditorBuildSettingsScene(k_ScenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
        }

        static void PlaceTarget(GameObject prefab, Scene scene, Vector3 position, string label, Vector3? movement)
        {
            var target = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            target.transform.position = position;
            target.name = $"Target {label}";
            AddLabel(target.transform, label, new Vector3(0f, 2.15f, 0f));
            if (movement.HasValue)
                AddMover(target, movement.Value, 1.5f);
        }

        static void PlaceDummy(GameObject prefab, Scene scene, Vector3 position, Vector3? movement)
        {
            var dummy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            dummy.transform.SetPositionAndRotation(position, Quaternion.LookRotation(-new Vector3(position.x, 0f, position.z)));
            if (movement.HasValue)
                AddMover(dummy, movement.Value, 1.2f);
        }

        static void AddMover(GameObject go, Vector3 offset, float speed)
        {
            var mover = go.AddComponent<PingPongMover>();
            SetVector3(mover, "m_Offset", offset);
            SetFloat(mover, "m_Speed", speed);
        }

        static void AddLabel(Transform parent, string text, Vector3 localPosition)
        {
            var label = new GameObject("Label");
            label.transform.SetParent(parent, false);
            label.transform.localPosition = localPosition;
            label.transform.localRotation = Quaternion.LookRotation(Vector3.forward);
            var tmp = label.AddComponent<TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.color = Color.white;
        }

        static GameObject FindXROriginPrefab()
        {
            var paths = AssetDatabase.FindAssets("\"XR Origin (XR Rig)\" t:Prefab")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith("XR Origin (XR Rig).prefab"))
                .OrderByDescending(p => p.Contains("Starter Assets"))
                .ToList();
            return paths.Count > 0 ? AssetDatabase.LoadAssetAtPath<GameObject>(paths[0]) : null;
        }

        static AudioClip LoadClipQuiet(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{k_AudioFolder}/{name}.wav");

        static T LoadAsset<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                Debug.LogError($"Asset introuvable : {path}");
            return asset;
        }

        // ----------------------------------------------------------------- Utilitaires

        static void CreateFolders()
        {
            foreach (var folder in new[] { k_MaterialsFolder, k_PrefabsFolder, k_BowsFolder, k_ScenesFolder, k_TexturesFolder, k_AudioFolder })
                Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
        }

        static T LoadOrCreate<T>(string path, System.Action<T> initialize) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;

            asset = ScriptableObject.CreateInstance<T>();
            initialize?.Invoke(asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void SavePrefab(GameObject root, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(root, $"{k_PrefabsFolder}/{name}.prefab");
            Object.DestroyImmediate(root);
        }

        static GameObject Part(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (!keepCollider)
                Object.DestroyImmediate(go.GetComponent<Collider>());

            if (parent != null)
                go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        static Transform Empty(string name, Transform parent, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        static SerializedProperty FindProperty(SerializedObject serializedObject, string propertyPath)
        {
            var property = serializedObject.FindProperty(propertyPath);
            if (property == null)
                Debug.LogError($"Champ « {propertyPath} » introuvable sur {serializedObject.targetObject.GetType().Name}.");
            return property;
        }

        static void SetReference(Object target, string propertyPath, Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = FindProperty(serializedObject, propertyPath);
            if (property == null)
                return;
            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(Object target, string propertyPath, float value)
        {
            var serializedObject = new SerializedObject(target);
            var property = FindProperty(serializedObject, propertyPath);
            if (property == null)
                return;
            property.floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetBool(Object target, string propertyPath, bool value)
        {
            var serializedObject = new SerializedObject(target);
            var property = FindProperty(serializedObject, propertyPath);
            if (property == null)
                return;
            property.boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetEnum(Object target, string propertyPath, int value)
        {
            var serializedObject = new SerializedObject(target);
            var property = FindProperty(serializedObject, propertyPath);
            if (property == null)
                return;
            property.enumValueIndex = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetVector3(Object target, string propertyPath, Vector3 value)
        {
            var serializedObject = new SerializedObject(target);
            var property = FindProperty(serializedObject, propertyPath);
            if (property == null)
                return;
            property.vector3Value = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
