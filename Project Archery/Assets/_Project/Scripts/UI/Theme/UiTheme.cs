using TMPro;
using UnityEngine;

namespace Archery.UI
{
    /// <summary>
    /// Le thème des menus (« Resources/UiTheme ») : panneaux sombres aux bordures dorées ornées (Kenney Fantasy UI
    /// Borders), boutons de couleur selon leur rôle, titres en Cinzel dorés, sons Kenney. Appliqué aux menus par
    /// <see cref="UiThemer"/> (dans l'éditeur, menu Archery > Interface).
    /// </summary>
    /// <remarks>
    /// Les sprites, les polices et les sons sont remplis par l'éditeur à la création de l'asset. Après un changement ici,
    /// menu Archery > Interface > Réappliquer le thème aux menus pour mettre les menus à jour.
    /// </remarks>
    [CreateAssetMenu(fileName = "UiTheme", menuName = "Archery/UI Theme")]
    public class UiTheme : ScriptableObject
    {
        public const string ResourceName = "UiTheme";

        public static UiTheme Load() => Resources.Load<UiTheme>(ResourceName);

        [Header("Polices")]
        [Tooltip("Titres des panneaux (Cinzel Decorative).")]
        public TMP_FontAsset titleFont;

        [Tooltip("Boutons, étiquettes, prix (Cinzel).")]
        public TMP_FontAsset labelFont;

        [Tooltip("Textes longs : résumés, descriptions, classement (Inter).")]
        public TMP_FontAsset bodyFont;

        [Header("Cadres (sprites blancs, teintés)")]
        public Sprite panelFill;
        public Sprite panelBorder;

        [Tooltip("Taille des ornements des grands panneaux : plus petit = plus gros (0,5 : deux fois plus gros).")]
        public float panelScale = 0.5f;

        public Sprite frameFill;
        public Sprite frameBorder;

        [Tooltip("Taille des ornements des cartes (boutique, constructions).")]
        public float frameScale = 0.7f;

        public Sprite buttonFill;
        public Sprite buttonBorder;

        [Tooltip("Taille des ornements des boutons.")]
        public float buttonScale = 0.8f;

        [Tooltip("Cadre tout simple (curseurs, poignées).")]
        public Sprite plainFill;
        public Sprite plainBorder;

        [Tooltip("Trait orné de part et d'autre sous les titres.")]
        public Sprite divider;

        [Header("Couleurs des panneaux")]
        public Color panelColor = new Color(0.09f, 0.06f, 0.19f, 0.94f);
        public Color panelBorderColor = new Color(1f, 0.8f, 0.38f);
        public Color cardColor = new Color(0.12f, 0.09f, 0.25f, 0.95f);
        public Color titleTop = new Color(1f, 0.96f, 0.72f);
        public Color titleBottom = new Color(1f, 0.64f, 0.2f);
        public Color textColor = new Color(0.96f, 0.93f, 0.86f);
        public Color labelColor = new Color(0.82f, 0.86f, 1f);

        [Header("Couleurs des boutons")]
        [Tooltip("Jouer, Rejouer, Enregistrer, Terminer.")]
        public Color primaryButton = new Color(0.1f, 0.6f, 0.46f);

        [Tooltip("Paramètres, Classement… (les autres boutons).")]
        public Color secondaryButton = new Color(0.2f, 0.36f, 0.86f);

        [Tooltip("Flèches < et >.")]
        public Color arrowButton = new Color(0.52f, 0.3f, 0.88f);

        [Tooltip("Retour, Fermer.")]
        public Color backButton = new Color(0.38f, 0.27f, 0.55f);

        [Tooltip("Quitter.")]
        public Color dangerButton = new Color(0.78f, 0.16f, 0.28f);

        public Color buttonBorderColor = new Color(1f, 0.88f, 0.58f);

        [Tooltip("Bordure d'un bouton visé par le rayon de la main.")]
        public Color hoverBorderColor = new Color(1f, 1f, 0.92f);

        public Color buttonTextColor = Color.white;

        [Header("Curseurs")]
        public Color sliderTrack = new Color(0.04f, 0.03f, 0.1f, 0.95f);
        public Color sliderHandle = new Color(1f, 0.82f, 0.4f);

        [Tooltip("Remplissage des curseurs : Général, Musique, Effets, Confort (dans cet ordre), puis les autres.")]
        public Color[] sliderFills =
        {
            new Color(1f, 0.74f, 0.25f), new Color(0.95f, 0.35f, 0.85f), new Color(0.3f, 0.85f, 1f), new Color(0.4f, 0.95f, 0.55f),
        };

        [Header("Sons")]
        public AudioClip hoverClip;
        public AudioClip clickClip;
        public AudioClip confirmClip;
        public AudioClip backClip;

        [Range(0f, 1f)]
        public float hoverVolume = 0.35f;

        [Range(0f, 1f)]
        public float clickVolume = 0.7f;
    }
}
