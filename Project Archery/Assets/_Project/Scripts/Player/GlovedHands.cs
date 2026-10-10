using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Player
{
    /// <summary>
    /// Remplace les manettes affichées par des mains d'archer gantées de cuir : le gant low-poly de Quaternius (CC0),
    /// muni d'un squelette (Glove Rig.json). Les mains suivent les manettes, se ferment avec la poignée et la gâchette,
    /// serrent la poignée de l'arc, tiennent la flèche et crochètent la corde à l'encoche.
    /// La main droite porte un gant à trois doigts (pouce et auriculaire nus).
    /// À placer sur le XR Origin, à côté du <see cref="PlayerRig"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class GlovedHands : MonoBehaviour
    {
        [Tooltip("Squelette, poids et maillage du gant (Art/Hands/Glove Rig.json), main droite ; la gauche en est le miroir.")]
        [SerializeField]
        TextAsset m_GloveRig;

        [Header("Matériaux")]
        [SerializeField]
        Material m_Leather;

        [Tooltip("Manchettes.")]
        [SerializeField]
        Material m_DarkLeather;

        [Tooltip("Pouce et auriculaire de la main droite.")]
        [SerializeField]
        Material m_Skin;

        [Header("Réglage dans le casque")]
        [Tooltip("Décalage de la main gauche par rapport à la manette (m), dans le repère de la manette. " +
                 "La main droite prend le décalage symétrique. Réglable en jeu.")]
        [SerializeField]
        Vector3 m_PositionOffset;

        [Tooltip("Rotation de la main gauche par rapport à la manette (°). La main droite prend la rotation symétrique. " +
                 "X à -45 : la poignée des manettes Quest est inclinée vers l'avant.")]
        [SerializeField]
        Vector3 m_RotationOffset = new Vector3(-45f, 0f, 0f);

        [Tooltip("Rayon (m) de la poignée autour de laquelle le poing se ferme.")]
        [SerializeField]
        float m_GripRadius = 0.016f;

        [Tooltip("Distance (m) entre le repose-flèche et le centre du poing qui tient l'arc.")]
        [SerializeField]
        float m_BowGripDrop = 0.05f;

        [Tooltip("Cache les modèles des manettes du XR Origin.")]
        [SerializeField]
        bool m_HideControllerModels = true;

        readonly List<GlovedHand> m_Hands = new List<GlovedHand>();
        readonly List<GameObject> m_HiddenControllers = new List<GameObject>();

        public Material Leather => m_Leather;
        public Material DarkLeather => m_DarkLeather;
        public Material Skin => m_Skin;
        public float GripRadius => m_GripRadius;
        public float BowGripDrop => m_BowGripDrop;

        /// <summary>Pose de la main dans le repère de la manette (réglage), symétrique pour la main droite.</summary>
        public Pose OffsetFor(bool isLeft)
        {
            if (isLeft)
                return new Pose(m_PositionOffset, Quaternion.Euler(m_RotationOffset));

            var position = new Vector3(-m_PositionOffset.x, m_PositionOffset.y, m_PositionOffset.z);
            var euler = new Vector3(m_RotationOffset.x, -m_RotationOffset.y, -m_RotationOffset.z);
            return new Pose(position, Quaternion.Euler(euler));
        }

        void Start()
        {
            var rig = PlayerRig.Instance;
            if (rig == null)
            {
                Debug.LogWarning("GlovedHands : aucun PlayerRig dans la scène, pas de mains gantées.", this);
                enabled = false;
                return;
            }

            var parent = rig.Origin != null ? rig.Origin.transform : transform;
            foreach (var interactor in rig.Hands)
            {
                var isLeft = interactor.handedness == InteractorHandedness.Left;
                var instance = new GameObject(isLeft ? "Gloved Hand Left" : "Gloved Hand Right");
                instance.transform.SetParent(parent, false);
                var hand = instance.AddComponent<GlovedHand>();
                if (hand.Init(this, interactor, isLeft, m_GloveRig))
                    m_Hands.Add(hand);
                else
                    Destroy(instance);
            }

            if (m_HideControllerModels && m_Hands.Count > 0)
            {
                foreach (var child in parent.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name.EndsWith("Controller Visual") && child.gameObject.activeSelf)
                    {
                        child.gameObject.SetActive(false);
                        m_HiddenControllers.Add(child.gameObject);
                    }
                }
            }
        }

        void OnEnable() => SetHandsVisible(true);

        void OnDisable() => SetHandsVisible(false);

        // Désactiver le composant rend les manettes : utile pour comparer dans le casque.
        void SetHandsVisible(bool visible)
        {
            foreach (var hand in m_Hands)
            {
                if (hand != null)
                    hand.gameObject.SetActive(visible);
            }

            foreach (var controller in m_HiddenControllers)
            {
                if (controller != null)
                    controller.SetActive(!visible);
            }
        }
    }
}
