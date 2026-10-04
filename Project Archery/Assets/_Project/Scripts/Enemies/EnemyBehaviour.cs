using UnityEngine;

namespace Archery.Enemies
{
    /// <summary>
    /// Comportement particulier d'un ennemi (par exemple le vol, <see cref="FlyingEnemy"/>). Il remplace le déplacement
    /// au sol (NavMesh) et l'attaque au corps à corps d'<see cref="Enemy"/>, qui garde le reste : PV, difficulté,
    /// ralentissements, score, mort.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public abstract class EnemyBehaviour : MonoBehaviour
    {
        protected Enemy Enemy { get; private set; }

        /// <summary>Vitesse actuelle, pour le paramètre Speed de l'Animator.</summary>
        public abstract Vector3 Velocity { get; }

        /// <summary>Faux pour un ennemi qui n'a pas besoin du NavMesh pour apparaître (volant).</summary>
        public virtual bool NeedsNavMesh => false;

        /// <summary>Décalage par rapport au point d'apparition (un volant apparaît en l'air).</summary>
        public virtual Vector3 SpawnOffset => Vector3.zero;

        protected virtual void Awake() => Enemy = GetComponent<Enemy>();

        /// <summary>Une image de jeu, tant que l'ennemi est en vie et ne s'enfuit pas : déplacement et attaque.</summary>
        public abstract void Tick(float deltaTime);

        /// <summary>Fin de vague : l'ennemi s'en va vers ce point. <see cref="TickFlee"/> est appelé ensuite à chaque image.</summary>
        public abstract void BeginFlee(Vector3 exitPoint);

        /// <summary>Pendant la fuite. Appeler <see cref="Enemy.Despawn"/> une fois l'ennemi parti.</summary>
        public abstract void TickFlee(float deltaTime);

        /// <summary>L'ennemi vient de mourir.</summary>
        public virtual void OnDied()
        {
        }

        /// <summary>
        /// Une image du corps après la mort. Renvoie vrai si le comportement anime lui-même le corps (chute d'un volant) :
        /// <see cref="Enemy"/> ne le fait alors pas basculer. Le corps s'enfonce puis disparaît dans tous les cas.
        /// </summary>
        public virtual bool TickCorpse(float deltaTime) => false;
    }
}
