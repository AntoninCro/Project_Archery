using UnityEngine;

namespace Archery.FinalBoss
{
    /// <summary>
    /// Les particules de l'ouverture de la porte de l'antre (12 novembre, le joueur : « la porte de MK devrait avoir beaucoup de
    /// particules quand elle s'ouvre »). Pendant que les battants s'écartent, des gerbes d'étincelles et de flammes jaillissent de la
    /// fente, sur toute sa hauteur ; grande ouverte, un souffle de feu sort de l'antre ; ensuite, tant qu'elle reste ouverte, des braises
    /// montent du couloir et des bouffées de flammes en sortent de temps en temps (on la voit vivre en arrivant de la tour).
    /// </summary>
    public partial class LairGate
    {
        static readonly Color k_SparkColor = new Color(1f, 0.55f, 0.15f, 1f);

        // Une gerbe toutes les … s pendant l'ouverture ; une bouffée toutes les … s une fois ouverte.
        const float k_OpeningSparkInterval = 0.1f;
        const float k_OpenPuffInterval = 1.6f;

        float m_NextOpeningFx;
        bool m_Blasted;
        ParticleSystem m_OpenEmbers;

        void BeginOpeningEffects()
        {
            m_NextOpeningFx = 0f;
            m_Blasted = false;
        }

        void StopOpeningEffects()
        {
            if (m_OpenEmbers != null)
                m_OpenEmbers.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        void UpdateOpeningEffects()
        {
            if (m_FireMaterial == null || !IsOpen || Time.time < m_NextOpeningFx)
                return;

            var opening = OpenAngle > 0f ? Mathf.Clamp01(m_Angle / OpenAngle) : 1f;
            if (opening < 0.999f)
            {
                // La fente qui s'élargit entre les deux battants : un point au hasard sur sa hauteur, juste devant elle.
                m_NextOpeningFx = Time.time + k_OpeningSparkInterval;
                var gap = Mathf.Lerp(0.1f, CorridorHalfWidth, opening);
                var point = transform.TransformPoint(Random.Range(-gap, gap), Random.Range(k_LeafBottom, m_DoorTop * 0.95f), k_HingeZ - 0.3f);
                FireEffects.SparkBurst(point, m_FireMaterial, 1.4f, 24, k_SparkColor);
                if (Random.value < 0.4f)
                    FireEffects.Burst(transform.TransformPoint(Random.Range(-gap, gap), 0.4f, k_HingeZ - 0.2f), m_FireMaterial, 0.9f, 24);
                return;
            }

            if (!m_Blasted)
            {
                // Grande ouverte : le souffle de l'antre, et ses braises.
                m_Blasted = true;
                for (var i = 0; i < 7; i++)
                {
                    var point = transform.TransformPoint(Random.Range(-0.8f, 0.8f) * CorridorHalfWidth, Random.Range(0.4f, m_DoorSide),
                                                         Random.Range(-1f, 1.2f));
                    FireEffects.Burst(point, m_FireMaterial, 1.6f, 50);
                    FireEffects.SparkBurst(point, m_FireMaterial, 2.6f, 40, k_SparkColor);
                }

                if (m_OpenEmbers == null)
                    m_OpenEmbers = FireEffects.CreateEmbers(transform, new Vector3(0f, 0.2f, 1.2f), m_FireMaterial, CorridorHalfWidth * 2f, 30f);
                else
                    m_OpenEmbers.Play();
            }

            // Ouverte : une bouffée de flammes et d'étincelles sort du couloir de temps en temps.
            m_NextOpeningFx = Time.time + k_OpenPuffInterval * Random.Range(0.7f, 1.3f);
            var puff = transform.TransformPoint(Random.Range(-0.6f, 0.6f) * CorridorHalfWidth, Random.Range(0.3f, 1.5f), Random.Range(0f, 1.5f));
            FireEffects.Burst(puff, m_FireMaterial, 1.1f, 30);
            FireEffects.SparkBurst(puff, m_FireMaterial, 1.8f, 24, k_SparkColor);
        }
    }
}
