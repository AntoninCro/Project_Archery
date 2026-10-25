using System;
using Archery.Bows;
using Archery.Core;
using Archery.Player;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

namespace Archery.Cyber
{
    /// <summary>
    /// Les balles du toit cyberpunk : des éclats d'erreur rouges, lents et lumineux, avec une courte traînée, qui
    /// tressautent en vol comme des pixels morts. Elles traversent le décor ; elles ne touchent que le joueur, à la tête et
    /// au haut du corps (une capsule sous la tête), jamais aux mains : on les esquive en se penchant ou en s'accroupissant.
    /// Une balle qui frôle la tête siffle ; depuis le 24 octobre, elle grésille en vol, en son 3D, de plus en plus fort en
    /// approchant : on l'entend arriver, même sans la voir.
    /// </summary>
    public class GlitchBullets : MonoBehaviour
    {
        const int k_Max = 160;
        const float k_Size = 0.16f;
        const float k_BulletRadius = 0.05f;
        const float k_HeadRadius = 0.15f;
        const float k_BodyRadius = 0.19f;
        const float k_BodyLength = 0.55f;
        const float k_NearMiss = 0.75f;
        const float k_Life = 9f;

        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_RoundId = Shader.PropertyToID("_Round");

        class Bullet
        {
            public Transform Root;
            public TrailRenderer Trail;
            public AudioSource Hum;
            public Vector3 Position;
            public Vector3 Velocity;
            public Vector3 Jitter;
            public float JitterTimer;
            public float Age;
            public bool Active;
            public bool Whistled;
        }

        readonly Bullet[] m_Bullets = new Bullet[k_Max];
        Material m_Core;
        Material m_TrailMaterial;
        Mesh m_Mesh;
        Action<Vector3> m_OnHit;
        AudioClip m_PassClip;

        /// <summary>Balles en vol.</summary>
        public int ActiveCount { get; private set; }

        /// <param name="color">Couleur (HDR) des balles.</param>
        /// <param name="onHit">Une balle touche le joueur (son point d'impact).</param>
        /// <param name="passClip">Optionnel : le sifflement d'une balle qui frôle la tête.</param>
        public void Configure(Color color, Action<Vector3> onHit, AudioClip passClip)
        {
            m_OnHit = onHit;
            m_PassClip = passClip;
            var shared = LaserBeam.SharedMaterial;
            if (shared == null)
            {
                Debug.LogWarning("GlitchBullets : matériau Resources/LaserGlow introuvable, pas de balles visibles.", this);
                return;
            }

            m_Core = new Material(shared);
            m_Core.SetColor(k_BaseColorId, color * 4f);
            m_Core.SetFloat(k_RoundId, 1f);
            m_TrailMaterial = new Material(shared);
            m_TrailMaterial.SetColor(k_BaseColorId, color * 2f);
            m_TrailMaterial.SetFloat(k_RoundId, 0f);
            m_Mesh = BuildMesh();
        }

        /// <summary>Tire une balle.</summary>
        public void Fire(Vector3 origin, Vector3 velocity)
        {
            var bullet = Free();
            if (bullet == null)
                return;

            bullet.Position = origin;
            bullet.Velocity = velocity;
            bullet.Age = 0f;
            bullet.Jitter = Vector3.zero;
            bullet.JitterTimer = 0f;
            bullet.Whistled = false;
            bullet.Active = true;
            bullet.Root.position = origin;
            bullet.Root.gameObject.SetActive(true);
            bullet.Trail.Clear();
            bullet.Hum.outputAudioMixerGroup = Sfx.Output;
            bullet.Hum.pitch = Random.Range(0.9f, 1.15f);
            bullet.Hum.Play();
            ActiveCount++;
        }

        /// <summary>Efface toutes les balles (fin de l'épreuve).</summary>
        public void Clear()
        {
            foreach (var bullet in m_Bullets)
            {
                if (bullet != null && bullet.Active)
                    Retire(bullet);
            }
        }

        Bullet Free()
        {
            for (var i = 0; i < m_Bullets.Length; i++)
            {
                if (m_Bullets[i] == null)
                    m_Bullets[i] = Create(i);
                if (!m_Bullets[i].Active)
                    return m_Bullets[i];
            }

            return null;
        }

        Bullet Create(int index)
        {
            var root = new GameObject("Balle " + index).transform;
            root.SetParent(transform, false);
            root.gameObject.SetActive(false);

            var core = new GameObject("Éclat");
            core.transform.SetParent(root, false);
            core.transform.localScale = Vector3.one * k_Size;
            core.AddComponent<MeshFilter>().sharedMesh = m_Mesh;
            var renderer = core.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_Core;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var trail = root.gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.22f;
            trail.widthMultiplier = 0.07f;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.minVertexDistance = 0.05f;
            trail.sharedMaterial = m_TrailMaterial;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.alignment = LineAlignment.View;

            // Le grésillement : faible au loin, fort tout près ; l'effet Doppler le fait monter quand elle arrive.
            var hum = root.gameObject.AddComponent<AudioSource>();
            hum.clip = CyberCues.Hum;
            hum.loop = true;
            hum.playOnAwake = false;
            hum.spatialBlend = 1f;
            hum.rolloffMode = AudioRolloffMode.Logarithmic;
            hum.minDistance = 1f;
            hum.maxDistance = 20f;
            hum.dopplerLevel = 1f;
            hum.volume = 0.55f;
            return new Bullet { Root = root, Trail = trail, Hum = hum };
        }

