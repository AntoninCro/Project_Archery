using System.Collections.Generic;
using Archery.Bows;
using Archery.Building;
using Archery.Chests;
using Archery.Combat;
using Archery.Enemies;
using Archery.Player;
using Archery.Waves;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Archery.Portal
{
    /// <summary>
    /// Fige la carte pendant l'épreuve du portail des étoiles (GDD, section 23) : la vague et son chrono, les ennemis
    /// (déplacements, attaques, animations), leurs projectiles, les bâtiments, les renforts des boss, les apparitions de
    /// coffres et de cristaux. Le joueur, lui, continue de vivre ailleurs : le temps du jeu ne s'arrête pas.
    /// </summary>
    /// <remarks>
    /// Les scripts concernés sont désactivés, les agents de navigation arrêtés, les animations mises en pause et les corps
    /// physiques bloqués, et plus rien ne prend de dégâts (<see cref="Health.Frozen"/> : une flèche encore en vol ne tue pas
    /// un ennemi dont les scripts dorment) ; <see cref="Unfreeze"/> remet tout comme avant. Ce qui appartient au joueur (son XR Origin, ses
    /// flèches) n'est jamais touché.
    /// </remarks>
    public static class MapFreeze
    {
        struct FrozenBody
        {
            public Rigidbody Body;
            public Vector3 Velocity;
            public Vector3 AngularVelocity;
        }

        static readonly List<Behaviour> s_Disabled = new List<Behaviour>();
        static readonly List<NavMeshAgent> s_Agents = new List<NavMeshAgent>();
        static readonly List<Animator> s_Animators = new List<Animator>();
        static readonly List<float> s_AnimatorSpeeds = new List<float>();
        static readonly List<FrozenBody> s_Bodies = new List<FrozenBody>();
        static readonly List<Health> s_Healths = new List<Health>();

        /// <summary>La carte est figée.</summary>
        public static bool IsFrozen { get; private set; }

        /// <summary>Fige ce qui bouge dans la scène <paramref name="map"/>, sauf ce qui est sous <paramref name="player"/>.</summary>
        public static void Freeze(Scene map, Transform player)
        {
            if (IsFrozen || !map.IsValid())
                return;

            IsFrozen = true;
            foreach (var root in map.GetRootGameObjects())
            {
                if (player != null && player.IsChildOf(root.transform) && root.transform != player)
                {
                    // Le joueur est rangé sous cette racine : on en fige le reste, objet par objet.
                    foreach (Transform child in root.transform)
                    {
                        if (!player.IsChildOf(child))
                            FreezeUnder(child, player);
                    }

                    continue;
                }

                if (player == null || !player.IsChildOf(root.transform))
                    FreezeUnder(root.transform, player);
            }
        }

        static void FreezeUnder(Transform root, Transform player)
        {
            // Les scripts qui font avancer la partie.
            Disable<WaveManager>(root);
            Disable<CrystalSpawner>(root);
            Disable<ChestSpawner>(root);
            Disable<Enemy>(root);
            Disable<Boss>(root);
            Disable<EnemyRangedAttack>(root);
            Disable<EnemyProjectile>(root);
            Disable<Burning>(root);
            Disable<Structure>(root);
            Disable<BallistaBolt>(root);

            // Plus rien ne prend de dégâts ni ne se régénère (ennemis, tour, bâtiments), sauf le joueur.
            var playerHealth = PlayerHealth.Instance != null ? PlayerHealth.Instance.Health : null;
            foreach (var health in root.GetComponentsInChildren<Health>())
            {
                if (health.Frozen || health == playerHealth || IsPlayers(health.transform, player))
                    continue;
                health.Frozen = true;
                s_Healths.Add(health);
            }

            foreach (var agent in root.GetComponentsInChildren<NavMeshAgent>())
            {
                if (!agent.isActiveAndEnabled || !agent.isOnNavMesh || agent.isStopped)
                    continue;
                agent.isStopped = true;
                agent.velocity = Vector3.zero;
                s_Agents.Add(agent);
            }

            foreach (var animator in root.GetComponentsInChildren<Animator>())
            {
                if (IsPlayers(animator.transform, player))
                    continue;
                s_Animators.Add(animator);
                s_AnimatorSpeeds.Add(animator.speed);
                animator.speed = 0f;
            }

            foreach (var body in root.GetComponentsInChildren<Rigidbody>())
            {
                if (body.isKinematic || IsPlayers(body.transform, player) || body.GetComponentInParent<Arrow>() != null)
                    continue;
                s_Bodies.Add(new FrozenBody { Body = body, Velocity = body.linearVelocity, AngularVelocity = body.angularVelocity });
                body.isKinematic = true;
            }
        }

        static void Disable<T>(Transform root) where T : Behaviour
        {
            foreach (var behaviour in root.GetComponentsInChildren<T>())
            {
                if (!behaviour.enabled)
                    continue;
                behaviour.enabled = false;
                s_Disabled.Add(behaviour);
            }
        }

        // Ce que le joueur porte ou peut prendre (son arc, ses flèches, ce qu'il attrape) reste libre : XRI gère leur physique.
        static bool IsPlayers(Transform target, Transform player) =>
            (player != null && target.IsChildOf(player)) || target.GetComponentInParent<XRBaseInteractable>() != null;

        /// <summary>Remet la carte en route, comme elle était au moment du gel.</summary>
        public static void Unfreeze()
        {
            if (!IsFrozen)
                return;

            foreach (var frozen in s_Bodies)
            {
                if (frozen.Body == null)
                    continue;
                frozen.Body.isKinematic = false;
                frozen.Body.linearVelocity = frozen.Velocity;
                frozen.Body.angularVelocity = frozen.AngularVelocity;
            }

            for (var i = 0; i < s_Animators.Count; i++)
            {
                if (s_Animators[i] != null)
                    s_Animators[i].speed = s_AnimatorSpeeds[i];
            }

            foreach (var agent in s_Agents)
            {
                if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
                    agent.isStopped = false;
            }

            foreach (var behaviour in s_Disabled)
            {
                if (behaviour != null)
                    behaviour.enabled = true;
            }

            foreach (var health in s_Healths)
            {
                if (health != null)
                    health.Frozen = false;
            }

            Clear();
            IsFrozen = false;
        }

        static void Clear()
        {
            s_Disabled.Clear();
            s_Agents.Clear();
            s_Animators.Clear();
            s_AnimatorSpeeds.Clear();
            s_Bodies.Clear();
            s_Healths.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Clear();
            IsFrozen = false;
        }
    }
}
