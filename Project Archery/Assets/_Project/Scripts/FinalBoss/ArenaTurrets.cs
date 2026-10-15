using System.Collections.Generic;
using Archery.Building;
using Archery.Core;
using Archery.Enemies;
using UnityEngine;

namespace Archery.FinalBoss
{
    /// <summary>
    /// Les tourelles du joueur (balistes, tours de givre) rejoignent l'arène du boss final pendant l'intro : sur chaque
    /// quart de cristaux qui s'allume, une tourelle (deux s'il y en a plus de quatre) descend du ciel dans un rayon de
    /// lumière, sur un emplacement du plateau (cases T du plan), de préférence dans le quart qui s'allume. Elles attendent
    /// le début du combat pour tirer, portent plus loin (Range Scale), et retrouvent leur place sur la carte quand l'arène
    /// se ferme. Ici, les tours de givre tirent des boules de glace (<see cref="IceBall"/>) au lieu de ralentir autour
    /// d'elles, et se relaient pour tirer à intervalles réguliers.
    /// </summary>
    /// <remarks>Piloté par l'<see cref="ArenaDirector"/>, sur le même objet. Au plus une tourelle par emplacement : les plus fortes d'abord.</remarks>
    [DisallowMultipleComponent]
    public class ArenaTurrets : MonoBehaviour
    {
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("Matériau du rayon de lumière (Light_Beam, shader Archery/LightBeam).")]
        [SerializeField]
        Material m_BeamMaterial;

        [Tooltip("Matériau des étincelles (Fire_Particles).")]
        [SerializeField]
        Material m_SparkMaterial;

        [Tooltip("Couleur du rayon et des étincelles.")]
        [SerializeField]
        Color m_Color = new Color(0.55f, 0.85f, 1f, 1f);

        [Tooltip("Hauteur (m) d'où descendent les tourelles.")]
        [SerializeField]
        float m_DropHeight = 18f;

        [Tooltip("Durée (s) de la descente.")]
        [SerializeField]
        float m_DropTime = 0.6f;

        [Tooltip("Multiplie la portée des tourelles dans l'arène : du haut du plateau, elles doivent atteindre le dragon.")]
        [SerializeField]
        float m_RangeScale = 2f;

        [Tooltip("Optionnel : son de l'arrivée d'une tourelle.")]
        [SerializeField]
        AudioClip m_ArriveClip;

        [Header("Tours de givre : boules de glace")]
        [Tooltip("Matériau des boules de glace (Frost_Ball, shader Archery/Fireball en bleu).")]
        [SerializeField]
        Material m_IceBallMaterial;

        [Tooltip("Couleur des étincelles et de la lueur des boules de glace.")]
        [SerializeField]
        Color m_IceColor = new Color(0.6f, 0.9f, 1f, 1f);

        [Tooltip("Vitesse (m/s) des boules de glace.")]
        [SerializeField]
        float m_IceBallSpeed = 28f;

        [Tooltip("Diamètre (m) des boules de glace.")]
        [SerializeField]
        float m_IceBallSize = 0.6f;

        [Tooltip("Optionnel : son du tir.")]
        [SerializeField]
        AudioClip m_IceShotClip;

        [Tooltip("Optionnel : son quand une boule de glace éclate.")]
        [SerializeField]
        AudioClip m_IceHitClip;

        class Moved
        {
            public Structure Turret;
            public Vector3 Position;
            public Quaternion Rotation;
            public bool WasEnabled;
            public Pose Slot;
            public int Quarter;
            public float StartTime = -1f;
            public bool Landed;
            public Transform Beam;
            public ParticleSystem Trail;
        }

        readonly List<Moved> m_Moved = new List<Moved>();
        readonly List<FrostTower> m_Frost = new List<FrostTower>();
        MaterialPropertyBlock m_Block;
        float m_FloorY;
        float m_FrostTimer = 0.5f;
        int m_FrostNext;
        bool m_Fighting;

        /// <summary>Choisit les tourelles qui viendront et leur emplacement. À appeler à l'arrivée du joueur dans l'arène.</summary>
        public void Prepare(ArenaShape arena)
        {
            if (m_Moved.Count > 0 || !(arena is ArenaGridBuilder grid))
                return;

            m_FloorY = arena.FloorY;
            var slots = new List<Pose>(grid.TurretSlots);
            var turrets = new List<Structure>();
            foreach (var structure in FindObjectsByType<Structure>())
            {
                if (structure != null && structure.IsBuilt && !structure.IsUnderConstruction && (structure is Ballista || structure is FrostTower))
                    turrets.Add(structure);
            }

            // Les plus fortes d'abord, s'il y a plus de tourelles que d'emplacements.
            turrets.Sort((a, b) => b.Level.CompareTo(a.Level));
            var count = Mathf.Min(turrets.Count, slots.Count);
            var quarters = CeilingCrystals.GroupCount;
            var next = 0;
            for (var quarter = 0; quarter < quarters && next < count; quarter++)
            {
                // Une par quart, deux s'il y en a beaucoup : réparties sur les quatre quarts, en commençant par le premier.
                var share = Mathf.CeilToInt((quarter + 1) * count / (float)quarters) - Mathf.CeilToInt(quarter * count / (float)quarters);
                for (var k = 0; k < share && next < count; k++, next++)
                {
                    var turret = turrets[next];
                    m_Moved.Add(new Moved
                    {
                        Turret = turret,
                        Position = turret.transform.position,
                        Rotation = turret.transform.rotation,
                        WasEnabled = turret.enabled,
                        Slot = TakeSlot(slots, quarter, arena),
                        Quarter = quarter,
                    });
                }
            }
        }

