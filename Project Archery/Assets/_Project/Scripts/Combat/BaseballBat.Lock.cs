using Archery.Core;
using Archery.Enemies;
using Archery.Player;
using Archery.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Archery.Combat
{
    /// <summary>
    /// Le verrou de la batte (14 novembre, l'idée du joueur : « on garde la visée automatique, mais on ajoute une visée manuelle :
    /// le joueur vise un ennemi avec sa main libre, et les grenades partent en priorité sur lui, un peu comme Death from the sky,
    /// mais en verrouillant un ennemi »).
    /// </summary>
    /// <remarks>
    /// La main libre (celle qui ne tient pas la batte, qui sort les grenades) vise un ennemi : gâchette à moitié, un anneau pâle
    /// montre lequel ; gâchette à fond, il est verrouillé (un engrenage doré tourne autour de lui, un clic, une vibration). Tant
    /// qu'il vit, toute grenade frappée file d'abord sur lui (avec le multitir, la première ; les autres sur les ennemis autour
    /// de la visée), même s'il est caché : c'est le choix du joueur. Gâchette à fond dans le vide : le verrou est levé. La main qui
    /// tient une grenade, qui vise un menu ou ce qu'elle peut prendre ne verrouille rien. Sans casque : le clic droit verrouille
    /// l'ennemi au centre de l'écran.
    /// </remarks>
    public partial class BaseballBat
    {
        // L'ennemi visé : à 6° au plus de la direction de la main (plus large pour un ennemi proche : 60 cm de part et d'autre de
        // son centre), à 90 m au plus. La gâchette à 15 % montre l'ennemi qu'elle verrouillerait.
        const float k_LockAngle = 6f;
        const float k_LockNear = 0.6f;
        const float k_LockRange = 90f;
        const float k_LockPreviewPress = 0.15f;

        // L'anneau : une roue dentée (10 dents) face au joueur, autour de l'ennemi, qui tourne ; laiton vif sur l'ennemi
        // verrouillé, pâle sur celui que la main vise.
        const int k_RingPoints = 40;
        const int k_RingTeeth = 10;
        const float k_ToothDepth = 0.12f;
        static readonly Color k_LockColor = new Color(1f, 0.62f, 0.18f, 1f);
        static readonly Color k_PreviewColor = new Color(1f, 0.85f, 0.55f, 0.35f);

        Enemy m_Locked;
        Enemy m_Preview;
        Enemy m_LastPreview;
        LineRenderer m_LockRing;
        LineRenderer m_PreviewRing;
        float m_LockRadius;
        float m_PreviewRadius;
        float m_LockSpin;
        float m_LockFlash;

        /// <summary>L'ennemi verrouillé par la main libre (null : aucun).</summary>
        public Enemy Locked => m_Locked != null && m_Locked.IsAlive ? m_Locked : null;

        // La main qui ne tient pas la batte (sans batte en main : celle de l'autre côté que sa place).
        XRBaseInputInteractor FreeHand
        {
            get
            {
                var rig = PlayerRig.Instance;
                if (rig == null)
                    return null;
                var holder = m_Grab != null ? m_Grab.Holder : null;
                var batSide = holder is XRBaseInputInteractor held ? held.handedness
                    : m_Side > 0f ? InteractorHandedness.Right : InteractorHandedness.Left;
                foreach (var hand in rig.Hands)
                {
                    if (hand != null && hand.isActiveAndEnabled && !ReferenceEquals(hand, holder) && hand.handedness != batSide)
                        return hand;
                }

                return null;
            }
        }

        void UpdateLock()
        {
            if (m_Locked != null && !m_Locked.IsAlive)
                SetLock(null);

            m_Preview = null;
            if (!m_Hidden && !SlowMotion.IsPaused)
            {
                if (m_Desktop)
                    UpdateDesktopLock();
                else
                    UpdateHandLock();
            }

            DrawRings(Time.unscaledDeltaTime);
        }

        void UpdateHandLock()
        {
            var hand = FreeHand;
            if (hand == null || hand.hasSelection || hand.hasHover || PointsAtUI(hand))
                return;

            var pressed = hand.activateInput.ReadWasPerformedThisFrame();
            if (!pressed && hand.activateInput.ReadValue() < k_LockPreviewPress)
                return;

            var pointer = hand.transform;
            var candidate = PickLock(pointer.position, pointer.forward);
            if (!pressed)
            {
                m_Preview = candidate;
                return;
            }

            SetLock(candidate);
            Haptics.Pulse(hand, candidate != null ? 0.5f : 0.2f, 0.06f);
        }

        // Sans casque : le clic droit verrouille l'ennemi au centre de l'écran (ou lève le verrou).
        void UpdateDesktopLock()
        {
            var controls = DesktopControls.Instance;
            var camera = controls != null ? controls.CameraTransform : null;
            var mouse = Mouse.current;
            if (camera == null || mouse == null || !controls.HasFocus || controls.RefocusedThisFrame || DesktopHand.IsTypingText)
                return;
            if (mouse.rightButton.wasPressedThisFrame)
                SetLock(PickLock(camera.position, camera.forward));
        }

        // La main vise un menu (son rayon touche une interface) : la gâchette clique, elle ne verrouille pas.
        static bool PointsAtUI(XRBaseInputInteractor hand) =>
            hand is IUIInteractor ui && ui.TryGetUIModel(out var model) && model.currentRaycast.isValid;

        // L'ennemi le plus près de la direction de la main.
        static Enemy PickLock(Vector3 origin, Vector3 forward)
        {
            Enemy best = null;
            var bestScore = float.MaxValue;
            foreach (var enemy in Enemy.Alive)
            {
                if (enemy == null || !enemy.IsAlive)
                    continue;
                var offset = enemy.Center - origin;
                var distance = offset.magnitude;
                if (distance < 0.3f || distance > k_LockRange)
                    continue;

                var allowed = Mathf.Max(k_LockAngle, Mathf.Atan2(k_LockNear, distance) * Mathf.Rad2Deg);
                var score = Vector3.Angle(forward, offset) / allowed;
                if (score <= 1f && score < bestScore)
                {
                    best = enemy;
                    bestScore = score;
                }
            }

            return best;
        }

        void SetLock(Enemy enemy)
        {
            if (enemy == m_Locked)
                return;

            var had = m_Locked != null;
            m_Locked = enemy;
            if (enemy != null)
            {
                m_LockRadius = RadiusOf(enemy);
                m_LockFlash = 1f;
            }

            if (enemy != null || had)
            {
                var theme = UiTheme.Load();
                var view = ViewPose();
                if (theme != null)
                    Sfx.Play(theme.clickClip, view.position + view.forward * 0.4f, theme.clickVolume, enemy != null ? 0.8f : 1.25f, 0f);
            }
        }

        void ClearLock()
        {
            m_Locked = null;
            m_Preview = null;
            if (m_LockRing != null)
                Destroy(m_LockRing.gameObject);
            if (m_PreviewRing != null)
                Destroy(m_PreviewRing.gameObject);
        }

        // Les grenades frappées filent d'abord sur l'ennemi verrouillé.
        void PreferLocked()
        {
            var locked = Locked;
            if (locked == null)
                return;
            s_Targets.Remove(locked);
            s_Targets.Insert(0, locked);
        }

        // La taille de l'ennemi : son rayon à l'horizontale (ou les deux tiers de sa demi-hauteur), plus 15 cm, de 35 cm à 3 m.
        static float RadiusOf(Enemy enemy)
        {
            var found = false;
            var bounds = new Bounds(enemy.Center, Vector3.one * 0.6f);
            foreach (var renderer in enemy.GetComponentsInChildren<Renderer>())
            {
                if (renderer == null || !renderer.enabled || renderer is ParticleSystemRenderer || renderer is LineRenderer || renderer is TrailRenderer)
                    continue;
                if (!found)
                    bounds = renderer.bounds;
                else
                    bounds.Encapsulate(renderer.bounds);
                found = true;
            }

            var extents = bounds.extents;
            return Mathf.Clamp(Mathf.Max(extents.x, extents.z, extents.y * 0.66f) + 0.15f, 0.35f, 3f);
        }

        // ---------------------------------------------------------------- les anneaux

        void DrawRings(float deltaTime)
        {
            m_LockSpin += deltaTime * 70f;
            m_LockFlash = Mathf.MoveTowards(m_LockFlash, 0f, deltaTime * 3f);

            var view = ViewPose();
            var locked = m_Hidden ? null : Locked;
            if (locked != null)
            {
                if (m_LockRing == null)
                    m_LockRing = CreateRing("Verrou de la batte");
                var color = Color.Lerp(k_LockColor, Color.white, m_LockFlash * 0.6f);
                PlaceRing(m_LockRing, locked, m_LockRadius * (1f + 0.25f * m_LockFlash), m_LockSpin, view.position, color, 0.035f + 0.03f * m_LockFlash);
            }
            else if (m_LockRing != null)
            {
                m_LockRing.enabled = false;
            }

            var preview = m_Preview != null && m_Preview != locked ? m_Preview : null;
            if (preview != null)
            {
                if (m_PreviewRing == null)
                    m_PreviewRing = CreateRing("Visée du verrou");
                if (preview != m_LastPreview)
                    m_PreviewRadius = RadiusOf(preview);
                PlaceRing(m_PreviewRing, preview, m_PreviewRadius, -m_LockSpin * 0.5f, view.position, k_PreviewColor, 0.02f);
            }
            else if (m_PreviewRing != null)
            {
                m_PreviewRing.enabled = false;
            }

            m_LastPreview = preview;
        }

        // Une roue dentée de rayon 1 (son transform la met à la taille de l'ennemi), dans le plan XY de son repère.
        LineRenderer CreateRing(string ringName)
        {
            var material = m_Definition != null ? m_Definition.batLockMaterial : null;
            if (material == null)
                return null;

            var ring = new GameObject(ringName).AddComponent<LineRenderer>();
            ring.transform.SetParent(transform, false);
            ring.useWorldSpace = false;
            ring.loop = true;
            ring.alignment = LineAlignment.TransformZ;
            ring.positionCount = k_RingPoints;
            ring.numCornerVertices = 2;
            ring.shadowCastingMode = ShadowCastingMode.Off;
            ring.receiveShadows = false;
            ring.sharedMaterial = material;
            for (var i = 0; i < k_RingPoints; i++)
            {
                var angle = i * Mathf.PI * 2f / k_RingPoints;
                var tooth = i % (k_RingPoints / k_RingTeeth) < k_RingPoints / k_RingTeeth / 2 ? 1f + k_ToothDepth : 1f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * tooth);
            }

            return ring;
        }

        // L'anneau autour de l'ennemi, face au joueur, tourné de spin degrés.
        static void PlaceRing(LineRenderer ring, Enemy enemy, float radius, float spin, Vector3 viewer, Color color, float width)
        {
            if (ring == null)
                return;
            var center = enemy.Center;
            var toViewer = viewer - center;
            if (toViewer.sqrMagnitude < 1e-4f)
                toViewer = Vector3.forward;
            ring.enabled = true;
            ring.transform.SetPositionAndRotation(center, Quaternion.LookRotation(-toViewer.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, spin));
            ring.transform.localScale = Vector3.one * radius;
            ring.startColor = ring.endColor = color;
            ring.widthMultiplier = width;
        }
    }
}
