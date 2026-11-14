#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Archery.Bows;
using Archery.Building;
using Archery.Combat;
using Archery.Enemies;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Archery.Journal
{
    // Les comptes du journal (3 novembre) : par vague et sur toute la partie, les ennemis, les dégâts et les éliminations par source,
    // les tirs, les dégâts reçus par le joueur et la tour, et la fluidité ; et les bilans écrits à partir d'eux. Depuis le 8 novembre :
    // plus l'or (retiré du jeu), mais les flèches qu'il a fallu pour tuer chaque sorte d'ennemi.
    public partial class GameJournal
    {
        const string k_PlayerArrows = "Joueur : flèches";

        sealed class Counts
        {
            public int Wave;
            public float Start;
            public int ScoreStart;
            public int HeadshotsStart;
            public readonly Dictionary<string, float> Damage = new Dictionary<string, float>();
            public readonly Dictionary<string, int> Kills = new Dictionary<string, int>();
            public readonly Dictionary<string, int> Spawned = new Dictionary<string, int>();
            public readonly Dictionary<string, int> Dead = new Dictionary<string, int>();
            public readonly Dictionary<string, float> PlayerHurt = new Dictionary<string, float>();
            public readonly Dictionary<string, float> TowerHurt = new Dictionary<string, float>();
            public readonly int[] Grades = new int[8];
            public int Shots;
            public int Hits;
            public int Deaths;
            public int TowerAttacks;

            // Les flèches pour tuer (8 novembre, le joueur : « 3 ou 4 flèches pour tuer certains ennemis », les orcs, la liche) : par
            // sorte d'ennemi tué, les flèches du joueur qui l'avaient touché, et combien d'ennemis de cette sorte, touchés, sont morts.
            public readonly Dictionary<string, int> ArrowsOnKilled = new Dictionary<string, int>();
            public readonly Dictionary<string, int> KilledAfterArrows = new Dictionary<string, int>();

            // Les coffres de la vague et leurs gardiens (4 novembre) : ouverts, sortis, tués.
            public int ChestsOpened;
            public int GuardsOut;
            public int GuardsKilled;

            // Les PV du joueur au plus bas, et ses PV max à ce moment (4 novembre : à 67 PV, est-on vraiment en danger ?). -1 : jamais
            // touché.
            public float PlayerLowest = -1f;
            public float PlayerLowestMax;

            // La durée de chaque image, en millisecondes (de 0 à 100 et plus) : la moyenne et les plus lentes.
            public readonly int[] FrameMs = new int[101];
            public int Frames;
            public double FrameSeconds;
        }

        Counts m_Total;
        Counts m_Wave;
        int m_WavesStarted;
        float m_LastTowerHit = -999f;
        bool m_PlayerLowTold;

        // Le nom d'une source de dégâts, gardé par objet (les flèches du pool reviennent souvent).
        readonly Dictionary<Object, string> m_SourceNames = new Dictionary<Object, string>();

        // Les flèches tirées qui n'ont pas encore touché d'ennemi.
        readonly HashSet<Arrow> m_Flying = new HashSet<Arrow>();

        // Les flèches du joueur qui ont touché chaque ennemi encore en vie (une fois chacune).
        readonly Dictionary<Enemy, List<Arrow>> m_ArrowsOn = new Dictionary<Enemy, List<Arrow>>();

        void ResetCounts()
        {
            m_Total = NewCounts(0);
            m_Wave = null;
            m_WavesStarted = 0;
            m_ArrowsOn.Clear();
            m_LastChests = -1;
            m_LastTowerHit = -999f;
            m_PlayerLowTold = false;
            m_SourceNames.Clear();
            m_Flying.Clear();
            m_BossSince.Clear();
            m_Errors.Clear();
            m_Warnings.Clear();
        }

        Counts NewCounts(int wave)
        {
            m_PlayerLowTold = false;
            var counts = new Counts
            {
                Wave = wave,
                Start = Time.unscaledTime,
                ScoreStart = m_Score != null ? m_Score.Score : 0,
                HeadshotsStart = m_Score != null ? m_Score.Headshots : 0,
            };
            if (wave <= 0)
                return counts;

            // Une vague. Les ennemis d'avant qui n'ont pas été tués (ceux qui fuient à la fin de la vague) ne comptent plus.
            m_WavesStarted++;
            m_ArrowsOn.Clear();
            return counts;
        }

        static void Add(Dictionary<string, float> map, string key, float value) =>
            map[key] = map.TryGetValue(key, out var old) ? old + value : value;

        static void Add(Dictionary<string, int> map, string key, int value) =>
            map[key] = map.TryGetValue(key, out var old) ? old + value : value;

        // ---------------------------------------------------------------- les événements

        void OnEnemyDamaged(Enemy enemy, DamageInfo info)
        {
            if (!Active || enemy == null || enemy.IsTrainingDummy || enemy.IsAlly || info.Amount <= 0f)
                return;
            var source = SourceName(info.Source);
            Add(m_Total.Damage, source, info.Amount);
            if (m_Wave != null)
                Add(m_Wave.Damage, source, info.Amount);
        }

        void OnEnemyKilled(Enemy enemy, DamageInfo info)
        {
            if (!Active || enemy == null || enemy.IsTrainingDummy || enemy.IsAlly)
                return;
            // Un gardien compte à part (« gardiens : N, N tués »), comme à son apparition : avant le 4 novembre, il comptait parmi les
            // ennemis tués et pas parmi les apparus (268 tués pour 261 apparus). Ses éliminations, elles, comptent pour leur source.
            var source = SourceName(info.Source);
            var name = EnemyName(enemy);
            Add(m_Total.Kills, source, 1);
            if (enemy.IsGuard)
                m_Total.GuardsKilled++;
            else
                Add(m_Total.Dead, name, 1);

            var arrows = 0;
            if (m_ArrowsOn.TryGetValue(enemy, out var hits))
            {
                arrows = hits.Count;
                m_ArrowsOn.Remove(enemy);
            }

            if (arrows > 0)
            {
                Add(m_Total.ArrowsOnKilled, name, arrows);
                Add(m_Total.KilledAfterArrows, name, 1);
            }

            if (m_Wave == null)
                return;
            if (arrows > 0)
            {
                Add(m_Wave.ArrowsOnKilled, name, arrows);
                Add(m_Wave.KilledAfterArrows, name, 1);
            }

            Add(m_Wave.Kills, source, 1);
            if (enemy.IsGuard)
                m_Wave.GuardsKilled++;
            else
                Add(m_Wave.Dead, name, 1);
        }

        // Un coffre apparaît avec ses gardiens (Building/CrystalSpawner).
        void OnGuardsCameOut(int count, Vector3 chest)
        {
            if (!Active)
                return;
            m_Total.GuardsOut += count;
            if (m_Wave != null)
                m_Wave.GuardsOut += count;
            Line($"Un coffre apparaît, gardé par {count} ennemis.");
        }

        // Un coffre ouvert (le nombre de coffres de la vague baisse pendant la vague) : avec les gardiens encore en vie, pour voir si le
        // joueur les tue ou fonce sur le coffre. À la fin de la vague, ceux qui restent disparaissent : pas comptés (m_LastChests = -1).
        void WatchChests()
        {
            var spawner = Archery.Building.CrystalSpawner.Instance;
            var remaining = spawner != null ? spawner.Remaining : 0;
            if (m_LastChests >= 0 && remaining < m_LastChests && m_Wave != null)
            {
                var guards = 0;
                foreach (var enemy in Enemy.Alive)
                {
                    if (enemy != null && enemy.IsGuard)
                        guards++;
                }

                for (var i = remaining; i < m_LastChests; i++)
                {
                    m_Total.ChestsOpened++;
                    m_Wave.ChestsOpened++;
                }

                Line($"Coffre ouvert (il en reste {remaining} ; gardiens encore en vie : {guards}).");
            }

            m_LastChests = remaining;
        }

        int m_LastChests = -1;

        void OnEnemySpawned(Enemy enemy, Transform point)
        {
            if (!Active || enemy == null)
                return;
            var name = EnemyName(enemy);
            Add(m_Total.Spawned, name, 1);
            if (m_Wave != null)
                Add(m_Wave.Spawned, name, 1);
        }

        void OnShotFired(Bow bow, ShotInfo shot)
        {
            if (!Active)
                return;
            var grade = Mathf.Clamp((int)shot.Grade, 0, m_Total.Grades.Length - 1);
            m_Total.Shots++;
            m_Total.Grades[grade]++;
            if (m_Wave != null)
            {
                m_Wave.Shots++;
                m_Wave.Grades[grade]++;
            }

            if (shot.Arrow != null)
            {
                m_Flying.Add(shot.Arrow);
                return;
            }

            // Un laser (l'arc cosmique) : il a déjà touché, dans la même image, avant que le tir soit annoncé, et sans flèche à suivre
            // (8 novembre au soir : le journal comptait 0 % de flèches qui touchent avec l'arc cosmique).
            if (m_PlayerHitFrame != Time.frameCount)
                return;
            m_Total.Hits++;
            if (m_Wave != null)
                m_Wave.Hits++;
        }

        // L'image du dernier coup d'un tir du joueur sur un ennemi (pour les lasers).
        int m_PlayerHitFrame = -1;

        // Une flèche tirée qui touche un ennemi (la première fois) : elle compte parmi celles qui touchent. Chaque ennemi touché
        // garde aussi les flèches qui l'ont touché (8 novembre : les flèches pour tuer), celles en plus du multitir comprises.
        void OnArrowHit(ArrowHit hit)
        {
            if (!Active || !hit.IsShot || hit.Arrow == null || hit.Collider == null)
                return;
            var enemy = hit.Collider.GetComponentInParent<Enemy>();
            if (enemy == null || enemy.IsAlly)
                return;
            if (!enemy.IsTrainingDummy && !hit.Arrow.IsExtra)
                m_PlayerHitFrame = Time.frameCount;
            if (!enemy.IsTrainingDummy && enemy.IsAlive)
            {
                if (!m_ArrowsOn.TryGetValue(enemy, out var arrows))
                    m_ArrowsOn[enemy] = arrows = new List<Arrow>(4);
                if (!arrows.Contains(hit.Arrow))
                    arrows.Add(hit.Arrow);
            }

            if (!m_Flying.Remove(hit.Arrow) || enemy.IsTrainingDummy)
                return;
            m_Total.Hits++;
            if (m_Wave != null)
                m_Wave.Hits++;
        }

        // Le vol d'une flèche est fini : elle ne compte plus. Sans ça, une flèche du pool tirée et ratée, puis reprise par la corde
        // triple (qui ne passe pas par Bow.ShotFired), comptait comme touchée : jusqu'à 148 % au premier journal (3 novembre).
        void OnShotEnded(Arrow arrow)
        {
            if (arrow != null)
                m_Flying.Remove(arrow);
        }

        void OnPlayerDamaged(Health health, DamageInfo info)
        {
            if (!Active || info.Amount <= 0f)
                return;
            var source = SourceName(info.Source);
            Add(m_Total.PlayerHurt, source, info.Amount);
            NoteLowest(m_Total, health);
            if (m_Wave != null)
            {
                Add(m_Wave.PlayerHurt, source, info.Amount);
                NoteLowest(m_Wave, health);
            }
            if (!m_PlayerLowTold && health.IsAlive && health.Normalized < 0.25f)
            {
                m_PlayerLowTold = true;
                Line($"Le joueur passe sous 25 % de ses PV ({HealthText(health)}, frappé par {source}).");
            }
        }

        static void NoteLowest(Counts counts, Health health)
        {
            var current = Mathf.Max(0f, health.Current);
            if (counts.PlayerLowest >= 0f && current >= counts.PlayerLowest)
                return;
            counts.PlayerLowest = current;
            counts.PlayerLowestMax = health.Max;
        }

        void OnPlayerDied(Health health, DamageInfo info)
        {
            if (!Active)
                return;
            m_Total.Deaths++;
            if (m_Wave != null)
                m_Wave.Deaths++;
            Line($"**Le joueur meurt** (dernier coup : {SourceName(info.Source)}).");
        }

        // Un coup sur la tour : compté par ennemi ; le premier d'une attaque est noté (comme le cor de la tour).
        void OnTowerDamaged(Health health, DamageInfo info)
        {
            if (!Active || info.Amount <= 0f)
                return;
            var source = SourceName(info.Source);
            Add(m_Total.TowerHurt, source, info.Amount);
            if (m_Wave != null)
                Add(m_Wave.TowerHurt, source, info.Amount);

            var fresh = Time.unscaledTime - m_LastTowerHit > k_AttackGap;
            m_LastTowerHit = Time.unscaledTime;
            if (!fresh)
                return;
            m_Total.TowerAttacks++;
            if (m_Wave != null)
                m_Wave.TowerAttacks++;
            Line($"La tour est attaquée ({source} ; PV {HealthText(health)}).");
        }

        void OnTowerDestroyed()
        {
            if (Active)
                Line("**La tour est détruite.**");
        }

        void CountFrame(float deltaTime)
        {
            // Une pause (le chargement de l'arène, l'éditeur en pause) n'est pas une image lente.
            if (deltaTime <= 0f || deltaTime > 0.5f)
                return;
            var ms = Mathf.Clamp(Mathf.RoundToInt(deltaTime * 1000f), 0, 100);
            m_Total.FrameMs[ms]++;
            m_Total.Frames++;
            m_Total.FrameSeconds += deltaTime;
            if (m_Wave == null)
                return;
            m_Wave.FrameMs[ms]++;
            m_Wave.Frames++;
            m_Wave.FrameSeconds += deltaTime;
        }

        // ---------------------------------------------------------------- les noms

        string SourceName(Object source)
        {
            if (source == null)
                return "inconnue";
            if (m_SourceNames.TryGetValue(source, out var name))
                return name;
            name = FindSourceName(source);
            m_SourceNames[source] = name;
            return name;
        }

        // Le bâtiment (ses carreaux, ses obus compris), le joueur (ses flèches, son katana, les pouvoirs de ses flèches, sa grenade),
        // l'ennemi ; sinon, le nom de la classe.
        static string FindSourceName(Object source)
        {
            if (source is Structure structure)
                return structure.DisplayName;
            if (source is Arrow || source is Bow)
                return k_PlayerArrows;
            if (source is Component component)
            {
                var owner = component.GetComponentInParent<Structure>();
                if (owner != null)
                    return owner.DisplayName;
                var enemy = component.GetComponentInParent<Enemy>();
                if (enemy != null)
                    return EnemyName(enemy);
                if (component.GetComponentInParent<Arrow>() != null)
                    return k_PlayerArrows;
            }

            var type = source.GetType();
            var space = type.Namespace ?? "";
            if (type.Name.Contains("Katana") || type.Name.Contains("Rift"))
                return "Joueur : katana";
            if (type.Name == "BaseballBat")
                return "Joueur : batte";
            if (space.EndsWith(".Grenade"))
                return "Joueur : grenade";
            if (space.EndsWith(".Upgrades"))
                return $"Joueur : pouvoirs des flèches ({type.Name})";
            if (space.EndsWith(".Bows"))
                return k_PlayerArrows;
            return type.Name;
        }

        static string EnemyName(Enemy enemy) =>
            enemy.Definition != null && !string.IsNullOrEmpty(enemy.Definition.displayName) ? enemy.Definition.displayName : enemy.name;

        // ---------------------------------------------------------------- les bilans

        // Le bilan d'une vague : les comptes, puis l'état de la partie (bâtiments, flèche, plans).
        void WriteWaveSummary(Counts counts, string how)
        {
            Write("");
            Write($"#### Bilan de la vague {counts.Wave} ({how}, en {Duration(Time.unscaledTime - counts.Start)})");
            Write("");
            WriteCounts(counts);
            Write($"- **Bâtiments** : {BuildingsText()}");
            Write($"- **La flèche** : {ArrowText()}");
            Write("");
        }

        // Le bilan de la partie, une seule fois : à la victoire, à la défaite, quand la scène recommence ou que le mode Jeu s'arrête.
        void CloseGame(string how)
        {
            if (!m_GameOpen || m_GameClosed)
                return;
            if (m_Wave != null)
            {
                WriteWaveSummary(m_Wave, "interrompue");
                m_Wave = null;
            }

            m_GameClosed = true;

            // Arrêtée avant sa première vague (la scène rechargée juste avant la fin du mode Jeu) : une ligne suffit.
            if (m_WavesStarted == 0)
            {
                Write("");
                Write($"### Partie {s_Games} : arrêtée avant la première vague ({how}, en {Duration(Time.unscaledTime - m_GameStart)}).");
                Write("");
                return;
            }

            var wave = m_Waves != null ? m_Waves.WaveNumber : 0;
            Write("");
            Write($"### Bilan de la partie {s_Games} : {how} (vague {wave}, en {Duration(Time.unscaledTime - m_GameStart)})");
            Write("");
            Write($"- **À la fin** : {Status()}.");
            WriteCounts(m_Total);
            Write($"- **Bâtiments à la fin** : {BuildingsText()}");
            Write($"- **La flèche à la fin** : {ArrowText()}");
            var owned = OwnedText();
            Write($"- **Objets à la fin** : {(owned.Length > 0 ? owned : "aucun")}");
            Write($"- **Plans en réserve** : {(m_Build != null ? PlansText() : "?")}");
            if (m_Errors.Count > 0)
                Write("- **Erreurs** : " + string.Join(" ; ", m_Errors.OrderByDescending(pair => pair.Value)
                                                                   .Select(pair => $"{OneLine(pair.Key)} (×{pair.Value})")));
            if (m_Warnings.Count > 0)
                Write("- **Avertissements** : " + string.Join(" ; ", m_Warnings.OrderByDescending(pair => pair.Value).Take(15)
                                                                          .Select(pair => $"{OneLine(pair.Key)} (×{pair.Value})")) +
                      (m_Warnings.Count > 15 ? $" ; et {m_Warnings.Count - 15} autres" : ""));
            Write("");
        }

        void WriteCounts(Counts counts)
        {
            var spawned = counts.Spawned.Values.Sum();
            var dead = counts.Dead.Values.Sum();
            Write($"- **Ennemis** : {spawned} apparus, {dead} tués" + (counts.Dead.Count > 0 ? $" ({EnemiesText(counts)})" : "") + ".");
            if (counts.ChestsOpened > 0 || counts.GuardsOut > 0)
                Write($"- **Coffres** : {counts.ChestsOpened} ouverts ; gardiens : {counts.GuardsOut} (en plus des ennemis apparus), " +
                      $"{counts.GuardsKilled} tués.");

            var damage = counts.Damage.Values.Sum();
            Write($"- **Dégâts aux ennemis** : {Mathf.RoundToInt(damage)}" + (damage > 0f ? " : " + Shares(counts.Damage, damage) : "") + ".");
            Write("- **Éliminations** : " + (counts.Kills.Count > 0 ? Ints(counts.Kills) : "aucune") + ".");
            if (counts.KilledAfterArrows.Count > 0)
                Write("- **Flèches pour tuer** (en moyenne, les flèches du joueur qui avaient touché chaque ennemi tué ; les bâtiments " +
                      "ont pu aider) : " + ArrowsToKillText(counts) + ".");

            var hitShare = counts.Shots > 0 ? Mathf.RoundToInt(100f * counts.Hits / counts.Shots) : 0;
            var headshots = m_Score != null ? m_Score.Headshots - counts.HeadshotsStart : 0;
            Write($"- **Tirs** : {counts.Shots} ({GradesText(counts)}) ; {counts.Hits} flèches qui touchent un ennemi ({hitShare} %) ; " +
                  $"{headshots} tirs à la tête.");

            var hurt = counts.PlayerHurt.Values.Sum();
            Write($"- **Le joueur** : {Mathf.RoundToInt(hurt)} PV perdus" + (hurt > 0f ? $" ({Shares(counts.PlayerHurt, hurt)})" : "") +
                  (counts.PlayerLowest >= 0f
                      ? $" ; au plus bas : {Mathf.CeilToInt(counts.PlayerLowest)}/{Mathf.CeilToInt(counts.PlayerLowestMax)} PV"
                      : "") +
                  $" ; morts : {counts.Deaths}.");
            var towerHurt = counts.TowerHurt.Values.Sum();
            Write($"- **La tour** : {counts.TowerAttacks} attaques, {Mathf.RoundToInt(towerHurt)} PV perdus" +
                  (towerHurt > 0f ? $" ({Shares(counts.TowerHurt, towerHurt)})" : "") + ".");

            var score = m_Score != null ? m_Score.Score - counts.ScoreStart : 0;
            Write($"- **Score** : +{score}.");
            Write($"- **Fluidité** : {FramesText(counts)}.");
        }

        // « Orc 3,4 (5 tués), Liche 3,1 (9 tués) » : la moyenne la plus haute d'abord.
        static string ArrowsToKillText(Counts counts) =>
            string.Join(", ", counts.KilledAfterArrows
                                    .Select(pair => (pair.Key, pair.Value,
                                                     Average: (counts.ArrowsOnKilled.TryGetValue(pair.Key, out var arrows) ? arrows : 0) /
                                                              (float)Mathf.Max(1, pair.Value)))
                                    .OrderByDescending(entry => entry.Average)
                                    .Select(entry => $"{entry.Key} {entry.Average.ToString("0.0", CultureInfo.GetCultureInfo("fr-FR"))} " +
                                                     $"({entry.Value} {(entry.Value > 1 ? "tués" : "tué")})"));

        static string EnemiesText(Counts counts) =>
            string.Join(", ", counts.Dead.OrderByDescending(pair => pair.Value)
                                     .Select(pair => $"{pair.Key} {pair.Value}" +
                                                     (counts.Spawned.TryGetValue(pair.Key, out var born) && born != pair.Value ? $"/{born}" : "")));

        // Les plus grosses parts d'abord : « Baliste 2 100 (17 %) ».
        static string Shares(Dictionary<string, float> map, float total) =>
            string.Join(", ", map.OrderByDescending(pair => pair.Value)
                                 .Select(pair => $"{pair.Key} {Mathf.RoundToInt(pair.Value)} ({Mathf.RoundToInt(100f * pair.Value / total)} %)"));

        static string Ints(Dictionary<string, int> map) =>
            string.Join(", ", map.OrderByDescending(pair => pair.Value).Select(pair => $"{pair.Key} {pair.Value}"));

        static string GradesText(Counts counts)
        {
            var parts = new List<string>();
            void Grade(ShotGrade grade, string name)
            {
                var count = counts.Grades[(int)grade];
                if (count > 0)
                    parts.Add($"{name} {count}");
            }

            Grade(ShotGrade.Perfect, "parfaits");
            Grade(ShotGrade.Held, "très bons");
            Grade(ShotGrade.Good, "bons");
            Grade(ShotGrade.Ok, "moyens");
            Grade(ShotGrade.Miss, "ratés");
            Grade(ShotGrade.None, "sans qualité");
            return parts.Count > 0 ? string.Join(", ", parts) : "aucun";
        }

        // La moyenne, le 1 % le plus lent, et les images de plus de 25 ms (sous 40 images par seconde).
        static string FramesText(Counts counts)
        {
            if (counts.Frames == 0 || counts.FrameSeconds <= 0.0)
                return "pas mesurée";
            var average = counts.Frames / counts.FrameSeconds;
            var slowest = 0;
            var seen = 0;
            for (var ms = counts.FrameMs.Length - 1; ms >= 0; ms--)
            {
                seen += counts.FrameMs[ms];
                if (seen < counts.Frames * 0.01f)
                    continue;
                slowest = Mathf.Max(1, ms);
                break;
            }

            var slow = 0;
            for (var ms = 26; ms < counts.FrameMs.Length; ms++)
                slow += counts.FrameMs[ms];
            return $"{Mathf.RoundToInt((float)average)} images/s en moyenne, le 1 % le plus lent à {Mathf.RoundToInt(1000f / slowest)} images/s, " +
                   $"{slow} images de plus de 25 ms sur {counts.Frames}";
        }
    }
}
#endif