        // Un emplacement libre dans le quart qui s'allume, sinon dans le quart le plus proche.
        static Pose TakeSlot(List<Pose> slots, int quarter, ArenaShape arena)
        {
            var quarters = CeilingCrystals.GroupCount;
            var best = 0;
            var bestGap = int.MaxValue;
            for (var i = 0; i < slots.Count; i++)
            {
                var local = arena.transform.InverseTransformPoint(slots[i].position);
                var gap = Mathf.Abs(CeilingCrystals.GroupOf(Mathf.Atan2(local.x, local.z)) - quarter);
                gap = Mathf.Min(gap, quarters - gap);
                if (gap >= bestGap)
                    continue;
                best = i;
                bestGap = gap;
            }

            var slot = slots[best];
            slots.RemoveAt(best);
            return slot;
        }

        /// <summary>Un quart de cristaux s'allume : ses tourelles descendent du ciel.</summary>
        public void Arrive(int quarter)
        {
            foreach (var moved in m_Moved)
            {
                if (moved.Quarter == quarter && moved.StartTime < 0f)
                    Begin(moved);
            }
        }

        /// <summary>Intro écourtée : toutes les tourelles qui manquent arrivent.</summary>
        public void ArriveAll()
        {
            foreach (var moved in m_Moved)
            {
                if (moved.StartTime < 0f)
                    Begin(moved);
            }
        }

        /// <summary>Le combat commence : les tourelles arrivées se mettent à tirer (les autres dès leur arrivée).</summary>
        public void StartFighting()
        {
            m_Fighting = true;
            foreach (var moved in m_Moved)
            {
                if (moved.Turret != null && moved.Landed)
                    moved.Turret.enabled = moved.WasEnabled;
            }
        }

        void Begin(Moved moved)
        {
            var turret = moved.Turret;
            if (turret == null)
                return;

            // Elle quitte la carte et apparaît haut dans le ciel, au-dessus de son emplacement, dans un rayon de lumière.
            moved.StartTime = Time.time;
            turret.enabled = false;
            turret.RangeScale = m_RangeScale;
            turret.Elevation = Mathf.Max(0f, moved.Slot.position.y - m_FloorY);
            if (turret is FrostTower frost)
            {
                // Ici, la tour de givre tire des boules de glace, à tour de rôle avec les autres.
                frost.FiresIceBalls = true;
                m_Frost.Add(frost);
            }

            turret.transform.SetPositionAndRotation(moved.Slot.position + Vector3.up * m_DropHeight, moved.Slot.rotation);
            moved.Beam = CreateBeam(moved.Slot.position);
            if (m_SparkMaterial != null)
                moved.Trail = FireEffects.CreateSparkTrail(turret.transform, m_SparkMaterial, 0.8f, m_Color);
        }

        void Update()
        {
            foreach (var moved in m_Moved)
            {
                if (moved.StartTime < 0f || moved.Turret == null)
                    continue;

                var age = Time.time - moved.StartTime;
                if (!moved.Landed)
                {
                    // Elle descend le long du rayon, de plus en plus vite.
                    var t = Mathf.Clamp01(age / Mathf.Max(0.05f, m_DropTime));
                    moved.Turret.transform.position = moved.Slot.position + Vector3.up * (m_DropHeight * (1f - t * t));
                    if (t >= 1f)
                        Land(moved);
                }

                UpdateBeam(moved, age);
            }

            UpdateFrost(Time.deltaTime);
        }

        // Les tours de givre se relaient : une boule de glace à intervalles réguliers (l'intervalle d'une tour divisé par
        // le nombre de tours), chacune son tour. Si celle dont c'est le tour ne voit pas le dragon, la suivante tire.
        void UpdateFrost(float deltaTime)
        {
            if (!m_Fighting || m_Frost.Count == 0)
                return;

            m_FrostTimer -= deltaTime;
            if (m_FrostTimer > 0f)
                return;

            var ready = 0;
            var intervals = 0f;
            foreach (var tower in m_Frost)
            {
                if (!IsReady(tower))
                    continue;
                ready++;
                intervals += tower.IceBallInterval;
            }

            var fired = false;
            for (var tries = 0; tries < m_Frost.Count && !fired; tries++)
            {
                var tower = m_Frost[m_FrostNext];
                m_FrostNext = (m_FrostNext + 1) % m_Frost.Count;
                fired = IsReady(tower) && FireIceBall(tower);
            }

            m_FrostTimer = fired && ready > 0 ? intervals / ready / ready : 0.25f;
        }

