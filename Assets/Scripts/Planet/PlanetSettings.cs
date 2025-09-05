using UnityEngine;

[CreateAssetMenu(fileName = "PlanetSettings", menuName = "Planet/Settings")]
public class PlanetSettings : ScriptableObject
{
    [Header("Planet Dimensions")]
    public float radius = 10f;

    [Header("LOD Settings")]
    [Tooltip("Minimum resolution for distant patches")]
    public int minResolution = 5;
    [Tooltip("Maximum resolution for patches close to player")]
    public int maxResolution = 50;
    [Tooltip("Distance at which patches reach min resolution")]
    public float maxLODDistance = 50f;

    [Tooltip("Maximum depth of patch subdivision")]
    public int maxLODDepth = 3;
    [Tooltip("Distance at which a patch subdivides")]
    public float subdivideDistance = 50f;
    [Tooltip("Distance at which child patches merge back")]
    public float mergeDistance = 60f;

    [Header("Noise Settings")]
    public int seed = 0;
    public float strength = 2f;
    public float scale = 2f;
    public int octaves = 4;
    public float persistence = 0.5f;
    public float mountainExponent = 2f;
    public float baseLandOffsetMultiplier = 0.005f;

    [Header("Surface Colors")]
    public float waterThresholdMultiplier = 0.995f;
    public float waterBlendMultiplier = 0.05f;
    public float slopeThreshold = 0.7f;
    public Color waterColor = new Color(0f, 0.3f, 1f);
    public Color grassColor = new Color(0.1f, 0.8f, 0f);
    public Color rockColor = new Color(0.3f, 0.3f, 0.3f);

    [Header("Atmosphere")]
    public bool generateAtmosphere = true;
    public Color atmosphereColor = new Color(0.3f, 0.5f, 1f);
    public Color sunsetColor = new Color(1f, 0.4f, 0.2f);
    public float atmosphereIntensity = 0.3f;
    public float atmosphereScale = 1.02f;
    public float atmosphereThickness = 0.05f;
}
