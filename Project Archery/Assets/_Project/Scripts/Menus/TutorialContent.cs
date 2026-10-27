using System;
using System.Collections.Generic;
using UnityEngine;

namespace Archery.Menus
{
    /// <summary>
    /// Le contenu du tutoriel du menu principal : ses catégories, et pour chacune ses fenêtres (un titre et quelques
    /// lignes). La vidéo d'une fenêtre se trouve toute seule : <c>Resources/Tutoriel/&lt;Video Prefix&gt;_&lt;numéro&gt;</c>.
    /// </summary>
    /// <remarks>
    /// L'asset est <c>Resources/TutorialContent</c> (créé par l'éditeur, avec les textes ci-dessous) : on y change les
    /// textes, l'ordre, les icônes. S'il manque, le jeu prend ces textes par défaut.
    /// </remarks>
    [CreateAssetMenu(fileName = "TutorialContent", menuName = "Archery/Tutorial Content")]
    public class TutorialContent : ScriptableObject
    {
        public const string ResourceName = "TutorialContent";

        [Serializable]
        public class Window
        {
            public string title;

            [TextArea(2, 5)]
            public string text;
        }

        [Serializable]
        public class Section
        {
            public string title;

            [Tooltip("Début du nom des vidéos de cette catégorie : « Tir » → Resources/Tutoriel/Tir_1, Tir_2…")]
            public string videoPrefix;

            [Tooltip("Optionnel : icône du bouton de la catégorie.")]
            public Sprite icon;

            public List<Window> windows = new List<Window>();
        }

        /// <summary>La fenêtre de la grenade à flèches, ajoutée à la catégorie Tir le 22 octobre (vidéo <c>Tir_3</c>).</summary>
        public const string GrenadeTitle = "La grenade à flèches";

        public const string GrenadeText =
            "Porte une main vide à ta hanche, comme pour fouiller ta poche, et serre la poignée : une grenade sort. Lance-la : " +
            "elle éclate au premier contact en une gerbe de flèches. Ta montre affiche sa recharge.";

        public List<Section> sections = new List<Section>();

        /// <summary>L'asset de Resources, ou le contenu par défaut s'il manque.</summary>
        public static TutorialContent Load()
        {
            var content = Resources.Load<TutorialContent>(ResourceName);
            return content != null && content.sections.Count > 0 ? content : CreateDefault();
        }

        /// <summary>Le contenu validé le 16 octobre : 4 catégories, 13 fenêtres ; plus la grenade le 22 octobre.</summary>
        public static TutorialContent CreateDefault()
        {
            var content = CreateInstance<TutorialContent>();
            content.name = "Tutoriel (par défaut)";
            content.sections = new List<Section>
            {
                NewSection("Tir", "Tir",
                    ("Tirer à l'arc",
                     "Prends l'arc avec la poignée. Attrape une flèche au-dessus de ton épaule et pose-la sur la corde : tends-la, puis lâche."),
                    ("L'anneau et les points faibles",
                     "À pleine tension, un anneau apparaît : lâche quand le cercle passe dans le vert (un « ding » te prévient). " +
                     "La tête compte double, la gemme des boss triple."),
                    (GrenadeTitle, GrenadeText)),
                NewSection("Déplacement", "Deplacement",
                    ("Marcher et tourner",
                     "Le joystick gauche te fait marcher là où tu regardes. Le joystick droit te tourne par crans (désactivable dans les paramètres)."),
                    ("Courir et sauter",
                     "En marchant, balance les bras pour courir jusqu'à trois fois plus vite. Lève les deux bras d'un coup pour sauter."),
                    ("Glisser",
                     "En pleine course, appuie sur A ou X : tu glisses loin, les mains libres pour tirer."),
                    ("Monter sur la tour",
                     "Reste une seconde sur la bande lumineuse au pied de la tour pour monter. Pour redescendre, saute. " +
                     "Le bouton « Tour » de ta montre te ramène aussi en haut.")),
                NewSection("Vagues", "Vagues",
                    ("Lancer une vague",
                     "Tire dans le gong pour lancer la vague : d'abord une accalmie, puis les ennemis arrivent de plus en plus vite. " +
                     "À la fin du chrono, le boss arrive."),
                    ("Ta vie et la tour",
                     "À zéro PV, la partie est perdue. Si la tour tombe, tu gagnes deux fois moins d'or : répare-la à la boutique. " +
                     "Les barricades retiennent les ennemis au sol."),
                    ("Les boss",
                     "Chaque vague se termine par un boss : vise sa gemme, son point faible. " +
                     "Deux coups de gong pendant le chrono le font venir plus tôt, pour plus d'or."),
                    ("La montre",
                     "Regarde ton poignet : tes PV, la vague, le chrono, tes plans de construction et tes bonus en cours.")),
                NewSection("Construction", "Construction",
                    ("Ouvrir les caisses de l'ingénieur",
                     "Pendant les vagues, des caisses de l'ingénieur tombent dans la jungle, sous un rayon violet. Vise-en une et appuie sur la gâchette " +
                     "(ou la gâchette de prise) : choisis un lot de plans de construction."),
                    ("Choisir un bâtiment",
                     "Touche le bouton « Plans » de ta montre avec l'autre main : l'hologramme d'un bâtiment suit le rayon de ta main. Joystick : gauche " +
                     "et droite le tournent, haut et bas changent de plan ; gâchette : le poser là où les runes s'allument (orange sur la voie, bleues " +
                     "au bord, vertes en hauteur) ; poignée : annuler. Pose-le sur un bâtiment pareil pour le faire monter de niveau ; entre les " +
                     "vagues, le panneau d'un bâtiment propose de le déplacer."),
                    ("Construire",
                     "Un marteau apparaît dans chaque main. Frappe trois fois le plan bleu pour construire le bâtiment.")),
            };
            return content;
        }

        static Section NewSection(string title, string videoPrefix, params (string Title, string Text)[] windows)
        {
            var section = new Section { title = title, videoPrefix = videoPrefix };
            foreach (var window in windows)
                section.windows.Add(new Window { title = window.Title, text = window.Text });
            return section;
        }
    }
}
