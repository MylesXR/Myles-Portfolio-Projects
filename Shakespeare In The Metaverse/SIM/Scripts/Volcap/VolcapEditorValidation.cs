using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolcapEditorValidation : MonoBehaviour
{
    public Material SVFmaterial;

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
            return;

        MeshFilter mf;
        if (TryGetComponent(out mf) && mf.sharedMesh == null)
            mf.sharedMesh = new Mesh();

        MeshRenderer mr;
        if (TryGetComponent(out mr) && mr.sharedMaterial != SVFmaterial)
            mr.sharedMaterial = SVFmaterial;
#endif 
    }
}
