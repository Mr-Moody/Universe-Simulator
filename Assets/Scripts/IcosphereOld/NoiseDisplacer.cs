#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SphereGenerator))]
public class NoiseDisplacer : MonoBehaviour
{
    [Header("Noise Settings")]
    [SerializeField] private int seed = 0;
    [SerializeField] private float strength = 2f; // strong hills
    [SerializeField] private float scale = 2f;    // option 2
    [SerializeField] private int octaves = 4;
    [SerializeField] private float persistence = 0.5f;
    [SerializeField] private float mountainExponent = 2f;
    [SerializeField] private float baseLandOffsetMultiplier = 0.005f;

    [Header("Collider Settings")]
    [SerializeField] private bool useDisplacedCollider = true;

    private SphereGenerator generator;
    public event System.Action<MeshFilter> OnNoiseApplied;

    private void OnEnable()
    {
        generator = GetComponent<SphereGenerator>();
        generator.OnSphereUpdated += HandleSphereUpdated;
    }

    private void OnDisable()
    {
        if (generator != null)
            generator.OnSphereUpdated -= HandleSphereUpdated;
    }

    private void OnValidate()
    {
        if (generator == null)
            generator = GetComponent<SphereGenerator>();

#if UNITY_EDITOR
        EditorApplication.delayCall += () =>
        {
            if (generator != null && generator.MeshFilter != null)
                ApplyNoise(generator.MeshFilter, generator.MeshCollider);
        };
#endif
    }

    private void HandleSphereUpdated(MeshFilter visualMeshFilter, MeshCollider colliderMesh)
    {
        ApplyNoise(visualMeshFilter, colliderMesh);
    }

    private void ApplyNoise(MeshFilter visualMeshFilter, MeshCollider colliderMesh)
    {
        if (generator.BaseMesh == null || visualMeshFilter == null) return;

        Mesh baseMesh = generator.BaseMesh;
        Mesh visualMesh = Instantiate(baseMesh);
        visualMeshFilter.sharedMesh = visualMesh;

        Vector3[] baseVerts = baseMesh.vertices;
        Vector3[] newVerts = new Vector3[baseVerts.Length];

        float radius = generator.Radius;
        float seaLevel = radius * 0.95f;
        float mountainStart = radius * 0.05f;
        float baseLandOffset = radius * baseLandOffsetMultiplier;

        for (int i = 0; i < baseVerts.Length; i++)
        {
            Vector3 v = baseVerts[i];
            Vector3 normal = v.normalized;

            // multi-layer Perlin noise
            float noise = 0f;
            float freq = 1f;
            float amp = 1f;
            for (int o = 0; o < octaves; o++)
            {
                float n = (
                    Mathf.PerlinNoise((normal.x + 1) * scale * freq + seed, (normal.y + 1) * scale * freq + seed) +
                    Mathf.PerlinNoise((normal.y + 1) * scale * freq + seed, (normal.z + 1) * scale * freq + seed) +
                    Mathf.PerlinNoise((normal.x + 1) * scale * freq + seed, (normal.z + 1) * scale * freq + seed)
                ) / 3f;

                noise += n * amp;
                freq *= 2f;
                amp *= persistence;
            }

            // mountain mask
            float heightAboveSea = Mathf.Max(0f, v.magnitude - seaLevel + baseLandOffset);
            float mountainMask = Mathf.Clamp01(heightAboveSea / mountainStart);
            noise = Mathf.Pow(noise, mountainExponent) * mountainMask;

            // option 4: center noise around 0 so valleys exist
            float finalHeight = v.magnitude + (noise - 0.5f) * 2f * strength + baseLandOffset;
            newVerts[i] = normal * finalHeight;
        }

        visualMesh.vertices = newVerts;
        visualMesh.RecalculateNormals();
        visualMesh.RecalculateBounds();

        // --- Collider mesh ---
        if (colliderMesh != null && useDisplacedCollider)
        {
            Mesh colliderBase = generator.MeshCollider.sharedMesh;
            if (colliderBase != null)
            {
                Mesh colliderCopy = Instantiate(colliderBase);
                Vector3[] cVerts = colliderCopy.vertices;

                for (int i = 0; i < cVerts.Length; i++)
                {
                    Vector3 v = cVerts[i];
                    Vector3 normal = v.normalized;

                    float noise = 0f;
                    float freq = 1f;
                    float amp = 1f;
                    for (int o = 0; o < octaves; o++)
                    {
                        float n = (
                            Mathf.PerlinNoise((normal.x + 1) * scale * freq + seed, (normal.y + 1) * scale * freq + seed) +
                            Mathf.PerlinNoise((normal.y + 1) * scale * freq + seed, (normal.z + 1) * scale * freq + seed) +
                            Mathf.PerlinNoise((normal.x + 1) * scale * freq + seed, (normal.z + 1) * scale * freq + seed)
                        ) / 3f;

                        noise += n * amp;
                        freq *= 2f;
                        amp *= persistence;
                    }

                    float heightAboveSea = Mathf.Max(0f, v.magnitude - seaLevel + baseLandOffset);
                    float mountainMask = Mathf.Clamp01(heightAboveSea / mountainStart);
                    noise = Mathf.Pow(noise, mountainExponent) * mountainMask;

                    cVerts[i] = normal * (v.magnitude + (noise - 0.5f) * 2f * strength + baseLandOffset);
                }

                colliderCopy.vertices = cVerts;
                colliderCopy.RecalculateNormals();
                colliderCopy.RecalculateBounds();
                colliderMesh.sharedMesh = null;
                colliderMesh.sharedMesh = colliderCopy;
            }
        }

        OnNoiseApplied?.Invoke(visualMeshFilter);
    }
}
