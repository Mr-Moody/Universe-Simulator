using System;
using UnityEngine;

public static class AtmosphereLayer
{
    private const string AtmosphereName = "Atmosphere";

    public static void CreateOrUpdateAtmosphere(Transform parent, PlanetSettings settings)
    {
        Transform existing = parent.Find(AtmosphereName);
        GameObject atmosphere;
        if (existing != null)
            atmosphere = existing.gameObject;
        else
        {
            atmosphere = new GameObject(AtmosphereName);
            atmosphere.transform.SetParent(parent, false);
        }

        MeshFilter mf = atmosphere.GetComponent<MeshFilter>();
        if (mf == null) mf = atmosphere.AddComponent<MeshFilter>();

        MeshRenderer mr = atmosphere.GetComponent<MeshRenderer>();
        if (mr == null) mr = atmosphere.AddComponent<MeshRenderer>();

        mr.sharedMaterial = mr.sharedMaterial != null ? mr.sharedMaterial : new Material(Shader.Find("Custom/AtmosphereShader"));

        // Apply settings
        Material mat = mr.sharedMaterial;
        if (mat.HasProperty("_PlanetRadius")) mat.SetFloat("_PlanetRadius", settings.radius);
        if (mat.HasProperty("_Thickness")) mat.SetFloat("_Thickness", settings.atmosphereThickness);
        if (mat.HasProperty("_AtmosphereColor")) mat.SetColor("_AtmosphereColor", settings.atmosphereColor);
        if (mat.HasProperty("_SunsetColor")) mat.SetColor("_SunsetColor", settings.sunsetColor);
        if (mat.HasProperty("_Intensity")) mat.SetFloat("_Intensity", settings.atmosphereIntensity);

        Light sun = RenderSettings.sun ?? UnityEngine.Object.FindObjectOfType<Light>();
        if (sun != null && mat.HasProperty("_SunDir"))
        {
            Vector3 sunDir = sun.transform.forward;
            mat.SetVector("_SunDir", new Vector4(sunDir.x, sunDir.y, sunDir.z, 0));
        }

        atmosphere.transform.localScale = Vector3.one * settings.radius * settings.atmosphereScale;

        // Simple sphere mesh
        if (mf.sharedMesh == null)
            mf.sharedMesh = GenerateSphereMesh(1f, 10);
    }

    private static Mesh GenerateSphereMesh(float radius, int resolution)
    {
        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Mesh mesh = UnityEngine.Object.Instantiate(temp.GetComponent<MeshFilter>().sharedMesh);
        UnityEngine.Object.DestroyImmediate(temp);

        Vector3[] verts = mesh.vertices;

        for (int i = 0; i < verts.Length; i++)
            verts[i] *= radius;

        mesh.vertices = verts;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}
