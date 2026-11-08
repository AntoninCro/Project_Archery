using System;
using System.Collections.Generic;
using Archery.Economy;
using Archery.Upgrades;
using Archery.Waves;
using UnityEngine;

namespace Archery.Shop
{
    /// <summary>
    /// La boutique entre les vagues (GDD, section 7). Depuis la refonte du 25 octobre, elle ne vend que des améliorations,
    /// sur deux lignes : les parchemins (toujours en vente, de plus en plus chers) et des objets tirés au sort selon les
    /// raretés (chers, jusqu'à trois par pause). On paie avec l'or du <see cref="ScoreManager"/>.
    /// <para>
    /// <b>Depuis le 8 novembre, le choix de la pause</b> (le joueur : « on supprime totalement l'argent du jeu, cela
    /// simplifierait grandement les règles » ; les parchemins n'étaient qu'un bouche-trou acheté avec l'or qui restait) : plus
    /// d'or ni de prix, trois lignes de trois cartes, <b>une carte à prendre par ligne</b> : un parchemin (Force, Adresse,
    /// Vigueur), un objet (tiré comme avant), une tourelle (un lot de 1 à 4 plans d'un bâtiment du chantier, comme l'orbe d'un
    /// coffre). Le coffre du boss est retiré (<c>ChestSpawner</c>). Le même geste partout : choisir un parmi trois.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Elle s'ouvre à la fin de chaque vague et se ferme au début de la suivante. Le <see cref="ShopPanel"/> l'affiche.
    /// Plus de relance ni de réparations depuis le 25 octobre au soir (demandé par le joueur) : la tour et les barricades se
    /// réparent seules à la fin de la vague (<see cref="WaveManager"/>). L'arc ne s'y achète plus depuis le 19 octobre.
    /// </remarks>
    [DisallowMultipleComponent]
    public class ShopManager : MonoBehaviour
    {
        [SerializeField]
        ShopCatalog m_Catalog;

        /// <summary>Les lignes de la pause (8 novembre), de trois cartes chacune.</summary>
        public const int Rows = 3;
        public const int CardsPerRow = 3;
        const int k_ScrollRow = 0;
        const int k_ObjectRow = 1;
        const int k_TowerRow = 2;

        // Les cartes, dans l'ordre du panneau : les parchemins (ligne du haut), les objets, puis les tourelles (8 novembre).
        readonly List<ShopOffer> m_UpgradeOffers = new List<ShopOffer>();
        WaveManager m_Waves;
        PlayerUpgrades m_Upgrades;
        int m_Wave;

        // Les lignes où l'on a déjà pris sa carte pendant cette pause.
        readonly bool[] m_RowTaken = new bool[Rows];

        /// <summary>
        /// La rareté de la pause monte de tant de crans (8 novembre au soir, le joueur : le gong appelé tôt, d'un cran ; ses
        /// tourelles, un plan de plus). Depuis le même soir, plus tard (« le bonus avec le gong est beaucoup trop puissant ») :
        /// une seule carte par ligne, tirée au hasard (BoostedCard), et plus toute la pause.
        /// </summary>
        public int RarityBoost { get; private set; }

        // La carte de chaque ligne que le gong rend plus rare (son rang dans la ligne), tirée à l'ouverture.
        readonly int[] m_BoostedCard = new int[Rows];

        int BoostFor(int row, int index) => row >= 0 && row < m_BoostedCard.Length && m_BoostedCard[row] == index ? RarityBoost : 0;

        public static ShopManager Instance { get; private set; }

        public bool IsOpen { get; private set; }
        public ShopCatalog Catalog => m_Catalog;
        public IReadOnlyList<ShopOffer> UpgradeOffers => m_UpgradeOffers;


        /// <summary>La boutique s'est ouverte ou fermée, ou ses offres ont changé.</summary>
        public event Action Changed;

        /// <summary>La pause s'ouvre après cette vague, ses cartes tirées (8 novembre : pour le journal de partie).</summary>
        public event Action<int> Opened;

        /// <summary>Le joueur vient de prendre cette carte (8 novembre : pour le journal de partie).</summary>
        public event Action<ShopOffer> Taken;

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs Shop Manager dans la scène.", this);
            Instance = this;

            if (m_Catalog == null)
                Debug.LogError("ShopManager : aucun Shop Catalog assigné.", this);
        }

