using System;
using System.Collections.Generic;
using Archery.Bows;
using Archery.Core;
using Archery.Player;
using Archery.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Archery.Upgrades
{
    /// <summary>
    /// La corde fantôme, refaite le 12 novembre (le joueur : « la corde fantôme n'est pas satisfaisante. Une flèche transparente est
    /// ancrée dans la corde après en avoir tiré une, posée comme si on chargeait l'arc avec elle à 0 % ; on peut la prendre puis tendre
    /// la corde comme les flèches classiques, et elle part avec la même puissance que la dernière (parfait, etc.), mais toujours avec
    /// un bonus de dégâts. De plus, on peut en tirer 1, 2, à l'infini pour chaque niveau de la corde fantôme »).
    /// </summary>
    /// <remarks>
    /// <para>Après un tir de l'arc (pas des arbalètes ni du katana), une flèche du pool, à l'allure d'un hologramme bleu pâle, se pose
    /// sur la corde, tournée là où l'arc vise. La main libre qui se ferme près de la corde la prend (<see cref="Arrow.TryPutInHand"/>) :
    /// l'arc l'encoche comme une flèche du carquois, on la tend, on la lâche. Au lâcher, l'arc reprend la qualité et la tension du
    /// dernier tir (<see cref="Bow.ForcedShotFor"/>), dégâts +20 % : il faut tendre la corde, mais l'anneau ne compte pas (tout vert
    /// si le dernier tir était parfait). C'est un vrai tir de l'arc : le multitir, l'écho, les pièces jouent.</para>
    /// <para>Par tir de l'arc : 1 flèche fantôme au niveau 1, 2 au niveau 2, sans fin au niveau 3 (la suivante se pose dès que la
    /// précédente part, toujours comme le dernier vrai tir). Une vraie flèche encochée efface la fantôme ; son tir en redonne.
    /// Lâchée sans tir, la fantôme revient sur la corde. Sans casque : le clic du milieu la tire, droit devant l'arc.</para>
    /// <para>Avant (du 26 octobre au 12 novembre) : dans les 1,2 s après un tir, tirer la corde vide d'au moins 30 cm lançait une
    /// flèche fantôme invisible avant son départ, à 120, 150 ou 200 % des dégâts du tir selon le niveau.</para>
    /// </remarks>
    public partial class ArrowPieces
    {
        // Les flèches fantômes par tir de l'arc, selon le niveau de la corde : 1, 2, sans fin.
        static readonly int[] k_GhostShots = { 1, 2, int.MaxValue };

        // Le bonus de dégâts de la flèche fantôme (les niveaux donnent maintenant plus de flèches, plus de dégâts) : +20 %, le bonus du
        // niveau 1 d'avant.
        const float k_GhostBonus = 1.2f;

        // Son allure : le shader des hologrammes (Archery/Hologram, celui des hologrammes de l'arène de MK), bleu pâle, ses bords
        // un peu plus vifs, des lignes qui défilent.
        static readonly Color k_GhostGlow = new Color(0.35f, 0.95f, 1.4f, 1f);

        sealed class GhostLook
        {
            public Renderer[] Renderers;
            public Material[][] Materials;
        }

        int m_GhostLeft;
        ShotGrade m_GhostGrade;
        float m_GhostRatio;
        float m_GhostSpeed;
        float m_GhostDamage;
        float m_GhostGravity;
        Arrow m_GhostArrow;
        Material m_GhostMaterial;
        readonly HashSet<Arrow> m_GhostArrows = new HashSet<Arrow>();
        readonly Dictionary<Arrow, GhostLook> m_GhostLooks = new Dictionary<Arrow, GhostLook>();
        readonly List<Arrow> m_GhostRestore = new List<Arrow>();
        readonly List<Renderer> m_GhostRenderers = new List<Renderer>();
        readonly List<Material[]> m_GhostSaved = new List<Material[]>();

        /// <summary>Cette flèche est une flèche fantôme (posée sur la corde, en main, ou en vol).</summary>
        public bool IsGhost(Arrow arrow) => arrow != null && m_GhostArrows.Contains(arrow);

        void EnableGhost()
        {
            Bow.ForcedShotFor = GhostShot;
            ArrowPool.Taken += OnGhostTaken;
        }

        void ClearGhost()
        {
            if (Bow.ForcedShotFor == (Func<Arrow, Bow.ForcedShot?>)GhostShot)
                Bow.ForcedShotFor = null;
            ArrowPool.Taken -= OnGhostTaken;

            if (m_GhostArrow != null && m_GhostArrow.CurrentState == Arrow.State.Pooled)
                m_GhostArrow.Despawn();
            m_GhostArrow = null;
            m_GhostRestore.Clear();
            m_GhostRestore.AddRange(m_GhostLooks.Keys);
            foreach (var arrow in m_GhostRestore)
                RestoreGhostLook(arrow);
            m_GhostRestore.Clear();
            m_GhostArrows.Clear();
            m_GhostLeft = 0;
        }

        // Le tir imposé d'une flèche fantôme : la qualité et la tension du dernier tir de l'arc, dégâts +20 %.
        Bow.ForcedShot? GhostShot(Arrow arrow) =>
            IsGhost(arrow)
                ? new Bow.ForcedShot { Grade = m_GhostGrade, Ratio = m_GhostRatio, DamageMultiplier = k_GhostBonus }
                : (Bow.ForcedShot?)null;

        // Un tir de l'arc : une flèche fantôme part (une de moins, sauf sans fin), ou un vrai tir pose les flèches fantômes de son niveau.
        void OnGhostShotStart(in ShotInfo shot)
        {
            if (shot.Arrow != null && m_GhostArrows.Contains(shot.Arrow))
            {
                if (m_GhostLeft != int.MaxValue)
                    m_GhostLeft = Mathf.Max(0, m_GhostLeft - 1);
                if (shot.Arrow == m_GhostArrow)
                    m_GhostArrow = null;
                return;
            }

            var level = Level(UpgradeEffect.Ghost);
            if (level <= 0 || shot.Arrow == null)
                return;
            if (m_Bow == null)
                m_Bow = FindAnyObjectByType<Bow>();
            if (m_Bow == null || shot.Aim == null || !shot.Aim.IsChildOf(m_Bow.transform))
                return;

            m_GhostLeft = k_GhostShots[Mathf.Clamp(level, 1, k_GhostShots.Length) - 1];
            m_GhostGrade = shot.Grade;
            m_GhostRatio = shot.DrawRatio;
            m_GhostSpeed = shot.Speed;
            m_GhostDamage = shot.Damage;
            m_GhostGravity = shot.Arrow.GravityScale;
        }

        // Chaque image : la flèche fantôme posée sur la corde suit l'arc ; la main libre la prend ; lâchée sans tir, elle revient.
        void UpdateGhost()
        {
            if (Level(UpgradeEffect.Ghost) <= 0)
                m_GhostLeft = 0;
            if (m_Bow == null && m_GhostLeft > 0)
                m_Bow = FindAnyObjectByType<Bow>();
            if (m_GhostLeft <= 0 || m_Bow == null || m_Bow.IsReplaced || m_Bow.BowHand == null)
            {
                HideGhost();
                return;
            }

            var desktop = DesktopControls.Instance;
            if (desktop != null && desktop.IsActive)
            {
                HideGhost();
                UpdateGhostDesktop();
                return;
            }

            if (m_GhostArrow != null)
            {
                var state = m_GhostArrow.CurrentState;
                if (!m_GhostArrow.gameObject.activeInHierarchy)
                {
                    ForgetGhost(m_GhostArrow);
                    m_GhostArrow = null;
                }
                else if (state == Arrow.State.Held || state == Arrow.State.Nocked)
                {
                    // En main ou encochée : l'arc s'en occupe.
                    return;
                }
                else if (state != Arrow.State.Pooled)
                {
                    // Lâchée sans tir (elle tombe) : elle disparaît, et une autre se pose sur la corde.
                    var dropped = m_GhostArrow;
                    m_GhostArrow = null;
                    ForgetGhost(dropped);
                    dropped.Despawn();
                }
            }

            // Une vraie flèche encochée : la fantôme s'efface (le tir de la vraie en redonnera).
            if (m_Bow.HasArrow)
            {
                HideGhost();
                return;
            }

            if (m_GhostArrow == null && !SpawnGhost())
                return;

            var nock = m_Bow.StringPoint;
            m_GhostArrow.SetNockedPose(nock, Quaternion.LookRotation(m_Bow.AimForward, m_Bow.transform.up));
            TryTakeGhost(nock);
        }

        bool SpawnGhost()
        {
            var pool = ArrowPool.Instance;
            if (pool == null)
                return false;

            var arrow = pool.Get();
            if (arrow == null)
                return false;

            m_GhostArrow = arrow;
            m_GhostArrows.Add(arrow);
            ApplyGhostLook(arrow);
            Sfx.Play(PieceSounds.Whoosh, m_Bow.StringPoint, 0.3f, 1.7f);
            Haptics.Pulse(m_Bow.BowHand, 0.12f, 0.04f);
            return true;
        }

        // La main libre (pas celle de l'arc, sans rien dedans) qui se ferme près de la corde prend la flèche fantôme ; l'arc l'encoche.
        void TryTakeGhost(Vector3 nock)
        {
            var rig = PlayerRig.Instance;
            if (rig == null)
                return;

            foreach (var hand in rig.Hands)
            {
                if (hand == null || hand.selectInput == null || hand.hasSelection || ReferenceEquals(hand, m_Bow.BowHand) ||
                    !hand.selectInput.ReadWasPerformedThisFrame() || Vector3.Distance(hand.transform.position, nock) > k_GhostGrab)
                    continue;

                if (m_GhostArrow.TryPutInHand(hand))
                    Haptics.Pulse(hand, 0.3f, 0.04f);
                return;
            }
        }

        // Elle s'efface (rendue au pool) si elle attend sur la corde ; en main, elle reste.
        void HideGhost()
        {
            if (m_GhostArrow == null)
                return;
            var state = m_GhostArrow.CurrentState;
            if (state == Arrow.State.Held || state == Arrow.State.Nocked)
                return;

            var arrow = m_GhostArrow;
            m_GhostArrow = null;
            ForgetGhost(arrow);
            if (arrow.gameObject.activeInHierarchy && state == Arrow.State.Pooled)
                arrow.Despawn();
        }

        // Sans casque : le clic du milieu tire la flèche fantôme, droit devant l'arc, comme le dernier tir (+20 %).
        void UpdateGhostDesktop()
        {
            var mouse = Mouse.current;
            var arrows = SpecialArrows.Instance;
            if (mouse == null || !mouse.middleButton.wasPressedThisFrame || arrows == null)
                return;

            var direction = m_Bow.AimForward;
            var origin = m_Bow.StringPoint + direction * 0.45f;
            var arrow = arrows.LaunchExtraShot(origin, direction, m_GhostSpeed, m_GhostDamage * k_GhostBonus, m_GhostGrade, k_GhostColor,
                                               m_GhostGravity);
            if (arrow == null)
                return;

            if (m_GhostLeft != int.MaxValue)
                m_GhostLeft = Mathf.Max(0, m_GhostLeft - 1);
            Sfx.Play(PieceSounds.Whoosh, origin, 0.8f, 1.35f);
            FloatingText.Spawn(origin + Vector3.up * 0.2f, "Fantôme !", k_GhostColor, 0.45f, 0.7f);
        }

        // Une flèche fantôme qui ne l'est plus (lâchée sans tir, effacée) : elle retrouve son allure.
        void ForgetGhost(Arrow arrow)
        {
            if (arrow == null)
                return;
            m_GhostArrows.Remove(arrow);
            RestoreGhostLook(arrow);
        }

        // Une flèche fantôme tirée garde son allure jusqu'à ce que le pool la redonne (plantée, elle reste fantôme) ; une flèche
        // reprise au pool retrouve la sienne.
        void OnGhostTaken(Arrow arrow)
        {
            if (arrow == null)
                return;
            m_GhostArrows.Remove(arrow);
            RestoreGhostLook(arrow);
        }

        void ApplyGhostLook(Arrow arrow)
        {
            var material = GhostMaterial();
            if (material == null || m_GhostLooks.ContainsKey(arrow))
                return;

            m_GhostRenderers.Clear();
            m_GhostSaved.Clear();
            foreach (var renderer in arrow.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer is TrailRenderer || renderer is ParticleSystemRenderer || renderer is LineRenderer)
                    continue;

                var saved = renderer.sharedMaterials;
                var ghost = new Material[saved.Length];
                for (var i = 0; i < ghost.Length; i++)
                    ghost[i] = material;
                m_GhostRenderers.Add(renderer);
                m_GhostSaved.Add(saved);
                renderer.sharedMaterials = ghost;
            }

            m_GhostLooks[arrow] = new GhostLook { Renderers = m_GhostRenderers.ToArray(), Materials = m_GhostSaved.ToArray() };
        }

        void RestoreGhostLook(Arrow arrow)
        {
            if (arrow == null || !m_GhostLooks.TryGetValue(arrow, out var look))
                return;

            for (var i = 0; i < look.Renderers.Length; i++)
            {
                if (look.Renderers[i] != null)
                    look.Renderers[i].sharedMaterials = look.Materials[i];
            }

            m_GhostLooks.Remove(arrow);
        }

        Material GhostMaterial()
        {
            if (m_GhostMaterial != null)
                return m_GhostMaterial;

            var shader = Shader.Find("Archery/Hologram");
            if (shader == null)
                return null;

            m_GhostMaterial = new Material(shader) { name = "Flèche fantôme" };
            m_GhostMaterial.SetColor("_Color", k_GhostGlow);
            m_GhostMaterial.SetFloat("_Intensity", 1.1f);
            m_GhostMaterial.SetFloat("_ScanDensity", 14f);
            m_GhostMaterial.SetFloat("_ScanSpeed", 0.8f);
            m_GhostMaterial.SetFloat("_ScanDepth", 0.35f);
            m_GhostMaterial.SetFloat("_Flicker", 0.12f);
            m_GhostMaterial.SetFloat("_Fresnel", 0.35f);
            return m_GhostMaterial;
        }
    }
}
