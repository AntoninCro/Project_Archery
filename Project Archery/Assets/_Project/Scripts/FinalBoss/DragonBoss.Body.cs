using System;
using Archery.Combat;
using Archery.Core;
using UnityEngine;

namespace Archery.FinalBoss
{
    // Le corps du dragon : os, zones de touche, yeux, lueur de la gueule, et les poses ajoutées par le code par-dessus
    // les animations du modèle (tête levée, mâchoire, coup de patte, salto raide).
    public partial class DragonBoss
    {
        static readonly string[] k_NeckChain = { "Neck01", "Neck02", "Neck03", "Head" };
        static readonly float[] k_NeckShares = { 0.3f, 0.3f, 0.25f, 0.15f };

        Transform[] m_NeckBones;
        Transform m_Mandible;
        Transform m_UpperArmL;
        Transform m_UpperArmR;
        Transform m_HandL;
        Transform m_HandR;
        Transform[] m_ArmL;
        Transform[] m_ArmR;
        Transform[] m_WingL;
        Transform[] m_WingR;
        Transform[] m_WingTipL;
        Transform[] m_WingTipR;
        Transform[] m_LegL;
        Transform[] m_LegR;
        Transform[] m_Tail;
        Vector3 m_HindPivot = new Vector3(0f, 0f, -1.7f);
        Vector3 m_FrontPaws = new Vector3(0f, 0f, 1.6f);
        Transform[] m_ModelBones = Array.Empty<Transform>();
        Vector3[] m_RestPositions;
        Quaternion[] m_RestRotations;
        Vector3 m_BodyCenter = new Vector3(0f, 2f, 0f);
        float m_WalkNatural;
        float m_RunNatural;
        Light m_EyeLight;
        Transform m_MouthGlow;
        Light m_MouthLight;

        // Poses ajoutées : ce que l'état en cours demande (Target), et où elles en sont (lissées).
        float m_NeckTarget;
        float m_NeckPitch;
        float m_JawTarget;
        float m_Jaw;
        float m_GlowTarget;
        float m_Glow;
        float m_PawTarget;
        float m_Paw;
        Vector3 m_PawDirection = Vector3.forward;
        float m_TPoseTarget;
        float m_TPose;
        float m_RearTarget;
        float m_Rear;

        void ClearPose()
        {
            m_NeckTarget = 0f;
            m_JawTarget = 0f;
            m_GlowTarget = 0f;
            m_PawTarget = 0f;
            m_TPoseTarget = 0f;
            m_RearTarget = 0f;
        }

        // Après l'animation de l'image : les poses ajoutées, puis ce qui part de la gueule (souffle, boule de feu, lueur).
        void LateUpdate()
        {
            var deltaTime = Time.deltaTime;
            var speed = Mathf.Max(1f, m_Tempo);
            m_NeckPitch = Mathf.Lerp(m_NeckPitch, m_NeckTarget, 1f - Mathf.Exp(-deltaTime * 12f * speed));
            m_Jaw = Mathf.Lerp(m_Jaw, m_JawTarget, 1f - Mathf.Exp(-deltaTime * 12f * speed));
            m_Glow = Mathf.Lerp(m_Glow, m_GlowTarget, 1f - Mathf.Exp(-deltaTime * 8f));
            m_Paw = Mathf.Lerp(m_Paw, m_PawTarget, 1f - Mathf.Exp(-deltaTime * 16f * speed));
            m_TPose = Mathf.MoveTowards(m_TPose, m_TPoseTarget, deltaTime * 5f);
            m_Rear = Mathf.Lerp(m_Rear, m_RearTarget, 1f - Mathf.Exp(-deltaTime * 16f * speed));

            if (m_TPose > 0.001f)
                ApplyTPose(m_TPose);
            if (m_Rear > 0.001f)
                ApplyRear(m_Rear);
            if (Mathf.Abs(m_NeckPitch) > 0.05f)
                ApplyNeck(m_NeckPitch);
            if (m_Jaw > 0.001f && m_Mandible != null)
                m_Mandible.rotation = Quaternion.AngleAxis(m_Jaw * 28f, transform.right) * m_Mandible.rotation;
            if (m_Paw > 0.001f)
                ApplyPaw(m_Paw);

            UpdateMouthGlow();
            if (m_Held != null)
                m_Held.Hold(HeldPosition(m_HeldCharge), m_HeldCharge);

            if (m_Breath != null && m_Firing)
            {
                var mouth = MouthPosition;
                m_Breath.transform.SetPositionAndRotation(mouth, Quaternion.LookRotation(m_BreathAim));
                if (m_BreathLight != null)
                    m_BreathLight.transform.position = mouth + m_BreathAim * 3f;
            }
        }

