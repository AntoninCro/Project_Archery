using System;
using System.Collections.Generic;
using Archery.Bows;
using Archery.Core;
using Archery.Enemies;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Archery.Portal
{
    /// <summary>
    /// Le monstre corrompu de la dimension (GDD, section 23) : la copie « cassée » d'un ennemi du jeu (tranches décalées,
    /// couleurs séparées, saccades), seul ennemi de l'épreuve. Il apparaît loin, dans le champ de vision du joueur, caché
    /// derrière un pilier ; puis il court très vite jusqu'à un autre pilier, toujours dans le champ de vision du joueur
    /// et de plus en plus près de lui ; tout près, il fonce sur lui. S'il le touche, le joueur est expulsé. Une flèche
    /// (tirée, ou au corps à corps) le tue : il éclate en pixels, et le suivant prend l'apparence d'un autre ennemi
    /// (<see cref="StarDimension"/>).
    /// </summary>
    /// <remarks>
    /// Sa course bugue : avec l'animation de course de l'ennemi, il court parfois à l'envers, de côté, penché, la tête
    /// en bas ou en tournant sur lui-même, change de pose par à-coups et saute quelques images. Caché, il grésille : on
    /// l'entend avant de le voir.
    /// </remarks>
    public class CorruptedMonster : MonoBehaviour, IArrowHitHandler, IArrowAbsorber
    {
        static readonly int k_GlitchId = Shader.PropertyToID("_Glitch");
        static readonly int k_BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_SpeedId = Animator.StringToHash("Speed");

        const float k_Height = 2.1f;
        const float k_Radius = 0.45f;
        const float k_TouchDistance = 0.8f;

        // Plus près que ça et sans pilier en vue plus proche : il fonce.
        const float k_LungeDistance = 8f;

        enum State
        {
            Hiding,
            Running,
            Lunging,
            Dead,
        }

        // Les façons de courir de travers.
        enum Pose
        {
            Normal,
            Backwards,
            Sideways,
            Tilted,
            Spinning,
            UpsideDown,
        }

        StarDimension m_Dimension;
        readonly List<Material> m_Materials = new List<Material>();
        Transform m_Visual;
        Vector3 m_VisualHome;
        Quaternion m_VisualHomeRotation = Quaternion.identity;
        Animator m_Animator;
        bool m_HasSpeed;
        AudioSource m_Audio;
        State m_State;
        float m_StateTime;
        float m_HideTime;
        float m_RunSpeed;
        float m_LungeSpeed;
        float m_CrackleTimer;
        float m_RetryTimer;
        Transform m_Post;
        Transform m_NextPost;
        Pose m_Pose;
        Quaternion m_PoseRotation = Quaternion.identity;
        float m_PoseTimer;
        float m_SpinSign = 1f;

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
            monster.m_HideTime = 1.5f / speed;
            monster.m_RunSpeed = 9f * speed;
            monster.m_LungeSpeed = 7f * speed;
            monster.BuildVisual(model, glitch);

            var collider = go.AddComponent<CapsuleCollider>();
            collider.center = Vector3.up * (k_Height * 0.5f);
            collider.height = k_Height;
            collider.radius = k_Radius;

            monster.m_Audio = go.AddComponent<AudioSource>();
            monster.m_Audio.spatialBlend = 1f;
            monster.m_Audio.minDistance = 2f;
            monster.m_Audio.maxDistance = 40f;
            monster.m_Audio.rolloffMode = AudioRolloffMode.Linear;
            monster.m_Audio.playOnAwake = false;
            monster.m_Audio.outputAudioMixerGroup = Sfx.Output;

            monster.m_Post = post;
            go.transform.position = dimension.HidePoint(post);
            monster.FacePlayer();
            monster.Enter(State.Hiding);
            dimension.Pixels(go.transform.position + Vector3.up * 1f, 30, 1.2f);
            return monster;
        }

        void OnDestroy()
        {
            foreach (var material in m_Materials)
            {
                if (material != null)
                    Destroy(material);
            }
        }

        /// <summary>Le point visé pour éviter de mettre un panneau devant lui.</summary>
        public Vector3 Point => transform.position + Vector3.up * (k_Height * 0.6f);

        public bool IsAlive => m_State != State.Dead;

        void Update()
        {
            if (m_State == State.Dead || m_Dimension == null || m_Dimension.Head == null)
                return;

            var deltaTime = Time.deltaTime;
            m_StateTime += deltaTime;
            switch (m_State)
            {
                case State.Hiding:
                    UpdateHiding(deltaTime);
                    break;
                case State.Running:
                    UpdateRunning(deltaTime);
                    break;
                case State.Lunging:
                    UpdateLunging(deltaTime);
                    break;
            }
        }

        // Caché derrière son pilier : il suit le côté opposé au joueur, et grésille de temps en temps. Puis il court vers
        // un pilier plus proche, dans le champ de vision du joueur ; s'il n'y en a pas et qu'il est près, il fonce.
        void UpdateHiding(float deltaTime)
        {
            if (m_Post != null)
                transform.position = Vector3.Lerp(transform.position, m_Dimension.HidePoint(m_Post), 1f - Mathf.Exp(-deltaTime * 6f));
            FacePlayer();

            m_CrackleTimer -= deltaTime;
            if (m_CrackleTimer <= 0f)
            {
                m_CrackleTimer = Random.Range(0.6f, 1f);
                Crackle(0.55f);
            }

            if (m_StateTime < m_HideTime || (m_RetryTimer -= deltaTime) > 0f)
                return;

            // Aucun pilier en vue depuis 2 s : il en prend un au bord du champ de vision.
            var distance = Vector3.Distance(Flat(transform.position), PlayerFlat());
            m_NextPost = m_Dimension.NextPost(m_Post, distance, m_StateTime > m_HideTime + 2f);
            if (m_NextPost != null)
                Enter(State.Running);
            else if (distance < k_LungeDistance)
                Enter(State.Lunging);
            else
                m_RetryTimer = 0.3f;
        }

        // Il court d'un pilier à l'autre, très vite et de travers.
        void UpdateRunning(float deltaTime)
        {
            if (m_NextPost == null)
            {
                Enter(State.Hiding);
                return;
            }

            var destination = m_Dimension.HidePoint(m_NextPost);
            if (MoveTowards(destination, m_RunSpeed * deltaTime))
            {
                m_Post = m_NextPost;
                m_NextPost = null;
                Crackle(0.8f);
                Enter(State.Hiding);
                return;
            }

            UpdatePose(deltaTime, destination);
        }

        // Tout près : il fonce droit sur le joueur ; s'il le touche, le joueur est expulsé.
        void UpdateLunging(float deltaTime)
        {
            var target = m_Dimension.Head.position;
            var toTarget = Flat(target) - Flat(transform.position);
            if (toTarget.magnitude <= k_TouchDistance + m_LungeSpeed * deltaTime)
            {
                m_Dimension.PlayerTouched();
                Enter(State.Dead);
                return;
            }

            MoveTowards(new Vector3(target.x, m_Dimension.FloorY, target.z), m_LungeSpeed * deltaTime);
            UpdatePose(deltaTime, target);
        }

        // Avance vers ce point, au sol ; de temps en temps, il saute quelques images (un bond, une traînée de blocs).
        // Vrai s'il est arrivé.
        bool MoveTowards(Vector3 destination, float step)
        {
            var position = transform.position;
            destination.y = m_Dimension.FloorY;
            var offset = destination - position;
            offset.y = 0f;
            if (offset.magnitude <= step + 0.05f)
            {
                transform.position = destination;
                return true;
            }

            var direction = offset.normalized;
            transform.position = position + direction * step;
            transform.rotation = Quaternion.LookRotation(direction);
            return false;
        }

        // La pose bugue par à-coups : toutes les 0,1 à 0,3 s, il change parfois de façon de courir, l'animation accélère
        // ou se fige, et il saute parfois en avant.
        void UpdatePose(float deltaTime, Vector3 destination)
        {
            m_PoseTimer -= deltaTime;
            if (m_PoseTimer <= 0f)
            {
                m_PoseTimer = Random.Range(0.1f, 0.3f);
                if (Random.value < 0.45f)
                    PickPose();
                if (m_Animator != null)
                    m_Animator.speed = Random.value < 0.12f ? 0f : Random.Range(1.4f, 2.6f);
                if (Random.value < 0.15f)
                    Skip(destination);
            }

            var rotation = m_PoseRotation;
            if (m_Pose == Pose.Spinning)
                rotation = Quaternion.Euler(0f, m_SpinSign * m_StateTime * 900f, 0f);

            // Un léger tremblement en plus, toujours.
            rotation *= Quaternion.Euler(Random.Range(-4f, 4f), Random.Range(-6f, 6f), Random.Range(-4f, 4f));
            SetVisual(rotation, m_Pose == Pose.UpsideDown ? k_Height : 0f);
        }

        void PickPose()
        {
            var roll = Random.value;
            m_Pose = roll < 0.25f ? Pose.Normal : roll < 0.45f ? Pose.Backwards : roll < 0.62f ? Pose.Sideways
                   : roll < 0.8f ? Pose.Tilted : roll < 0.92f ? Pose.Spinning : Pose.UpsideDown;
            m_SpinSign = Random.value < 0.5f ? -1f : 1f;
            m_PoseRotation = m_Pose switch
            {
                Pose.Backwards => Quaternion.Euler(0f, 180f, 0f),
                Pose.Sideways => Quaternion.Euler(0f, Random.value < 0.5f ? -90f : 90f, 0f),
                Pose.Tilted => Quaternion.Euler(Random.Range(-35f, 35f), Random.Range(-40f, 40f), Random.Range(25f, 60f) * m_SpinSign),
                Pose.UpsideDown => Quaternion.Euler(0f, Random.Range(0f, 360f), 180f),
                _ => Quaternion.identity,
            };
        }

        // Il saute quelques images : un bond en avant, vers sa destination.
        void Skip(Vector3 destination)
        {
            var from = transform.position;
            var offset = destination - from;
            offset.y = 0f;
            var jump = Mathf.Min(Random.Range(0.6f, 1.3f), offset.magnitude * 0.5f);
            if (jump < 0.2f)
                return;

            transform.position = from + offset.normalized * jump;
            m_Dimension.Streak(from + Vector3.up, transform.position + Vector3.up);
        }

        // La copie du modèle tournée autour des pieds (ou de la tête, quand il court la tête en bas).
        void SetVisual(Quaternion rotation, float lift)
        {
            if (m_Visual == null)
                return;
            m_Visual.localRotation = rotation * m_VisualHomeRotation;
            m_Visual.localPosition = rotation * m_VisualHome + Vector3.up * lift;
        }

        void Enter(State state)
        {
            m_State = state;
            m_StateTime = 0f;
            m_RetryTimer = 0f;
            var glitch = state == State.Hiding ? 0.35f : state == State.Lunging ? 1f : 0.8f;
            foreach (var material in m_Materials)
                material.SetFloat(k_GlitchId, glitch);

            var running = state == State.Running || state == State.Lunging;
            if (m_Animator != null)
            {
                if (m_HasSpeed)
                    m_Animator.SetFloat(k_SpeedId, running ? (state == State.Running ? m_RunSpeed : m_LungeSpeed) : 0f);
                m_Animator.speed = running ? 1.8f : 1f;
            }

            if (running)
            {
                PickPose();
                m_PoseTimer = Random.Range(0.1f, 0.3f);
                Crackle(state == State.Lunging ? 1f : 0.7f);
            }
            else
            {
                m_Pose = Pose.Normal;
                SetVisual(Quaternion.identity, 0f);
            }
        }

        void Crackle(float volume)
        {
            var clip = m_Dimension.RandomGlitchClip();
            if (clip == null || m_Audio == null)
                return;
            m_Audio.pitch = Random.Range(0.75f, 1.2f);
            m_Audio.PlayOneShot(clip, volume);
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
            var toPlayer = PlayerFlat() - Flat(transform.position);
            if (toPlayer.sqrMagnitude > 1e-4f)
                transform.rotation = Quaternion.LookRotation(toPlayer);
        }

        Vector3 PlayerFlat() => Flat(m_Dimension.Head.position);

        static Vector3 Flat(Vector3 point) => new Vector3(point.x, 0f, point.z);

        // ---------------------------------------------------------------- apparence

        // Une copie du modèle de l'ennemi, sans ses scripts ni ses collisions, ramenée à 2,1 m, avec le matériau bugué.
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
                foreach (var parameter in m_Animator.parameters)
                    m_HasSpeed |= parameter.type == AnimatorControllerParameterType.Float && parameter.nameHash == k_SpeedId;
            }

            m_Visual = copy.transform;
            var renderers = copy.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return;

            // À la bonne taille, les pieds au sol.
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);
            var scale = k_Height / Mathf.Max(0.2f, bounds.size.y);
            copy.transform.localScale *= scale;
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
                bounds.Encapsulate(renderer.bounds);
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

                    materials[i] = material;
                    m_Materials.Add(material);
                }

                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
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
