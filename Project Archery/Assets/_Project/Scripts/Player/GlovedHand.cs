using System;
using System.Collections.Generic;
using Archery.Bows;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Archery.Player
{
    /// <summary>
    /// Une main gantée, créée par <see cref="GlovedHands"/> à partir du gant low-poly de Quaternius (Art/Hands).
    /// Elle plie les doigts (repos, poing, crochet sur la corde, index tendu vers un menu) et se place
    /// sur la manette, sur la poignée de l'arc tenu ou sur l'encoche de la flèche tirée.
    /// </summary>
    /// <remarks>
    /// Le modèle d'origine n'a pas de squelette : « Glove Rig.json » lui en donne un (articulations placées dans
    /// la géométrie du gant, poids de chaque sommet), et la main est construite au lancement : os, maillage à facettes
    /// et SkinnedMeshRenderer. La main gauche est le miroir de la droite. La main droite porte un gant à trois doigts
    /// (pouce et auriculaire nus).
    /// Pas d'animation : chaque articulation tourne autour d'un axe calculé au démarrage depuis le squelette,
    /// qui ramène le doigt vers la paume. Les angles du poing ont été ajustés (en simulation) pour que chaque
    /// doigt s'enroule autour d'une poignée de 3,2 cm de diamètre.
    /// </remarks>
    [DisallowMultipleComponent]
    public class GlovedHand : MonoBehaviour
    {
        enum Mode
        {
            Free,
            Fist,
            Bow,
            Draw,
            Point,
        }

        sealed class Joint
        {
            public Transform transform;
            public Quaternion rest;
            public Vector3 axis;
        }

        // Données de « Glove Rig.json » (main droite, en mètres, poignet à l'origine).
        [Serializable]
        sealed class RigJoint
        {
            public string name;
            public int parent;
            public Vector3 position;
        }

        [Serializable]
        sealed class RigData
        {
            public RigJoint[] joints;
            public float[] vertices;
            public int[] bones;
            public float[] weights;
            public int[] leather;
            public int[] bare;
            public int[] cuff;
        }

        // Pouce : métacarpe, phalange proximale, distale. Autres doigts : proximale, intermédiaire, distale.
        static readonly string[] k_Fingers = { "Thumb", "Index", "Middle", "Ring", "Little" };

        // Angles (°) ajoutés à la pose de repos (gant à plat), doigt par doigt.
        static readonly Vector3[] k_Fist =
        {
            new Vector3(20f, 30f, 30f), new Vector3(44f, 90f, 45f), new Vector3(38f, 86f, 55f),
            new Vector3(35f, 74f, 55f), new Vector3(29f, 58f, 40f),
        };

        // Prise méditerranéenne : index au-dessus de la flèche, majeur et annulaire en dessous, en crochet sur la corde.
        static readonly Vector3[] k_Hook =
        {
            new Vector3(15f, 25f, 20f), new Vector3(18f, 70f, 50f), new Vector3(18f, 72f, 50f),
            new Vector3(22f, 72f, 50f), new Vector3(60f, 85f, 45f),
        };

        static readonly Vector3[] k_Point =
        {
            new Vector3(20f, 30f, 30f), new Vector3(-5f, 0f, 0f), new Vector3(38f, 86f, 55f),
            new Vector3(35f, 74f, 55f), new Vector3(29f, 58f, 40f),
        };

        const float k_FingerSpeed = 18f;
        const float k_BlendTime = 0.12f;

        // Au repos, les doigts restent un peu fermés autour de la manette.
        const float k_RestCurl = 0.15f;

        GlovedHands m_Owner;
        XRBaseInputInteractor m_Interactor;
        NearFarInteractor m_NearFar;
        bool m_IsLeft;

        readonly Joint[,] m_Joints = new Joint[5, 3];
        readonly Vector3[] m_Angles = new Vector3[5];
        readonly Vector3[] m_Targets = new Vector3[5];

        // Dans le repère de la racine de la main : axe du poing (de l'auriculaire vers l'index) et normale de la paume,
        // centre de la poignée tenue dans le poing, point où la corde se loge dans le crochet des doigts.
        Quaternion m_InverseHandBasis = Quaternion.identity;
        Vector3 m_GripCenter;
        Vector3 m_HookPoint;

        Mode m_Mode;
        Bow m_Bow;
        Arrow m_Arrow;

        // Pose de la main dans le repère de la manette, et fondu au changement de prise.
        bool m_Placed;
        float m_Blend = 1f;
        Vector3 m_RelativePosition;
        Quaternion m_RelativeRotation = Quaternion.identity;
        Vector3 m_FromPosition;
        Quaternion m_FromRotation = Quaternion.identity;

        /// <summary>Construit la main depuis les données du gant. Renvoie false si elles sont incomplètes.</summary>
        public bool Init(GlovedHands owner, XRBaseInputInteractor interactor, bool isLeft, TextAsset rig)
        {
            m_Owner = owner;
            m_Interactor = interactor;
            m_NearFar = interactor as NearFarInteractor;
            m_IsLeft = isLeft;

            var data = rig != null ? JsonUtility.FromJson<RigData>(rig.text) : null;
            if (data == null || data.joints == null || data.joints.Length == 0 || data.vertices == null)
            {
                Debug.LogError("GlovedHand : les données du gant (Glove Rig.json) sont absentes ou vides.", this);
                return false;
            }

            BuildGlove(data);

            // Os nommés « L_IndexProximal », « R_Wrist »… : on retire le préfixe.
            var bones = new Dictionary<string, Transform>();
            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Length > 2 && child.name[1] == '_')
                    bones[child.name.Substring(2)] = child;
            }

            for (var f = 0; f < k_Fingers.Length; f++)
            {
                foreach (var boneName in Chain(f))
                {
                    if (!bones.ContainsKey(boneName))
                    {
                        Debug.LogError("GlovedHand : il manque l'os « " + boneName + " » dans les données du gant.", this);
                        return false;
                    }
                }
            }

            if (!bones.TryGetValue("Wrist", out var wrist) || !bones.ContainsKey("LittleMetacarpal"))
            {
                Debug.LogError("GlovedHand : il manque le poignet ou le métacarpe de l'auriculaire dans les données du gant.", this);
                return false;
            }

            var index = bones["IndexProximal"].position;
            var little = bones["LittleProximal"].position;
            var knuckles = (index + bones["MiddleProximal"].position + bones["RingProximal"].position + little) * 0.25f;
            var forward = (knuckles - wrist.position).normalized;
            var across = (index - little).normalized;
            var palm = Vector3.Cross(bones["MiddleProximal"].position - wrist.position, index - little).normalized;
            if (isLeft)
                palm = -palm;

            // Axes de flexion : ils amènent chaque phalange vers la paume ; le pouce se replie en travers, vers l'auriculaire.
            var littleBase = bones["LittleMetacarpal"].position;
            for (var f = 0; f < k_Fingers.Length; f++)
            {
                var chain = Chain(f);
                for (var j = 0; j < 3; j++)
                {
                    var joint = bones[chain[j]];
                    var direction = (bones[chain[j + 1]].position - joint.position).normalized;
                    var toward = f == 0 ? palm * 0.6f + (littleBase - joint.position).normalized * 0.4f : palm;
                    m_Joints[f, j] = new Joint
                    {
                        transform = joint,
                        rest = joint.localRotation,
                        axis = joint.InverseTransformDirection(Vector3.Cross(direction, toward).normalized),
                    };
                }
            }

            var root = transform;
            m_InverseHandBasis = Quaternion.Inverse(Quaternion.LookRotation(
                root.InverseTransformDirection(across), root.InverseTransformDirection(palm)));
            var center = knuckles + palm * (0.012f + owner.GripRadius) - forward * 0.012f;
            m_GripCenter = root.InverseTransformPoint(center);

            ApplyAngles(k_Hook);
            m_HookPoint = root.InverseTransformPoint((bones["IndexDistal"].position + bones["MiddleDistal"].position) * 0.5f);
            ApplyAngles(m_Angles);
            return true;
        }

        static string[] Chain(int finger)
        {
            var prefix = k_Fingers[finger];
            return finger == 0
                ? new[] { prefix + "Metacarpal", prefix + "Proximal", prefix + "Distal", prefix + "Tip" }
                : new[] { prefix + "Proximal", prefix + "Intermediate", prefix + "Distal", prefix + "Tip" };
        }

        void OnEnable() => Application.onBeforeRender += OnBeforeRender;

        void OnDisable() => Application.onBeforeRender -= OnBeforeRender;

        void LateUpdate()
        {
            if (m_Owner == null)
                return;

            if (m_Interactor == null)
            {
                gameObject.SetActive(false);
                return;
            }

            var deltaTime = Time.deltaTime;
            UpdateMode();
            UpdateTargets();
            var blend = 1f - Mathf.Exp(-k_FingerSpeed * deltaTime);
            for (var f = 0; f < m_Angles.Length; f++)
                m_Angles[f] = Vector3.Lerp(m_Angles[f], m_Targets[f], blend);
            ApplyAngles(m_Angles);
            PlaceHand(deltaTime);
        }

        // Juste avant le rendu, la manette, l'arc et la flèche ont leur pose la plus récente : la main les rejoint.
        void OnBeforeRender()
        {
            if (m_Owner != null && m_Interactor != null && isActiveAndEnabled)
                PlaceHand(0f);
        }

        void UpdateMode()
        {
            Bow bow = null;
            Arrow arrow = null;
            var holding = false;
            var selected = m_Interactor.interactablesSelected;
            for (var i = 0; i < selected.Count; i++)
            {
                if (selected[i] is Bow heldBow)
                    bow = heldBow;
                else if (selected[i] is Arrow heldArrow)
                    arrow = heldArrow;
                holding = true;
            }

            Mode mode;
            if (bow != null && bow.Model != null && bow.ArrowRest != null)
                mode = Mode.Bow;
            else if (arrow != null && arrow.CurrentState == Arrow.State.Nocked)
                mode = Mode.Draw;
            else if (holding)
                mode = Mode.Fist;
            else if (m_NearFar != null && m_NearFar.TryGetCurrentUIRaycastResult(out _))
                mode = Mode.Point;
            else
                mode = Mode.Free;

            m_Bow = bow;
            m_Arrow = arrow;
            if (mode == m_Mode)
                return;

            m_Mode = mode;
            m_FromPosition = m_RelativePosition;
            m_FromRotation = m_RelativeRotation;
            m_Blend = m_Placed ? 0f : 1f;
        }

        void UpdateTargets()
        {
            switch (m_Mode)
            {
                case Mode.Bow:
                case Mode.Fist:
                    k_Fist.CopyTo(m_Targets, 0);
                    break;
                case Mode.Draw:
                    k_Hook.CopyTo(m_Targets, 0);
                    break;
                case Mode.Point:
                    k_Point.CopyTo(m_Targets, 0);
                    break;
                default:
                    // Main libre : la poignée ferme les doigts, la gâchette plie l'index.
                    var grip = m_Interactor.selectInput != null ? m_Interactor.selectInput.ReadValue() : 0f;
                    var trigger = m_Interactor.activateInput != null ? m_Interactor.activateInput.ReadValue() : 0f;
                    for (var f = 0; f < m_Targets.Length; f++)
                    {
                        var amount = f == 0 ? Mathf.Max(grip, trigger * 0.5f) : f == 1 ? Mathf.Max(trigger, grip * 0.5f) : grip;
                        m_Targets[f] = k_Fist[f] * Mathf.Max(k_RestCurl, amount);
                    }

                    break;
            }
        }

        void ApplyAngles(Vector3[] angles)
        {
            for (var f = 0; f < angles.Length; f++)
            {
                for (var j = 0; j < 3; j++)
                {
                    var joint = m_Joints[f, j];
                    if (joint != null)
                        joint.transform.localRotation = joint.rest * Quaternion.AngleAxis(angles[f][j], joint.axis);
                }
            }
        }

        void PlaceHand(float deltaTime)
        {
            var controller = m_Interactor.transform;
            var controllerPosition = controller.position;
            var controllerRotation = controller.rotation;
            var offset = m_Owner.OffsetFor(m_IsLeft);
            var side = m_IsLeft ? 1f : -1f;

            // Poignée de la manette : l'axe du poing suit l'avant de la manette, la paume regarde vers l'intérieur.
            var gripRotation = controllerRotation * offset.rotation * Quaternion.LookRotation(Vector3.forward, Vector3.right * side);

            Quaternion targetRotation;
            Vector3 targetPoint;
            Vector3 localPoint;
            switch (m_Mode)
            {
                case Mode.Bow when m_Bow != null:
                    // Le poing serre la poignée sous le repose-flèche, paume vers l'intérieur de l'arc.
                    var model = m_Bow.Model;
                    targetRotation = Quaternion.LookRotation(model.up, model.right * side) * m_InverseHandBasis;
                    targetPoint = m_Bow.ArrowRest.position - model.up * m_Owner.BowGripDrop;
                    localPoint = m_GripCenter;
                    break;
                case Mode.Draw when m_Arrow != null:
                    // La corde se loge dans le crochet des doigts, à l'encoche.
                    targetRotation = gripRotation * m_InverseHandBasis;
                    targetPoint = m_Arrow.NockPosition;
                    localPoint = m_HookPoint;
                    break;
                default:
                    targetRotation = gripRotation * m_InverseHandBasis;
                    targetPoint = controllerPosition + controllerRotation * offset.position;
                    localPoint = m_GripCenter;
                    break;
            }

            var targetPosition = targetPoint - targetRotation * Vector3.Scale(localPoint, transform.lossyScale);
            var inverse = Quaternion.Inverse(controllerRotation);
            var relativePosition = inverse * (targetPosition - controllerPosition);
            var relativeRotation = inverse * targetRotation;

            if (!m_Placed)
            {
                m_Placed = true;
                m_Blend = 1f;
            }

            if (m_Blend < 1f)
            {
                m_Blend = Mathf.Min(1f, m_Blend + deltaTime / k_BlendTime);
                var s = Mathf.SmoothStep(0f, 1f, m_Blend);
                relativePosition = Vector3.Lerp(m_FromPosition, relativePosition, s);
                relativeRotation = Quaternion.Slerp(m_FromRotation, relativeRotation, s);
            }

            m_RelativePosition = relativePosition;
            m_RelativeRotation = relativeRotation;
            transform.SetPositionAndRotation(controllerPosition + controllerRotation * relativePosition, controllerRotation * relativeRotation);
        }

        // Os (enfants de la racine), puis maillage à facettes, pondéré sur deux os par sommet, en trois matières :
        // cuir, doigts nus (peau sur la main droite, cuir sur la gauche), manchette (cuir foncé).
        void BuildGlove(RigData data)
        {
            var root = transform;
            var mirror = m_IsLeft ? -1f : 1f;
            var prefix = m_IsLeft ? "L_" : "R_";
            var bones = new Transform[data.joints.Length];
            for (var i = 0; i < data.joints.Length; i++)
            {
                var joint = data.joints[i];
                var bone = new GameObject(prefix + joint.name).transform;
                bone.SetParent(joint.parent >= 0 && joint.parent < i ? bones[joint.parent] : root, false);
                bone.position = root.TransformPoint(new Vector3(joint.position.x * mirror, joint.position.y, joint.position.z));
                bones[i] = bone;
            }

            var count = data.vertices.Length / 3;
            var vertices = new Vector3[count];
            var weights = new BoneWeight[count];
            for (var k = 0; k < count; k++)
            {
                vertices[k] = new Vector3(data.vertices[3 * k] * mirror, data.vertices[3 * k + 1], data.vertices[3 * k + 2]);
                weights[k] = new BoneWeight
                {
                    boneIndex0 = data.bones[2 * k],
                    weight0 = data.weights[2 * k],
                    boneIndex1 = data.bones[2 * k + 1],
                    weight1 = data.weights[2 * k + 1],
                };
            }

            var mesh = new Mesh { name = m_IsLeft ? "Glove Left" : "Glove Right" };
            mesh.vertices = vertices;
            mesh.boneWeights = weights;
            mesh.subMeshCount = 3;
            mesh.SetTriangles(Triangles(data.leather), 0);
            mesh.SetTriangles(Triangles(data.bare), 1);
            mesh.SetTriangles(Triangles(data.cuff), 2);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var skin = new GameObject("Glove");
            skin.transform.SetParent(root, false);
            var bindposes = new Matrix4x4[bones.Length];
            for (var i = 0; i < bones.Length; i++)
                bindposes[i] = bones[i].worldToLocalMatrix * skin.transform.localToWorldMatrix;
            mesh.bindposes = bindposes;

            var renderer = skin.AddComponent<SkinnedMeshRenderer>();
            renderer.bones = bones;
            renderer.rootBone = bones[0];
            renderer.sharedMesh = mesh;
            renderer.sharedMaterials = new[] { m_Owner.Leather, m_IsLeft ? m_Owner.Leather : m_Owner.Skin, m_Owner.DarkLeather };
            renderer.updateWhenOffscreen = true;
            renderer.shadowCastingMode = ShadowCastingMode.On;
        }

        // Triangles du fichier (main droite) ; le miroir de la main gauche inverse leur sens.
        int[] Triangles(int[] indices)
        {
            var triangles = (int[])indices.Clone();
            if (m_IsLeft)
            {
                for (var t = 0; t + 2 < triangles.Length; t += 3)
                    (triangles[t + 1], triangles[t + 2]) = (triangles[t + 2], triangles[t + 1]);
            }

            return triangles;
        }
    }
}