        void Update()
        {
            if (ActiveCount == 0)
                return;

            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            var deltaTime = Time.deltaTime;
            foreach (var bullet in m_Bullets)
            {
                if (bullet == null || !bullet.Active)
                    continue;

                var previous = bullet.Position;
                bullet.Position += bullet.Velocity * deltaTime;
                bullet.Age += deltaTime;

                // Des à-coups : la balle saute de quelques centimètres, puis revient sur sa ligne.
                bullet.JitterTimer -= deltaTime;
                if (bullet.JitterTimer <= 0f)
                {
                    var jump = Random.value < 0.3f;
                    bullet.Jitter = jump ? Random.insideUnitSphere * 0.05f : Vector3.zero;
                    bullet.JitterTimer = jump ? 0.05f : Random.Range(0.08f, 0.25f);
                }

                bullet.Root.position = bullet.Position + bullet.Jitter;

                if (head != null && Touches(previous, bullet.Position, head.position, out var point))
                {
                    m_OnHit?.Invoke(point);
                    Retire(bullet);
                    continue;
                }

                if (head != null)
                    Whistle(bullet, head.position);

                if (bullet.Age > k_Life || (head != null && Behind(bullet, head.position)))
                    Retire(bullet);
            }
        }

        // La balle passe dans la capsule du joueur : de la tête au milieu du torse, droit vers le bas.
        static bool Touches(Vector3 from, Vector3 to, Vector3 head, out Vector3 point)
        {
            var chest = head + Vector3.down * k_BodyLength;
            ClosestPoints(from, to, head, chest, out var onBullet, out var onBody);
            var radius = Vector3.Distance(onBody, head) < k_HeadRadius ? k_HeadRadius : k_BodyRadius;
            point = onBullet;
            return (onBullet - onBody).sqrMagnitude <= (radius + k_BulletRadius) * (radius + k_BulletRadius);
        }

        void Whistle(Bullet bullet, Vector3 head)
        {
            if (bullet.Whistled || m_PassClip == null || (bullet.Position - head).sqrMagnitude > k_NearMiss * k_NearMiss)
                return;
            bullet.Whistled = true;
            Sfx.Play(m_PassClip, bullet.Position, 0.6f, Random.Range(1.3f, 1.6f));
        }

        // Elle a dépassé le joueur de plus de 4 m et s'éloigne.
        static bool Behind(Bullet bullet, Vector3 head)
        {
            var away = bullet.Position - head;
            return away.sqrMagnitude > 16f && Vector3.Dot(away, bullet.Velocity) > 0f;
        }

        void Retire(Bullet bullet)
        {
            bullet.Active = false;
            bullet.Root.gameObject.SetActive(false);
            ActiveCount = Mathf.Max(0, ActiveCount - 1);
        }

        // Les points les plus proches entre deux segments [a0, a1] et [b0, b1].
        static void ClosestPoints(Vector3 a0, Vector3 a1, Vector3 b0, Vector3 b1, out Vector3 onA, out Vector3 onB)
        {
            var d1 = a1 - a0;
            var d2 = b1 - b0;
            var r = a0 - b0;
            var a = Vector3.Dot(d1, d1);
            var e = Vector3.Dot(d2, d2);
            var f = Vector3.Dot(d2, r);
            float s, t;
            if (a <= 1e-8f)
            {
                s = 0f;
                t = e > 1e-8f ? Mathf.Clamp01(f / e) : 0f;
            }
            else
            {
                var c = Vector3.Dot(d1, r);
                if (e <= 1e-8f)
                {
                    t = 0f;
                    s = Mathf.Clamp01(-c / a);
                }
                else
                {
                    var b = Vector3.Dot(d1, d2);
                    var denominator = a * e - b * b;
                    s = denominator > 1e-8f ? Mathf.Clamp01((b * f - c * e) / denominator) : 0f;
                    t = (b * s + f) / e;
                    if (t < 0f)
                    {
                        t = 0f;
                        s = Mathf.Clamp01(-c / a);
                    }
                    else if (t > 1f)
                    {
                        t = 1f;
                        s = Mathf.Clamp01((b - c) / a);
                    }
                }
            }

            onA = a0 + d1 * s;
            onB = b0 + d2 * t;
        }

        // Trois carrés croisés (UV de 0 à 1) : un point lumineux qui se voit de tous les côtés.
        static Mesh BuildMesh()
        {
            var vertices = new Vector3[12];
            var uvs = new Vector2[12];
            var triangles = new int[18];
            var axes = new[] { (Vector3.right, Vector3.up), (Vector3.forward, Vector3.up), (Vector3.right, Vector3.forward) };
            for (var q = 0; q < 3; q++)
            {
                var (u, v) = axes[q];
                var start = q * 4;
                vertices[start] = (-u - v) * 0.5f;
                vertices[start + 1] = (u - v) * 0.5f;
                vertices[start + 2] = (u + v) * 0.5f;
                vertices[start + 3] = (-u + v) * 0.5f;
                uvs[start] = new Vector2(0f, 0f);
                uvs[start + 1] = new Vector2(1f, 0f);
                uvs[start + 2] = new Vector2(1f, 1f);
                uvs[start + 3] = new Vector2(0f, 1f);
                var t = q * 6;
                triangles[t] = start;
                triangles[t + 1] = start + 2;
                triangles[t + 2] = start + 1;
                triangles[t + 3] = start;
                triangles[t + 4] = start + 3;
                triangles[t + 5] = start + 2;
            }

            var mesh = new Mesh { name = "Balle glitchée", vertices = vertices, uv = uvs, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }

        void OnDestroy()
        {
            if (m_Core != null)
                Destroy(m_Core);
            if (m_TrailMaterial != null)
                Destroy(m_TrailMaterial);
            if (m_Mesh != null)
                Destroy(m_Mesh);
        }
    }
}
