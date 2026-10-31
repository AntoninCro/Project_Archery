using Archery.Core;
using UnityEngine;

namespace Archery.Building
{
    /// <summary>
    /// Geyser (refonte du tower defense, étape 5, lot 3 ; 28 octobre ; <c>Docs/Refonte_TowerDefense.md</c>, parties 6.1 et
    /// 6.2) : sur la voie. Chargé, il jaillit dès qu'un ennemi au sol passe dessus et projette à la verticale tous ceux qui sont
    /// dessus (<c>Enemy.Launch</c>) : ils retombent sur place, blessés par leur chute, et sont des cibles de choix en l'air.
    /// Puis il se recharge. Plus haut, plus large et plus souvent à chaque niveau. Commune (proposée).
    /// </summary>
    /// <remarks>
    /// La différence avec le tremplin : tout le groupe, et à la verticale (le tremplin en lance un seul, vers la forêt). La
    /// colonne d'eau est un cylindre au matériau des rayons de lumière (<c>Light_Beam</c>), teinté en bleu.
    /// </remarks>
    public class StructureGeyser : Structure
    {
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("Vitesse (m/s) vers le haut des ennemis projetés, par niveau.")]
        [SerializeField]
        float[] m_UpSpeedPerLevel = { 9f, 10.5f, 12f };

        [Tooltip("Rayon (m) du geyser, par niveau.")]
        [SerializeField]
        float[] m_RadiusPerLevel = { 1.5f, 1.75f, 2f };

        [Tooltip("Temps (s) de recharge après chaque jet, par niveau.")]
        [SerializeField]
        float[] m_CooldownPerLevel = { 4f, 3.4f, 2.8f };

        [Tooltip("Colonne d'eau montrée pendant le jet : un objet posé au sol, dont l'échelle Y donne la hauteur (m) de son cylindre.")]
        [SerializeField]
        Transform m_WaterColumn;

        [Tooltip("Hauteur (m) de la colonne au plus fort du jet.")]
        [SerializeField]
        float m_ColumnHeight = 5f;

        [SerializeField]
        Color m_WaterColor = new Color(0.55f, 0.85f, 1f, 0.75f);

        [SerializeField]
        AudioClip m_JetClip;

        MaterialPropertyBlock m_Block;
        Vector3 m_ColumnScale = Vector3.one;
        float m_Cooldown;
        float m_Jet;

        protected override void Awake()
        {
            base.Awake();
            if (m_WaterColumn == null)
                return;

            m_ColumnScale = m_WaterColumn.localScale;
            var renderer = m_WaterColumn.GetComponentInChildren<Renderer>(true);
            if (renderer != null)
            {
                m_Block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(m_Block);
                m_Block.SetColor(k_BaseColorId, m_WaterColor);
                renderer.SetPropertyBlock(m_Block);
            }

            m_WaterColumn.gameObject.SetActive(false);
        }

        // La colonne d'eau n'est pas une partie du bâtiment : pas de plan bleu, pas de volume à frapper.
        protected override bool IsBlueprintPart(Renderer renderer) => m_WaterColumn == null || !renderer.transform.IsChildOf(m_WaterColumn);

        protected override void Update()
        {
            base.Update();
            if (!IsBuilt)
                return;

            UpdateColumn();
            if (m_Cooldown > 0f)
            {
                m_Cooldown -= Time.deltaTime * OverchargeRate;
                return;
            }

            var level = Mathf.Max(1, Level);
            var radius = ValueFor(m_RadiusPerLevel, level, 1.5f);
            var speed = ValueFor(m_UpSpeedPerLevel, level, 9f);
            var launched = false;
            foreach (var enemy in AliveEnemies())
            {
                if (enemy == null || !enemy.CanBeLaunched || enemy.IsAirborne || !IsOnTop(enemy.transform.position, radius))
                    continue;

                // Un peu de hasard sur le côté : le groupe s'éparpille un peu en retombant.
                var side = Random.insideUnitCircle * 0.6f;
                launched |= enemy.Launch(new Vector3(side.x, speed, side.y), this);
            }

            if (!launched)
                return;

            m_Cooldown = ValueFor(m_CooldownPerLevel, level, 4f);
            m_Jet = 1f;
            Sfx.Play(m_JetClip, transform.position + Vector3.up * 0.5f, 0.9f, Random.Range(0.9f, 1.1f));
        }

        // Le jet monte d'un coup, puis retombe en une seconde.
        void UpdateColumn()
        {
            if (m_WaterColumn == null)
                return;

            m_Jet = Mathf.MoveTowards(m_Jet, 0f, Time.deltaTime);
            var visible = m_Jet > 0f;
            if (m_WaterColumn.gameObject.activeSelf != visible)
                m_WaterColumn.gameObject.SetActive(visible);
            if (!visible)
                return;

            var height = m_ColumnHeight * Mathf.Sin(Mathf.Sqrt(m_Jet) * Mathf.PI * 0.5f);
            var width = 1f + 0.15f * (Mathf.Max(1, Level) - 1);
            m_WaterColumn.localScale = new Vector3(m_ColumnScale.x * width, m_ColumnScale.y * height, m_ColumnScale.z * width);
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
            Gizmos.color = new Color(0.55f, 0.85f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, ValueFor(m_RadiusPerLevel, Mathf.Max(1, Level), 1.5f));
        }
    }
}
