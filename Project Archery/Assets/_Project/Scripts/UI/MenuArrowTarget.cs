using Archery.Bows;
using Archery.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Archery.UI
{
    /// <summary>
    /// Un élément d'un menu (bouton, case, curseur) qu'on peut aussi déclencher d'une flèche : un bouton est cliqué,
    /// une case change, un curseur prend la valeur de l'endroit touché. La flèche disparaît à l'impact. Pour « Quitter »,
    /// il faut tirer deux fois (une flèche perdue ne ferme pas le jeu).
    /// </summary>
    /// <remarks>
    /// Ajouté en jeu par le menu (<see cref="AttachAll"/>). Il pose un collider à la taille de l'élément, quelques
    /// millimètres derrière lui : le rayon de la main touche l'interface d'abord, et la clique comme avant.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class MenuArrowTarget : MonoBehaviour, IArrowHitHandler, IArrowAbsorber
    {
        const float k_Depth = 0.04f;     // épaisseur du collider (m)
        const float k_Behind = 0.004f;   // recul derrière l'interface (m)

        Selectable m_Selectable;
        RectTransform m_Rect;
        BoxCollider m_Collider;
        float m_ConfirmUntil;

        // « Quitter » : une flèche perdue ne doit pas fermer le jeu.
        bool NeedsConfirmation => name.ToLowerInvariant().Contains("quit");

        /// <summary>Rend tirables tous les boutons, cases et curseurs sous <paramref name="root"/>.</summary>
        public static void AttachAll(GameObject root)
        {
            if (root == null)
                return;

            foreach (var selectable in root.GetComponentsInChildren<Selectable>(true))
            {
                if (selectable.GetComponent<MenuArrowTarget>() == null)
                    selectable.gameObject.AddComponent<MenuArrowTarget>();
            }
        }

        void Awake()
        {
            m_Selectable = GetComponent<Selectable>();
            m_Rect = (RectTransform)transform;
            m_Collider = gameObject.AddComponent<BoxCollider>();
            Fit();
        }

        void OnEnable() => Fit();

        void Start() => Fit();

        // La mise en page change la taille de l'élément après coup.
        void OnRectTransformDimensionsChange() => Fit();

        // Le collider couvre l'élément, juste derrière lui (une interface en 3D regarde vers -Z).
        void Fit()
        {
            if (m_Collider == null || m_Rect == null)
                return;

            var rect = m_Rect.rect;
            var scale = Mathf.Max(1e-5f, Mathf.Abs(transform.lossyScale.z));
            var depth = k_Depth / scale;
            m_Collider.size = new Vector3(Mathf.Max(1f, rect.width), Mathf.Max(1f, rect.height), depth);
            m_Collider.center = new Vector3(rect.center.x, rect.center.y, k_Behind / scale + depth * 0.5f);
        }

        public bool OnArrowHit(in ArrowHit hit)
        {
            if (!hit.IsShot || m_Selectable == null || !m_Selectable.IsActive() || !m_Selectable.IsInteractable())
                return false;

            if (m_Selectable is Slider slider)
            {
                SetSlider(slider, hit.Point);
            }
            else
            {
                // « Quitter » d'une flèche : il faut tirer une deuxième fois dans les 3 s.
                if (NeedsConfirmation && Time.unscaledTime > m_ConfirmUntil)
                {
                    m_ConfirmUntil = Time.unscaledTime + 3f;
                    HeadMessages.Show("Tire encore une fois pour quitter", new Color(1f, 0.55f, 0.5f), 2.5f);
                    return false;
                }

                m_ConfirmUntil = 0f;

                // Comme un clic : le bouton (ou la case) réagit, avec ses effets et son son.
                var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
                ExecuteEvents.Execute(gameObject, data, ExecuteEvents.pointerClickHandler);
            }

            return false;
        }

        // Le curseur prend la valeur de l'endroit touché, le long de sa zone de remplissage.
        static void SetSlider(Slider slider, Vector3 point)
        {
            var area = slider.fillRect != null && slider.fillRect.parent is RectTransform fill ? fill
                : slider.handleRect != null && slider.handleRect.parent is RectTransform handle ? handle
                : (RectTransform)slider.transform;
            var local = area.InverseTransformPoint(point);
            var rect = area.rect;
            var vertical = slider.direction == Slider.Direction.BottomToTop || slider.direction == Slider.Direction.TopToBottom;
            var t = vertical ? Mathf.InverseLerp(rect.yMin, rect.yMax, local.y) : Mathf.InverseLerp(rect.xMin, rect.xMax, local.x);
            if (slider.direction == Slider.Direction.RightToLeft || slider.direction == Slider.Direction.TopToBottom)
                t = 1f - t;
            slider.normalizedValue = t;

            var theme = UiTheme.Load();
            if (theme != null)
                Sfx.Play(theme.clickClip, point, theme.clickVolume, 1f, 0f);
        }
    }
}
