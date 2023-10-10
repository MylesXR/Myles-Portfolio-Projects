using MUXR.Interactables;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverlapPiecesCheck : MonoBehaviour
{
    public SocketGrabbable currentPiece;
    public int hologramLayer;
    public void Init(int _hologramLayer, SocketGrabbable _currentPiece)
    {
        hologramLayer = _hologramLayer;
        currentPiece = _currentPiece;
    }
    private void OnTriggerEnter(Collider other)
    {
        SocketGrabbable grababble;
        bool notSameLayer = hologramLayer != other.gameObject.layer;

        if (other.GetType() == typeof(BoxCollider) && notSameLayer && other.transform.parent != null && other.transform.parent.TryGetComponent<SocketGrabbable>(out grababble))
            currentPiece.SetOverlapHologramShader(true);
    }
}
