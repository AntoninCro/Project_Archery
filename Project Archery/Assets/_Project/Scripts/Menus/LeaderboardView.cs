using System.Globalization;
using System.Text;
using Archery.Save;
using TMPro;
using UnityEngine;

namespace Archery.Menus
{
    /// <summary>
    /// Affiche le classement (GDD, section 14) dans un texte TextMeshPro : les 10 meilleures parties,
    /// avec le nom, le score, la difficulté et la vague atteinte. Une ligne peut être mise en valeur
    /// (la partie qu'on vient d'enregistrer).
    /// </summary>
    [DisallowMultipleComponent]
    public class LeaderboardView : MonoBehaviour
    {
        [Tooltip("Texte du tableau. Vide : celui de cet objet.")]
        [SerializeField]
        TMP_Text m_Table;

        [SerializeField]
        int m_Rows = 10;

        [SerializeField]
        Color m_HighlightColor = new Color(1f, 0.85f, 0.3f);

        readonly StringBuilder m_Builder = new StringBuilder();
        int m_Highlight = -1;

        void Awake()
        {
            if (m_Table == null)
                m_Table = GetComponent<TMP_Text>();
        }

        void OnEnable() => Refresh();

        /// <summary>Met en valeur la ligne de ce rang (0 = première place ; -1 = aucune).</summary>
        public void Highlight(int rank)
        {
            m_Highlight = rank;
            Refresh();
        }

        public void Refresh()
        {
            if (m_Table == null)
                return;

            var entries = Leaderboard.Entries;
            if (entries.Count == 0)
            {
                m_Table.text = "Aucune partie enregistrée pour l'instant.";
                return;
            }

            // Colonnes alignées avec la balise <pos> de TextMeshPro.
            m_Builder.Clear();
            m_Builder.Append("<b>#<pos=8%>Nom<pos=46%>Score<pos=64%>Difficulté<pos=88%>Vague</b>\n");
            var rows = Mathf.Min(m_Rows, entries.Count);
            for (var i = 0; i < rows; i++)
                AppendRow(i, entries[i]);

            // La partie mise en valeur est hors du top : on l'ajoute en dessous.
            if (m_Highlight >= rows && m_Highlight < entries.Count)
            {
                m_Builder.Append("...\n");
                AppendRow(m_Highlight, entries[m_Highlight]);
            }

            m_Table.text = m_Builder.ToString();
        }

        void AppendRow(int rank, LeaderboardEntry entry)
        {
            var highlighted = rank == m_Highlight;
            if (highlighted)
                m_Builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(m_HighlightColor)).Append('>');

            m_Builder.Append(rank + 1)
                .Append("<pos=8%>").Append(entry.name)
                .Append("<pos=46%>").Append(entry.score.ToString("#,0", CultureInfo.InvariantCulture).Replace(',', ' '))
                .Append("<pos=64%>").Append(entry.difficulty)
                .Append("<pos=88%>").Append(entry.wave);

            if (highlighted)
                m_Builder.Append("</color>");
            m_Builder.Append('\n');
        }
    }
}
