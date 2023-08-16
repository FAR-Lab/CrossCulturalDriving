using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class PlaceTreeShadowCasters {
    [MenuItem("Terrain/Place Tree Shadow Casters")]
    private static void Run() {
        var terrain = Terrain.activeTerrain;
        if (terrain == null) return;

        var td = terrain.terrainData;

        var parent = new GameObject("Tree Shadow Casters");
        foreach (var tree in td.treeInstances) {
            var pos = Vector3.Scale(tree.position, td.size) + terrain.transform.position;

            var treeProt = td.treePrototypes[tree.prototypeIndex];
            var prefab = treeProt.prefab;

            var obj = Object.Instantiate(prefab, pos, Quaternion.AngleAxis(tree.rotation, Vector3.up));
            var renderer = obj.GetComponent<MeshRenderer>();
            renderer.receiveShadows = false;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            GameObjectUtility.SetStaticEditorFlags(obj, StaticEditorFlags.ContributeGI);

            var t = obj.transform;
            t.localScale = new Vector3(tree.widthScale, tree.heightScale, tree.widthScale);
            t.parent = parent.transform;
        }
    }
}