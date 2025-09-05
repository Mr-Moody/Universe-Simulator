using System;
using UnityEngine;

public class CubeSpherePatch
{
    private Vector3 localUp;
    private Vector3 axisA;
    private Vector3 axisB;
    private int resolution;
    private float radius;
    private PlanetSettings settings;

    private Mesh mesh;
    private GameObject patchObject;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    private int depth;
    private CubeSpherePatch[] children;

    // Edge arrays for seamless stitching
    private Vector3[] topEdge;
    private Vector3[] bottomEdge;
    private Vector3[] leftEdge;
    private Vector3[] rightEdge;

    public GameObject GameObject => patchObject;

    public CubeSpherePatch(Vector3 localUp, int resolution, float radius, PlanetSettings settings, int depth = 0)
    {
        this.localUp = localUp;
        this.resolution = resolution;
        this.radius = radius;
        this.settings = settings;
        this.depth = depth;

        axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        axisB = Vector3.Cross(localUp, axisA);
    }

    public void CreateGameObject(Transform parent)
    {
        patchObject = new GameObject($"Patch_Depth{depth}");
        patchObject.transform.SetParent(parent, false);

        meshFilter = patchObject.AddComponent<MeshFilter>();
        meshRenderer = patchObject.AddComponent<MeshRenderer>();
        meshCollider = patchObject.AddComponent<MeshCollider>();

        if (meshRenderer.sharedMaterial == null)
            meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));

        mesh = new Mesh();
    }

    public void GenerateMesh(Vector2 uvMin, Vector2 uvMax, Vector3[] top = null, Vector3[] bottom = null, Vector3[] left = null, Vector3[] right = null)
    {
        int res = Mathf.Max(2, resolution);
        Vector3[] vertices = new Vector3[res * res];
        Color[] colors = new Color[res * res];
        int[] triangles = new int[(res - 1) * (res - 1) * 6];
        int t = 0;
        float baseLandOffset = radius * settings.baseLandOffsetMultiplier;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                int i = x + y * res;
                Vector3 vertex;

                // Use edges from parent if provided, otherwise generate noise
                if (y == 0 && bottom != null) vertex = bottom[x];
                else if (y == res - 1 && top != null) vertex = top[x];
                else if (x == 0 && left != null) vertex = left[y];
                else if (x == res - 1 && right != null) vertex = right[y];
                else
                {
                    Vector2 percent = uvMin + new Vector2(x / (float)(res - 1), y / (float)(res - 1)) * (uvMax - uvMin);
                    Vector3 pointOnCube = localUp + (percent.x - 0.5f) * 2 * axisA + (percent.y - 0.5f) * 2 * axisB;
                    Vector3 normal = pointOnCube.normalized;
                    float displacement = GenerateNoise(normal);
                    vertex = normal * (radius + displacement + radius * settings.baseLandOffsetMultiplier);
                }

                vertices[i] = vertex;
            }
        }

        // Store edges for children
        topEdge = new Vector3[res];
        bottomEdge = new Vector3[res];
        leftEdge = new Vector3[res];
        rightEdge = new Vector3[res];

        for (int i = 0; i < res; i++)
        {
            bottomEdge[i] = vertices[i];                 // y = 0
            topEdge[i] = vertices[i + (res - 1) * res];  // y = res-1
            leftEdge[i] = vertices[i * res];             // x = 0
            rightEdge[i] = vertices[i * res + (res - 1)]; // x = res-1
        }

        // Triangles
        for (int y = 0; y < res - 1; y++)
        {
            for (int x = 0; x < res - 1; x++)
            {
                int i = x + y * res;
                triangles[t++] = i;
                triangles[t++] = i + res + 1;
                triangles[t++] = i + res;
                triangles[t++] = i;
                triangles[t++] = i + 1;
                triangles[t++] = i + res + 1;
            }
        }

        // Vertex colors
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                int i = x + y * res;
                colors[i] = CalculateVertexColor(vertices, i, x, y, res);
            }
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (meshFilter != null) meshFilter.sharedMesh = mesh;
        if (meshCollider != null && vertices.Length >= 3) meshCollider.sharedMesh = mesh;
    }

    private float GenerateNoise(Vector3 normal)
    {
        float noise = 0f, freq = 1f, amp = 1f;
        for (int o = 0; o < settings.octaves; o++)
        {
            float n = (
                Mathf.PerlinNoise((normal.x + 1) * settings.scale * freq + settings.seed,
                                  (normal.y + 1) * settings.scale * freq + settings.seed) +
                Mathf.PerlinNoise((normal.y + 1) * settings.scale * freq + settings.seed,
                                  (normal.z + 1) * settings.scale * freq + settings.seed) +
                Mathf.PerlinNoise((normal.x + 1) * settings.scale * freq + settings.seed,
                                  (normal.z + 1) * settings.scale * freq + settings.seed)
            ) / 3f;

            noise += n * amp;
            freq *= 2f;
            amp *= settings.persistence;
        }
        return Mathf.Pow(noise, settings.mountainExponent) * settings.strength;
    }

    private Color CalculateVertexColor(Vector3[] vertices, int index, int x, int y, int res)
    {
        float waterThreshold = radius * settings.waterThresholdMultiplier;
        float waterBlend = radius * settings.waterBlendMultiplier;

        Vector3 current = vertices[index];
        float currentHeight = current.magnitude;

        Vector3 left = x > 0 ? vertices[index - 1] : current;
        Vector3 right = x < res - 1 ? vertices[index + 1] : current;
        Vector3 down = y > 0 ? vertices[index - res] : current;
        Vector3 up = y < res - 1 ? vertices[index + res] : current;

        float dx = (right.magnitude - left.magnitude) / 2f;
        float dy = (up.magnitude - down.magnitude) / 2f;
        float slope = Mathf.Sqrt(dx * dx + dy * dy);

        float slopeT = Mathf.InverseLerp(0f, settings.slopeThreshold, slope);
        slopeT = Mathf.Clamp01(slopeT);

        float waterT = Mathf.InverseLerp(waterThreshold - waterBlend, waterThreshold + waterBlend, currentHeight);
        waterT = Mathf.Clamp01(waterT);

        Color landColor = Color.Lerp(settings.grassColor, settings.rockColor, slopeT);
        return Color.Lerp(settings.waterColor, landColor, waterT);
    }

    public void UpdateLOD(Transform player, int maxDepth, float baseSubdivideDistance, float mergeDistance,
                      Vector2 uvMin, Vector2 uvMax, float maxViewAngle = 90f)
    {
        if (patchObject == null) return;

        // --- 1. Calculate patch world position as patch center ---
        Vector3 patchCenterWorld = patchObject.transform.position;

        // --- 2. Distance from camera to patch surface ---
        float distanceToSurface = Mathf.Max(0f, Vector3.Distance(player.position, patchCenterWorld) - radius);

        // --- 3. Angular FOV check ---
        Vector3 patchToCamera = (player.position - patchCenterWorld).normalized;
        float angleToCamera = Vector3.Angle(player.forward, patchToCamera);
        bool inFOV = angleToCamera > maxViewAngle;

        // --- 4. Scale subdivide distance by depth (progressive LOD) ---
        float subdivideDistance = baseSubdivideDistance / (depth + 1);
        float mergeThreshold = mergeDistance * 1.1f; // hysteresis

        bool shouldSubdivide = distanceToSurface < subdivideDistance && depth < maxDepth; //add FOV logic later
        bool shouldMerge = children != null && distanceToSurface > mergeThreshold;

        // --- 5. Subdivide ---
        if (shouldSubdivide)
        {
            if (children == null)
            {
                // Create 4 children with proper UV regions
                Vector2 patchSize = (uvMax - uvMin) * 0.5f;

                children = new CubeSpherePatch[4];
                for (int i = 0; i < 4; i++)
                {
                    Vector2 childUVMinsLocal = uvMin + new Vector2((i % 2) * patchSize.x, (i / 2) * patchSize.y);

                    children[i] = new CubeSpherePatch(localUp, resolution, radius, settings, depth + 1);
                    children[i].CreateGameObject(patchObject.transform);
                    children[i].GenerateMesh(childUVMinsLocal, childUVMinsLocal + patchSize);

                    if (meshRenderer != null)
                        children[i].GameObject.GetComponent<MeshRenderer>().sharedMaterial = meshRenderer.sharedMaterial;
                }
            }

            // Disable parent only after children exist
            if (meshRenderer != null)
                meshRenderer.enabled = false;

            // Update children recursively
            Vector2 childPatchSize = (uvMax - uvMin) * 0.5f;
            for (int i = 0; i < 4; i++)
            {
                Vector2 childUVMinsLocal = uvMin + new Vector2((i % 2) * childPatchSize.x, (i / 2) * childPatchSize.y);
                children[i].UpdateLOD(player, maxDepth, baseSubdivideDistance, mergeDistance,
                                      childUVMinsLocal, childUVMinsLocal + childPatchSize, maxViewAngle);
            }
        }
        // --- 6. Merge ---
        else if (shouldMerge)
        {
            foreach (var c in children)
            {
                if (c != null && c.GameObject != null)
                {
#if UNITY_EDITOR
                UnityEngine.Object.DestroyImmediate(c.GameObject);
#else
                    UnityEngine.Object.Destroy(c.GameObject);
#endif
                }
            }
            children = null;

            // Re-enable parent when children are gone
            if (meshRenderer != null)
                meshRenderer.enabled = true;
        }
        // --- 7. Keep parent visible if no children ---
        else if (children == null)
        {
            if (meshRenderer != null)
                meshRenderer.enabled = true;
        }
        // --- 8. Update existing children if neither subdivide nor merge ---
        else
        {
            Vector2 childPatchSize = (uvMax - uvMin) * 0.5f;

            for (int i = 0; i < 4; i++)
            {
                Vector2 childUVMinsLocal = uvMin + new Vector2((i % 2) * childPatchSize.x, (i / 2) * childPatchSize.y);
                children[i].UpdateLOD(player, maxDepth, baseSubdivideDistance, mergeDistance,
                                      childUVMinsLocal, childUVMinsLocal + childPatchSize, maxViewAngle);
            }

            if (meshRenderer != null)
                meshRenderer.enabled = false;
        }
    }
}
