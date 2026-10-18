using Archery.Save;
using Archery.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Archery.Menus
{
    /// <summary>
    /// Page des paramètres (GDD, section 14) : volumes général, musique et effets, rotation au joystick (par crans,
    /// continue à la vitesse choisie, ou coupée), vignette de confort.
    /// Les changements s'appliquent tout de suite et sont enregistrés dans settings.json.
    /// </summary>
    /// <remarks>Les curseurs vont de 0 à 1 (valeurs par défaut d'un Slider). Tous les champs sont optionnels.</remarks>
    [DisallowMultipleComponent]
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField]
        Slider m_MasterSlider;

        [SerializeField]
        Slider m_MusicSlider;

        [SerializeField]
        Slider m_EffectsSlider;

        [Tooltip("Optionnel : textes « 80 % » à côté des curseurs.")]
        [SerializeField]
        TMP_Text m_MasterValue;

        [SerializeField]
        TMP_Text m_MusicValue;

        [SerializeField]
        TMP_Text m_EffectsValue;

        [Tooltip("Force de la vignette de confort pendant le slide (0 = désactivée).")]
        [SerializeField]
        Slider m_VignetteSlider;

        [SerializeField]
        TMP_Text m_VignetteValue;

        [Tooltip("Rotation au joystick avec un Toggle…")]
        [SerializeField]
        Toggle m_SnapTurnToggle;

        [Tooltip("… ou avec un bouton dont le texte affiche l'état (plus facile à viser en VR).")]
        [SerializeField]
        Button m_SnapTurnButton;

        [Tooltip("Texte du bouton de rotation.")]
        [SerializeField]
        TMP_Text m_SnapTurnText;

        [Tooltip("Optionnel : vitesse de la rotation continue. Vide : une ligne est ajoutée sous le bouton de rotation, " +
                 "sur le modèle de celle de la vignette.")]
        [SerializeField]
        Slider m_TurnSpeedSlider;

        [SerializeField]
        TMP_Text m_TurnSpeedValue;

        GameObject m_TurnSpeedRow;

        void Awake()
        {
            BuildTurnSpeedRow();

            // Le bouton passe d'un mode à l'autre : par crans, continue, coupée.
            if (m_SnapTurnButton != null)
                m_SnapTurnButton.onClick.AddListener(NextTurnMode);
            if (m_TurnSpeedSlider != null)
                m_TurnSpeedSlider.onValueChanged.AddListener(OnTurnSpeedChanged);

            if (m_MasterSlider != null)
                m_MasterSlider.onValueChanged.AddListener(value => Apply(settings => settings.MasterVolume = value, m_MasterValue, value));
            if (m_MusicSlider != null)
                m_MusicSlider.onValueChanged.AddListener(value => Apply(settings => settings.MusicVolume = value, m_MusicValue, value));
            if (m_EffectsSlider != null)
                m_EffectsSlider.onValueChanged.AddListener(value => Apply(settings => settings.EffectsVolume = value, m_EffectsValue, value));
            if (m_VignetteSlider != null)
                m_VignetteSlider.onValueChanged.AddListener(OnVignetteChanged);
            if (m_SnapTurnToggle != null)
                m_SnapTurnToggle.onValueChanged.AddListener(OnSnapTurnChanged);
        }

        // À chaque ouverture, la page montre les valeurs enregistrées.
        void OnEnable()
        {
            var data = GameSettings.Data;
            Show(m_MasterSlider, m_MasterValue, data.masterVolume);
            Show(m_MusicSlider, m_MusicValue, data.musicVolume);
            Show(m_EffectsSlider, m_EffectsValue, data.effectsVolume);
            if (m_VignetteSlider != null)
                m_VignetteSlider.SetValueWithoutNotify(data.comfortVignette);
            ShowVignette(data.comfortVignette);
            ShowSnapTurn(data.snapTurn);
        }

        // La ligne « Vitesse de rotation » : une copie de celle de la vignette, placée juste sous le bouton de rotation.
        void BuildTurnSpeedRow()
        {
            if (m_TurnSpeedSlider != null)
            {
                m_TurnSpeedRow = m_TurnSpeedSlider.transform.parent.gameObject;
                return;
            }

            if (m_VignetteSlider == null || m_SnapTurnButton == null)
                return;

            var template = m_VignetteSlider.transform.parent;
            var row = Instantiate(template.gameObject, template.parent, false);
            row.name = "Turn Speed row";
            row.transform.SetSiblingIndex(m_SnapTurnButton.transform.GetSiblingIndex() + 1);
            m_TurnSpeedRow = row;
            m_TurnSpeedSlider = row.GetComponentInChildren<Slider>(true);
            foreach (var text in row.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name == "Label")
                    text.text = "Vitesse de rotation";
                else if (text.name == "Value")
                    m_TurnSpeedValue = text;
            }

            // Une ligne de plus : on resserre un peu les lignes pour que tout tienne dans la page.
            if (template.parent.TryGetComponent<VerticalLayoutGroup>(out var layout))
                layout.spacing = Mathf.Min(layout.spacing, 12f);
            MenuArrowTarget.AttachAll(row);
        }

        void NextTurnMode()
        {
            var settings = GameSettings.Instance;
            if (settings == null)
            {
                Debug.LogWarning("SettingsPanel : aucun Game Settings dans la scène.", this);
                return;
            }

            var data = GameSettings.Data;
            if (!data.snapTurn)
            {
                settings.SmoothTurn = false;
                settings.SnapTurn = true;
            }
            else if (!data.smoothTurn)
            {
                settings.SmoothTurn = true;
            }
            else
            {
                settings.SnapTurn = false;
            }

            ShowSnapTurn(data.snapTurn);
        }

        void OnTurnSpeedChanged(float value)
        {
            var settings = GameSettings.Instance;
            if (settings != null)
                settings.TurnSpeed = Mathf.Lerp(GameSettings.MinTurnSpeed, GameSettings.MaxTurnSpeed, value);
            ShowTurnSpeed();
        }

        void ShowTurnSpeed()
        {
            var speed = Mathf.Clamp(GameSettings.Data.turnSpeed, GameSettings.MinTurnSpeed, GameSettings.MaxTurnSpeed);
            if (m_TurnSpeedSlider != null)
                m_TurnSpeedSlider.SetValueWithoutNotify(Mathf.InverseLerp(GameSettings.MinTurnSpeed, GameSettings.MaxTurnSpeed, speed));
            if (m_TurnSpeedValue != null)
                m_TurnSpeedValue.text = Mathf.RoundToInt(speed) + " °/s";
        }

        void Apply(System.Action<GameSettings> change, TMP_Text label, float value)
        {
            var settings = GameSettings.Instance;
            if (settings != null)
                change(settings);
            else
                Debug.LogWarning("SettingsPanel : aucun Game Settings dans la scène.", this);
            SetLabel(label, value);
        }

        void OnVignetteChanged(float value)
        {
            Apply(settings => settings.ComfortVignette = value, null, value);
            ShowVignette(value);
        }

        void ShowVignette(float value)
        {
            if (m_VignetteValue != null)
                m_VignetteValue.text = value < 0.01f ? "Aucune" : Mathf.RoundToInt(value * 100f) + " %";
        }

        void OnSnapTurnChanged(bool isOn)
        {
            var settings = GameSettings.Instance;
            if (settings != null)
                settings.SnapTurn = isOn;
            else
                Debug.LogWarning("SettingsPanel : aucun Game Settings dans la scène.", this);
            ShowSnapTurn(GameSettings.Data.snapTurn);
        }

        void ShowSnapTurn(bool isOn)
        {
            var smooth = isOn && GameSettings.Data.smoothTurn;
            if (m_SnapTurnToggle != null)
                m_SnapTurnToggle.SetIsOnWithoutNotify(isOn);
            if (m_SnapTurnText != null)
                m_SnapTurnText.text = "Rotation au joystick : " + (!isOn ? "non" : smooth ? "continue" : "par crans");

            // La vitesse ne sert qu'à la rotation continue.
            if (m_TurnSpeedRow != null)
                m_TurnSpeedRow.SetActive(smooth);
            ShowTurnSpeed();
        }

        static void Show(Slider slider, TMP_Text label, float value)
        {
            if (slider != null)
                slider.SetValueWithoutNotify(value);
            SetLabel(label, value);
        }

        static void SetLabel(TMP_Text label, float value)
        {
            if (label != null)
                label.text = Mathf.RoundToInt(value * 100f) + " %";
        }
    }
}
