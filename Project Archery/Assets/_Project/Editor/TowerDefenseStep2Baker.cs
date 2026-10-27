using Archery.Menus;
using UnityEditor;
using UnityEngine;

namespace Archery.EditorTools
{
    /// <summary>
    /// Refonte du tower defense, étape 2 (27 octobre, <c>Docs/Refonte_TowerDefense.md</c>) : plus de matériaux, des plans
    /// trouvés dans les caisses de l'ingénieur. Les textes du tutoriel sont dans <c>Resources/TutorialContent</c> : à
    /// l'ouverture de Unity, la fenêtre « Récolter les cristaux », le texte qui fait payer en matériaux et les matériaux de la
    /// montre sont remplacés, une seule fois (seulement s'ils ont encore leur texte d'avant : un texte retouché à la main
    /// n'est pas touché). Étape 3, le même jour : « Choisir un bâtiment » parle de la pose à la montre (bouton « Plans »,
    /// hologramme, joystick, runes, déplacer), y compris si un texte précédent de l'étape 2 ou 3 avait déjà été posé.
    /// </summary>
    [InitializeOnLoad]
    static class TowerDefenseStep2Baker
    {
        const string k_ContentPath = "Assets/_Project/Resources/TutorialContent.asset";
        const string k_OldHarvestTitle = "Récolter les cristaux";
        const string k_OldPayment = "paie-le en matériaux";
        const string k_OldWatch = "tes matériaux";
        const string k_NewWatch = "tes plans de construction";

        const string k_CrateTitle = "Ouvrir les caisses de l'ingénieur";
        const string k_CrateText =
            "Pendant les vagues, des caisses de l'ingénieur tombent dans la jungle, sous un rayon violet. Vise-en une et appuie sur la gâchette " +
            "(ou la gâchette de prise) : choisis un lot de plans de construction.";
        // Le texte de l'étape 2 (socles), remplacé par celui de l'étape 3 (la grille) s'il a déjà été posé.
        const string k_Step2ChooseText =
            "Vise un cercle bleu au sol et appuie sur la gâchette de prise : choisis un bâtiment dont tu as le plan. Un autre plan du même " +
            "bâtiment le fait monter de niveau. Barricade et pièges sur la voie, baliste et tour de givre au bord.";
        // Le premier texte de l'étape 3 (viser un emplacement, puis le panneau), remplacé par celui de la pose à la montre.
        const string k_GridChooseText =
            "Avec un plan, des cercles de runes s'allument au sol autour de toi : orange sur la voie, bleus au bord, verts en hauteur. Vise-en " +
            "un de près et appuie sur la gâchette de prise : choisis le bâtiment. Barricade et pièges sur la voie, baliste et tour de givre " +
            "au bord ou en hauteur. Un autre plan du même bâtiment le fait monter de niveau ; entre les vagues, tu peux le déplacer.";
        const string k_ChooseText =
            "Touche le bouton « Plans » de ta montre avec l'autre main : l'hologramme d'un bâtiment suit le rayon de ta main. Joystick : gauche " +
            "et droite le tournent, haut et bas changent de plan ; gâchette : le poser là où les runes s'allument (orange sur la voie, bleues " +
            "au bord, vertes en hauteur) ; poignée : annuler. Pose-le sur un bâtiment pareil pour le faire monter de niveau ; entre les " +
            "vagues, le panneau d'un bâtiment propose de le déplacer.";

        static TowerDefenseStep2Baker()
        {
            EditorApplication.delayCall -= Run;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;

            var content = AssetDatabase.LoadAssetAtPath<TutorialContent>(k_ContentPath);
            if (content == null)
                return;

            var changed = 0;
            foreach (var section in content.sections)
            {
                if (section == null)
                    continue;

                foreach (var window in section.windows)
                {
                    if (window == null)
                        continue;

                    if (window.title == k_OldHarvestTitle)
                    {
                        window.title = k_CrateTitle;
                        window.text = k_CrateText;
                        changed++;
                    }
                    else if (window.text != null &&
                             (window.text.Contains(k_OldPayment) || window.text == k_Step2ChooseText || window.text == k_GridChooseText))
                    {
                        window.text = k_ChooseText;
                        changed++;
                    }
                    else if (window.text != null && window.text.Contains(k_OldWatch))
                    {
                        // La montre (catégorie « Vagues ») affichait les matériaux ; elle affiche les plans.
                        window.text = window.text.Replace(k_OldWatch, k_NewWatch);
                        changed++;
                    }
                }
            }

            if (changed == 0)
                return;

            EditorUtility.SetDirty(content);
            AssetDatabase.SaveAssetIfDirty(content);
            Debug.Log($"Refonte du tower defense, étape 2 : {changed} fenêtre(s) du tutoriel « Construction » parlent maintenant des caisses " +
                      "de l'ingénieur et des plans, plus des matériaux.", content);
        }
    }
}
