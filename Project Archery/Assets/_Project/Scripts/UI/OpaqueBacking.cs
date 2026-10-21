using UnityEngine;
using UnityEngine.UI;

namespace Archery.UI
{
    /// <summary>
    /// Un fond plein et opaque juste derrière une fenêtre d'interface dans le monde (menu principal, choix de l'arc, grand
    /// écran du tutoriel). Il écrit la profondeur (<c>Resources/UIOpaqueBacking</c>) : ce qui est derrière la fenêtre ne
    /// passe plus par-dessus, même dessiné après elle. Les interfaces n'écrivent pas la profondeur, et le texte des
    /// panneaux de difficulté (rang 1) traversait les fenêtres (rang 0), aussi opaques soient-elles.
    /// </summary>
    public static class OpaqueBacking
    {
        const string k_MaterialResource = "UIOpaqueBacking";

        // 1 cm derrière le panneau : il cache ce qui est plus loin, et le contenu de la fenêtre reste devant lui.
        const float k_Behind = 0.01f;

        static Material s_Material;

        /// <summary>
        /// Crée le fond derrière <paramref name="panel"/> (même place, rentré de <paramref name="inset"/> unités du
        /// Canvas), juste avant lui parmi ses frères. Il faut l'afficher et le cacher avec le panneau.
        /// </summary>
        public static GameObject Create(RectTransform panel, Color color, float inset)
        {
            var go = new GameObject("Fond opaque", typeof(RectTransform));
            go.layer = panel.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(panel.parent, false);
            rect.SetSiblingIndex(panel.GetSiblingIndex());
            rect.anchorMin = panel.anchorMin;
            rect.anchorMax = panel.anchorMax;
            rect.pivot = panel.pivot;
            rect.anchoredPosition = panel.anchoredPosition;
            rect.sizeDelta = panel.sizeDelta - Vector2.one * (2f * inset);

            var scale = Mathf.Max(1e-5f, Mathf.Abs(rect.parent.lossyScale.z));
            var position = rect.localPosition;
            rect.localPosition = new Vector3(position.x, position.y, k_Behind / scale);

            var image = go.AddComponent<Image>();
            color.a = 1f;
            image.color = color;
            image.raycastTarget = false;
            if (s_Material == null)
                s_Material = Resources.Load<Material>(k_MaterialResource);
            if (s_Material != null)
                image.material = s_Material;

            go.SetActive(panel.gameObject.activeSelf);
            return go;
        }
    }
}
