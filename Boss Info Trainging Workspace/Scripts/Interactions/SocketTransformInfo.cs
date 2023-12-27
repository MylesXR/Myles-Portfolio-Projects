using UnityEngine;

public enum PossibleRotations { _0 = 0, _90 = 1, _180 = 2, _270 = 3 }
public class SocketTransformInfo : MonoBehaviour
{
    public PossibleRotations socketRotationX;
    public PossibleRotations socketRotationY;
    public PossibleRotations socketRotationZ;

}
