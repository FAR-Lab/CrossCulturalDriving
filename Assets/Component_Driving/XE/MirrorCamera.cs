// Attach this to a camera.
// Inverts the view of the camera so everything rendered by it, is flipped

using UnityEngine;
using UnityEngine.Rendering;

//[ExecuteInEditMode]
public class MirrorCamera : MonoBehaviour {
    private const int FrameSkip = 3;
    private static int SharedFrameOffset;
    public Material ReferenceMaterial;
    public MeshRenderer TargetRenderer;
    public RenderTexture rt;
    public int width = 256;
    public int height = 256;
    public RenderTextureFormat rtf;
    public bool flipHorizontal;
    private Camera cam;
    private int frameOffset;

    private void Start() {
        cam = GetComponent<Camera>();

        rt = new RenderTexture(width, height, 16, rtf);
        rt.Create();
        rt.name = "MirrorCameraTexture" + transform.name;


        var mat = new Material(ReferenceMaterial);
        mat.SetTexture("_MainTex", rt, RenderTextureSubElement.Color);
        TargetRenderer.material = mat;

        cam.forceIntoRenderTexture = true;
        cam.targetTexture = rt;
        cam.stereoTargetEye = StereoTargetEyeMask.None;
        // Here follows an attempt to reduce rendering load onthe oculus Quest:
        // Based on a solution on the unity forums: http://answers.unity.com/answers/1429100/view.html
        cam.enabled = false;
        frameOffset = SharedFrameOffset;
        SharedFrameOffset++;
//  Debug.Log("Camera Mirror script start up assigned RT");
    }

    private void Update() {
        if ((Time.frameCount + frameOffset) % FrameSkip == 0) cam.Render();
    }

    private void OnDestroy() {
        rt.Release();
    }

    private void OnPostRender() {
        GL.invertCulling = false;
    }

    private void OnPreCull() {
        cam.ResetWorldToCameraMatrix();
        cam.ResetProjectionMatrix();
        var scale = new Vector3(flipHorizontal ? -1 : 1, 1, 1);
        cam.projectionMatrix = cam.projectionMatrix * Matrix4x4.Scale(scale);
    }

    private void OnPreRender() {
        GL.invertCulling = flipHorizontal;
    }
}