        void Start()
        {
            // Une pièce prise ailleurs (le coffre du boss, pendant la pause) change le niveau et les remplacements affichés.
            m_Upgrades = PlayerUpgrades.Instance;
            if (m_Upgrades != null)
                m_Upgrades.Changed += OnUpgradesChanged;

            m_Waves = WaveManager.Instance;
            if (m_Waves == null)
            {
                Debug.LogWarning("ShopManager : aucun Wave Manager, la boutique ne s'ouvrira jamais.", this);
                return;
            }

            m_Waves.WaveEnded += Open;
            m_Waves.WaveStarted += OnWaveStarted;
            m_Waves.GameOver += Close;
        }

        void OnDestroy()
        {
            if (m_Upgrades != null)
                m_Upgrades.Changed -= OnUpgradesChanged;
            if (m_Waves != null)
            {
                m_Waves.WaveEnded -= Open;
                m_Waves.WaveStarted -= OnWaveStarted;
                m_Waves.GameOver -= Close;
            }

            if (Instance == this)
                Instance = null;
        }

        void OnWaveStarted(int wave) => Close();

        void OnUpgradesChanged()
        {
            if (!IsOpen || m_Catalog == null)
                return;

            RefreshScrollOffers(m_Upgrades);
            RefreshObjectOffers();
            Changed?.Invoke();
        }

        /// <summary>Ouvre la boutique après la vague <paramref name="wave"/> : nouveaux objets, parchemins au prix de cette vague.</summary>
        public void Open(int wave)
        {
            if (m_Catalog == null)
                return;

            m_Wave = wave;
            for (var i = 0; i < m_RowTaken.Length; i++)
                m_RowTaken[i] = false;
            var waves = WaveManager.Instance;
            RarityBoost = waves != null && waves.BossCalledEarly ? 1 : 0;
            for (var i = 0; i < m_BoostedCard.Length; i++)
                m_BoostedCard[i] = UnityEngine.Random.Range(0, CardsPerRow);
            IsOpen = true;
            RollOffers();
            Changed?.Invoke();
            Opened?.Invoke(wave);
        }

        public void Close()
        {
            if (!IsOpen)
                return;

            IsOpen = false;
            Changed?.Invoke();
        }

        /// <summary>Prend la carte (8 novembre : sans rien payer, une par ligne).</summary>
        public ShopResult TryBuy(ShopOffer offer)
        {
            if (!IsOpen || offer == null || !offer.CanBuy)
                return ShopResult.Failed("Indisponible");
            if (offer.Row >= 0 && offer.Row < m_RowTaken.Length && m_RowTaken[offer.Row])
                return ShopResult.Failed("Une carte par ligne");

            string message;
            if (offer.Plans.HasValue)
            {
                message = offer.Plans.Value.Apply(0f);
            }
            else
            {
                if (offer.Kind != ShopOfferKind.Upgrade || offer.Upgrade == null)
                    return ShopResult.Failed("Indisponible");

                var upgrades = PlayerUpgrades.Instance;
                if (upgrades == null)
                    return ShopResult.Failed("Il manque un Player Upgrades dans la scène");
                if (!upgrades.CanTake(offer.Upgrade))
                    return ShopResult.Failed("Niveau maximal atteint");

                // Prise avant l'ajout : les autres cartes se mettent à jour dès que la pièce est prise (OnUpgradesChanged).
                var occupant = upgrades.Occupant(offer.Upgrade);
                offer.Sold = true;
                upgrades.Add(offer.Upgrade, offer.ScrollScale);
                message = PurchaseMessage(offer.Upgrade, occupant, upgrades.Level(offer.Upgrade.effect));
            }

            TakeRow(offer);
            Changed?.Invoke();
            Taken?.Invoke(offer);
            return ShopResult.Done(message, offer.Color);
        }

        // La carte prise reste mise en avant ; les deux autres de sa ligne s'éteignent.
        void TakeRow(ShopOffer chosen)
        {
            if (chosen.Row >= 0 && chosen.Row < m_RowTaken.Length)
                m_RowTaken[chosen.Row] = true;

            foreach (var offer in m_UpgradeOffers)
            {
                if (offer == null || offer.Row != chosen.Row)
                    continue;

                if (offer == chosen)
                {
                    offer.Sold = true;
                    offer.Highlight = true;
                    offer.PriceText = "Pris";
                }
                else
                {
                    offer.Kind = ShopOfferKind.Unavailable;
                    offer.PriceText = "";
                }
            }
        }