        // La tête se lève (angle négatif) ou plonge, répartie sur les os du cou.
        void ApplyNeck(float pitch)
        {
            var axis = transform.right;
            for (var i = 0; i < m_NeckBones.Length; i++)
            {
                var bone = m_NeckBones[i];
                if (bone != null)
                    bone.rotation = Quaternion.AngleAxis(pitch * k_NeckShares[i], axis) * bone.rotation;
            }
        }

        // La patte qui frappe : l'épaule tourne pour que la main parte dans la direction voulue.
        void ApplyPaw(float weight)
        {
            var shoulder = m_PawSide > 0 ? m_UpperArmR : m_UpperArmL;
            var hand = m_PawSide > 0 ? m_HandR : m_HandL;
            if (shoulder == null || hand == null)
                return;

            var current = hand.position - shoulder.position;
            if (current.sqrMagnitude < 1e-4f)
                return;
            shoulder.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(current, m_PawDirection), weight) * shoulder.rotation;
        }

        // Le salto « raté » : tout le corps se fige dans sa pose de repos, pattes et ailes tendues sur les côtés, raide
        // comme un piquet (une T-pose de dragon).
        void ApplyTPose(float weight)
        {
            for (var i = 0; i < m_ModelBones.Length; i++)
            {
                if (m_ModelBones[i] != null)
                    m_ModelBones[i].localRotation = Quaternion.Slerp(m_ModelBones[i].localRotation, m_RestRotations[i], weight);
            }

            var right = transform.right;
            var up = transform.up;
            var forward = transform.forward;
            AimChain(m_ArmL, -right, weight);
            AimChain(m_ArmR, right, weight);
            AimChain(m_WingL, (-right + up * 0.3f).normalized, weight);
            AimChain(m_WingR, (right + up * 0.3f).normalized, weight);
            AimChain(m_WingTipL, (-right * 0.8f - forward * 0.6f).normalized, weight);
            AimChain(m_WingTipR, (right * 0.8f - forward * 0.6f).normalized, weight);
            AimChain(m_LegL, (-right * 0.6f - up).normalized, weight);
            AimChain(m_LegR, (right * 0.6f - up).normalized, weight);
        }

        // Dressé sur ses pattes arrière : les pattes avant levées devant lui, la queue posée au sol derrière (comme un trépied).
        void ApplyRear(float weight)
        {
            var arms = (transform.forward + transform.up * 0.2f).normalized;
            AimChain(m_ArmL, (arms - transform.right * 0.25f).normalized, weight * 0.8f);
            AimChain(m_ArmR, (arms + transform.right * 0.25f).normalized, weight * 0.8f);
            AimChain(m_Tail, (-Flat(transform.forward) - Vector3.up * 0.2f).normalized, weight);
        }

