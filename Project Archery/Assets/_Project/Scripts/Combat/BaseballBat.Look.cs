using Archery.Grenades;
using UnityEngine;

namespace Archery.Combat
{
    /// <summary>
    /// La batte steampunk qui vit (14 novembre, le joueur : « l'apparence ne me convient pas, il faudrait la retravailler pour
    /// qu'elle ait plus de steampunk, avec vraiment des conduits de vapeur qui bougent dans la batte ») : les pièces mobiles du
    /// modèle fait par <c>Editor/BatteBaker</c>, animées ici.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>« Conduits » : la vapeur qui coule dans les tubes de verre le long de la batte (une texture de vapeur qui défile) ; elle
    /// coule plus vite et plus blanche quand la grenade est prête, et jaillit à chaque coup.</item>
    /// <item>« Aiguille » : l'aiguille du manomètre montre la recharge de la grenade (la pression monte) ; prête, elle est dans le
    /// rouge et tremble.</item>
    /// <item>« Piston » : il pompe le long de la batte ; « Engrenage » : il tourne ; plus vite après un coup.</item>
    /// <item>« Foyer » : la fenêtre du foyer scintille, et flambe à chaque coup.</item>
    /// <item>« Fuite » : de petites bouffées de vapeur s'échappent de la vanne, de temps en temps.</item>
    /// </list>
    /// Une pièce absente du modèle est simplement ignorée.
    /// </remarks>
    public partial class BaseballBat
    {
        static readonly int k_BaseMapStId = Shader.PropertyToID("_BaseMap_ST");
        static readonly int k_BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int k_EmissionId = Shader.PropertyToID("_EmissionColor");
        static readonly Color k_FurnaceGlow = new Color(1f, 0.42f, 0.08f) * 3f;
        static readonly Color k_ConduitSteam = new Color(1f, 0.97f, 0.92f, 1f);

        // L'aiguille : de -135° (vide) à +115° (prête, dans la zone rouge, de 95° à 135° sur le cadran), elle tremble de 5° une fois
        // prête. Le piston : 1 cm de course. La vapeur des conduits : la texture se répète tous les 10 cm.
        const float k_NeedleEmpty = -135f;
        const float k_NeedleFull = 115f;
        const float k_NeedleTremble = 5f;
        const float k_PistonStroke = 0.01f;

        Transform m_Gear;
        Transform m_Piston;
        Transform m_Needle;
        Quaternion m_GearRest;
        Quaternion m_NeedleRest;
        Vector3 m_PistonRest;
        Renderer m_Conduits;
        Renderer m_Furnace;
        MaterialPropertyBlock m_LookBlock;
        ParticleSystem m_Leak;
        float m_NextLeak;
        float m_Surge;
        float m_GearAngle;
        float m_PistonPhase;
        float m_Scroll;
        float m_NeedleAngle = k_NeedleEmpty;

        void BuildLook(Transform bat)
        {
            m_LookBlock = new MaterialPropertyBlock();
            foreach (var child in bat.GetComponentsInChildren<Transform>(true))
            {
                switch (child.name)
                {
                    case "Engrenage":
                        m_Gear = child;
                        m_GearRest = child.localRotation;
                        break;
                    case "Piston":
                        m_Piston = child;
                        m_PistonRest = child.localPosition;
                        break;
                    case "Aiguille":
                        m_Needle = child;
                        m_NeedleRest = child.localRotation;
                        break;
                    case "Conduits":
                        m_Conduits = child.GetComponent<Renderer>();
                        break;
                    case "Foyer":
                        m_Furnace = child.GetComponent<Renderer>();
                        break;
                    case "Fuite":
                        m_Leak = CreateSteam(child, m_Definition.batSteamMaterial);
                        break;
                }
            }

            m_NextLeak = Time.time + Random.Range(1f, 3f);
        }

        // Un coup (0 : un petit coup ; 1 : un parfait) : la vapeur jaillit dans les conduits, le foyer flambe, tout s'emballe.
        void OnLookHit(float strength) => m_Surge = Mathf.Max(m_Surge, 0.35f + 0.65f * Mathf.Clamp01(strength));

        void UpdateLook(float deltaTime)
        {
            if (m_LookBlock == null)
                return;

            var belt = GrenadeBelt.Instance;
            var pressure = belt != null ? belt.Progress : 1f;
            var ready = pressure >= 0.999f;
            m_Surge = Mathf.MoveTowards(m_Surge, 0f, deltaTime * 1.2f);

            if (m_Gear != null)
            {
                m_GearAngle += deltaTime * (40f + 60f * pressure + 260f * m_Surge);
                m_Gear.localRotation = m_GearRest * Quaternion.Euler(0f, m_GearAngle, 0f);
            }

            if (m_Piston != null)
            {
                m_PistonPhase += deltaTime * Mathf.PI * 2f * (1.5f + 1.5f * pressure + 5f * m_Surge);
                m_Piston.localPosition = m_PistonRest + Vector3.up * (Mathf.Sin(m_PistonPhase) * k_PistonStroke);
            }

            if (m_Needle != null)
            {
                var target = Mathf.Lerp(k_NeedleEmpty, k_NeedleFull, pressure) + 20f * m_Surge;
                if (ready)
                    target += Mathf.Sin(Time.time * 23f) * k_NeedleTremble;
                m_NeedleAngle = Mathf.Lerp(m_NeedleAngle, target, 1f - Mathf.Exp(-deltaTime * 12f));
                m_Needle.localRotation = m_NeedleRest * Quaternion.Euler(0f, m_NeedleAngle, 0f);
            }

            if (m_Conduits != null)
            {
                m_Scroll += deltaTime * (2.5f + 2.5f * pressure + 14f * m_Surge);
                m_Scroll %= 1000f;
                m_Conduits.GetPropertyBlock(m_LookBlock);
                m_LookBlock.SetVector(k_BaseMapStId, new Vector4(1f, 1f, 0f, -m_Scroll));
                var steam = k_ConduitSteam;
                steam.a = Mathf.Clamp01(0.45f + 0.35f * pressure + 0.5f * m_Surge);
                m_LookBlock.SetColor(k_BaseColorId, steam);
                m_Conduits.SetPropertyBlock(m_LookBlock);
            }

            if (m_Furnace != null)
            {
                var flicker = 0.7f + 0.3f * Mathf.PerlinNoise(Time.time * 3f, 0.37f) + 1.5f * m_Surge;
                m_Furnace.GetPropertyBlock(m_LookBlock);
                m_LookBlock.SetColor(k_EmissionId, k_FurnaceGlow * flicker);
                m_Furnace.SetPropertyBlock(m_LookBlock);
            }

            // Une petite fuite de vapeur à la vanne, de temps en temps (plus souvent quand la grenade est prête).
            if (m_Leak != null && Time.time >= m_NextLeak)
            {
                m_NextLeak = Time.time + Random.Range(ready ? 1.2f : 2.5f, ready ? 2.5f : 5f);
                var main = m_Leak.main;
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.035f);
                m_Leak.Emit(Random.Range(2, 5));
            }
        }
    }
}