        /// <summary>
        /// « Multitir niveau 2 ! », « Flèche de foudre ! (rangée dans l'atelier) » quand son emplacement était déjà pris
        /// (<paramref name="occupant"/>, 26 octobre : on l'équipe dans l'atelier). À appeler une fois la pièce prise : si elle
        /// forme un duo avec une pièce possédée sans qu'il soit actif (l'une des deux est rangée), le message le dit et nomme
        /// l'autre pièce (26 octobre) ; un duo qui se forme tout de suite s'annonce à part (ArrowPieces).
        /// </summary>
        public static string PurchaseMessage(Upgrade upgrade, Upgrade occupant, int level)
        {
            var name = upgrade.kind == UpgradeKind.Object && level > 1 ? $"{upgrade.displayName} niveau {level}" : upgrade.displayName;
            var text = occupant != null ? name + " ! (rangée dans l'atelier)" : name + " !";

            var owned = PlayerUpgrades.Instance;
            var duo = ArrowDuos.Completes(upgrade, owned);
            var partner = ArrowDuos.PartnerOf(duo, upgrade, owned);
            if (partner != null)
                text += $"\n<size=60%><color=#FFD54F>Duo {duo.Name} avec « {partner.displayName} » : équipe les deux dans l'atelier</color></size>";
            return text;
        }

        // Les trois lignes (8 novembre) : les parchemins du catalogue, trois objets tirés au sort, trois tourelles. Chaque ligne a
        // toujours trois cartes (une carte vide si rien à proposer) : la carte i du panneau est l'offre i.
        void RollOffers()
        {
            m_UpgradeOffers.Clear();
            var owned = PlayerUpgrades.Instance;
            foreach (var upgrade in m_Catalog.upgrades)
            {
                if (upgrade == null || upgrade.kind != UpgradeKind.Scroll || CountOf(k_ScrollRow) >= CardsPerRow)
                    continue;

                // Sa rareté (8 novembre au soir), tirée pour chaque carte : « une Adresse épique ou une Force commune ? ».
                var rarity = m_Catalog.RollRarity(m_Wave, owned != null ? owned.LuckMultiplier : 1f, BoostFor(k_ScrollRow, CountOf(k_ScrollRow)));
                var scale = ShopCatalog.ScrollScale(rarity);
                m_UpgradeOffers.Add(Free(new ShopOffer
                {
                    Kind = ShopOfferKind.Upgrade,
                    Upgrade = upgrade,
                    ScrollScale = scale,
                    Title = upgrade.displayName,
                    Description = ShopCatalog.ScrollText(upgrade, scale),
                    Color = m_Catalog.ColorOf(rarity),
                    Icon = upgrade.icon,
                }, k_ScrollRow));
            }

            FillRow(k_ScrollRow);
            RefreshScrollOffers(owned);

            // L'emplacement d'abord, de préférence un autre à chaque carte (26 octobre au soir : ShopCatalog.PickObject).
            m_ShownSlots.Clear();
            for (var i = 0; i < CardsPerRow; i++)
            {
                var upgrade = m_Catalog.PickObject(m_Wave, owned, IsOffered, m_ShownSlots, BoostFor(k_ObjectRow, i));
                if (upgrade != null)
                    m_ShownSlots.Add(upgrade.slot);
                m_UpgradeOffers.Add(upgrade != null
                    ? Free(ObjectOffer(upgrade), k_ObjectRow)
                    : Free(ShopOffer.Info("Rupture de stock", "Tu as déjà tous les objets.", m_Catalog.commonColor), k_ObjectRow));
            }

            // Les tourelles : des lots de plans de bâtiments différents du chantier, de 1 à 4 plans (BuildManager.RollPlansPerChest).
            foreach (var reward in Archery.Chests.ChestRewards.RollPlans(CardsPerRow, RarityBoost, m_BoostedCard[k_TowerRow]))
            {
                m_UpgradeOffers.Add(Free(new ShopOffer
                {
                    Kind = ShopOfferKind.Upgrade,
                    Plans = reward,
                    Title = reward.Title,
                    Subtitle = reward.Subtitle,
                    Description = reward.Structure != null ? reward.Structure.DescriptionFor(1) : "",
                    Color = reward.Color,
                    Icon = reward.Icon,
                }, k_TowerRow));
            }

            FillRow(k_TowerRow);
        }

