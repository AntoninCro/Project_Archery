using System;
using System.Collections.Generic;
using Archery.Economy;
using Archery.Upgrades;
using Archery.Waves;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine.InputSystem;
#endif

namespace Archery.Shop
{
    /// <summary>
    /// La boutique entre les vagues (GDD, section 7). Depuis la refonte du 25 octobre, elle ne vend que des améliorations,
    /// sur deux lignes : les parchemins (toujours en vente, de plus en plus chers) et des objets tirés au sort selon les
    /// raretés (chers, jusqu'à trois par pause). On paie avec l'or du <see cref="ScoreManager"/>.
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

        // Les cartes, dans l'ordre du panneau : les parchemins (ligne du haut), puis les objets (ligne du bas).
        readonly List<ShopOffer> m_UpgradeOffers = new List<ShopOffer>();
        readonly List<Upgrade> m_Candidates = new List<Upgrade>();
        WaveManager m_Waves;
        PlayerUpgrades m_Upgrades;
        int m_Wave;

        // Objets achetés pendant cette pause ; objets et parchemins achetés en boutique pendant la partie (ceux des coffres,
        // non : ce sont des cadeaux), et chaque parchemin à part (26 octobre : chacun a son prix).
        int m_ObjectsThisPause;
        int m_ObjectsBought;
        int m_ScrollsBought;
        readonly int[] m_ScrollsOfType = new int[Enum.GetValues(typeof(UpgradeEffect)).Length];

        public static ShopManager Instance { get; private set; }

        public bool IsOpen { get; private set; }
        public ShopCatalog Catalog => m_Catalog;
        public IReadOnlyList<ShopOffer> UpgradeOffers => m_UpgradeOffers;

        int WavesToWin => m_Waves != null ? m_Waves.WavesToWin : 0;

        bool ObjectLimitReached => m_Catalog != null && m_ObjectsThisPause >= m_Catalog.objectsPerShop;

