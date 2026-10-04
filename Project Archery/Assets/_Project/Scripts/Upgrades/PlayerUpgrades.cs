using System;
using System.Collections.Generic;
using Archery.Bows;
using Archery.Combat;
using Archery.Economy;
using Archery.Enemies;
using Archery.Player;
using Archery.UI;
using UnityEngine;

namespace Archery.Upgrades
{
    /// <summary>Bonus temporaire donné par une orbe de coffre (GDD, section 13).</summary>
    public enum TemporaryBuff
    {
        /// <summary>Dégâts doublés.</summary>
        DoubleDamage,

        /// <summary>Tout tir à pleine tension est parfait ; l'anneau est presque entièrement vert.</summary>
        PerfectShots,

        /// <summary>Anneau de timing accéléré, bandes un peu plus larges.</summary>
        FastRing,
    }

    /// <summary>
    /// Les améliorations achetées pendant la partie (GDD, section 6). Elles se cumulent sans limite et durent
    /// jusqu'à la fin de la partie. Ce script applique les bonus de statistiques (arc, PV, or, tête, vampirisme) ;
    /// la chance est lue par la boutique, et les flèches spéciales sont gérées par <see cref="SpecialArrows"/>.
    /// Il gère aussi les bonus temporaires des coffres (<see cref="TemporaryBuff"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerUpgrades : MonoBehaviour
    {
        // « Charge rapide » élargit aussi toutes les bandes de l'anneau de 10 % par exemplaire.
        const float k_QuickChargeBandBonus = 0.1f;

        static readonly int k_EffectCount = Enum.GetValues(typeof(UpgradeEffect)).Length;
        static readonly int k_BuffCount = Enum.GetValues(typeof(TemporaryBuff)).Length;

        [Tooltip("Couleur du « +2 PV » du vampirisme.")]
        [SerializeField]
        Color m_HealPopupColor = new Color(0.45f, 1f, 0.45f);

        [Header("Bonus temporaires (coffres)")]
        [Tooltip("Multiplie les dégâts pendant « Dégâts ×2 ».")]
        [SerializeField]
        float m_BuffDamageMultiplier = 2f;

        [Tooltip("Multiplie la vitesse de l'anneau pendant « Anneau rapide ».")]
        [SerializeField]
        float m_BuffRingSpeedMultiplier = 1.75f;

        [Tooltip("Élargit les bandes de l'anneau pendant « Anneau rapide », pour que le parfait reste faisable.")]
        [SerializeField]
        float m_BuffRingBandMultiplier = 1.25f;

        [Tooltip("Couleur du message de fin d'un bonus.")]
        [SerializeField]
        Color m_BuffEndColor = new Color(0.8f, 0.8f, 0.85f);

        readonly int[] m_Stacks = new int[k_EffectCount];
        readonly float[] m_BuffTimers = new float[k_BuffCount];
        readonly float[] m_Totals = new float[k_EffectCount];
        readonly List<Upgrade> m_Owned = new List<Upgrade>();
        float m_BaseMaxHealth = -1f;

        public static PlayerUpgrades Instance { get; private set; }

        /// <summary>Une amélioration vient d'être ajoutée.</summary>
        public event Action Changed;

        /// <summary>Un bonus temporaire commence ou se termine.</summary>
        public event Action BuffsChanged;

        /// <summary>Améliorations possédées, une fois chacune, dans l'ordre du premier achat.</summary>
        public IReadOnlyList<Upgrade> Owned => m_Owned;

        /// <summary>Nombre total d'améliorations obtenues pendant la partie, en boutique ou dans un coffre (chaque exemplaire compte).</summary>
        public int Count { get; private set; }

        public float DamageMultiplier => 1f + Total(UpgradeEffect.Damage);
        public float ArrowSpeedMultiplier => 1f + Total(UpgradeEffect.ArrowSpeed);
        public float RingSpeedMultiplier => 1f + Total(UpgradeEffect.QuickCharge);
        public float BandWidthMultiplier => 1f + k_QuickChargeBandBonus * Stacks(UpgradeEffect.QuickCharge);
        public float GoldWidthMultiplier => 1f + Total(UpgradeEffect.Precision);
        public float BonusMaxHealth => Total(UpgradeEffect.Vitality);
        public float HeadDamageMultiplier => 1f + Total(UpgradeEffect.HeadHunter);
        public float LifeOnHeadshot => Total(UpgradeEffect.Vampirism);
        public int ChainCount => Mathf.RoundToInt(Total(UpgradeEffect.ChainLightning));
        public int RicochetCount => Mathf.RoundToInt(Total(UpgradeEffect.Ricochet));

        /// <summary>Part des points changée en or, en plus du taux de base (Butin).</summary>
        public float MoneyRateBonus => Total(UpgradeEffect.Loot);

        /// <summary>Multiplie les chances des cartes rares et légendaires en boutique (Chance).</summary>
        public float LuckMultiplier => 1f + Total(UpgradeEffect.Luck);

        /// <summary>Vitesse (°/s) à laquelle les flèches tournent vers l'ennemi proche (0 = pas d'auto-visée).</summary>
        public float HomingTurnRate => Total(UpgradeEffect.Homing);

        // Nombres moyens, sans limite : voir RollCount.

        /// <summary>Flèches en plus par tir, en moyenne (multitir).</summary>
        public float MultishotAverage => Total(UpgradeEffect.Multishot);

        /// <summary>Échos du tir, en moyenne.</summary>
        public float EchoAverage => Total(UpgradeEffect.Echo);

        /// <summary>Ennemis traversés en plus par flèche, en moyenne (perçage).</summary>
        public float PierceAverage => Total(UpgradeEffect.Piercing);

        /// <summary>Flèches en plus quand une flèche se divise en vol, en moyenne (déluge).</summary>
        public float DelugeAverage => Total(UpgradeEffect.Deluge);

        // Chances par flèche (de 0 à 1). Ce qui dépasse 100 % renforce l'effet : voir les « Overflow ».

        public float LightningChance => Mathf.Clamp01(Total(UpgradeEffect.Lightning));
        public float ExplosiveChance => Mathf.Clamp01(Total(UpgradeEffect.Explosive));
        public float FrostChance => Mathf.Clamp01(Total(UpgradeEffect.Frost));

        /// <summary>Part de la chance de foudre au-delà de 100 % (0,4 = 140 %) : éclairs plus forts.</summary>
        public float LightningOverflow => Mathf.Max(0f, Total(UpgradeEffect.Lightning) - 1f);

        public float ExplosiveOverflow => Mathf.Max(0f, Total(UpgradeEffect.Explosive) - 1f);
        public float FrostOverflow => Mathf.Max(0f, Total(UpgradeEffect.Frost) - 1f);

        /// <summary>
        /// Tire un nombre entier autour d'une moyenne : 1,5 donne 1, plus 1 avec 50 % de chance.
        /// C'est la règle du multitir, de l'écho, du perçage et du déluge.
        /// </summary>
        public static int RollCount(float average)
        {
            if (average <= 0f)
                return 0;

            var whole = Mathf.FloorToInt(average);
            return whole + (UnityEngine.Random.value < average - whole ? 1 : 0);
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
                Debug.LogWarning("Il y a plusieurs Player Upgrades dans la scène.", this);
            Instance = this;
        }

        void OnEnable() => Enemy.Damaged += OnEnemyDamaged;

        void OnDisable() => Enemy.Damaged -= OnEnemyDamaged;

        void Start() => Apply();

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public int Stacks(UpgradeEffect effect) => m_Stacks[(int)effect];

        /// <summary>Secondes restantes d'un bonus temporaire (0 = inactif).</summary>
        public float BuffTimeLeft(TemporaryBuff buff) => m_BuffTimers[(int)buff];

        public bool HasBuff(TemporaryBuff buff) => m_BuffTimers[(int)buff] > 0f;

        public static string NameOf(TemporaryBuff buff) => buff switch
        {
            TemporaryBuff.DoubleDamage => "Dégâts ×2",
            TemporaryBuff.PerfectShots => "Tirs parfaits",
            _ => "Anneau rapide",
        };

        /// <summary>Lance (ou prolonge) un bonus temporaire pendant <paramref name="duration"/> secondes.</summary>
        public void AddBuff(TemporaryBuff buff, float duration)
        {
            var index = (int)buff;
            m_BuffTimers[index] = Mathf.Max(m_BuffTimers[index], duration);
            Apply();
            BuffsChanged?.Invoke();
        }

        void Update()
        {
            var expired = false;
            for (var i = 0; i < m_BuffTimers.Length; i++)
            {
                if (m_BuffTimers[i] <= 0f)
                    continue;

                m_BuffTimers[i] -= Time.deltaTime;
                if (m_BuffTimers[i] > 0f)
                    continue;

                m_BuffTimers[i] = 0f;
                expired = true;
                AnnounceBuffEnd((TemporaryBuff)i);
            }

            if (!expired)
                return;

            Apply();
            BuffsChanged?.Invoke();
        }

        void AnnounceBuffEnd(TemporaryBuff buff)
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : null;
            if (head != null)
                FloatingText.Spawn(head.position + rig.HeadYaw * new Vector3(0f, -0.3f, 1.5f), "Fin : " + NameOf(buff), m_BuffEndColor, 0.6f, 1.5f);
        }

