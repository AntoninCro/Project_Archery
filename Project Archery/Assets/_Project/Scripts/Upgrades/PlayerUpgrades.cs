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
    /// Les améliorations obtenues pendant la partie (GDD, section 6), jusqu'à la fin de la partie : depuis le 25 octobre,
    /// des objets pris une seule fois (Max Stacks), dont certains ne jouent qu'aux tirs parfaits (<see cref="UpgradeTrigger"/>),
    /// et des parchemins qui se cumulent sans limite. Ce script applique les bonus de statistiques (arc, PV, or, tête, vampirisme) ;
    /// la chance est lue par la boutique, et les flèches spéciales sont gérées par <see cref="SpecialArrows"/>.
    /// Il gère aussi les bonus temporaires des coffres (<see cref="TemporaryBuff"/>).
    /// </summary>
    /// <remarks>
    /// Depuis le 26 octobre (demandé par le joueur), une pièce de la flèche n'en remplace plus une autre : toutes celles qu'on
    /// prend restent dans l'atelier (à côté de la boutique), et une seule par emplacement est équipée. Une pièce
    /// prise quand son emplacement est vide s'équipe toute seule ; sinon, on la monte dans l'atelier (<see cref="Equip"/>).
    /// Seules les pièces équipées jouent : <see cref="Stacks"/> et <see cref="Total(UpgradeEffect)"/> ne comptent qu'elles
    /// (avec les parchemins et les objets hors emplacement) ; <see cref="Level"/> dit le niveau d'une pièce, équipée ou non.
    /// </remarks>
    [DisallowMultipleComponent]
    public class PlayerUpgrades : MonoBehaviour
    {
        // « Charge rapide » élargit aussi toutes les bandes de l'anneau de 10 % par exemplaire.
        const float k_QuickChargeBandBonus = 0.1f;

        static readonly int k_SlotCount = Enum.GetValues(typeof(ArrowSlot)).Length;

        // Parchemins (25 octobre) : la Force fait aussi aller les flèches plus vite (le cinquième de son bonus de dégâts :
        // la portée suit le carré de la vitesse), l'Adresse élargit aussi les autres bandes (la moitié de son bonus).
        const float k_StrengthSpeedShare = 0.2f;
        const float k_DexterityBandShare = 0.5f;

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

        // Ce qu'on possède (niveaux et valeurs de tous les exemplaires pris, équipés ou non) ; ce qui joue (les pièces
        // équipées, les parchemins et les objets hors emplacement), recalculé à chaque changement (RefreshActive).
        readonly int[] m_Levels = new int[k_EffectCount];
        readonly float[] m_Values = new float[k_EffectCount];
        readonly int[] m_Stacks = new int[k_EffectCount];
        readonly float[] m_BuffTimers = new float[k_BuffCount];
        readonly float[] m_Totals = new float[k_EffectCount];
        readonly bool[] m_PerfectOnly = new bool[k_EffectCount];
        readonly Upgrade[] m_Equipped = new Upgrade[k_SlotCount];
        bool m_Suspended;
        readonly List<Upgrade> m_Owned = new List<Upgrade>();
        float m_BaseMaxHealth = -1f;

        public static PlayerUpgrades Instance { get; private set; }

        /// <summary>Une amélioration vient d'être ajoutée, ou une pièce équipée.</summary>
        public event Action Changed;

        /// <summary>Un bonus temporaire commence ou se termine.</summary>
        public event Action BuffsChanged;

        /// <summary>Améliorations possédées, une fois chacune, dans l'ordre du premier achat (les pièces rangées dans l'atelier aussi).</summary>
        public IReadOnlyList<Upgrade> Owned => m_Owned;

        /// <summary>Nombre total d'améliorations obtenues pendant la partie, en boutique ou dans un coffre (chaque exemplaire compte).</summary>
        public int Count { get; private set; }

        // Les anciennes cartes de statistiques (retirées de la boutique le 25 octobre) et les parchemins s'additionnent ; le
        // pacte de verre (26 octobre) multiplie le tout (×2 au niveau 1, +0,5 par niveau).
        // Le talisman de l'avare (26 octobre au soir) multiplie aussi, selon l'or gardé (MiserBonus).
        public float DamageMultiplier => (1f + Total(UpgradeEffect.Damage) + Total(UpgradeEffect.Strength)) * GlassPactMultiplier * (1f + m_MiserBonus);

        // Le talisman de l'avare : l'or pour 1 % de dégâts en plus, et le bonus au plus, aux niveaux 1, 2 et 3.
        static readonly Vector3 k_MiserGoldPerPercent = new Vector3(20f, 15f, 10f);
        static readonly Vector3 k_MiserMax = new Vector3(1f, 1.5f, 2f);
        float m_MiserBonus;

        /// <summary>Le bonus de dégâts du talisman de l'avare en ce moment (0,35 = +35 %), selon l'or gardé.</summary>
        public float MiserBonus => m_MiserBonus;

        float ComputeMiser()
        {
            var level = Stacks(UpgradeEffect.Miser);
            var score = ScoreManager.Instance;
            if (level == 0 || score == null)
                return 0f;

            var percents = Mathf.Floor(score.Money / Mathf.Max(1f, SpecialArrows.ByLevel(k_MiserGoldPerPercent, level)));
            return Mathf.Min(SpecialArrows.ByLevel(k_MiserMax, level), percents * 0.01f);
        }

        // L'empennage plume (26 octobre) : la flèche va aussi plus vite, de 25 % par niveau (26 octobre, réglage du joueur ; 10 % avant).
        public float ArrowSpeedMultiplier =>
            1f + Total(UpgradeEffect.ArrowSpeed) + k_StrengthSpeedShare * Total(UpgradeEffect.Strength) + 0.25f * Total(UpgradeEffect.Feather);

        public float RingSpeedMultiplier => 1f + Total(UpgradeEffect.QuickCharge) + Total(UpgradeEffect.Dexterity);

        float GlassPactMultiplier => Stacks(UpgradeEffect.GlassPact) > 0 ? 1.5f + 0.5f * Stacks(UpgradeEffect.GlassPact) : 1f;

        float m_BonusRingSpeed;

        /// <summary>
        /// Vitesse de l'anneau en plus, d'une pièce qui change en cours de partie (26 octobre : la corde de transe, à chaque
        /// tir parfait d'affilée). 0,08 = 8 % plus vite.
        /// </summary>
        public float BonusRingSpeed
        {
            get => m_BonusRingSpeed;
            set
            {
                if (Mathf.Approximately(m_BonusRingSpeed, value))
                    return;
                m_BonusRingSpeed = value;
                Apply();
            }
        }

        public float BandWidthMultiplier =>
            1f + k_QuickChargeBandBonus * Stacks(UpgradeEffect.QuickCharge) + k_DexterityBandShare * Total(UpgradeEffect.Dexterity);

        public float GoldWidthMultiplier => 1f + Total(UpgradeEffect.Precision) + Total(UpgradeEffect.Dexterity);
        // Seule la Vigueur donne des PV max : les +5 PV de la Force et de l'Adresse (26 octobre, comme dans Dead Cells) ont été
        // retirés le même jour, à la demande du joueur.
        public float BonusMaxHealth => Total(UpgradeEffect.Vitality) + Total(UpgradeEffect.Vigor);
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

        /// <summary>Flèches en plus quand une flèche se divise en vol, en moyenne (déluge).</summary>
        public float DelugeAverage => Total(UpgradeEffect.Deluge);

        // La foudre, la glace, l'explosion et le perçage n'ont plus de chance depuis le 26 octobre : leurs valeurs par niveau sont
        // dans SpecialArrows.

        // Pour un tir de cette qualité : un objet qui ne joue qu'aux tirs parfaits ne compte pas pour les autres tirs.

        public float MultishotAverageFor(ShotGrade grade) => Total(UpgradeEffect.Multishot, grade);
        public float EchoAverageFor(ShotGrade grade) => Total(UpgradeEffect.Echo, grade);
        public float DelugeAverageFor(ShotGrade grade) => Total(UpgradeEffect.Deluge, grade);

        /// <summary>L'objet de cet effet ne joue qu'aux tirs parfaits (refonte du 25 octobre).</summary>
        public bool IsPerfectOnly(UpgradeEffect effect) => m_PerfectOnly[(int)effect];

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

        /// <summary>Le niveau de ce qui joue : 0 pour une pièce rangée dans l'atelier (et pour tout, pendant l'épreuve du portail).</summary>
        public int Stacks(UpgradeEffect effect) => m_Suspended ? 0 : m_Stacks[(int)effect];

        /// <summary>Secondes restantes d'un bonus temporaire (0 = inactif).</summary>
        public float BuffTimeLeft(TemporaryBuff buff) => m_Suspended ? 0f : m_BuffTimers[(int)buff];

        public bool HasBuff(TemporaryBuff buff) => !m_Suspended && m_BuffTimers[(int)buff] > 0f;

        /// <summary>
        /// Améliorations et bonus temporaires mis de côté : le joueur n'a plus que son arc, sans bonus (épreuve du portail
        /// des étoiles). Il les retrouve tels quels quand on remet à faux.
        /// </summary>
        public bool Suspended
        {
            get => m_Suspended;
            set
            {
                if (m_Suspended == value)
                    return;
                m_Suspended = value;
                Apply();
                Changed?.Invoke();
                BuffsChanged?.Invoke();
            }
        }

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
            // Mis de côté (épreuve du portail), les bonus temporaires attendent le retour.
            if (m_Suspended)
                return;

            // L'avare : les dégâts suivent l'or, repoussés vers l'arc quand ils changent d'au moins 1 %.
            var miser = ComputeMiser();
            if (Mathf.Abs(miser - m_MiserBonus) >= 0.005f)
            {
                m_MiserBonus = miser;
                Apply();
            }

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
            HeadMessages.Show("Fin : " + NameOf(buff), m_BuffEndColor, 3f, 0.8f);
        }

        /// <summary>Somme des valeurs de tous les exemplaires de cet effet (0 tant qu'elles sont mises de côté).</summary>
        public float Total(UpgradeEffect effect) => m_Suspended ? 0f : m_Totals[(int)effect];

        /// <summary>Comme <see cref="Total(UpgradeEffect)"/>, pour un tir de cette qualité : 0 hors tir parfait si l'objet ne joue qu'au parfait.</summary>
        public float Total(UpgradeEffect effect, ShotGrade grade) =>
            m_PerfectOnly[(int)effect] && grade != ShotGrade.Perfect ? 0f : Total(effect);

        /// <summary>Faux si l'amélioration a déjà atteint son nombre maximal d'exemplaires (équipée ou rangée dans l'atelier).</summary>
        public bool CanTake(Upgrade upgrade) =>
            upgrade != null && (upgrade.maxStacks <= 0 || Level(upgrade.effect) < upgrade.maxStacks);

        /// <summary>
        /// Le niveau d'une amélioration possédée (ses exemplaires pris), qu'elle soit équipée ou rangée dans l'atelier ; 0 si on
        /// ne l'a pas. Pour ce qui joue, <see cref="Stacks"/>.
        /// </summary>
        public int Level(UpgradeEffect effect) => m_Levels[(int)effect];

        /// <summary>La pièce équipée dans cet emplacement de la flèche (refonte roguelike, étapes 2 et 5, 26 octobre), ou null.</summary>
        public Upgrade InSlot(ArrowSlot slot) => slot == ArrowSlot.None ? null : m_Equipped[(int)slot];

        /// <summary>La pièce est équipée (les parchemins et les objets hors emplacement jouent toujours).</summary>
        public bool IsEquipped(Upgrade upgrade)
        {
            if (upgrade == null || m_Levels[(int)upgrade.effect] == 0)
                return false;

            var equipped = InSlot(upgrade.slot);
            return upgrade.slot == ArrowSlot.None || (equipped != null && equipped.effect == upgrade.effect);
        }

        /// <summary>Les pièces possédées pour cet emplacement, équipée ou non, dans l'ordre où on les a prises.</summary>
        public void OwnedIn(ArrowSlot slot, List<Upgrade> results)
        {
            results.Clear();
            if (slot == ArrowSlot.None)
                return;

            foreach (var owned in m_Owned)
            {
                if (owned.kind == UpgradeKind.Object && owned.slot == slot)
                    results.Add(owned);
            }
        }

        /// <summary>
        /// La pièce déjà équipée à la place de <paramref name="upgrade"/> : prendre celle-ci la range alors dans l'atelier, sans
        /// rien retirer (26 octobre). Null si l'emplacement est libre, ou si c'est la même pièce (elle monte de niveau).
        /// </summary>
        public Upgrade Occupant(Upgrade upgrade)
        {
            var current = upgrade != null ? InSlot(upgrade.slot) : null;
            return current != null && current.effect != upgrade.effect ? current : null;
        }

        /// <summary>
        /// Ajoute un exemplaire : la même pièce monte de niveau, équipée ou non. Une pièce nouvelle s'équipe toute seule si son
        /// emplacement est vide ; sinon elle attend dans l'atelier (<see cref="Equip"/>). Rien n'est jamais perdu.
        /// </summary>
        public void Add(Upgrade upgrade)
        {
            if (!CanTake(upgrade))
                return;

            var index = (int)upgrade.effect;
            if (m_Levels[index] == 0)
            {
                m_Owned.Add(upgrade);
                if (upgrade.kind == UpgradeKind.Object && upgrade.slot != ArrowSlot.None && InSlot(upgrade.slot) == null)
                    m_Equipped[(int)upgrade.slot] = upgrade;
            }

            m_Levels[index]++;
            m_Values[index] += upgrade.value;
            if (upgrade.trigger == UpgradeTrigger.PerfectShot)
                m_PerfectOnly[index] = true;
            Count++;
            RefreshActive();
            Apply();
            Changed?.Invoke();
        }

        /// <summary>
        /// Équipe une pièce possédée (l'atelier, 26 octobre) ; celle qui occupait son emplacement retourne dans
        /// l'atelier, avec tous ses niveaux. Faux si on ne la possède pas, ou si elle n'a pas d'emplacement.
        /// </summary>
        public bool Equip(Upgrade piece)
        {
            if (piece == null || piece.slot == ArrowSlot.None || m_Levels[(int)piece.effect] == 0)
                return false;

            var owned = m_Owned.Find(upgrade => upgrade.effect == piece.effect);
            if (owned == null || m_Equipped[(int)piece.slot] == owned)
                return false;

            m_Equipped[(int)piece.slot] = owned;
            RefreshActive();
            Apply();
            Changed?.Invoke();
            return true;
        }

        // Ce qui joue : les parchemins, les objets hors emplacement et les pièces équipées, avec tous leurs niveaux.
        void RefreshActive()
        {
            Array.Clear(m_Stacks, 0, m_Stacks.Length);
            Array.Clear(m_Totals, 0, m_Totals.Length);
            foreach (var owned in m_Owned)
            {
                if (!IsEquipped(owned))
                    continue;

                var index = (int)owned.effect;
                m_Stacks[index] = m_Levels[index];
                m_Totals[index] = m_Values[index];
            }
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
                bow.RingSpeedMultiplier = RingSpeedMultiplier * (fastRing ? m_BuffRingSpeedMultiplier : 1f) * (1f + m_BonusRingSpeed);
                bow.BandWidthMultiplier = BandWidthMultiplier * (fastRing ? m_BuffRingBandMultiplier : 1f);

                // Tirs parfaits : tout tir à pleine tension compte comme parfait, et l'anneau s'affiche entièrement vert
                // (Bow.NextShotPerfect, 27 octobre ; avant, le vert était élargi 20 fois : il restait un liseré rouge, et le vert
                // clair de la corde de siège et de l'arc cosmique disparaissait).
                bow.GoldWidthMultiplier = GoldWidthMultiplier;
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

        // Vampirisme : chaque tir à la tête rend des PV (une flèche, ou une faille du katana).
        void OnEnemyDamaged(Enemy enemy, DamageInfo info)
        {
            var heal = LifeOnHeadshot;
            if (heal <= 0f || info.Zone != HitZone.Head || !(info.Source is Arrow || info.Source is IPlayerShot))
                return;

            var player = PlayerHealth.Instance;
            if (player == null || !player.IsAlive)
                return;

            player.Health.Heal(heal);
            FloatingText.Spawn(info.Point + Vector3.up * 0.4f, $"+{heal:0} PV", m_HealPopupColor, 0.7f, 1f);
        }
    }
}
