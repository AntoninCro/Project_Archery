using Archery.Combat;
using Archery.Core;
using Archery.FinalBoss;
using Archery.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Archery.Effects
{
    /// <summary>
    /// L'aspect d'un point faible (GDD, section 10) : une gemme taillée de la couleur du boss, sertie dans une monture
    /// dorée à griffes, et devant elle un sceau doré du thème des menus, toujours tourné vers le joueur.
    /// La gemme bat comme un cœur, plus vite quand le boss a peu de PV ; quand une flèche la touche, elle jette un
    /// éclat, une onde dorée part du sceau et le sceau s'emballe un instant. À la mort du boss, tout se replie.
    /// </summary>
    /// <remarks>
    /// Fabriqué en jeu par le boss (<see cref="Attach"/>) sur la zone de touche du point faible. La boule des prefabs
    /// ne sert plus qu'à placer la zone et à régler sa taille dans l'éditeur : elle est cachée en jeu.
    /// </remarks>
    [DisallowMultipleComponent]
    public class WeakPointVisual : MonoBehaviour
    {
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");

        // Lumière « peinte » sur les facettes (les matériaux ne sont pas éclairés) : d'en haut à gauche, de face.
        static readonly Vector3 k_Light = new Vector3(-0.4f, 0.6f, 0.7f).normalized;
        static readonly Color k_GoldDark = new Color(0.4f, 0.24f, 0.07f);
        static readonly Color k_Gold = new Color(0.95f, 0.72f, 0.3f);
        static readonly Color k_GoldLight = new Color(1f, 0.94f, 0.68f);

        const int k_Sides = 8;               // côtés de la table de la gemme
        const float k_GemGlow = 2.2f;        // éclat de la gemme (le Bloom la fait briller)
        const float k_SealSize = 3.4f;       // côté du sceau, en rayons de la gemme
        const float k_SealOffset = 0.9f;     // le sceau passe un peu devant la gemme, vers le joueur (en rayons)

        static Mesh s_GemMesh;
        static Mesh s_MountMesh;
        static Material s_GemMaterial;
        static Material s_GoldMaterial;
        static Camera s_Camera;

        Hitbox m_Hitbox;
        Health m_Owner;
        Color m_Color;
        float m_Radius;              // rayon de la gemme, dans le repère de cet objet
        Transform m_Jewel;           // la gemme et sa monture, tournées vers l'extérieur du corps
        Transform m_Gem;
        MeshRenderer m_GemRenderer;
        MaterialPropertyBlock m_Block;

        Transform m_SealRoot;
        Canvas m_SealCanvas;
        Image m_Seal;
        Canvas m_WaveCanvas;
        Image m_Wave;
        Color m_SealColor;
        float m_SealRadius;          // rayon de la gemme (m) quand le sceau a été fait

        float m_Beat;                // phase du battement de cœur (0 à 1)
        float m_Flash;               // éclat après un coup (1 → 0)
        float m_WaveTime = 1f;       // avancée de l'onde dorée (0 → 1 ; 1 : pas d'onde)
        float m_WaveAngle;
        float m_SealAngle;
        float m_Spin;                // vitesse en plus du sceau après un coup (°/s)
        float m_Presence = 1f;       // 1 tant que le boss vit, descend à 0 à sa mort

        float WorldRadius => m_Radius * Mathf.Abs(transform.lossyScale.x);

        /// <summary>Habille ce point faible (sa zone de touche, avec la boule de l'éditeur) d'une gemme de cette couleur.</summary>
        public static WeakPointVisual Attach(Hitbox hitbox, Color color)
        {
            if (hitbox == null)
                return null;
            if (!hitbox.TryGetComponent<WeakPointVisual>(out var visual))
                visual = hitbox.gameObject.AddComponent<WeakPointVisual>();
            visual.Setup(hitbox, color);
            return visual;
        }

        void Setup(Hitbox hitbox, Color color)
        {
            m_Color = color;
            if (m_Jewel != null)
                return;

            m_Hitbox = hitbox;
            m_Hitbox.Struck += OnStruck;
            m_Owner = hitbox.Owner != null ? hitbox.Owner : GetComponentInParent<Health>();
            m_Block = new MaterialPropertyBlock();

            // La boule de l'éditeur donne la taille et le matériau de départ, puis se cache. Le battement remplace son « Pulse ».
            m_Radius = 0.5f;
            if (TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null)
                m_Radius = filter.sharedMesh.bounds.extents.x;
            var hasPlaceholder = TryGetComponent<MeshRenderer>(out var placeholder);
            if (!EnsureShared(hasPlaceholder ? placeholder.sharedMaterial : null))
                return;

            if (hasPlaceholder)
                placeholder.enabled = false;
            if (TryGetComponent<Pulse>(out var pulse))
                pulse.Stop();

            // La table de la gemme regarde vers l'extérieur du corps.
            var outward = Outward(out var forward);
            m_Jewel = new GameObject("Joyau").transform;
            m_Jewel.gameObject.layer = gameObject.layer;
            m_Jewel.SetParent(transform, false);
            m_Jewel.localScale = Vector3.one * m_Radius;
            m_Jewel.rotation = Quaternion.LookRotation(outward, Mathf.Abs(Vector3.Dot(outward, Vector3.up)) > 0.95f ? forward : Vector3.up);
            CreateRenderer("Monture", m_Jewel, s_MountMesh, s_GoldMaterial);
            m_GemRenderer = CreateRenderer("Gemme", m_Jewel, s_GemMesh, s_GemMaterial);
            m_Gem = m_GemRenderer.transform;

            // Le sceau du thème des menus, et l'onde des coups (le même cadre, qui s'agrandit en s'effaçant).
            var theme = UiTheme.Load();
            if (theme == null)
                return;

            m_SealColor = theme.panelBorderColor;
            m_SealRadius = Mathf.Max(0.01f, WorldRadius);
            m_SealRoot = new GameObject("Sceau").transform;
            m_SealRoot.gameObject.layer = gameObject.layer;
            m_SealRoot.SetParent(transform, false);
            var size = k_SealSize * m_SealRadius;
            m_Seal = UiThemer.Seal(m_SealRoot, "Cadre", theme, size, Vector3.zero, m_SealColor);
            m_SealCanvas = m_Seal.transform.parent.GetComponent<Canvas>();
            m_Wave = UiThemer.Seal(m_SealRoot, "Onde", theme, size, Vector3.zero, m_SealColor);
            m_WaveCanvas = m_Wave.transform.parent.GetComponent<Canvas>();
            m_WaveCanvas.enabled = false;
            m_SealAngle = Random.value * 90f;
            m_Beat = Random.value;
        }

        void OnDestroy()
        {
            if (m_Hitbox != null)
                m_Hitbox.Struck -= OnStruck;
        }

        void OnStruck(Vector3 point)
        {
            if (m_Presence <= 0f)
                return;

            m_Flash = 1f;
            m_WaveTime = 0f;
            m_WaveAngle = m_SealAngle;
            m_Spin = 420f;
        }

        void LateUpdate()
        {
            if (m_Jewel == null)
                return;

            var deltaTime = Time.deltaTime;
            var alive = m_Owner == null || m_Owner.IsAlive;
            m_Presence = Mathf.MoveTowards(m_Presence, alive ? 1f : 0f, deltaTime * 2f);
            var present = m_Presence > 0f;
            if (m_Jewel.gameObject.activeSelf != present)
                m_Jewel.gameObject.SetActive(present);

            // Battement de cœur : « boum-boum », de 1,3 s à pleine vie jusqu'à 0,55 s presque mort.
            var health = m_Owner != null ? m_Owner.Normalized : 1f;
            m_Beat = (m_Beat + deltaTime / Mathf.Lerp(0.55f, 1.3f, health)) % 1f;
            var beat = Bump(m_Beat, 0.06f) + 0.6f * Bump(m_Beat, 0.28f);
            m_Flash = Mathf.MoveTowards(m_Flash, 0f, deltaTime / 0.35f);

            m_Jewel.localScale = Vector3.one * (m_Radius * Mathf.SmoothStep(0f, 1f, m_Presence));
            m_Gem.localScale = Vector3.one * (1f + 0.08f * beat + 0.3f * m_Flash);
            var glow = m_Color * (k_GemGlow * (1f + 0.7f * beat) + 4f * m_Flash);
            glow.a = 1f;
            m_GemRenderer.GetPropertyBlock(m_Block);
            m_Block.SetColor(k_BaseColorId, glow);
            m_GemRenderer.SetPropertyBlock(m_Block);

            // Le dragon cache tous ses rendus pendant l'intro : le sceau suit la gemme.
            UpdateSeal(deltaTime, beat, present && m_GemRenderer.enabled);
        }

        void UpdateSeal(float deltaTime, float beat, bool shown)
        {
            if (m_SealRoot == null)
                return;

            if (m_SealCanvas.enabled != shown)
                m_SealCanvas.enabled = shown;
            var waving = shown && m_WaveTime < 1f;
            if (m_WaveCanvas.enabled != waving)
                m_WaveCanvas.enabled = waving;
            if (!shown)
                return;

            // Un peu devant la gemme, face au joueur, à la taille de la gemme (le boss peut grandir).
            if (s_Camera == null)
                s_Camera = Camera.main;
            var center = transform.position;
            var toCamera = s_Camera != null ? s_Camera.transform.position - center : -transform.forward;
            toCamera = toCamera.sqrMagnitude > 1e-6f ? toCamera.normalized : Vector3.back;
            var radius = WorldRadius;
            m_SealRoot.position = center + toCamera * (k_SealOffset * radius);
            m_SealRoot.rotation = Quaternion.LookRotation(-toCamera, Vector3.up);
            m_SealRoot.localScale = Vector3.one * (radius / m_SealRadius / Mathf.Max(1e-5f, Mathf.Abs(transform.lossyScale.x)));

            // Il tourne doucement, s'emballe après un coup, bat avec la gemme.
            m_Spin *= Mathf.Exp(-deltaTime * 3f);
            m_SealAngle = (m_SealAngle + deltaTime * (18f + m_Spin)) % 360f;
            var presence = Mathf.SmoothStep(0f, 1f, m_Presence);
            var seal = m_Seal.rectTransform;
            seal.localRotation = Quaternion.Euler(0f, 0f, 45f + m_SealAngle);
            seal.localScale = Vector3.one * ((1f + 0.04f * beat + 0.12f * m_Flash) * presence);
            var color = Color.Lerp(m_SealColor, Color.white, 0.5f * m_Flash);
            color.a = m_Presence * (0.8f + 0.2f * beat);
            m_Seal.color = color;

            if (!waving)
                return;

            m_WaveTime = Mathf.Min(1f, m_WaveTime + deltaTime / 0.45f);
            var ease = 1f - (1f - m_WaveTime) * (1f - m_WaveTime);
            var wave = m_Wave.rectTransform;
            wave.localRotation = Quaternion.Euler(0f, 0f, 45f + m_WaveAngle);
            wave.localScale = Vector3.one * (1f + 0.9f * ease);
            var waveColor = Color.Lerp(m_SealColor, Color.white, 0.3f);
            waveColor.a = (1f - m_WaveTime) * m_Presence;
            m_Wave.color = waveColor;
        }

        // Du milieu du corps (ses modèles animés) vers le point faible ; « forward » : l'avant du boss.
        Vector3 Outward(out Vector3 forward)
        {
            var root = m_Owner != null ? m_Owner.transform : transform.root;
            forward = root.forward;
            var found = false;
            var bounds = new Bounds();
            foreach (var body in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!found)
                    bounds = body.bounds;
                else
                    bounds.Encapsulate(body.bounds);
                found = true;
            }

            var outward = transform.position - (found ? bounds.center : root.position);
            return outward.sqrMagnitude > 1e-6f ? outward.normalized : forward;
        }

        MeshRenderer CreateRenderer(string name, Transform parent, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.layer = gameObject.layer;
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        // Une bosse douce de 0 à 1, centrée sur « center », large de 0,18 période.
        static float Bump(float t, float center)
        {
            var x = (t - center) / 0.09f;
            if (x <= -1f || x >= 1f)
                return 0f;
            var y = 1f - x * x;
            return y * y;
        }

        // ---------------------------------------------------------------- maillages et matériaux, partagés

        // Matériaux non éclairés, comme l'ancienne boule : la couleur vient des sommets (facettes « peintes »), et
        // celle de la gemme, au-delà de 1, du bloc de propriétés (pour le Bloom).
        static bool EnsureShared(Material source)
        {
            if (s_GemMesh == null)
                s_GemMesh = BuildGem();
            if (s_MountMesh == null)
                s_MountMesh = BuildMount();
            if (s_GemMaterial != null && s_GoldMaterial != null)
                return true;

            if (source == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null)
                    return false;
                source = new Material(shader);
            }

            s_GemMaterial = new Material(source) { name = "Point faible (gemme)" };
            s_GemMaterial.SetColor(k_BaseColorId, Color.white);
            s_GoldMaterial = new Material(source) { name = "Point faible (or)" };
            s_GoldMaterial.SetColor(k_BaseColorId, Color.white);
            return true;
        }

        // La gemme, de rayon 1 à la ceinture : table octogonale vers +Z, couronne à facettes (étoiles et demi-losanges),
        // ceinture, culasse en pointe vers le corps. Les facettes sont en gris (la couleur du boss les teinte).
        static Mesh BuildGem()
        {
            const float tableZ = 0.42f;
            const float tableRadius = 0.56f;
            const float girdle = 0.07f;
            const float culet = -0.9f;
            const int ring = k_Sides * 2;

            var builder = new MeshBuilder();
            var table = new Vector3[k_Sides];
            var top = new Vector3[ring];
            var bottom = new Vector3[ring];
            for (var i = 0; i < k_Sides; i++)
                table[i] = Ring(i * 2f * Mathf.PI / k_Sides, tableRadius, tableZ);
            for (var k = 0; k < ring; k++)
            {
                top[k] = Ring(k * Mathf.PI / k_Sides, 1f, girdle);
                bottom[k] = Ring(k * Mathf.PI / k_Sides, 1f, -girdle);
            }

            var tableCenter = new Vector3(0f, 0f, tableZ);
            var culetPoint = new Vector3(0f, 0f, culet);
            for (var i = 0; i < k_Sides; i++)
            {
                var next = (i + 1) % k_Sides;
                var g0 = top[2 * i];
                var g1 = top[2 * i + 1];
                var g2 = top[(2 * i + 2) % ring];
                GemFacet(builder, tableCenter, table[i], table[next], 1.12f);
                GemFacet(builder, table[i], table[next], g1, 1f);
                GemFacet(builder, table[i], g1, g0, 0.86f);
                GemFacet(builder, table[next], g2, g1, 0.94f);
            }

            for (var k = 0; k < ring; k++)
            {
                var next = (k + 1) % ring;
                var variation = k % 2 == 0 ? 1f : 0.8f;
                GemFacet(builder, top[k], top[next], bottom[next], 0.7f * variation);
                GemFacet(builder, top[k], bottom[next], bottom[k], 0.7f * variation);
                GemFacet(builder, bottom[k], bottom[next], culetPoint, variation);
            }

            return builder.ToMesh("Point faible (gemme)");
        }

        // La monture dorée, à l'échelle de la gemme : un chaton (anneau) autour de la ceinture, six griffes rabattues
        // sur la couronne, et derrière, une rosace de douze pétales qui plongent vers le corps.
        static Mesh BuildMount()
        {
            const int segments = k_Sides * 2;
            const float inner = 1.02f;
            const float outer = 1.16f;
            const float low = -0.16f;
            const float high = 0.12f;

            var builder = new MeshBuilder();
            for (var k = 0; k < segments; k++)
            {
                var a0 = k * 2f * Mathf.PI / segments;
                var a1 = (k + 1) * 2f * Mathf.PI / segments;
                var middle = (a0 + a1) * 0.5f;
                var radial = new Vector3(Mathf.Cos(middle), Mathf.Sin(middle), 0f);
                FacetQuad(builder, Ring(a0, outer, low), Ring(a1, outer, low), Ring(a1, outer, high), Ring(a0, outer, high), radial);
                FacetQuad(builder, Ring(a0, outer, high), Ring(a1, outer, high), Ring(a1, inner, high), Ring(a0, inner, high), Vector3.forward);
                FacetQuad(builder, Ring(a0, inner, high), Ring(a1, inner, high), Ring(a1, inner, low), Ring(a0, inner, low), -radial);
            }

            for (var j = 0; j < 6; j++)
            {
                var angle = (j + 0.5f) * Mathf.PI / 3f;
                Prong(builder, angle, new Vector2(1.2f, -0.12f), new Vector2(1.14f, 0.27f), 0.2f, 0.16f);
                Prong(builder, angle, new Vector2(1.14f, 0.27f), new Vector2(0.78f, 0.27f), 0.16f, 0.08f);
            }

            const int petals = 12;
            const float half = Mathf.PI / petals;
            for (var p = 0; p < petals; p++)
            {
                var angle = p * 2f * Mathf.PI / petals;
                var ridge = Ring(angle, 1.1f, -0.06f);
                var tip = Ring(angle, 1.62f, -0.3f);
                Facet(builder, ridge, Ring(angle - half, 1.1f, -0.13f), tip, Vector3.forward, true);
                Facet(builder, ridge, tip, Ring(angle + half, 1.1f, -0.13f), Vector3.forward, true);
            }

            return builder.ToMesh("Point faible (monture)");
        }

        // Une griffe : un prisme effilé de « from » à « to » (rayon, hauteur), dans le plan de cet angle.
        static void Prong(MeshBuilder builder, float angle, Vector2 from, Vector2 to, float widthFrom, float widthTo)
        {
            const float thickness = 0.08f;
            const float overlap = 0.04f;     // les deux morceaux se recouvrent au coude
            var radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            var tangent = new Vector3(-radial.y, radial.x, 0f);
            var a = radial * from.x + Vector3.forward * from.y;
            var b = radial * to.x + Vector3.forward * to.y;
            var along = (b - a).normalized;
            a -= along * overlap;
            b += along * overlap;
            var across = Vector3.Cross(along, tangent).normalized * (thickness * 0.5f);
            var wa = tangent * (widthFrom * 0.5f);
            var wb = tangent * (widthTo * 0.5f);
            var corners = new[]
            {
                a - wa - across, a + wa - across, a + wa + across, a - wa + across,
                b - wb - across, b + wb - across, b + wb + across, b - wb + across,
            };

            var center = (a + b) * 0.5f;
            var faces = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 0, 1, 5, 4, 1, 2, 6, 5, 2, 3, 7, 6, 3, 0, 4, 7 };
            for (var f = 0; f < faces.Length; f += 4)
            {
                var p0 = corners[faces[f]];
                var p1 = corners[faces[f + 1]];
                var p2 = corners[faces[f + 2]];
                var p3 = corners[faces[f + 3]];
                FacetQuad(builder, p0, p1, p2, p3, (p0 + p1 + p2 + p3) * 0.25f - center);
            }
        }

        static Vector3 Ring(float angle, float radius, float z) => new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, z);

        // La gemme est convexe autour de l'origine : chaque facette regarde à l'opposé.
        static void GemFacet(MeshBuilder builder, Vector3 a, Vector3 b, Vector3 c, float variation) =>
            Facet(builder, a, b, c, (a + b + c) / 3f, false, variation);

        static void FacetQuad(MeshBuilder builder, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 side)
        {
            Facet(builder, a, b, c, side, true);
            Facet(builder, a, c, d, side, true);
        }

        // Une facette tournée vers « side » (ses sommets remis dans le bon sens au besoin), teintée selon la lumière
        // peinte : en or pour la monture, en gris pour la gemme.
        static void Facet(MeshBuilder builder, Vector3 a, Vector3 b, Vector3 c, Vector3 side, bool gold, float variation = 1f)
        {
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), side) < 0f)
                (b, c) = (c, b);
            var normal = Vector3.Cross(b - a, c - a).normalized;
            var light = Mathf.Clamp01(Vector3.Dot(normal, k_Light) * 0.5f + 0.5f);
            Color color;
            if (gold)
            {
                color = light < 0.5f ? Color.Lerp(k_GoldDark, k_Gold, light * 2f) : Color.Lerp(k_Gold, k_GoldLight, light * 2f - 1f);
            }
            else
            {
                var shade = Mathf.Min(1f, (0.28f + 0.72f * light * light) * variation);
                color = new Color(shade, shade, shade);
            }

            builder.Triangle(a, b, c, color);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_GemMesh = null;
            s_MountMesh = null;
            s_GemMaterial = null;
            s_GoldMaterial = null;
            s_Camera = null;
        }
    }
}
