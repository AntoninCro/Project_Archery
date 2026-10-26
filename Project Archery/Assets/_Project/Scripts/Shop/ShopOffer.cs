using Archery.Upgrades;
using UnityEngine;

namespace Archery.Shop
{
    // Plus d'arc ni de réparations en boutique depuis le 25 octobre au soir : la tour et les barricades se réparent seules.
    public enum ShopOfferKind
    {
        /// <summary>Un objet ou un parchemin.</summary>
        Upgrade,

        /// <summary>Carte d'information, qu'on ne peut pas acheter (« La tour est intacte »…).</summary>
        Unavailable,

        /// <summary>Construction ou amélioration d'un bâtiment (panneau des socles, payée en matériaux).</summary>
        Build,
    }

    /// <summary>Une offre de la boutique, telle qu'affichée sur une carte.</summary>
    public class ShopOffer
    {
        public ShopOfferKind Kind;
        public Upgrade Upgrade;
        public string Title;
        public string Subtitle;
        public string Description;
        public Color Color = Color.white;
        public Sprite Icon;
        public int Price;

        /// <summary>Monnaie affichée après le prix : « or », ou « mat. » pour les matériaux.</summary>
        public string PriceUnit = "or";

        /// <summary>
        /// Optionnel : affiché à la place du prix, pour une carte qui ne se paie pas (l'atelier, 26 octobre :
        /// « Équipée », « Équiper »). Le clic ne coûte alors rien.
        /// </summary>
        public string PriceText;

        /// <summary>Optionnel : le verbe du viseur sans casque (« Équiper · Pointe éclatée ») ; vide, « Acheter » (ou « Construire »).</summary>
        public string Verb;

        /// <summary>La carte est mise en avant (l'emplacement choisi dans l'atelier) : fond plus teinté, cadre plein.</summary>
        public bool Highlight;

        public bool Sold;

        public bool CanBuy => !Sold && Kind != ShopOfferKind.Unavailable;

        public static ShopOffer Info(string title, string description, Color color, Sprite icon = null) => new ShopOffer
        {
            Kind = ShopOfferKind.Unavailable,
            Title = title,
            Subtitle = "",
            Description = description,
            Color = color,
            Icon = icon,
        };
    }

    /// <summary>Résultat d'un achat, avec le message à afficher.</summary>
    public readonly struct ShopResult
    {
        public readonly bool Success;
        public readonly string Message;
        public readonly Color Color;

        ShopResult(bool success, string message, Color color)
        {
            Success = success;
            Message = message;
            Color = color;
        }

        public static ShopResult Done(string message, Color color) => new ShopResult(true, message, color);

        public static ShopResult Failed(string message) => new ShopResult(false, message, new Color(1f, 0.45f, 0.4f));
    }
}
