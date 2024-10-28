using UnityEditor;
using UnityEngine;

public class SwapPosRot : Editor
{
    [MenuItem("Tools/Swap Position and Rotation")]
    private static void SwapPositionAndRotation()
    {
        if (Selection.gameObjects.Length != 2)
        {
            EditorUtility.DisplayDialog("Swap Position and Rotation",
                "Please select exactly two GameObjects in the hierarchy.",
                "OK");
            return;
        }

        GameObject objA = Selection.gameObjects[0];
        GameObject objB = Selection.gameObjects[1];

        Vector3 positionA = objA.transform.position;
        Quaternion rotationA = objA.transform.rotation;

        Undo.RecordObject(objA.transform, "Swap Position and Rotation");
        Undo.RecordObject(objB.transform, "Swap Position and Rotation");

        objA.transform.position = objB.transform.position;
        objA.transform.rotation = objB.transform.rotation;

        objB.transform.position = positionA;
        objB.transform.rotation = rotationA;

        EditorUtility.SetDirty(objA);
        EditorUtility.SetDirty(objB);
    }
}