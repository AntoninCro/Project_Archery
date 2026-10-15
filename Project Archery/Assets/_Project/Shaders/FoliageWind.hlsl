// Le vent dans la végétation (shader Archery/Foliage) : chaque sommet se déplace dans le monde selon le vent, d'autant
// plus qu'il est haut dans son modèle. Le pied ne bouge pas.
// La couleur des sommets est écrite à l'import des modèles (FoliageWindImporter) : rouge = hauteur dans le modèle (0 au
// pied, 1 en haut), vert = taille du modèle ÷ 16 m. Ça marche aussi pour les objets statiques, fusionnés au lancement.
#ifndef ARCHERY_FOLIAGE_WIND_INCLUDED
#define ARCHERY_FOLIAGE_WIND_INCLUDED

// Réglé pour toutes les scènes par Archery.World.Wind : xy = direction (monde, plan XZ), z = force, w = vitesse.
// Sans lui (z = 0), rien ne bouge.
float4 _ArcheryWind;

float3 ApplyFoliageWind(float3 positionWS, half4 color)
{
    float strength = _ArcheryWind.z;
    float bend = color.r * color.r;
    float height = color.g * 16.0;

    // Une herbe se couche de quelques centimètres, la cime d'un grand arbre d'une vingtaine.
    float amplitude = strength * (0.06 + 0.03 * height) * bend;
    float2 direction = normalize(_ArcheryWind.xy + float2(1e-4, 0.0));
    float time = _Time.y * _ArcheryWind.w;

    // Des vagues qui traversent la carte dans le sens du vent, des rafales lentes, et un frémissement des feuilles.
    float phase = dot(positionWS.xz, direction) * 0.18 + dot(positionWS.xz, float2(0.07, -0.05));
    float gust = 0.5 + 0.5 * sin(time * 0.35 - phase * 0.4);
    float sway = sin(time * 1.3 - phase) * 0.6 + sin(time * 2.7 - phase * 1.7) * 0.25;
    float flutter = sin(time * 7.0 + dot(positionWS, float3(1.7, 2.3, 1.9))) * 0.1;

    float push = (sway * (0.6 + 0.4 * gust) + 0.35 * gust) * amplitude;
    positionWS.xz += direction * push + float2(-direction.y, direction.x) * flutter * amplitude;
    positionWS.y -= abs(push) * 0.2;
    return positionWS;
}

// La même chose, pour une position dans le repère de l'objet.
float4 FoliageWindOS(float4 positionOS, half4 color)
{
    float3 positionWS = ApplyFoliageWind(TransformObjectToWorld(positionOS.xyz), color);
    return float4(TransformWorldToObject(positionWS), positionOS.w);
}

#endif
