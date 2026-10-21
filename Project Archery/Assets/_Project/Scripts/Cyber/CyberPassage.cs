using System.Collections;
using System.Collections.Generic;
using Archery.BowClasses;
using Archery.Bows;
using Archery.Core;
using Archery.Economy;
using Archery.FinalBoss;
using Archery.Player;
using Archery.Portal;
using Archery.UI;
using Archery.Upgrades;
using Archery.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

namespace Archery.Cyber
{
    /// <summary>
    /// Le passage vers le toit cyberpunk (2ᵉ easter egg, GDD section 23 ; lore : Docs/Lore.md) : deux éclats de miroir
    /// glitchés (<see cref="GlitchShard"/>) sont cachés derrière les cascades. Les deux pris, la porte de l'antre du boss
    /// final glitche (elle tressaute et grésille) et se traverse, même fermée. La traverser emmène sur le toit (scène
    /// <c>Dimension_Cyberpunk</c>, <see cref="CyberDimension"/>) : la carte se fige pendant ce temps, l'arc est rangé, et
    /// le joueur a une arbalète dans chaque main. Une seule tentative par partie ; les éclats se perdent à la fin de la partie.
    /// </summary>
    /// <remarks>
    /// Comme le portail des étoiles (<see cref="StarPortal"/>) : même fondu, même carte figée (<see cref="MapFreeze"/>),
    /// même retour devant l'entrée, avec tous ses PV. Pas pendant la vague du boss final (la porte est alors ouverte pour
    /// lui). Test (éditeur, Development Build) : G donne les deux éclats, puis fait entrer.
    /// </remarks>
    [DisallowMultipleComponent]
    public class CyberPassage : MonoBehaviour
    {
        [Tooltip("La porte de l'antre du boss final : elle glitche quand les deux éclats sont pris.")]
        [SerializeField]
        LairGate m_Gate;

        [Tooltip("Les éclats de miroir à trouver.")]
        [SerializeField]
        GlitchShard[] m_Shards = new GlitchShard[0];

        [Tooltip("Nom de la scène du toit (dans la liste des scènes du build).")]
        [SerializeField]
        string m_DimensionScene = "Dimension_Cyberpunk";

        [SerializeField]
        float m_FadeTime = 1f;

        [Tooltip("Points gagnés en réussissant l'épreuve (× la difficulté).")]
        [SerializeField]
        int m_ScoreBonus = 2500;

        [Tooltip("Couleur des messages.")]
        [SerializeField]
        Color m_Color = new Color(0.3f, 0.95f, 1f);

        [Header("Sons")]
        [Tooltip("Le grésillement de la porte glitchée.")]
        [SerializeField]
        AudioClip m_CrackleClip;

        [Tooltip("La porte se met à glitcher (les deux éclats pris).")]
        [SerializeField]
        AudioClip m_GateClip;

        [SerializeField]
        AudioClip m_EnterClip;

        // Les déplacements coupés sur le toit : on reste sur la zone, on esquive avec le corps (la rotation et la gravité restent).
        readonly List<Behaviour> m_PausedLocomotion = new List<Behaviour>();
        readonly List<GameObject> m_HiddenClouds = new List<GameObject>();
        static readonly Collider[] s_Overlaps = new Collider[16];

        int m_ShardsTaken;
        bool m_Used;
        bool m_Busy;
        Scene m_MainScene;
        Scene m_Dimension;
        Bow m_Bow;
        Quiver m_Quiver;
        bool m_QuiverWasEnabled;
        SkyController m_Sky;
        Light m_Sun;

        public static CyberPassage Instance { get; private set; }

        /// <summary>Le joueur est sur le toit (la carte est figée).</summary>
        public static bool PlayerInDimension { get; private set; }

        /// <summary>L'épreuve du toit a été réussie pendant cette partie (pour le classement).</summary>
        public static bool TrialCompleted { get; private set; }

        int ShardCount
        {
            get
            {
                var count = 0;
                foreach (var shard in m_Shards)
                {
                    if (shard != null)
                        count++;
                }

                return count;
            }
        }

        void Awake()
        {
            Instance = this;
            PlayerInDimension = false;
            TrialCompleted = false;
            m_MainScene = gameObject.scene;
            if (m_Gate == null)
                m_Gate = FindAnyObjectByType<LairGate>();
        }

