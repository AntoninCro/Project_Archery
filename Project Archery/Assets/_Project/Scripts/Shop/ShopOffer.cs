using Archery.Bows;
using Archery.Upgrades;
using UnityEngine;

namespace Archery.Shop
{
    public enum ShopOfferKind
    {
        Upgrade,
        Bow,
        Repair,
        Rebuild,

        /// <summary>Réparer ou reconstruire toutes les barricades.</summary>
        Barricades,

        /// <summary>Carte d'information, qu'on ne peut pas acheter (« La tour est intacte »…).</summary>
        Unavailable,
    }

    /// <summary>Une offre de la boutique, telle qu'affichée sur une carte.</summary>
    public class ShopOffer
    {
        public ShopOfferKind Kind;
        public Upgrade Upgrade;
        public BowDefinition Bow;
        public string Title;
        public string Subtitle;
        public string Description;
        public Color Color = Color.white;
        public Sprite Icon;
        public int Price;
        public bool Sold;

        public bool CanBuy => !Sold && Kind != ShopOfferKind.Unavailable;

        public static ShopOffer Info(string title, string description, Color color) => new ShopOffer
        {
            Kind = ShopOfferKind.Unavailable,
            Title = title,
            Subtitle = "",
            Description = description,
            Color = color,
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
