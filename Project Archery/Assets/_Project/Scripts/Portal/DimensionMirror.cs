using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Archery.Portal
{
    /// <summary>
    /// Le sol miroir de la dimension du portail : tout ce qui est sous les <see cref="m_Sources"/> (torii, galaxies,
    /// nébuleuses) est recopié à l'envers sous le plan de cet objet, et suit les modifications (dans l'éditeur comme en
    /// jeu). Un rendu désactivé (case du Mesh Renderer décochée) n'apparaît que dans le reflet : c'est le cas du haut des
    /// torii. Un objet désactivé n'apparaît nulle part.
    /// </summary>
    /// <remarks>
    /// Le sol, sombre et translucide, est au-dessus des copies : on les voit à travers, comme un reflet. Un vrai reflet
    /// en temps réel coûterait trop cher dans le casque. Les copies sont cachées et ne sont pas enregistrées dans la scène.
    /// </remarks>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class DimensionMirror : MonoBehaviour
    {
        const string k_RootName = "Reflet (copies)";

        [Tooltip("Ce qui se reflète dans le sol, avec tous ses enfants.")]
        [SerializeField]
        Transform[] m_Sources = new Transform[0];

        class Copy
        {
            public GameObject GameObject;
            public MeshFilter Filter;
            public MeshRenderer Renderer;
        }

        static readonly List<MeshRenderer> s_Found = new List<MeshRenderer>();
        readonly List<MeshRenderer> m_Reflected = new List<MeshRenderer>();
        readonly Dictionary<MeshRenderer, Copy> m_Copies = new Dictionary<MeshRenderer, Copy>();
        readonly List<MeshRenderer> m_Stale = new List<MeshRenderer>();
        readonly HashSet<MeshRenderer> m_Current = new HashSet<MeshRenderer>();
        Transform m_Root;
        MaterialPropertyBlock m_Block;
        float m_GatherTimer;

        void OnDisable() => Clear();

        void LateUpdate() => Sync();

        void Sync()
        {
            EnsureRoot();

            // Dans l'éditeur, tout est recopié à chaque modification. En jeu, la liste des objets reflétés est relue deux fois
            // par seconde, leurs maillages et matériaux ne sont copiés qu'à la création de la copie, et seul ce qui a bougé
            // (un torii qui bugue, avec son flash) est replacé : il y a des centaines de torii.
            var playing = Application.isPlaying;
            m_GatherTimer -= Time.unscaledDeltaTime;
            if (!playing || m_GatherTimer <= 0f || m_Reflected.Count == 0)
            {
                m_GatherTimer = 0.5f;
                Gather();
            }

            m_Block ??= new MaterialPropertyBlock();
            foreach (var source in m_Reflected)
            {
                if (source == null)
                    continue;
                var created = false;
                if (!m_Copies.TryGetValue(source, out var copy) || copy.GameObject == null)
                {
                    copy = Create(source);
                    m_Copies[source] = copy;
                    created = true;
                }

                var full = created || !playing;
                var sourceTransform = source.transform;
                if (!full && !sourceTransform.hasChanged)
                    continue;

                Place(source, copy, full);
                sourceTransform.hasChanged = false;
            }
        }

        void Gather()
        {
            m_Reflected.Clear();
            foreach (var root in m_Sources)
            {
                if (root == null)
                    continue;
                root.GetComponentsInChildren(true, s_Found);
                foreach (var renderer in s_Found)
                {
                    if (renderer.gameObject.activeInHierarchy && renderer.transform != m_Root && !renderer.transform.IsChildOf(transform))
                        m_Reflected.Add(renderer);
                }
            }

            // Les copies d'objets supprimés, désactivés ou retirés des sources disparaissent.
            m_Current.Clear();
            m_Current.UnionWith(m_Reflected);
            m_Stale.Clear();
            foreach (var pair in m_Copies)
            {
                if (pair.Key == null || !m_Current.Contains(pair.Key))
                    m_Stale.Add(pair.Key);
            }

            foreach (var key in m_Stale)
            {
                Kill(m_Copies[key].GameObject);
                m_Copies.Remove(key);
            }
        }

        Copy Create(MeshRenderer source)
        {
            var go = new GameObject(source.name) { hideFlags = HideFlags.HideAndDontSave };
            go.transform.SetParent(m_Root, false);
            var copy = new Copy { GameObject = go, Filter = go.AddComponent<MeshFilter>(), Renderer = go.AddComponent<MeshRenderer>() };
            copy.Renderer.shadowCastingMode = ShadowCastingMode.Off;
            copy.Renderer.receiveShadows = false;
            copy.Renderer.lightProbeUsage = LightProbeUsage.Off;
            copy.Renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return copy;
        }

        // La copie à la même place que l'objet, par rapport au plan du miroir ; la racine des copies, retournée, fait le reste.
        // Les maillages et matériaux (qui allouent des tableaux à la lecture) ne sont recopiés que si full est vrai.
        void Place(MeshRenderer source, Copy copy, bool full)
        {
            var sourceTransform = source.transform;
            var copyTransform = copy.GameObject.transform;
            copyTransform.localPosition = transform.InverseTransformPoint(sourceTransform.position);
            copyTransform.localRotation = Quaternion.Inverse(transform.rotation) * sourceTransform.rotation;
            var scale = sourceTransform.lossyScale;
            var own = transform.lossyScale;
            copyTransform.localScale = new Vector3(scale.x / own.x, scale.y / own.y, scale.z / own.z);

            if (full)
            {
                var filter = source.GetComponent<MeshFilter>();
                var mesh = filter != null ? filter.sharedMesh : null;
                if (copy.Filter.sharedMesh != mesh)
                    copy.Filter.sharedMesh = mesh;
                var materials = source.sharedMaterials;
                if (!SameMaterials(copy.Renderer.sharedMaterials, materials))
                    copy.Renderer.sharedMaterials = materials;
                copy.Renderer.sortingOrder = source.sortingOrder;
                copy.GameObject.layer = source.gameObject.layer;
            }

            // Les effets posés sur l'objet (le torii qui bugue vire au cyan) passent aussi dans le reflet.
            if (source.HasPropertyBlock())
            {
                source.GetPropertyBlock(m_Block);
                copy.Renderer.SetPropertyBlock(m_Block);
            }
            else if (copy.Renderer.HasPropertyBlock())
            {
                copy.Renderer.SetPropertyBlock(null);
            }
        }

        static bool SameMaterials(Material[] a, Material[] b)
        {
            if (a.Length != b.Length)
                return false;
            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }

        void EnsureRoot()
        {
            if (m_Root != null)
                return;

            // Une racine restée d'avant (rechargement des scripts) : on repart de zéro.
            var old = transform.Find(k_RootName);
            if (old != null)
                Kill(old.gameObject);
            m_Copies.Clear();

            var root = new GameObject(k_RootName) { hideFlags = HideFlags.HideAndDontSave };
            m_Root = root.transform;
            m_Root.SetParent(transform, false);
            m_Root.localScale = new Vector3(1f, -1f, 1f);
        }

        void Clear()
        {
            if (m_Root != null)
                Kill(m_Root.gameObject);
            m_Root = null;
            m_Copies.Clear();
            m_Reflected.Clear();
        }

        static void Kill(GameObject go)
        {
            if (go == null)
                return;
            if (Application.isPlaying)
                Destroy(go);
            else
                DestroyImmediate(go);
        }
    }
}
