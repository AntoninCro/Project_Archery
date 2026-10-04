using System;
using Archery.Bows;
using Archery.Core;
using TMPro;
using UnityEngine;

namespace Archery.Difficulty
{
    /// <summary>
    /// Panneau à viser pour choisir une difficulté avant la première vague ; le ciel change en direct.
    /// Il prend la couleur de sa difficulté. Celui qui est choisi est plus gros et plus lumineux.
    /// </summary>
    /// <remarks>
    /// À placer sur l'objet du panneau ; les flèches touchent ses colliders (ou ceux de ses enfants).
    /// <see cref="Select"/> peut aussi être branché sur un bouton d'interface.
    /// </remarks>
    [DisallowMultipleComponent]
    public class DifficultyButton : MonoBehaviour, IArrowHitHandler
    {
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField]
        DifficultyDefinition m_Difficulty;

        [Tooltip("Optionnel : texte rempli avec le nom de la difficulté.")]
        [SerializeField]
        TMP_Text m_NameText;

        [Tooltip("Optionnel : texte rempli avec le résumé de la difficulté.")]
        [SerializeField]
        TMP_Text m_DescriptionText;

        [Tooltip("Rendus teintés avec la couleur de la difficulté (la planche du panneau, par exemple).")]
        [SerializeField]
        Renderer[] m_TintedRenderers = Array.Empty<Renderer>();

        [Tooltip("Optionnel : objet affiché seulement quand cette difficulté est choisie (cadre, halo…).")]
        [SerializeField]
        GameObject m_SelectedIndicator;

        [Tooltip("Taille du panneau choisi, par rapport aux autres.")]
        [SerializeField]
        float m_SelectedScale = 1.15f;

        [Tooltip("Luminosité de la couleur des panneaux qui ne sont pas choisis.")]
        [Range(0f, 1f)]
        [SerializeField]
        float m_UnselectedBrightness = 0.45f;

        [SerializeField]
        AudioClip m_SelectClip;

        MaterialPropertyBlock m_PropertyBlock;
        Vector3 m_BaseScale;
        float m_Pulse;
        bool m_IsSelected;

        public DifficultyDefinition Difficulty => m_Difficulty;

        void Awake()
        {
            m_BaseScale = transform.localScale;
            m_PropertyBlock = new MaterialPropertyBlock();
            if (m_Difficulty == null)
                Debug.LogError("DifficultyButton : aucune difficulté assignée.", this);
        }

        void OnEnable()
        {
            DifficultyManager.Changed += OnDifficultyChanged;
            Refresh();
        }

        void OnDisable()
        {
            DifficultyManager.Changed -= OnDifficultyChanged;
        }

        public bool OnArrowHit(in ArrowHit hit)
        {
            if (hit.IsShot)
                Select();
            return false;
        }

        /// <summary>Choisit la difficulté de ce panneau.</summary>
        public void Select()
        {
            var manager = DifficultyManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("DifficultyButton : aucun Difficulty Manager dans la scène.", this);
                return;
            }

            if (!manager.TrySelect(m_Difficulty))
                return;

            m_Pulse = 1f;
            Sfx.Play(m_SelectClip, transform.position);
        }

        void Update()
        {
            // Le panneau choisi grossit, avec une petite pulsation quand une flèche le touche.
            m_Pulse = Mathf.MoveTowards(m_Pulse, 0f, Time.deltaTime * 2.5f);
            var targetScale = (m_IsSelected ? m_SelectedScale : 1f) + 0.12f * m_Pulse;
            var smoothing = 1f - Mathf.Exp(-12f * Time.deltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, m_BaseScale * targetScale, smoothing);
        }

        void OnDifficultyChanged(DifficultyDefinition difficulty) => Refresh();

        void Refresh()
        {
            if (m_Difficulty == null)
                return;

            m_IsSelected = DifficultyManager.Current == m_Difficulty;

            if (m_NameText != null)
                m_NameText.text = m_Difficulty.displayName;
            if (m_DescriptionText != null)
                m_DescriptionText.text = m_Difficulty.description;
            if (m_SelectedIndicator != null)
                m_SelectedIndicator.SetActive(m_IsSelected);

            var color = m_Difficulty.color * (m_IsSelected ? 1f : m_UnselectedBrightness);
            color.a = 1f;
            foreach (var tinted in m_TintedRenderers)
            {
                if (tinted == null)
                    continue;
                tinted.GetPropertyBlock(m_PropertyBlock);
                m_PropertyBlock.SetColor(k_BaseColorId, color);
                tinted.SetPropertyBlock(m_PropertyBlock);
            }
        }
    }
}
