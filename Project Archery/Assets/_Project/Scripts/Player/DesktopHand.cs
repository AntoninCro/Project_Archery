using System.Collections.Generic;
using System.Text.RegularExpressions;
using Archery.Bows;
using Archery.Building;
using Archery.Chests;
using Archery.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Player
{
    /// <summary>
    /// Sans casque : la main de la touche E. Elle vise le centre de l'écran, comme le rayon d'une manette, et E fait ce
    /// que fait la gâchette de prise en VR : récolter un gisement, ouvrir un coffre, prendre une orbe, ouvrir le panneau
    /// d'un socle. Elle clique aussi les boutons des panneaux (boutique, construction, menus) et, pendant une
    /// construction, donne les coups de marteau sur le plan.
    /// </summary>
    /// <remarks>
    /// C'est un interacteur XRI : les objets la traitent comme une main, avec les mêmes règles (distance, état).
    /// Créée sous la caméra par <see cref="DesktopControls"/>, active seulement sans casque. Le marteau flotte à son
    /// point d'attache, en bas à droite de l'écran.
    /// </remarks>
    [AddComponentMenu("Archery/Desktop Hand")]
    [DisallowMultipleComponent]
    public class DesktopHand : XRBaseInputInteractor
    {
        [Tooltip("Distance maximale (m) du viseur. Chaque objet garde sa propre portée (5 m pour un gisement ou un socle).")]
        [SerializeField]
        float m_MaxDistance = 12f;

        [Tooltip("Distance maximale (m) entre les yeux et le plan pour le frapper au marteau.")]
        [SerializeField]
        float m_HammerReach = 3.5f;

        [Tooltip("Place du marteau par rapport à la caméra (X à droite, Y en haut, Z devant).")]
        [SerializeField]
        Vector3 m_HammerOffset = new Vector3(0.3f, -0.38f, 0.5f);

        [Tooltip("Orientation du marteau au repos (°), par rapport à la caméra.")]
        [SerializeField]
        Vector3 m_HammerEuler = new Vector3(-15f, -20f, 0f);

        [Tooltip("Angle (°) dont le marteau s'abat à chaque coup.")]
        [SerializeField]
        float m_SwingAngle = 80f;

        [Tooltip("Durée (s) d'un coup de marteau, aller et retour. Le plan est frappé au tiers.")]
        [SerializeField]
        float m_SwingTime = 0.36f;

        const float k_StrikeAt = 0.35f;
        const float k_FarPromptDistance = 4.5f;

        static readonly RaycastHit[] s_Hits = new RaycastHit[32];
        static readonly Selectable[] s_Selectables = new Selectable[256];
        static readonly Regex s_Tags = new Regex("<[^>]*>");
        static readonly List<RaycastHit> s_Sorted = new List<RaycastHit>();

        DesktopCrosshair m_Crosshair;
        Transform m_HammerHold;
        PointerEventData m_Pointer;
        IXRSelectInteractable m_Target;
        Selectable m_Hovered;
        bool m_SelectNow;
        float m_Swing = -1f;
        bool m_StrikePending;
        Vector3 m_StrikePoint;

        /// <summary>Un champ de texte a le clavier (nom du classement) : les touches écrivent au lieu de jouer.</summary>
        public static bool IsTypingText
        {
            get
            {
                var system = EventSystem.current;
                var selected = system != null ? system.currentSelectedGameObject : null;
                return selected != null && selected.TryGetComponent<TMP_InputField>(out var field) && field.isFocused;
            }
        }

        // La gâchette, c'est la touche E : elle n'est « appuyée » que l'image où on la presse, sur l'objet visé.
        public override bool isSelectActive => m_SelectNow;

        public override bool isHoverActive => base.isHoverActive && m_Target != null;

        protected override void Awake()
        {
            base.Awake();

            // Le marteau flotte à ce point d'attache : en bas à droite de l'écran.
            var generated = attachTransform;
            m_HammerHold = new GameObject("Hammer Hold").transform;
            m_HammerHold.SetParent(transform, false);
            attachTransform = m_HammerHold;
            if (generated != null && generated != transform)
                Destroy(generated.gameObject);

            m_Crosshair = DesktopCrosshair.Create(transform);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (m_Crosshair != null)
                m_Crosshair.SetVisible(true);
        }

        protected override void OnDisable()
        {
            SetHovered(null);
            m_Target = null;
            m_SelectNow = false;
            m_Swing = -1f;
            if (m_Crosshair != null)
                m_Crosshair.SetVisible(false);
            base.OnDisable();
        }

        public override void GetValidTargets(List<IXRInteractable> targets)
        {
            targets.Clear();
            if (m_Target != null)
                targets.Add(m_Target);
        }

        public override bool CanSelect(IXRSelectInteractable interactable) => base.CanSelect(interactable) && interactable == m_Target;

        // Avant que XRI cherche les cibles : on regarde ce qui est visé et si E vient d'être pressée.
        public override void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.PreprocessInteractor(updatePhase);
            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
                Think();
        }

        void Update() => UpdateHammer(Time.unscaledDeltaTime);

        void Think()
        {
            m_SelectNow = false;
            var controls = DesktopControls.Instance;
            var typing = IsTypingText;
            var focus = controls != null && controls.HasFocus && !typing;
            m_Crosshair.SetCenterMessage(controls != null && !controls.HasFocus ? "Clic gauche pour jouer" : "");
            if (!focus)
            {
                m_Target = null;
                SetHovered(null);
                m_Crosshair.SetPrompt(null, false);
                return;
            }

            var keyboard = Keyboard.current;
            var pressed = keyboard != null && keyboard.eKey.wasPressedThisFrame;
            var held = keyboard != null && keyboard.eKey.isPressed;
            var ray = new Ray(transform.position, transform.forward);

            // Ce qui est visé : un objet interactif (ou le mur qui cache la suite), un bouton de panneau, le plan à
            // construire. Le plus proche l'emporte.
            var interactable = FindInteractable(ray, out var interactableDistance, out var blockDistance);
            var selectable = FindSelectable(ray, Mathf.Min(m_MaxDistance, blockDistance + 0.15f), out var selectableDistance, out var selectablePoint);
            var hammer = BuildManager.Instance != null ? BuildManager.Instance.Hammer : null;
            var onBlueprint = TryAimBlueprint(ray, hammer, blockDistance, out var blueprintDistance, out var blueprintPoint);

            m_Target = null;
            if (selectable != null && (!onBlueprint || selectableDistance <= blueprintDistance) &&
                (interactable == null || selectableDistance <= interactableDistance + 0.15f))
            {
                SetHovered(selectable);
                UseSelectable(selectable, selectablePoint, pressed, held);
                return;
            }

            SetHovered(null);
            if (onBlueprint && (interactable == null || blueprintDistance <= interactableDistance))
            {
                var inReach = blueprintDistance <= m_HammerReach;
                var manager = BuildManager.Instance;
                m_Crosshair.SetPrompt(inReach ? $"Frapper le plan ({manager.Strikes}/{manager.StrikesToBuild})" : "Approche-toi du plan", inReach);
                if (pressed)
                    Swing(inReach, blueprintPoint);
                return;
            }

            if (interactable != null)
            {
                var ready = interactable.IsSelectableBy(this);
                if (ready)
                {
                    m_Target = interactable;
                    m_Crosshair.SetPrompt(Describe(interactable), true);
                    m_SelectNow = pressed;
                }
                else
                {
                    // Trop loin, ou rien à faire pour l'instant (coffre déjà ouvert, construction en cours…).
                    m_Crosshair.SetPrompt(interactableDistance > k_FarPromptDistance ? Describe(interactable) + " (approche-toi)" : null, false);
                }

                return;
            }

            m_Crosshair.SetPrompt(hammer != null ? "Vise le plan bleu pour le frapper" : null, false);
            if (pressed && hammer != null)
                Swing(false, Vector3.zero);
        }

        // ---- Objets interactifs (gisements, socles, coffres, orbes) : le premier touché par le rayon du viseur.

        IXRSelectInteractable FindInteractable(Ray ray, out float distance, out float blockDistance)
        {
            distance = float.PositiveInfinity;
            blockDistance = m_MaxDistance;
            var count = Physics.RaycastNonAlloc(ray, s_Hits, m_MaxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            s_Sorted.Clear();
            for (var i = 0; i < count; i++)
                s_Sorted.Add(s_Hits[i]);
            s_Sorted.Sort((a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in s_Sorted)
            {
                var collider = hit.collider;
                if (collider == null)
                    continue;

                if (interactionManager != null && interactionManager.TryGetInteractableForCollider(collider, out var found))
                {
                    // L'arc, les flèches et le marteau se tiennent, ils ne se « visent » pas : on regarde au travers.
                    if (found is IXRSelectInteractable selectable && !(found is XRGrabInteractable) && !(found is Arrow))
                    {
                        distance = hit.distance;
                        blockDistance = hit.distance;
                        return selectable;
                    }

                    continue;
                }

                // Le corps du joueur, ce qu'il tient, les zones invisibles : on regarde au travers.
                if (collider.isTrigger || ArrowIgnore.IsIgnored(collider) || collider.GetComponentInParent<BuildHammer>() != null)
                    continue;

                blockDistance = hit.distance;
                return null;
            }

            return null;
        }

        static string Describe(IXRSelectInteractable interactable)
        {
            switch (interactable)
            {
                case CrystalDeposit deposit:
                    return $"Récolter les cristaux (+{deposit.Amount} mat.)";
                case BuildSocket socket:
                    return socket.Structure == null ? $"Construire · {socket.DisplayName}" : $"Améliorer · {socket.Structure.DisplayName}";
                case ChestLid _:
                    return "Ouvrir le coffre";
                case ChestOrb orb:
                    return string.IsNullOrEmpty(orb.Reward.Title) ? "Prendre l'orbe" : "Prendre · " + Plain(orb.Reward.Title);
                default:
                    return "Utiliser";
            }
        }

        // ---- Boutons des panneaux (Canvas en World Space) : celui que traverse le rayon du viseur.

        Selectable FindSelectable(Ray ray, float maxDistance, out float distance, out Vector3 point)
        {
            distance = maxDistance;
            point = Vector3.zero;
            Selectable best = null;
            var count = Selectable.AllSelectablesNoAlloc(s_Selectables);
            for (var i = 0; i < count; i++)
            {
                var selectable = s_Selectables[i];
                s_Selectables[i] = null;
                if (selectable == null || !selectable.IsInteractable() || !(selectable.transform is RectTransform rect))
                    continue;

                var canvas = selectable.GetComponentInParent<Canvas>();
                if (canvas == null || canvas.renderMode != RenderMode.WorldSpace || !canvas.isActiveAndEnabled)
                    continue;

                var plane = new Plane(rect.forward, rect.position);
                if (!plane.Raycast(ray, out var enter) || enter > distance)
                    continue;

                var hit = ray.GetPoint(enter);
                var local = rect.InverseTransformPoint(hit);
                if (!rect.rect.Contains(new Vector2(local.x, local.y)))
                    continue;

                best = selectable;
                distance = enter;
                point = hit;
            }

            return best;
        }

        void UseSelectable(Selectable selectable, Vector3 point, bool pressed, bool held)
        {
            switch (selectable)
            {
                case Slider slider:
                    m_Crosshair.SetPrompt("Régler (maintenir)", true);
                    if (held)
                        SetSlider(slider, point);
                    return;
                case TMP_InputField field:
                    m_Crosshair.SetPrompt("Écrire", true);
                    if (pressed)
                    {
                        if (EventSystem.current != null)
                            EventSystem.current.SetSelectedGameObject(field.gameObject);
                        field.ActivateInputField();
                    }

                    return;
            }

            m_Crosshair.SetPrompt(Label(selectable), true);
            if (pressed)
                ExecuteEvents.Execute(selectable.gameObject, Pointer(), ExecuteEvents.pointerClickHandler);
        }

        static void SetSlider(Slider slider, Vector3 point)
        {
            var rect = (RectTransform)slider.transform;
            var local = rect.InverseTransformPoint(point);
            var area = rect.rect;
            var vertical = slider.direction == Slider.Direction.BottomToTop || slider.direction == Slider.Direction.TopToBottom;
            var t = vertical ? Mathf.InverseLerp(area.yMin, area.yMax, local.y) : Mathf.InverseLerp(area.xMin, area.xMax, local.x);
            if (slider.direction == Slider.Direction.RightToLeft || slider.direction == Slider.Direction.TopToBottom)
                t = 1f - t;
            slider.normalizedValue = t;
        }

        // « Acheter · Flèches de feu (120 or) » pour une carte, le texte du bouton sinon.
        static string Label(Selectable selectable)
        {
            var card = selectable.GetComponent<ShopCard>();
            var offer = card != null ? card.Offer : null;
            if (offer != null)
            {
                var verb = offer.Kind == ShopOfferKind.Build ? offer.Subtitle == "Amélioration" ? "Améliorer" : "Construire"
                    : offer.Kind == ShopOfferKind.Repair ? "Réparer"
                    : offer.Kind == ShopOfferKind.Rebuild ? "Reconstruire"
                    : "Acheter";
                return $"{verb} · {Plain(offer.Title)} ({offer.Price} {offer.PriceUnit})";
            }

            if (selectable is Toggle toggle)
                return toggle.isOn ? "Décocher" : "Cocher";

            var text = selectable.GetComponentInChildren<TMP_Text>();
            var label = text != null ? Plain(text.text) : "";
            return string.IsNullOrEmpty(label) ? "Cliquer" : label;
        }

        static string Plain(string text) =>
            string.IsNullOrEmpty(text) ? "" : s_Tags.Replace(text, "").Replace('\n', ' ').Trim();

        PointerEventData Pointer()
        {
            m_Pointer ??= new PointerEventData(EventSystem.current);
            m_Pointer.Reset();
            m_Pointer.button = PointerEventData.InputButton.Left;
            m_Pointer.position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            return m_Pointer;
        }

        // Le bouton visé s'éclaire, comme sous le rayon d'une manette.
        void SetHovered(Selectable selectable)
        {
            if (selectable == m_Hovered)
                return;

            if (m_Hovered != null)
                ExecuteEvents.Execute(m_Hovered.gameObject, Pointer(), ExecuteEvents.pointerExitHandler);
            m_Hovered = selectable;
            if (m_Hovered != null)
                ExecuteEvents.Execute(m_Hovered.gameObject, Pointer(), ExecuteEvents.pointerEnterHandler);
        }

        // ---- Le marteau : E l'abat, et s'il vise le plan d'assez près, le coup compte.

        bool TryAimBlueprint(Ray ray, BuildHammer hammer, float blockDistance, out float distance, out Vector3 point)
        {
            distance = 0f;
            point = Vector3.zero;
            var target = hammer != null ? hammer.Target : null;
            if (target == null || !target.IsUnderConstruction)
                return false;

            // Les colliders du bâtiment en plan sont dans ses limites : ils ne cachent pas le plan, seul un mur devant le cache.
            var bounds = target.ConstructionBounds;
            if (!bounds.IntersectRay(ray, out distance) || distance > m_MaxDistance || distance > blockDistance + 0.05f)
                return false;

            // Debout dans le plan (une barricade sur la voie) : on frappe devant soi.
            if (distance < 0.05f)
                distance = Mathf.Min(blockDistance, 1.2f);
            point = ray.GetPoint(distance);
            return true;
        }

        void Swing(bool strike, Vector3 point)
        {
            if (m_Swing >= 0f)
                return;

            m_Swing = 0f;
            m_StrikePending = strike;
            m_StrikePoint = point;
        }

        void UpdateHammer(float deltaTime)
        {
            if (m_HammerHold == null)
                return;

            var angle = 0f;
            if (m_Swing >= 0f)
            {
                m_Swing += deltaTime / Mathf.Max(0.05f, m_SwingTime);
                if (m_StrikePending && m_Swing >= k_StrikeAt)
                {
                    m_StrikePending = false;
                    var hammer = BuildManager.Instance != null ? BuildManager.Instance.Hammer : null;
                    if (hammer != null)
                        hammer.DesktopStrike(m_StrikePoint);
                }

                // Il s'abat vite, puis remonte plus lentement.
                angle = m_Swing < k_StrikeAt ? Mathf.Sin(m_Swing / k_StrikeAt * Mathf.PI * 0.5f)
                    : Mathf.Cos((m_Swing - k_StrikeAt) / (1f - k_StrikeAt) * Mathf.PI * 0.5f);
                angle *= m_SwingAngle;
                if (m_Swing >= 1f)
                    m_Swing = -1f;
            }

            m_HammerHold.localPosition = m_HammerOffset;
            m_HammerHold.localRotation = Quaternion.Euler(m_HammerEuler) * Quaternion.Euler(angle, 0f, 0f);
        }
    }
}
