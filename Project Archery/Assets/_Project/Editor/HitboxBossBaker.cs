using System.Collections.Generic;
using Archery.Combat;
using Archery.Enemies;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le 12 novembre au soir, après une courte partie du joueur (<c>Docs/Refonte_TowerDefense.md</c>, 12.75). Chaque prefab ou
    /// réglage n'est changé que s'il a encore sa valeur d'avant : une seule fois, et jamais un réglage retouché à la main.
    /// <list type="bullet">
    /// <item>« il faudrait vraiment retravailler la hitbox des petits ennemis comme le slime ou le champignon ainsi que le soldat de
    /// base : elles sont trop petites, on passe souvent à travers ; et les champignons qui sautillent sont intenables puisque leurs
    /// hitbox ne suivent pas leurs sauts » : les zones du corps et de la tête du Gluant, du Gluant bleu, du Champignon, du Champignon
    /// furieux et du Fantassin, agrandies à la taille de leur modèle (mesurée dans leurs FBX : le chapeau du champignon fait 1,5 m de
    /// large en jeu, ses zones 0,8 m) ; le centre du corps (<i>Center Height</i>) suit ; et la zone du corps des quatre qui sautillent
    /// suit leurs sauts (<i>Body Follows Hops</i>, <c>Enemy.Hop.cs</c>) ;</item>
    /// <item>« les boss font vraiment rien, et la raison est leur vitesse : ils devraient aller plus vite que les ennemis de base » :
    /// la vitesse des cinq boss des vagues (de 1 à 2,3 m/s ; les ennemis au sol, de 1,3 à 3,4, 2,3 en moyenne), et la vitesse de leur
    /// animation de course d'autant (leurs pas suivent).</item>
    /// </list>
    /// </summary>
    [InitializeOnLoad]
    static class HitboxBossBaker
    {
        const string k_Prefabs = "Assets/_Project/Prefabs/";
        const string k_Definitions = "Assets/_Project/Data/Enemies/";

        // La zone du corps (capsule posée sur la racine) : rayon, hauteur, hauteur du centre ; la tête (sphère sur un os) : rayon,
        // dans le repère de l'os ; le centre du corps de l'Enemy. Avant, après.
        struct Sizes
        {
            public string Name;
            public float Radius, NewRadius, Height, NewHeight, Center, NewCenter, Head, NewHead;
            public bool FollowsHops;
        }

        static readonly Sizes[] k_Sizes =
        {
            // Le gluant (1,27 m de large, 1,22 m de haut en jeu) : une boule qui le couvre, du sol au sommet ; avant, plus petite et à
            // moitié sous le sol.
            new Sizes { Name = "Enemy_Gluant", Radius = 0.546f, NewRadius = 0.6f, Height = 1.092f, NewHeight = 1.2f, Center = 0.39f,
                        NewCenter = 0.6f, Head = 0.22f, NewHead = 0.29f, FollowsHops = true },
            new Sizes { Name = "Enemy_GluantBleu", Radius = 0.546f, NewRadius = 0.6f, Height = 1.092f, NewHeight = 1.2f, Center = 0.39f,
                        NewCenter = 0.6f, Head = 0.25f, NewHead = 0.32f, FollowsHops = true },
            // Le champignon (1,48 m de large au chapeau, 1,21 m de haut) : le pied, du sol à 1 m ; le chapeau (la tête) bien plus large.
            new Sizes { Name = "Enemy_Champignon", Radius = 0.364f, NewRadius = 0.5f, Height = 0.728f, NewHeight = 1f, Center = 0.358f,
                        NewCenter = 0.5f, Head = 0.32f, NewHead = 0.45f, FollowsHops = true },
            new Sizes { Name = "Enemy_ChampignonFurieux", Radius = 0.406f, NewRadius = 0.55f, Height = 0.812f, NewHeight = 1.1f,
                        Center = 0.399f, NewCenter = 0.55f, Head = 0.32f, NewHead = 0.45f, FollowsHops = true },
            // Le fantassin (1,19 m de haut, un bouclier et une lance de chaque côté) : un corps plus large, la tête un peu plus grosse
            // (son os est à l'échelle 0,01 : 32 = 30 cm en jeu).
            new Sizes { Name = "Enemy_Fantassin", Radius = 0.304f, NewRadius = 0.4f, Height = 1.188f, NewHeight = 1.25f, Center = 0.594f,
                        NewCenter = 0.625f, Head = 32f, NewHead = 38f, FollowsHops = false },
        };

        // Les boss des vagues : leur vitesse (m/s, avant la difficulté) et celle de leur animation de course (0,35 pour tous avant :
        // elle suit la vitesse, pour que leurs pas ne glissent pas).
        static readonly (string Name, float Old, float New, float Animation)[] k_Bosses =
        {
            ("Boss_Champignon", 1.5f, 3f, 0.7f), // Thomas (le champignon : 2 et 2,7)
            ("Boss_Mimique", 2.3f, 3.4f, 0.52f), // Jean (la mimique : 3)
            ("Boss_Tortue", 1f, 2.6f, 0.91f), // Gontrand (la tortue : 1,3)
            ("Boss_ChefOrc", 1.7f, 3f, 0.62f), // Francine (l'orc : 2,1)
            ("Boss_Liche", 1.2f, 2.4f, 0.7f), // Nolan, qui tire de loin (la liche : 1,8)
        };

        const float k_OldBossAnimation = 0.35f;

        static HitboxBossBaker()
        {
            EditorApplication.delayCall -= Later;
            EditorApplication.delayCall += Later;
        }

        static void Later()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            var log = new List<string>();
            foreach (var sizes in k_Sizes)
                UpdateHitboxes(sizes, log);
            foreach (var boss in k_Bosses)
                UpdateBoss(boss.Name, boss.Old, boss.New, boss.Animation, log);
            if (log.Count == 0)
                return;

            AssetDatabase.SaveAssets();
            Debug.Log($"Les petits ennemis et les boss (12 novembre, le soir) : {log.Count} réglage(s) changé(s) :\n" + string.Join("\n", log));
        }

        static void UpdateHitboxes(Sizes sizes, List<string> log)
        {
            var path = k_Prefabs + sizes.Name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                return;

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                CapsuleCollider body = null;
                SphereCollider head = null;
                foreach (var hitbox in root.GetComponentsInChildren<Hitbox>(true))
                {
                    if (hitbox.Zone == HitZone.Body && hitbox.transform.parent == root.transform && body == null)
                        body = hitbox.GetComponent<CapsuleCollider>();
                    else if (hitbox.Zone == HitZone.Head && head == null)
                        head = hitbox.GetComponent<SphereCollider>();
                }

                var enemy = root.GetComponent<Enemy>();
                if (body == null || head == null || enemy == null)
                    return;
                // La garde : la capsule du corps a encore ses mesures d'avant (le reste change avec elle).
                if (!Mathf.Approximately(body.radius, sizes.Radius) || !Mathf.Approximately(body.height, sizes.Height) ||
                    !Mathf.Approximately(body.center.y, sizes.Center))
                    return;

                var bodyObject = new SerializedObject(body);
                bodyObject.FindProperty("m_Radius").floatValue = sizes.NewRadius;
                bodyObject.FindProperty("m_Height").floatValue = sizes.NewHeight;
                bodyObject.FindProperty("m_Center").vector3Value = new Vector3(body.center.x, sizes.NewCenter, body.center.z);
                bodyObject.ApplyModifiedPropertiesWithoutUndo();

                var headText = "";
                if (Mathf.Approximately(head.radius, sizes.Head))
                {
                    var headObject = new SerializedObject(head);
                    headObject.FindProperty("m_Radius").floatValue = sizes.NewHead;
                    headObject.ApplyModifiedPropertiesWithoutUndo();
                    headText = $", tête {sizes.Head:0.##} → {sizes.NewHead:0.##}";
                }

                var enemyObject = new SerializedObject(enemy);
                var center = enemyObject.FindProperty("m_CenterHeight");
                if (center != null && Mathf.Approximately(center.floatValue, sizes.Center))
                    center.floatValue = sizes.NewCenter;
                var follows = enemyObject.FindProperty("m_BodyFollowsHops");
                if (follows != null && sizes.FollowsHops)
                    follows.boolValue = true;
                enemyObject.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, path);
                log.Add($"{sizes.Name} : corps {sizes.Radius:0.###} × {sizes.Height:0.###} (centre {sizes.Center:0.###}) → " +
                        $"{sizes.NewRadius:0.##} × {sizes.NewHeight:0.##} (centre {sizes.NewCenter:0.###}){headText}" +
                        (sizes.FollowsHops ? " ; le corps suit ses sauts" : ""));
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void UpdateBoss(string name, float old, float speed, float animation, List<string> log)
        {
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(k_Definitions + name + ".asset");
            if (definition == null || !Mathf.Approximately(definition.moveSpeed, old))
                return;

            // L'animation d'abord (le prefab), puis la vitesse (la garde du tout).
            var path = k_Prefabs + name + ".prefab";
            var animationText = "";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var boss = root.GetComponent<Boss>();
                    var property = boss != null ? new SerializedObject(boss).FindProperty("m_RunAnimationSpeed") : null;
                    if (property != null && Mathf.Approximately(property.floatValue, k_OldBossAnimation))
                    {
                        property.floatValue = animation;
                        property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        animationText = $", animation de course {k_OldBossAnimation:0.##} → {animation:0.##}";
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("moveSpeed").floatValue = speed;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            log.Add($"{name} ({definition.displayName}) : vitesse {old:0.#} → {speed:0.#} m/s{animationText}");
        }
    }
}
