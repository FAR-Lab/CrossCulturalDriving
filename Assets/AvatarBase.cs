using UnityEngine;

public class AvatarBase : MonoBehaviour
{
    private SkinnedMeshRenderer skinnedMeshRenderer;

    private void Awake()
    {
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
    }

    public void SetMaterialsColor(Color color)
    {
        Material[] mats = skinnedMeshRenderer.materials;
        for (int i = 0; i < mats.Length; i++)
        {
            mats[i].color = color;
        }
        skinnedMeshRenderer.materials = mats;
    }
}