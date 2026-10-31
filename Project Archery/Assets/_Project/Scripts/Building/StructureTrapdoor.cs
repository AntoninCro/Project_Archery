using Archery.Core;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Trappe (refonte du tower defense, étape 5, lot 4 ; 28 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, parties 6.1 et 6.2) :
    /// sur la voie. Chargée, elle s'ouvre dès qu'un ennemi au sol passe dessus : tous ceux qui sont dessus tombent et ressortent
    /// plus loin en arrière sur la voie (8 / 10 / 12 m), étourdis 1 s. Puis elle se referme et se recharge. Rare (proposée).
    /// </summary>
    /// <remarks>
    /// Le point d'arrivée suit la voie la plus proche (<see cref="BuildGrid.TryPointBack"/>), vers l'apparition des ennemis ;
    /// sans voie connue, droit vers la forêt. Les deux battants (<c>Left Door</c>, <c>Right Door</c>) tournent autour de leur axe Z.
    /// </remarks>
    public class StructureTrapdoor : Structure
    {
        [Tooltip("Rayon (m) de la trappe, par niveau.")]
        [SerializeField]
        float[] m_RadiusPerLevel = { 1.5f, 1.75f, 2f };

        [Tooltip("Distance (m) dont les ennemis reculent sur la voie, par niveau.")]
        [SerializeField]
        float[] m_BackPerLevel = { 8f, 10f, 12f };

        [Tooltip("Temps (s) de recharge après chaque ouverture, par niveau.")]
        [SerializeField]
        float[] m_CooldownPerLevel = { 6f, 5f, 4f };

        [Tooltip("Étourdissement (s) des ennemis à leur arrivée.")]
        [SerializeField]
        float m_ArrivalStun = 1f;

        [Tooltip("Battants : ils s'ouvrent vers le bas en tournant autour de leur axe Z (gauche : +, droite : -).")]
        [SerializeField]
        Transform m_LeftDoor;

        [SerializeField]
        Transform m_RightDoor;

        [SerializeField]
        AudioClip m_OpenClip;

        Quaternion m_LeftRest = Quaternion.identity;
        Quaternion m_RightRest = Quaternion.identity;
        float m_Cooldown;
        float m_Open;

        protected override void Awake()
        {
            if (m_LeftDoor != null)
                m_LeftRest = m_LeftDoor.localRotation;
            if (m_RightDoor != null)
                m_RightRest = m_RightDoor.localRotation;
            base.Awake();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsBuilt)
                return;

            // Les battants : ouverts d'un coup, refermés en une seconde.
            m_Open = Mathf.MoveTowards(m_Open, 0f, Time.deltaTime);
            var angle = 85f * Mathf.Clamp01(m_Open * 1.6f);
            if (m_LeftDoor != null)
                m_LeftDoor.localRotation = m_LeftRest * Quaternion.Euler(0f, 0f, angle);
            if (m_RightDoor != null)
                m_RightDoor.localRotation = m_RightRest * Quaternion.Euler(0f, 0f, -angle);

            if (m_Cooldown > 0f)
            {
                m_Cooldown -= Time.deltaTime * OverchargeRate;
                return;
            }

            var level = Mathf.Max(1, Level);
            var radius = ValueFor(m_RadiusPerLevel, level, 1.5f);
            var back = ValueFor(m_BackPerLevel, level, 8f);
            var grid = BuildGrid.Instance;
            var dropped = false;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.CanBeLaunched || enemy.IsAirborne || !IsOnTop(enemy.transform.position, radius))
                    continue;

                var position = enemy.transform.position;
                if (grid == null || !grid.TryPointBack(position, back, out var arrival))
                    arrival = position + TowardForest(enemy) * back;
                dropped |= enemy.Teleport(arrival, m_ArrivalStun);
            }

            if (!dropped)
                return;

            m_Cooldown = ValueFor(m_CooldownPerLevel, level, 6f);
            m_Open = 1f;
            Sfx.Play(m_OpenClip, transform.position + Vector3.up * 0.3f, 0.9f, Random.Range(0.9f, 1.1f));
        }

        bool IsOnTop(Vector3 position, float radius)
        {
            var offset = position - transform.position;
            if (offset.y < -0.8f || offset.y > 1.2f)
                return false;
            offset.y = 0f;
            return offset.sqrMagnitude <= radius * radius;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.6f, 0.45f, 0.3f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, ValueFor(m_RadiusPerLevel, Mathf.Max(1, Level), 1.5f));
        }
    }
}
