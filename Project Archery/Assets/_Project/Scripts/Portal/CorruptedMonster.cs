using System;
using System.Collections.Generic;
using Archery.Bows;
using Archery.Core;
using Archery.Enemies;
using Archery.FinalBoss;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Archery.Portal
{
    /// <summary>
    /// Un monstre corrompu de la dimension (GDD, section 23) : la silhouette sombre et étirée d'un ennemi du jeu, au contour
    /// qui grésille, aux deux yeux rouges. Ils sont plusieurs (<see cref="StarDimension"/>). Il apparaît loin, dans le
    /// champ de vision du joueur, caché derrière un pilier, puis avance de pilier en pilier, de plus en plus près, en
    /// passant hors de la vue du joueur (sur les côtés, dans son dos) ; tout près, il fonce. S'il touche le joueur, celui-ci est expulsé. Une flèche (tirée, ou au
    /// corps à corps) le tue : il éclate en pixels, et le suivant prend l'apparence d'un autre ennemi.
    /// </summary>
    /// <remarks>
    /// <b>Il ne bouge que quand on ne le regarde pas</b> : dans l'axe du regard (25°) et sans pilier entre lui et le
    /// joueur, il se fige, comme une statue. Le regard détourné, il avance par saccades, comme une vidéo qui saute des
    /// images, figé dans une pose qui change à chaque saut, toujours tourné vers le joueur. On le voit bouger du coin de
    /// l'œil ; quand on le regarde, il est déjà plus près. Caché, il penche parfois la tête hors du pilier.
    /// <para>On l'entend venir, surtout dans son dos : un grésillement continu, de plus en plus fort à mesure qu'il approche
    /// (doux caché, fort quand il avance, à fond quand il fonce, presque éteint quand on le regarde), un pas lourd à chaque
    /// saut, et un grondement quand il fonce. Les sons baissent avec la distance bien plus vite qu'avant (atténuation
    /// logarithmique) : loin, un murmure ; tout près, très fort.</para>
    /// </remarks>
    public class CorruptedMonster : MonoBehaviour, IArrowHitHandler, IArrowAbsorber
    {
        static readonly int k_GlitchId = Shader.PropertyToID("_Glitch");
        static readonly int k_SilhouetteId = Shader.PropertyToID("_Silhouette");
        static readonly int k_BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_SpeedId = Animator.StringToHash("Speed");
        static readonly RaycastHit[] s_Hits = new RaycastHit[16];

        // La taille de l'ennemi d'origine, ramenée à 2,1 m, puis étirée : plus haut et plus fin.
        const float k_BaseHeight = 2.1f;
        const float k_Stretch = 1.3f;
        const float k_Thin = 0.85f;
        const float k_Radius = 0.45f;
        const float k_TouchDistance = 0.8f;

        // Plus près que ça et sans pilier en vue plus proche : il fonce.
        const float k_LungeDistance = 8f;

        // Le regard du joueur : un monstre à moins de ce demi-angle de l'axe de la tête, et visible, est « regardé ».
        const float k_GazeCone = 25f;

        // Caché, il penche la tête hors du pilier : de côté (m) et en se penchant (°).
        const float k_PeekShift = 0.45f;
        const float k_PeekLean = 20f;

        enum State
        {
            Hiding,
            Moving,
            Lunging,
            Dead,
        }

        StarDimension m_Dimension;
        readonly List<Material> m_Materials = new List<Material>();
        Transform m_Visual;
        Vector3 m_VisualHome;
        Quaternion m_VisualHomeRotation = Quaternion.identity;
        Animator m_Animator;
        bool m_HasSpeed;
        bool m_PoseReady;
        int m_PoseState;
        AudioSource m_Audio;
        AudioSource m_Steps;
        AudioSource m_Static;
        Collider m_Collider;
        Transform m_HeadBone;
        Transform m_Eyes;
        float m_EyeTimer;
        State m_State;
        float m_StateTime;
        float m_HideTime;
        float m_Speed;
        float m_LungeSpeed;
        float m_CrackleTimer;
        float m_RetryTimer;
        float m_StepTimer;
        float m_Lean;
        float m_LeanSide = 1f;
        bool m_WillPeek;
        bool m_Watched;
        float m_GlitchShown = -1f;
        Transform m_Post;
        Transform m_NextPost;

        /// <summary>
        /// Fabrique un monstre à l'apparence de <paramref name="model"/> (ses scripts, collisions et sons retirés), caché
        /// derrière <paramref name="post"/>. <paramref name="speed"/> : 1 en Normal, plus en Difficile et Impossible.
        /// </summary>
        public static CorruptedMonster Spawn(StarDimension dimension, Enemy model, Transform post, Shader glitch, float speed)
        {
            var go = new GameObject("Monstre corrompu");
            go.transform.SetParent(dimension.transform, true);
            var monster = go.AddComponent<CorruptedMonster>();
            monster.m_Dimension = dimension;
            speed = Mathf.Max(0.5f, speed);
            monster.m_HideTime = 0.8f / speed;
            monster.m_Speed = 10f * speed;
            monster.m_LungeSpeed = 8f * speed;
            monster.BuildVisual(model, glitch);
            monster.BuildEyes(dimension.GlowMaterial);

            var collider = go.AddComponent<CapsuleCollider>();
            collider.center = Vector3.up * (monster.Height * 0.5f);
            collider.height = monster.Height;
            collider.radius = k_Radius;
            monster.m_Collider = collider;

            monster.m_Audio = monster.AddSource(3f);
            monster.m_Steps = monster.AddSource(3f);
            monster.m_Static = monster.AddSource(2f);
            var staticClip = dimension.StaticClip;
            if (staticClip != null)
            {
                monster.m_Static.clip = staticClip;
                monster.m_Static.loop = true;
                monster.m_Static.volume = 0f;
                monster.m_Static.pitch = Random.Range(0.85f, 1.15f);
                monster.m_Static.time = Random.Range(0f, staticClip.length * 0.9f);
                monster.m_Static.Play();
            }

            monster.m_Post = post;
            dimension.Claim(post);
            go.transform.position = dimension.HidePoint(post);
            monster.FacePlayer();
            monster.Enter(State.Hiding);
            dimension.Pixels(go.transform.position + Vector3.up * 1f, 30, 1.2f);
            return monster;
        }

        void OnDestroy()
        {
            if (m_Dimension != null)
            {
                m_Dimension.Release(m_Post);
                m_Dimension.Release(m_NextPost);
            }

            foreach (var material in m_Materials)
            {
                if (material != null)
                    Destroy(material);
            }

            if (m_Eyes != null)
            {
                var filter = m_Eyes.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                    Destroy(filter.sharedMesh);
            }
        }

        /// <summary>Sa hauteur, une fois étiré.</summary>
        public float Height => k_BaseHeight * k_Stretch;

        /// <summary>Le point visé pour éviter de mettre un panneau devant lui.</summary>
        public Vector3 Point => transform.position + Vector3.up * (Height * 0.6f);

        public bool IsAlive => m_State != State.Dead;

        void Update()
        {
            if (m_State == State.Dead || m_Dimension == null || m_Dimension.Head == null)
                return;

            var deltaTime = Time.deltaTime;
            m_StateTime += deltaTime;
            EnsurePose();
            m_Watched = IsSeen();
            UpdateGlitch();
            UpdateStatic(deltaTime);
            switch (m_State)
            {
                case State.Hiding:
                    UpdateHiding(deltaTime);
                    break;
                case State.Moving:
                    UpdateMoving(deltaTime);
                    break;
                case State.Lunging:
                    UpdateLunging(deltaTime);
                    break;
            }
        }

        void LateUpdate() => UpdateEyes(Time.deltaTime);

        // ---------------------------------------------------------------- le regard du joueur

        // Regardé : dans l'axe de la tête du joueur, et rien (un pilier) entre eux, pour le milieu du corps ou le haut.
        bool IsSeen()
        {
            var head = m_Dimension.Head;
            return Seen(head, Point) || Seen(head, transform.position + Vector3.up * (Height * 0.88f));
        }

        bool Seen(Transform head, Vector3 target)
        {
            var offset = target - head.position;
            var distance = offset.magnitude;
            if (distance < 0.05f || Vector3.Angle(head.forward, offset) > k_GazeCone)
                return false;

            var count = Physics.RaycastNonAlloc(head.position, offset / distance, s_Hits, distance, Physics.DefaultRaycastLayers,
                                                QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var hit = s_Hits[i];
                if (hit.collider == null || hit.collider == m_Collider || ArrowIgnore.IsIgnored(hit.collider) ||
                    hit.collider.GetComponentInParent<Arrow>() != null || hit.collider.GetComponentInParent<CorruptedMonster>() != null)
                    continue;
                if (hit.distance < distance - 0.3f)
                    return false;
            }

            return true;
        }

        // ---------------------------------------------------------------- les états

        // Caché derrière son pilier, du côté opposé au joueur ; il grésille, et penche parfois la tête dehors. Puis, dès
        // qu'on ne le regarde plus, il part vers un pilier plus proche, hors de la vue du joueur ; s'il n'y en a pas et
        // qu'il est près, il fonce.
        void UpdateHiding(float deltaTime)
        {
            m_CrackleTimer -= deltaTime;
            if (m_CrackleTimer <= 0f)
            {
                m_CrackleTimer = Random.Range(0.6f, 1.2f);
                Crackle(0.5f, Random.Range(0.6f, 1f));
            }

            if (m_Watched)
                return;

            if (m_WillPeek && m_StateTime > m_HideTime * 0.4f)
                m_Lean = Mathf.MoveTowards(m_Lean, 1f, deltaTime * 2.5f);
            FacePlayer();
            if (m_Post != null)
            {
                var side = Vector3.Cross(Vector3.up, transform.forward).normalized;
                var target = m_Dimension.HidePoint(m_Post) + side * (m_LeanSide * k_PeekShift * m_Lean);
                transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-deltaTime * 6f));
            }

            SetVisual(Quaternion.Euler(0f, 0f, -m_LeanSide * k_PeekLean * m_Lean));
            if (m_StateTime < m_HideTime || (m_RetryTimer -= deltaTime) > 0f)
                return;

            // Aucun pilier hors de la vue du joueur depuis 1,2 s : il en prend un qu'il voit du coin de l'œil.
            var distance = FlatDistanceToPlayer();
            m_NextPost = m_Dimension.NextPost(m_Post, distance, m_StateTime > m_HideTime + 1.2f);
            if (m_NextPost != null)
            {
                m_Dimension.Claim(m_NextPost);
                Enter(State.Moving);
            }
            else if (distance < k_LungeDistance)
            {
                Enter(State.Lunging);
            }
            else
            {
                m_RetryTimer = 0.3f;
            }
        }

        // Vers le pilier suivant, par saccades, seulement quand on ne le regarde pas.
        void UpdateMoving(float deltaTime)
        {
            if (m_NextPost == null)
            {
                Enter(State.Hiding);
                return;
            }

            if (!Step(deltaTime, m_Dimension.HidePoint(m_NextPost), m_Speed))
                return;

            m_Dimension.Release(m_Post);
            m_Post = m_NextPost;
            m_NextPost = null;
            Enter(State.Hiding);
        }

        // Tout près : il fonce sur le joueur, par saccades, toujours seulement quand on ne le regarde pas.
        void UpdateLunging(float deltaTime)
        {
            var head = m_Dimension.Head.position;
            Step(deltaTime, new Vector3(head.x, m_Dimension.FloorY, head.z), m_LungeSpeed);
            if (FlatDistanceToPlayer() <= k_TouchDistance)
            {
                m_Dimension.PlayerTouched();
                Enter(State.Dead);
            }
        }

        // Un saut de saccade vers ce point, toutes les 0,08 à 0,16 s, quand on ne le regarde pas : il avance d'un coup,
        // dans une nouvelle pose figée, toujours tourné vers le joueur. Vrai s'il est arrivé.
        bool Step(float deltaTime, Vector3 destination, float speed)
        {
            if (m_Watched)
                return false;

            m_StepTimer -= deltaTime;
            if (m_StepTimer > 0f)
                return false;

            var interval = Random.Range(0.08f, 0.16f);
            m_StepTimer = interval;
            var position = transform.position;
            destination.y = m_Dimension.FloorY;
            var offset = destination - position;
            offset.y = 0f;
            var step = speed * interval * Random.Range(0.9f, 1.4f);
            var arrived = offset.magnitude <= step + 0.05f;
            transform.position = arrived ? destination : position + offset.normalized * step;

            FacePlayer();
            RandomPose();
            SetVisual(Quaternion.Euler(Random.Range(-4f, 4f), Random.Range(-6f, 6f), Random.Range(-4f, 4f)));
            StepSound();
            return arrived;
        }

        void Enter(State state)
        {
            m_State = state;
            m_StateTime = 0f;
            m_RetryTimer = 0f;
            m_StepTimer = 0f;
            m_Lean = 0f;
            m_WillPeek = state == State.Hiding && Random.value < 0.6f;
            m_LeanSide = Random.value < 0.5f ? -1f : 1f;
            if (state == State.Hiding)
                SetVisual(Quaternion.identity);
            // Il fonce : un grondement grave et fort, pour qu'on sache qu'il arrive.
            else if (state == State.Lunging)
                Crackle(1f, 0.45f);
        }

        // Le bug du corps : léger caché, plus fort quand il avance ou fonce, presque calme quand on le regarde (une statue).
        void UpdateGlitch()
        {
            var glitch = m_Watched ? 0.25f : m_State == State.Hiding ? 0.4f : m_State == State.Lunging ? 1f : 0.8f;
            if (Mathf.Approximately(glitch, m_GlitchShown))
                return;

            m_GlitchShown = glitch;
            foreach (var material in m_Materials)
                material.SetFloat(k_GlitchId, glitch);
        }

        void Crackle(float volume, float pitch)
        {
            var clip = m_Dimension.RandomGlitchClip();
            if (clip == null || m_Audio == null)
                return;
            m_Audio.pitch = pitch;
            m_Audio.PlayOneShot(clip, volume);
        }

        // ---------------------------------------------------------------- les sons

        // Une source de son sur le monstre. Atténuation logarithmique : pleine jusqu'à minDistance, puis moitié moins à
        // chaque fois que la distance double (3 m : 1, 6 m : 0,5, 12 m : 0,25, 24 m : 0,13) ; on sent bien qu'il approche.
        // Pas d'effet Doppler : il saute d'un coup, ce qui ferait siffler le son.
        AudioSource AddSource(float minDistance)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = minDistance;
            source.maxDistance = 60f;
            source.dopplerLevel = 0f;
            source.priority = 40;
            source.playOnAwake = false;
            source.outputAudioMixerGroup = Sfx.Output;
            return source;
        }

        // Le grésillement continu : doux caché, fort quand il avance, à fond quand il fonce ; regardé, il retient son
        // souffle (presque plus rien). Coupé vite, rallumé un peu moins vite.
        void UpdateStatic(float deltaTime)
        {
            if (m_Static == null)
                return;

            var target = m_State == State.Dead ? 0f
                       : m_Watched ? 0.05f
                       : m_State == State.Hiding ? 0.35f
                       : m_State == State.Lunging ? 1f : 0.8f;
            var speed = target < m_Static.volume ? 6f : 3f;
            m_Static.volume = Mathf.MoveTowards(m_Static.volume, target, deltaTime * speed);
        }

        // Un pas lourd à chaque saut, plus grave que nature. Chaque pas coupe le précédent : un piétinement haché, comme
        // une vidéo qui saute des images.
        void StepSound()
        {
            var clip = m_Dimension.RandomStepClip();
            if (clip == null || m_Steps == null)
                return;
            m_Steps.Stop();
            m_Steps.clip = clip;
            m_Steps.pitch = Random.Range(0.55f, 0.75f);
            m_Steps.volume = Random.Range(0.8f, 1f);
            m_Steps.Play();
        }

        public bool OnArrowHit(in ArrowHit hit)
        {
            if (m_State == State.Dead)
                return false;

            Enter(State.Dead);
            m_Dimension.MonsterKilled(this);
            return true;
        }

        void FacePlayer()
        {
            var toPlayer = Flat(m_Dimension.Head.position) - Flat(transform.position);
            if (toPlayer.sqrMagnitude > 1e-4f)
                transform.rotation = Quaternion.LookRotation(toPlayer);
        }

        float FlatDistanceToPlayer() => Vector3.Distance(Flat(transform.position), Flat(m_Dimension.Head.position));

        static Vector3 Flat(Vector3 point) => new Vector3(point.x, 0f, point.z);

        // ---------------------------------------------------------------- les poses

        // La première fois : l'animation de course de l'ennemi, mise en route puis figée. Ensuite, chaque saut la fige à
        // un autre instant au hasard : une pose de course arrêtée en plein mouvement, qui change par à-coups.
        void EnsurePose()
        {
            if (m_PoseReady || m_Animator == null)
                return;

            m_PoseReady = true;
            m_Animator.speed = 1f;
            if (m_HasSpeed)
                m_Animator.SetFloat(k_SpeedId, 6f);
            for (var i = 0; i < 4; i++)
                m_Animator.Update(0.25f);
            m_PoseState = m_Animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
            m_Animator.speed = 0f;
            RandomPose();
        }

        void RandomPose()
        {
            if (m_Animator == null || m_PoseState == 0)
                return;
            m_Animator.Play(m_PoseState, 0, Random.value);
            m_Animator.Update(0f);
        }

        // La copie du modèle tournée autour des pieds.
        void SetVisual(Quaternion rotation)
        {
            if (m_Visual == null)
                return;
            m_Visual.localRotation = rotation * m_VisualHomeRotation;
            m_Visual.localPosition = rotation * m_VisualHome;
        }

        // ---------------------------------------------------------------- les yeux

        // Deux points rouges qui brillent, au niveau de la tête, toujours tournés vers le joueur.
        void BuildEyes(Material material)
        {
            if (material == null)
                return;

            var builder = new MeshBuilder();
            var color = new Color(3f, 0.22f, 0.16f, 1f);
            const int sides = 10;
            const float radius = 0.035f;
            foreach (var x in new[] { -0.075f, 0.075f })
            {
                var center = new Vector3(x, 0f, 0f);
                for (var i = 0; i < sides; i++)
                {
                    var a0 = i * Mathf.PI * 2f / sides;
                    var a1 = (i + 1) * Mathf.PI * 2f / sides;
                    builder.Triangle(center, center + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0f) * radius,
                                     center + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f) * radius, color);
                }
            }

            var eyes = new GameObject("Yeux");
            eyes.transform.SetParent(transform, false);
            eyes.AddComponent<MeshFilter>().sharedMesh = builder.ToMesh("Yeux");
            var renderer = eyes.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_Eyes = eyes.transform;
            m_EyeTimer = Random.Range(2f, 4f);
        }

        // Devant la tête, vers le joueur ; ils clignent de temps en temps.
        void UpdateEyes(float deltaTime)
        {
            if (m_Eyes == null || m_Dimension == null || m_Dimension.Head == null || m_State == State.Dead)
                return;

            var headPoint = m_HeadBone != null ? m_HeadBone.position : transform.position + Vector3.up * (Height * 0.88f);
            var toPlayer = m_Dimension.Head.position - headPoint;
            toPlayer.y = 0f;
            toPlayer = toPlayer.sqrMagnitude > 1e-4f ? toPlayer.normalized : transform.forward;
            m_Eyes.SetPositionAndRotation(headPoint + toPlayer * 0.25f, Quaternion.LookRotation(-toPlayer, Vector3.up));

            m_EyeTimer -= deltaTime;
            if (m_EyeTimer <= 0f)
            {
                var open = !m_Eyes.gameObject.activeSelf;
                m_Eyes.gameObject.SetActive(open);
                m_EyeTimer = open ? Random.Range(2f, 4f) : 0.08f;
            }
        }

        // ---------------------------------------------------------------- apparence

        // Une copie du modèle de l'ennemi, sans ses scripts ni ses collisions, ramenée à 2,1 m puis étirée (plus haute,
        // plus fine), en silhouette sombre au contour qui grésille.
        void BuildVisual(Enemy model, Shader glitch)
        {
            if (model == null)
                return;

            var holder = new GameObject("Copie");
            holder.SetActive(false);
            var copy = Object.Instantiate(model.gameObject, holder.transform);
            Strip(copy);
            copy.name = "Apparence : " + model.name;
            copy.transform.SetParent(transform, false);
            copy.transform.localPosition = Vector3.zero;
            copy.transform.localRotation = Quaternion.identity;
            Destroy(holder);

            m_Animator = copy.GetComponentInChildren<Animator>();
            if (m_Animator != null)
            {
                m_Animator.applyRootMotion = false;
                m_Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                m_Animator.speed = 0f;
                foreach (var parameter in m_Animator.parameters)
                    m_HasSpeed |= parameter.type == AnimatorControllerParameterType.Float && parameter.nameHash == k_SpeedId;
            }

            m_HeadBone = FindHead(copy.transform);
            m_Visual = copy.transform;
            var renderers = copy.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;

            // À la bonne taille, étiré, les pieds au sol.
            var scale = k_BaseHeight / Mathf.Max(0.2f, Bounds(renderers).size.y);
            copy.transform.localScale = Vector3.Scale(copy.transform.localScale * scale, new Vector3(k_Thin, k_Stretch, k_Thin));
            var bounds = Bounds(renderers);
            copy.transform.position += new Vector3(transform.position.x - bounds.center.x, transform.position.y - bounds.min.y,
                                                   transform.position.z - bounds.center.z);
            m_VisualHome = copy.transform.localPosition;
            m_VisualHomeRotation = copy.transform.localRotation;

            if (glitch == null)
                return;

            foreach (var renderer in renderers)
            {
                var shared = renderer.sharedMaterials;
                var materials = new Material[shared.Length];
                for (var i = 0; i < shared.Length; i++)
                {
                    var material = new Material(glitch) { name = "Corrompu" };
                    var source = shared[i];
                    if (source != null)
                    {
                        var texture = source.HasProperty(k_BaseMapId) ? source.GetTexture(k_BaseMapId) : source.mainTexture;
                        if (texture != null)
                            material.SetTexture(k_BaseMapId, texture);
                        var color = source.HasProperty(k_BaseColorId) ? source.GetColor(k_BaseColorId)
                                  : source.HasProperty("_Color") ? source.color : Color.white;
                        material.SetColor(k_BaseColorId, color);
                    }

                    material.SetFloat(k_SilhouetteId, 1f);
                    materials[i] = material;
                    m_Materials.Add(material);
                }

                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        static Bounds Bounds(Renderer[] renderers)
        {
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        // L'os de la tête, s'il en a un (son nom contient « head ») : les yeux le suivent.
        static Transform FindHead(Transform root)
        {
            foreach (var bone in root.GetComponentsInChildren<Transform>(true))
            {
                var name = bone.name.ToLowerInvariant();
                if (name.Contains("head") && !name.Contains("end") && !name.Contains("top"))
                    return bone;
            }

            return null;
        }

        // Retire ce qui ferait vivre l'ennemi (scripts, navigation, physique, sons), en commençant par ce dont rien ne
        // dépend (RequireComponent).
        static void Strip(GameObject copy)
        {
            for (var pass = 0; pass < 6; pass++)
            {
                var removed = false;
                foreach (var component in copy.GetComponentsInChildren<Component>(true))
                {
                    if (component == null || !Removable(component) || IsRequired(component))
                        continue;
                    Object.DestroyImmediate(component);
                    removed = true;
                }

                if (!removed)
                    break;
            }
        }

        static bool Removable(Component component) =>
            component is MonoBehaviour || component is Collider || component is Rigidbody || component is NavMeshAgent ||
            component is NavMeshObstacle || component is AudioSource || component is Joint;

        static bool IsRequired(Component component)
        {
            var type = component.GetType();
            foreach (var other in component.GetComponents<Component>())
            {
                if (other == null || other == component)
                    continue;
                foreach (RequireComponent require in other.GetType().GetCustomAttributes(typeof(RequireComponent), true))
                {
                    if (Requires(require.m_Type0, type) || Requires(require.m_Type1, type) || Requires(require.m_Type2, type))
                        return true;
                }
            }

            return false;
        }

        static bool Requires(Type required, Type type) => required != null && required.IsAssignableFrom(type);
    }
}
