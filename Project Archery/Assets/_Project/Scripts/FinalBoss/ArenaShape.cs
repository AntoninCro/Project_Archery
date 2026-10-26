using System.Collections.Generic;
using UnityEngine;

namespace Archery.FinalBoss
{
    /// <summary>
    /// Ce que le combat du boss final doit savoir de son arène, quelle que soit sa forme : son centre et son sol, ses
    /// piliers, où est la lave, où est la voûte, jusqu'où se battre. L'arène en cubes (<see cref="ArenaGridBuilder"/>) en
    /// est une ; l'arène ronde du début (DragonArenaBuilder) a été supprimée le 25 octobre, avec le ménage des fichiers.
    /// </summary>
    /// <remarks>L'objet est le centre du sol de l'arène, axe Z bleu vers le dragon ; le joueur arrive côté -Z.</remarks>
    public abstract class ArenaShape : MonoBehaviour
    {
        /// <summary>Un pilier : centre au sol (monde), rayon à la base et hauteur.</summary>
        public struct Pillar
        {
            public Vector3 Position;
            public float Radius;
            public float Height;
        }

        readonly List<Pillar> m_HandPlacedPillars = new List<Pillar>();
        readonly List<Pose> m_PillarPerches = new List<Pose>();

        /// <summary>Centre du sol de l'arène (monde).</summary>
        public Vector3 Center => transform.position;

        /// <summary>Hauteur (monde) du sol de combat.</summary>
        public float FloorY => transform.position.y;

        /// <summary>
        /// Rayon (m) autour du centre où l'on se bat : le dragon y fait tomber ses rochers, la lave y déborde. Ce qui n'est
        /// pas du sol libre dans ce rayon est écarté par <see cref="IsFreeGround"/>.
        /// </summary>
        public abstract float RoamRadius { get; }

        /// <summary>Rayon (m) de la voûte couverte par les cristaux du plafond.</summary>
        public abstract float CeilingRadius { get; }

        /// <summary>Matériau de la lave (flaques de la phase 3).</summary>
        public abstract Material LavaMaterial { get; }

        /// <summary>Hauteur (monde) du haut des coulées de lave, qui se dévoilent pendant l'intro.</summary>
        public abstract float LavaFallTopY { get; }

        /// <summary>L'arène éclaire elle-même sa lave : l'Arena Director n'y ajoute pas ses lumières.</summary>
        public virtual bool HasOwnLavaLights => false;

        /// <summary>Les piliers : le dragon les contourne, le souffle et les boules de feu s'y arrêtent.</summary>
        public abstract IReadOnlyList<Pillar> Pillars { get; }

        /// <summary>
        /// Les perchoirs du dragon (monde) : le dessus des piliers, et le bord du plateau pour une arène qui en a un. Il
        /// s'y pose pour cracher son feu ou s'en élance pour son salto. Chacun est tourné vers l'arène.
        /// </summary>
        public virtual IReadOnlyList<Pose> Perches
        {
            get
            {
                m_PillarPerches.Clear();
                AddPillarPerches(m_PillarPerches);
                return m_PillarPerches;
            }
        }

        /// <summary>
        /// Le haut de la coulée de lave qui tombe du plateau (monde), tourné vers l'arène : le dragon s'y tient debout
        /// pendant l'intro. Faux si l'arène n'en a pas : il dort alors à sa place dans la scène.
        /// </summary>
        public virtual bool TryGetLavaFallTop(out Pose pose)
        {
            pose = default;
            return false;
        }

        /// <summary>Ce point (monde, les pieds du joueur) est-il dans la lave ?</summary>
        public abstract bool IsLavaAt(Vector3 worldPosition);

        /// <summary>Sol libre autour de ce point (ni lave, ni pilier, ni paroi) : on peut y faire tomber un rocher, déborder la lave.</summary>
        public abstract bool IsFreeGround(Vector3 point, float margin);

        /// <summary>Point de la voûte au-dessus de ce point du sol, et la direction de la voûte vers le sol.</summary>
        public abstract Vector3 CeilingPoint(Vector3 floorPoint, out Vector3 inward);

        /// <summary>Ramène un point (le dragon au sol) dans la zone où l'on se bat, à <paramref name="margin"/> m des bords.</summary>
        public abstract Vector3 Confine(Vector3 point, float margin);

        /// <summary>Hauteur (monde) du sol sous ce point : le dragon y pose les pattes.</summary>
        public virtual float GroundHeightAt(Vector3 point) => FloorY;

        /// <summary>
        /// Allumage de l'arène pendant l'intro (0 : noir, 1 : allumée), donné par l'Arena Director : les lumières propres
        /// à l'arène le suivent.
        /// </summary>
        public virtual void SetLightLevel(float level)
        {
        }

        /// <summary>Les piliers posés à la main (<see cref="ArenaPillar"/>), en plus de ceux de l'arène.</summary>
        protected List<Pillar> HandPlacedPillars()
        {
            m_HandPlacedPillars.Clear();
            foreach (var pillar in ArenaPillar.All)
            {
                if (pillar != null)
                    m_HandPlacedPillars.Add(new Pillar { Position = pillar.transform.position, Radius = pillar.Radius, Height = pillar.Height });
            }

            return m_HandPlacedPillars;
        }

        /// <summary>Le dessus de chaque pilier, tourné vers le centre de l'arène.</summary>
        protected void AddPillarPerches(List<Pose> perches)
        {
            foreach (var pillar in Pillars)
            {
                var top = pillar.Position + Vector3.up * pillar.Height;
                var toCenter = Center - top;
                toCenter.y = 0f;
                perches.Add(new Pose(top, toCenter.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toCenter) : Quaternion.identity));
            }
        }
    }
}
