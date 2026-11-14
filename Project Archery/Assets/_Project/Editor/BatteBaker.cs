using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Archery.BowClasses;
using Archery.Bows;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Archery.EditorTools
{
    /// <summary>
    /// La batte de baseball dans le jeu (13 novembre, l'idée du joueur ; <c>Combat/BaseballBat</c>) : une batte <b>steampunk</b>
    /// faite ici (le joueur, le même jour : un autre univers, loin du cyberpunk des autres, « des tuyaux un peu partout », de la
    /// fumée quand elle frappe), dans <c>Art/Batte</c> ; sa fiche (<c>Data/BowClasses/Bow_Batte</c>), l'aperçu du menu des armes
    /// (<c>Prefabs/Bow Visual Batte</c>) et sa carte à la fin de la liste des armes (<c>Prefabs/UI/Bow Classes</c>), <b>débloquée
    /// dès le départ</b> (le joueur : plus tard, un secret dans un acte 2).
    /// </summary>
    /// <remarks>
    /// <para>La batte : un tonneau de bois sombre verni, cerclé de laiton riveté, un bout en laiton ; une poignée de cuir enroulé, un
    /// pommeau et un collier de laiton ; deux tuyaux de cuivre qui sortent du collier, longent la batte et finissent en cheminées
    /// près du bout (la vapeur en sort, repères « Vapeur ») ; un manomètre à l'avant, un volant de vanne rouge à l'arrière, et sur
    /// le côté une petite fenêtre de foyer qui rougeoie, grillagée de laiton.</para>
    /// <para>Refaite le 14 novembre (le joueur : « l'apparence ne me convient pas, il faudrait plus de steampunk, avec vraiment des
    /// conduits de vapeur qui bougent dans la batte »), avec des pièces qui bougent (<c>BaseballBat.Look.cs</c>) : deux tubes de verre
    /// à demi enfoncés dans le bois, de part et d'autre du foyer, où coule la vapeur (« Conduits », une texture de vapeur faite ici
    /// qui défile) ; le cadran du manomètre gradué, sa zone rouge et son aiguille (« Aiguille »), qui montre la recharge de la
    /// grenade ; un engrenage sur son axe (« Engrenage ») ; un piston qui pompe (« Piston ») ; la lueur du foyer à part (« Foyer »),
    /// qui scintille ; une fuite à la vanne (« Fuite »).</para>
    /// <para>Tout est fait de formes simples (tours de profil, tubes, boîtes). Se fait tout seul, une fois (et de nouveau tant que la
    /// fiche n'a pas ce modèle, son aperçu, la vapeur, l'anneau du verrou ou sa carte). La fiche n'est écrite qu'à sa création :
    /// ensuite, c'est elle qui fait foi. Les sons choisis par le joueur (<c>Audio/Batte_Coup</c>, <c>Audio/Batte_Parfait</c>, faits par
    /// <c>Tools/sons/batte.py</c>) remplacent les sons provisoires dès qu'ils sont là. Menu <c>Archery > Batte > Refaire le modèle et
    /// la carte</c>.</para>
    /// </remarks>
    [InitializeOnLoad]
    static class BatteBaker
    {
        const string k_Folder = "Assets/_Project/Art/Batte";
        const string k_Model = k_Folder + "/Batte.prefab";
        const string k_SteamTexture = k_Folder + "/Batte_Vapeur.png";
        const string k_Definition = "Assets/_Project/Data/BowClasses/Bow_Batte.asset";
        const string k_Visual = "Assets/_Project/Prefabs/Bow Visual Batte.prefab";
        const string k_Loadout = "Assets/_Project/Prefabs/UI/Bow Classes.prefab";
        const string k_HitClip = "Assets/kenney_impact-sounds/Audio/impactWood_heavy_000.ogg";
        const string k_GlowShader = "Assets/_Project/Shaders/Glow.shader";

        // Les sons choisis par le joueur (13 novembre au soir, freesound, CC0) : « Bat_hit2 » de hashtagsmcgee (une batte
        // d'aluminium, un « ping » sec) pour les coups qui ne sont pas parfaits ; pour le parfait, « Big impact » d'AudioPapkin,
        // saturé fort et mêlé à ce coup (Tools/sons/batte.py). Dès qu'ils sont dans Audio/, ils remplacent les sons provisoires.
        const string k_ChosenHit = "Assets/_Project/Audio/Batte_Coup.wav";
        const string k_ChosenPerfect = "Assets/_Project/Audio/Batte_Parfait.wav";
        const string k_SteamMaterial = "Assets/_Project/Art/Grenade/Grenade fumee.mat";
        const string k_Vent = "Vapeur";

        // La pièce qui dit que le modèle est celui du 14 novembre (avant, sans pièces mobiles : il est refait).
        const string k_Gear = "Engrenage";

        // Le bout de la batte, depuis le milieu de la poignée (repère « Bout », lu par la batte en jeu).
        const float k_TipDistance = 0.72f;

        // Les matériaux du corps, dans l'ordre des sous-maillages.
        const int k_Wood = 0;
        const int k_Brass = 1;
        const int k_Copper = 2;
        const int k_Leather = 3;
        const int k_Dial = 4;
        const int k_Red = 5;
        const int k_Glass = 6;
        const int k_BodyMaterials = 7;

        // Le profil du tonneau, de la poignée au bout : (hauteur, rayon) en mètres, le milieu de la poignée à 0. 86 cm en tout
        // avec le pommeau, comme une vraie batte (poignée de 2,8 cm de diamètre, tonneau de 6,8 cm). Le bout (au-delà de 71 cm)
        // est en laiton.
        static readonly Vector2[] k_Barrel =
        {
            new Vector2(0.16f, 0.0148f),
            new Vector2(0.19f, 0.016f),
            new Vector2(0.27f, 0.019f),
            new Vector2(0.37f, 0.026f),
            new Vector2(0.47f, 0.032f),
            new Vector2(0.58f, 0.034f),
            new Vector2(0.69f, 0.034f),
            new Vector2(0.712f, 0.032f),
            new Vector2(0.726f, 0.024f),
            new Vector2(0.733f, 0f),
        };

        // Le pommeau de laiton, sous la poignée.
        static readonly Vector2[] k_Knob =
        {
            new Vector2(-0.127f, 0f),
            new Vector2(-0.127f, 0.019f),
            new Vector2(-0.118f, 0.024f),
            new Vector2(-0.106f, 0.023f),
            new Vector2(-0.098f, 0.0135f),
        };

        const float k_GripBottom = -0.098f;
        const float k_GripTop = 0.16f;
        const float k_BrassTop = 0.71f;
        const int k_Sides = 16;

        // Les cerclages de laiton (hauteurs), le collier au haut de la poignée d'où sortent les tuyaux, et les rivets.
        static readonly float[] k_Bands = { 0.34f, 0.5f, 0.66f };
        const float k_CollarHeight = 0.175f;
        const float k_CollarRadius = 0.023f;
        const int k_RivetsPerBand = 8;

        // Où sont les pièces (angles autour de la batte, en degrés : 0 = +X, 90 = +Z) : le foyer (0°), les tubes de verre de part
        // et d'autre (±40°, au-dessus du manomètre), le manomètre (90°), l'engrenage (110°), les tuyaux de cuivre (150° et 210°), le piston (180°), le
        // volant (270°).
        static readonly float[] k_PipeAngles = { 150f, 210f };
        static readonly float[] k_GlassAngles = { 40f, -40f };
        const float k_PipeRadius = 0.0045f;
        const float k_GlassRadius = 0.006f;
        const float k_SteamRadius = 0.0042f;
        const float k_GlassFrom = 0.27f;
        const float k_GlassTo = 0.68f;
        const float k_GaugeHeight = 0.24f;
        const float k_ValveHeight = 0.32f;
        const float k_FurnaceHeight = 0.42f;
        const float k_GearAngle = 110f;
        const float k_GearHeight = 0.3f;
        const float k_GearRadius = 0.010f;
        const int k_GearTeeth = 10;
        const float k_PistonAngle = 180f;
        const float k_PistonFrom = 0.2f;
        const float k_PistonTo = 0.255f;

        // L'aiguille du manomètre, comme en jeu : 0° vers le haut du cadran, de -135° (vide) à +135° ; la zone rouge de 95° à 135°.
        const float k_DialFrom = -135f;
        const float k_DialRed = 95f;
        const float k_DialTo = 135f;

        static readonly Color k_CardColor = new Color(1f, 0.55f, 0.2f, 1f);

        const string k_Name = "Batte de baseball";
        const string k_Description = "Une batte à vapeur : lance la grenade, frappe-la, elle file sur l'ennemi que tu regardes.";
        const string k_Power = "Lance une grenade en l'air et frappe-la avec la batte : plus elle a volé, meilleur est le coup. Grenade toutes les 3 s";

        static BatteBaker()
        {
            EditorApplication.delayCall -= AutoBake;
            EditorApplication.delayCall += AutoBake;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredEditMode)
                    return;
                EditorApplication.delayCall -= AutoBake;
                EditorApplication.delayCall += AutoBake;
            };
        }

        static void AutoBake()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(k_Loadout) == null)
                return;
            var definition = AssetDatabase.LoadAssetAtPath<BowDefinition>(k_Definition);
            if (definition != null && definition.bat && definition.batModel != null && definition.batModel.transform.Find(k_Vent) != null &&
                definition.batModel.transform.Find(k_Gear) != null && definition.batSteamMaterial != null &&
                definition.batLockMaterial != null && definition.visualPrefab != null && InLoadout(definition))
            {
                var sounds = ApplyChosenSounds(definition);
                if (TuneDamage(definition) | sounds)
                {
                    EditorUtility.SetDirty(definition);
                    AssetDatabase.SaveAssets();
                }

                return;
            }

            Bake();
        }

        // Les sons choisis par le joueur, s'ils sont dans Audio/ et que la fiche a encore les sons provisoires (ou aucun) : un son
        // changé à la main dans la fiche reste. Vrai si un son a changé.
        static bool ApplyChosenSounds(BowDefinition definition)
        {
            var changed = false;
            var hit = AssetDatabase.LoadAssetAtPath<AudioClip>(k_ChosenHit);
            var provisional = AssetDatabase.LoadAssetAtPath<AudioClip>(k_HitClip);
            if (hit != null && (definition.batHitClip == null || definition.batHitClip == provisional))
            {
                definition.batHitClip = hit;
                changed = true;
            }

            var perfect = AssetDatabase.LoadAssetAtPath<AudioClip>(k_ChosenPerfect);
            if (perfect != null && definition.batHomeRunClip == null)
            {
                definition.batHomeRunClip = perfect;
                changed = true;
            }

            if (changed)
                Debug.Log("La batte de baseball : les sons choisis par le joueur sont en place (Audio/Batte_Coup, Audio/Batte_Parfait).");
            return changed;
        }

        [MenuItem("Archery/Batte/Refaire le modèle et la carte")]
        static void Bake()
        {
            if (!AssetDatabase.IsValidFolder(k_Folder))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Batte");

            var steam = SteamMaterial();
            var model = BuildModel(steam);
            if (model == null)
                return;

            var created = false;
            var definition = AssetDatabase.LoadAssetAtPath<BowDefinition>(k_Definition);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<BowDefinition>();
                Fill(definition);
                AssetDatabase.CreateAsset(definition, k_Definition);
                created = true;
            }

            definition.bat = true;
            definition.batModel = model;
            if (definition.batHitClip == null)
                definition.batHitClip = AssetDatabase.LoadAssetAtPath<AudioClip>(k_HitClip);
            ApplyChosenSounds(definition);
            TuneDamage(definition);
            if (definition.batSteamMaterial == null)
                definition.batSteamMaterial = AssetDatabase.LoadAssetAtPath<Material>(k_SteamMaterial);
            if (definition.batLockMaterial == null)
                definition.batLockMaterial = LockMaterial();
            definition.visualPrefab = BuildVisual(model);
            EditorUtility.SetDirty(definition);

            AddToLoadout(definition);
            AssetDatabase.SaveAssets();
            Debug.Log($"La batte de baseball : la batte steampunk refaite dans {k_Folder} (14 novembre : tubes de verre où coule la vapeur, " +
                      $"manomètre de la recharge, engrenage, piston, foyer qui scintille), fiche {k_Definition}{(created ? " (créée)" : "")}, " +
                      $"carte dans {k_Loadout}, l'anneau du verrou de la main libre. Raccourci de test : F6 l'équipe, F6 encore rend " +
                      "l'arme d'avant.");
        }

        // Les réglages de départ. Les flèches de la grenade gardent les dégâts de l'arc du début de partie
        // (ArrowGrenade.ArrowDamage) ; Damage et Arrow Speed ne servent qu'aux jauges du menu.
        static void Fill(BowDefinition definition)
        {
            definition.displayName = k_Name;
            definition.description = k_Description;
            definition.specialPower = k_Power;
            definition.damage = 10f;
            definition.arrowSpeed = 34f;
            definition.arrowGravity = 1f;
            definition.ringDuration = 1f;
            definition.batDamage = k_BatDamage;
        }

        // Le coup de batte, après l'essai du 13 novembre au soir (le joueur : « le coup en mêlée est bon, mais fait peut-être un peu
        // trop de dégâts ») : 22 au lieu de 30 (−25 %). La fiche ne change que si elle a encore l'ancienne valeur.
        const float k_BatDamage = 22f;
        const float k_OldBatDamage = 30f;

        static bool TuneDamage(BowDefinition definition)
        {
            if (!Mathf.Approximately(definition.batDamage, k_OldBatDamage))
                return false;
            definition.batDamage = k_BatDamage;
            Debug.Log($"La batte de baseball : le coup en mêlée passe de {k_OldBatDamage} à {k_BatDamage} dégâts (essai du 13 novembre au soir).");
            return true;
        }

        // ---------------------------------------------------------------- le modèle

        // Le corps et ses pièces : chacune son maillage (Art/Batte/Batte_*.asset) ; les pièces mobiles sont des enfants placés là où
        // elles tournent ou glissent (leur Y local est leur axe).
        static GameObject BuildModel(Material steam)
        {
            var wood = Lit("Batte bois", new Color(0.33f, 0.18f, 0.09f), 0f, 0.55f);
            var brass = Lit("Batte laiton", new Color(0.8f, 0.6f, 0.26f), 1f, 0.62f);
            var copper = Lit("Batte cuivre", new Color(0.74f, 0.37f, 0.2f), 1f, 0.55f);
            var leather = Lit("Batte cuir", new Color(0.3f, 0.17f, 0.09f), 0f, 0.25f);
            var dial = Lit("Batte cadran", new Color(0.93f, 0.87f, 0.7f), 0f, 0.35f);
            var red = Lit("Batte rouge", new Color(0.55f, 0.07f, 0.04f), 0.3f, 0.45f);
            var glass = Glass("Batte verre");
            var furnace = Lit("Batte foyer", new Color(1f, 0.45f, 0.1f), 0f, 0.3f, new Color(1f, 0.42f, 0.08f) * 3f);

            var body = new Builder(k_BodyMaterials);
            var conduits = new Builder(1);
            var glow = new Builder(1);
            var gear = new Builder(1);
            var piston = new Builder(1);
            var needle = new Builder(2);
            var vents = BuildBody(body, conduits, glow);
            var gearPose = BuildGear(body, gear);
            var pistonPose = BuildPiston(body, piston);
            var needlePose = BuildNeedle(body, needle);
            var leak = LeakPose();

            var root = new GameObject("Batte");
            try
            {
                AddPart(root.transform, "Modele", Pose.identity, SaveMesh("Batte_Maillage", body),
                        new[] { wood, brass, copper, leather, dial, red, glass });
                AddPart(root.transform, "Conduits", Pose.identity, SaveMesh("Batte_Conduits", conduits), new[] { steam });
                AddPart(root.transform, "Foyer", Pose.identity, SaveMesh("Batte_Foyer", glow), new[] { furnace });
                AddPart(root.transform, k_Gear, gearPose, SaveMesh("Batte_Engrenage", gear), new[] { brass });
                AddPart(root.transform, "Piston", pistonPose, SaveMesh("Batte_Piston", piston), new[] { copper });
                AddPart(root.transform, "Aiguille", needlePose, SaveMesh("Batte_Aiguille", needle), new[] { red, brass });

                AddMarker(root.transform, "Bout", new Pose(new Vector3(0f, k_TipDistance, 0f), Quaternion.identity));
                foreach (var vent in vents)
                    AddMarker(root.transform, k_Vent, vent);
                AddMarker(root.transform, "Fuite", leak);
                return PrefabUtility.SaveAsPrefabAsset(root, k_Model);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static void AddPart(Transform parent, string name, Pose pose, Mesh mesh, Material[] materials)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = pose.position;
            part.transform.localRotation = pose.rotation;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        static void AddMarker(Transform parent, string name, Pose pose)
        {
            var marker = new GameObject(name).transform;
            marker.SetParent(parent, false);
            marker.localPosition = pose.position;
            marker.localRotation = pose.rotation;
        }

        static Mesh SaveMesh(string name, Builder builder)
        {
            var path = $"{k_Folder}/{name}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = new Mesh { name = name };
                AssetDatabase.CreateAsset(mesh, path);
            }

            builder.Fill(mesh);
            EditorUtility.SetDirty(mesh);
            return mesh;
        }

        // Le corps : le pommeau, la poignée de cuir enroulé, le tonneau et son bout, le collier, les cerclages et leurs rivets, les
        // tuyaux de cuivre, les tubes de verre (la vapeur qui y coule va dans « conduits »), le manomètre, le volant, le cadre du
        // foyer (sa lueur va dans « glow »). Renvoie les bouches de vapeur.
        static List<Pose> BuildBody(Builder body, Builder conduits, Builder glow)
        {
            body.Lathe(k_Knob, k_Sides, Matrix4x4.identity, _ => k_Brass);
            var grip = new List<Vector2>();
            var turns = Mathf.RoundToInt((k_GripTop - k_GripBottom) / 0.008f);
            for (var i = 0; i <= turns; i++)
                grip.Add(new Vector2(Mathf.Lerp(k_GripBottom, k_GripTop, i / (float)turns), i % 2 == 0 ? 0.0135f : 0.0148f));
            body.Lathe(grip.ToArray(), k_Sides, Matrix4x4.identity, _ => k_Leather);
            body.Lathe(k_Barrel, k_Sides, Matrix4x4.identity, middle => middle > k_BrassTop ? k_Brass : k_Wood);

            Band(body, k_CollarHeight, k_CollarRadius, 0.012f);
            foreach (var height in k_Bands)
            {
                var radius = RadiusAt(height) + 0.004f;
                Band(body, height, radius, 0.009f);
                for (var i = 0; i < k_RivetsPerBand; i++)
                {
                    var outward = Around((i + 0.5f) * 360f / k_RivetsPerBand);
                    Dome(body, new Vector3(0f, height, 0f) + outward * radius, outward, 0.0032f, k_Brass);
                }
            }

            var vents = new List<Pose>();
            foreach (var angle in k_PipeAngles)
                vents.Add(Pipe(body, angle));
            foreach (var angle in k_GlassAngles)
                GlassConduit(body, conduits, angle);

            Gauge(body);
            Valve(body);
            Furnace(body, glow);
            return vents;
        }

        static Vector3 Around(float degrees)
        {
            var radians = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
        }

        // Le rayon du tonneau à cette hauteur.
        static float RadiusAt(float height)
        {
            for (var i = 0; i < k_Barrel.Length - 1; i++)
            {
                if (height <= k_Barrel[i + 1].x)
                    return Mathf.Lerp(k_Barrel[i].y, k_Barrel[i + 1].y, Mathf.InverseLerp(k_Barrel[i].x, k_Barrel[i + 1].x, height));
            }

            return k_Barrel[k_Barrel.Length - 1].y;
        }

        // Un anneau de laiton autour de la batte (sa face intérieure est cachée dans le bois).
        static void Band(Builder builder, float height, float radius, float halfHeight)
        {
            var inner = radius - 0.006f;
            builder.Lathe(new[]
            {
                new Vector2(height - halfHeight, inner),
                new Vector2(height - halfHeight, radius),
                new Vector2(height + halfHeight, radius),
                new Vector2(height + halfHeight, inner),
            }, k_Sides, Matrix4x4.identity, _ => k_Brass);
        }

        // Un anneau autour d'un axe (collier de tuyau, bague de tube) : de rayon inner à outer, sur ±half le long de l'axe.
        static void Ring(Builder builder, Vector3 center, Vector3 axis, float inner, float outer, float half, int material)
        {
            builder.Lathe(new[]
            {
                new Vector2(-half, inner),
                new Vector2(-half, outer),
                new Vector2(half, outer),
                new Vector2(half, inner),
            }, 10, Matrix4x4.TRS(center, Quaternion.FromToRotation(Vector3.up, axis), Vector3.one), _ => material);
        }

        // Une demi-sphère (rivet, moyeu, bouchon) posée en center, tournée vers outward.
        static void Dome(Builder builder, Vector3 center, Vector3 outward, float radius, int material)
        {
            var profile = new Vector2[4];
            for (var i = 0; i < profile.Length; i++)
            {
                var angle = i / (float)(profile.Length - 1) * Mathf.PI * 0.5f;
                profile[i] = new Vector2(Mathf.Sin(angle) * radius, Mathf.Cos(angle) * radius);
            }

            builder.Lathe(profile, 8, Matrix4x4.TRS(center, Quaternion.FromToRotation(Vector3.up, outward), Vector3.one), _ => material);
        }

        static Pose Pipe(Builder builder, float angle)
        {
            var outward = Around(angle);
            var path = new List<Vector3>
            {
                outward * 0.018f + Vector3.up * k_CollarHeight,
                outward * 0.028f + Vector3.up * 0.2f,
            };
            for (var height = 0.25f; height <= 0.601f; height += 0.05f)
                path.Add(outward * Mathf.Max(0.029f, RadiusAt(height) + 0.0095f) + Vector3.up * height);
            path.Add(outward * 0.049f + Vector3.up * 0.628f);
            path.Add(outward * 0.058f + Vector3.up * 0.648f);
            path.Add(outward * 0.066f + Vector3.up * 0.664f);
            builder.Tube(path, k_PipeRadius, 8, k_Copper);

            // Des colliers de laiton qui tiennent le tuyau, chacun vissé au bois par une patte, et la cheminée évasée au bout.
            for (var i = 3; i < path.Count - 3; i += 2)
            {
                var along = (path[i + 1] - path[i - 1]).normalized;
                Ring(builder, path[i], along, 0.0045f, 0.0062f, 0.003f, k_Brass);
                var height = path[i].y;
                var surface = RadiusAt(height) - 0.002f;
                var reach = Vector3.Dot(path[i], outward);
                builder.Box(outward * ((surface + reach) * 0.5f) + Vector3.up * height, new Vector3(0.004f, 0.006f, reach - surface),
                            Quaternion.LookRotation(outward, Vector3.up), k_Brass);
            }

            var end = path[path.Count - 1];
            var direction = (end - path[path.Count - 2]).normalized;
            builder.Lathe(new[]
            {
                new Vector2(-0.004f, 0.0055f),
                new Vector2(0.006f, 0.0058f),
                new Vector2(0.016f, 0.0085f),
                new Vector2(0.018f, 0.0085f),
                new Vector2(0.018f, 0.0065f),
                new Vector2(0.008f, 0.0045f),
            }, 8, Matrix4x4.TRS(end, Quaternion.FromToRotation(Vector3.up, direction), Vector3.one), _ => k_Brass);
            return new Pose(end + direction * 0.018f, Quaternion.LookRotation(direction, Vector3.up));
        }

        // Un tube de verre à demi enfoncé dans le bois, le long de la batte, avec la vapeur dedans (un tube plus fin, dans
        // « conduits », dont la texture défile en jeu), une bague et un bouchon de laiton à chaque bout.
        static void GlassConduit(Builder body, Builder conduits, float angle)
        {
            var outward = Around(angle);
            var path = new List<Vector3>();
            for (var height = k_GlassFrom; height <= k_GlassTo + 0.001f; height += 0.04f)
                path.Add(outward * (RadiusAt(height) + 0.002f) + Vector3.up * height);
            body.Tube(path, k_GlassRadius, 10, k_Glass);
            conduits.Tube(path, k_SteamRadius, 8, 0, 10f);

            for (var end = 0; end < 2; end++)
            {
                var point = end == 0 ? path[0] : path[path.Count - 1];
                var direction = end == 0 ? (path[0] - path[1]).normalized : (path[path.Count - 1] - path[path.Count - 2]).normalized;
                Ring(body, point, direction, k_GlassRadius - 0.001f, k_GlassRadius + 0.0015f, 0.004f, k_Brass);
                Dome(body, point + direction * 0.003f, direction, k_GlassRadius + 0.0005f, k_Brass);
            }
        }

        // Le manomètre, à l'avant : un boîtier de laiton, un cadran crème gradué (des traits sombres, puis rouges dans la zone
        // rouge) ; l'aiguille est une pièce à part.
        static void Gauge(Builder body)
        {
            var outward = Around(90f);
            var center = GaugeCenter();
            var toOutward = Matrix4x4.TRS(center, Quaternion.FromToRotation(Vector3.up, outward), Vector3.one);
            body.Lathe(new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 0.016f),
                new Vector2(0.012f, 0.016f),
                new Vector2(0.012f, 0.0135f),
                new Vector2(0.009f, 0.0135f),
            }, k_Sides, toOutward, _ => k_Brass);
            body.Lathe(new[] { new Vector2(0.009f, 0.0135f), new Vector2(0.009f, 0f) }, k_Sides, toOutward, _ => k_Dial);

            var rotation = Quaternion.FromToRotation(Vector3.up, outward);
            for (var tick = 0; tick <= 10; tick++)
            {
                var dialAngle = Mathf.Lerp(k_DialFrom, k_DialTo, tick / 10f);
                var direction = rotation * (Quaternion.Euler(0f, dialAngle, 0f) * Vector3.back);
                var material = dialAngle >= k_DialRed - 0.1f ? k_Red : k_Leather;
                var length = dialAngle >= k_DialRed - 0.1f ? 0.004f : 0.0025f;
                body.Box(center + outward * 0.0092f + direction * (0.0115f - length * 0.5f), new Vector3(0.0012f, length, 0.0006f),
                         Quaternion.LookRotation(outward, direction), material);
            }
        }

        static Vector3 GaugeCenter() => new Vector3(0f, k_GaugeHeight, 0f) + Around(90f) * (RadiusAt(k_GaugeHeight) - 0.002f);

        // L'aiguille : son pivot au centre du cadran, son Y local vers l'avant du manomètre ; elle pointe vers -Z (le haut du cadran)
        // et tourne autour de Y en jeu.
        static Pose BuildNeedle(Builder body, Builder needle)
        {
            var outward = Around(90f);
            var pivot = GaugeCenter() + outward * 0.0098f;
            needle.Box(new Vector3(0f, 0f, -0.0042f), new Vector3(0.0016f, 0.0008f, 0.0105f), Quaternion.identity, 0);
            Dome(needle, Vector3.zero, Vector3.up, 0.0018f, 1);
            return new Pose(pivot, Quaternion.FromToRotation(Vector3.up, outward));
        }

        // Le volant de la vanne, à l'arrière : une tige de cuivre, une roue rouge à deux rayons de laiton.
        static void Valve(Builder builder)
        {
            var outward = Around(270f);
            var surface = new Vector3(0f, k_ValveHeight, 0f) + outward * (RadiusAt(k_ValveHeight) - 0.002f);
            var hub = surface + outward * 0.014f;
            builder.Tube(new[] { surface, hub }, 0.0025f, 8, k_Copper);

            var wheel = new Vector2[9];
            for (var i = 0; i < wheel.Length; i++)
            {
                var t = i / (float)(wheel.Length - 1) * Mathf.PI * 2f;
                wheel[i] = new Vector2(Mathf.Sin(t) * 0.0022f, 0.011f + Mathf.Cos(t) * 0.0022f);
            }

            builder.Lathe(wheel, 12, Matrix4x4.TRS(hub, Quaternion.FromToRotation(Vector3.up, outward), Vector3.one), _ => k_Red, true);
            var rotation = Quaternion.LookRotation(outward, Vector3.up);
            builder.Box(hub, new Vector3(0.022f, 0.0022f, 0.0018f), rotation, k_Brass);
            builder.Box(hub, new Vector3(0.0022f, 0.022f, 0.0018f), rotation, k_Brass);
            Dome(builder, hub, outward, 0.003f, k_Brass);
        }

        // La fuite de vapeur : entre la tige et le volant, vers le haut et l'extérieur.
        static Pose LeakPose()
        {
            var outward = Around(270f);
            var point = new Vector3(0f, k_ValveHeight, 0f) + outward * (RadiusAt(k_ValveHeight) + 0.006f);
            return new Pose(point, Quaternion.LookRotation((outward + Vector3.up).normalized, Vector3.up));
        }

        // La fenêtre du foyer, sur le côté : un cadre et deux barreaux de laiton ; la lueur orange à part (« glow »).
        static void Furnace(Builder body, Builder glow)
        {
            var outward = Around(0f);
            var surface = new Vector3(0f, k_FurnaceHeight, 0f) + outward * RadiusAt(k_FurnaceHeight);
            var rotation = Quaternion.LookRotation(outward, Vector3.up);
            body.Box(surface + outward * 0.0005f, new Vector3(0.02f, 0.042f, 0.007f), rotation, k_Brass);
            glow.Box(surface + outward * 0.0035f, new Vector3(0.012f, 0.032f, 0.002f), rotation, 0);
            for (var i = -1; i <= 1; i += 2)
                body.Box(surface + outward * 0.0048f + Vector3.up * (0.006f * i), new Vector3(0.014f, 0.0018f, 0.0014f), rotation, k_Brass);
        }

        // L'engrenage : son axe (dans le corps), et la roue dentée (pièce à part, son pivot au centre, son Y local sur l'axe).
        static Pose BuildGear(Builder body, Builder gear)
        {
            var outward = Around(k_GearAngle);
            var surface = new Vector3(0f, k_GearHeight, 0f) + outward * (RadiusAt(k_GearHeight) - 0.001f);
            var center = surface + outward * 0.006f;
            body.Tube(new[] { surface, center + outward * 0.002f }, 0.002f, 8, k_Copper);

            gear.Lathe(new[]
            {
                new Vector2(-0.002f, 0f),
                new Vector2(-0.002f, k_GearRadius),
                new Vector2(0.002f, k_GearRadius),
                new Vector2(0.002f, 0f),
            }, 20, Matrix4x4.identity, _ => 0);
            for (var i = 0; i < k_GearTeeth; i++)
            {
                var direction = Quaternion.Euler(0f, i * 360f / k_GearTeeth, 0f) * Vector3.forward;
                gear.Box(direction * (k_GearRadius + 0.0016f), new Vector3(0.0035f, 0.0036f, 0.0034f), Quaternion.LookRotation(direction, Vector3.up), 0);
            }

            Dome(gear, Vector3.up * 0.002f, Vector3.up, 0.003f, 0);
            return new Pose(center, Quaternion.FromToRotation(Vector3.up, outward));
        }

        // Le piston : son cylindre de laiton (dans le corps), et la tige (pièce à part, son pivot en bas, son Y local le long de la
        // batte) qui sort du haut du cylindre et pompe en jeu.
        static Pose BuildPiston(Builder body, Builder piston)
        {
            var outward = Around(k_PistonAngle);
            var middle = (k_PistonFrom + k_PistonTo) * 0.5f;
            var axis = outward * (RadiusAt(middle) + 0.0075f);
            body.Lathe(new[]
            {
                new Vector2(k_PistonFrom, 0f),
                new Vector2(k_PistonFrom, 0.0072f),
                new Vector2(k_PistonFrom + 0.004f, 0.0072f),
                new Vector2(k_PistonFrom + 0.004f, 0.0062f),
                new Vector2(k_PistonTo - 0.004f, 0.0062f),
                new Vector2(k_PistonTo - 0.004f, 0.0072f),
                new Vector2(k_PistonTo, 0.0072f),
                new Vector2(k_PistonTo, 0f),
            }, 12, Matrix4x4.Translate(axis), _ => k_Brass);
            body.Box(outward * ((RadiusAt(middle) + axis.magnitude) * 0.5f) + Vector3.up * middle,
                     new Vector3(0.004f, 0.03f, axis.magnitude - RadiusAt(middle) + 0.002f), Quaternion.LookRotation(outward, Vector3.up), k_Brass);

            piston.Lathe(new[]
            {
                new Vector2(-0.01f, 0f),
                new Vector2(-0.01f, 0.0026f),
                new Vector2(0.038f, 0.0026f),
                new Vector2(0.038f, 0f),
            }, 8, Matrix4x4.identity, _ => 0);
            Dome(piston, Vector3.up * 0.038f, Vector3.up, 0.004f, 0);
            return new Pose(axis + Vector3.up * k_PistonTo, Quaternion.identity);
        }

        // ---------------------------------------------------------------- les formes

        // Un maillage à plusieurs matériaux, fait de formes simples posées les unes après les autres (avec leurs coordonnées de
        // texture : autour et le long d'un tour ou d'un tube).
        sealed class Builder
        {
            readonly List<Vector3> m_Vertices = new List<Vector3>();
            readonly List<Vector3> m_Normals = new List<Vector3>();
            readonly List<Vector2> m_UVs = new List<Vector2>();
            readonly List<int>[] m_Triangles;

            public Builder(int materials)
            {
                m_Triangles = new List<int>[materials];
                for (var i = 0; i < materials; i++)
                    m_Triangles[i] = new List<int>();
            }

            // Un tour de profil (hauteur, rayon) autour de l'axe Y, placé par matrix ; material choisit le matériau de chaque
            // bande selon sa hauteur moyenne. Fermé : le profil est une boucle (son dernier point est le premier), comme un
            // anneau. La normale suit la pente du profil (un profil parcouru vers le haut, ou une boucle dans le sens direct,
            // regarde vers l'extérieur).
            public void Lathe(Vector2[] profile, int sides, Matrix4x4 matrix, Func<float, int> material, bool closed = false)
            {
                var start = m_Vertices.Count;
                var count = profile.Length;
                for (var i = 0; i < count; i++)
                {
                    var previous = closed ? (i == 0 ? count - 2 : i - 1) : Mathf.Max(i - 1, 0);
                    var next = closed ? (i == count - 1 ? 1 : i + 1) : Mathf.Min(i + 1, count - 1);
                    var slope = profile[next] - profile[previous];
                    var outward = new Vector2(slope.x, -slope.y).normalized;
                    for (var side = 0; side <= sides; side++)
                    {
                        var angle = side * Mathf.PI * 2f / sides;
                        var cos = Mathf.Cos(angle);
                        var sin = Mathf.Sin(angle);
                        m_Vertices.Add(matrix.MultiplyPoint3x4(new Vector3(cos * profile[i].y, profile[i].x, sin * profile[i].y)));
                        m_Normals.Add(matrix.MultiplyVector(new Vector3(cos * outward.x, outward.y, sin * outward.x)).normalized);
                        m_UVs.Add(new Vector2(side / (float)sides, i / (float)Mathf.Max(1, count - 1)));
                    }
                }

                var ring = sides + 1;
                for (var i = 0; i < count - 1; i++)
                {
                    var list = m_Triangles[material((profile[i].x + profile[i + 1].x) * 0.5f)];
                    for (var side = 0; side < sides; side++)
                    {
                        var a = start + i * ring + side;
                        var b = a + 1;
                        var c = a + ring;
                        var d = c + 1;
                        list.AddRange(new[] { a, c, b, b, c, d });
                    }
                }
            }

            // Un tube le long d'un chemin (sans bouchons : ses bouts entrent dans une autre forme). V de la texture : la longueur
            // parcourue, fois uvPerMeter.
            public void Tube(IList<Vector3> path, float radius, int sides, int material, float uvPerMeter = 1f)
            {
                var start = m_Vertices.Count;
                var normal = Vector3.zero;
                var length = 0f;
                for (var i = 0; i < path.Count; i++)
                {
                    var tangent = (path[Mathf.Min(i + 1, path.Count - 1)] - path[Mathf.Max(i - 1, 0)]).normalized;
                    if (i > 0)
                        length += Vector3.Distance(path[i], path[i - 1]);

                    // Le repère glisse le long du chemin sans tourner sur lui-même.
                    normal = i == 0 ? Vector3.Cross(tangent, Mathf.Abs(tangent.y) < 0.9f ? Vector3.up : Vector3.right).normalized
                        : Vector3.ProjectOnPlane(normal, tangent).normalized;
                    var binormal = Vector3.Cross(normal, tangent);
                    for (var side = 0; side <= sides; side++)
                    {
                        var angle = side * Mathf.PI * 2f / sides;
                        var direction = normal * Mathf.Cos(angle) + binormal * Mathf.Sin(angle);
                        m_Vertices.Add(path[i] + direction * radius);
                        m_Normals.Add(direction);
                        m_UVs.Add(new Vector2(side / (float)sides, length * uvPerMeter));
                    }
                }

                var ring = sides + 1;
                var list = m_Triangles[material];
                for (var i = 0; i < path.Count - 1; i++)
                {
                    for (var side = 0; side < sides; side++)
                    {
                        var a = start + i * ring + side;
                        var b = a + 1;
                        var c = a + ring;
                        var d = c + 1;
                        list.AddRange(new[] { a, c, b, b, c, d });
                    }
                }
            }

            // Une boîte (size : sa taille dans son repère, tourné de rotation).
            public void Box(Vector3 center, Vector3 size, Quaternion rotation, int material)
            {
                var half = size * 0.5f;
                Face(center, rotation * Vector3.right * half.x, rotation * Vector3.up * half.y, rotation * Vector3.forward * half.z, material);
                Face(center, -(rotation * Vector3.right) * half.x, rotation * Vector3.up * half.y, rotation * Vector3.forward * half.z, material);
                Face(center, rotation * Vector3.up * half.y, rotation * Vector3.forward * half.z, rotation * Vector3.right * half.x, material);
                Face(center, -(rotation * Vector3.up) * half.y, rotation * Vector3.forward * half.z, rotation * Vector3.right * half.x, material);
                Face(center, rotation * Vector3.forward * half.z, rotation * Vector3.right * half.x, rotation * Vector3.up * half.y, material);
                Face(center, -(rotation * Vector3.forward) * half.z, rotation * Vector3.right * half.x, rotation * Vector3.up * half.y, material);
            }

            // Une face de boîte : out va du centre à la face (sa normale), u et w sont ses deux demi-côtés.
            void Face(Vector3 center, Vector3 @out, Vector3 u, Vector3 w, int material)
            {
                var normal = @out.normalized;
                var v = Vector3.Cross(u, normal).normalized * w.magnitude;
                var c = center + @out;
                var start = m_Vertices.Count;
                m_Vertices.Add(c - u - v);
                m_Vertices.Add(c - u + v);
                m_Vertices.Add(c + u + v);
                m_Vertices.Add(c + u - v);
                for (var i = 0; i < 4; i++)
                    m_Normals.Add(normal);
                m_UVs.Add(new Vector2(0f, 0f));
                m_UVs.Add(new Vector2(0f, 1f));
                m_UVs.Add(new Vector2(1f, 1f));
                m_UVs.Add(new Vector2(1f, 0f));
                m_Triangles[material].AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
            }

            public void Fill(Mesh mesh)
            {
                mesh.Clear();
                mesh.indexFormat = m_Vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
                mesh.SetVertices(m_Vertices);
                mesh.SetNormals(m_Normals);
                mesh.SetUVs(0, m_UVs);
                mesh.subMeshCount = m_Triangles.Length;
                for (var i = 0; i < m_Triangles.Length; i++)
                    mesh.SetTriangles(m_Triangles[i], i);
                mesh.RecalculateBounds();
            }
        }

        // ---------------------------------------------------------------- les matériaux

        static Material Lit(string name, Color color, float metallic, float smoothness, Color? emission = null)
        {
            var path = $"{k_Folder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // Le verre des tubes : transparent, presque incolore (un peu bleuté), très lisse.
        static Material Glass(string name)
        {
            var path = $"{k_Folder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            material.SetColor("_BaseColor", new Color(0.8f, 0.92f, 1f, 0.22f));
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", 0.95f);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // La vapeur des conduits : la fumée de la grenade (des particules transparentes, sans lumière), avec une texture de
        // vapeur faite ici, qui se répète tous les 10 cm le long du tube et défile en jeu.
        static Material SteamMaterial()
        {
            var path = $"{k_Folder}/Batte vapeur.mat";
            var texture = SteamTexture();
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<Material>(k_SteamMaterial);
                material = source != null ? new Material(source) : new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                material.name = "Batte vapeur";
                material.SetColor("_BaseColor", new Color(1f, 0.97f, 0.92f, 0.8f));
                AssetDatabase.CreateAsset(material, path);
            }

            if (texture != null && material.GetTexture("_BaseMap") != texture)
            {
                material.SetTexture("_BaseMap", texture);
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        // Une texture de vapeur qui se raccorde dans les deux sens : des volutes blanches (un bruit de valeurs sur une grille qui
        // boucle, étiré le long du tube), transparentes entre elles.
        static Texture2D SteamTexture()
        {
            const int width = 32;
            const int height = 128;
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(k_SteamTexture) == null)
            {
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                var random = new System.Random(1311);
                var grids = new[] { Grid(random, 4, 8), Grid(random, 8, 16), Grid(random, 16, 32) };
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var u = x / (float)width;
                        var v = y / (float)height;
                        var noise = 0.55f * Sample(grids[0], u, v) + 0.3f * Sample(grids[1], u, v) + 0.15f * Sample(grids[2], u, v);
                        var alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.38f, 0.78f, noise));
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }

                texture.Apply();
                File.WriteAllBytes(k_SteamTexture, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(k_SteamTexture);
                if (AssetImporter.GetAtPath(k_SteamTexture) is TextureImporter importer)
                {
                    importer.wrapMode = TextureWrapMode.Repeat;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = true;
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(k_SteamTexture);
        }

        static float[,] Grid(System.Random random, int columns, int rows)
        {
            var grid = new float[columns, rows];
            for (var x = 0; x < columns; x++)
            {
                for (var y = 0; y < rows; y++)
                    grid[x, y] = (float)random.NextDouble();
            }

            return grid;
        }

        // Le bruit de la grille en (u, v), lissé entre ses points, la grille bouclant sur ses bords (la texture se raccorde).
        static float Sample(float[,] grid, float u, float v)
        {
            var columns = grid.GetLength(0);
            var rows = grid.GetLength(1);
            var x = u * columns;
            var y = v * rows;
            var x0 = Mathf.FloorToInt(x);
            var y0 = Mathf.FloorToInt(y);
            var tx = Mathf.SmoothStep(0f, 1f, x - x0);
            var ty = Mathf.SmoothStep(0f, 1f, y - y0);
            float At(int i, int j) => grid[((i % columns) + columns) % columns, ((j % rows) + rows) % rows];
            return Mathf.Lerp(Mathf.Lerp(At(x0, y0), At(x0 + 1, y0), tx), Mathf.Lerp(At(x0, y0 + 1), At(x0 + 1, y0 + 1), tx), ty);
        }

        // L'anneau du verrou : la lueur additive des traînées (Archery/Glow : la couleur vient des sommets de la ligne).
        static Material LockMaterial()
        {
            var path = $"{k_Folder}/Batte verrou.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(k_GlowShader);
            if (shader == null)
                return null;

            material = new Material(shader) { name = "Batte verrou" };
            material.SetColor("_Color", Color.white);
            material.SetFloat("_Intensity", 2.2f);
            material.SetFloat("_Softness", 0.35f);
            material.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // L'aperçu du menu des armes : la batte debout, un peu penchée, comme le katana ; tournée pour montrer le manomètre.
        static BowVisual BuildVisual(GameObject bat)
        {
            var root = new GameObject("Bow Visual Batte");
            try
            {
                root.AddComponent<BowVisual>();
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(bat, root.transform);
                instance.transform.localPosition = new Vector3(0f, -0.3f, 0f);
                instance.transform.localRotation = Quaternion.Euler(0f, 180f, -12f);
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, k_Visual);
                return prefab != null ? prefab.GetComponent<BowVisual>() : null;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ---------------------------------------------------------------- liste des armes

        static bool InLoadout(BowDefinition definition)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(k_Loadout);
            var loadout = prefab != null ? prefab.GetComponentInChildren<BowLoadout>(true) : null;
            return loadout != null && loadout.Entries.Any(entry => entry != null && entry.bow == definition);
        }

        // La carte de la batte à la fin de la liste des armes, sans succès à remplir (une ligne de plus, rien d'autre).
        static void AddToLoadout(BowDefinition definition)
        {
            var root = PrefabUtility.LoadPrefabContents(k_Loadout);
            try
            {
                var loadout = root.GetComponentInChildren<BowLoadout>(true);
                if (loadout == null || loadout.Entries.Any(entry => entry != null && entry.bow == definition))
                    return;

                var settings = new SerializedObject(loadout);
                var entries = settings.FindProperty("m_Entries");
                entries.arraySize++;
                var entry = entries.GetArrayElementAtIndex(entries.arraySize - 1);
                entry.FindPropertyRelative("bow").objectReferenceValue = definition;
                entry.FindPropertyRelative("unlockedBy").objectReferenceValue = null;
                entry.FindPropertyRelative("color").colorValue = k_CardColor;
                entry.FindPropertyRelative("comingSoonName").stringValue = "";
                settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, k_Loadout);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
