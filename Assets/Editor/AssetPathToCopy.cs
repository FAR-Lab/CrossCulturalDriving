// From https://forum.unity.com/threads/please-include-a-copy-path-when-right-clicking-a-game-object.429480/
using UnityEditor;
 
public static class CopyPathMenuItem
{
    [MenuItem("GameObject/Copy TransformPath")]
    private static void CopyPath()
    {
        var go = Selection.activeGameObject;
 
        if (go == null)
        {
            return;
        }
 
        var path = go.name;
 
        while (go.transform.parent != null)
        {
            go = go.transform.parent.gameObject;
            path = string.Format("/{0}/{1}", go.name, path);
        }
 
        EditorGUIUtility.systemCopyBuffer = path;
    }
 
    [MenuItem("GameObject/2D Object/Copy Path", true)]
    private static bool CopyPathValidation()
    {
        // We can only copy the path in case 1 object is selected
        return Selection.gameObjects.Length == 1;
    }
}