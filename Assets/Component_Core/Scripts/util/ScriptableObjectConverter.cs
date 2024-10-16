using UnityEngine;
# if UNITY_EDITOR
using UnityEditor;
# endif
using System.IO;

# if UNITY_EDITOR
public static class ScriptableObjectConverter
{
    [MenuItem("Tools/Convert ScriptableObject to Instance")]
    public static void ConvertSOToInstance()
    { // Get the selected object in the Project window
        Object selectedObject = Selection.activeObject;
        
        if (selectedObject == null || !(selectedObject is MonoScript))
        {
            Debug.LogError("Please select a ScriptableObject script in the Project window.");
            return;
        }

        MonoScript monoScript = (MonoScript)selectedObject;
        System.Type scriptType = monoScript.GetClass();

        if (scriptType == null || !typeof(ScriptableObject).IsAssignableFrom(scriptType))
        {
            Debug.LogError("Selected script is not a valid ScriptableObject.");
            return;
        }

        ScriptableObject instance = ScriptableObject.CreateInstance(scriptType);

        string assetPath = AssetDatabase.GetAssetPath(selectedObject);
        string folderPath = Path.GetDirectoryName(assetPath);
        string assetName = Path.GetFileNameWithoutExtension(assetPath);
        string instancePath = Path.Combine(folderPath, $"{assetName}.asset");

        AssetDatabase.CreateAsset(instance, instancePath);
        AssetDatabase.SaveAssets();

        Debug.Log($"ScriptableObject instance created at {instancePath}");
    }
}
# endif