        void OnEnable()
        {
            foreach (var shard in m_Shards)
            {
                if (shard != null)
                    shard.Picked += OnShardPicked;
            }

            if (m_Gate != null)
                m_Gate.GlitchCrossed += OnGlitchCrossed;
        }

        void OnDisable()
        {
            foreach (var shard in m_Shards)
            {
                if (shard != null)
                    shard.Picked -= OnShardPicked;
            }

            if (m_Gate != null)
            {
                m_Gate.GlitchCrossed -= OnGlitchCrossed;
                m_Gate.SetGlitched(false);
            }
        }

        void OnDestroy()
        {
            if (Instance != this)
                return;
            Instance = null;
            PlayerInDimension = false;
        }

        void Update()
        {
            UpdateDebugKeys();
            if (m_Gate == null)
                return;

            // La porte glitche tant que le passage est ouvert : les deux éclats pris, pas encore tenté, pas pendant le boss.
            var open = ShardCount > 0 && m_ShardsTaken >= ShardCount && !m_Used && !PlayerInDimension && !m_Gate.IsOpen &&
                       !FinalBossDirector.PlayerInArena && !StarPortal.PlayerInDimension;
            if (open != m_Gate.Glitched)
                m_Gate.SetGlitched(open, m_CrackleClip);
        }

        void OnShardPicked(GlitchShard shard)
        {
            m_ShardsTaken++;
            var count = ShardCount;
            if (m_ShardsTaken < count)
            {
                HeadMessages.Show($"Éclat de miroir : {m_ShardsTaken} / {count}", m_Color, 3f, 1f);
                return;
            }

            Sfx.Play(m_GateClip, m_Gate != null ? m_Gate.transform.position : shard.transform.position, 1f, 0.8f, 0.6f);
            HeadMessages.Show($"Éclats de miroir : {count} / {count}\n<size=60%>quelque chose grésille, au bout de la voie du milieu</size>",
                              m_Color, 5f, 1.1f);
        }

        void OnGlitchCrossed()
        {
            if (m_Busy || m_Used || PlayerInDimension || StarPortal.PlayerInDimension)
                return;
            StartCoroutine(Enter());
        }

        // ---------------------------------------------------------------- l'épreuve

        IEnumerator Enter()
        {
            m_Busy = true;
            m_Used = true;
            if (m_Gate != null)
                m_Gate.SetGlitched(false);
            Sfx.Play(m_EnterClip, HeadPosition(), 1f, 1f, 0f);

            var fader = ScreenFader.Instance;
            if (fader != null)
                yield return fader.Fade(1f, m_FadeTime);

            var loading = LoadDimension();
            if (loading == null)
            {
                Debug.LogError($"CyberPassage : impossible de charger la scène « {m_DimensionScene} ». " +
                               "Menu Archery > Toit cyberpunk > Reconstruire le décor, puis File > Build Profiles > Scene List.", this);
                if (fader != null)
                    fader.FadeTo(0f, 0.5f);
                m_Busy = false;
                yield break;
            }

            // La carte se fige ; l'arc et le carquois sont rangés ; on ne se déplace plus (on esquive avec le corps).
            var rig = PlayerRig.Instance;
            MapFreeze.Freeze(m_MainScene, rig != null && rig.Origin != null ? rig.Origin.transform : null);
            AmbienceManager.Silenced = true;
            SuspendMapLighting();
            SuspendPlayer(true);
            PlayerInDimension = true;

            while (!loading.isDone)
                yield return null;

            m_Dimension = SceneManager.GetSceneByName(m_DimensionScene);
            if (m_Dimension.IsValid())
                SceneManager.SetActiveScene(m_Dimension);

            var dimension = CyberDimension.Instance;
            if (dimension == null)
            {
                Debug.LogError("CyberPassage : la scène du toit n'a pas de Cyber Dimension.", this);
                yield return Return(false, false);
                yield break;
            }

            var spawn = dimension.PlayerSpawn;
            var teleport = PlayerTeleport.Instance;
            if (teleport != null && spawn != null)
                yield return teleport.TeleportWhileHidden(spawn.position, spawn.eulerAngles.y);

            dimension.Begin(this);
            if (fader != null)
                fader.FadeTo(0f, m_FadeTime);
            m_Busy = false;
        }

