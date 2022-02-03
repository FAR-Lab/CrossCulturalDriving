// Attach this to a camera.
// Inverts the view of the camera so everything rendered by it, is flipped

using System;
using UnityEngine;
using System.Collections;
using System.Security.Cryptography.X509Certificates;
using UnityEngine.Rendering;

//[ExecuteInEditMode]
public class MirrorCamera : MonoBehaviour {
    Camera cam;
    public Material ReferenceMaterial;
    public MeshRenderer TargetRenderer;
    public RenderTexture rt;
    public int width = 256;
    public int height = 256;
    public int depth = 0;
    public RenderTextureFormat rtf;
    public bool flipHorizontal;

    void Start() {
        cam = GetComponent<Camera>();
        rt = new RenderTexture(width, height, depth, rtf);
        rt.Create();
        rt.name = "MirrorCameraTexture"+ transform.name;
        Debug.Log("Create render texture"+rt.name);
        
        Material mat    = new Material(ReferenceMaterial);
        mat.SetTexture("_MainTex", rt, RenderTextureSubElement.Color);
        TargetRenderer.material = mat;
        
        cam.forceIntoRenderTexture = true;
        cam.targetTexture = rt;
        cam.stereoTargetEye = StereoTargetEyeMask.None;
        //  Debug.Log("Camera Mirror script start up assigned RT");
    }

    void OnPreCull() {
        cam.ResetWorldToCameraMatrix();
        cam.ResetProjectionMatrix();
        Vector3 scale = new Vector3(flipHorizontal ? -1 : 1, 1, 1);
        cam.projectionMatrix = cam.projectionMatrix * Matrix4x4.Scale(scale);
    }

    private void OnDestroy() { rt.Release(); }

    void OnPreRender() { GL.invertCulling = flipHorizontal; }

    void OnPostRender() { GL.invertCulling = false; }
}