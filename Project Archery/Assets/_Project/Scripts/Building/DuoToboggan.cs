using System.Collections.Generic;
using Archery.Core;
using Archery.Enemies;
using Archery.UI;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Le duo <b>Toboggan</b> (Tremplin + Tour de givre ; <c>Docs/Refonte_TowerDefense.md</c>, 12.15 et 12.17) : une piste de glace
    /// en pente vers la forêt, sur la voie, bordée de bottes de paille. Un ennemi qui y monte glisse : sur le ventre, en
    /// tournoyant, il redescend vers la forêt, renverse ceux qu'il croise, et décolle du tremplin de glace en vrille, au ralenti ;
    /// les notes du jury s'affichent au-dessus de lui (« 9,5 ! ») : le tir au vol, en spectacle. Il retombe sonné.
    /// <list type="bullet">
    /// <item>1 : une glissade par ennemi, un petit saut (une dizaine de mètres perdus) ;</item>
    /// <item>2 : ceux qu'il percute glissent aussi (les quilles) ; un saut plus haut et plus long ;</item>
    /// <item>3, le <b>saut à ski</b> : il part à plus de 20 m de haut, 50 m vers la forêt, sous un « 10 ! 10 ! 10 ! » ;
    /// l'atterrissage le tue.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Le modèle (la « Piste ») est fait la forêt du côté -z ; s'il est posé à l'envers, il est retourné (la voie décide). Un
    /// ennemi ne glisse qu'une fois (le joueur, 31 octobre : l'équilibre) ; la voie ne se bloque jamais. Les boss sont seulement
    /// ralentis ; les volants pas touchés.
    /// </remarks>
    public class DuoToboggan : TowerDuo
    {
        [Header("La piste")]
        [Tooltip("La piste (pièce du modèle), faite la forêt du côté -z : retournée si la voie va dans l'autre sens.")]
        [SerializeField]
        Transform m_Track;

        [Tooltip("Demi-largeur (m) de la piste ; où l'ennemi glisse (z de la piste : il y monte après le tremplin).")]
        [SerializeField]
        float m_HalfWidth = 2.6f;

        [SerializeField]
        float m_SlipFromZ = -1.2f;

        [SerializeField]
        float m_TopZ = 5f;

        [Tooltip("Où commence la pente du tremplin, et où l'ennemi décolle (z de la piste).")]
        [SerializeField]
        float m_KickerStartZ = -2.4f;

        [SerializeField]
        float m_TakeOffZ = -3.8f;

        [Tooltip("Hauteur (m) de la glace en haut de la piste, en bas, et au bout du tremplin (par niveau : le saut à ski est plus haut).")]
        [SerializeField]
        float m_TopHeight = 1f;

        [SerializeField]
        float m_LowHeight = 0.15f;

        [SerializeField]
        float[] m_KickerHeightPerLevel = { 0.66f, 0.9f, 1.3f };

        [Header("La glissade")]
        [Tooltip("Vitesse (m/s) au départ, accélération (m/s²) et vitesse au plus.")]
        [SerializeField]
        float m_StartSpeed = 3f;

        [SerializeField]
        float m_Acceleration = 9f;

        [SerializeField]
        float m_MaxSpeed = 13f;

        [Tooltip("Tours (°/s) sur lui-même pendant qu'il glisse.")]
        [SerializeField]
        float m_SlideSpin = 540f;

        [Tooltip("Dégâts et rayon (m) du choc pour ceux qu'il percute en glissant.")]
        [SerializeField]
        float m_BumpDamage = 12f;

        [SerializeField]
        float m_BumpRadius = 1.3f;

        [Header("Le saut")]
        [Tooltip("Vitesse (m/s) vers la forêt au décollage, par niveau.")]
        [SerializeField]
        float[] m_JumpForwardPerLevel = { 4f, 6f, 9f };

        [Tooltip("Vitesse (m/s) vers le haut au décollage, par niveau.")]
        [SerializeField]
        float[] m_JumpLiftPerLevel = { 8f, 11f, 17f };

        [Tooltip("Pesanteur du vol (part de la vraie), par niveau : le ralenti.")]
        [SerializeField]
        float[] m_JumpGravityPerLevel = { 0.5f, 0.45f, 0.55f };

        [Tooltip("Vrille (°/s) en vol, autour de l'axe du saut, et sur lui-même.")]
        [SerializeField]
        float m_Twist = 420f;

        [SerializeField]
        float m_Turn = 260f;

        [Tooltip("Temps (s) qu'il reste sonné en retombant.")]
        [SerializeField]
        float m_DizzyTime = 1.5f;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_SlideClip;

        [SerializeField]
        AudioClip m_JumpClip;

        [SerializeField]
        AudioClip m_JuryClip;

        class Slider
        {
            public Enemy Enemy;
            public float X;
            public float Z;
            public float Speed;
            public float Yaw;
            public float Seed;
        }

        static readonly string[] k_Notes = { "8,5 !", "9 !", "9,5 !", "7 !", "10 !", "6,5 !", "9,8 !" };

        readonly List<Slider> m_Sliders = new List<Slider>();
        readonly HashSet<Enemy> m_Slid = new HashSet<Enemy>();
        readonly HashSet<Enemy> m_Flying = new HashSet<Enemy>();
        readonly List<Enemy> m_Scratch = new List<Enemy>();
        float m_NextSide;
        bool m_Flipped;

        void OnEnable() => Enemy.Landed += OnLanded;

        void OnDisable() => Enemy.Landed -= OnLanded;

        protected override void OnDestroy()
        {
            foreach (var slider in m_Sliders)
            {
                if (slider.Enemy != null && slider.Enemy.IsHeld)
                    slider.Enemy.Release(Vector3.zero);
            }

            m_Sliders.Clear();
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
            CatchNewcomers(level);
            for (var i = m_Sliders.Count - 1; i >= 0; i--)
            {
                if (!Slide(m_Sliders[i], level))
                    m_Sliders.RemoveAt(i);
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

        // Ceux qui montent sur la glace (passé le tremplin) glissent : une fois chacun.
        void CatchNewcomers(int level)
        {
            foreach (var enemy in GroundEnemies())
            {
                if (m_Slid.Contains(enemy))
                    continue;
                var local = m_Track.InverseTransformPoint(enemy.transform.position);
                if (Mathf.Abs(local.x) > m_HalfWidth || local.z < m_SlipFromZ || local.z > m_TopZ || Mathf.Abs(local.y) > 2.5f)
                    continue;
                StartSlide(enemy, local, level);
            }
        }

        void StartSlide(Enemy enemy, Vector3 local, int level)
        {
            m_Slid.Add(enemy);
            if (enemy.IsBoss || !enemy.CanBeLaunched)
            {
                enemy.Slow(0.5f, 2f);
                return;
            }

            if (!enemy.Hold(this))
                return;

            m_Sliders.Add(new Slider
            {
                Enemy = enemy,
                X = Mathf.Clamp(local.x, -m_HalfWidth + 0.4f, m_HalfWidth - 0.4f),
                Z = Mathf.Max(local.z, m_KickerStartZ + 0.5f),
                Speed = m_StartSpeed,
                Yaw = Random.Range(0f, 360f),
                Seed = Random.value * 10f,
            });
            Sfx.Play(m_SlideClip, enemy.transform.position, 0.7f, Random.Range(0.9f, 1.15f));
        }

        // Sur le ventre, en tournoyant, vers la forêt ; il renverse ceux qu'il croise ; au bout du tremplin, il décolle.
        bool Slide(Slider slider, int level)
        {
            var enemy = slider.Enemy;
            if (enemy == null || !enemy.IsAlive || !enemy.IsHeld)
            {
                if (enemy != null && enemy.IsHeld)
                    enemy.Release(Vector3.zero);
                return false;
            }

            var deltaTime = Time.deltaTime;
            slider.Speed = Mathf.Min(m_MaxSpeed, slider.Speed + m_Acceleration * deltaTime);
            slider.Z -= slider.Speed * deltaTime;
            slider.X += Mathf.Sin(Time.time * 3f + slider.Seed) * 0.8f * deltaTime;
            slider.X = Mathf.Clamp(slider.X, -m_HalfWidth + 0.4f, m_HalfWidth - 0.4f);
            slider.Yaw += m_SlideSpin * deltaTime;

            var surface = Surface(slider.Z, level);
            enemy.transform.position = m_Track.TransformPoint(new Vector3(slider.X, surface + 0.3f, slider.Z));
            enemy.transform.rotation = Quaternion.Euler(0f, m_Track.eulerAngles.y + slider.Yaw, 0f) * Quaternion.Euler(90f, 0f, 0f);

            Bump(enemy, level);
            if (slider.Z > m_TakeOffZ)
                return true;

            TakeOff(enemy, level);
            return false;
        }

        // La hauteur de la glace (repère de la piste) : la pente, puis le tremplin qui remonte.
        float Surface(float z, int level)
        {
            if (z >= m_KickerStartZ)
                return Mathf.Lerp(m_LowHeight, m_TopHeight, Mathf.InverseLerp(m_KickerStartZ, m_TopZ, z));
            var t = Mathf.InverseLerp(m_KickerStartZ, m_TakeOffZ, z);
            return Mathf.Lerp(m_LowHeight, ValueFor(m_KickerHeightPerLevel, level, 0.7f), t * t);
        }

        void Bump(Enemy slider, int level)
        {
            m_Scratch.Clear();
            m_Scratch.AddRange(EnemiesNear(slider.transform.position, m_BumpRadius, true, slider));
            foreach (var other in m_Scratch)
            {
                if (!Hurt(other, m_BumpDamage, other.Center, other.transform.position - slider.transform.position))
                    continue;

                // Niveau 2 : percuté sur la piste, il glisse aussi.
                var local = m_Track.InverseTransformPoint(other.transform.position);
                if (level >= 2 && !m_Slid.Contains(other) && Mathf.Abs(local.x) <= m_HalfWidth + 0.5f &&
                    local.z > m_KickerStartZ && local.z < m_TopZ + 1f)
                    StartSlide(other, local, level);
                else
                    KnockDown(other, slider.transform.position, 3f, 4f);
            }
        }

        void TakeOff(Enemy enemy, int level)
        {
            var forest = m_Track.TransformDirection(Vector3.back);
            forest.y = 0f;
            forest.Normalize();
            var velocity = forest * ValueFor(m_JumpForwardPerLevel, level, 4f) + Vector3.up * ValueFor(m_JumpLiftPerLevel, level, 8f);
            enemy.Release(velocity, level >= 3, ValueFor(m_JumpGravityPerLevel, level, 0.5f));
            enemy.Tumble(forest * m_Twist + Vector3.up * m_Turn);
            m_Flying.Add(enemy);
            Sfx.Play(m_JumpClip, enemy.transform.position, 0.9f, Random.Range(0.9f, 1.1f));
            Jury(enemy.transform.position, velocity, ValueFor(m_JumpGravityPerLevel, level, 0.5f), level);
        }

        // Les notes du jury, au sommet du saut : trois au hasard ; trois « 10 ! » pour le saut à ski.
        void Jury(Vector3 from, Vector3 velocity, float gravity, int level)
        {
            var g = -Physics.gravity.y * Mathf.Max(0.1f, gravity);
            var rise = velocity.y / g;
            var apex = from + new Vector3(velocity.x, 0f, velocity.z) * rise + Vector3.up * (velocity.y * rise * 0.5f + 1.6f);
            var side = Vector3.Cross(Vector3.up, new Vector3(velocity.x, 0f, velocity.z).normalized);
            var gold = new Color(1f, 0.82f, 0.3f, 1f);
            for (var i = -1; i <= 1; i++)
            {
                var note = level >= 3 ? "10 !" : k_Notes[Random.Range(0, k_Notes.Length)];
                FloatingText.Spawn(apex + side * (i * 1.6f) + Vector3.up * (i == 0 ? 0.5f : 0f), note, gold, level >= 3 ? 2.2f : 1.5f,
                                   Mathf.Max(1.5f, rise + 1f));
            }

            Sfx.Play(m_JuryClip, from, 0.7f, level >= 3 ? 1.2f : 1f);
        }

        void OnLanded(Enemy enemy)
        {
            if (enemy == null || !m_Flying.Remove(enemy) || !enemy.IsAlive)
                return;
            enemy.Stun(m_DizzyTime);
            enemy.Prepare(Preparation.Stunned, m_DizzyTime);
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
