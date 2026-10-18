using System.Collections.Generic;
using Archery.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.World
{
    /// <summary>
    /// Des nuages en blocs, à la Minecraft : une couche de pavés plats, haut au-dessus de la carte, qui dérive avec le vent
    /// (<see cref="Wind"/>) et prend la couleur du ciel de la difficulté (blancs à midi, roses au coucher du soleil,
    /// gris-bleu la nuit, rougeâtres sous le ciel rouge).
    /// </summary>
    /// <remarks>
    /// Créée au lancement par le <see cref="SkyController"/> : rien à poser dans la scène. Le motif se répète tous les
    /// <see cref="m_Period"/> pavés ; la couche se recale autour du joueur sans que ça se voie, et s'efface au loin
    /// (shader Archery/Clouds, matériau Resources/Clouds) : le ciel n'a pas de bord.
    /// </remarks>
    public class CloudLayer : MonoBehaviour
    {
        const string k_MaterialResource = "Clouds";
        static readonly int k_TopColorId = Shader.PropertyToID("_TopColor");

        [Tooltip("Hauteur (m) du dessous des nuages.")]
        [SerializeField]
        float m_Altitude = 80f;

        [Tooltip("Côté (m) d'un pavé.")]
        [SerializeField]
        float m_CellSize = 10f;

        [Tooltip("Épaisseur (m) des nuages.")]
        [SerializeField]
        float m_Thickness = 4f;

        [Tooltip("Le motif se répète tous les tant de pavés (multiple de 8).")]
        [SerializeField]
        int m_Period = 48;

        [Tooltip("Part du ciel couverte de nuages.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_Coverage = 0.4f;

        [Tooltip("Vitesse (m/s) à laquelle ils dérivent avec le vent.")]
        [SerializeField]
        float m_Speed = 2f;

        [SerializeField]
        int m_Seed = 7;

        Mesh m_Mesh;
        Material m_Material;
        Vector2 m_Drift;

        /// <summary>Crée la couche de nuages (au lancement du jeu, par le ciel). Null si le matériau Resources/Clouds manque.</summary>
        public static CloudLayer Create()
        {
            var material = Resources.Load<Material>(k_MaterialResource);
            if (material == null)
            {
                Debug.LogWarning("CloudLayer : matériau « Resources/Clouds » introuvable, pas de nuages.");
                return null;
            }

            var go = new GameObject("Nuages");
            var layer = go.AddComponent<CloudLayer>();
            layer.m_Material = new Material(material) { name = "Nuages (en jeu)" };
            layer.Build();
            return layer;
        }

        /// <summary>Les nuages prennent la lumière du ciel : la lumière ambiante d'en haut, plus celle du soleil ou de la lune.</summary>
        public void SetSky(SkySettings sky)
        {
            if (m_Material == null)
                return;

            var color = sky.ambientSky * 1.1f + sky.lightColor * (sky.lightIntensity * 0.7f);
            color.r = Mathf.Min(1f, color.r);
            color.g = Mathf.Min(1f, color.g);
            color.b = Mathf.Min(1f, color.b);
            color.a = 1f;
            m_Material.SetColor(k_TopColorId, color);
        }

        void LateUpdate()
        {
            // Ils dérivent avec le vent ; la couche se recale autour du joueur d'un motif entier, ce qui ne se voit pas.
            m_Drift += Wind.Direction.normalized * (m_Speed * Time.deltaTime);
            var period = m_Period * m_CellSize;
            var viewer = Viewer();
            transform.SetPositionAndRotation(new Vector3(Snap(m_Drift.x, viewer.x, period), m_Altitude, Snap(m_Drift.y, viewer.z, period)), Quaternion.identity);
        }

        // Le maillage couvre deux motifs : décalé d'un nombre entier de motifs, il s'étend toujours d'au moins un demi-motif
        // de chaque côté du joueur.
        static float Snap(float drift, float viewer, float period)
        {
            drift = Mathf.Repeat(drift, period);
            return drift + period * Mathf.Floor((viewer - period * 0.5f - drift) / period);
        }

        static Vector3 Viewer()
        {
            var rig = PlayerRig.Instance;
            if (rig != null && rig.Head != null)
                return rig.Head.position;
            var camera = Camera.main;
            return camera != null ? camera.transform.position : Vector3.zero;
        }

        void OnDestroy()
        {
            if (m_Mesh != null)
                Destroy(m_Mesh);
            if (m_Material != null)
                Destroy(m_Material);
        }

        // ---------------------------------------------------------------- les pavés

        void Build()
        {
            var period = Mathf.Max(8, m_Period / 8 * 8);
            var cloudy = Pattern(period);

            // Deux motifs sur deux, en un seul maillage : seulement les faces qu'on voit (dessus, dessous, et les côtés
            // qui donnent sur le ciel).
            var size = period * 2;
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            var cell = m_CellSize;
            var top = m_Thickness;
            for (var z = 0; z < size; z++)
            {
                for (var x = 0; x < size; x++)
                {
                    if (!cloudy[x % period, z % period])
                        continue;

                    float x0 = x * cell, x1 = (x + 1) * cell, z0 = z * cell, z1 = (z + 1) * cell;
                    AddQuad(vertices, colors, triangles, new Vector3(x0, top, z0), new Vector3(x0, top, z1), new Vector3(x1, top, z1), new Vector3(x1, top, z0), 1f);
                    AddQuad(vertices, colors, triangles, new Vector3(x0, 0f, z0), new Vector3(x1, 0f, z0), new Vector3(x1, 0f, z1), new Vector3(x0, 0f, z1), 0.62f);
                    if (!IsCloudy(cloudy, period, size, x - 1, z))
                        AddQuad(vertices, colors, triangles, new Vector3(x0, 0f, z1), new Vector3(x0, top, z1), new Vector3(x0, top, z0), new Vector3(x0, 0f, z0), 0.82f);
                    if (!IsCloudy(cloudy, period, size, x + 1, z))
                        AddQuad(vertices, colors, triangles, new Vector3(x1, 0f, z0), new Vector3(x1, top, z0), new Vector3(x1, top, z1), new Vector3(x1, 0f, z1), 0.82f);
                    if (!IsCloudy(cloudy, period, size, x, z - 1))
                        AddQuad(vertices, colors, triangles, new Vector3(x0, 0f, z0), new Vector3(x0, top, z0), new Vector3(x1, top, z0), new Vector3(x1, 0f, z0), 0.9f);
                    if (!IsCloudy(cloudy, period, size, x, z + 1))
                        AddQuad(vertices, colors, triangles, new Vector3(x1, 0f, z1), new Vector3(x1, top, z1), new Vector3(x0, top, z1), new Vector3(x0, 0f, z1), 0.9f);
                }
            }

            m_Mesh = new Mesh { name = "Nuages", indexFormat = IndexFormat.UInt32 };
            m_Mesh.SetVertices(vertices);
            m_Mesh.SetColors(colors);
            m_Mesh.SetTriangles(triangles, 0);
            m_Mesh.RecalculateNormals();
            m_Mesh.RecalculateBounds();

            gameObject.AddComponent<MeshFilter>().sharedMesh = m_Mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_Material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        // Hors du maillage, c'est du ciel : la face du bord est dessinée.
        static bool IsCloudy(bool[,] cloudy, int period, int size, int x, int z) =>
            x >= 0 && z >= 0 && x < size && z < size && cloudy[x % period, z % period];

        // Un quadrilatère (sommets dans le sens des aiguilles d'une montre, vu de l'extérieur) et son ombrage.
        static void AddQuad(List<Vector3> vertices, List<Color> colors, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d, float shade)
        {
            var start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);
            var color = new Color(shade, shade, shade, 1f);
            for (var i = 0; i < 4; i++)
                colors.Add(color);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        // Les pavés nuageux : un bruit qui se répète tous les « period » pavés (grandes masses, plus des détails), dont on
        // garde la part la plus haute (Coverage).
        bool[,] Pattern(int period)
        {
            var random = new System.Random(m_Seed);
            var coarse = Lattice(random, period / 8);
            var fine = Lattice(random, period / 4);
            var values = new float[period, period];
            var sorted = new List<float>(period * period);
            for (var z = 0; z < period; z++)
            {
                for (var x = 0; x < period; x++)
                {
                    var value = 0.7f * Sample(coarse, x / 8f, z / 8f) + 0.3f * Sample(fine, x / 4f, z / 4f);
                    values[x, z] = value;
                    sorted.Add(value);
                }
            }

            sorted.Sort();
            var threshold = sorted[Mathf.Clamp(Mathf.RoundToInt((1f - m_Coverage) * (sorted.Count - 1)), 0, sorted.Count - 1)];
            var cloudy = new bool[period, period];
            for (var z = 0; z < period; z++)
            {
                for (var x = 0; x < period; x++)
                    cloudy[x, z] = values[x, z] > threshold;
            }

            return cloudy;
        }

        static float[,] Lattice(System.Random random, int size)
        {
            var lattice = new float[size, size];
            for (var z = 0; z < size; z++)
            {
                for (var x = 0; x < size; x++)
                    lattice[x, z] = (float)random.NextDouble();
            }

            return lattice;
        }

        // Bruit de valeur lissé, qui boucle sur la taille du réseau.
        static float Sample(float[,] lattice, float x, float z)
        {
            var size = lattice.GetLength(0);
            var x0 = Mathf.FloorToInt(x);
            var z0 = Mathf.FloorToInt(z);
            var fx = x - x0;
            var fz = z - z0;
            fx = fx * fx * (3f - 2f * fx);
            fz = fz * fz * (3f - 2f * fz);
            float At(int i, int j) => lattice[((i % size) + size) % size, ((j % size) + size) % size];
            var bottom = Mathf.Lerp(At(x0, z0), At(x0 + 1, z0), fx);
            var top = Mathf.Lerp(At(x0, z0 + 1), At(x0 + 1, z0 + 1), fx);
            return Mathf.Lerp(bottom, top, fz);
        }
    }
}
