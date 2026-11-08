using Archery.Menus;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Le chantier du début devient un trio (3 novembre, le joueur ; <c>Docs/Refonte_TowerDefense.md</c>, 12.53) : 4 bâtiments au lieu
    /// de 3. Le tirage est dans le code (<c>Building/BuildManager</c>) ; ici, la fenêtre « Ouvrir les coffres » du tutoriel
    /// (<c>Resources/TutorialContent</c>) prend le nouveau texte, une seule fois, et seulement si elle a encore l'ancien mot pour mot
    /// (jamais un texte retouché à la main).
    /// </summary>
    [InitializeOnLoad]
    static class StartingTrioBaker
    {
        const string k_Tutorial = "Assets/_Project/Resources/TutorialContent.asset";

        const string k_OldChestText =
            "Au début de chaque vague, des coffres apparaissent d'un côté de la jungle, sous un rayon doré (l'autre côté à la vague " +
            "suivante). Vise-en un et appuie sur la gâchette (ou la gâchette de prise) : choisis un parchemin, deux plans d'un bâtiment " +
            "de ton chantier, ou un objet. Ton chantier : 3 bâtiments au début, un de plus après chaque boss.";

        static StartingTrioBaker()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            var content = AssetDatabase.LoadAssetAtPath<TutorialContent>(k_Tutorial);
            if (content == null)
                return;

            var changed = false;
            foreach (var section in content.sections)
            {
                if (section == null)
                    continue;

                foreach (var window in section.windows)
                {
                    if (window == null || window.text != k_OldChestText)
                        continue;

                    window.text = TutorialContent.ChestText;
                    changed = true;
                }
            }

            if (!changed)
                return;

            EditorUtility.SetDirty(content);
            AssetDatabase.SaveAssetIfDirty(content);
            Debug.Log($"Le chantier du début (3 novembre) : tutoriel, « {TutorialContent.ChestTitle} » : 4 bâtiments au début (de quoi " +
                      "bâtir un trio).", content);
        }
    }
}
