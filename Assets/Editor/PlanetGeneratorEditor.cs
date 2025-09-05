using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PlanetGenerator))]
public class PlanetGeneratorEditor : Editor
{
    SerializedProperty settingsProp;
    SerializedProperty materialProp;
    SerializedProperty playerCameraProp;
    PlanetGenerator generator;
    SerializedObject settingsSO;
    private bool pendingRegeneration = false;

    // Collapsible sections
    private bool showDimensions = true;
    private bool showLOD = true;
    private bool showNoise = true;
    private bool showColors = true;
    private bool showAtmosphere = true;

    private void OnEnable()
    {
        generator = (PlanetGenerator)target;
        settingsProp = serializedObject.FindProperty("settings");
        materialProp = serializedObject.FindProperty("planetMaterial");
        playerCameraProp = serializedObject.FindProperty("playerCamera");

        RefreshSettingsSO();
    }

    private void RefreshSettingsSO()
    {
        if (settingsProp != null && settingsProp.objectReferenceValue != null)
        {
            settingsSO = new SerializedObject(settingsProp.objectReferenceValue);
        }
        else
        {
            settingsSO = null;
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUI.BeginChangeCheck();

        // Planet Material
        EditorGUILayout.LabelField("Planet Material", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(materialProp);

        // Player Camera
        EditorGUILayout.PropertyField(playerCameraProp, new GUIContent("Player Camera"));

        // Planet Settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Planet Settings", EditorStyles.boldLabel);

        if (settingsProp != null && settingsProp.objectReferenceValue != null)
        {
            if (settingsSO == null || settingsSO.targetObject != settingsProp.objectReferenceValue)
                RefreshSettingsSO();

            if (settingsSO != null)
            {
                settingsSO.Update();

                DrawSection(settingsSO, "Planet Dimensions", ref showDimensions, "radius");
                DrawSection(settingsSO, "LOD Settings", ref showLOD,
                    "minResolution", "maxResolution", "maxLODDistance",
                    "maxLODDepth", "subdivideDistance", "mergeDistance");
                DrawSection(settingsSO, "Noise Settings", ref showNoise,
                    "seed", "strength", "scale", "octaves", "persistence", "mountainExponent", "baseLandOffsetMultiplier");
                DrawSection(settingsSO, "Surface Colors", ref showColors,
                    "waterThresholdMultiplier", "waterBlendMultiplier", "slopeThreshold",
                    "waterColor", "grassColor", "rockColor");
                DrawSection(settingsSO, "Atmosphere", ref showAtmosphere,
                    "generateAtmosphere", "atmosphereColor", "sunsetColor", "atmosphereIntensity", "atmosphereScale", "atmosphereThickness");

                settingsSO.ApplyModifiedProperties();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No PlanetSettings assigned. Please assign or regenerate a settings asset.", MessageType.Warning);
        }

        // Apply & regenerate on changes
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
            ScheduleRegeneration();
        }
        else
        {
            serializedObject.ApplyModifiedProperties();
        }
    }

    private void DrawSection(SerializedObject so, string label, ref bool foldout, params string[] properties)
    {
        foldout = EditorGUILayout.Foldout(foldout, label, true);
        if (!foldout) return;

        EditorGUI.indentLevel++;
        foreach (string propName in properties)
        {
            SerializedProperty prop = so.FindProperty(propName);
            if (prop != null)
            {
                EditorGUILayout.PropertyField(prop, true);
            }
            else
            {
                EditorGUILayout.HelpBox($"Property '{propName}' not found in PlanetSettings", MessageType.None);
            }
        }
        EditorGUI.indentLevel--;
    }

    private void ScheduleRegeneration()
    {
        if (pendingRegeneration) return;

        pendingRegeneration = true;
        EditorApplication.delayCall += () =>
        {
            if (generator != null)
                generator.RegenerateImmediate();

            pendingRegeneration = false;
        };
    }
}
