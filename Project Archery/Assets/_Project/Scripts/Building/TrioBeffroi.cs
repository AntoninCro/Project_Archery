using System.Collections.Generic;
using Archery.Bows;
using Archery.Core;
using Archery.Enemies;
using Archery.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Archery.Building
{
    /// <summary>
    /// Le trio <b>Beffroi</b> (Boussole folle + Phare, 1er novembre ; <c>Docs/Refonte_TowerDefense.md</c>, 12.19) : un beffroi de
    /// pierre, son grand cadran doré aux aiguilles qui tournent à l'envers, sa cloche, la rose des vents à son pied. Quand la
    /// cloche sonne, les ennemis du rayon <b>rembobinent</b> leurs 4 dernières secondes : ils repartent à reculons sur le chemin
    /// qu'ils viennent de faire (« Rembobiné ! ») et perdent leur avance.
    /// <list type="bullet">
    /// <item>1 : la cloche toutes les 10 s ;</item>
    /// <item>2 : à l'arrivée, ils restent figés 2 s (étourdis : préparés) ;</item>
    /// <item>3, <b>le Phare du passé</b> (2 novembre au soir, choisi par le joueur ; <c>Docs/Refonte_TowerDefense.md</c>, 12.45) :
    /// en plus, toutes les 20 s, avec cinq ennemis à portée, la lanterne du sommet s'allume et un grand faisceau sépia fait le tour
    /// du beffroi en 6 s, sur toute la carte, comme un phare ; chaque ennemi au sol qu'il balaie rembobine son chemin, puis reste
    /// figé.</item>
    /// </list>
    /// <b>Minuit</b>, l'ancien niveau 3 (douze coups ; une grande bulle part du beffroi et couvre toute la carte ; dedans, le monde
    /// en noir et blanc, le temps s'arrête 9 s sauf pour toi, tes flèches attendent en l'air et repartent toutes ensemble au
    /// douzième coup), passe à la <b>Grande Horloge</b>, l'ultime de la glace et du temps (12.44) : le joueur, le 2 novembre au
    /// soir, « elle reprend le principe du Beffroi au niveau 3 ». Son code reste ici, en sommeil : il ne joue qu'au niveau 4, que
    /// le Beffroi n'atteint pas ; la Grande Horloge (<see cref="UltimeHorloge"/>, 2 novembre, tard) a le sien, repris d'ici avec les
    /// réglages du joueur.
    /// </summary>
    /// <remarks>
    /// Le beffroi note le chemin des ennemis au sol près de lui (une position tous les dixièmes de seconde, 12,8 s ; sur toute la
    /// portée du faisceau au niveau 3). Rembobiner, c'est tenir l'ennemi (<see cref="Enemy.Hold"/>) et le ramener le long de ce
    /// chemin, à l'envers, en 1,2 s, puis le lâcher. Le temps arrêté : les ennemis gelés (<see cref="Enemy.Freeze"/>, sans glace),
    /// les flèches arrêtées comme celles du fût suspendu (<see cref="Arrow.Hover"/>). Les boss ne sont jamais rembobinés, et figés
    /// moins longtemps ; les volants pas rembobinés.
    /// </remarks>
    public class TrioBeffroi : TowerTrio
    {
        const float k_Sample = 0.1f;

        [Header("Le beffroi")]
        [Tooltip("La cloche (son repère en haut, à l'attache) : elle se balance quand elle sonne.")]
        [SerializeField]
        Transform m_Bell;

        [Tooltip("Les aiguilles (repère au centre du cadran, l'aiguille vers le haut) : petites et grandes, l'une après l'autre ; " +
                 "elles tournent à l'envers, plus vite quand le temps recule.")]
        [SerializeField]
        Transform[] m_Hands = new Transform[0];

        [Tooltip("Portée (m, à plat, autour du beffroi).")]
        [SerializeField]
        float m_Range = 15f;

        [Header("Rembobiner")]
        [Tooltip("Temps (s) entre deux coups de cloche, par niveau.")]
        [SerializeField]
        float[] m_IntervalPerLevel = { 10f, 8.5f, 7f };

        [Tooltip("Combien de secondes de leur chemin ils rembobinent, et en combien de temps (s).")]
        [SerializeField]
        float m_RewindSeconds = 4f;

        [SerializeField]
        float m_RewindTime = 1.2f;

        [Tooltip("Ennemis rembobinés au plus par coup de cloche.")]
        [SerializeField]
        int m_MaxRewound = 12;

        [Tooltip("Niveau 2 : figés (étourdis) tant de secondes à l'arrivée.")]
        [SerializeField]
        float m_ArrivalStun = 2f;

        [Header("Le Phare du passé (niveau 3)")]
        [Tooltip("La lanterne du sommet (celle du niveau 2) : le faisceau en part. Vide : au-dessus de la cloche.")]
        [SerializeField]
        Transform m_Lantern;

        [Tooltip("Temps (s) entre deux tours du faisceau ; il faut au moins cinq ennemis à portée de la cloche.")]
        [SerializeField]
        float m_BeamInterval = 20f;

        [Tooltip("Portée (m, à plat) du faisceau, et sa longueur : toute la carte.")]
        [SerializeField]
        float m_BeamRadius = 110f;

        [Tooltip("Durée (s) d'un tour complet du faisceau.")]
        [SerializeField]
        float m_BeamTurnTime = 6f;

        [Tooltip("Largeur (°) du faisceau.")]
        [SerializeField]
        float m_BeamWidth = 6f;

        [Tooltip("Le matériau du faisceau (sépia, transparent). Vide : le faisceau ne se voit pas, il rembobine quand même.")]
        [SerializeField]
        Material m_BeamMaterial;

        [Header("Minuit (la Grande Horloge, l'ultime ; en sommeil)")]
        [Tooltip("Temps (s) entre deux Minuit ; il faut au moins cinq ennemis à portée.")]
        [SerializeField]
        float m_MidnightInterval = 30f;

        [Tooltip("Rayon (m) où le temps s'arrête, et durée (s) des douze coups.")]
        [SerializeField]
        float m_MidnightRadius = 20f;

        [SerializeField]
        float m_MidnightTime = 6f;

        [Tooltip("Un boss n'est figé que tant de secondes.")]
        [SerializeField]
        float m_BossStopTime = 2f;

        [Tooltip("La bulle du temps arrêté (2 novembre, le joueur) : elle part du beffroi et atteint le rayon de Minuit en tant de " +
                 "secondes ; le temps ne s'arrête qu'à son passage. Dedans, le monde est en noir et blanc.")]
        [SerializeField]
        float m_BubbleGrowTime = 0.8f;

        [Tooltip("Le matériau de la bulle (le verre de la bulle). Vide : la bulle ne se voit pas, le noir et blanc reste.")]
        [SerializeField]
        Material m_BubbleMaterial;

        [Tooltip("La saturation dans la bulle (−100 : noir et blanc).")]
        [Range(-100f, 0f)]
        [SerializeField]
        float m_BubbleSaturation = -100f;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_BellClip;

        [SerializeField]
        AudioClip m_RewindClip;

        class Track
        {
            public readonly Vector3[] Points = new Vector3[128]; // 12,8 s (8 s rembobinées depuis le 2 novembre ; 64 avant)
            public int Head;
            public int Count;
            public float Seen;
        }

        class Rewind
        {
            public Enemy Enemy;
            public readonly List<Vector3> Path = new List<Vector3>();
            public float Time;
            public float NextFx;
            public float FxEvery;
        }

        class Stopped
        {
            public Arrow Arrow;
            public Vector3 Velocity;
        }

        static readonly Color k_Sepia = new Color(0.78f, 0.62f, 0.4f, 1f);
        static readonly Color k_Gold = new Color(1f, 0.82f, 0.4f, 1f);
        static readonly Color k_Night = new Color(0.55f, 0.65f, 1f, 1f);

        readonly Dictionary<Enemy, Track> m_Tracks = new Dictionary<Enemy, Track>();
        readonly List<Rewind> m_Rewinds = new List<Rewind>();
        readonly List<Stopped> m_Stopped = new List<Stopped>();
        readonly HashSet<Enemy> m_Frozen = new HashSet<Enemy>();
        readonly List<Enemy> m_Scratch = new List<Enemy>();
        float m_NextSample;
        float m_NextRing;
        float m_Swing;
        float m_HandSpeed = 1f;
        float m_NextMidnight;
        int m_Strokes;
        float m_NextStroke;
        float m_MidnightEnd;
        float m_NextArrowScan;
        float m_MidnightStart = -1f;
        Transform m_Bubble;
        Volume m_Grayscale;
        VolumeProfile m_GrayProfile;
        static Mesh s_TwoSidedSphere;
        readonly HashSet<Enemy> m_Swept = new HashSet<Enemy>();
        float m_NextBeam;
        float m_BeamStart = -1f;
        float m_BeamFrom;
        float m_NextBeamSound;
        Transform m_Beam;
        Mesh m_BeamMesh;

        // Il faut au moins tant d'ennemis à portée de la cloche pour le faisceau (et Minuit).
        const int k_Crowd = 5;

        protected override void OnDestroy()
        {
            foreach (var rewind in m_Rewinds)
            {
                if (rewind.Enemy != null && rewind.Enemy.IsHeld)
                    rewind.Enemy.Release(Vector3.zero);
            }

            EndMidnight();
            m_Rewinds.Clear();
            if (m_Beam != null)
                Destroy(m_Beam.gameObject);
            if (m_BeamMesh != null)
                Destroy(m_BeamMesh);
            if (m_Bubble != null)
                Destroy(m_Bubble.gameObject);
            if (m_Grayscale != null)
                Destroy(m_Grayscale.gameObject);
            if (m_GrayProfile != null)
                Destroy(m_GrayProfile);
            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();
            UpdateRewinds();
            AnimateClock();
            UpdateBubble();
            if (!IsBuilt || IsUnderConstruction)
            {
                if (m_BeamStart >= 0f)
                    EndBeam();
                return;
            }

            var level = Mathf.Max(1, Level);
            Record(level >= 3 ? Mathf.Max(m_Range + 8f, m_BeamRadius) : m_Range + 8f);
            // Minuit : la Grande Horloge (l'ultime, le niveau 4 ; en sommeil tant qu'elle n'est pas codée).
            if (level >= 4 && UpdateMidnight())
                return;
            if (level >= 3)
                UpdateBeam();

            if (Time.time < m_NextRing || EnemiesNear(transform.position, m_Range).Count == 0)
                return;

            Ring(1f);
            RewindAll();
            m_NextRing = Time.time + ValueFor(m_IntervalPerLevel, level, 10f) / OverchargeRate;
        }

        // ---------------------------------------------------------------- le chemin des ennemis

        // Une position tous les dixièmes de seconde pour chaque ennemi au sol à moins de reach m du beffroi (toute la portée du
        // faisceau au niveau 3) ; les autres sont oubliés.
        void Record(float reach)
        {
            if (Time.time < m_NextSample)
                return;
            m_NextSample = Time.time + k_Sample;

            foreach (var enemy in EnemiesNear(transform.position, reach))
            {
                if (enemy.IsHeld || enemy.IsBoss)
                    continue;
                if (!m_Tracks.TryGetValue(enemy, out var track))
                {
                    track = new Track();
                    m_Tracks[enemy] = track;
                }

                track.Points[track.Head] = enemy.transform.position;
                track.Head = (track.Head + 1) % track.Points.Length;
                track.Count = Mathf.Min(track.Count + 1, track.Points.Length);
                track.Seen = Time.time;
            }

            m_Scratch.Clear();
            foreach (var pair in m_Tracks)
            {
                if (pair.Key == null || !pair.Key.IsAlive || Time.time - pair.Value.Seen > 1f)
                    m_Scratch.Add(pair.Key);
            }

            foreach (var enemy in m_Scratch)
                m_Tracks.Remove(enemy);
        }

        // ---------------------------------------------------------------- rembobiner

        int RewindSamples => Mathf.Clamp(Mathf.RoundToInt(m_RewindSeconds / k_Sample), 2, 127);

        void RewindAll()
        {
            var samples = RewindSamples;
            var count = 0;
            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(transform.position, m_Range));
            foreach (var enemy in m_Scratch)
            {
                if (count >= m_MaxRewound)
                    break;
                if (StartRewind(enemy, samples, 0.08f))
                    count++;
            }

            if (count > 0)
            {
                m_HandSpeed = -24f;
                Sfx.Play(m_RewindClip, transform.position + Vector3.up * 4f, 0.8f, 0.7f);
            }
        }

        // Tenu, il repartira à reculons le long de son chemin (sa traînée sépia toutes les fxEvery s) ; faux s'il ne peut pas (en
        // l'air, tenu par autre chose, déjà rembobiné, pas assez de chemin noté).
        bool StartRewind(Enemy enemy, int samples, float fxEvery)
        {
            if (!enemy.CanBeLaunched || enemy.IsAirborne || IsRewinding(enemy) || !m_Tracks.TryGetValue(enemy, out var track) ||
                track.Count < 3)
                return false;

            // Son chemin, du plus récent au plus ancien, en commençant là où il est.
            var rewind = new Rewind { Enemy = enemy, FxEvery = fxEvery };
            rewind.Path.Add(enemy.transform.position);
            for (var k = 1; k <= Mathf.Min(samples, track.Count); k++)
            {
                var index = (track.Head - k + track.Points.Length * 2) % track.Points.Length;
                rewind.Path.Add(track.Points[index]);
            }

            if (!enemy.Hold(this))
                return false;

            track.Count = 0;
            m_Rewinds.Add(rewind);
            FloatingText.Spawn(enemy.transform.position + Vector3.up * 2f, "<< Rembobiné !", k_Sepia, 1.2f, 1.3f);
            return true;
        }

        bool IsRewinding(Enemy enemy)
        {
            foreach (var rewind in m_Rewinds)
            {
                if (rewind.Enemy == enemy)
                    return true;
            }

            return false;
        }

        // Il repart à reculons le long de son chemin, sans se retourner ; au bout, il est lâché là (figé au niveau 2).
        void UpdateRewinds()
        {
            for (var i = m_Rewinds.Count - 1; i >= 0; i--)
            {
                var rewind = m_Rewinds[i];
                var enemy = rewind.Enemy;
                if (enemy == null || !enemy.IsAlive || !enemy.IsHeld)
                {
                    m_Rewinds.RemoveAt(i);
                    continue;
                }

                rewind.Time += Time.deltaTime;
                var t = Mathf.Clamp01(rewind.Time / Mathf.Max(0.1f, m_RewindTime));
                var along = t * (rewind.Path.Count - 1);
                var index = Mathf.Min(rewind.Path.Count - 2, Mathf.FloorToInt(along));
                enemy.KeepHeld();
                enemy.transform.position = Vector3.Lerp(rewind.Path[index], rewind.Path[index + 1], along - index);
                if (Time.time >= rewind.NextFx)
                {
                    rewind.NextFx = Time.time + rewind.FxEvery;
                    Burst(enemy.Center, k_Sepia, 5, 1f, 0.3f, 0.5f, -0.1f);
                }

                if (t < 1f)
                    continue;

                enemy.Release(Vector3.zero);
                if (Level >= 2)
                {
                    enemy.Stun(m_ArrivalStun);
                    FloatingText.Spawn(enemy.transform.position + Vector3.up * 2f, "Figé !", k_Sepia, 1f, 1f);
                }

                m_Rewinds.RemoveAt(i);
            }
        }

        // ---------------------------------------------------------------- le Phare du passé (niveau 3, 2 novembre au soir)

        // Toutes les 20 s, avec du monde à portée de la cloche : le faisceau fait un tour complet et rembobine tout ce qu'il balaie.
        void UpdateBeam()
        {
            if (m_BeamStart < 0f)
            {
                if (Time.time < m_NextBeam || EnemiesNear(transform.position, m_Range).Count < k_Crowd)
                    return;
                StartBeam();
            }

            var swept = Mathf.Clamp01((Time.time - m_BeamStart) / Mathf.Max(0.5f, m_BeamTurnTime)) * 360f;
            Sweep(swept);
            ShowBeam(swept);
            if (swept >= 360f)
                EndBeam();
        }

        void StartBeam()
        {
            m_BeamStart = Time.time;
            m_NextBeam = Time.time + m_BeamInterval / OverchargeRate;
            m_Swept.Clear();

            // Il part vers l'ennemi le plus proche : le premier est rembobiné tout de suite, on voit d'où vient l'effet.
            var origin = transform.position;
            var nearest = float.MaxValue;
            var toward = transform.forward;
            foreach (var enemy in EnemiesNear(origin, m_BeamRadius))
            {
                var flat = enemy.transform.position - origin;
                flat.y = 0f;
                if (flat.sqrMagnitude < nearest)
                {
                    nearest = flat.sqrMagnitude;
                    toward = flat;
                }
            }

            m_BeamFrom = Heading(toward) - m_BeamWidth * 0.5f;
            m_HandSpeed = -120f;
            Ring(1.2f);
            Sfx.Play(m_RewindClip, transform.position + Vector3.up * 4f, 1f, 0.6f);
            m_NextBeamSound = Time.time + 1.5f;
            HeadMessages.Show("Le Phare du passé !", k_Sepia, 2.5f);
        }

        static float Heading(Vector3 flat) => Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;

        // Chaque ennemi au sol que le faisceau a dépassé depuis son départ (swept degrés) est rembobiné, une fois par tour.
        void Sweep(float swept)
        {
            var origin = transform.position;
            var samples = RewindSamples;
            var count = 0;
            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(origin, m_BeamRadius));
            foreach (var enemy in m_Scratch)
            {
                if (m_Swept.Contains(enemy))
                    continue;
                var flat = enemy.transform.position - origin;
                flat.y = 0f;
                if (Mathf.Repeat(Heading(flat) - m_BeamFrom, 360f) > swept + m_BeamWidth)
                    continue;

                // Une seule fois par tour, même s'il n'a pas pu être rembobiné (tenu par autre chose) : pas de retard derrière le faisceau.
                m_Swept.Add(enemy);
                // Une traînée plus clairsemée que celle de la cloche : toute la carte recule en même temps (le Quest).
                if (StartRewind(enemy, samples, 0.3f))
                    count++;
            }

            if (count > 0 && Time.time >= m_NextBeamSound)
            {
                m_NextBeamSound = Time.time + 1.5f;
                Sfx.Play(m_RewindClip, transform.position + Vector3.up * 4f, 0.7f, 0.65f);
            }
        }

        void EndBeam()
        {
            m_BeamStart = -1f;
            m_Swept.Clear();
            if (m_Beam != null)
                m_Beam.gameObject.SetActive(false);
        }

        Vector3 BeamOrigin => m_Lantern != null ? m_Lantern.position : Top + Vector3.up * 2f;

        // Le faisceau part de la lanterne et tourne ; il s'allonge en un instant au départ.
        void ShowBeam(float swept)
        {
            if (m_BeamMaterial == null)
                return;

            EnsureBeam();
            m_Beam.gameObject.SetActive(true);
            m_Beam.position = BeamOrigin;
            m_Beam.rotation = Quaternion.Euler(0f, m_BeamFrom + swept + m_BeamWidth * 0.5f, 0f);
            var grow = Mathf.Clamp01((Time.time - m_BeamStart) / 0.3f);
            m_Beam.localScale = new Vector3(1f, 1f, Mathf.Max(0.01f, grow));
        }

        void EnsureBeam()
        {
            if (m_Beam != null)
                return;

            var height = Mathf.Max(2f, BeamOrigin.y - transform.position.y);
            m_BeamMesh = BeamMesh(height, m_BeamRadius, m_BeamWidth * 0.5f);
            var go = new GameObject("Faisceau du passé (effet)");
            go.AddComponent<MeshFilter>().sharedMesh = m_BeamMesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_BeamMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            m_Beam = go.transform;
        }

        // Une part de lumière, vue des deux côtés : de la lanterne (l'origine) jusqu'au sol du beffroi à ses pieds et, au loin, un
        // bord de 4 m de haut posé au sol ; halfAngle degrés de chaque côté de l'avant.
        static Mesh BeamMesh(float height, float length, float halfAngle)
        {
            Vector3 Far(float side, float up)
            {
                var angle = side * halfAngle * Mathf.Deg2Rad;
                return new Vector3(Mathf.Sin(angle) * length, up - height, Mathf.Cos(angle) * length);
            }

            var apex = Vector3.zero;
            var foot = new Vector3(0f, -height, 0f);
            Vector3 leftLow = Far(-1f, 0f), rightLow = Far(1f, 0f), leftHigh = Far(-1f, 4f), rightHigh = Far(1f, 4f);
            var faces = new[]
            {
                apex, foot, leftLow, apex, leftLow, leftHigh, // le côté gauche
                apex, rightLow, foot, apex, rightHigh, rightLow, // le côté droit
                apex, leftHigh, rightHigh, // le dessus
                leftHigh, leftLow, rightLow, leftHigh, rightLow, rightHigh, // le bout
            };

            // Chaque triangle deux fois, à l'endroit et à l'envers, avec ses propres sommets (les normales de chaque face).
            var vertices = new Vector3[faces.Length * 2];
            var triangles = new int[faces.Length * 2];
            for (var i = 0; i < faces.Length; i += 3)
            {
                for (var k = 0; k < 3; k++)
                {
                    vertices[i + k] = faces[i + k];
                    triangles[i + k] = i + k;
                    vertices[faces.Length + i + k] = faces[i + 2 - k];
                    triangles[faces.Length + i + k] = faces.Length + i + k;
                }
            }

            var mesh = new Mesh { name = "Faisceau du passé", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ---------------------------------------------------------------- Minuit (la Grande Horloge, l'ultime ; en sommeil)

        // Douze coups ; vrai pendant qu'ils sonnent.
        bool UpdateMidnight()
        {
            if (m_Strokes > 0)
            {
                FreezeTime();
                if (Time.time < m_NextStroke)
                    return true;

                var stroke = 13 - m_Strokes;
                m_Strokes--;
                m_NextStroke = Time.time + m_MidnightTime / 12f;
                Ring(1.2f);
                FloatingText.Spawn(Top + Vector3.up * 1.5f, "Dong ! " + stroke, k_Night, 1.4f, 1f);
                if (m_Strokes == 0)
                {
                    EndMidnight();
                    // Les mots du joueur (2 novembre) ; « Minuit ! » avant.
                    HeadMessages.Show("Le temps reprend son cours", k_Gold, 2.5f);
                    m_NextRing = Time.time + 2f;
                }

                return true;
            }

            if (Time.time < m_NextMidnight || EnemiesNear(transform.position, m_Range).Count < k_Crowd)
                return false;

            m_NextMidnight = Time.time + m_MidnightInterval / OverchargeRate;
            m_Strokes = 12;
            m_NextStroke = Time.time;
            m_MidnightStart = Time.time;
            m_MidnightEnd = Time.time + m_MidnightTime + 0.2f;
            m_HandSpeed = -240f;
            // Les mots du joueur (2 novembre) ; « Le temps s'arrête… » avant.
            HeadMessages.Show("Za Warudo ! Temps, arrête-toi !", k_Night, 3f);
            return true;
        }

        Vector3 Top => m_Bell != null ? m_Bell.position : transform.position + Vector3.up * 10f;

        // Le rayon atteint par la bulle de Minuit : elle part vite, puis ralentit en arrivant au bord.
        float MidnightReach
        {
            get
            {
                if (m_MidnightStart < 0f)
                    return 0f;
                var t = Mathf.Clamp01((Time.time - m_MidnightStart) / Mathf.Max(0.05f, m_BubbleGrowTime));
                return m_MidnightRadius * (1f - (1f - t) * (1f - t));
            }
        }

        // Le temps arrêté : les ennemis dans la bulle figés (un boss un instant), tes flèches arrêtées en l'air.
        void FreezeTime()
        {
            var left = Mathf.Max(0.1f, m_MidnightEnd - Time.time);
            var reach = MidnightReach;
            foreach (var enemy in EnemiesNear(transform.position, reach, false))
            {
                if (!m_Frozen.Add(enemy))
                    continue;
                enemy.Freeze(enemy.IsBoss ? Mathf.Min(left, m_BossStopTime) : left);
            }

            if (Time.time < m_NextArrowScan)
                return;
            m_NextArrowScan = Time.time + 0.05f;
            var radius = reach * reach;
            foreach (var arrow in FindObjectsByType<Arrow>())
            {
                if (arrow == null || arrow.IsHovering || Stops(arrow))
                    continue;
                var flat = arrow.transform.position - transform.position;
                flat.y = 0f;
                if (flat.sqrMagnitude > radius)
                    continue;

                var velocity = arrow.Velocity;
                if (velocity.sqrMagnitude < 1f)
                    continue;
                arrow.Hover(true);
                if (arrow.IsHovering)
                    m_Stopped.Add(new Stopped { Arrow = arrow, Velocity = velocity });
            }
        }

        bool Stops(Arrow arrow)
        {
            foreach (var stopped in m_Stopped)
            {
                if (stopped.Arrow == arrow)
                    return true;
            }

            return false;
        }

        // Le douzième coup : les flèches repartent toutes ensemble, les ennemis reprennent.
        void EndMidnight()
        {
            foreach (var stopped in m_Stopped)
            {
                if (stopped.Arrow == null || !stopped.Arrow.IsHovering)
                    continue;
                stopped.Arrow.Hover(false);
                stopped.Arrow.Velocity = stopped.Velocity;
            }

            foreach (var enemy in m_Frozen)
            {
                if (enemy != null)
                    enemy.Thaw();
            }

            if (m_Stopped.Count > 0)
                Burst(Top, k_Gold, 60, 10f, 0.3f, 0.9f, 0.3f);
            m_Stopped.Clear();
            m_Frozen.Clear();
            m_Strokes = 0;
            m_MidnightStart = -1f;
        }

        // ---------------------------------------------------------------- la bulle de Minuit (2 novembre, le joueur)

        // La bulle qui grandit (sa paroi se voit pendant qu'elle passe, puis elle s'efface), et le noir et blanc pour qui est dedans :
        // un volume global à part, au-dessus de celui de la carte, dont le poids suit la bulle (celui de la carte n'est pas touché).
        void UpdateBubble()
        {
            var reach = MidnightReach;
            var growing = m_MidnightStart >= 0f && Time.time - m_MidnightStart < m_BubbleGrowTime + 0.4f;
            if (m_BubbleMaterial != null && (growing || (m_Bubble != null && m_Bubble.gameObject.activeSelf)))
            {
                EnsureBubble();
                m_Bubble.gameObject.SetActive(growing);
                m_Bubble.position = transform.position;
                m_Bubble.localScale = Vector3.one * (2f * Mathf.Max(0.1f, reach));
            }

            var viewer = Camera.main != null ? Camera.main.transform.position : transform.position;
            var inside = m_MidnightStart >= 0f && (viewer - transform.position).sqrMagnitude <= reach * reach;
            if (!inside && m_Grayscale == null)
                return;

            EnsureGrayscale();
            m_Grayscale.weight = Mathf.MoveTowards(m_Grayscale.weight, inside ? 1f : 0f, Time.deltaTime * (inside ? 6f : 2.5f));
            m_Grayscale.enabled = m_Grayscale.weight > 0.001f;
        }

        void EnsureBubble()
        {
            if (m_Bubble != null)
                return;

            var go = new GameObject("Bulle de Minuit (effet)");
            go.AddComponent<MeshFilter>().sharedMesh = TwoSidedSphere();
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_BubbleMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            m_Bubble = go.transform;
        }

        void EnsureGrayscale()
        {
            if (m_Grayscale != null)
                return;

            m_GrayProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            var adjustments = m_GrayProfile.Add<ColorAdjustments>(true);
            adjustments.saturation.Override(m_BubbleSaturation);
            var go = new GameObject("Minuit, noir et blanc (effet)");
            m_Grayscale = go.AddComponent<Volume>();
            m_Grayscale.isGlobal = true;
            m_Grayscale.priority = 100f;
            m_Grayscale.sharedProfile = m_GrayProfile;
            m_Grayscale.weight = 0f;
        }

        // Une sphère vue des deux côtés : sa paroi se voit en arrivant, et encore de l'intérieur quand elle est passée.
        static Mesh TwoSidedSphere()
        {
            if (s_TwoSidedSphere != null)
                return s_TwoSidedSphere;

            var primitive = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var source = primitive.GetComponent<MeshFilter>().sharedMesh;
            var triangles = source.triangles;
            var both = new int[triangles.Length * 2];
            triangles.CopyTo(both, 0);
            for (var i = 0; i < triangles.Length; i += 3)
            {
                both[triangles.Length + i] = triangles[i];
                both[triangles.Length + i + 1] = triangles[i + 2];
                both[triangles.Length + i + 2] = triangles[i + 1];
            }

            s_TwoSidedSphere = new Mesh
            {
                name = "Sphère deux faces",
                vertices = source.vertices,
                normals = source.normals,
                uv = source.uv,
                triangles = both,
            };
            Destroy(primitive);
            return s_TwoSidedSphere;
        }

        // ---------------------------------------------------------------- la cloche, l'horloge

        void Ring(float volume)
        {
            m_Swing = 1f;
            Sfx.Play(m_BellClip, Top, volume, Random.Range(0.95f, 1.02f));
            Burst(Top, k_Gold, 20, 4f, 0.25f, 0.8f, 0f);
        }

        // La cloche se balance et s'apaise ; les aiguilles tournent à l'envers, plus vite quand le temps recule.
        void AnimateClock()
        {
            if (m_Bell != null)
            {
                m_Swing = Mathf.MoveTowards(m_Swing, 0f, Time.deltaTime * 0.5f);
                m_Bell.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 7f) * 28f * m_Swing, 0f, 0f);
            }

            m_HandSpeed = Mathf.MoveTowards(m_HandSpeed, -1f, Time.deltaTime * (m_Strokes > 0 || m_BeamStart >= 0f ? 0f : 40f));
            for (var i = 0; i < m_Hands.Length; i++)
            {
                if (m_Hands[i] == null)
                    continue;
                // Les grandes aiguilles (impaires) douze fois plus vite que les petites.
                var speed = m_HandSpeed * (i % 2 == 1 ? 12f : 1f);
                m_Hands[i].Rotate(Vector3.forward, -speed * Time.deltaTime * (i >= 2 ? -1f : 1f), Space.Self);
            }
        }
    }
}
