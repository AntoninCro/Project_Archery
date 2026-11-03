using System.Collections.Generic;
using Archery.Core;
using Archery.Enemies;
using Archery.UI;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Le duo <b>Toboggan</b> (Tremplin + Tour de givre ; <c>Docs/Refonte_TowerDefense.md</c>, 12.15, 12.17 et 12.18) : un grand
    /// toboggan de jardin d'enfants qui enjambe la voie. Un portique de bois, sa plate-forme à 5,6 m, une échelle sur son flanc ; de
    /// la plate-forme, une piste de glace descend en l'air vers la forêt jusqu'à un tremplin. L'ennemi qui passe sous la
    /// plate-forme est pris : il grimpe l'échelle, glisse sur le ventre en tournoyant et décolle du tremplin en vrille, au ralenti,
    /// sous les notes du jury (« 9,5 ! ») : le tir au vol, en spectacle. Il retombe sonné, plus loin vers la forêt.
    /// <list type="bullet">
    /// <item>1 : une glissade par ennemi, un petit saut ;</item>
    /// <item>2 : un saut plus haut et plus long ; il retombe sur les autres et les renverse (les quilles, « Strike ! ») ;</item>
    /// <item>3, le <b>saut à ski</b> : un grand tremplin l'envoie à plus de 20 m de haut, loin vers la forêt, sous un
    /// « 10 ! 10 ! 10 ! » ; l'atterrissage le tue.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Revu le 31 octobre au soir, après le premier essai du joueur : « on ne comprend pas comment les ennemis glissent dessus », et
    /// un ennemi qui revenait traversait la piste, posée au sol (les bâtiments n'ont pas de collider). Tout est maintenant au-dessus
    /// de la tête des ennemis : ils passent dessous, et l'on voit l'échelle, la glissade et le saut. Toujours une seule glissade par
    /// ennemi (l'équilibre, le joueur). Le modèle (la « Piste ») est fait la forêt du côté -z ; s'il est posé à l'envers, il est
    /// retourné (la voie décide). Les boss sont seulement ralentis ; les volants pas touchés.
    /// </remarks>
    public class DuoToboggan : TowerDuo
    {
        [Header("Le portique")]
        [Tooltip("La piste (pièce du modèle), faite la forêt du côté -z : retournée si la voie va dans l'autre sens.")]
        [SerializeField]
        Transform m_Track;

        [Tooltip("Où un ennemi est pris, sous la plate-forme (repère de la piste) : demi-largeur, et de z à z.")]
        [SerializeField]
        float m_CatchHalfWidth = 1.9f;

        [SerializeField]
        float m_CatchFromZ = 1.2f;

        [SerializeField]
        float m_CatchToZ = 3.4f;

        [Tooltip("L'échelle : son x et son z (repère de la piste) ; l'ennemi grimpe de son côté extérieur.")]
        [SerializeField]
        float m_LadderX = 2.25f;

        [SerializeField]
        float m_LadderZ = 2.3f;

        [Tooltip("Vitesse (m/s) à laquelle il grimpe.")]
        [SerializeField]
        float m_ClimbSpeed = 4f;

        [Tooltip("Ennemis sur le toboggan en même temps, au plus, par niveau (les autres passent dessous). 5 au niveau 3 depuis le " +
                 "2 novembre (le tableau d'équilibrage : l'échelle limitait le saut à ski à moins d'un ennemi par seconde).")]
        [SerializeField]
        int[] m_MaxRidersPerLevel = { 3, 3, 5 };

        int MaxRiders => ValueFor(m_MaxRidersPerLevel, Mathf.Max(1, Level), 3);

        [Header("La piste de glace")]
        [Tooltip("Le dessus de la plate-forme (m), où la piste commence (z), et son bas avant le tremplin (z, hauteur de la glace).")]
        [SerializeField]
        float m_DeckHeight = 5.6f;

        [SerializeField]
        float m_ChuteStartZ = 1.2f;

        [SerializeField]
        float m_ChuteEndZ = -3f;

        [SerializeField]
        float m_ChuteEndHeight = 3.3f;

        [Tooltip("Le tremplin : la pente (°, en montant vers la forêt) et la longueur (m) de chacun de ses morceaux, comme le modèle.")]
        [SerializeField]
        float[] m_KickerAngles = { -8f, 6f, 20f, 34f };

        [SerializeField]
        float[] m_KickerLengths = { 0.45f, 0.45f, 0.45f, 0.45f };

        [Tooltip("Le grand tremplin du saut à ski (niveau 3).")]
        [SerializeField]
        float[] m_BigKickerAngles = { -4f, 16f, 36f, 56f };

        [SerializeField]
        float[] m_BigKickerLengths = { 0.5f, 0.55f, 0.6f, 0.7f };

        [Header("La glissade")]
        [Tooltip("Vitesse (m/s) au départ, accélération (m/s²) et vitesse au plus.")]
        [SerializeField]
        float m_StartSpeed = 3f;

        [SerializeField]
        float m_Acceleration = 10f;

        [SerializeField]
        float m_MaxSpeed = 14f;

        [Tooltip("Tours (°/s) sur lui-même pendant qu'il glisse.")]
        [SerializeField]
        float m_SlideSpin = 420f;

        [Header("Le saut")]
        [Tooltip("Vitesse (m/s) vers la forêt au décollage, par niveau.")]
        [SerializeField]
        float[] m_LaunchForwardPerLevel = { 2.5f, 4.5f, 9f };

        [Tooltip("Vitesse (m/s) vers le haut au décollage, par niveau.")]
        [SerializeField]
        float[] m_LaunchLiftPerLevel = { 6f, 8.5f, 15f };

        [Tooltip("Pesanteur du vol (part de la vraie), par niveau : le ralenti.")]
        [SerializeField]
        float[] m_LaunchGravityPerLevel = { 0.55f, 0.5f, 0.55f };

        [Tooltip("Vrille (°/s) en vol, autour de l'axe du saut, et sur lui-même.")]
        [SerializeField]
        float m_Twist = 420f;

        [SerializeField]
        float m_Turn = 260f;

        [Tooltip("Temps (s) qu'il reste sonné en retombant.")]
        [SerializeField]
        float m_DizzyTime = 1.2f;

        [Header("Les quilles (niveau 2)")]
        [Tooltip("En retombant, il renverse et blesse ceux qui sont à moins de ce rayon (m).")]
        [SerializeField]
        float m_StrikeRadius = 2.5f;

        [SerializeField]
        float m_StrikeDamage = 20f;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_ClimbClip;

        [SerializeField]
        AudioClip m_SlideClip;

        [SerializeField]
        AudioClip m_JumpClip;

        [SerializeField]
        AudioClip m_JuryClip;

        enum Phase
        {
            Hop,
            Climb,
            Walk,
            Slide,
        }

        class Rider
        {
            public Enemy Enemy;
            public Phase Phase;
            public float Time;
            public Vector3 From;
            public float Distance;
            public float Speed;
            public float Yaw;
            public float NextRung;
        }

        static readonly string[] k_Notes = { "8,5 !", "9 !", "9,5 !", "7 !", "10 !", "6,5 !", "9,8 !" };
        static readonly Color k_Gold = new Color(1f, 0.82f, 0.3f, 1f);

        readonly List<Rider> m_Riders = new List<Rider>();
        readonly HashSet<Enemy> m_Slid = new HashSet<Enemy>();
        readonly Dictionary<Enemy, int> m_Flying = new Dictionary<Enemy, int>();
        readonly List<Enemy> m_Scratch = new List<Enemy>();

        // La piste, dans le repère de la piste : des points (z, hauteur de la glace) et la distance de chacun depuis le haut.
        readonly List<Vector2> m_Path = new List<Vector2>();
        readonly List<float> m_PathDistances = new List<float>();
        int m_PathLevel;
        float m_NextSide;
        bool m_Flipped;

        void OnEnable() => Enemy.Landed += OnLanded;

        void OnDisable() => Enemy.Landed -= OnLanded;

        protected override void OnDestroy()
        {
            foreach (var rider in m_Riders)
            {
                if (rider.Enemy != null && rider.Enemy.IsHeld)
                    rider.Enemy.Release(Vector3.zero);
            }

            m_Riders.Clear();
            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsBuilt || m_Track == null)
                return;

            if (Time.time >= m_NextSide)
            {
                m_NextSide = Time.time + 2f;
                FaceForest();
            }

            var level = Mathf.Max(1, Level);
            if (m_PathLevel != level)
                BuildPath(level);

            CatchNewcomers();
            for (var i = m_Riders.Count - 1; i >= 0; i--)
            {
                if (!Ride(m_Riders[i], level))
                    m_Riders.RemoveAt(i);
            }

            if (m_Slid.Count > 64)
                ForgetGone();
        }

        // La piste descend vers la forêt : retournée (un demi-tour, les cases ne changent pas) si la forêt est de l'autre côté.
        void FaceForest()
        {
            var forest = ForestwardAt(transform.position);
            var flip = Vector3.Dot(transform.forward, forest) > 0f;
            if (flip == m_Flipped && m_Track.localRotation == (flip ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity))
                return;
            m_Flipped = flip;
            m_Track.localRotation = flip ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
        }

        // Les points de la glace : la pente depuis la plate-forme, puis les morceaux du tremplin (le grand au niveau 3).
        void BuildPath(int level)
        {
            m_PathLevel = level;
            m_Path.Clear();
            m_PathDistances.Clear();
            m_Path.Add(new Vector2(m_ChuteStartZ, m_DeckHeight));
            m_Path.Add(new Vector2(m_ChuteEndZ, m_ChuteEndHeight));
            var angles = level >= 3 ? m_BigKickerAngles : m_KickerAngles;
            var lengths = level >= 3 ? m_BigKickerLengths : m_KickerLengths;
            var point = m_Path[1];
            for (var i = 0; i < angles.Length; i++)
            {
                var a = angles[i] * Mathf.Deg2Rad;
                var length = i < lengths.Length ? lengths[i] : 0.45f;
                point += new Vector2(-Mathf.Cos(a), Mathf.Sin(a)) * length;
                m_Path.Add(point);
            }

            var total = 0f;
            m_PathDistances.Add(0f);
            for (var i = 1; i < m_Path.Count; i++)
            {
                total += Vector2.Distance(m_Path[i - 1], m_Path[i]);
                m_PathDistances.Add(total);
            }
        }

        float PathLength => m_PathDistances.Count > 0 ? m_PathDistances[m_PathDistances.Count - 1] : 0f;

        // Le point de la glace à cette distance du haut (z, hauteur), et le sens de la pente là.
        Vector2 PathAt(float distance, out Vector2 direction)
        {
            direction = new Vector2(-1f, 0f);
            for (var i = 1; i < m_Path.Count; i++)
            {
                if (distance > m_PathDistances[i] && i < m_Path.Count - 1)
                    continue;
                var segment = m_Path[i] - m_Path[i - 1];
                var length = Mathf.Max(1e-4f, m_PathDistances[i] - m_PathDistances[i - 1]);
                direction = segment / length;
                return m_Path[i - 1] + segment * Mathf.Clamp01((distance - m_PathDistances[i - 1]) / length);
            }

            return m_Path.Count > 0 ? m_Path[0] : Vector2.zero;
        }

        // Ceux qui passent sous la plate-forme sont pris (une fois chacun) ; sans place sur le toboggan, ils passent dessous.
        void CatchNewcomers()
        {
            if (m_Riders.Count >= MaxRiders)
                return;

            foreach (var enemy in GroundEnemies())
            {
                if (m_Slid.Contains(enemy))
                    continue;
                var local = m_Track.InverseTransformPoint(enemy.transform.position);
                if (Mathf.Abs(local.x) > m_CatchHalfWidth || local.z < m_CatchFromZ || local.z > m_CatchToZ || Mathf.Abs(local.y) > 2.5f)
                    continue;

                m_Slid.Add(enemy);
                if (enemy.IsBoss || !enemy.CanBeLaunched)
                {
                    enemy.Slow(0.5f, 2f);
                    continue;
                }

                if (!enemy.Hold(this))
                    continue;
                m_Riders.Add(new Rider { Enemy = enemy, Phase = Phase.Hop, From = local, Yaw = Random.Range(0f, 360f) });
                if (m_Riders.Count >= MaxRiders)
                    return;
            }
        }

        // Un saut jusqu'au pied de l'échelle, l'échelle, quelques pas sur la plate-forme, la glissade, le saut.
        bool Ride(Rider rider, int level)
        {
            var enemy = rider.Enemy;
            if (enemy == null || !enemy.IsAlive || !enemy.IsHeld)
            {
                if (enemy != null && enemy.IsHeld)
                    enemy.Release(Vector3.zero);
                return false;
            }

            enemy.KeepHeld();
            rider.Time += Time.deltaTime;
            var outside = m_LadderX + Mathf.Sign(m_LadderX) * 0.4f;
            var foot = new Vector3(outside, 0f, m_LadderZ);
            var facingLadder = Quaternion.LookRotation(m_Track.TransformDirection(new Vector3(-Mathf.Sign(m_LadderX), 0f, 0f)));
            switch (rider.Phase)
            {
                case Phase.Hop:
                {
                    var t = Mathf.Clamp01(rider.Time / 0.4f);
                    var local = Vector3.Lerp(rider.From, foot, t) + Vector3.up * (4f * 0.6f * t * (1f - t));
                    enemy.transform.SetPositionAndRotation(m_Track.TransformPoint(local), facingLadder);
                    if (t >= 1f)
                        Next(rider, Phase.Climb);
                    return true;
                }

                case Phase.Climb:
                {
                    // Barreau après barreau : un petit à-coup à chacun, et le bois qui craque.
                    var height = Mathf.Min(m_DeckHeight, rider.Time * m_ClimbSpeed);
                    var bob = Mathf.Abs(Mathf.Sin(height / 0.4f * Mathf.PI)) * 0.08f;
                    enemy.transform.SetPositionAndRotation(m_Track.TransformPoint(foot + Vector3.up * (height + bob)), facingLadder);
                    if (height >= rider.NextRung)
                    {
                        rider.NextRung = height + 1.2f;
                        Sfx.Play(m_ClimbClip, enemy.transform.position, 0.35f, Random.Range(0.9f, 1.2f));
                    }

                    if (height >= m_DeckHeight)
                        Next(rider, Phase.Walk);
                    return true;
                }

                case Phase.Walk:
                {
                    var t = Mathf.Clamp01(rider.Time / 0.6f);
                    var top = new Vector3(0f, m_DeckHeight, m_ChuteStartZ + 0.4f);
                    var local = Vector3.Lerp(foot + Vector3.up * m_DeckHeight, top, t);
                    var forward = m_Track.TransformDirection(new Vector3(-outside, 0f, top.z - m_LadderZ).normalized);
                    enemy.transform.SetPositionAndRotation(m_Track.TransformPoint(local), Quaternion.LookRotation(forward));
                    if (t < 1f)
                        return true;

                    Next(rider, Phase.Slide);
                    rider.Speed = m_StartSpeed;
                    Sfx.Play(m_SlideClip, enemy.transform.position, 0.7f, Random.Range(0.9f, 1.15f));
                    return true;
                }

                default:
                {
                    // Sur le ventre, la tête vers la forêt, en tournoyant ; au bout du tremplin, il décolle.
                    rider.Speed = Mathf.Min(m_MaxSpeed, rider.Speed + m_Acceleration * Time.deltaTime);
                    rider.Distance += rider.Speed * Time.deltaTime;
                    rider.Yaw += m_SlideSpin * Time.deltaTime;
                    var point = PathAt(rider.Distance, out var direction);
                    var local = new Vector3(Mathf.Sin(rider.Time * 4f) * 0.15f, point.y + 0.3f, point.x);
                    enemy.transform.position = m_Track.TransformPoint(local);
                    enemy.transform.rotation = Quaternion.Euler(0f, m_Track.eulerAngles.y + rider.Yaw, 0f) * Quaternion.Euler(90f, 0f, 0f);
                    if (rider.Distance < PathLength)
                        return true;

                    TakeOff(enemy, direction, level);
                    return false;
                }
            }
        }

        static void Next(Rider rider, Phase phase)
        {
            rider.Phase = phase;
            rider.Time = 0f;
        }

        void TakeOff(Enemy enemy, Vector2 slope, int level)
        {
            var forest = m_Track.TransformDirection(Vector3.back);
            forest.y = 0f;
            forest.Normalize();
            var lift = ValueFor(m_LaunchLiftPerLevel, level, 6f) + Mathf.Max(0f, slope.y) * 2f;
            var velocity = forest * ValueFor(m_LaunchForwardPerLevel, level, 2.5f) + Vector3.up * lift;
            var gravity = ValueFor(m_LaunchGravityPerLevel, level, 0.55f);
            enemy.Release(velocity, level >= 3, gravity);
            enemy.Tumble(forest * m_Twist + Vector3.up * m_Turn);
            m_Flying[enemy] = level;
            Sfx.Play(m_JumpClip, enemy.transform.position, 0.9f, Random.Range(0.9f, 1.1f));
            Jury(enemy.transform.position, velocity, gravity, level);
        }

        // Les notes du jury, au sommet du saut : trois au hasard ; trois « 10 ! » pour le saut à ski.
        void Jury(Vector3 from, Vector3 velocity, float gravity, int level)
        {
            var g = -Physics.gravity.y * Mathf.Max(0.1f, gravity);
            var rise = velocity.y / g;
            var apex = from + new Vector3(velocity.x, 0f, velocity.z) * rise + Vector3.up * (velocity.y * rise * 0.5f + 1.6f);
            var side = Vector3.Cross(Vector3.up, new Vector3(velocity.x, 0f, velocity.z).normalized);
            for (var i = -1; i <= 1; i++)
            {
                var note = level >= 3 ? "10 !" : k_Notes[Random.Range(0, k_Notes.Length)];
                FloatingText.Spawn(apex + side * (i * 1.6f) + Vector3.up * (i == 0 ? 0.5f : 0f), note, k_Gold, level >= 3 ? 2.2f : 1.5f,
                                   Mathf.Max(1.5f, rise + 1f));
            }

            Sfx.Play(m_JuryClip, from, 0.7f, level >= 3 ? 1.2f : 1f);
        }

        // Il retombe sonné ; au niveau 2, sur les autres, qu'il renverse.
        void OnLanded(Enemy enemy)
        {
            if (enemy == null || !m_Flying.TryGetValue(enemy, out var level))
                return;
            m_Flying.Remove(enemy);
            if (enemy.IsAlive)
            {
                enemy.Stun(m_DizzyTime);
                enemy.Prepare(Preparation.Stunned, m_DizzyTime);
            }

            if (level < 2)
                return;
            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(enemy.transform.position, m_StrikeRadius, true, enemy));
            var struck = 0;
            foreach (var other in m_Scratch)
            {
                if (Hurt(other, m_StrikeDamage, other.Center, other.transform.position - enemy.transform.position))
                    KnockDown(other, enemy.transform.position, 3.5f, 4.5f);
                struck++;
            }

            if (struck >= 2)
                FloatingText.Spawn(enemy.transform.position + Vector3.up * 2.4f, "Strike !", k_Gold, 1.6f, 1.3f);
        }

        void ForgetGone()
        {
            m_Scratch.Clear();
            foreach (var enemy in m_Slid)
            {
                if (enemy == null || !enemy.IsAlive)
                    m_Scratch.Add(enemy);
            }

            foreach (var enemy in m_Scratch)
            {
                m_Slid.Remove(enemy);
                m_Flying.Remove(enemy);
            }
        }
    }
}