        static bool IsReady(FrostTower tower) => tower != null && tower.isActiveAndEnabled && tower.IsBuilt;

        bool FireIceBall(FrostTower tower)
        {
            if (!tower.TryFindIceBallTarget(out var target))
                return false;

            // Comme la baliste, elle vise un peu devant le dragon s'il bouge.
            var muzzle = tower.IceBallMuzzle;
            var aim = target.Center;
            if (target.TryGetComponent<EnemyBehaviour>(out var behaviour))
                aim += behaviour.Velocity * (Vector3.Distance(muzzle, aim) / Mathf.Max(1f, m_IceBallSpeed));
            var direction = aim - muzzle;
            if (direction.sqrMagnitude < 1e-4f)
                return false;

            IceBall.Launch(muzzle, direction.normalized * m_IceBallSpeed, m_IceBallSize, tower.IceBallSlow, tower.IceBallSlowTime,
                           tower.transform, m_IceBallMaterial, m_SparkMaterial, m_IceColor, m_IceHitClip);
            Sfx.Play(m_IceShotClip, muzzle, 0.8f, Random.Range(1.1f, 1.25f));
            return true;
        }

        /// <summary>Une tourelle vient de se poser sur le plateau, à cet emplacement (une rune s'y allume).</summary>
        public event System.Action<Vector3> TurretLanded;

        // Elle touche le plateau : une gerbe d'étincelles, et le rayon s'éteint.
        void Land(Moved moved)
        {
            moved.Landed = true;
            TurretLanded?.Invoke(moved.Slot.position);
            if (moved.Trail != null)
            {
                moved.Trail.transform.SetParent(null, true);
                moved.Trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(moved.Trail.gameObject, 1f);
                moved.Trail = null;
            }

            if (m_SparkMaterial != null)
                FireEffects.SparkBurst(moved.Slot.position + Vector3.up * 0.3f, m_SparkMaterial, 2f, 60, m_Color);
            Sfx.Play(m_ArriveClip, moved.Slot.position, 1f, Random.Range(0.95f, 1.05f));
            ArenaPlayer.Rumble(0.2f, 0.1f);
            if (m_Fighting)
                moved.Turret.enabled = moved.WasEnabled;
        }

        // Le rayon s'allume vite, reste pendant la descente, s'élargit à l'arrivée puis s'éteint.
        void UpdateBeam(Moved moved, float age)
        {
            if (moved.Beam == null)
                return;

            var fadeIn = Mathf.Clamp01(age / 0.15f);
            var fadeOut = 1f - Mathf.Clamp01((age - m_DropTime - 0.1f) / 0.8f);
            if (fadeOut <= 0f)
            {
                DestroyBeam(moved);
                return;
            }

            m_Block ??= new MaterialPropertyBlock();
            var renderer = moved.Beam.GetComponent<MeshRenderer>();
            renderer.GetPropertyBlock(m_Block);
            var color = m_Color;
            color.a = fadeIn * fadeOut;
            m_Block.SetColor(k_BaseColorId, color);
            renderer.SetPropertyBlock(m_Block);

            var width = 2.2f * (1f + 0.5f * Mathf.Clamp01((age - m_DropTime) / 0.3f));
            moved.Beam.localScale = new Vector3(width, moved.Beam.localScale.y, width);
        }

        Transform CreateBeam(Vector3 ground)
        {
            if (m_BeamMaterial == null)
                return null;

            var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.name = "Rayon de téléportation";
            DestroyImmediate(beam.GetComponent<Collider>());
            var height = m_DropHeight + 4f;
            beam.transform.position = ground + Vector3.up * (height * 0.5f);
            beam.transform.localScale = new Vector3(2.2f, height * 0.5f, 2.2f);
            var renderer = beam.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = m_BeamMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return beam.transform;
        }

        void DestroyBeam(Moved moved)
        {
            if (moved.Beam == null)
                return;
            Destroy(moved.Beam.gameObject);
            moved.Beam = null;
        }

        // L'arène se ferme (victoire, mort) : chaque tourelle retrouve sa place sur la carte, telle qu'elle était.
        void OnDestroy()
        {
            foreach (var moved in m_Moved)
            {
                DestroyBeam(moved);
                if (moved.Trail != null)
                    Destroy(moved.Trail.gameObject);
                if (moved.Turret == null || moved.StartTime < 0f)
                    continue;

                moved.Turret.transform.SetPositionAndRotation(moved.Position, moved.Rotation);
                moved.Turret.RangeScale = 1f;
                moved.Turret.Elevation = 0f;
                if (moved.Turret is FrostTower frost)
                    frost.FiresIceBalls = false;
                moved.Turret.enabled = moved.WasEnabled;
            }

            m_Moved.Clear();
            m_Frost.Clear();
        }
    }
}