        AsyncOperation LoadDimension()
        {
#if UNITY_EDITOR
            // Dans l'éditeur, le toit se charge même s'il manque dans la liste des scènes du build (par son chemin).
            if (!Application.CanStreamedLevelBeLoaded(m_DimensionScene))
            {
                foreach (var guid in UnityEditor.AssetDatabase.FindAssets($"{m_DimensionScene} t:Scene"))
                {
                    var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                    if (System.IO.Path.GetFileNameWithoutExtension(path) == m_DimensionScene)
                        return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                            path, new LoadSceneParameters(LoadSceneMode.Additive));
                }

                return null;
            }
#endif
            return SceneManager.LoadSceneAsync(m_DimensionScene, LoadSceneMode.Additive);
        }

        /// <summary>L'épreuve est finie (appelé par le toit) : retour devant la porte de l'antre.</summary>
        public void EndTrial(bool success)
        {
            if (!m_Busy && PlayerInDimension)
                StartCoroutine(Return(success, true));
        }

        IEnumerator Return(bool success, bool fade)
        {
            m_Busy = true;
            var fader = ScreenFader.Instance;
            if (fade && fader != null)
                yield return fader.Fade(1f, m_FadeTime);

            TwinCrossbows.Unequip();
            if (m_MainScene.IsValid())
                SceneManager.SetActiveScene(m_MainScene);
            RestoreMapLighting();

            // Devant la porte, côté voie, tourné vers elle, à une place libre.
            var arrival = FreeArrival(out var yaw);
            var teleport = PlayerTeleport.Instance;
            if (teleport != null)
                yield return teleport.TeleportWhileHidden(arrival, yaw);

            if (m_Dimension.IsValid() && m_Dimension.isLoaded)
            {
                EditorSelection.ReleaseScene(m_Dimension);
                var unloading = SceneManager.UnloadSceneAsync(m_Dimension);
                while (unloading != null && !unloading.isDone)
                    yield return null;
            }

            SuspendPlayer(false);
            MapFreeze.Unfreeze();
            AmbienceManager.Silenced = false;
            PlayerInDimension = false;

            var player = PlayerHealth.Instance;
            if (player != null && player.IsAlive)
                player.Health.Heal(player.Health.Max);

            if (success)
                Reward();
            else
                HeadMessages.Show("Supprimé du toit…\n<size=60%>la porte redevient une porte jusqu'à la fin de la partie</size>",
                                  new Color(1f, 0.3f, 0.3f), 4f, 1.1f);

            if (fader != null)
                fader.FadeTo(0f, m_FadeTime);
            m_Busy = false;
        }

        void Reward()
        {
            TrialCompleted = true;
            var score = ScoreManager.Instance;
            var points = 0;
            if (score != null)
                points = score.AddPoints(m_ScoreBonus * score.DifficultyMultiplier);

            // Les Akimbow : débloquées pour toujours (succès « Intrus », dans le menu des armes), et en main tout de suite,
            // pour le reste de la partie.
            var achievements = AchievementManager.Instance;
            if (achievements != null)
                achievements.Report(AchievementType.CyberTrial);
            var akimbow = AkimbowDefinition();
            if (akimbow != null && m_Bow != null)
                m_Bow.SetDefinition(akimbow);

            var weaponText = akimbow != null ? " · les Akimbow sont à toi" : "";
            HeadMessages.Show($"Échec de la suppression.\n<size=60%>+{points} points · un losange au classement{weaponText}</size>",
                              m_Color, 6f, 1.3f);
        }

        // Les Akimbow de la liste des armes, ou null.
        static BowDefinition AkimbowDefinition()
        {
            var loadout = BowLoadout.Instance;
            if (loadout == null)
                return null;
            foreach (var entry in loadout.Entries)
            {
                if (entry != null && entry.bow != null && entry.bow.akimbo)
                    return entry.bow;
            }

            return null;
        }