        static string RarityName(float scrollScale) =>
            ShopCatalog.NameOf(scrollScale >= 3f ? UpgradeRarity.Legendary : scrollScale >= 2f ? UpgradeRarity.Epic
                : scrollScale >= 1.5f ? UpgradeRarity.Rare : UpgradeRarity.Common);

        // Une carte de la pause : rien à payer (le prix laisse la place à « Prendre »).
        static ShopOffer Free(ShopOffer offer, int row)
        {
            offer.Row = row;
            offer.Price = 0;
            if (offer.Kind != ShopOfferKind.Unavailable)
            {
                offer.PriceText = "Prendre";
                offer.Verb = "Prendre";
            }

            return offer;
        }

        int CountOf(int row)
        {
            var count = 0;
            foreach (var offer in m_UpgradeOffers)
            {
                if (offer != null && offer.Row == row)
                    count++;
            }

            return count;
        }

        // Une ligne incomplète (moins de trois parchemins, pas de tower defense) : des cartes vides, pour garder les lignes alignées.
        void FillRow(int row)
        {
            while (CountOf(row) < CardsPerRow)
                m_UpgradeOffers.Add(Free(ShopOffer.Info("—", "Rien d'autre à proposer.", m_Catalog.commonColor), row));
        }

        readonly List<ArrowSlot> m_ShownSlots = new List<ArrowSlot>();

        // Le prix et le niveau des parchemins, après chaque achat de l'un d'eux : chacun a son prix (26 octobre).
        void RefreshScrollOffers(PlayerUpgrades owned)
        {
            foreach (var offer in m_UpgradeOffers)
            {
                if (offer.Kind != ShopOfferKind.Upgrade || offer.Upgrade == null || offer.Upgrade.kind != UpgradeKind.Scroll)
                    continue;

                var effect = offer.Upgrade.effect;
                var level = (owned != null ? owned.Level(effect) : 0) + (offer.Sold ? 0 : 1);
                offer.Subtitle = ShopCatalog.SubtitleOf(offer.Upgrade, level) + " · " + RarityName(offer.ScrollScale);
            }
        }

        // Après chaque achat ou pièce prise : le prix (un objet de plus acheté), le niveau et ce que la pièce remplacerait.
        void RefreshObjectOffers()
        {
            foreach (var offer in m_UpgradeOffers)
            {
                if (offer.Kind == ShopOfferKind.Upgrade && !offer.Sold && offer.Upgrade != null && offer.Upgrade.kind != UpgradeKind.Scroll)
                    UpdateObjectOffer(offer);
            }
        }

        bool IsOffered(Upgrade upgrade)
        {
            foreach (var offer in m_UpgradeOffers)
            {
                if (offer.Upgrade == upgrade)
                    return true;
            }

            return false;
        }

        ShopOffer ObjectOffer(Upgrade upgrade)
        {
            var offer = new ShopOffer
            {
                Kind = ShopOfferKind.Upgrade,
                Upgrade = upgrade,
                Title = upgrade.displayName,
                Color = m_Catalog.ColorOf(upgrade),
                Icon = upgrade.icon,
            };
            UpdateObjectOffer(offer);
            return offer;
        }

        // « Pointe · niveau 2 », et dessous la description (au niveau 2 et plus, ce qui change : « 50 % → 100 % ➶ », depuis le
        // 27 octobre au soir), le duo qu'elle pourrait former et, si son emplacement est pris, qu'elle ira dans l'atelier. Une pièce
        // montée au niveau maximal entre-temps (par le coffre du boss) ne s'achète plus.
        void UpdateObjectOffer(ShopOffer offer)
        {
            var upgrade = offer.Upgrade;
            var level = (m_Upgrades != null ? m_Upgrades.Level(upgrade.effect) : 0) + 1;
            var occupant = m_Upgrades != null ? m_Upgrades.Occupant(upgrade) : null;
            offer.Subtitle = ShopCatalog.SubtitleOf(upgrade, level);
            offer.Description = ShopCatalog.Lines(upgrade.LevelUpAt(level), ShopCatalog.DuoLine(upgrade, m_Upgrades), ShopCatalog.StorageLine(occupant));
            if (m_Upgrades != null && !m_Upgrades.CanTake(upgrade))
            {
                offer.Kind = ShopOfferKind.Unavailable;
                offer.Subtitle = "Niveau maximal";
            }
        }

        // (Le raccourci M, 500 pièces d'or, est retiré le 8 novembre avec l'or.)
    }
}
