using System;
using System.Collections.Generic;
using Archery.Bows;
using Archery.Chests;
using Archery.Defense;
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
    /// La boutique entre les vagues (GDD, section 7). Elle propose des améliorations tirées au sort
    /// selon les raretés, le prochain arc, et la réparation ou la reconstruction de la tour.
    /// On paie avec l'or du <see cref="ScoreManager"/>.
    /// </summary>
    /// <remarks>
    /// Elle s'ouvre à la fin de chaque vague et se ferme au début de la suivante.
    /// Le <see cref="ShopPanel"/> l'affiche.
    /// </remarks>
    [DisallowMultipleComponent]
    public class ShopManager : MonoBehaviour
    {
        [SerializeField]
        ShopCatalog m_Catalog;

        [Tooltip("L'arc du joueur. Vide : celui de la scène.")]
        [SerializeField]
        Bow m_Bow;

        readonly List<ShopOffer> m_UpgradeOffers = new List<ShopOffer>();
        readonly List<Upgrade> m_Candidates = new List<Upgrade>();
        ShopOffer m_BowOffer;
        ShopOffer m_TowerOffer;
        ShopOffer m_BarricadeOffer;
        ShopOffer m_BrazierOffer;
        WaveManager m_Waves;
        int m_Wave;
        int m_Rerolls;

        public static ShopManager Instance { get; private set; }

        public bool IsOpen { get; private set; }
        public ShopCatalog Catalog => m_Catalog;
        public IReadOnlyList<ShopOffer> UpgradeOffers => m_UpgradeOffers;
        public ShopOffer BowOffer => m_BowOffer;
        public ShopOffer TowerOffer => m_TowerOffer;
        public ShopOffer BarricadeOffer => m_BarricadeOffer;
        public ShopOffer BrazierOffer => m_BrazierOffer;
        public int RerollCost => m_Catalog != null
            ? ShopCatalog.RoundPrice((m_Catalog.rerollCost + m_Catalog.rerollCostIncrease * m_Rerolls) *
                                     m_Catalog.WaveFactor(m_Wave) * EndlessFactor)
            : 0;

        int WavesToWin => m_Waves != null ? m_Waves.WavesToWin : 0;

        // Chaque amélioration achetée fait monter le prix des suivantes.
        static int Purchases => PlayerUpgrades.Instance != null ? PlayerUpgrades.Instance.Count : 0;

        // En mode infini, tous les prix montent de 20 % par vague, en se cumulant.
        float EndlessFactor => m_Catalog != null ? m_Catalog.EndlessFactor(m_Wave, WavesToWin) : 1f;

        /// <summary>La boutique s'est ouverte ou fermée, ou ses offres ont changé.</summary>
        public event Action Changed;

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs Shop Manager dans la scène.", this);
            Instance = this;

            if (m_Bow == null)
                m_Bow = FindAnyObjectByType<Bow>();
            if (m_Catalog == null)
                Debug.LogError("ShopManager : aucun Shop Catalog assigné.", this);
        }

        void Start()
        {
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

        /// <summary>Ouvre la boutique après la vague <paramref name="wave"/> : nouvelles offres, relance au prix de départ.</summary>
        public void Open(int wave)
        {
            if (m_Catalog == null)
                return;

            m_Wave = wave;
            m_Rerolls = 0;
            IsOpen = true;
            RollUpgrades();
            RefreshBowOffer();
            RefreshTowerOffer();
            RefreshDefenseOffers();
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
            if (!IsOpen || offer == null || !offer.CanBuy)
                return ShopResult.Failed("Indisponible");

            // On vérifie tout avant de payer.
            var upgrades = PlayerUpgrades.Instance;
            var tower = Tower.Instance;
            switch (offer.Kind)
            {
                case ShopOfferKind.Upgrade when upgrades == null:
                    return ShopResult.Failed("Il manque un Player Upgrades dans la scène");
                case ShopOfferKind.Upgrade when !upgrades.CanTake(offer.Upgrade):
                    return ShopResult.Failed("Niveau maximal atteint");
                case ShopOfferKind.Bow when m_Bow == null:
                    return ShopResult.Failed("Il n'y a pas d'arc dans la scène");
                case ShopOfferKind.Repair:
                case ShopOfferKind.Rebuild:
                    if (tower == null)
                        return ShopResult.Failed("Il n'y a pas de tour dans la scène");
                    break;
                case ShopOfferKind.Brazier when Brazier.Instance == null:
                    return ShopResult.Failed("Il n'y a pas de brasero dans la scène");
            }

            var score = ScoreManager.Instance;
            if (score == null || !score.TrySpend(offer.Price))
                return ShopResult.Failed("Pas assez d'or");

            ShopResult result;
            switch (offer.Kind)
            {
                case ShopOfferKind.Upgrade:
                    upgrades.Add(offer.Upgrade);
                    offer.Sold = true;
                    RefreshUpgradePrices();
                    result = ShopResult.Done(offer.Upgrade.displayName + " !", offer.Color);
                    break;

                case ShopOfferKind.Bow:
                    m_Bow.SetDefinition(offer.Bow);
                    RefreshBowOffer();
                    result = ShopResult.Done(offer.Bow.displayName + " en main !", offer.Color);
                    break;

                case ShopOfferKind.Repair:
                    tower.Repair(tower.Health.Max * m_Catalog.repairFraction);
                    result = ShopResult.Done("Tour réparée", offer.Color);
                    break;

                case ShopOfferKind.Barricades:
                    Barricade.RebuildAll();
                    result = ShopResult.Done("Barricades réparées", offer.Color);
                    break;

                case ShopOfferKind.Brazier:
                    Brazier.Instance.Build();
                    result = ShopResult.Done("Brasero allumé : trempe une flèche dans le feu !", offer.Color);
                    break;

                default:
                    tower.Rebuild();
                    result = ShopResult.Done("Tour reconstruite !", offer.Color);
                    break;
            }

            RefreshTowerOffer();
            RefreshDefenseOffers();
            Changed?.Invoke();
            return result;
        }

        /// <summary>Remplace les améliorations proposées. Le prix augmente à chaque relance.</summary>
        public ShopResult TryReroll()
        {
            if (!IsOpen)
                return ShopResult.Failed("Indisponible");

            var score = ScoreManager.Instance;
            if (score == null || !score.TrySpend(RerollCost))
                return ShopResult.Failed("Pas assez d'or");

            m_Rerolls++;
            RollUpgrades();
            Changed?.Invoke();
            return ShopResult.Done("Nouvelles offres", Color.white);
        }

        void RollUpgrades()
        {
            m_UpgradeOffers.Clear();
            var owned = PlayerUpgrades.Instance;
            var luck = owned != null ? owned.LuckMultiplier : 1f;
            for (var i = 0; i < m_Catalog.upgradeOffers; i++)
            {
                var upgrade = PickUpgrade(m_Catalog.RollRarity(m_Wave, luck), owned);
                m_UpgradeOffers.Add(upgrade != null
                    ? UpgradeOffer(upgrade, owned)
                    : ShopOffer.Info("Rupture de stock", "Plus aucune amélioration à proposer.", m_Catalog.commonColor));
            }
        }

        // La rareté tirée d'abord ; s'il n'en reste aucune de cette rareté, une autre.
        Upgrade PickUpgrade(UpgradeRarity rarity, PlayerUpgrades owned)
        {
            var upgrade = PickUpgradeOf(rarity, owned);
            if (upgrade != null)
                return upgrade;

            foreach (UpgradeRarity other in Enum.GetValues(typeof(UpgradeRarity)))
            {
                upgrade = PickUpgradeOf(other, owned);
                if (upgrade != null)
                    return upgrade;
            }

            return null;
        }

        Upgrade PickUpgradeOf(UpgradeRarity rarity, PlayerUpgrades owned)
        {
            m_Candidates.Clear();
            foreach (var upgrade in m_Catalog.upgrades)
            {
                if (upgrade != null && upgrade.rarity == rarity && (owned == null || owned.CanTake(upgrade)) && !IsOffered(upgrade))
                    m_Candidates.Add(upgrade);
            }

            return m_Candidates.Count > 0 ? m_Candidates[UnityEngine.Random.Range(0, m_Candidates.Count)] : null;
        }

        // Après un achat, les autres améliorations proposées coûtent plus cher.
        void RefreshUpgradePrices()
        {
            foreach (var offer in m_UpgradeOffers)
            {
                if (offer.Kind == ShopOfferKind.Upgrade && !offer.Sold && offer.Upgrade != null)
                    offer.Price = m_Catalog.PriceOf(offer.Upgrade.rarity, m_Wave, WavesToWin, Purchases);
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

        ShopOffer UpgradeOffer(Upgrade upgrade, PlayerUpgrades owned)
        {
            var level = (owned != null ? owned.Stacks(upgrade.effect) : 0) + 1;
            var rarity = ShopCatalog.NameOf(upgrade.rarity);
            return new ShopOffer
            {
                Kind = ShopOfferKind.Upgrade,
                Upgrade = upgrade,
                Title = upgrade.displayName,
                Subtitle = level > 1 ? $"{rarity} · niveau {level}" : rarity,
                Description = upgrade.description,
                Color = m_Catalog.ColorOf(upgrade.rarity),
                Icon = upgrade.icon,
                Price = m_Catalog.PriceOf(upgrade.rarity, m_Wave, WavesToWin, Purchases),
            };
        }

        // Le prochain arc de la liste, s'il est déjà en vente.
        void RefreshBowOffer()
        {
            var color = m_Catalog.bowColor;
            var bows = m_Catalog.bows;
            var current = m_Bow != null ? m_Bow.Definition : null;
            var index = current != null ? bows.IndexOf(current) : 0;
            var next = index + 1 < bows.Count ? bows[index + 1] : null;

            if (BowClasses.IsActive)
            {
                var name = current != null ? current.displayName : "Ton arc";
                m_BowOffer = ShopOffer.Info("Arc", $"{name} : l'arc se choisit au menu, avant la partie.", color);
                return;
            }

            var legendary = LegendaryBow.Instance;
            if (legendary != null && legendary.IsAssembled)
            {
                m_BowOffer = ShopOffer.Info("Arc légendaire", "Tu as assemblé l'arc légendaire : aucun arc ne le vaut.", legendary.Color);
                return;
            }

            if (m_Bow == null || next == null)
            {
                m_BowOffer = ShopOffer.Info("Arcs", "Tu as déjà le meilleur arc.", color);
                return;
            }

            if (next.availableFromWave > m_Wave + 1)
            {
                m_BowOffer = ShopOffer.Info(next.displayName, $"En vente avant la vague {next.availableFromWave}.", color);
                return;
            }

            m_BowOffer = new ShopOffer
            {
                Kind = ShopOfferKind.Bow,
                Bow = next,
                Title = next.displayName,
                Subtitle = "Arc",
                Description = $"{next.description}\n{next.arrowSpeed:0} m/s · {next.damage:0} dégâts",
                Color = color,
                Price = ShopCatalog.RoundPrice(next.price * EndlessFactor),
            };
        }

        // Reconstruire la tour détruite, sinon la réparer si elle a perdu des PV.
        void RefreshTowerOffer()
        {
            var color = m_Catalog.towerColor;
            var tower = Tower.Instance;
            if (tower == null)
            {
                m_TowerOffer = ShopOffer.Info("Tour", "Il n'y a pas de tour dans la scène.", color);
                return;
            }

            var health = tower.Health;
            if (!tower.IsStanding)
            {
                m_TowerOffer = new ShopOffer
                {
                    Kind = ShopOfferKind.Rebuild,
                    Title = "Reconstruire la tour",
                    Subtitle = "Tour",
                    Description = "La tour revient avec tous ses PV, et l'or n'est plus divisé par deux.",
                    Color = color,
                    Price = ShopCatalog.RoundPrice(m_Catalog.rebuildPrice * EndlessFactor),
                };
            }
            else if (health.Current < health.Max - 0.5f)
            {
                m_TowerOffer = new ShopOffer
                {
                    Kind = ShopOfferKind.Repair,
                    Title = "Réparer la tour",
                    Subtitle = "Tour",
                    Description = $"+{Mathf.RoundToInt(m_Catalog.repairFraction * 100f)} % des PV max.\n" +
                                  $"PV : {Mathf.CeilToInt(health.Current)} / {Mathf.CeilToInt(health.Max)}",
                    Color = color,
                    Price = ShopCatalog.RoundPrice(m_Catalog.repairPrice * EndlessFactor),
                };
            }
            else
            {
                m_TowerOffer = ShopOffer.Info("Tour", "La tour est intacte.", color);
            }
        }

        // Barricades : les réparer toutes d'un coup. Brasero : l'allumer, une seule fois.
        void RefreshDefenseOffers()
        {
            var color = m_Catalog.defenseColor;
            if (Barricade.All.Count == 0)
            {
                m_BarricadeOffer = ShopOffer.Info("Barricades", "Il n'y a pas de barricades sur la carte.", color);
            }
            else
            {
                Barricade.TotalHealth(out var current, out var max);
                m_BarricadeOffer = current < max - 0.5f
                    ? new ShopOffer
                    {
                        Kind = ShopOfferKind.Barricades,
                        Title = "Réparer les barricades",
                        Subtitle = "Défense",
                        Description = $"Toutes les barricades reviennent avec tous leurs PV.\nPV : {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}",
                        Color = color,
                        Price = ShopCatalog.RoundPrice(m_Catalog.barricadePrice * EndlessFactor),
                    }
                    : ShopOffer.Info("Barricades", "Les barricades sont intactes.", color);
            }

            var brazier = Brazier.Instance;
            if (brazier == null)
            {
                m_BrazierOffer = ShopOffer.Info("Brasero", "Il n'y a pas de brasero sur la tour.", color);
            }
            else if (brazier.IsBuilt)
            {
                m_BrazierOffer = ShopOffer.Info("Brasero", "Allumé. Trempe la pointe d'une flèche dans le feu : sa cible brûlera.", color);
            }
            else
            {
                m_BrazierOffer = new ShopOffer
                {
                    Kind = ShopOfferKind.Brazier,
                    Title = "Brasero",
                    Subtitle = "Défense · une seule fois",
                    Description = "Un feu en haut de la tour. Trempe une flèche dedans : sa cible brûle 3 s.",
                    Color = color,
                    Price = ShopCatalog.RoundPrice(m_Catalog.brazierPrice * EndlessFactor),
                };
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
