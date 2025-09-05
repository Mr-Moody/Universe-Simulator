using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SphereGenerator : MonoBehaviour
{
    [Header("Resolutions")]
    [SerializeField] private int visualResolution = 3;   // high-res for rendering
    [SerializeField] private int colliderResolution = 1; // low-res for physics
    [SerializeField] private float radius = 1f;

    [Header("Materials")]
    [SerializeField] private Material surfaceMaterial;

    [Header("Atmosphere Settings")]
    [SerializeField] private bool generateAtmosphere = true;
    [SerializeField] private Material atmosphereMaterial;
    [SerializeField] private float atmosphereScale = 1.02f;
    [SerializeField] private int atmosphereResolution = 2;

    private const string SphereName = "GeneratedSphere";
    private const string AtmosphereName = "AtmosphereSphere";

    private GameObject sphere;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    private GameObject atmosphereSphere;
    private MeshFilter atmosphereMeshFilter;
    private MeshRenderer atmosphereRenderer;

    private float lastRadius;
    private int lastVisualRes;
    private int lastColliderRes;

    private Mesh baseMesh; // high-res sphere mesh

    public float Radius => radius;
    public MeshFilter MeshFilter => meshFilter;
    public MeshCollider MeshCollider => meshCollider;
    public Mesh BaseMesh => baseMesh;

    public System.Action<MeshFilter, MeshCollider> OnSphereUpdated;

    private void OnEnable() => UpdateSphere();
    private void OnValidate() => UpdateSphere();

    private void Update()
    {
        if (radius != lastRadius || visualResolution != lastVisualRes || colliderResolution != lastColliderRes)
            UpdateSphere();

        UpdateAtmosphereSunDirection();
    }

    private void UpdateAtmosphereSunDirection()
    {
        if (atmosphereMaterial == null || atmosphereSphere == null) return;

        if (atmosphereMaterial.HasProperty("_SunDir"))
        {
            Light sun = RenderSettings.sun ?? FindObjectOfType<Light>();
            if (sun != null)
            {
                Vector3 sunDir = sun.transform.forward; // world-space direction
                atmosphereMaterial.SetVector("_SunDir", new Vector4(sunDir.x, sunDir.y, sunDir.z, 0));
            }
        }
    }

    private void UpdateSphere()
    {
        radius = Mathf.Max(0.1f, radius);
        visualResolution = Mathf.Max(0, visualResolution);
        colliderResolution = Mathf.Max(0, colliderResolution);

        lastRadius = radius;
        lastVisualRes = visualResolution;
        lastColliderRes = colliderResolution;

        CreateOrUpdatePlanet();
        CreateOrUpdateAtmosphere();

        OnSphereUpdated?.Invoke(meshFilter, meshCollider);
    }

    private void CreateOrUpdatePlanet()
    {
        // Planet GameObject
        if (sphere == null)
        {
            Transform existing = transform.Find(SphereName);
            if (existing != null)
                sphere = existing.gameObject;
            else
            {
                sphere = new GameObject(SphereName);
                sphere.transform.SetParent(transform, false);
            }
        }

        // Ensure components exist
        meshFilter = sphere.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = sphere.AddComponent<MeshFilter>();

        meshCollider = sphere.GetComponent<MeshCollider>();
        if (meshCollider == null)
            meshCollider = sphere.AddComponent<MeshCollider>();

        var renderer = sphere.GetComponent<MeshRenderer>();
        if (renderer == null)
            renderer = sphere.AddComponent<MeshRenderer>();

        if (surfaceMaterial != null)
            renderer.sharedMaterial = surfaceMaterial;

        // Create planet meshes
        Mesh visualMesh = CreateSphereMesh(radius, visualResolution);
        if (meshFilter != null)
            meshFilter.sharedMesh = visualMesh;

        Mesh colliderMesh = CreateSphereMesh(radius, colliderResolution);
        if (meshCollider != null)
            meshCollider.sharedMesh = colliderMesh;

        baseMesh = visualMesh;
    }

    private void CreateOrUpdateAtmosphere()
    {
        if (!generateAtmosphere || atmosphereMaterial == null)
        {
            if (atmosphereSphere != null)
                DestroyImmediate(atmosphereSphere);
            return;
        }

        if (atmosphereSphere == null)
        {
            Transform existing = transform.Find(AtmosphereName);
            if (existing != null)
                atmosphereSphere = existing.gameObject;
            else
            {
                atmosphereSphere = new GameObject(AtmosphereName);
                atmosphereSphere.transform.SetParent(transform, false);
            }
        }

        // Ensure components exist
        atmosphereMeshFilter = atmosphereSphere.GetComponent<MeshFilter>();
        if (atmosphereMeshFilter == null)
            atmosphereMeshFilter = atmosphereSphere.AddComponent<MeshFilter>();

        atmosphereRenderer = atmosphereSphere.GetComponent<MeshRenderer>();
        if (atmosphereRenderer == null)
            atmosphereRenderer = atmosphereSphere.AddComponent<MeshRenderer>();

        atmosphereRenderer.sharedMaterial = atmosphereMaterial;

        // Scale slightly larger than planet
        atmosphereSphere.transform.localScale = Vector3.one * atmosphereScale;

        // Generate mesh
        atmosphereMeshFilter.sharedMesh = CreateSphereMesh(radius, atmosphereResolution);

        // Update shader properties for volumetric atmosphere
        if (atmosphereMaterial.HasProperty("_SunDir"))
        {
            Light sun = RenderSettings.sun ?? UnityEngine.Object.FindFirstObjectByType<Light>();
            if (sun != null)
            {
                Vector3 sunDir = sun.transform.forward;
                atmosphereMaterial.SetVector("_SunDir", new Vector4(sunDir.x, sunDir.y, sunDir.z, 0));
            }
        }

        if (atmosphereMaterial.HasProperty("_PlanetRadius"))
            atmosphereMaterial.SetFloat("_PlanetRadius", radius);
    }

    #region Sphere Mesh Generation

    private Mesh CreateSphereMesh(float radius, int resolution)
    {
        float t = (1f + Mathf.Sqrt(5f)) / 2f;

        List<Vector3> vertices = new List<Vector3>
        {
            new Vector3(-1,  t,  0),
            new Vector3( 1,  t,  0),
            new Vector3(-1, -t,  0),
            new Vector3( 1, -t,  0),
            new Vector3( 0, -1,  t),
            new Vector3( 0,  1,  t),
            new Vector3( 0, -1, -t),
            new Vector3( 0,  1, -t),
            new Vector3( t,  0, -1),
            new Vector3( t,  0,  1),
            new Vector3(-t,  0, -1),
            new Vector3(-t,  0,  1)
        };

        for (int i = 0; i < vertices.Count; i++)
            vertices[i] = vertices[i].normalized;

        List<int[]> faces = new List<int[]>
        {
            new [] {0, 11, 5}, new [] {0, 5, 1}, new [] {0, 1, 7}, new [] {0, 7, 10}, new [] {0, 10, 11},
            new [] {1, 5, 9}, new [] {5, 11, 4}, new [] {11, 10, 2}, new [] {10, 7, 6}, new [] {7, 1, 8},
            new [] {3, 9, 4}, new [] {3, 4, 2}, new [] {3, 2, 6}, new [] {3, 6, 8}, new [] {3, 8, 9},
            new [] {4, 9, 5}, new [] {2, 4, 11}, new [] {6, 2, 10}, new [] {8, 6, 7}, new [] {9, 8, 1}
        };

        Dictionary<long, int> middlePointIndexCache = new Dictionary<long, int>();
        for (int i = 0; i < resolution; i++)
        {
            List<int[]> faces2 = new List<int[]>();
            foreach (var tri in faces)
            {
                int a = tri[0], b = tri[1], c = tri[2];
                int ab = GetMiddlePoint(a, b, ref vertices, ref middlePointIndexCache);
                int bc = GetMiddlePoint(b, c, ref vertices, ref middlePointIndexCache);
                int ca = GetMiddlePoint(c, a, ref vertices, ref middlePointIndexCache);

                faces2.Add(new[] { a, ab, ca });
                faces2.Add(new[] { b, bc, ab });
                faces2.Add(new[] { c, ca, bc });
                faces2.Add(new[] { ab, bc, ca });
            }
            faces = faces2;
        }

        for (int i = 0; i < vertices.Count; i++)
            vertices[i] = vertices[i].normalized * radius;

        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();

        List<int> triangles = new List<int>();
        foreach (var face in faces)
        {
            triangles.Add(face[0]);
            triangles.Add(face[1]);
            triangles.Add(face[2]);
        }
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    private int GetMiddlePoint(int p1, int p2, ref List<Vector3> vertices, ref Dictionary<long, int> cache)
    {
        long key = p1 < p2 ? ((long)p1 << 32) + p2 : ((long)p2 << 32) + p1;
        if (cache.TryGetValue(key, out int ret)) return ret;

        Vector3 middle = ((vertices[p1] + vertices[p2]) / 2f).normalized;
        vertices.Add(middle);
        int index = vertices.Count - 1;
        cache.Add(key, index);
        return index;
    }

    #endregion
}