        /// <summary>La boutique s'est ouverte ou fermée, ou ses offres ont changé.</summary>
        public event Action Changed;

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
            m_ObjectsThisPause = 0;
            IsOpen = true;
            RollOffers();
            Changed?.Invoke();
        }

        public void Close()
        {
            if (!IsOpen)
                return;

            IsOpen = false;
            Changed?.Invoke();
        }

        public ShopResult TryBuy(ShopOffer offer)
        {
            if (!IsOpen || offer == null || !offer.CanBuy || offer.Kind != ShopOfferKind.Upgrade || offer.Upgrade == null)
                return ShopResult.Failed("Indisponible");

            // On vérifie tout avant de payer.
            var upgrades = PlayerUpgrades.Instance;
            var isScroll = offer.Upgrade.kind == UpgradeKind.Scroll;
            if (upgrades == null)
                return ShopResult.Failed("Il manque un Player Upgrades dans la scène");
            if (!upgrades.CanTake(offer.Upgrade))
                return ShopResult.Failed("Niveau maximal atteint");
            if (!isScroll && ObjectLimitReached)
                return ShopResult.Failed(m_Catalog.objectsPerShop == 1 ? "Un objet par pause" : $"{m_Catalog.objectsPerShop} objets par pause au plus");

            var score = ScoreManager.Instance;
            if (score == null || !score.TrySpend(offer.Price))
                return ShopResult.Failed("Pas assez d'or");

            var occupant = upgrades.Occupant(offer.Upgrade);
            if (isScroll)
            {
                // Le parchemin reste en vente, au niveau suivant : il coûte plus cher, les autres un peu plus.
                m_ScrollsBought++;
                m_ScrollsOfType[(int)offer.Upgrade.effect]++;
                upgrades.Add(offer.Upgrade);
                RefreshScrollOffers(upgrades);
            }
            else
            {
                // Chaque objet acheté rend les suivants plus chers. Compté avant l'ajout : les autres cartes se mettent à
                // jour dès que la pièce est prise (OnUpgradesChanged).
                m_ObjectsThisPause++;
                m_ObjectsBought++;
                offer.Sold = true;
                upgrades.Add(offer.Upgrade);
                RefreshObjectOffers();
            }

            Changed?.Invoke();
            return ShopResult.Done(PurchaseMessage(offer.Upgrade, occupant, upgrades.Level(offer.Upgrade.effect)), offer.Color);
        }

        /// <summary>
        /// « Multitir niveau 2 ! », « Flèche de foudre ! (rangée dans l'atelier) » quand son emplacement était déjà pris
        /// (<paramref name="occupant"/>, 26 octobre : on l'équipe dans l'atelier).
        /// </summary>
        public static string PurchaseMessage(Upgrade upgrade, Upgrade occupant, int level)
        {
            var name = upgrade.kind == UpgradeKind.Object && level > 1 ? $"{upgrade.displayName} niveau {level}" : upgrade.displayName;
            return occupant != null ? name + " ! (rangée dans l'atelier)" : name + " !";
        }

        // Tous les parchemins du catalogue, puis les objets tirés au sort.
        void RollOffers()
        {
            m_UpgradeOffers.Clear();
            var owned = PlayerUpgrades.Instance;
            foreach (var upgrade in m_Catalog.upgrades)
            {
                if (upgrade == null || upgrade.kind != UpgradeKind.Scroll)
                    continue;

                m_UpgradeOffers.Add(new ShopOffer
                {
                    Kind = ShopOfferKind.Upgrade,
                    Upgrade = upgrade,
                    Title = upgrade.displayName,
                    Description = upgrade.description,
                    Color = m_Catalog.ColorOf(upgrade),
                    Icon = upgrade.icon,
                });
            }

            RefreshScrollOffers(owned);

            var luck = owned != null ? owned.LuckMultiplier : 1f;
            for (var i = 0; i < m_Catalog.objectOffers; i++)
            {
                var upgrade = PickObject(m_Catalog.RollRarity(m_Wave, luck), owned);
                m_UpgradeOffers.Add(upgrade != null
                    ? ObjectOffer(upgrade)
                    : ShopOffer.Info("Rupture de stock", "Tu as déjà tous les objets.", m_Catalog.commonColor));
            }
        }

        // La rareté tirée d'abord ; s'il ne reste aucun objet de cette rareté, une autre.
        Upgrade PickObject(UpgradeRarity rarity, PlayerUpgrades owned)
        {
            var upgrade = PickObjectOf(rarity, owned);
            if (upgrade != null)
                return upgrade;

            foreach (UpgradeRarity other in Enum.GetValues(typeof(UpgradeRarity)))
            {
                upgrade = PickObjectOf(other, owned);
                if (upgrade != null)
                    return upgrade;
            }

            return null;
        }

        Upgrade PickObjectOf(UpgradeRarity rarity, PlayerUpgrades owned)
        {
            m_Candidates.Clear();
            foreach (var upgrade in m_Catalog.upgrades)
            {
                if (upgrade != null && upgrade.kind == UpgradeKind.Object && upgrade.rarity == rarity &&
                    (owned == null || owned.CanTake(upgrade)) && !IsOffered(upgrade))
                    m_Candidates.Add(upgrade);
            }

            return m_Candidates.Count > 0 ? m_Candidates[UnityEngine.Random.Range(0, m_Candidates.Count)] : null;
        }

        // Le prix et le niveau des parchemins, après chaque achat de l'un d'eux : chacun a son prix (26 octobre). Sous la
        // description (inchangée), les PV max que donnent la Force et l'Adresse.
        void RefreshScrollOffers(PlayerUpgrades owned)
        {
            foreach (var offer in m_UpgradeOffers)
            {
                if (offer.Kind != ShopOfferKind.Upgrade || offer.Upgrade == null || offer.Upgrade.kind != UpgradeKind.Scroll)
                    continue;

                var effect = offer.Upgrade.effect;
                var level = (owned != null ? owned.Level(effect) : 0) + 1;
                var health = owned != null && PlayerUpgrades.ScrollGivesHealth(effect) ? owned.HealthPerScroll : 0f;
                offer.Subtitle = ShopCatalog.SubtitleOf(offer.Upgrade, level);
                offer.Description = health > 0f ? $"{offer.Upgrade.description}\n<color=#8BE28B>+{health:0} PV max</color>" : offer.Upgrade.description;
                offer.Price = m_Catalog.ScrollPriceOf(m_Wave, WavesToWin, m_ScrollsBought, m_ScrollsOfType[(int)effect]);
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

        // « Pointe · niveau 2 », et sous la description (inchangée), le duo qu'elle pourrait former et, si son emplacement est
        // pris, qu'elle ira dans l'atelier. Une pièce montée au niveau maximal entre-temps (par le coffre du boss) ne s'achète plus.
        void UpdateObjectOffer(ShopOffer offer)
        {
            var upgrade = offer.Upgrade;
            var level = (m_Upgrades != null ? m_Upgrades.Level(upgrade.effect) : 0) + 1;
            var occupant = m_Upgrades != null ? m_Upgrades.Occupant(upgrade) : null;
            offer.Subtitle = ShopCatalog.SubtitleOf(upgrade, level);
            offer.Description = ShopCatalog.Lines(upgrade.description, ShopCatalog.DuoLine(ArrowDuos.Completes(upgrade, m_Upgrades)),
                                                  ShopCatalog.StorageLine(occupant));
            offer.Price = m_Catalog.ObjectPriceOf(upgrade.rarity, m_Wave, WavesToWin, m_ObjectsBought);
            if (m_Upgrades != null && !m_Upgrades.CanTake(upgrade))
            {
                offer.Kind = ShopOfferKind.Unavailable;
                offer.Subtitle = "Niveau maximal";
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Raccourci de test au clavier : M donne 500 pièces d'or.
        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.mKey.wasPressedThisFrame && ScoreManager.Instance != null)
                ScoreManager.Instance.AddMoney(500);
        }
#endif
    }
}