        /// <summary>Somme des valeurs de tous les exemplaires de cet effet.</summary>
        public float Total(UpgradeEffect effect) => m_Totals[(int)effect];

        /// <summary>Faux si l'amélioration a déjà atteint son nombre maximal d'exemplaires.</summary>
        public bool CanTake(Upgrade upgrade) =>
            upgrade != null && (upgrade.maxStacks <= 0 || Stacks(upgrade.effect) < upgrade.maxStacks);

        public void Add(Upgrade upgrade)
        {
            if (!CanTake(upgrade))
                return;

            var index = (int)upgrade.effect;
            if (m_Stacks[index] == 0)
                m_Owned.Add(upgrade);

            m_Stacks[index]++;
            m_Totals[index] += upgrade.value;
            Count++;
            Apply();
            Changed?.Invoke();
        }

        // Pousse les bonus vers les scripts concernés.
        void Apply()
        {
            // Bonus temporaires des coffres, par-dessus les améliorations.
            var damageBuff = HasBuff(TemporaryBuff.DoubleDamage) ? m_BuffDamageMultiplier : 1f;
            var fastRing = HasBuff(TemporaryBuff.FastRing);
            var perfectShots = HasBuff(TemporaryBuff.PerfectShots);

            foreach (var bow in FindObjectsByType<Bow>())
            {
                bow.DamageMultiplier = DamageMultiplier * damageBuff;
                bow.SpeedMultiplier = ArrowSpeedMultiplier;
                bow.RingSpeedMultiplier = RingSpeedMultiplier * (fastRing ? m_BuffRingSpeedMultiplier : 1f);
                bow.BandWidthMultiplier = BandWidthMultiplier * (fastRing ? m_BuffRingBandMultiplier : 1f);

                // Tirs parfaits : l'anneau devient presque entièrement vert, et tout tir à pleine tension compte comme parfait.
                bow.GoldWidthMultiplier = GoldWidthMultiplier * (perfectShots ? 20f : 1f);
                bow.ForcePerfect = perfectShots;
            }

            var player = PlayerHealth.Instance;
            if (player != null)
            {
                if (m_BaseMaxHealth < 0f)
                    m_BaseMaxHealth = player.Health.Max;
                player.Health.SetMaxHealth(m_BaseMaxHealth + BonusMaxHealth);
            }

            var score = ScoreManager.Instance;
            if (score != null)
                score.MoneyRateBonus = MoneyRateBonus;

            Hitbox.HeadDamageMultiplier = HeadDamageMultiplier;
        }

        // Vampirisme : chaque tir à la tête rend des PV.
        void OnEnemyDamaged(Enemy enemy, DamageInfo info)
        {
            var heal = LifeOnHeadshot;
            if (heal <= 0f || info.Zone != HitZone.Head || !(info.Source is Arrow))
                return;

            var player = PlayerHealth.Instance;
            if (player == null || !player.IsAlive)
                return;

            player.Health.Heal(heal);
            FloatingText.Spawn(info.Point + Vector3.up * 0.4f, $"+{heal:0} PV", m_HealPopupColor, 0.7f, 1f);
        }
    }
}
