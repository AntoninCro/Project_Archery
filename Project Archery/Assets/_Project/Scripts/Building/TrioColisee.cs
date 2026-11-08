using System;
using System.Collections.Generic;
using Archery.Combat;
using Archery.Core;
using Archery.Enemies;
using Archery.UI;
using Archery.Waves;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Archery.Building
{
    /// <summary>
    /// Le trio <b>Colisée</b> (Chant des sirènes + Barricade, 1er novembre ; <c>Docs/Refonte_TowerDefense.md</c>, 12.19) : deux
    /// gradins de marbre de part et d'autre de la voie, une sirène de bronze sur sa colonne. Une <b>arène</b> s'élève autour d'un
    /// groupe d'ennemis : hypnotisés (« Gladiateurs ! »), ils se battent entre eux ; le dernier debout <b>se range de ton côté</b>
    /// (une couronne de laurier dorée) et frappe les ennemis qui passent. Ta flèche élimine le gladiateur qu'elle touche.
    /// <list type="bullet">
    /// <item>1 : une arène toutes les 15 s, un champion à la fois ;</item>
    /// <item>2 : deux champions à la fois ; un champion qui tombe éclate en bulles qui hypnotisent ses voisins (ils se battent) ;</item>
    /// <item>3, <b>les jeux du cirque</b> : en plus, toutes les 30 s, l'arène s'élargit sur toute la voie (20 m) et avale un groupe
    /// entier ; le vainqueur devient géant (×3), en armure dorée, et marche à contre-sens sur la voie.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Comme le Chant des sirènes : le charme (<see cref="Enemy.Charm"/>) fait courir les ennemis où le trio les envoie, et c'est le
    /// trio qui porte les coups. Un champion ne prend plus de dégâts (ni des flèches ni des bâtiments : il est de ton côté) ; il
    /// tombe au bout de son temps, ou quand il ne reste plus que lui (sinon la vague ne finirait pas). Les boss ne sont jamais
    /// gladiateurs (l'arène les ralentit) ; les volants pas touchés.
    /// <para>Après le premier essai (1er novembre) : les champions de tous les Colisées sont du même camp (deux champions sortis
    /// de deux arènes se battaient sans fin, sans pouvoir se tuer) ; un tireur gladiateur ou champion tire sur sa cible au lieu de
    /// rester à regarder de loin (<see cref="Enemy.ShootAt"/>) ; le grand champion porte une armure dorée (ses matériaux,
    /// copiés et dorés le temps qu'il est champion).</para>
    /// </remarks>
    public class TrioColisee : TowerTrio
    {
        [Header("Le colisée")]
        [Tooltip("Un pan de mur du modèle (repère en bas, au milieu ; sa largeur le long de son axe X) : les murs de l'arène en " +
                 "sont des copies.")]
        [SerializeField]
        Transform m_WallTemplate;

        [Tooltip("Largeur (m) du pan de mur du modèle.")]
        [SerializeField]
        float m_WallWidth = 1.6f;

        [Tooltip("La couronne de laurier du modèle (sur la tête de la sirène) : chaque champion en porte une copie.")]
        [SerializeField]
        Transform m_CrownTemplate;

        [Tooltip("Portée (m, à plat) : où l'arène peut s'élever.")]
        [SerializeField]
        float m_Range = 16f;

        [Header("L'arène")]
        [Tooltip("Temps (s) entre deux arènes, par niveau.")]
        [SerializeField]
        float[] m_IntervalPerLevel = { 15f, 13f, 12f };

        [Tooltip("Rayon (m) de l'arène ; il faut au moins deux ennemis dedans, au plus tant de gladiateurs.")]
        [SerializeField]
        float m_ArenaRadius = 4f;

        [SerializeField]
        int m_MaxGladiators = 5;

        [Tooltip("Durée (s) au plus d'un combat : à la fin, le plus en forme gagne.")]
        [SerializeField]
        float m_FightTime = 10f;

        [Tooltip("Un coup de gladiateur coûte cette part des PV max de celui qu'il frappe ; le temps (s) entre deux coups.")]
        [SerializeField]
        float m_HitShare = 0.2f;

        [SerializeField]
        float m_HitCooldown = 0.7f;

        [Header("Les champions")]
        [Tooltip("Champions à la fois, par niveau.")]
        [SerializeField]
        int[] m_ChampionsPerLevel = { 1, 2, 2 };

        [Tooltip("Combien de temps (s) un champion se bat pour toi.")]
        [SerializeField]
        float m_ChampionTime = 30f;

        [Tooltip("Dégâts d'un coup de champion, par niveau (vague 1, en Normal) ; le temps (s) entre deux coups.")]
        [SerializeField]
        float[] m_ChampionDamagePerLevel = { 30f, 40f, 50f };

        [SerializeField]
        float m_ChampionCooldown = 0.8f;

        [Tooltip("Niveau 2 : un champion qui tombe hypnotise tant de voisins (dans ce rayon, m), qui se battent tant de secondes.")]
        [SerializeField]
        int m_BubbleCount = 3;

        [SerializeField]
        float m_BubbleRadius = 6f;

        [SerializeField]
        float m_BubbleFightTime = 5f;

        [Header("Les jeux du cirque (niveau 3)")]
        [Tooltip("Temps (s) entre deux jeux du cirque ; il faut au moins six ennemis à portée.")]
        [SerializeField]
        float m_CircusInterval = 30f;

        [SerializeField]
        float m_CircusRadius = 10f;

        [SerializeField]
        int m_CircusGladiators = 14;

        [Tooltip("Le champion des jeux : sa taille (fois la sienne) et ses dégâts (fois ceux d'un champion).")]
        [SerializeField]
        float m_GiantScale = 3f;

        [SerializeField]
        float m_GiantDamage = 3f;

        [Header("Les tireurs (1er novembre)")]
        [Tooltip("Un tireur champion tire sur sa proie de cette distance (m) ; le grand champion, une fois et demie plus loin.")]
        [SerializeField]
        float m_RangedReach = 10f;

        [Header("Sons")]
        [SerializeField]
        AudioClip m_RiseClip;

        [SerializeField]
        AudioClip m_HitClip;

        [SerializeField]
        AudioClip m_ChampionClip;

        class Fight
        {
            public Vector3 Center;
            public float Radius;
            public readonly List<Enemy> Fighters = new List<Enemy>();
            public readonly List<Transform> Walls = new List<Transform>();
            public readonly Dictionary<Enemy, float> NextHit = new Dictionary<Enemy, float>();
            public float Start;
            public float Until;
            public bool Circus;
            public bool Ended;
        }

        class Champion
        {
            public Enemy Enemy;
            public float Until;
            public Transform Crown;
            public Func<DamageInfo, float> Filter;
            public Vector3 Scale;
            public bool Giant;
            public float NextHit;
            public float Height;
            public readonly List<(Renderer Renderer, Material[] Materials)> Armor = new List<(Renderer, Material[])>();
        }

        // Les champions de tous les Colisées (1er novembre) : du même camp, ils ne se chassent pas entre eux, et la vague finit quand
        // il ne reste qu'eux, d'où qu'ils viennent.
        static readonly HashSet<Enemy> s_Champions = new HashSet<Enemy>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetChampions() => s_Champions.Clear();

        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_ColorId = Shader.PropertyToID("_Color");
        static readonly int k_MetallicId = Shader.PropertyToID("_Metallic");
        static readonly int k_SmoothnessId = Shader.PropertyToID("_Smoothness");
        static readonly int k_EmissionId = Shader.PropertyToID("_EmissionColor");
        static readonly Color k_Armor = new Color(1f, 0.78f, 0.3f, 1f);

        static readonly Color k_Gold = new Color(1f, 0.82f, 0.35f, 1f);
        static readonly Color k_Pink = new Color(1f, 0.55f, 0.8f, 1f);
        static readonly Color k_Dust = new Color(0.85f, 0.8f, 0.7f, 1f);

        readonly List<Fight> m_Fights = new List<Fight>();
        readonly List<Champion> m_Champions = new List<Champion>();
        readonly Dictionary<Enemy, Action<Health, DamageInfo>> m_Watched = new Dictionary<Enemy, Action<Health, DamageInfo>>();
        readonly HashSet<Enemy> m_Eliminating = new HashSet<Enemy>();
        readonly List<Enemy> m_Scratch = new List<Enemy>();
        float m_NextArena;
        float m_NextCircus;

        protected override void OnDestroy()
        {
            foreach (var fight in m_Fights)
                EndFight(fight, false);
            for (var i = m_Champions.Count - 1; i >= 0; i--)
                Dismiss(m_Champions[i], false);
            m_Fights.Clear();
            m_Champions.Clear();
            base.OnDestroy();
        }

        protected override void Update()
        {
            base.Update();
            UpdateFights();
            UpdateChampions();
            if (!IsBuilt || IsUnderConstruction)
                return;

            var level = Mathf.Max(1, Level);
            if (level >= 3 && Time.time >= m_NextCircus && !HasCircus() && TryArena(true))
            {
                m_NextCircus = Time.time + m_CircusInterval / OverchargeRate;
                return;
            }

            if (Time.time < m_NextArena || m_Fights.Count > 0 || m_Champions.Count >= ValueFor(m_ChampionsPerLevel, level, 1))
                return;
            if (TryArena(false))
                m_NextArena = Time.time + ValueFor(m_IntervalPerLevel, level, 15f) / OverchargeRate;
        }

        bool HasCircus()
        {
            foreach (var fight in m_Fights)
            {
                if (fight.Circus)
                    return true;
            }

            return false;
        }

        // ---------------------------------------------------------------- l'arène

        bool CanFight(Enemy enemy) =>
            enemy != null && enemy.IsAlive && enemy.CanBeLaunched && !enemy.IsAirborne && !enemy.IsCharmed && !s_Champions.Contains(enemy) &&
            !IsInvolved(enemy);

        // Un gladiateur tireur s'arrête au milieu de l'arène et tire de là ; les autres vont au contact.
        static float FightStop(Enemy enemy, Fight fight) => enemy.IsRanged ? fight.Radius * 0.5f : -1f;

        bool IsInvolved(Enemy enemy)
        {
            foreach (var fight in m_Fights)
            {
                if (fight.Fighters.Contains(enemy))
                    return true;
            }

            foreach (var champion in m_Champions)
            {
                if (champion.Enemy == enemy)
                    return true;
            }

            return false;
        }

        // L'arène s'élève autour du groupe le plus serré à portée : il faut au moins deux ennemis dedans (six pour les jeux).
        bool TryArena(bool circus)
        {
            var radius = circus ? m_CircusRadius : m_ArenaRadius;
            m_Scratch.Clear();
            foreach (var enemy in EnemiesNear(transform.position, m_Range + (circus ? 6f : 0f)))
            {
                if (CanFight(enemy))
                    m_Scratch.Add(enemy);
            }

            if (m_Scratch.Count < (circus ? 6 : 2))
                return false;

            var center = m_Scratch[0].transform.position;
            var best = -1;
            foreach (var enemy in m_Scratch)
            {
                var count = 0;
                foreach (var other in m_Scratch)
                {
                    var gap = other.transform.position - enemy.transform.position;
                    gap.y = 0f;
                    if (gap.sqrMagnitude <= radius * radius * 0.6f)
                        count++;
                }

                if (count > best)
                {
                    best = count;
                    center = enemy.transform.position;
                }
            }

            var fight = new Fight { Center = center, Radius = radius, Start = Time.time, Until = Time.time + m_FightTime, Circus = circus };
            var max = circus ? m_CircusGladiators : m_MaxGladiators;
            foreach (var enemy in EnemiesNear(center, radius, true))
            {
                if (enemy.IsBoss)
                {
                    enemy.Slow(0.5f, m_FightTime);
                    continue;
                }

                if (fight.Fighters.Count >= max || !CanFight(enemy) || !enemy.Charm(m_FightTime + 1f, center, FightStop(enemy, fight)))
                    continue;
                fight.Fighters.Add(enemy);
                fight.NextHit[enemy] = Time.time + Random.Range(0.4f, 1f);
                Watch(enemy);
            }

            if (fight.Fighters.Count < 2)
            {
                foreach (var enemy in fight.Fighters)
                {
                    Unwatch(enemy);
                    enemy.Uncharm();
                }

                return false;
            }

            RaiseWalls(fight);
            m_Fights.Add(fight);
            FloatingText.Spawn(center + Vector3.up * 3f, circus ? "Les jeux du cirque !" : "Gladiateurs !", k_Gold, circus ? 2.2f : 1.6f, 2f);
            if (circus)
                HeadMessages.Show("Les jeux du cirque !", k_Gold, 2.5f);
            Sfx.Play(m_RiseClip, center, 1f, circus ? 0.7f : 1f);
            return true;
        }

        // Les murs : des copies du pan de mur du modèle, en cercle, tournés vers le centre ; ils montent du sol.
        void RaiseWalls(Fight fight)
        {
            if (m_WallTemplate == null)
                return;

            var count = fight.Circus ? 28 : 16;
            var width = 2f * Mathf.PI * fight.Radius / count;
            for (var i = 0; i < count; i++)
            {
                var angle = i * Mathf.PI * 2f / count;
                var point = fight.Center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * fight.Radius;
                point.y = GroundY(point, 1.5f);
                var wall = Instantiate(m_WallTemplate.gameObject).transform;
                wall.name = "Mur d'arène";
                var scale = m_WallTemplate.lossyScale;
                wall.localScale = new Vector3(scale.x * width / Mathf.Max(0.1f, m_WallWidth) * 1.05f, scale.y * (fight.Circus ? 1.4f : 1f), scale.z);
                var inward = fight.Center - point;
                inward.y = 0f;
                wall.rotation = inward.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(inward) : Quaternion.identity;
                wall.position = point + Vector3.down * 2.5f;
                fight.Walls.Add(wall);
            }
        }

        // Les murs montent (0,6 s), puis redescendent à la fin du combat et disparaissent.
        static void MoveWalls(Fight fight, float t)
        {
            foreach (var wall in fight.Walls)
            {
                if (wall == null)
                    continue;
                var position = wall.position;
                var ground = GroundY(position + Vector3.up * 3f, 1.5f);
                position.y = ground - 2.5f * (1f - Mathf.SmoothStep(0f, 1f, t));
                wall.position = position;
            }
        }

        void UpdateFights()
        {
            var level = Mathf.Max(1, Level);
            for (var i = m_Fights.Count - 1; i >= 0; i--)
            {
                var fight = m_Fights[i];
                if (fight.Ended)
                {
                    // Les murs redescendent.
                    var down = 1f - Mathf.Clamp01((Time.time - fight.Until) / 0.8f);
                    MoveWalls(fight, down);
                    if (down > 0f)
                        continue;
                    foreach (var wall in fight.Walls)
                    {
                        if (wall != null)
                            Destroy(wall.gameObject);
                    }

                    m_Fights.RemoveAt(i);
                    continue;
                }

                MoveWalls(fight, Mathf.Clamp01((Time.time - fight.Start) / 0.6f));
                fight.Fighters.RemoveAll(enemy => enemy == null || !enemy.IsAlive);
                foreach (var enemy in fight.Fighters)
                    Brawl(fight, enemy, level);

                if (fight.Fighters.Count > 1 && Time.time < fight.Until)
                    continue;

                Crown(fight, level);
                EndFight(fight, true);
            }
        }

        // Un gladiateur court sur son voisin le plus proche, et le frappe quand il l'atteint ; il ne sort pas de l'arène. Un tireur
        // reste au milieu et tire sur lui (le coup porte quand son projectile arrive).
        void Brawl(Fight fight, Enemy enemy, int level)
        {
            if (!enemy.IsCharmed && !enemy.IsAirborne)
                enemy.Charm(Mathf.Max(0.5f, fight.Until - Time.time + 1f), fight.Center, FightStop(enemy, fight));

            Enemy foe = null;
            var nearest = float.MaxValue;
            foreach (var other in fight.Fighters)
            {
                if (other == enemy || other == null || !other.IsAlive)
                    continue;
                var d = (other.transform.position - enemy.transform.position).sqrMagnitude;
                if (d < nearest)
                {
                    nearest = d;
                    foe = other;
                }
            }

            var flat = enemy.transform.position - fight.Center;
            flat.y = 0f;
            if (enemy.IsRanged)
            {
                enemy.SetCharmDestination(fight.Center);
                if (foe == null || Time.time < fight.NextHit[enemy])
                    return;

                fight.NextHit[enemy] = Time.time + m_HitCooldown * Random.Range(1.3f, 1.7f);
                var target = foe;
                enemy.ShootAt(foe.Center, () =>
                {
                    if (target != null && target.IsAlive && fight.Fighters.Contains(target))
                        GladiatorHit(target, enemy);
                });
                return;
            }

            enemy.SetCharmDestination(foe != null && flat.magnitude < fight.Radius ? foe.transform.position : fight.Center);
            if (foe == null || nearest > 1.7f * 1.7f || Time.time < fight.NextHit[enemy])
                return;

            fight.NextHit[enemy] = Time.time + m_HitCooldown * Random.Range(0.8f, 1.2f);
            GladiatorHit(foe, enemy);
        }

        // Un coup de gladiateur : une part des PV max de celui qu'il frappe.
        void GladiatorHit(Enemy foe, Enemy enemy)
        {
            foe.Health.TakeDamage(new DamageInfo
            {
                Amount = foe.Health.Max * m_HitShare,
                Zone = HitZone.Body,
                Point = foe.Center,
                Direction = foe.transform.position - enemy.transform.position,
                Source = this,
            });
            Burst(foe.Center, k_Dust, 8, 3f, 0.25f, 0.4f, 0.6f);
            Sfx.Play(m_HitClip, foe.Center, 0.6f, Random.Range(0.9f, 1.2f));
        }

        // Le dernier debout (ou le plus en forme à la fin du temps) devient champion, s'il reste une place.
        void Crown(Fight fight, int level)
        {
            Enemy winner = null;
            var best = -1f;
            foreach (var enemy in fight.Fighters)
            {
                if (enemy != null && enemy.IsAlive && enemy.Health.Normalized > best)
                {
                    best = enemy.Health.Normalized;
                    winner = enemy;
                }
            }

            if (winner == null || (!fight.Circus && m_Champions.Count >= ValueFor(m_ChampionsPerLevel, level, 1)))
                return;
            fight.Fighters.Remove(winner);
            Unwatch(winner);
            MakeChampion(winner, fight.Circus);
        }

        void EndFight(Fight fight, bool animate)
        {
            foreach (var enemy in fight.Fighters)
            {
                if (enemy == null)
                    continue;
                Unwatch(enemy);
                enemy.Uncharm();
            }

            fight.Fighters.Clear();
            fight.Ended = true;
            fight.Until = Time.time;
            if (animate)
                return;
            foreach (var wall in fight.Walls)
            {
                if (wall != null)
                    Destroy(wall.gameObject);
            }

            fight.Walls.Clear();
        }

        // Ta flèche élimine le gladiateur qu'elle touche (l'élimination compte pour toi : ses points, ton combo).
        void Watch(Enemy enemy)
        {
            if (m_Watched.ContainsKey(enemy))
                return;
            Action<Health, DamageInfo> handler = (health, info) => OnGladiatorHit(enemy, info);
            m_Watched[enemy] = handler;
            enemy.Health.Damaged += handler;
        }

        void Unwatch(Enemy enemy)
        {
            if (enemy == null || !m_Watched.TryGetValue(enemy, out var handler))
                return;
            m_Watched.Remove(enemy);
            if (enemy.Health != null)
                enemy.Health.Damaged -= handler;
        }

        void OnGladiatorHit(Enemy enemy, DamageInfo info)
        {
            if (!Enemy.IsPlayerHit(info) || enemy == null || !enemy.IsAlive || !m_Eliminating.Add(enemy))
                return;

            FloatingText.Spawn(enemy.Center + Vector3.up * 1.2f, "Éliminé !", k_Gold, 1.3f, 1.2f);
            enemy.Health.TakeDamage(new DamageInfo
            {
                Amount = enemy.Health.Current + 1f,
                Zone = HitZone.Body,
                Point = enemy.Center,
                Direction = info.Direction,
                Source = info.Source,
            });
            m_Eliminating.Remove(enemy);
        }

        // ---------------------------------------------------------------- les champions

        void MakeChampion(Enemy enemy, bool giant)
        {
            var champion = new Champion
            {
                Enemy = enemy,
                Until = Time.time + m_ChampionTime,
                Filter = enemy.Health.DamageFilter,
                Scale = enemy.transform.localScale,
                Height = HeightOf(enemy),
            };

            // De ton côté : plus aucun coup ne le blesse, sauf le trio lui-même (quand il tombe).
            var previous = champion.Filter;
            enemy.Health.DamageFilter = info => ReferenceEquals(info.Source, this) ? (previous != null ? previous(info) : info.Amount) : 0f;
            if (giant && s_Reshaped.Add(enemy))
            {
                champion.Giant = true;
                enemy.transform.localScale = champion.Scale * m_GiantScale;
                GildArmor(champion);
            }

            s_Champions.Add(enemy);
            enemy.IsAlly = true;
            enemy.Charm(m_ChampionTime + 1f, enemy.transform.position, ChampionStop(champion));
            if (m_CrownTemplate != null)
            {
                champion.Crown = Instantiate(m_CrownTemplate.gameObject).transform;
                champion.Crown.name = "Couronne de champion";
                champion.Crown.localScale = m_CrownTemplate.lossyScale * (champion.Giant ? m_GiantScale : 1f);
            }

            m_Champions.Add(champion);
            FloatingText.Spawn(enemy.Center + Vector3.up * (champion.Giant ? 5f : 2f), champion.Giant ? "Le grand champion !" : "Champion !", k_Gold,
                               champion.Giant ? 2.4f : 1.8f, 2f);
            if (champion.Giant)
                HeadMessages.Show("Le guerrier d'or !", k_Gold, 2.5f);
            Sfx.Play(m_ChampionClip, enemy.Center, 1f, champion.Giant ? 0.8f : 1f);
            Burst(enemy.Center, k_Gold, champion.Giant ? 120 : 50, champion.Giant ? 9f : 6f, champion.Giant ? 0.45f : 0.3f, 0.9f, 0.3f);
        }

        // Un tireur champion s'arrête à distance de tir de sa proie ; les autres vont au contact.
        float ChampionStop(Champion champion) => champion.Enemy.IsRanged ? RangedReach(champion) - 2f : -1f;

        float RangedReach(Champion champion) => m_RangedReach * (champion.Giant ? 1.5f : 1f);

        // L'armure dorée du grand champion (le joueur, 1er novembre : « il n'y a pas de guerrier en armure dorée ») : chacun de ses
        // matériaux est copié, doré et rendu métallique, avec une lueur d'or ; les siens lui sont rendus quand il tombe.
        void GildArmor(Champion champion)
        {
            foreach (var renderer in champion.Enemy.GetComponentsInChildren<Renderer>())
            {
                if (!(renderer is MeshRenderer || renderer is SkinnedMeshRenderer) || !renderer.enabled)
                    continue;

                var own = renderer.sharedMaterials;
                var gold = new Material[own.Length];
                for (var i = 0; i < own.Length; i++)
                {
                    if (own[i] == null)
                        continue;
                    gold[i] = new Material(own[i]) { name = own[i].name + " (or)" };
                    Gild(gold[i]);
                }

                champion.Armor.Add((renderer, own));
                renderer.sharedMaterials = gold;
            }
        }

        static void Gild(Material material)
        {
            if (material.HasProperty(k_BaseColorId))
                material.SetColor(k_BaseColorId, k_Armor);
            else if (material.HasProperty(k_ColorId))
                material.SetColor(k_ColorId, k_Armor);
            if (material.HasProperty(k_MetallicId))
                material.SetFloat(k_MetallicId, 0.9f);
            if (material.HasProperty(k_SmoothnessId))
                material.SetFloat(k_SmoothnessId, 0.72f);
            if (material.HasProperty(k_EmissionId))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor(k_EmissionId, k_Armor * 0.3f);
            }
        }

        static void Ungild(Champion champion)
        {
            foreach (var (renderer, own) in champion.Armor)
            {
                if (renderer == null)
                    continue;
                foreach (var gold in renderer.sharedMaterials)
                {
                    if (gold != null)
                        Destroy(gold);
                }

                renderer.sharedMaterials = own;
            }

            champion.Armor.Clear();
        }

        static float HeightOf(Enemy enemy)
        {
            var renderer = enemy.GetComponentInChildren<Renderer>();
            return renderer != null ? Mathf.Max(1f, renderer.bounds.max.y - enemy.transform.position.y) : 1.8f;
        }

        // Le champion court sur l'ennemi le plus proche et le frappe ; sans personne, il marche vers la forêt. Il tombe au bout de
        // son temps, à la fin de la vague, ou quand il ne reste plus que des champions.
        void UpdateChampions()
        {
            if (m_Champions.Count == 0)
                return;

            var level = Mathf.Max(1, Level);
            var waves = WaveManager.Instance;
            var others = 0;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy != null && enemy.IsAlive && !IsChampion(enemy))
                    others++;
            }

            for (var i = m_Champions.Count - 1; i >= 0; i--)
            {
                var champion = m_Champions[i];
                var enemy = champion.Enemy;
                if (enemy == null || !enemy.IsAlive)
                {
                    Dismiss(champion, false);
                    continue;
                }

                if (Time.time >= champion.Until || others == 0 || (waves != null && waves.Phase != WavePhase.Wave))
                {
                    Dismiss(champion, true);
                    continue;
                }

                if (!enemy.IsCharmed && !enemy.IsAirborne)
                    enemy.Charm(champion.Until - Time.time + 1f, enemy.transform.position, ChampionStop(champion));
                if (champion.Crown != null)
                    champion.Crown.position = enemy.transform.position + Vector3.up * (champion.Height * (champion.Giant ? m_GiantScale : 1f) + 0.1f);
                Hunt(champion, level);
            }
        }

        // Un champion de n'importe quel Colisée.
        static bool IsChampion(Enemy enemy) => s_Champions.Contains(enemy);

        void Hunt(Champion champion, int level)
        {
            var enemy = champion.Enemy;
            Enemy prey = null;
            var nearest = 25f * 25f;
            foreach (var other in GroundEnemies())
            {
                if (other == enemy || IsChampion(other))
                    continue;
                var d = (other.transform.position - enemy.transform.position).sqrMagnitude;
                if (d < nearest)
                {
                    nearest = d;
                    prey = other;
                }
            }

            if (prey == null)
            {
                enemy.SetCharmDestination(enemy.transform.position + ForestwardAt(enemy.transform.position) * 6f);
                return;
            }

            enemy.SetCharmDestination(prey.transform.position);
            var reach = enemy.IsRanged ? RangedReach(champion) : champion.Giant ? 3.5f : 1.8f;
            if (nearest > reach * reach || Time.time < champion.NextHit)
                return;

            champion.NextHit = Time.time + m_ChampionCooldown * (enemy.IsRanged ? 1.6f : 1f);
            if (enemy.IsRanged)
            {
                // Un tireur : le coup porte quand son projectile arrive.
                var target = prey;
                enemy.ShootAt(prey.Center, () =>
                {
                    if (target != null && target.IsAlive && enemy != null && enemy.IsAlive)
                        ChampionHit(champion, target, level);
                });
                return;
            }

            ChampionHit(champion, prey, level);
        }

        void ChampionHit(Champion champion, Enemy prey, int level)
        {
            var damage = ValueFor(m_ChampionDamagePerLevel, level, 30f) * (champion.Giant ? m_GiantDamage : 1f);
            if (Hurt(prey, damage, prey.Center, prey.transform.position - champion.Enemy.transform.position) && champion.Giant)
                KnockDown(prey, champion.Enemy.transform.position, 5f, 6f);
            Burst(prey.Center, k_Gold, 10, 3f, 0.2f, 0.4f, 0.5f);
            Sfx.Play(m_HitClip, prey.Center, 0.8f, champion.Giant ? 0.6f : 1f);
        }

        // Le champion tombe (glorieux) : il reprend sa taille, perd sa couronne ; au niveau 2, il éclate en bulles qui hypnotisent
        // ses voisins, qui se battent un moment.
        void Dismiss(Champion champion, bool fall)
        {
            m_Champions.Remove(champion);
            var enemy = champion.Enemy;
            s_Champions.Remove(enemy);
            Ungild(champion);
            if (champion.Crown != null)
                Destroy(champion.Crown.gameObject);
            if (champion.Giant)
                s_Reshaped.Remove(enemy);
            if (enemy == null)
                return;

            enemy.Health.DamageFilter = champion.Filter;
            enemy.IsAlly = false;
            if (champion.Giant)
                enemy.transform.localScale = champion.Scale;

            // Le Colisée retiré (la vitrine, F4 ; une fusion) : le champion redevient un ennemi tout de suite. Essai du joueur, 1er
            // novembre : il gardait son charme (jusqu'à 31 s), courait vers sa dernière proie, puis restait planté là.
            if (!fall && enemy.IsAlive)
                enemy.Uncharm();
            if (!fall || !enemy.IsAlive)
                return;

            var point = enemy.Center;
            enemy.Uncharm();
            enemy.Health.TakeDamage(new DamageInfo
            {
                Amount = enemy.Health.Current + 1f,
                Zone = HitZone.Body,
                Point = point,
                Direction = Vector3.down,
                Source = this,
            });
            Burst(point, k_Gold, 40, 5f, 0.3f, 0.8f, 0.3f);
            if (Mathf.Max(1, Level) >= 2)
                Bubbles(point);
        }

        // Les bulles du champion tombé : ses voisins se battent entre eux, sans arène.
        void Bubbles(Vector3 point)
        {
            var ground = point;
            ground.y = GroundY(point, 1.5f);
            var fight = new Fight { Center = ground, Radius = m_BubbleRadius, Start = Time.time, Until = Time.time + m_BubbleFightTime };
            foreach (var enemy in EnemiesNear(ground, m_BubbleRadius))
            {
                if (fight.Fighters.Count >= m_BubbleCount || !CanFight(enemy) || !enemy.Charm(m_BubbleFightTime + 1f, ground, FightStop(enemy, fight)))
                    continue;
                fight.Fighters.Add(enemy);
                fight.NextHit[enemy] = Time.time + Random.Range(0.3f, 0.8f);
                Watch(enemy);
                Burst(enemy.Center, k_Pink, 16, 2f, 0.4f, 0.8f, -0.1f);
            }

            if (fight.Fighters.Count < 2)
            {
                EndFight(fight, false);
                return;
            }

            m_Fights.Add(fight);
            FloatingText.Spawn(ground + Vector3.up * 2.5f, "Hypnotisés !", k_Pink, 1.4f, 1.5f);
        }

        /// <summary>La portée dessinée pendant la pose (3 novembre, le placement lisible).</summary>
        public override float ReachAt(int level) => m_Range;
    }
}
