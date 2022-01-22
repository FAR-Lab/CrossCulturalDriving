// Attach this to a camera.
// Inverts the view of the camera so everything rendered by it, is flipped

using System;
using UnityEngine;
using System.Collections;
using System.Security.Cryptography.X509Certificates;
using UnityEngine.Rendering;

public class MirrorCamera : MonoBehaviour
{
    Camera cam;
    public Material TargetMaterial;
    private RenderTexture rt;
    public int width=256;
    public int height=256;
    public int depth=0;
    public RenderTextureFormat rtf;
   

    void Start() {
        cam = GetComponent<Camera>();
        rt = new RenderTexture(width, height, depth, rtf);
        rt.Create();
        TargetMaterial.SetTexture("_MainTex",rt,RenderTextureSubElement.Color);
        cam.forceIntoRenderTexture = true;
        cam.targetTexture = rt;
      //  Debug.Log("Camera Mirror script start up assigned RT");
    }

    void OnPreCull()
    {
        cam.ResetWorldToCameraMatrix();
        cam.ResetProjectionMatrix();
        cam.projectionMatrix = cam.projectionMatrix * Matrix4x4.Scale(new Vector3(-1, 1, 1));
    }

    private void OnDestroy() {
        rt.Release();
        
    }

    void OnPreRender()
    {
        GL.invertCulling = true;
    }

    void OnPostRender()
    {
        GL.invertCulling = false;
    }
}