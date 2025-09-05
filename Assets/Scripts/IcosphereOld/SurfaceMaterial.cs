using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SphereGenerator))]
[RequireComponent(typeof(NoiseDisplacer))]
public class SurfaceMaterial : MonoBehaviour
{
    private SphereGenerator generator;
    private NoiseDisplacer noise;
    private MeshFilter meshFilter;
    private Mesh mesh;

    [Header("Water Settings")]
    [SerializeField] private float waterThresholdMultiplier = 0.995f;
    [SerializeField] private float waterBlendMultiplier = 0.05f;

    [Header("Slope Settings")]
    [SerializeField] private float slopeThreshold = 0.7f;
    [SerializeField] private float slopeBlend = 0.3f;

    private void OnEnable()
    {
        generator = GetComponent<SphereGenerator>();
        noise = GetComponent<NoiseDisplacer>();

        if (generator != null)
            generator.OnSphereUpdated += HandleSphereUpdated;

        if (noise != null)
            noise.OnNoiseApplied += HandleNoiseApplied;
    }

    private void OnDisable()
    {
        if (generator != null)
            generator.OnSphereUpdated -= HandleSphereUpdated;

        if (noise != null)
            noise.OnNoiseApplied -= HandleNoiseApplied;
    }

    private void HandleSphereUpdated(MeshFilter visualMeshFilter, MeshCollider colliderMesh)
    {
        meshFilter = visualMeshFilter;
    }

    private void HandleNoiseApplied(MeshFilter visualMeshFilter)
    {
        meshFilter = visualMeshFilter;

        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            mesh = meshFilter.sharedMesh;
            ApplyColors();
        }
    }

    private void ApplyColors()
    {
        if (mesh == null || generator == null) return;

        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Color[] colors = new Color[vertices.Length];

        float radius = generator.Radius;
        float waterThreshold = radius * waterThresholdMultiplier;
        float waterBlend = radius * waterBlendMultiplier;

        Color waterColor = new Color(0f, 0.3f, 1f);
        Color grassColor = new Color(0.1f, 0.8f, 0f);
        Color rockColor = new Color(0.3f, 0.3f, 0.3f);

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = vertices[i];
            Vector3 n = normals[i];

            float height = v.magnitude;

            // --- Water blending ---
            float waterT = Mathf.InverseLerp(waterThreshold - waterBlend, waterThreshold + waterBlend, height);
            waterT = Mathf.Clamp01(waterT);

            // --- Slope blending ---
            float slope = 1f - Vector3.Dot(n, v.normalized);
            float slopeT = Mathf.InverseLerp(0f, slopeThreshold, slope);
            slopeT = Mathf.Clamp01(slopeT);

            // Land color: grass for flat, rock for steep
            Color landColor = Color.Lerp(grassColor, rockColor, slopeT);

            // Final color: water vs land
            Color finalColor = Color.Lerp(waterColor, landColor, waterT);

            // --- Roughness calculation ---
            // Water is smooth, mountains are rough
            float roughness = Mathf.Max(waterT, slopeT); // option: blend water+steep
            finalColor.a = roughness; // store roughness in alpha

            colors[i] = finalColor;
        }

        mesh.colors = colors;
    }
}
