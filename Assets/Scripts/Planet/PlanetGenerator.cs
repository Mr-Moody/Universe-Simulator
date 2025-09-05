using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class PlanetGenerator : MonoBehaviour
{
    [Header("Planet Settings")]
    public PlanetSettings settings;

    [Header("Planet Material")]
    public Material planetMaterial;

    [Header("LOD Settings")]
    [SerializeField] private Transform playerCamera;

    public Transform PlayerCamera => playerCamera;

    private CubeSpherePatch[] patches;
    private bool pendingRegeneration = false;

    private void OnEnable()
    {
        EnsureSettings();
        ScheduleRegeneration();
    }

//    private void OnValidate()
//    {
//#if UNITY_EDITOR
//        EnsureSettings();
//        ScheduleRegeneration();
//#endif
//    }

#if UNITY_EDITOR
    private void EnsureSettings()
    {
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<PlanetSettings>();
            string folderPath = "Assets/Planets";
            if (!AssetDatabase.IsValidFolder(folderPath))
                AssetDatabase.CreateFolder("Assets", "Planets");

            string path = $"{folderPath}/{gameObject.name}_Settings.asset";
            path = AssetDatabase.GenerateUniqueAssetPath(path);

            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Created unique PlanetSettings for {gameObject.name} at {path}");
        }
    }
#endif

    private void DeleteOldPatches()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child != null && (child.name.Contains("Patch") || child.name.Contains("Atmosphere")))
                DestroyImmediate(child.gameObject);
        }
        patches = null;
    }

    public void RegenerateImmediate()
    {
#if UNITY_EDITOR
        if (pendingRegeneration) return;
        pendingRegeneration = true;

        Undo.RegisterFullObjectHierarchyUndo(gameObject, "Regenerate Planet");

        DeleteOldPatches();

        if (planetMaterial == null)
        {
            Shader shader = Shader.Find("Custom/PlanetShader");
            if (shader != null)
                planetMaterial = new Material(shader);
        }

        // Initialize six cube faces
        patches = new CubeSpherePatch[6];
        Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

        for (int i = 0; i < 6; i++)
        {
            // Use minResolution at start; LOD will adjust dynamically
            patches[i] = new CubeSpherePatch(directions[i], settings.minResolution, settings.radius, settings);
            patches[i].CreateGameObject(transform);

            var rend = patches[i].GameObject.GetComponent<MeshRenderer>();
            if (rend != null && planetMaterial != null)
                rend.sharedMaterial = planetMaterial;

            patches[i].GenerateMesh(Vector2.zero, Vector2.one);
        }

        if (settings.generateAtmosphere)
            AtmosphereLayer.CreateOrUpdateAtmosphere(transform, settings);

        pendingRegeneration = false;
#endif
    }

    public void ScheduleRegeneration()
    {
#if UNITY_EDITOR
        if (pendingRegeneration) return;

        pendingRegeneration = true;
        EditorApplication.delayCall += () =>
        {
            if (this != null && settings != null)
                RegenerateImmediate();

            pendingRegeneration = false;
        };
#endif
    }

    private void Update()
    {
        if (patches == null || playerCamera == null) return;

        foreach (var patch in patches)
        {
            if (patch != null)
            {
                // Dynamically calculate resolution based on camera distance
                float dist = Vector3.Distance(patch.GameObject.transform.position, playerCamera.position);
                int dynamicResolution = Mathf.Clamp(
                    Mathf.RoundToInt(Mathf.Lerp(settings.maxResolution, settings.minResolution, dist / settings.maxLODDistance)),
                    settings.minResolution,
                    settings.maxResolution
                );

                patch.UpdateLOD(playerCamera, settings.maxLODDepth, settings.subdivideDistance, settings.mergeDistance, Vector2.zero, Vector2.one);
            }
        }
    }
}