        // Devant la porte (côté voie), à 2 m, sinon un peu plus loin ou sur le côté : pas sur un ennemi figé.
        Vector3 FreeArrival(out float yaw)
        {
            yaw = 0f;
            if (m_Gate == null)
                return HeadPosition();

            var gate = m_Gate.transform;
            var outward = -gate.forward;
            outward.y = 0f;
            outward = outward.sqrMagnitude > 1e-4f ? outward.normalized : Vector3.back;
            yaw = Quaternion.LookRotation(-outward).eulerAngles.y;
            Vector3? first = null;
            foreach (var distance in new[] { 2.5f, 4f, 5.5f })
            {
                foreach (var angle in new[] { 0f, 30f, -30f, 60f, -60f })
                {
                    var point = gate.position + Quaternion.Euler(0f, angle, 0f) * outward * distance;
                    if (Physics.Raycast(point + Vector3.up * 6f, Vector3.down, out var ground, 12f, Physics.DefaultRaycastLayers,
                                        QueryTriggerInteraction.Ignore))
                        point.y = ground.point.y;
                    first ??= point;
                    if (IsFree(point))
                        return point;
                }
            }

            return first ?? gate.position + outward * 2.5f;
        }

        static bool IsFree(Vector3 feet)
        {
            var count = Physics.OverlapCapsuleNonAlloc(feet + Vector3.up * 0.7f, feet + Vector3.up * 1.7f, 0.45f, s_Overlaps,
                                                       Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var collider = s_Overlaps[i];
                if (collider != null && !(collider is TerrainCollider) && !ArrowIgnore.IsIgnored(collider))
                    return false;
            }

            return true;
        }

        // L'arc et le carquois rangés, les améliorations en pause, les déplacements coupés (sauf la rotation et la gravité).
        void SuspendPlayer(bool suspend)
        {
            var upgrades = PlayerUpgrades.Instance;
            if (upgrades != null)
                upgrades.Suspended = suspend;

            if (m_Bow == null)
                m_Bow = FindAnyObjectByType<Bow>();
            if (m_Bow != null)
                m_Bow.SetStowed(suspend);

            if (m_Quiver == null)
                m_Quiver = FindAnyObjectByType<Quiver>();
            if (m_Quiver != null)
            {
                if (suspend)
                {
                    m_QuiverWasEnabled = m_Quiver.enabled;
                    m_Quiver.enabled = false;
                }
                else
                {
                    m_Quiver.enabled = m_QuiverWasEnabled;
                }
            }

            if (suspend)
            {
                m_PausedLocomotion.Clear();
                var rig = PlayerRig.Instance;
                if (rig == null || rig.Origin == null)
                    return;
                foreach (var provider in rig.Origin.GetComponentsInChildren<LocomotionProvider>(true))
                {
                    if (!provider.enabled || provider is SnapTurnProvider || provider is ContinuousTurnProvider || provider is GravityProvider)
                        continue;
                    provider.enabled = false;
                    m_PausedLocomotion.Add(provider);
                }
            }
            else
            {
                foreach (var provider in m_PausedLocomotion)
                {
                    if (provider != null)
                        provider.enabled = true;
                }

                m_PausedLocomotion.Clear();
            }
        }

        void SuspendMapLighting()
        {
            m_Sky = FindAnyObjectByType<SkyController>();
            if (m_Sky != null)
                m_Sky.enabled = false;
            m_Sun = RenderSettings.sun;
            if (m_Sun != null)
                m_Sun.enabled = false;

            m_HiddenClouds.Clear();
            foreach (var clouds in FindObjectsByType<CloudLayer>())
            {
                if (!clouds.gameObject.activeSelf)
                    continue;
                clouds.gameObject.SetActive(false);
                m_HiddenClouds.Add(clouds.gameObject);
            }
        }

        void RestoreMapLighting()
        {
            if (m_Sun != null)
                m_Sun.enabled = true;
            if (m_Sky != null)
                m_Sky.enabled = true;
            foreach (var clouds in m_HiddenClouds)
            {
                if (clouds != null)
                    clouds.SetActive(true);
            }

            m_HiddenClouds.Clear();
        }

        static Vector3 HeadPosition()
        {
            var rig = PlayerRig.Instance;
            return rig != null && rig.Head != null ? rig.Head.position : Vector3.zero;
        }

        // Test : G donne les deux éclats (la porte glitche) ; un deuxième appui fait entrer.
        void UpdateDebugKeys()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.gKey.wasPressedThisFrame || m_Busy || m_Used || PlayerInDimension)
                return;

            if (m_ShardsTaken < ShardCount)
            {
                m_ShardsTaken = ShardCount;
                foreach (var shard in m_Shards)
                {
                    if (shard != null)
                        shard.gameObject.SetActive(false);
                }

                HeadMessages.Show("Test : les deux éclats de miroir", m_Color, 2f);
            }
            else
            {
                StartCoroutine(Enter());
            }
#endif
        }
    }
}
