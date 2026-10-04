using Archery.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Archery.Menus
{
    /// <summary>
    /// Page des paramètres (GDD, section 14) : volumes général, musique et effets, rotation par crans, vignette de confort.
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

        void Awake()
        {
            if (m_SnapTurnButton != null)
                m_SnapTurnButton.onClick.AddListener(() => OnSnapTurnChanged(!GameSettings.Data.snapTurn));

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
            if (m_SnapTurnToggle != null)
                m_SnapTurnToggle.SetIsOnWithoutNotify(isOn);
            if (m_SnapTurnText != null)
                m_SnapTurnText.text = "Rotation au joystick : " + (isOn ? "oui" : "non");
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