        // Tend une chaîne d'os (épaule, coude, main…) dans une direction : chaque os pointe droit vers le suivant.
        static void AimChain(Transform[] chain, Vector3 direction, float weight)
        {
            if (chain == null)
                return;

            for (var i = 0; i < chain.Length - 1; i++)
            {
                var bone = chain[i];
                var next = chain[i + 1];
                if (bone == null || next == null)
                    continue;

                var current = next.position - bone.position;
                if (current.sqrMagnitude < 1e-6f)
                    continue;
                bone.rotation = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(current, direction), weight) * bone.rotation;
            }
        }

        // Une lumière de plus en plus vive au fond de la gueule ouverte (charge du souffle).
        void UpdateMouthGlow()
        {
            var on = m_Glow > 0.02f && !IsHidden;
            if (m_MouthGlow != null)
            {
                if (m_MouthGlow.gameObject.activeSelf != on)
                    m_MouthGlow.gameObject.SetActive(on);
                if (on)
                {
                    m_MouthGlow.position = MouthPosition - SnoutDirection * 0.3f;
                    m_MouthGlow.localScale = Vector3.one * Mathf.Lerp(0.15f, 0.75f, m_Glow);
                }
            }

            if (m_MouthLight == null)
                return;

            m_MouthLight.enabled = on;
            if (!on)
                return;
            m_MouthLight.transform.position = MouthPosition + SnoutDirection * 0.4f;
            m_MouthLight.intensity = 10f * m_Glow;
            m_MouthLight.range = 4f + 8f * m_Glow;
        }

        // ---------------------------------------------------------------- pose de repos et foulée

        // La pose du modèle avant toute animation (sa pose de repos), et le milieu de son corps.
        void CaptureRestPose()
        {
            var model = m_Animator != null ? m_Animator.transform : transform;
            var all = model.GetComponentsInChildren<Transform>(true);
            m_ModelBones = new Transform[Mathf.Max(0, all.Length - 1)];
            Array.Copy(all, 1, m_ModelBones, 0, m_ModelBones.Length);
            m_RestPositions = new Vector3[m_ModelBones.Length];
            m_RestRotations = new Quaternion[m_ModelBones.Length];
            for (var i = 0; i < m_ModelBones.Length; i++)
            {
                m_RestPositions[i] = m_ModelBones[i].localPosition;
                m_RestRotations[i] = m_ModelBones[i].localRotation;
            }

            if (m_Spine != null && m_Chest != null)
                m_BodyCenter = transform.InverseTransformPoint((m_Spine.position + m_Chest.position) * 0.5f);

            // La portée de sa tête dépend de la taille du modèle : mesurée sur sa pose de repos (debout, tête en avant).
            if (m_Head != null)
                m_Reach = Mathf.Max(3f, HorizontalDistance(transform.position, m_Head.position) + m_MeleeExtraReach);

            m_NeckBones = new Transform[k_NeckChain.Length];
            for (var i = 0; i < k_NeckChain.Length; i++)
                m_NeckBones[i] = FindDeep(transform, k_NeckChain[i]);
            m_Mandible = FindBone(m_JawBone);
            m_ArmL = Chain("UpperArm_L", "LowerArm_L", "Hand_L");
            m_ArmR = Chain("UpperArm_R", "RowerArm_R", "Hand_R");
            m_UpperArmL = m_ArmL[0];
            m_UpperArmR = m_ArmR[0];
            m_HandL = m_ArmL[2];
            m_HandR = m_ArmR[2];
            m_WingL = Chain("Wing01_L", "Wing02_L", "Wing03_L");
            m_WingR = Chain("Wing01_R", "Wing02_R", "Wing03_R");
            m_WingTipL = Chain("Wing03_L", "WingMiddle05_L");
            m_WingTipR = Chain("Wing03_R", "WingMiddle05_R");
            m_LegL = Chain("UpperLeg_L", "LowerLeg_L", "Feet_L");
            m_LegR = Chain("UpperReg_R", "RowerReg_R", "Feet_R");
            m_Tail = Chain("joint19", "joint20", "joint21", "joint22", "joint23");

            // Il se dresse en pivotant sur ses pattes arrière, et frappe le sol entre ses pattes avant.
            if (m_LegL[2] != null && m_LegR[2] != null)
            {
                m_HindPivot = transform.InverseTransformPoint((m_LegL[2].position + m_LegR[2].position) * 0.5f);
                m_HindPivot.y = 0f;
            }

            if (m_HandL != null && m_HandR != null)
            {
                m_FrontPaws = transform.InverseTransformPoint((m_HandL.position + m_HandR.position) * 0.5f);
                m_FrontPaws.y = 0f;
            }
        }

        Transform[] Chain(params string[] names)
        {
            var chain = new Transform[names.Length];
            for (var i = 0; i < names.Length; i++)
                chain[i] = FindDeep(transform, names[i]);
            return chain;
        }

        void RestorePose()
        {
            for (var i = 0; i < m_ModelBones.Length; i++)
            {
                if (m_ModelBones[i] == null)
                    continue;
                m_ModelBones[i].localPosition = m_RestPositions[i];
                m_ModelBones[i].localRotation = m_RestRotations[i];
            }
        }

        // Vitesse à laquelle la marche et la course avancent, jouées normalement : réglée à la main, sinon mesurée.
        void MeasureStrides()
        {
            m_WalkNatural = m_WalkStrideSpeed > 0f ? m_WalkStrideSpeed : MeasureStride("Walk");
            m_RunNatural = m_RunStrideSpeed > 0f ? m_RunStrideSpeed : MeasureStride("Run");
            RestorePose();
        }

        // On joue l'animation image par image : une patte posée (au plus bas de sa course) recule sous le corps à la
        // vitesse où le dragon devrait avancer.
        float MeasureStride(string clipName)
        {
            if (m_Animator == null || m_Animator.runtimeAnimatorController == null)
                return 0f;

            AnimationClip clip = null;
            foreach (var candidate in m_Animator.runtimeAnimatorController.animationClips)
            {
                if (candidate != null && candidate.name == clipName)
                {
                    clip = candidate;
                    break;
                }
            }

            if (clip == null || clip.length <= 0f)
                return 0f;

            var feet = new[] { m_HandL, m_HandR, FindDeep(transform, "Feet_L"), FindDeep(transform, "Feet_R") };
            const int samples = 40;
            var heights = new float[feet.Length, samples + 1];
            var depths = new float[feet.Length, samples + 1];
            try
            {
                for (var i = 0; i <= samples; i++)
                {
                    clip.SampleAnimation(m_Animator.gameObject, clip.length * i / samples);
                    for (var f = 0; f < feet.Length; f++)
                    {
                        if (feet[f] == null)
                            continue;
                        var local = transform.InverseTransformPoint(feet[f].position);
                        heights[f, i] = local.y;
                        depths[f, i] = local.z;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"DragonBoss : foulée de « {clipName} » non mesurée ({exception.Message}).", this);
                return 0f;
            }

            var distance = 0f;
            var time = 0f;
            var step = clip.length / samples;
            for (var f = 0; f < feet.Length; f++)
            {
                if (feet[f] == null)
                    continue;

                var lowest = float.MaxValue;
                for (var i = 0; i <= samples; i++)
                    lowest = Mathf.Min(lowest, heights[f, i]);
                for (var i = 0; i < samples; i++)
                {
                    if (heights[f, i] > lowest + 0.12f || heights[f, i + 1] > lowest + 0.12f)
                        continue;
                    var back = depths[f, i] - depths[f, i + 1];
                    if (back <= 0f)
                        continue;
                    distance += back;
                    time += step;
                }
            }

            return time > 0.05f ? distance / time : 0f;
        }

        // ---------------------------------------------------------------- construction

        Transform FindBone(string boneName)
        {
            if (string.IsNullOrEmpty(boneName))
                return null;

            var bone = FindDeep(transform, boneName);
            if (bone == null)
                Debug.LogWarning($"DragonBoss : os « {boneName} » introuvable dans le modèle.", this);
            return bone;
        }

        static Transform FindDeep(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;
                var found = FindDeep(child, name);
                if (found != null)
                    return found;
            }

            return null;
        }

        void SetHitboxesEnabled(bool enabled)
        {
            foreach (var hitbox in m_Hitboxes)
            {
                if (hitbox != null)
                    hitbox.enabled = enabled;
            }
        }

        // Zones de touche accrochées aux os : leur taille suit l'écart entre les os, donc la taille du modèle.
        void BuildHitboxes()
        {
            var health = GetComponent<Health>();
            var scale = Mathf.Max(0.1f, m_HitboxScale);

            if (m_Head != null && m_Neck != null)
            {
                var length = Vector3.Distance(m_Head.position, m_Neck.position);
                var snout = (m_Head.position - m_Neck.position).normalized;
                AddSphere("Zone tête", m_Head, m_Head.position + snout * length * 0.5f, length * 0.75f * scale, HitZone.Head, 2f, health);
            }

            AddChain(new[] { "Neck01", "Neck02", "Neck03", m_HeadBone }, 0.35f, HitZone.Body, health, scale);
            if (m_Spine != null && m_Chest != null)
            {
                var length = Vector3.Distance(m_Spine.position, m_Chest.position);
                AddCapsule("Zone corps", m_Spine, m_Spine.position, m_Chest.position, length * 0.65f * scale, HitZone.Body, 1f, health);
                AddSphere("Zone poitrine", m_Chest, m_Chest.position, length * 0.7f * scale, HitZone.Body, 1f, health);
            }

            AddChain(new[] { "UpperArm_L", "LowerArm_L", "Hand_L" }, 0.3f, HitZone.Body, health, scale);
            AddChain(new[] { "UpperArm_R", "RowerArm_R", "Hand_R" }, 0.3f, HitZone.Body, health, scale);
            AddChain(new[] { "UpperLeg_L", "LowerLeg_L", "Feet_L" }, 0.3f, HitZone.Body, health, scale);
            AddChain(new[] { "UpperReg_R", "RowerReg_R", "Feet_R" }, 0.3f, HitZone.Body, health, scale);
            BuildWeakPoint(health, scale);
        }

        // Point faible : une boule cyan qui pulse sous la poitrine (dégâts ×3), comme les autres boss.
        void BuildWeakPoint(Health health, float scale)
        {
            if (m_Chest == null || m_Spine == null)
                return;

            var bodyLength = Vector3.Distance(m_Spine.position, m_Chest.position);
            var forward = m_NeckBase != null ? (m_NeckBase.position - m_Chest.position).normalized : transform.forward;
            var position = m_Chest.position + forward * bodyLength * 0.3f - transform.up * bodyLength * 0.45f;
            var radius = bodyLength * 0.28f * scale;

            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "Point faible";
            orb.transform.SetParent(m_Chest, true);
            orb.transform.position = position;
            orb.transform.localScale = Vector3.one;
            orb.transform.localScale = Vector3.one * (radius * 2f) / Mathf.Max(1e-4f, orb.transform.lossyScale.x);
            var renderer = orb.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = m_WeakPointMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            orb.AddComponent<Pulse>();

            var collider = orb.GetComponent<SphereCollider>();
            collider.radius = 0.6f;   // un peu plus grand que la boule visible (rayon 0,5)
            var hitbox = orb.AddComponent<Hitbox>();
            hitbox.Configure(HitZone.WeakPoint, 3f, health);
            m_Hitboxes.Add(collider);
        }

        void AddChain(string[] bones, float radiusFactor, HitZone zone, Health health, float scale)
        {
            for (var i = 0; i < bones.Length - 1; i++)
            {
                var from = FindDeep(transform, bones[i]);
                var to = FindDeep(transform, bones[i + 1]);
                if (from == null || to == null)
                    continue;

                var length = Vector3.Distance(from.position, to.position);
                if (length < 1e-3f)
                    continue;
                AddCapsule($"Zone {bones[i]}", from, from.position, to.position, length * radiusFactor * scale, zone, 1f, health);
            }
        }

        void AddSphere(string name, Transform bone, Vector3 center, float radius, HitZone zone, float multiplier, Health health)
        {
            var go = new GameObject(name);
            go.transform.SetParent(bone, false);
            go.transform.position = center;
            var collider = go.AddComponent<SphereCollider>();
            collider.radius = radius / Mathf.Max(1e-4f, go.transform.lossyScale.x);
            var hitbox = go.AddComponent<Hitbox>();
            hitbox.Configure(zone, multiplier, health);
            m_Hitboxes.Add(collider);
        }

        void AddCapsule(string name, Transform bone, Vector3 from, Vector3 to, float radius, HitZone zone, float multiplier, Health health)
        {
            var go = new GameObject(name);
            go.transform.SetParent(bone, false);
            go.transform.position = (from + to) * 0.5f;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, (to - from).normalized);
            var lossy = Mathf.Max(1e-4f, go.transform.lossyScale.x);
            var collider = go.AddComponent<CapsuleCollider>();
            collider.direction = 1;
            collider.radius = radius / lossy;
            collider.height = (Vector3.Distance(from, to) + radius * 2f) / lossy;
            var hitbox = go.AddComponent<Hitbox>();
            hitbox.Configure(zone, multiplier, health);
            m_Hitboxes.Add(collider);
        }

        void BuildBreath()
        {
            if (m_FireMaterial == null)
                return;

            m_Breath = FireEffects.CreateBreath(transform, m_FireMaterial, Mathf.Max(m_BreathRange, m_PerchBreathRange), m_BreathAngle);

            var lightObject = new GameObject("Lueur du souffle");
            lightObject.transform.SetParent(transform, false);
            m_BreathLight = lightObject.AddComponent<Light>();
            m_BreathLight.type = LightType.Point;
            m_BreathLight.color = new Color(1f, 0.5f, 0.15f);
            m_BreathLight.range = 16f;
            m_BreathLight.intensity = 6f;
            m_BreathLight.shadows = LightShadows.None;
            m_BreathLight.enabled = false;

            m_FireSource = gameObject.AddComponent<AudioSource>();
            m_FireSource.loop = true;
            m_FireSource.playOnAwake = false;
            m_FireSource.spatialBlend = 1f;
            m_FireSource.minDistance = 4f;
            m_FireSource.maxDistance = 40f;
            m_FireSource.volume = 1f;
            m_FireSource.pitch = 0.75f;
            m_FireSource.outputAudioMixerGroup = Sfx.Output;
        }

        // Deux yeux rouges qui brillent (le bloom les fait rayonner), et une petite lueur rouge sur le museau.
        void BuildEyes()
        {
            if (m_EyeMaterial == null || m_Head == null)
                return;

            var middle = Vector3.zero;
            var count = 0;
            foreach (var boneName in new[] { "Eye_L", "Eye_R" })
            {
                var bone = FindDeep(transform, boneName);
                if (bone == null)
                    continue;

                var side = Mathf.Sign(transform.InverseTransformPoint(bone.position).x);
                var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.name = "Œil rouge";
                DestroyImmediate(eye.GetComponent<Collider>());
                eye.transform.SetParent(bone, false);
                eye.transform.position = bone.position + transform.right * (m_EyeOffset.x * side) + transform.up * m_EyeOffset.y + transform.forward * m_EyeOffset.z;
                eye.transform.localScale = Vector3.one * (m_EyeRadius * 2f) / Mathf.Max(1e-6f, bone.lossyScale.x);
                var renderer = eye.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = m_EyeMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                middle += eye.transform.position;
                count++;
            }

            if (count == 0 || m_EyeLightIntensity <= 0f)
                return;

            var lightObject = new GameObject("Lueur des yeux");
            lightObject.transform.SetParent(m_Head, false);
            lightObject.transform.position = middle / count + transform.forward * 0.3f;
            m_EyeLight = lightObject.AddComponent<Light>();
            m_EyeLight.type = LightType.Point;
            m_EyeLight.color = new Color(1f, 0.12f, 0.05f);
            m_EyeLight.range = 2.5f;
            m_EyeLight.intensity = m_EyeLightIntensity;
            m_EyeLight.shadows = LightShadows.None;
        }

        // La lueur de la gueule pendant la charge du souffle : une petite boule de feu et sa lumière, éteintes au repos.
        void BuildMouthGlow()
        {
            if (m_FireballMaterial != null)
            {
                var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                glow.name = "Lueur de la gueule";
                DestroyImmediate(glow.GetComponent<Collider>());
                glow.transform.SetParent(transform, false);
                var renderer = glow.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = m_FireballMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                m_MouthGlow = glow.transform;
                glow.SetActive(false);
            }

            var lightObject = new GameObject("Lumière de la gueule");
            lightObject.transform.SetParent(transform, false);
            m_MouthLight = lightObject.AddComponent<Light>();
            m_MouthLight.type = LightType.Point;
            m_MouthLight.color = new Color(1f, 0.62f, 0.25f);
            m_MouthLight.shadows = LightShadows.None;
            m_MouthLight.enabled = false;
        }
    }
}
